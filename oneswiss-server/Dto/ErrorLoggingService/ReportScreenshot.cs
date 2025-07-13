using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportScreenshot
{
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;
}