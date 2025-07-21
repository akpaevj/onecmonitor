using OneSTools.FileDatabase.Extensions;

namespace OneSwiss.V8.Platform.Services;

public class Args
{
    public List<ArgsItem> Items { get; } = [];
    
    public int ItemsCount => Items.Count;

    public void AddOption(string name)
        => Items.Add(new ArgsOption { Option = name });
    
    public void AddValue(string value)
        => Items.Add(new ArgsValue { Value = value });
    
    public void AddParameter(string key, string value)
        => Items.Add(new ArgsParameter { Key = key, Value = value });

    public ArgsOption? ItemByIndexAsOption(int index)
    {
        var item = Items[index];
        
        if (item is ArgsOption option)
            return option;

        return null;
    }
    
    public ArgsValue? ItemByIndexAsValue(int index)
    {
        var item = Items[index];
        
        if (item is ArgsValue value)
            return value;

        return null;
    }
    
    public ArgsParameter? ItemByIndexAsParameter(int index)
    {
        var item = Items[index];
        
        if (item is ArgsParameter parameter)
            return parameter;

        return null;
    }
    
    public bool HasOption(string optionName)
        => Items.FirstOrDefault(item =>
            item is ArgsOption option &&
            option.Option.Equals(optionName, StringComparison.CurrentCultureIgnoreCase)) is not null;

    public bool HasParameter(string parameterNameLong, string parameterNameShort, out string? value)
    {
        if (Items.FirstOrDefault(item => 
                item is ArgsParameter argsParameter &&
                (argsParameter.Key.Equals(parameterNameLong, StringComparison.CurrentCultureIgnoreCase) ||
                 argsParameter.Key.Equals(parameterNameShort, StringComparison.CurrentCultureIgnoreCase))) is not
            ArgsParameter parameter)
        {
            value = null;
            return false;
        }

        value = parameter.Value;
        return true;
    }
    
    public bool HasParameter(string parameterNameLong, out string? value)
    {
        if (Items.FirstOrDefault(item => 
                item is ArgsParameter argsParameter &&
                argsParameter.Key.Equals(parameterNameLong, StringComparison.CurrentCultureIgnoreCase)) is not
            ArgsParameter parameter)
        {
            value = null;
            return false;
        }

        value = parameter.Value;
        return true;
    }
}

public abstract record ArgsItem;

public record ArgsOption : ArgsItem
{
    public string Option { get; set; } = string.Empty;
}

public record ArgsValue : ArgsItem
{
    public string Value { get; set; } = string.Empty;
}

public record ArgsParameter : ArgsItem
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public static class ArgsParser
{
    public static Args ParsePairs(string commandLine)
    {
        var result = new Args();
        
        var args = Parse(commandLine);

        var keyRead = false;
        var key = string.Empty;
        
        foreach (var argItem in args)
        {
            if (keyRead)
            {
                if (argItem.StartsWith('-'))
                {
                    result.AddOption(key.TrimStart('-'));
                    keyRead = true;
                    key = argItem;
                }
                else
                {
                    result.AddParameter(key.TrimStart('-'), argItem);
                    keyRead = false;
                    key = string.Empty;
                }
            }
            else
            {
                if (argItem.StartsWith("--"))
                {
                    key = argItem.TrimStart('-');

                    if (key.Contains('='))
                    {
                        var kv = key.Split('=', 2);
                        
                        result.AddParameter(kv[0].TrimStart('-'), kv[1]);
                        keyRead = false;
                        key = string.Empty;
                    }
                    else
                    {
                        result.AddOption(key);
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
                    if (string.IsNullOrEmpty(key))
                        result.AddValue(argItem);
                    else
                        result.AddParameter(key, argItem);
                    
                    keyRead = false;
                    key = string.Empty;
                }
            }
        }
        
        if (keyRead && !string.IsNullOrEmpty(key))
            result.AddOption(key.TrimStart('-'));

        return result;
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