using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public async Task<IActionResult> Index(bool isTemplate, bool showArchived, CancellationToken cancellationToken)
    {
        var items = await appDbContext.MaintenanceTasks
            .AsNoTracking()
            .Where(c => showArchived || !c.IsArchived)
            .Where(c => c.IsTemplate == isTemplate)
            .ToListAsync(cancellationToken);

        return View(new MaintenanceTasksIndexViewModel
        {
            ShowArchived = showArchived,
            IsTemplate = isTemplate,
            Items = mapper.Map<List<MaintenanceTaskListItemViewModel>>(items),
            Templates = await UiHelper.SelectableItemsFrom(
                appDbContext.MaintenanceTasks.Where(c => c.IsTemplate == true),
                mapper, 
                cancellationToken)
        });
    }
    
    public async Task<IActionResult> Edit(Guid id, bool isTemplate, bool fromTemplate, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return View(await PrepareViewModel(new MaintenanceTaskEditViewModel(), isTemplate, cancellationToken));
        
        var model = await appDbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(c => c.Steps)
            .Include(c => c.InfoBases)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (model == null)
            return NotFound();

        if (fromTemplate)
        {
            // Подменим все гуиды, что-бы избежать перезаписи шагов
            model.Id = Guid.Empty;
            model.Steps.ForEach(c =>
            {
                var currentId = c.Id;
                var newId = Guid.NewGuid();
                
                model.Steps.ForEach(i =>
                {
                    if (i.PreviousStepId == currentId)
                        i.PreviousStepId = newId;
                    
                    if (i.LeftStepId == currentId)
                        i.LeftStepId = newId;
                    
                    if (i.RightStepId == currentId)
                        i.RightStepId = newId;
                });
                
                c.Id = newId;
            });
        }
        
        var vm = mapper.Map<MaintenanceTaskEditViewModel>(model);
        var steps = mapper.Map<List<MaintenanceStepViewModel>>(model.Steps);
        vm.Steps = JsonSerializer.Serialize(steps, _jsonOptions);
        
        return View(await PrepareViewModel(vm, isTemplate, cancellationToken));
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(MaintenanceTaskEditViewModel vm, bool isTemplate, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;
        vm.IsTemplate = isTemplate;
        
        var viewModelSteps = JsonSerializer.Deserialize<List<MaintenanceStepViewModel>>(vm.Steps, _jsonOptions);
        if (viewModelSteps?.Count == 0)
            ModelState.AddModelError(nameof(MaintenanceTaskEditViewModel.Steps), "Не сконфигурированы шаги обслуживания");
        
        if (!isTemplate && vm.InfoBases.Count == 0)
            ModelState.AddModelError(nameof(MaintenanceTaskEditViewModel.InfoBases), "Не указаны обслуживаемые информационные базы");
        
        if (!isTemplate)
            viewModelSteps!.ForEach(c => ValidateStep(c, true));
        
        if (!ModelState.IsValid)
            return View("Edit", await PrepareViewModel(vm, isTemplate, cancellationToken));
        
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
            
        return RedirectToAction("Index", new { isTemplate });
    }

    [HttpPost]
    public async Task<IActionResult> EditStep(CancellationToken cancellationToken)
    {
        var vm = await HttpContext.Request.ReadFromJsonAsync<MaintenanceStepViewModel>(_jsonOptions, cancellationToken);
        return await UpdateStep(vm!, cancellationToken);
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateStep(MaintenanceStepViewModel vm, CancellationToken cancellationToken)
        => PartialView("EditStep", await PrepareViewModel(vm, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> ValidateStep(MaintenanceStepViewModel vm, bool isTemplate, CancellationToken cancellationToken)
    {
        if (!isTemplate)
            ValidateStep(vm, false);

        if (ModelState.IsValid)
            return Json(vm);
        
        var view = PartialView("EditStep", await PrepareViewModel(vm, cancellationToken));
        view.StatusCode = 400;
        
        return view;
    }
    
    private void ValidateStep(MaintenanceStepViewModel vm, bool taskValidation)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (vm.Kind)
        {
            case MaintenanceStepKind.LoadExtension:
            case MaintenanceStepKind.UpdateConfiguration:
            case MaintenanceStepKind.LoadConfiguration:
            case MaintenanceStepKind.StartExternalDataProcessor:
            case MaintenanceStepKind.ExecuteOneScript:
                if (vm.FileId == null || vm.FileId == Guid.Empty)
                    ModelState.AddModelError(taskValidation ? nameof(MaintenanceTask.Steps) : nameof(vm.FileId), "Не указан файл");
                break;
            case MaintenanceStepKind.LockConnections:
                if (string.IsNullOrEmpty(vm.AccessCode))
                    ModelState.AddModelError(taskValidation ? nameof(MaintenanceTask.Steps) : nameof(vm.AccessCode), "Не указан код доступа");
                if (string.IsNullOrEmpty(vm.Message))
                    ModelState.AddModelError(taskValidation ? nameof(MaintenanceTask.Steps) : nameof(vm.Message), "Не указан тест сообщения");
                break;
            case MaintenanceStepKind.DeleteExtension:
                if (string.IsNullOrEmpty(vm.ExtensionName))
                    ModelState.AddModelError(taskValidation ? nameof(MaintenanceTask.Steps) : nameof(vm.ExtensionName), "Не указано наименование расширения");
                break;
        }
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
                .Include(c => c.Steps).ThenInclude(c => c.File)
                .Include(c => c.InfoBases).ThenInclude(c => c.Credentials)
                .Include(c => c.InfoBases).ThenInclude(c => c.Cluster).ThenInclude(c => c.Agent)
                .Include(c => c.InfoBases).ThenInclude(c => c.Cluster).ThenInclude(c => c.Credentials)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (task == null)
                return NotFound();
            
            var connections = await connectionsManager.GetActiveAgentsConnections(cancellationToken);

            foreach (var connection in connections)
                await connection.StartMaintenanceTask(task, cancellationToken);
        
            var taskToUpdate = await appDbContext.MaintenanceTasks.FindAsync([id], cancellationToken);
            taskToUpdate!.StartDateTime = DateTime.Now;
            await appDbContext.SaveChangesAsync(cancellationToken);
        
            return RedirectToAction("Log", new { id = taskToUpdate.Id });
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
            
            return RedirectToAction("Index", new { item!.IsTemplate });
        }
        catch (Exception ex)
        {
            return View("Error", new ErrorViewModel(ex.ToString()));
        }
    }
    
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await appDbContext.MaintenanceTasks.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            item!.IsArchived = !item.IsArchived;;
            
            await appDbContext.SaveChangesAsync(cancellationToken);
            
            return RedirectToAction("Index", new { item.IsTemplate });
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
            .Include(c => c.InfoBases)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (task == null)
            return NotFound();

        return View(new MaintenanceTaskLogViewModel
        {
            TaskId = task.Id,
            InfoBases = task.InfoBases
        });
    }
    
    private async Task<MaintenanceTaskEditViewModel> PrepareViewModel(MaintenanceTaskEditViewModel vm, bool isTemplate, CancellationToken cancellationToken)
    {
        vm.AvailableInfoBases = await UiHelper.SelectableItemsFrom(
            appDbContext.InfoBases,
            vm.InfoBases,
            mapper, 
            cancellationToken);
        
        vm.IsTemplate = isTemplate;

        return vm;
    }

    private async Task<MaintenanceStepViewModel> PrepareViewModel(MaintenanceStepViewModel vm, CancellationToken cancellationToken)
    {
        vm.Kinds = UiHelper.SelectListFromEnum<MaintenanceStepKind>(vm.Kind);
        
        var files = vm.Kind switch
        {
            MaintenanceStepKind.LoadConfiguration => appDbContext.V8Files.Where(c => c.FileType == V8FileType.Cf),
            MaintenanceStepKind.LoadExtension => appDbContext.V8Files.Where(c => c.FileType == V8FileType.Cfe),
            MaintenanceStepKind.UpdateConfiguration => appDbContext.V8Files.Where(c => c.FileType == V8FileType.Cfu),
            MaintenanceStepKind.StartExternalDataProcessor => appDbContext.V8Files.Where(c => c.FileType == V8FileType.Epf),
            MaintenanceStepKind.ExecuteOneScript => appDbContext.V8Files.Where(c => c.FileType == V8FileType.Ospx),
            _ => null
        };
        
        if (files is not null)
            vm.Files = await UiHelper.SelectListFrom(files.Where(c => !c.IsArchived), c => c.ToString(), vm.FileId, cancellationToken);

        return vm;
    }
}