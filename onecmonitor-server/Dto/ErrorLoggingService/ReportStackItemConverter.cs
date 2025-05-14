using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class ReportStackItemConverter : JsonConverter<ReportStackItem>
{
    public override ReportStackItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        
        var item = new ReportStackItem
        {
            Module = reader.GetString() ?? string.Empty
        };

        reader.Read();
        
        item.Line = reader.GetInt32();
        reader.Read();
        
        item.Code = reader.GetString() ?? string.Empty;
        reader.Read();

        return item;
    }

    public override void Write(Utf8JsonWriter writer, ReportStackItem value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}