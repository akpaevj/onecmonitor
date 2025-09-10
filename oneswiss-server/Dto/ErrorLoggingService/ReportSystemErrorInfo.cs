using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportSystemErrorInfo
{
    [JsonPropertyName("clientStack")] public string ClientStack { get; set; } = string.Empty;

    [JsonPropertyName("clientStackHash")] public string ClientStackHash { get; set; } = string.Empty;
}