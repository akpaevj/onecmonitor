using MessagePack;
using OneScript.Contexts;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Common.DTO;

[ContextClass("Кластер", "Cluster")]
[MessagePackObject]
public class ClusterDto : AutoContext<ClusterDto>
{
    [ContextProperty("Идентификатор", "Id")]
    [Key(0)]
    public string Id { get; set; } = string.Empty;

    [ContextProperty("ВнутреннийИдентификатор", "InternalId")]
    [Key(1)]
    public string ClusterInternalId { get; set; } = string.Empty;

    [ContextProperty("Хост", "Host")]
    [Key(2)]
    public string Host { get; set; } = string.Empty;

    [ContextProperty("Порт", "Port")]
    [Key(3)]
    public int Port { get; set; }

    [ContextProperty("ПортRagent", "PortRagent")]
    [Key(4)]
    public int RagentPort { get; set; }

    [ContextProperty("УчетныеДанные", "Credentials")]
    [Key(5)]
    public CredentialsDto? Credentials { get; set; }
}