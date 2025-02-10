using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels;

public record SelectItemDialogViewModel(
    string ItemsModelProperty,
    List<SelectableItemViewModel> Items,
    List<SelectableItemViewModel> AvailableItems);