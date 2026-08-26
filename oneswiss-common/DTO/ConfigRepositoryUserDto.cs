using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class ConfigRepositoryUserDto
{
    [Key(1)] public Guid Id { get; set; }

    [Key(2)] public string Name { get; set; }
}