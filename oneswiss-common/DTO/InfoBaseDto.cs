using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO;

[ContextClass("ИнформационнаяБаза", "InfoBase")]
[MessagePackObject]
public class InfoBaseDto
{
    [ContextProperty("Идентификатор", "Id")]
    [Key(0)]
    public Guid Id { get; set; }
    [ContextProperty("ВнутреннийИдентификатор", "InternalId")]
    [Key(1)] 
    public string InfoBaseInternalId { get; set; } = string.Empty;
    [ContextProperty("ИмяИнформационнойБазы", "InfoBaseName")]
    [Key(2)]
    public string InfoBaseName { get; set; } = string.Empty;
    [ContextProperty("УчетныеДанные", "Credentials")]
    [Key(3)] 
    public CredentialsDto? Credentials { get; set; }
    [ContextProperty("Кластер", "Cluster")]
    [Key(4)] 
    public required ClusterDto Cluster { get; set; }
}