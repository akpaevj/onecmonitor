using MessagePack;
using OneSwiss.V8.Platform;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class ConfigRepositoryDetailsDto
{
    [Key(0)] public Guid Id { get; set; } = Guid.Empty;

    [Key(1)] public V8Platform Platform { get; set; } = null!;

    [Key(2)] public List<ConfigRepositoryUserDto> Users { get; set; } = [];
}