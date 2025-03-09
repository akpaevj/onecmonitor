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
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var viewModel = new List<AgentViewModel>();

            var savedAgents = await appDbContext.Agents.ToListAsync(cancellationToken);
            
            foreach (var agent in savedAgents)
            {
                var agentConnection = connectionsManager.GetAgentConnection(agent.Id);
            
                var installedPlatforms = agentConnection == null ? [] : await agentConnection.GetInstalledPlatforms(cancellationToken);
                var ragents = agentConnection == null ? [] : await agentConnection.GetRagentServices(cancellationToken);
                var rases = agentConnection == null ? [] : await agentConnection.GetRasServices(cancellationToken);
                
                viewModel.Add(new AgentViewModel
                {
                    Id = agent.Id,
                    InstanceName = agent.InstanceName,
                    IsConnected = agentConnection != null,
                    InstalledPlatforms = installedPlatforms,
                    RagentServices = ragents,
                    RasServices = rases,
                });
            }

            return View(viewModel);
        }
        
        public async Task<IActionResult> Delete(Guid id)
        {
            if (connectionsManager.IsConnected(id))
            {
                return View("Error", new ErrorViewModel("Connected agent cannot be deleted"));
            }

            var item = appDbContext.Agents.FirstOrDefault(c => c.Id == id);

            appDbContext.Agents.Remove(item!);

            await appDbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
