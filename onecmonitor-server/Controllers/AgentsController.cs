using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.Agents.Index;

namespace OnecMonitor.Server.Controllers
{
    public class AgentsController : Controller
    {
        private readonly AppDbContext _appDbContext;
        private readonly AgentsConnectionsManager _connectionsManager;

        public AgentsController(AppDbContext appDbContext, AgentsConnectionsManager connectionsManager)
        {
            _appDbContext = appDbContext;
            _connectionsManager = connectionsManager;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AgentsIndexViewModel();

            var savedAgents = await _appDbContext.Agents.ToListAsync();
            var connectedAgents = _connectionsManager.GetConnectedAgents(savedAgents);

            foreach (var agent in savedAgents)
            {
                var connectedAgent = connectedAgents.FirstOrDefault(c => c == agent);

                viewModel.Agents.Add(new AgentsListItemViewModel()
                {
                    Id = agent.Id,
                    InstanceName = agent.InstanceName,
                    IsConnected = connectedAgent != null
                });
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            if (_connectionsManager.IsConnected(id))
            {
                return View("Error", new ErrorViewModel()
                {
                    Message = "Connected agent cannot be deleted"
                });
            }

            var item = _appDbContext.Agents.FirstOrDefault(c => c.Id == id);

            _appDbContext.Agents.Remove(item!);

            await _appDbContext.SaveChangesAsync();

            return Redirect("/Agents");
        }
    }
}
