namespace OneSwiss.Server.Dto;

public class V8InfoBaseDto : V8MetadataDto
{
    public string Version { get; set; } = string.Empty;
    public string SslVersion { get; set; } = string.Empty;
    public bool UseUserGroups { get; set; } = false;
}