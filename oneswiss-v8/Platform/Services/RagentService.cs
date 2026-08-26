using System.ComponentModel;
using MessagePack;
using MessagePack.Formatters;
using OneScript.Contexts;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.V8.Platform.Services;

// См. комментарий в CrServer.cs - AutoContext<T> примешивает публичные виртуальные свойства
// движка OneScript, которые нельзя ни атрибутировать (объявлены во внешней библиотеке), ни
// проигнорировать через override, поэтому автоматический контракт MessagePack здесь неприменим.
[MessagePackFormatter(typeof(RagentServiceFormatter))]
[DisplayName("Служба агента сервера 1С")]
[MessagePackObject]
[ContextClass("СервисRagent", "RagentService")]
public class RagentService : AutoContext<RagentService>, IV8Service
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
    [DisplayName("Порт")]
    [ContextProperty("Порт", "Port", CanWrite = false)]
    public int Port { get; set; }

    [Key(3)]
    [DisplayName("Платформа")]
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8Platform Platform { get; set; } = null!;

    [Key(4)]
    [DisplayName("Каталог агента сервера")]
    [ContextProperty("РабочийКаталог", "WorkingDirectory", CanWrite = false)]
    public string WorkingDirectory { get; set; } = null!;

    [Key(5)]
    [DisplayName("Тип отладки")]
    [ContextProperty("ТипОтладки", "DebugType", CanWrite = false)]
    public RagentDebugType DebugType { get; set; } = RagentDebugType.None;
}

public class RagentServiceFormatter : IMessagePackFormatter<RagentService?>
{
    public void Serialize(ref MessagePackWriter writer, RagentService? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(6);
        writer.Write(value.Name);
        writer.Write(value.IsActive);
        writer.Write(value.Port);
        options.Resolver.GetFormatterWithVerify<V8Platform>().Serialize(ref writer, value.Platform, options);
        writer.Write(value.WorkingDirectory);
        options.Resolver.GetFormatterWithVerify<RagentDebugType>().Serialize(ref writer, value.DebugType, options);
    }

    public RagentService? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return null;

        var count = reader.ReadArrayHeader();
        var result = new RagentService
        {
            Name = reader.ReadString() ?? string.Empty,
            IsActive = reader.ReadBoolean(),
            Port = reader.ReadInt32(),
            Platform = options.Resolver.GetFormatterWithVerify<V8Platform>().Deserialize(ref reader, options),
            WorkingDirectory = reader.ReadString() ?? string.Empty,
            DebugType = options.Resolver.GetFormatterWithVerify<RagentDebugType>().Deserialize(ref reader, options)
        };

        for (var i = 6; i < count; i++)
            reader.Skip();

        return result;
    }
}