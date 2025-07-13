using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class GetInfoRequest
{
    [JsonPropertyName("appName")]
    public string? AppName { get; set; }
    [JsonPropertyName("appStackHash")]
    public string? AppStackHash { get; set; }
    [JsonPropertyName("appVersion")]
    public string? AppVersion { get; set; }
    [JsonPropertyName("clientStackHash")]
    public string? ClientStackHash { get; set; }
    [JsonPropertyName("configHash")]
    public string? ConfigHash { get; set; }
    [JsonPropertyName("configName")]
    public string? ConfigName { get; set; }
    [JsonPropertyName("configurationInterfaceLanguageCode")]
    public string? ConfigurationInterfaceLanguageCode { get; set; }
    [JsonPropertyName("configVersion")]
    public string? ConfigVersion { get; set; }
    [JsonPropertyName("clientID")]
    public string? ClientId { get; set; }
    [JsonPropertyName("reportID")]
    public string? ReportId { get; set; }
    [JsonPropertyName("errorCategories")]
    public string[]? ErrorCategories { get; set; }
    [JsonPropertyName("platformInterfaceLanguageCode")]
    public string? PlatformInterfaceLanguageCode { get; set; }
    [JsonPropertyName("platformType")]
    public string? PlatformType { get; set; }
    [JsonPropertyName("serverStackHash")]
    public string? ServerStackHash { get; set; }
    [JsonPropertyName("systemCrash")]
    public bool? SystemCrash { get; set; }
}