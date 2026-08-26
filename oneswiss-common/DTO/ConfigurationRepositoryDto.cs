using MessagePack;
using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class ConfigurationRepositoryDto
{
    [Key(0)]
    public Guid Id { get; set; }

    [Key(1)]
    public Guid InternalId { get; set; }

    [Key(2)]
    public CredentialsDto? Credentials { get; set; }

    [Key(3)]
    public string Name { get; set; }

    [Key(4)]
    public string Host { get; set; }

    [Key(5)]
    public int Port { get; set; }

    [Key(6)]
    public AgentDto Agent { get; set; } = null!;

    [Key(7)]
    public List<ConfigRepositoryUserDto> Users { get; set; } = [];
}