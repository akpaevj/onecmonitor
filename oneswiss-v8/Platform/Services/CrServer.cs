using System.ComponentModel;
using MessagePack;
using MessagePack.Formatters;
using OneScript.Contexts;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.V8.Platform.Services;

// AutoContext<T> примешивает публичные виртуальные свойства движка OneScript (SystemType,
// IsIndexed, DynamicMethodSignatures из ContextIValueImpl/PropertyNameIndexAccessor), которые
// MessagePack не может ни сериализовать, ни просто проигнорировать: он требует Key/IgnoreMember
// непосредственно на объявляющем типе (ScriptEngine.Machine.Contexts), а не на переопределении в
// CrServer - см. https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/master/doc/analyzers/MsgPack004.md.
// Поэтому автоматический DynamicObjectResolver для этого типа неприменим - вместо него используется
// явный форматтер, читающий/пишущий только собственные [Key] свойства CrServer.
[MessagePackFormatter(typeof(CrServerFormatter))]
[ContextClass("СлужбаCrServer", "CrServerService")]
[DisplayName("Служба хранилища конфигураций 1С")]
[MessagePackObject]
public class CrServer : AutoContext<CrServer>, IV8Service
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
    [DisplayName("Каталог")]
    [ContextProperty("Каталог", "Directory", CanWrite = false)]
    public string Directory { get; set; } = null!;

    [Key(4)]
    [DisplayName("Платформа")]
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8Platform Platform { get; set; } = null!;

    [Key(5)]
    [DisplayName("Список хранилищ")]
    public List<string> Repositories { get; set; } = [];
}

public class CrServerFormatter : IMessagePackFormatter<CrServer?>
{
    public void Serialize(ref MessagePackWriter writer, CrServer? value, MessagePackSerializerOptions options)
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
        writer.Write(value.Directory);
        options.Resolver.GetFormatterWithVerify<V8Platform>().Serialize(ref writer, value.Platform, options);
        options.Resolver.GetFormatterWithVerify<List<string>>().Serialize(ref writer, value.Repositories, options);
    }

    public CrServer? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return null;

        var count = reader.ReadArrayHeader();
        var result = new CrServer
        {
            Name = reader.ReadString() ?? string.Empty,
            IsActive = reader.ReadBoolean(),
            Port = reader.ReadInt32(),
            Directory = reader.ReadString() ?? string.Empty,
            Platform = options.Resolver.GetFormatterWithVerify<V8Platform>().Deserialize(ref reader, options),
            Repositories = options.Resolver.GetFormatterWithVerify<List<string>>().Deserialize(ref reader, options)
        };

        for (var i = 6; i < count; i++)
            reader.Skip();

        return result;
    }
}