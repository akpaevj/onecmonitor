using System.Text.Json;
using System.Text.Json.Serialization;
using OneSwiss.Common.Helpers;

namespace OneSwiss.Common.Converters.Json
{
    public class DateTimeToUtcDateTime64 : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.ValueSpan;

            return DateTime.SpecifyKind(DateTime.Parse(s.ToString()), DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            //writer .WriteString(value);
        }
    }
}
