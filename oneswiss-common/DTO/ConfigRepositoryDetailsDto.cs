using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class ConfigRepositoryDetailsDto
{
    [Key(1)] 
    public Guid Id { get; set; } = Guid.Empty;
    [Key(2)]
    public List<ConfigRepositoryUserDto> Users { get; set; } = [];
}