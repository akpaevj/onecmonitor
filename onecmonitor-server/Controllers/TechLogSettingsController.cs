using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.EventLogSettings;
using OnecMonitor.Server.ViewModels.TechLogSettings;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Models;
using OneSwiss.Common.Services;
using OneSwiss.Common.Storage;

namespace OnecMonitor.Server.Controllers;

public class TechLogSettingsController(
    TechLogRepositoryManager repositoryManager,
    AppDbContext appDbContext,
    AgentsConnectionsManager connectionsManager, 
    IMapper mapper) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await appDbContext.TechLogSettings
            .ProjectTo<TechLogSettingsEditViewModel>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return View(await PrepareViewModel(settings ?? new TechLogSettingsEditViewModel(), cancellationToken));
    }
    
    // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
    [HttpPost]
    public async Task<IActionResult> Save(TechLogSettingsEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;

        if (!vm.Enabled)
            ModelState.Clear();
        else
        {
            if (!vm.DbmsId.HasValue || vm.DbmsId.Value == Guid.Empty)
                ModelState.AddModelError(nameof(EventLogSettingsEditViewModel.DbmsId), "Не указана СУБД");
        }
        
        if (!ModelState.IsValid)
            return View("Index", await PrepareViewModel(vm, cancellationToken));

        await appDbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var model = isNew ? new TechLogSettings()
            {
                Id = Guid.NewGuid()
            } : await appDbContext.TechLogSettings.FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
            if (model == null)
                return NotFound();

            appDbContext.Entry(model).State = isNew ? EntityState.Added : EntityState.Modified;
        
            mapper.Map(vm, model);
        
            await appDbContext.SaveChangesAsync(cancellationToken);

            if (vm.Enabled)
                await InitDataBase(cancellationToken);
            
            await appDbContext.Database.CommitTransactionAsync(cancellationToken);
            
            TechLogHelper.UpdateTechLogSettings(mapper, repositoryManager, appDbContext);

            await RaiseUpdateSettings(cancellationToken);

            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            await appDbContext.Database.RollbackTransactionAsync(cancellationToken);
            return View("Error", new ErrorViewModel(e.ToString()));
        }
    }

    private async Task InitDataBase(CancellationToken cancellationToken)
    {
        var settings = await appDbContext.TechLogSettings
            .AsNoTracking()
            .Include(c => c.Dbms)
            .Include(c => c.Credentials)
            .FirstOrDefaultAsync(cancellationToken);

        var settingsDto = mapper.Map<TechLogSettingsDto>(settings);
        
        using var context = new ClickHouseContext(
            settingsDto.Dbms, 
            settingsDto.Credentials, 
            settingsDto.DatabaseName, 
            settingsDto.Table);
        
        await context.InitTechLogTable(cancellationToken);
    }

    private async Task RaiseUpdateSettings(CancellationToken cancellationToken)
    {
        var connections = await connectionsManager.GetActiveAgentsConnections(cancellationToken);
        
        foreach (var connection in connections)
            await connection.SendSettingsRequest(cancellationToken);
    }
    
    private async Task<TechLogSettingsEditViewModel> PrepareViewModel(TechLogSettingsEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.AvailableDbms = await UiHelper.SelectListFrom(
            appDbContext.Dbms.Where(c => c.Type == DbmsType.ClickHouse),
            i => i.Name,
            vm.DbmsId,
            cancellationToken);
        
        vm.AvailableCredentials = await UiHelper.SelectListFrom(
            appDbContext.Credentials,
            i => i.Name,
            vm.CredentialsId,
            cancellationToken);

        return vm;
    }
}