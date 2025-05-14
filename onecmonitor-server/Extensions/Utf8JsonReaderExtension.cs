using System.Text.Json;

namespace OnecMonitor.Server.Extensions;

public static class Utf8JsonReaderExtension
{
    public static void SkipTo(this ref Utf8JsonReader reader, JsonTokenType tokenType, bool skipFindingToken = false)
    {
        while (reader.TokenType != tokenType)
            reader.Read();
        
        if (skipFindingToken)
            reader.Read();
    }
}