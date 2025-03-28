namespace OnecMonitor.Server.ViewModels.V8Files;

public class V8FileEditViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public IFormFile File { get; set; } = null!;
}