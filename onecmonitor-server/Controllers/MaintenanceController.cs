using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.Maintenance;

namespace OnecMonitor.Server.Controllers;

public class MaintenanceController(AppDbContext appDbContext, IMapper mapper) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(new MaintenanceIndexViewModel());
    }
    
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        return View(await PrepareViewModel(new MaintenanceEditViewModel(), cancellationToken));
    }
    
    public IActionResult Step(MaintenanceStepViewModel vm)
        => vm.Kind switch
        {
            MaintenanceStepKind.LockConnections 
                or MaintenanceStepKind.LoadExtension
                or MaintenanceStepKind.UpdateConfiguration 
                or MaintenanceStepKind.LoadConfiguration
                or MaintenanceStepKind.StartExternalDataProcessor => PartialView($"Steps/{vm.Kind}", vm),
            MaintenanceStepKind.UnlockConnections or MaintenanceStepKind.CloseConnections
                or MaintenanceStepKind.UpdateDatabase => Ok(),
            _ => NotFound()
        };

    public IActionResult ValidateStep(MaintenanceStepViewModel vm)
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

        var view = PartialView($"Steps/{vm.Kind}", vm);
        view.StatusCode = ModelState.IsValid ? 200 : 400;
        
        return view;
    }
    
    private async Task<MaintenanceEditViewModel> PrepareViewModel(MaintenanceEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.AvailableInfoBases = await UiHelper.SelectableItemsFrom(
            appDbContext.InfoBases,
            vm.InfoBases,
            mapper, 
            cancellationToken);

        vm.MaintenanceStepKinds = UiHelper.SelectListFromEnum<MaintenanceStepKind>();

        return vm;
    }
}