using System.Text.Json;
using System.Text.Json.Serialization;
using OneSwiss.Server.Extensions;

namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportErrorConverter : JsonConverter<ReportError>
{
    public override ReportError? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();

        var item = new ReportError
        {
            Text = reader.GetString() ?? string.Empty
        };
        
        reader.Read();
        
        // старт массива категорий
        reader.Read();

        var categories = new List<string>();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            categories.Add(reader.GetString() ?? string.Empty);
            reader.Read();
        }
        
        item.Categories = categories.ToArray();
        
        // конец массива категорий
        reader.Read();
        
        reader.SkipTo(JsonTokenType.EndArray);
        
        return item;
    }

    public override void Write(Utf8JsonWriter writer, ReportError value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}