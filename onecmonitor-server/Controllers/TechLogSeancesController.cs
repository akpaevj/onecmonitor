using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common.Storage;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels.TechLogSeances;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using OnecMonitor.Server.Helpers;

namespace OnecMonitor.Server.Controllers
{
    public class TechLogSeancesController(
        AppDbContext dbContext,
        AgentsConnectionsManager connectionsManager,
        ITechLogStorage clickHouseContext,
        TechLogAnalyzer analyzerService,
        IMapper mapper)
        : Controller
    {
        public async Task<IActionResult> Index(int pageNumber = 1, CancellationToken cancellationToken = default)
        {
            var viewModel = new TechLogSeancesIndexViewModel
            {
                CurrentPage = pageNumber,
            };

            viewModel.Seances = await dbContext.TechLogSeances
                .AsNoTracking()
                .OrderBy(c => c.StartDateTime)
                .Skip((viewModel.CurrentPage - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .ProjectTo<TechLogSeancesListItemViewModel>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var eventsCount = await dbContext.TechLogSeances.CountAsync(cancellationToken);
            var pagesCount = eventsCount / viewModel.PageSize;
            if (eventsCount % viewModel.PageSize != 0)
                pagesCount++;

            viewModel.PagesCount = pagesCount;

            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
        {
            var vm = id != Guid.Empty
                ? await dbContext.TechLogSeances
                    .AsNoTracking()
                    .Include(c => c.Templates)
                    .Include(c => c.Agents)
                    .ProjectTo<TechLogSeanceEditViewModel>(mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                : new TechLogSeanceEditViewModel();

            if (vm == null)
                return NotFound();
        
            return View(await PrepareViewModel(vm, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TechLogSeanceEditViewModel viewModel, CancellationToken cancellationToken)
        {
            var isNew = viewModel.Id == Guid.Empty;
            
            ModelState[nameof(TechLogSeanceEditViewModel.StartDateTime)]?.Errors.Clear();

            viewModel.StartDateTime = viewModel.StartMode switch
            {
                TechLogSeanceStartMode.Monitor => DateTime.MinValue,
                TechLogSeanceStartMode.Immediately => DateTime.UtcNow,
                _ => viewModel.StartDateTime
            };
            viewModel.StartDateTime = DateTime.SpecifyKind(viewModel.StartDateTime, DateTimeKind.Utc);

            viewModel.Duration = viewModel.StartMode switch
            {
                TechLogSeanceStartMode.Monitor => 0,
                _ => viewModel.Duration
            };

            if (viewModel.StartMode is TechLogSeanceStartMode.Immediately or TechLogSeanceStartMode.Scheduled && viewModel.Duration <= 0)
                ModelState.AddModelError(nameof(viewModel.Duration), "Duration cannot be less than 1 minute");

            if (viewModel.StartMode == TechLogSeanceStartMode.Scheduled && viewModel.StartDateTime < DateTime.UtcNow)
                ModelState.AddModelError(nameof(viewModel.StartDateTime), "Scheduled seance must happened in the future");
            
            var connectedAgents = viewModel.Agents.Select(c => Guid.Parse(c.Id));
            var newConnectedAgents = await dbContext.Agents
                .Where(c => connectedAgents
                    .Contains(c.Id))
                .ToListAsync(cancellationToken);

            var connectedTemplates = viewModel.Templates.Select(c => Guid.Parse(c.Id));
            var newConnectedTemplates = await dbContext.LogTemplates
                .Where(c => connectedTemplates
                    .Contains(c.Id))
                .ToListAsync(cancellationToken);

            if (viewModel.StartMode != TechLogSeanceStartMode.Monitor && newConnectedAgents.Count == 0)
                ModelState.AddModelError(nameof(viewModel.Agents), "It doesn't make sense to start seance without connected agents=)");

            if (newConnectedTemplates.Count == 0)
                ModelState.AddModelError(nameof(viewModel.Templates), "You must connect templates");

            if (!ModelState.IsValid)
                return View(await PrepareViewModel(viewModel, cancellationToken));
            
            var model = isNew ? new TechLogSeance()
            {
                Id = Guid.NewGuid()
            } : await dbContext.TechLogSeances
                .Include(c => c.Agents)
                .Include(c => c.Templates)
                .FirstOrDefaultAsync(i => i.Id == viewModel.Id, cancellationToken);
        
            if (model == null)
                return NotFound();
            
            if (isNew)
                dbContext.Entry(model).State = EntityState.Added;
            
            mapper.Map(viewModel, model);

            // add new items
            newConnectedTemplates.Where(c => !model.Templates.Contains(c)).ToList().ForEach(model.Templates.Add);
            // remove deleted items
            model.Templates.Where(c => !newConnectedTemplates.Contains(c)).ToList().ForEach(c => model.Templates.Remove(c));

            // add new items
            newConnectedAgents.Where(c => !model.Agents.Contains(c)).ToList().ForEach(model.Agents.Add);
            // fix affected agents to send notify
            var affectedAgents = model.Agents.ToList();
            // remove deleted items
            model.Agents.Where(c => !newConnectedAgents.Contains(c)).ToList().ForEach(c => model.Agents.Remove(c));

            await dbContext.SaveChangesAsync(cancellationToken);
            await connectionsManager.UpdateTechLogSeances(affectedAgents, cancellationToken);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var item = await dbContext.TechLogSeances.Include(c => c.Agents).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (item == null)
                    throw new Exception($"Failed to find item with id {id}");

                dbContext.Entry(item).State = EntityState.Deleted;

                await clickHouseContext.DeleteTechLogSeanceData(id.ToString(), cancellationToken);

                await dbContext.Database.CommitTransactionAsync(cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                await connectionsManager.UpdateTechLogSeances(item.Agents, cancellationToken);

                return RedirectToAction("Index");
            }
            catch
            {
                await dbContext.Database.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<IActionResult> TechLog(Guid id, int pageNumber = 1, [FromQuery] string filter = "", CancellationToken cancellationToken = default)
        {
            var viewModel = new TechLogViewModel
            {
                SeanceId = id,
                CurrentPage = pageNumber,
                Filter = filter,
                TechLogFilters = await dbContext.TechLogFilters.ToListAsync(cancellationToken)
            };

            filter = filter.Trim();

            filter = string.IsNullOrEmpty(filter) ? $"SeanceId = toUUID('{id}')" : $"SeanceId = toUUID('{id}') and {filter}";

            var items = await clickHouseContext.GetTjEvents(viewModel.PageSize, (viewModel.CurrentPage - 1) * viewModel.PageSize, filter, cancellationToken);
            viewModel.TjEvents = items.Select(c => new TechLogListItemViewModel(c)).ToList();

            var eventsCount = await clickHouseContext.GetTjEventsCount(filter, cancellationToken);
            var pagesCount = eventsCount / viewModel.PageSize;
            if (eventsCount % viewModel.PageSize != 0)
                pagesCount++;

            viewModel.PagesCount = pagesCount;

            return View(viewModel);
        }

        public async Task<IActionResult> CallTimeline(Guid id, CancellationToken cancellationToken)
        {
            var chain = await analyzerService.GetCallEventsChain(id, cancellationToken);

            return View(new CallChainViewModel { Chain = chain });
        }

        public async Task<IActionResult> LockWaitingTimeline(Guid id, CancellationToken cancellationToken)
        {
            var graph = await analyzerService.GetLockWaitingGraph(id, cancellationToken);

            return View(new LockWaitingTimelineViewModel { Graph = graph });
        }

        public async Task<IActionResult> LockWaitingGraph(Guid id, CancellationToken cancellationToken)
        {
            var graph = await analyzerService.GetLockWaitingGraph(id, cancellationToken);

            return View(new LockWaitingGraphViewModel { Graph = graph });
        }

        private async Task<TechLogSeanceEditViewModel> PrepareViewModel(TechLogSeanceEditViewModel vm, CancellationToken cancellationToken)
        {
            vm.AvailableAgents = await UiHelper.SelectableItemsFrom(
                dbContext.Agents,
                vm.Agents,
                mapper, 
                cancellationToken);
            
            vm.AvailableTemplates = await UiHelper.SelectableItemsFrom(
                dbContext.LogTemplates,
                vm.Templates,
                mapper, 
                cancellationToken);
            
            return vm;
        }
    }
}
