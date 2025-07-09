using MessagePack;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class ConfigurationRepositoryMeta
{
    [Key(0)]
    public string Name { get; set; } = string.Empty;
    [Key(1)]
    public Guid InternalId { get; set; }
    [Key(2)] 
    public int CrServerPort { get; set; }
}