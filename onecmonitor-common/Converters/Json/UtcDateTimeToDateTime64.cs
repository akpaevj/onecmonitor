using Newtonsoft.Json;
using OnecMonitor.Common.Helpers;

namespace OnecMonitor.Common.Converters.Json
{
    public class DateTimeToUtcDateTime64 : JsonConverter<DateTime>
    {
        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var s = (string)reader.Value!;

            return DateTime.SpecifyKind(DateTime.Parse(s), DateTimeKind.Utc);
        }

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            writer.WriteValue(ClickHouseHelper.SerializeDateTime(value));
        }
    }
}
