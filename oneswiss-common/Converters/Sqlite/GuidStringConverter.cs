using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OneSwiss.Common.Converters.Sqlite
{
    public class GuidStringConverter : ValueConverter<Guid, string>
    {
        public GuidStringConverter() : base(g => g.ToString(), s => Guid.Parse(s)) { }
    }
}
