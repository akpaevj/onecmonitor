using MessagePack;
using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class InfoBaseDto
{
    [Key(0)]
    public Guid Id { get; set; }

    [Key(1)]
    public string InfoBaseInternalId { get; set; } = string.Empty;

    [Key(2)]
    public string InfoBaseName { get; set; } = string.Empty;

    [Key(3)]
    public CredentialsDto? Credentials { get; set; }

    [Key(4)]
    public required ClusterDto Cluster { get; set; }
}