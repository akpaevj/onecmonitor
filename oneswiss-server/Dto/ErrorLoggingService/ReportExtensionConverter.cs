using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportExtensionConverter : JsonConverter<ReportExtension>
{
    public override ReportExtension? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();

        var item = new ReportExtension
        {
            Name = reader.GetString() ?? string.Empty
        };

        reader.Read();

        item.Hash = reader.GetString() ?? string.Empty;
        reader.Read();

        return item;
    }

    public override void Write(Utf8JsonWriter writer, ReportExtension value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}