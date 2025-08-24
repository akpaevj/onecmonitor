using MessagePack;
using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;
using OneSwiss.V8.Platform;

namespace OneSwiss.Common.DTO;

[ContextClass("ХранилищеКонфигурации", "ConfigurationRepository")]
[MessagePackObject]
public class ConfigurationRepositoryDto
{
    [ContextProperty("Идентификатор", "Id", Converter = typeof(GuidContextConverter))]
    [Key(0)]
    public Guid Id { get; set; }
    [ContextProperty("УчетныеДанные", "Credentials")]
    [Key(1)] 
    public CredentialsDto? Credentials { get; set; }
    [ContextProperty("ИмяХранилищаКонфигураций", "ConfigurationRepositoryName")]
    [Key(2)]
    public string Name { get; set; }
    [ContextProperty("ХостСервераХранилищКонфигураций", "ConfigurationRepositoryServerHost")]
    [Key(3)]
    public string Host { get; set; }
    [ContextProperty("ПортСервераХранилищКонфигураций", "ConfigurationRepositoryServerPort")]
    [Key(4)]
    public int Port { get; set; }
    [Key(5)]
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8Platform Platform { get; set; } = null!;
}