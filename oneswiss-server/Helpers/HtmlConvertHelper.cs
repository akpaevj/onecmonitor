namespace OneSwiss.Server.Helpers;

public class HtmlConvertHelper
{
    public static string DateTimeToString(DateTime date)
    {
        return date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
    }
}