using System.Text.Json.Serialization;

namespace OneSTools.Common.Designer.Agent.Models;

public class ExtensionPropertiesMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("body")]
    public ExtensionInfo Body { get; set; }
}