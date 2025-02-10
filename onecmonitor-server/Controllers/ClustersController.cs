using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
                .ProjectTo<ClusterEditViewModel>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken)
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

        if (isNew)
            appDbContext.Entry(model).State = EntityState.Added;

        mapper.Map(vm, model);
        
        var infoBasesIds = vm.InfoBases.Select(c => Guid.Parse(c.Id));
        
        var newInfobases = await appDbContext.InfoBases
            .Where(c => infoBasesIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        
        // add new
        newInfobases
            .Where(c => !model.InfoBases.Contains(c))
            .ToList()
            .ForEach(model.InfoBases.Add);
        // remove deleted
        model.InfoBases
            .Where(c => !newInfobases.Contains(c))
            .ToList()
            .ForEach(c => model.InfoBases.Remove(c));
        
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    private async Task<ClusterEditViewModel> PrepareViewModel(ClusterEditViewModel vm, CancellationToken cancellationToken)
    {
        var agents = await appDbContext.Agents.ToListAsync(cancellationToken);
        vm.Agents = new SelectList(
            agents.Select(c => new { Id = c.Id.ToString(), Name = c.InstanceName }), 
            nameof(Cluster.Id),
            nameof(Cluster.Name),
            vm.AgentId.ToString());
        
        var items = await appDbContext.InfoBases.ToListAsync(cancellationToken);
        var selectList = items
            .Where(i => vm.InfoBases.FirstOrDefault(c => i.Id.ToString() == c.Id) == null).ToList();
        
        vm.AvailableInfoBases = mapper.Map<List<SelectableItem>>(selectList);

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
        var connectedAgents = connectionsManager.GetConnectedAgents(appDbContext.Agents.ToList());

        foreach (var commandsConnection in connectedAgents.Select(agent => connectionsManager.GetCommandsSubscriberConnection(agent.Id)))
        {
            try
            {
                var clusters = await commandsConnection.GetV8Clusters(cancellationToken);
                var a = 1;
            }
            catch (Exception e)
            {
                return View("Error", new ErrorViewModel()
                {
                    Message = e.Message
                });
            }
        }
            
        return RedirectToAction("Index", "Clusters");
    }
}