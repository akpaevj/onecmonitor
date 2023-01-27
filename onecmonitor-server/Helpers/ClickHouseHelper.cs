using Newtonsoft.Json;
using OnecMonitor.Server.Converters.Json;
using System.Text;

namespace OnecMonitor.Server.Helpers
{
    public class ClickHouseHelper
    {
        public static string SerializeDateTime(DateTime dateTime)
            => dateTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff");

        public static string SerializeArray(int[] items)
        {
            var builder = new StringBuilder();

            builder.Append('[');

            for (int i = 0; i < items.Length; i++)
            {
                builder.Append(items[i]);

                if (i != items.Length - 1)
                    builder.Append(',');
            }

            builder.Append(']');

            return builder.ToString();
        }

        public static string SerializeArray(string[] items)
        {
            var builder = new StringBuilder();

            builder.Append('[');

            for (int i = 0; i < items.Length; i++)
            {
                builder.Append('\'');
                builder.Append(items[i]);
                builder.Append('\'');

                if (i != items.Length - 1)
                    builder.Append(',');
            }

            builder.Append(']');

            return builder.ToString();
        }
    }
}
