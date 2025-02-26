using Microsoft.AspNetCore.Mvc;

namespace OnecMonitor.Server.ViewModels.V8Files;

public class V8FileEditViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
}