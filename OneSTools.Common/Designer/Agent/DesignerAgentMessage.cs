using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneSTools.Common.Designer.Agent;

public class DesignerAgentMessage
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("error-type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ErrorType { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonDocument Body { get; init; }
}