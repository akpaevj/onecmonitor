using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.ViewModels.Clusters;
using OnecMonitor.Server.ViewModels.InfoBases;

namespace OnecMonitor.Server.Controllers;

public class ClustersController(AppDbContext appDbContext, IMapper mapper) : Controller
{
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var clusters = await appDbContext.Clusters
            .AsNoTracking()
            .Include(c => c.Agent)
            .ProjectTo<ClusterViewModel>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        var infoBases = await appDbContext.InfoBases
            .AsNoTracking()
            .Include(i => i.Cluster)
            .ProjectTo<InfoBaseViewModel>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        
        return View(new DashboardViewModel
        {
            Clusters = clusters,
            InfoBases = infoBases
        });
    }
    
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vm = id != Guid.Empty
            ? await appDbContext.Clusters
                .AsNoTracking()
                .Include(c => c.Agent)
                .Include(c => c.Credentials)
                .ProjectTo<ClusterEditViewModel>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            : new ClusterEditViewModel();

        if (vm == null)
            return NotFound();
        
        return View(await PrepareViewModel(vm, cancellationToken));
    }

    public async Task<IActionResult> Save(ClusterEditViewModel vm, CancellationToken cancellationToken)
    {
        var model = await appDbContext.Clusters.FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();

        model.CredentialsId = vm.CredentialsId;

        appDbContext.Entry(model).State = EntityState.Modified;
        
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Dashboard");
    }
    
    private async Task<ClusterEditViewModel> PrepareViewModel(ClusterEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.Credentials = await UiHelper.SelectListFrom(
            appDbContext.Credentials,
            i => i.Name,
            vm.CredentialsId,
            cancellationToken);
        
        vm.Agents = await UiHelper.SelectListFrom(
            appDbContext.Agents,
            i => i.InstanceName,
            vm.AgentId,
            cancellationToken);

        return vm;
    }
}