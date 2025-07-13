using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportSystemInfo
{
    [JsonPropertyName("osVersion")]
    public string OsVersion { get; set; } = string.Empty;
    [JsonPropertyName("fullRAM")]
    public long FullRam { get; set; }
    [JsonPropertyName("freeRAM")]
    public long FreeRam { get; set; }
    [JsonPropertyName("processor")]
    public string Processor { get; set; } = string.Empty;
    [JsonPropertyName("clientID")]
    public string ClientId { get; set; } = string.Empty;
}