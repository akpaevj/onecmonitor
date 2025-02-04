using System.Text.Json.Serialization;

namespace OneSTools.Common.Designer.Agent.Models;

public class ExtensionInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    [JsonPropertyName("active")]
    public bool IsActive { get; set; } = false;
    [JsonPropertyName("safe-mode")]
    public bool SafeMode { get; set; } = false;
    [JsonPropertyName("unsafe-action-protection")]
    public bool UnsafeActionProtection { get; set; } = false;
    [JsonPropertyName("hash-sum")]
    public string HashSum { get; set; } = string.Empty;
}