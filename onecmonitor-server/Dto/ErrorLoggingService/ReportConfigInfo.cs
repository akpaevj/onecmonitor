using System.Text.Json.Serialization;

namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class ReportConfigInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    [JsonPropertyName("compatibilityMode")]
    public string CompatibilityMode { get; set; } = string.Empty;
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
    [JsonPropertyName("changeEnabled")]
    public bool ChangeEnabled { get; set; }
    [JsonPropertyName("extensions")] 
    public List<ReportExtension> Extensions { get; set; } = [];
}