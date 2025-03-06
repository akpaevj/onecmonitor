using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OnecMonitor.Common.Models.MaintenanceTasks;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;
using OnecMonitor.Server.Services;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.MaintenanceTasks;

namespace OnecMonitor.Server.Controllers;

public class MaintenanceTasksController(AppDbContext appDbContext, AgentsConnectionsManager connectionsManager, IMapper mapper) : Controller
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await appDbContext.MaintenanceTasks
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return View(new MaintenanceTasksIndexViewModel
        {
            Items = mapper.Map<List<MaintenanceTaskListItemViewModel>>(items)
        });
    }
    
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return View(await PrepareViewModel(new MaintenanceTaskEditViewModel(), cancellationToken));
        
        var model = await appDbContext.MaintenanceTasks
            .Include(c => c.Steps)
            .Include(c => c.InfoBases)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (model == null)
            return NotFound();
        
        var vm = mapper.Map<MaintenanceTaskEditViewModel>(model);
        var steps = mapper.Map<List<MaintenanceStepViewModel>>(model.Steps);
        vm.Steps = JsonSerializer.Serialize(steps, _jsonOptions);
        
        return View(await PrepareViewModel(vm, cancellationToken));
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(MaintenanceTaskEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;
        
        var viewModelSteps = JsonSerializer.Deserialize<List<MaintenanceStepViewModel>>(vm.Steps, _jsonOptions);
        if (viewModelSteps?.Count == 0)
            ModelState.AddModelError(nameof(MaintenanceTaskEditViewModel.Steps), "Не сконфигурированы шаги обслуживания");
        
        if (!ModelState.IsValid)
            return View("Edit", await PrepareViewModel(vm, cancellationToken));
        
        var model = isNew ? new MaintenanceTask
        {
            Id = Guid.NewGuid()
        } : await appDbContext.MaintenanceTasks
            .Include(c => c.InfoBases)
            .Include(c => c.Steps)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();
        
        mapper.Map(vm, model);
        
        var steps = mapper.Map<List<MaintenanceStep>>(viewModelSteps);
        steps.ForEach(c => c.MaintenanceTaskId = model.Id);

        model.Steps.Except(steps).ToList().ForEach(c =>
        {
            appDbContext.Entry(c).State = EntityState.Deleted;
        });
        steps.Except(model.Steps).ToList().ForEach(c =>
        {
            appDbContext.Entry(c).State = EntityState.Added;
        });
        steps.Intersect(model.Steps).ToList().ForEach(c =>
        {
            appDbContext.Entry(c).State = EntityState.Modified;
        });
        
        await UiHelper.UpdateModelItems(appDbContext.InfoBases, vm.InfoBases, model.InfoBases, cancellationToken);
        
        if (isNew)
            appDbContext.MaintenanceTasks.Add(model);
        else
            appDbContext.Entry(model).State = EntityState.Modified;
        
        await appDbContext.SaveChangesAsync(cancellationToken);
            
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> EditStep(CancellationToken cancellationToken)
    {
        var 
            vm = await HttpContext.Request.ReadFromJsonAsync<MaintenanceStepViewModel>(_jsonOptions, cancellationToken);
        return await UpdateStep(vm!, cancellationToken);
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateStep(MaintenanceStepViewModel vm, CancellationToken cancellationToken)
        => PartialView("EditStep", await PrepareViewModel(vm, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> ValidateStep(MaintenanceStepViewModel vm, CancellationToken cancellationToken)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (vm.Kind)
        {
            case MaintenanceStepKind.LoadExtension:
            case MaintenanceStepKind.UpdateConfiguration:
            case MaintenanceStepKind.LoadConfiguration:
            case MaintenanceStepKind.StartExternalDataProcessor:
                if (vm.FileId == null || vm.FileId == Guid.Empty)
                    ModelState.AddModelError(nameof(vm.FileId), "Не указан файл");
                break;
            case MaintenanceStepKind.LockConnections:
                if (string.IsNullOrEmpty(vm.AccessCode))
                    ModelState.AddModelError(nameof(vm.AccessCode), "Не указан код доступа");
                if (string.IsNullOrEmpty(vm.Message))
                    ModelState.AddModelError(nameof(vm.Message), "Не указан тест сообщения");
                break;
        }

        if (ModelState.IsValid)
            return Json(vm);
        
        var view = PartialView("EditStep", await PrepareViewModel(vm, cancellationToken));
        view.StatusCode = 400;
        
        return view;
    }

    public async Task<IActionResult> Restart(Guid id, CancellationToken cancellationToken)
    {
        var task = await appDbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(c => c.Steps)
                .ThenInclude(c => c.Logs)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (task == null)
            return NotFound();
        
        appDbContext.MaintenanceStepLogs.RemoveRange(task.Steps.SelectMany(c => c.Logs));
        
        task.FinishDateTime = DateTime.MinValue;
        task.IsFaulted = false;
        task.StartDateTime = DateTime.Now;
        
        appDbContext.Entry(task).State = EntityState.Modified;
        
        await appDbContext.SaveChangesAsync(cancellationToken);
        
        return await Start(id, cancellationToken);
    }

    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var task = await appDbContext.MaintenanceTasks
                .AsNoTracking()
                .Include(c => c.Steps)
                    .ThenInclude(c => c.File)
                .Include(c => c.InfoBases)
                    .ThenInclude(ib => ib.Credentials)
                .Include(c => c.InfoBases)
                    .ThenInclude(infoBase => infoBase.Cluster)
                    .ThenInclude(cluster => cluster.Agent)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (task == null)
                return NotFound();
        
            var affectedAgents = task.InfoBases.Select(c => c.Cluster.Agent).Distinct().ToList();
            var connectedAgents = connectionsManager.GetAgentsConnections(affectedAgents);

            foreach (var connection in connectedAgents)
                await connection.StartMaintenanceTask(task, cancellationToken);
        
            var taskToUpdate = await appDbContext.MaintenanceTasks.FindAsync([id], cancellationToken);
            taskToUpdate!.StartDateTime = DateTime.Now;
            await appDbContext.SaveChangesAsync(cancellationToken);
        
            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            return View("Error", new ErrorViewModel(e.Message));
        }
    }
    
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await appDbContext.MaintenanceTasks
                .Include(c => c.Steps)
                .ThenInclude(c => c.Logs)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            appDbContext.MaintenanceTasks.Remove(item!);
            
            await appDbContext.SaveChangesAsync(cancellationToken);
            
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            return View("Error", new ErrorViewModel(ex.ToString()));
        }
    }
    
    public async Task<IActionResult> Log(Guid id, CancellationToken cancellationToken)
    {
        var task = await appDbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(c => c.Steps)
                .ThenInclude(c => c.Logs)
                .ThenInclude(c => c.InfoBase)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (task == null)
            return NotFound();

        return View(new MaintenanceTaskLogViewModel
        {
            TaskId = task.Id,
            Items = task.Steps
                .SelectMany(c => c.Logs)
                .GroupBy(c => c.InfoBase)
                .ToDictionary(c => c.Key, c => c.ToList())
        });
    }
    
    private async Task<MaintenanceTaskEditViewModel> PrepareViewModel(MaintenanceTaskEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.AvailableInfoBases = await UiHelper.SelectableItemsFrom(
            appDbContext.InfoBases,
            vm.InfoBases,
            mapper, 
            cancellationToken);

        return vm;
    }

    private async Task<MaintenanceStepViewModel> PrepareViewModel(MaintenanceStepViewModel vm, CancellationToken cancellationToken)
    {
        vm.Kinds = UiHelper.SelectListFromEnum<MaintenanceStepKind>(vm.Kind);
        
        var files = vm.Kind switch
        {
            MaintenanceStepKind.LoadConfiguration => appDbContext.V8Files.Where(c => c.IsConfiguration),
            MaintenanceStepKind.LoadExtension => appDbContext.V8Files.Where(c => c.IsExtension),
            MaintenanceStepKind.UpdateConfiguration => appDbContext.V8Files.Where(c => c.IsUpdate),
            MaintenanceStepKind.StartExternalDataProcessor => appDbContext.V8Files.Where(c => c.IsExternalDataProcessor),
            _ => null
        };
        
        if (files is not null)
            vm.Files = await UiHelper.SelectListFrom(files, c => c.ToString(), vm.FileId, cancellationToken);

        return vm;
    }
}