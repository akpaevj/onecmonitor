using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OnecMonitor.Server.Converters.Sqlite
{
    public class DateTimeStringConverter : ValueConverter<DateTime, string>
    {
        public DateTimeStringConverter() : base(
            g => g.Kind == DateTimeKind.Utc ? g.ToString() : g.ToUniversalTime().ToString(), 
            s => DateTime.SpecifyKind(DateTime.Parse(s), DateTimeKind.Utc)) { }
    }
}
