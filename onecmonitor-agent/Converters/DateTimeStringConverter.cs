using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OnecMonitor.Agent.Converters
{
    public class DateTimeStringConverter : ValueConverter<DateTime, string>
    {
        public DateTimeStringConverter() : base(g => g.ToUniversalTime().ToString(), s => DateTime.SpecifyKind(DateTime.Parse(s), DateTimeKind.Utc)) { }
    }
}
