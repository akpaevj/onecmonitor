using MessagePack;
using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;

namespace OneSwiss.Common.DTO;

[ContextClass("ХранилищеКонфигурации", "ConfigurationRepository")]
[MessagePackObject]
public class ConfigurationRepositoryDto
{
    [ContextProperty("Идентификатор", "Id", Converter = typeof(GuidContextConverter))]
    [Key(0)]
    public Guid Id { get; set; }

    [ContextProperty("ВнутреннийИдентификатор", "InternalId", Converter = typeof(GuidContextConverter))]
    [Key(1)]
    public Guid InternalId { get; set; }

    [ContextProperty("УчетныеДанные", "Credentials")]
    [Key(2)]
    public CredentialsDto? Credentials { get; set; }

    [ContextProperty("ИмяХранилищаКонфигураций", "ConfigurationRepositoryName")]
    [Key(3)]
    public string Name { get; set; }

    [ContextProperty("ХостСервераХранилищКонфигураций", "ConfigurationRepositoryServerHost")]
    [Key(4)]
    public string Host { get; set; }

    [ContextProperty("ПортСервераХранилищКонфигураций", "ConfigurationRepositoryServerPort")]
    [Key(5)]
    public int Port { get; set; }

    [Key(6)]
    [ContextProperty("Агент", "Agent", CanWrite = false)]
    public AgentDto Agent { get; set; } = null!;

    [Key(7)]
    [ContextProperty("Пользователи", "Users", CanWrite = false,
        Converter = typeof(ListContextConverter<ConfigRepositoryUserDto>))]
    public List<ConfigRepositoryUserDto> Users { get; set; } = [];
}