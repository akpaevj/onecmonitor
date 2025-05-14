using System.Text.Json.Serialization;

namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class ReportServerInfo
{
    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("dbms")]
    public string Dbms { get; set; } = string.Empty;
}