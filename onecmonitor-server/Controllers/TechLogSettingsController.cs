using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common.Storage;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.TechLogSettings;

namespace OnecMonitor.Server.Controllers;

public class TechLogSettingsController(AppDbContext appDbContext, ITechLogStorage techLogStorage, AgentsConnectionsManager connectionsManager, IMapper mapper) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await appDbContext.TechLogSettings
            .ProjectTo<TechLogSettingsEditViewModel>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return View(settings ?? new TechLogSettingsEditViewModel());
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(TechLogSettingsEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;
        
        vm.ClickHouseDatabase ??= string.Empty;
        vm.ClickHousePassword ??= string.Empty;
        vm.ClickHouseHost ??= string.Empty;
        vm.ClickHouseUser ??= string.Empty;

        if (!vm.Enabled)
        {
            ModelState.Remove(nameof(vm.ClickHouseHost));
            ModelState.Remove(nameof(vm.ClickHouseDatabase));
            ModelState.Remove(nameof(vm.ClickHousePort));
            ModelState.Remove(nameof(vm.ClickHouseUser));
            ModelState.Remove(nameof(vm.ClickHousePassword));
        }
        
        if (!ModelState.IsValid)
            return View("Index", vm);

        await appDbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await techLogStorage.CheckConnection(cancellationToken);
            
            var model = isNew ? new TechLogSettings
            {
                Id = Guid.NewGuid()
            } : await appDbContext.TechLogSettings.FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
            if (model == null)
                return NotFound();

            appDbContext.Entry(model).State = isNew ? EntityState.Added : EntityState.Modified;
        
            mapper.Map(vm, model);
        
            await appDbContext.SaveChangesAsync(cancellationToken);
            
            await appDbContext.Database.CommitTransactionAsync(cancellationToken);

            var agents = connectionsManager.GetConnectedAgents(await appDbContext.Agents.ToListAsync(cancellationToken));
            var subscribers = connectionsManager.GetCommandSubscribers(agents);
            foreach (var subscriber in subscribers)
                await subscriber.SendUpdateSettingsRequest(cancellationToken);

            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            await appDbContext.Database.RollbackTransactionAsync(cancellationToken);
            return View("Error", new ErrorViewModel(e.ToString()));
        }
    }
}