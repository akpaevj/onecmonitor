using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportClientInfo
{
    [JsonPropertyName("platformType")] public string PlatformType { get; set; } = string.Empty;

    [JsonPropertyName("appVersion")] public string AppVersion { get; set; } = string.Empty;

    [JsonPropertyName("appName")] public string AppName { get; set; } = string.Empty;

    [JsonPropertyName("systemInfo")] public ReportSystemInfo SystemInfo { get; set; } = null!;
}