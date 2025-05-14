using System.Text.Json.Serialization;

namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class ReportSessionInfo
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;
    [JsonPropertyName("dataSeparation")]
    public string DataSeparation { get; set; } = string.Empty;
    [JsonPropertyName("platformInterfaceLanguageCode")]
    public string PlatformInterfaceLanguageCode { get; set; } = string.Empty;
    [JsonPropertyName("configurationInterfaceLanguageCode")]
    public string ConfigurationInterfaceLanguageCode { get; set; } = string.Empty;
    [JsonPropertyName("localeCode")]
    public string LocaleCode { get; set; } = string.Empty;
}