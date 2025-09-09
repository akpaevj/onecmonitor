using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportInfoBaseInfo
{
    [JsonPropertyName("localeCode")] public string LocaleCode { get; set; } = string.Empty;
}