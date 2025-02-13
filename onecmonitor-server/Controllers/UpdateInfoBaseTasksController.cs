using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.UpdateInfoBaseTasks;

namespace OnecMonitor.Server.Controllers;

public class UpdateInfoBaseTasksController(AppDbContext appDbContext, AgentsConnectionsManager connectionsManager, IMapper mapper)
    : Controller
{
    public async Task<IActionResult> Index()
        => View(await appDbContext.UpdateInfoBaseTasks
            .Include(c => c.Results)
            .ProjectTo<UpdateInfoBaseTaskListItemViewModel>(mapper.ConfigurationProvider)
            .ToListAsync());
    
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vm = id != Guid.Empty
            ? await appDbContext.UpdateInfoBaseTasks
                .AsNoTracking()
                .Include(c => c.Configurations)
                .Include(c => c.InfoBases)
                .ProjectTo<UpdateInfoBaseTaskEditViewModel>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken)
            : new UpdateInfoBaseTaskEditViewModel();

        if (vm == null)
            return NotFound();
        
        return View(await PrepareViewModel(vm, cancellationToken));
    }

    public async Task<IActionResult> Save(UpdateInfoBaseTaskEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;

        var configIds = vm.Configurations.Select(c => c.Id).ToList();
        var configs = appDbContext.Configurations
            .AsNoTracking()
            .Where(c => configIds.Contains(c.Id.ToString()))
            .ToList();
        
        var countOfUpdatesAndConfigs = configs.Count(c => c.IsUpdate || c.IsConfiguration);
        if (countOfUpdatesAndConfigs > 0)
            ModelState.AddModelError(
                nameof(UpdateInfoBaseTaskEditViewModel.Configurations),
                "Список конфигураций может содержать только одну конфигурацию или обновление конфигурации");
        
        if (!ModelState.IsValid)
            return View("Edit", await PrepareViewModel(vm, cancellationToken));
        
        var model = isNew ? new UpdateInfoBaseTask
        {
            Id = Guid.NewGuid()
        } : await appDbContext.UpdateInfoBaseTasks
            .Include(c => c.InfoBases)
            .Include(c => c.Configurations)
            .Include(c => c.Results)
            .FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
                
        if (model == null)
            return NotFound();

        if (isNew)
            appDbContext.Entry(model).State = EntityState.Added;

        mapper.Map(vm, model);

        await UiHelper.UpdateModelItems(appDbContext.InfoBases, vm.InfoBases, model.InfoBases, cancellationToken);
        await UiHelper.UpdateModelItems(appDbContext.Configurations, vm.Configurations, model.Configurations, cancellationToken);
        
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var task = await appDbContext.UpdateInfoBaseTasks
                .AsNoTracking()
                .Include(c => c.InfoBases)
                .ThenInclude(c => c.Cluster)
                .ThenInclude(c => c.Agent)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        
            if (task == null)
                return NotFound();
        
            var affectedAgents = task.InfoBases.Select(c => c.Cluster.Agent).Distinct().ToList();
            var connectedAgents = connectionsManager.GetConnectedAgents(affectedAgents);

            foreach (var agent in connectedAgents)
            {
                var commandsConnection = connectionsManager.GetCommandsSubscriberConnection(agent.Id)!;
                await commandsConnection.RequestInfoBasesUpdating(cancellationToken);
            }
        
            var taskToUpdate = await appDbContext.UpdateInfoBaseTasks.FindAsync([id], cancellationToken);
            taskToUpdate!.StartDateTime = DateTime.Now;
            await appDbContext.SaveChangesAsync(cancellationToken);
        
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            return View("Error", new ErrorViewModel(e.Message));
        }
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await appDbContext.UpdateInfoBaseTasks.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            appDbContext.UpdateInfoBaseTasks.Remove(item!);
            
            await appDbContext.SaveChangesAsync(cancellationToken);
            
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            return View("Error", new ErrorViewModel(ex.ToString()));
        }
    }

    public async Task<IActionResult> Log(Guid id, CancellationToken cancellationToken)
    {
        var results = await appDbContext.UpdateInfoBaseTaskResults
            .Where(c => c.UpdateInfoBaseTaskId == id)
            .Include(c => c.InfoBase)
            .Include(c => c.Log)
            .ToListAsync(cancellationToken);

        return View(results);
    }
    
    private async Task<UpdateInfoBaseTaskEditViewModel> PrepareViewModel(UpdateInfoBaseTaskEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.AvailableConfigurations = await UiHelper.SelectableItemsFrom(
            appDbContext.Configurations,
            vm.Configurations,
            mapper,
            cancellationToken);
        
        vm.AvailableInfoBases = await UiHelper.SelectableItemsFrom(
            appDbContext.InfoBases,
            vm.InfoBases,
            mapper,
            cancellationToken);
        
        return vm;
    }
}