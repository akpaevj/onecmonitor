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
    [ContextProperty("УчетныеДанные", "Credentials")]
    [Key(1)] 
    public CredentialsDto? Credentials { get; set; }
    [ContextProperty("ИмяХранилищаКонфигураций", "ConfigurationRepositoryName")]
    [Key(2)]
    public string Name { get; set; }
    [ContextProperty("ПортСервераХранилищКонфигураций", "ConfigurationRepositoryServerPort")]
    [Key(3)]
    public int Port { get; set; }
}