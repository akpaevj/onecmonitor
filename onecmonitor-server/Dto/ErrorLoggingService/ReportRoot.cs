using System.Text.Json.Serialization;

namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class ReportRoot
{
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("clientInfo")] 
    public ReportClientInfo ClientInfo { get; set; } = null!;
    [JsonPropertyName("sessionInfo")]
    public ReportSessionInfo SessionInfo { get; set; } = null!;
    [JsonPropertyName("infoBaseInfo")]
    public ReportInfoBaseInfo InfoBaseInfo { get; set; } = null!;
    [JsonPropertyName("serverInfo")]
    public ReportServerInfo ServerInfo { get; set; } = null!;
    [JsonPropertyName("configInfo")]
    public ReportConfigInfo ConfigInfo { get; set; } = null!;
    [JsonPropertyName("errorInfo")]
    public ReportErrorInfo ErrorInfo { get; set; } = null!;
    [JsonPropertyName("screenshot")]
    public ReportScreenshot? Screenshot { get; set; }
    [JsonPropertyName("additionalInfo")]
    public string? AdditionalInfo { get; set; } = string.Empty;
}