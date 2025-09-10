using System.Text;
using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportRoot
{
    [JsonPropertyName("time")] public DateTime Time { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("clientInfo")] public ReportClientInfo ClientInfo { get; set; } = null!;

    [JsonPropertyName("sessionInfo")] public ReportSessionInfo SessionInfo { get; set; } = null!;

    [JsonPropertyName("infoBaseInfo")] public ReportInfoBaseInfo InfoBaseInfo { get; set; } = null!;

    [JsonPropertyName("serverInfo")] public ReportServerInfo ServerInfo { get; set; } = null!;

    [JsonPropertyName("configInfo")] public ReportConfigInfo ConfigInfo { get; set; } = null!;

    [JsonPropertyName("errorInfo")] public ReportErrorInfo ErrorInfo { get; set; } = null!;

    [JsonPropertyName("screenshot")] public ReportScreenshot? Screenshot { get; set; }

    [JsonPropertyName("additionalInfo")] public string? AdditionalInfo { get; set; } = string.Empty;

    public string GetStack()
    {
        var stackBuilder = new StringBuilder();
        var prefix = "";

        foreach (var item in ErrorInfo.ApplicationErrorInfo.Stack)
        {
            stackBuilder.AppendLine($"{prefix}{item.Module} : {item.Line} : {item.Code.Trim()}");
            prefix += "\t";
        }

        return stackBuilder.ToString();
    }
}