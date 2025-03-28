using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels;

public record SelectItemDialogViewModel(
    string Title,
    string ItemsModelProperty,
    List<SelectableItemViewModel> Items,
    List<SelectableItemViewModel> AvailableItems);