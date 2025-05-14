using System.Text.Json.Serialization;

namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class ReportScreenshot
{
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;
}