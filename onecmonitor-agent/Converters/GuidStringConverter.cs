using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OnecMonitor.Agent.Converters
{
    public class GuidStringConverter : ValueConverter<Guid, string>
    {
        public GuidStringConverter() : base(g => g.ToString(), s => Guid.Parse(s)) { }
    }
}
