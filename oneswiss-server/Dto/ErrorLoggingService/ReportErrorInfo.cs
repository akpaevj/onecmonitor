using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportErrorInfo
{
    [JsonPropertyName("systemErrorInfo")] 
    public ReportSystemErrorInfo SystemErrorInfo { get; set; } = null!;
    [JsonPropertyName("applicationErrorInfo")]
    public ReportApplicationErrorInfo ApplicationErrorInfo { get; set; } = null!;
}