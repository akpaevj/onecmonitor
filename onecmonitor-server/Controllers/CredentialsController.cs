using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.Credentials;

namespace OnecMonitor.Server.Controllers;

public class CredentialsController(AppDbContext appDbContext, IMapper mapper) : Controller
{
    public async Task<IActionResult> Index()
        => View(await appDbContext.Credentials.ProjectTo<CredentialsListItemViewModel>(mapper.ConfigurationProvider).ToListAsync());
    
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vm = id != Guid.Empty
            ? await appDbContext.Credentials
                .AsNoTracking()
                .Include(c => c.Clusters)
                .Include(c => c.InfoBases)
                .ProjectTo<CredentialsEditViewModel>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            : new CredentialsEditViewModel();

        if (vm == null)
            return NotFound();
        
        return View(await PrepareViewModel(vm, cancellationToken));
    }
    
    public async Task<IActionResult> Save(CredentialsEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;
        
        if (!ModelState.IsValid)
            return View("Edit", await PrepareViewModel(vm, cancellationToken));
        
        var model = isNew ? new Credentials
        {
            Id = Guid.NewGuid()
        } : await appDbContext.Credentials
            .Include(c => c.InfoBases)
            .Include(c => c.Clusters)
            .FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();

        if (isNew)
            appDbContext.Entry(model).State = EntityState.Added;

        mapper.Map(vm, model);

        await UiHelper.UpdateModelItems(appDbContext.InfoBases, vm.InfoBases, model.InfoBases, cancellationToken);
        await UiHelper.UpdateModelItems(appDbContext.Clusters, vm.Clusters, model.Clusters, cancellationToken);
        
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    private async Task<CredentialsEditViewModel> PrepareViewModel(CredentialsEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.AvailableClusters = await UiHelper.SelectableItemsFrom(
            appDbContext.Clusters,
            vm.Clusters,
            mapper, 
            cancellationToken);
        
        vm.AvailableInfoBases = await UiHelper.SelectableItemsFrom(
            appDbContext.InfoBases,
            vm.InfoBases,
            mapper, 
            cancellationToken);

        return vm;
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await appDbContext.Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        
        if (item == null)
            return NotFound();
        
        appDbContext.Entry(item).State = EntityState.Deleted;
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}