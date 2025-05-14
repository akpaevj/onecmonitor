using System.Text.Json.Serialization;

namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class GetInfoResponse
{
    [JsonPropertyName("needSendReport")]
    public bool? NeedSendReport { get; set; }
    [JsonPropertyName("userMessage")]
    public string? UserMessage { get; set; }
    [JsonPropertyName("dumpType")]
    public int? DumpType { get; set; }
}