namespace OnecMonitor.Server.Helpers
{
    public class HtmlConvertHelper
    {
        public static string DateTimeToString(DateTime date)
            => date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
    }
}
