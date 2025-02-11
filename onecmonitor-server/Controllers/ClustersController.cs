using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.Clusters;

namespace OnecMonitor.Server.Controllers;

public class ClustersController(AppDbContext appDbContext, AgentsConnectionsManager connectionsManager, IMapper mapper) : Controller
{
    public async Task<IActionResult> Index()
        => View(await appDbContext.Clusters.Include(c => c.Agent).ToListAsync());
    
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vm = id != Guid.Empty
            ? await appDbContext.Clusters
                .AsNoTracking()
                .Include(c => c.Agent)
                .Include(c => c.InfoBases)
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
        var isNew = vm.Id == Guid.Empty;
        
        if (!ModelState.IsValid)
            return View("Edit", await PrepareViewModel(vm, cancellationToken));
        
        var model = isNew ? new Cluster
        {
            Id = Guid.NewGuid()
        } : await appDbContext.Clusters
            .Include(c => c.InfoBases)
            .FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();

        appDbContext.Entry(model).State = isNew ? EntityState.Added : EntityState.Modified;
        
        mapper.Map(vm, model);

        await UiHelper.UpdateModelItems(appDbContext.InfoBases, vm.InfoBases, model.InfoBases, cancellationToken);
        
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
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
        
        vm.AvailableInfoBases = await UiHelper.SelectableItemsFrom(
            appDbContext.InfoBases,
            vm.InfoBases,
            mapper, 
            cancellationToken);

        return vm;
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await appDbContext.Clusters.FindAsync(
            [id], 
            cancellationToken: cancellationToken);
        
        appDbContext.Entry(item!).State = EntityState.Deleted;
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> UpdateClusters(CancellationToken cancellationToken)
    {
        await appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var defaultCredentials = await appDbContext.Credentials
            .FirstOrDefaultAsync(c => c.DefaultForClusters, cancellationToken);
        
        try
        {
            var connectedAgents = connectionsManager.GetConnectedAgents(appDbContext.Agents.ToList());
            foreach (var agent in connectedAgents)
            {
                var commandsConnection = connectionsManager.GetCommandsSubscriberConnection(agent.Id);
                
                if (commandsConnection == null)
                    continue;

                var clusters = await commandsConnection!.GetV8Clusters(cancellationToken);
                var currentIds = await appDbContext.Clusters.Select(c => c.ClusterInternalId).ToListAsync(cancellationToken);
                var newClusters = clusters.Where(c => !currentIds.Contains(c.Id)).ToList();

                foreach (var cluster in newClusters)
                {
                    await appDbContext.Clusters.AddAsync(new Cluster
                    {
                        Id = Guid.NewGuid(),
                        AgentId = agent.Id,
                        ClusterInternalId = cluster.Id,
                        CredentialsId = defaultCredentials?.Id,
                        Name = cluster.Name,
                        Host = cluster.Host,
                        Port = cluster.Port
                    }, cancellationToken);
                }
            }

            await appDbContext.Database.CommitTransactionAsync(cancellationToken);
            await appDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            await appDbContext.Database.RollbackTransactionAsync(cancellationToken);

            return View("Error", new ErrorViewModel(e.Message));
        }
    }
}