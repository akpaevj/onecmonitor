namespace OnecMonitor.Server.ViewModels.V8Files;

public class V8FilesIndexViewModel
{
    public bool ShowArchived { get; set; } = false; 
    public List<V8FileListItemViewModel> Items { get; init; } = [];
}