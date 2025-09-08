using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportApplicationErrorInfo
{
    [JsonPropertyName("errors")] public List<ReportError> Errors { get; set; } = [];

    [JsonPropertyName("stack")] public List<ReportStackItem> Stack { get; set; } = [];

    [JsonPropertyName("stackHash")] public string StackHash { get; set; } = string.Empty;
}