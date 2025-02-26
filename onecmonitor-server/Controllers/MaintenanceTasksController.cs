using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;
using OnecMonitor.Server.ViewModels;
using OnecMonitor.Server.ViewModels.MaintenanceTasks;

namespace OnecMonitor.Server.Controllers;

public class MaintenanceTasksController(AppDbContext appDbContext, IMapper mapper) : Controller
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(new MaintenanceTasksIndexViewModel
        {
            Items = await appDbContext.MaintenanceTasks
                .AsNoTracking()
                .ProjectTo<MaintenanceTaskListItemViewModel>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken)
        });
    
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return View(await PrepareViewModel(new MaintenanceTaskEditViewModel(), cancellationToken));
        
        var model = await appDbContext.MaintenanceTasks
            .AsNoTracking()
            .Include(c => c.RootNode)
            .ThenInclude(c => c.Step)
            .AsNoTracking()
            .Include(c => c.InfoBases)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (model == null)
            return NotFound();
        
        await LoadNodesRecursively(model.RootNode, cancellationToken);
        
        var vm = mapper.Map<MaintenanceTaskEditViewModel>(model);
        var rootNodeVm = mapper.Map<MaintenanceStepNodeViewModel>(model.RootNode);
        vm.SerializedStepNode = JsonSerializer.Serialize(rootNodeVm, _jsonOptions);
        
        return View(await PrepareViewModel(vm, cancellationToken));
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(MaintenanceTaskEditViewModel vm, CancellationToken cancellationToken)
    {
        var isNew = vm.Id == Guid.Empty;
        
        if (!ModelState.IsValid)
            return View("Edit", await PrepareViewModel(vm, cancellationToken));
        
        var model = isNew ? new MaintenanceTask
        {
            Id = Guid.NewGuid()
        } : await appDbContext.MaintenanceTasks
            .Include(c => c.RootNode)
            .FirstOrDefaultAsync(i => i.Id == vm.Id, cancellationToken);
        
        if (model == null)
            return NotFound();
        
        mapper.Map(vm, model);
        model.RootNode = JsonSerializer.Deserialize<MaintenanceStepNode>(vm.SerializedStepNode, _jsonOptions)!;
        model.RootNodeId = model.RootNode.Id;
        
        await UiHelper.UpdateModelItems(appDbContext.InfoBases, vm.InfoBases, model.InfoBases, cancellationToken);
        
        if (isNew)
            appDbContext.MaintenanceTasks.Add(model);
        else
            appDbContext.MaintenanceTasks.Update(model);
        
        await appDbContext.SaveChangesAsync(cancellationToken);
            
        return RedirectToAction("Index");
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

    private async Task LoadNodesRecursively(MaintenanceStepNode node, CancellationToken cancellationToken)
    {
        if (node.LeftNodeId != null && node.LeftNodeId != Guid.Empty)
            node.LeftNode = await LoadNodeRecursively(node.LeftNodeId, cancellationToken);
        
        if (node.RightNodeId != null && node.RightNodeId != Guid.Empty)
            node.RightNode = await LoadNodeRecursively(node.RightNodeId, cancellationToken);
    }

    private async Task<MaintenanceStepNode> LoadNodeRecursively(Guid? id, CancellationToken cancellationToken)
    {
        var node = (await appDbContext.MaintenanceStepNodes
            .AsNoTracking()
            .Include(c => c.Step)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken))!;
            
        await LoadNodesRecursively(node, cancellationToken);

        return node;
    }
}