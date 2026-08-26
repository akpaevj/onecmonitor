using System.ComponentModel;
using MessagePack;
using MessagePack.Formatters;
using OneScript.Contexts;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.V8.Platform.Services;

// См. комментарий в CrServer.cs - AutoContext<T> примешивает публичные виртуальные свойства
// движка OneScript, которые нельзя ни атрибутировать (объявлены во внешней библиотеке), ни
// проигнорировать через override, поэтому автоматический контракт MessagePack здесь неприменим.
[MessagePackFormatter(typeof(RasServiceFormatter))]
[DisplayName("Служба сервера удаленного администрирования 1С")]
[MessagePackObject]
[ContextClass("СервисRas", "RasService")]
public class RasService : AutoContext<RasService>, IV8Service
{
    [DisplayName("Имя")]
    [ContextProperty("Имя", "Name", CanWrite = false)]
    [Key(0)]
    public string Name { get; set; } = string.Empty;

    [DisplayName("Запущена")]
    [ContextProperty("Запущена", "IsActive", CanWrite = false)]
    [Key(1)]
    public bool IsActive { get; set; }

    [Key(2)]
    [DisplayName("Хост агента кластера")]
    [ContextProperty("ХостRagent", "RagentHost", CanWrite = false)]
    public string RagentHost { get; set; } = string.Empty;

    [Key(3)]
    [DisplayName("Порт агента кластера")]
    [ContextProperty("ПортRagent", "RagentPort", CanWrite = false)]
    public int RagentPort { get; set; }

    [Key(4)]
    [DisplayName("Порт")]
    [ContextProperty("Порт", "Port", CanWrite = false)]
    public int Port { get; set; }

    [Key(5)]
    [DisplayName("Платформа")]
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8Platform Platform { get; set; } = null!;
}

public class RasServiceFormatter : IMessagePackFormatter<RasService?>
{
    public void Serialize(ref MessagePackWriter writer, RasService? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(6);
        writer.Write(value.Name);
        writer.Write(value.IsActive);
        writer.Write(value.RagentHost);
        writer.Write(value.RagentPort);
        writer.Write(value.Port);
        options.Resolver.GetFormatterWithVerify<V8Platform>().Serialize(ref writer, value.Platform, options);
    }

    public RasService? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return null;

        var count = reader.ReadArrayHeader();
        var result = new RasService
        {
            Name = reader.ReadString() ?? string.Empty,
            IsActive = reader.ReadBoolean(),
            RagentHost = reader.ReadString() ?? string.Empty,
            RagentPort = reader.ReadInt32(),
            Port = reader.ReadInt32(),
            Platform = options.Resolver.GetFormatterWithVerify<V8Platform>().Deserialize(ref reader, options)
        };

        for (var i = 6; i < count; i++)
            reader.Skip();

        return result;
    }
}