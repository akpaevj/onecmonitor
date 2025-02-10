using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.Agents;

namespace OnecMonitor.Server.Controllers
{
    public class AgentsController(AppDbContext appDbContext, AgentsConnectionsManager connectionsManager, IMapper mapper)
        : Controller
    {
        public async Task<IActionResult> Index()
        {
            var viewModel = new AgentsIndexViewModel();

            var savedAgents = await appDbContext.Agents.ToListAsync();
            var connectedAgents = connectionsManager.GetConnectedAgents(savedAgents);

            foreach (var agent in savedAgents)
            {
                var connectedAgent = connectedAgents.FirstOrDefault(c => Equals(c, agent));

                viewModel.Agents.Add(new AgentsListItemViewModel()
                {
                    Id = agent.Id,
                    InstanceName = agent.InstanceName,
                    IsConnected = connectedAgent != null
                });
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
        {
            var agent = await appDbContext.Agents
                .Include(c => c.Clusters)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            
            if (agent == null)
                return NotFound();

            var agentConnection = connectionsManager.GetCommandsSubscriberConnection(agent.Id);
            
            var installedPlatforms = agentConnection switch
            {
                null => [],
                _ => await agentConnection.GetInstalledPlatforms(cancellationToken)
            };
            
            var services = agentConnection switch
            {
                null => [],
                _ => await agentConnection.GetV8Services(cancellationToken)
            };

            var vm = new AgentEditViewModel
            {
                Id = agent.Id,
                InstanceName = agent.InstanceName,
                IsConnected = agentConnection != null,
                InstalledPlatforms = installedPlatforms,
                Services = services,
                Clusters = mapper.Map<List<SelectableItemViewModel>>(agent.Clusters)
            };
            
            return View(await PrepareVewModel(vm, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Save(AgentEditViewModel vm, CancellationToken cancellationToken)
        {
            var model = await appDbContext.Agents
                .Include(c => c.Clusters)
                .FirstOrDefaultAsync(c => c.Id == vm.Id, cancellationToken);
            
            if (model == null)
                return NotFound();
            
            await UiHelper.UpdateModelItems(appDbContext.Clusters, vm.Clusters, model.Clusters, cancellationToken);
            
            await appDbContext.SaveChangesAsync(cancellationToken);
            
            return RedirectToAction("Index");
        }
        
        public async Task<IActionResult> Delete(Guid id)
        {
            if (connectionsManager.IsConnected(id))
            {
                return View("Error", new ErrorViewModel()
                {
                    Message = "Connected agent cannot be deleted"
                });
            }

            var item = appDbContext.Agents.FirstOrDefault(c => c.Id == id);

            appDbContext.Agents.Remove(item!);

            await appDbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        
        private async Task<AgentEditViewModel> PrepareVewModel(AgentEditViewModel vm, CancellationToken cancellationToken)
        {
            vm.AvailableClusters = await UiHelper.SelectableItemsFrom(
                appDbContext.Clusters,
                vm.Clusters,
                mapper, 
                cancellationToken);
            
            return vm;
        }
    }
}
