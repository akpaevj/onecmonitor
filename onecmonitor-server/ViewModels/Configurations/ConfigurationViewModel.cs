using Microsoft.AspNetCore.Mvc;

namespace OnecMonitor.Server.ViewModels.Configurations;

public class ConfigurationViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public string Version { get; set; }
    public bool IsExtension { get; set; }
    public IFormFile File { get; set; }
}