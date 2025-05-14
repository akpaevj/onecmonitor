using System.Text.Json.Serialization;

namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class ReportInfoBaseInfo
{
    [JsonPropertyName("localeCode")]
    public string LocaleCode { get; set; } = string.Empty;
}