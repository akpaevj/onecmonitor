using MessagePack;
using OneScript.Contexts;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Common.DTO;

[ContextClass("ИнформационнаяБаза", "InfoBase")]
[MessagePackObject]
public class InfoBaseDto : AutoContext<InfoBaseDto>
{
    [Key(0)]
    public Guid Id { get; set; }

    // OneScript не умеет маршалить System.Guid как возвращаемое значение - отдаём строкой.
    [IgnoreMember]
    [ContextProperty("Идентификатор", "Id", CanWrite = false)]
    public string IdAsString => Id.ToString();

    [Key(1)]
    public string InfoBaseInternalId { get; set; } = string.Empty;

    [Key(2)]
    [ContextProperty("ИмяИнформационнойБазы", "InfoBaseName", CanWrite = false)]
    public string InfoBaseName { get; set; } = string.Empty;

    [Key(3)]
    [ContextProperty("УчетныеДанные", "Credentials", CanWrite = false)]
    public CredentialsDto? Credentials { get; set; }

    [Key(4)]
    [ContextProperty("Кластер", "Cluster", CanWrite = false)]
    public required ClusterDto Cluster { get; set; }
}