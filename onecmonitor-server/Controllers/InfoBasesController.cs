using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels.InfoBases;

namespace OnecMonitor.Server.Controllers;

public class InfoBasesController(AppDbContext appDbContext, AgentsConnectionsManager connectionsManager, IMapper mapper) : Controller
{
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vm = id == Guid.Empty
            ? new InfoBaseEditViewModel()
            : await appDbContext.InfoBases
                .Include(c => c.Cluster)
                .Include(c => c.Credentials)
                .ProjectTo<InfoBaseEditViewModel>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (vm == null)
            return NotFound();
        
        return View(await PrepareViewModel(vm, cancellationToken));
    }
    
    public async Task<IActionResult> Save(InfoBaseEditViewModel vm, CancellationToken cancellationToken)
    {
        var model = await appDbContext.InfoBases.FirstOrDefaultAsync(c => c.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();
        
        model.CredentialsId = vm.CredentialsId;
        model.PublishAddress = vm.PublishAddress;
        model.Name = vm.Name;
        
        appDbContext.Entry(model).State = EntityState.Modified;

        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Dashboard", "Clusters");
    }
    
    private async Task<InfoBaseEditViewModel> PrepareViewModel(InfoBaseEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.Clusters = await UiHelper.SelectListFrom(
            appDbContext.Clusters,
            i => i.Name,
            vm.ClusterId,
            cancellationToken);
        
        vm.Credentials = await UiHelper.SelectListFrom(
            appDbContext.Credentials,
            i => i.Name,
            vm.CredentialsId,
            cancellationToken);

        return vm;
    }
}