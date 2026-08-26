using MessagePack;
using MessagePack.Formatters;
using OneScript.Contexts;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.V8.Edt;

// См. комментарий в Platform/Services/CrServer.cs - AutoContext<T> примешивает публичные
// виртуальные свойства движка OneScript, которые нельзя ни атрибутировать (объявлены во внешней
// библиотеке), ни проигнорировать через override, поэтому автоматический контракт MessagePack
// здесь неприменим.
[MessagePackFormatter(typeof(EdtInstallationFormatter))]
[MessagePackObject]
[ContextClass("ИнсталляцияEDT", "EdtInstallation")]
public class EdtInstallation : AutoContext<EdtInstallation>
{
    [Key(0)]
    [ContextProperty("Версия", "Version", CanWrite = false)]
    public string Version { get; set; }

    [Key(1)]
    [ContextProperty("Путь", "Path", CanWrite = false)]
    public string Path { get; set; }

    [Key(2)]
    [ContextProperty("СуществуетEdtCli", "ExistsEdtCli", CanWrite = false)]
    public bool HasEdtCli { get; set; }

    [Key(3)]
    [ContextProperty("ПутьEdtCli", "PathEdtCli", CanWrite = false)]
    public string EdtCliPath { get; set; }

    [Key(4)]
    [ContextProperty("ИзСтартера", "FromStarter", CanWrite = false)]
    public bool FromStarter { get; set; }
}

public class EdtInstallationFormatter : IMessagePackFormatter<EdtInstallation?>
{
    public void Serialize(ref MessagePackWriter writer, EdtInstallation? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(5);
        writer.Write(value.Version);
        writer.Write(value.Path);
        writer.Write(value.HasEdtCli);
        writer.Write(value.EdtCliPath);
        writer.Write(value.FromStarter);
    }

    public EdtInstallation? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return null;

        var count = reader.ReadArrayHeader();
        var result = new EdtInstallation
        {
            Version = reader.ReadString() ?? string.Empty,
            Path = reader.ReadString() ?? string.Empty,
            HasEdtCli = reader.ReadBoolean(),
            EdtCliPath = reader.ReadString() ?? string.Empty,
            FromStarter = reader.ReadBoolean()
        };

        for (var i = 5; i < count; i++)
            reader.Skip();

        return result;
    }
}