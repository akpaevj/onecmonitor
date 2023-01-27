using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Host;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels.Log;
using OnecMonitor.Server.ViewModels.TechLogSeances;
using System.Threading;

namespace OnecMonitor.Server.Controllers
{
    public class TechLogSeancesController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly AgentsConnectionsManager _connectionsManager;
        private readonly IClickHouseContext _clickHouseContext;
        private readonly TechLogAnalyzer _analyzerService;
        private readonly ILogger<TechLogSeancesController> _logger;

        public TechLogSeancesController(
            AppDbContext dbContext,
            AgentsConnectionsManager connectionsManager, 
            IClickHouseContext clickHouseContext,
            TechLogAnalyzer analyzerService,
            ILogger<TechLogSeancesController> logger)
        {
            _dbContext = dbContext;
            _connectionsManager = connectionsManager;
            _clickHouseContext = clickHouseContext;
            _analyzerService = analyzerService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, CancellationToken cancellationToken = default)
        {
            var viewModel = new TechLogSeancesIndexViewModel
            {
                CurrentPage = pageNumber,
            };

            var items = await _dbContext.TechLogSeances
                .AsNoTracking()
                .OrderBy(c => c.StartDateTime)
                .Skip((viewModel.CurrentPage - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .ToListAsync(cancellationToken);

            viewModel.Seances = items.Select(c => new TechLogSeancesListItemViewModel()
            {
                Id = c.Id,
                Description = c.Description,
                StartMode = c.StartMode,
                StartDateTime= c.StartDateTime,
                Duration = c.Duration
            }).ToList();

            var eventsCount = await _dbContext.TechLogSeances.CountAsync(cancellationToken);
            var pagesCount = eventsCount / viewModel.PageSize;
            if (eventsCount % viewModel.PageSize != 0)
                pagesCount++;

            viewModel.PagesCount = pagesCount;

            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
            => View(await GetViewModel(id, cancellationToken));

        [HttpPost]
        public async Task<IActionResult> Edit(TechLogSeanceEditViewModel viewModel, Guid[] connectedAgents, Guid[] connectedTemplates, CancellationToken cancellationToken)
        {
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

            var newConnectedAgents = await _dbContext.Agents
                .Where(c => connectedAgents
                    .Contains(c.Id))
                .ToListAsync(cancellationToken);

            var newConnectedTemplates = await _dbContext.LogTemplates
                .Where(c => connectedTemplates
                    .Contains(c.Id))
                .ToListAsync(cancellationToken);

            if ((viewModel.StartMode == TechLogSeanceStartMode.Immediately || viewModel.StartMode == TechLogSeanceStartMode.Scheduled) && viewModel.Duration <= 0)
                ModelState.AddModelError(nameof(viewModel.Duration), "Duration cannot be less than 1 minute");

            if (viewModel.StartMode == TechLogSeanceStartMode.Scheduled && viewModel.StartDateTime < DateTime.UtcNow)
                ModelState.AddModelError(nameof(viewModel.StartDateTime), "Scheduled seance must happend in the future");

            if (viewModel.StartMode != TechLogSeanceStartMode.Monitor && newConnectedAgents.Count == 0)
                ModelState.AddModelError(nameof(viewModel.ConnectedAgents), "It doesn't make sense to start seance without connected agents=)");

            if (newConnectedTemplates.Count == 0)
                ModelState.AddModelError(nameof(viewModel.ConnectedTemplates), "You must connect templates");

            if (!ModelState.IsValid)
                return View(await GetViewModel(viewModel, cancellationToken));

            var affectedAgents = new List<Agent>();

            if (viewModel.Id == Guid.Empty)
            {
                var item = new TechLogSeance()
                {
                    Id = Guid.NewGuid(),
                    Description = viewModel.Description,
                    StartMode = viewModel.StartMode,
                    StartDateTime = viewModel.StartDateTime,
                    Duration = viewModel.Duration,
                    ConnectedTemplates = newConnectedTemplates,
                    ConnectedAgents = newConnectedAgents
                };

                affectedAgents.AddRange(item.ConnectedAgents);

                await _dbContext.AddAsync(item, cancellationToken);
            }
            else
            {
                var item = await _dbContext.TechLogSeances
                    .Include(c => c.ConnectedTemplates)
                    .Include(c => c.ConnectedAgents)
                    .SingleAsync(c => c.Id == viewModel.Id, cancellationToken);

                item.Description = viewModel.Description;
                item.StartMode = viewModel.StartMode;
                item.StartDateTime = viewModel.StartDateTime;
                item.Duration = viewModel.Duration;

                // add new items
                newConnectedTemplates.Where(c => !item.ConnectedTemplates.Contains(c)).ToList().ForEach(item.ConnectedTemplates.Add);
                // remove deleted items
                item.ConnectedTemplates.Where(c => !newConnectedTemplates.Contains(c)).ToList().ForEach(c => item.ConnectedTemplates.Remove(c));

                // add new items
                newConnectedAgents.Where(c => !item.ConnectedAgents.Contains(c)).ToList().ForEach(item.ConnectedAgents.Add);
                // fix affected agents to send notify
                affectedAgents.AddRange(item.ConnectedAgents);
                // remove deleted items
                item.ConnectedAgents.Where(c => !newConnectedAgents.Contains(c)).ToList().ForEach(c => item.ConnectedAgents.Remove(c));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _connectionsManager.UpdateTechLogSeances(affectedAgents, cancellationToken);

            return Redirect("/TechLogSeances");
        }

        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var item = await _dbContext.TechLogSeances.Include(c => c.ConnectedAgents).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (item == null)
                    throw new Exception($"Failed to find item with id {id}");

                _dbContext.Entry(item).State = EntityState.Deleted;

                await _clickHouseContext.DeleteTechLogSeanceData(id.ToString(), cancellationToken);

                await _dbContext.Database.CommitTransactionAsync(cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                await _connectionsManager.UpdateTechLogSeances(item.ConnectedAgents, cancellationToken);

                return Redirect("/TechLogSeances");
            }
            catch
            {
                await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
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
                TechLogFilters = await _dbContext.TechLogFilters.ToListAsync(cancellationToken)
            };

            filter = filter.Trim();

            if (string.IsNullOrEmpty(filter))
                filter = $"_seance_id = toUUID('{id}')";
            else
                filter = $"_seance_id = toUUID('{id}') and {filter}";

            var items = await _clickHouseContext.GetTjEvents(viewModel.PageSize, (viewModel.CurrentPage - 1) * viewModel.PageSize, filter, cancellationToken);
            viewModel.TjEvents = items.Select(c => new TechLogListItemViewModel(c)).ToList();

            var eventsCount = await _clickHouseContext.GetTjEventsCount(filter, cancellationToken);
            var pagesCount = eventsCount / viewModel.PageSize;
            if (eventsCount % viewModel.PageSize != 0)
                pagesCount++;

            viewModel.PagesCount = pagesCount;

            return View(viewModel);
        }

        public async Task<IActionResult> CallTimeline(Guid id, CancellationToken cancellationToken)
        {
            var chain = await _analyzerService.GetCallEventsChain(id, cancellationToken);

            return View(new CallChainViewModel { Chain = chain });
        }

        public async Task<IActionResult> LockWaitingTimeline(Guid id, CancellationToken cancellationToken)
        {
            var graph = await _analyzerService.GetLockWaitingGraph(id, cancellationToken);

            return View(new LockWaitingTimelineViewModel { Graph = graph });
        }

        public async Task<IActionResult> LockWaitingGraph(Guid id, CancellationToken cancellationToken)
        {
            var graph = await _analyzerService.GetLockWaitingGraph(id, cancellationToken);

            return View(new LockWaitingGraphViewModel { Graph = graph });
        }

        private async Task<TechLogSeanceEditViewModel> GetViewModel(Guid id, CancellationToken cancellationToken)
        {
            var viewModel = new TechLogSeanceEditViewModel();

            if (id != Guid.Empty)
            {
                var item = await _dbContext.TechLogSeances
                    .AsNoTracking()
                    .Include(c => c.ConnectedTemplates)
                    .Include(c => c.ConnectedAgents)
                    .SingleAsync(c => c.Id == id, cancellationToken);

                viewModel.Id = item!.Id;
                viewModel.Description = item.Description;
                viewModel.StartMode = item.StartMode;
                viewModel.StartDateTime = item.StartDateTime;
                viewModel.Duration = item.Duration;
                viewModel.ConnectedTemplates = item.ConnectedTemplates.Select(c => (c.Id, c.Name)).ToList();
                viewModel.ConnectedAgents = item.ConnectedAgents.Select(c => (c.Id, c.InstanceName)).ToList();
            }
            else
            {
                viewModel.StartMode =  TechLogSeanceStartMode.Immediately;
                viewModel.StartDateTime = DateTime.UtcNow;
                viewModel.Duration = 6;
                viewModel.ConnectedAgents = new List<(Guid Id, string Name)>();
                viewModel.ConnectedTemplates = new List<(Guid Id, string Name)>();
            }

            return await GetViewModel(viewModel, cancellationToken);
        }

        private async Task<TechLogSeanceEditViewModel> GetViewModel(TechLogSeanceEditViewModel viewModel, CancellationToken cancellationToken)
        {
            viewModel.AllAgents = new SelectList(await _dbContext.Agents.OrderBy(c => c.InstanceName)
                .ToListAsync(cancellationToken), nameof(Agent.Id), nameof(Agent.InstanceName));

            viewModel.AllTemplates = new SelectList(await _dbContext.LogTemplates.OrderBy(c => c.Name)
                .ToListAsync(cancellationToken), nameof(LogTemplate.Id), nameof(LogTemplate.Name));

            return viewModel;
        }
    }
}
