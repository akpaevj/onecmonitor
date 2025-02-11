using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.InfoBases;

namespace OnecMonitor.Server.Controllers;

public class InfoBasesController(AppDbContext appDbContext, AgentsConnectionsManager connectionsManager, IMapper mapper) : Controller
{
    // GET
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(new InfoBasesIndexViewModel
        {
            Items = await appDbContext.InfoBases
                .Include(c => c.Cluster)
                .ProjectTo<InfoBaseListItemViewModel>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken)
        });

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
    
    public async Task<IActionResult> Save(Guid id, InfoBaseEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = id == Guid.Empty;
        
        if (!ModelState.IsValid)
            return View("Edit", await PrepareViewModel(vm, cancellationToken));
        
        var model = isNew ? new InfoBase
        {
            Id = Guid.NewGuid()
        } : await appDbContext.InfoBases.FindAsync([id], cancellationToken);
        
        if (model == null)
            return NotFound();

        if (isNew)
            await appDbContext.InfoBases.AddAsync(model, cancellationToken);
        
        mapper.Map(vm, model);

        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
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
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await appDbContext.InfoBases.FindAsync(
            [id], 
            cancellationToken: cancellationToken);
        
        appDbContext.Entry(item!).State = EntityState.Deleted;
        await appDbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> UpdateInfoBases(CancellationToken cancellationToken)
    {
        await appDbContext.Database.BeginTransactionAsync(cancellationToken);

        var defaultCredentials = await appDbContext.Credentials
            .FirstOrDefaultAsync(c => c.DefaultV8Admin, cancellationToken);
        if (defaultCredentials == null)
        {
            await appDbContext.Database.RollbackTransactionAsync(cancellationToken);
            return View("Error", new ErrorViewModel("Before updating you must specified default infobases admin credentials"));
        }
        
        try
        {
            var connectedAgents = connectionsManager.GetConnectedAgents(appDbContext.Agents.ToList());
            foreach (var agent in connectedAgents)
            {
                var commandsConnection = connectionsManager.GetCommandsSubscriberConnection(agent.Id);
                
                if (commandsConnection == null)
                    continue;
                
                var clusters = await appDbContext.Clusters
                    .Include(c => c.Credentials)
                    .Where(c => c.AgentId == agent.Id)
                    .ToListAsync(cancellationToken);

                foreach (var cluster in clusters)
                {
                    var infoBases = await commandsConnection.GetV8InfoBasesSummaries(cluster, cancellationToken);
                    var currentIds = await appDbContext.InfoBases.Select(c => c.InfoBaseInternalId).ToListAsync(cancellationToken);
                    var newInfoBases = infoBases.Where(c => !currentIds.Contains(c.Id)).ToList();
                    
                    foreach (var infoBase in newInfoBases)
                    {
                        await appDbContext.InfoBases.AddAsync(new InfoBase()
                        {
                            Id = Guid.NewGuid(),
                            Name = infoBase.Name,
                            InfoBaseName = infoBase.Name,
                            InfoBaseInternalId = infoBase.Id,
                            ClusterId = cluster.Id,
                            CredentialsId = defaultCredentials.Id,
                            PublishAddress = "http://localhost"
                        }, cancellationToken);
                    }
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