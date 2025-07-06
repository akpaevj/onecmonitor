namespace OneSwiss.V8.Platform.Services;

public record ArgsKeyValue(string Key, string Value);

public static class ArgsParser
{
    public static string? GetOptionValue(this ArgsKeyValue[] args, string longKey, string shortKey)
    {
        var arg = args.FirstOrDefault(a => a.Key == longKey);
        return arg != null ? arg.Value : args.FirstOrDefault(a => a.Key == shortKey)?.Value;
    }

    public static string? GetOptionValue(this ArgsKeyValue[] args, string key)
        => args.FirstOrDefault(a => a.Key == key)?.Value;
    
    public static ArgsKeyValue[] ParsePairs(string commandLine)
    {
        var items = new List<ArgsKeyValue>();
        
        var args = Parse(commandLine);

        var keyRead = false;
        var key = string.Empty;
        
        foreach (var argItem in args)
        {
            if (keyRead)
            {
                items.Add(new ArgsKeyValue(key, argItem));
                keyRead = false;
                key = string.Empty;
            }
            else
            {
                if (argItem.StartsWith("--"))
                {
                    key = argItem.TrimStart('-');

                    if (key.Contains('='))
                    {
                        var kv = key.Split('=', 2);
                        
                        items.Add(new ArgsKeyValue(kv[0], kv[1]));
                        keyRead = false;
                        key = string.Empty;
                    }
                    else
                    {
                        items.Add(new ArgsKeyValue("", key));
                        keyRead = false;
                        key = string.Empty;
                    }
                }
                else if (argItem.StartsWith('-'))
                {
                    key = argItem.TrimStart('-');
                    keyRead = true;
                }
                else
                {
                    items.Add(new ArgsKeyValue(key, argItem));
                    keyRead = false;
                    key = string.Empty;
                }
            }
        }

        return items.ToArray();
    }

    private static string[] Parse(string commandLine)
    {
        var inQuotes = false;

        return commandLine.Split(c =>
            {
                if (c == '\"')
                    inQuotes = !inQuotes;

                return !inQuotes && c == ' ';
            })
            .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
            .Where(arg => !string.IsNullOrEmpty(arg))
            .ToArray();
    }
    
    private static IEnumerable<string> Split(this string str, 
        Func<char, bool> controller)
    {
        var nextPiece = 0;

        for (var c = 0; c < str.Length; c++)
        {
            if (!controller(str[c])) 
                continue;
            
            yield return str.Substring(nextPiece, c - nextPiece);
            nextPiece = c + 1;
        }

        yield return str[nextPiece..];
    }
    
    private static string TrimMatchingQuotes(this string input, char quote)
    {
        if (input.Length >= 2 && input[0] == quote && input[^1] == quote)
            return input.Substring(1, input.Length - 2);

        return input;
    }
}