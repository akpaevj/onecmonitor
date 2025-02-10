using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
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
                .Include(c => c.Extensions)
                .Include(c => c.InfoBases)
                .Include(c => c.Configuration)
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
        
        if (!ModelState.IsValid)
            return View("Edit", await PrepareViewModel(vm, cancellationToken));
        
        var model = isNew ? new UpdateInfoBaseTask
        {
            Id = Guid.NewGuid()
        } : await appDbContext.UpdateInfoBaseTasks
            .Include(c => c.InfoBases)
            .Include(c => c.Extensions)
            .Include(c => c.Results)
            .FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
                
        if (model == null)
            return NotFound();

        if (isNew)
            appDbContext.Entry(model).State = EntityState.Added;

        mapper.Map(vm, model);

        await UiHelper.UpdateModelItems(appDbContext.InfoBases, vm.InfoBases, model.InfoBases, cancellationToken);
        await UiHelper.UpdateModelItems(appDbContext.Configurations, vm.Extensions, model.Extensions, cancellationToken);
        
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    private async Task<UpdateInfoBaseTaskEditViewModel> PrepareViewModel(UpdateInfoBaseTaskEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.Configurations = await UiHelper.SelectListFrom(
            appDbContext.Configurations.Where(c => !c.IsExtension),
            i => $"{i.Name} ({i.Version})",
            vm.ConfigurationId,
            cancellationToken);

        vm.AvailableExtensions = await UiHelper.SelectableItemsFrom(
            appDbContext.Configurations.Where(c => c.IsExtension),
            vm.Extensions,
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