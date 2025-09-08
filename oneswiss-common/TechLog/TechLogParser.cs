using OneSwiss.Common.Models;

namespace OneSwiss.Common.TechLog;

public static class TechLogParser
{
    public static bool TryParse(TechLogEventContent eventContent, out TjEvent tjEvent)
    {
        var content = eventContent.Content.AsSpan();

        tjEvent = new TjEvent
        {
            AgentId = eventContent.AgentId,
            SeanceId = eventContent.SeanceId,
            TemplateId = eventContent.TemplateId,
            FileName = eventContent.FileName,
            EndPosition = eventContent.EndPosition
        };

        int offset;
        if (TryParseDateTime(content[..26], out var dateTime))
        {
            offset = 27;
            tjEvent.DateTime = dateTime;
        }
        else
        {
            return false;
        }

        if (TryReadLongValue(content[offset..], out var duration, out var dLen))
        {
            offset += dLen + 1;
            tjEvent.Duration = duration;
        }
        else
        {
            return false;
        }

        if (TryReadNamelessValue(content[offset..], out var eventName, out var nLen))
        {
            offset += nLen + 1;
            tjEvent.EventName = eventName.ToString();
        }
        else
        {
            return false;
        }

        if (TryReadIntValue(content[offset..], out var level, out var lLen))
        {
            offset += lLen + 1;
            tjEvent.Level = level;
        }
        else
        {
            return false;
        }

        return TryReadProperties(content[offset..], tjEvent);
    }

    private static bool TryParseDateTime(ReadOnlySpan<char> content, out DateTime dateTime)
    {
        if (DateTime.TryParse(content, out var eventDateTime))
        {
            dateTime = eventDateTime;
            return true;
        }

        dateTime = DateTime.MinValue;
        return false;
    }

    private static bool TryReadIntValue(ReadOnlySpan<char> content, out int intValue, out int length)
    {
        if (TryReadNamelessValue(content, out var value, out length) && int.TryParse(value, out intValue))
            return true;

        intValue = int.MinValue;
        return false;
    }

    private static bool TryReadLongValue(ReadOnlySpan<char> content, out long longValue, out int length)
    {
        if (TryReadNamelessValue(content, out var value, out length) && long.TryParse(value, out longValue))
            return true;

        longValue = long.MinValue;
        return false;
    }

    private static bool TryReadNamelessValue(ReadOnlySpan<char> content, out ReadOnlySpan<char> value, out int length)
    {
        var i = content.IndexOf(',');

        if (i == -1)
        {
            value = ReadOnlySpan<char>.Empty;
            length = 0;
            return false;
        }

        value = content[..i];
        length = i;
        return true;
    }

    private static bool TryGetTextValue(ReadOnlySpan<char> content, out ReadOnlySpan<char> value, out int length)
    {
        var firstChar = content[0];

        var index = 0;
        var counted = 0;

        while (index < content.Length)
        {
            index += content[index..].IndexOf(firstChar);
            var nextCh = index + 1 >= content.Length ? '\0' : content[index + 1];

            if (content[index] == firstChar)
                counted++;

            index++;

            if (counted <= 1 || counted % 2 != 0 || nextCh == firstChar)
                continue;

            value = content[1..(index - 1)];
            length = index;
            return true;
        }

        value = ReadOnlySpan<char>.Empty;
        length = 0;
        return false;
    }

    private static void AddProperty(TjEvent tjEvent, ReadOnlySpan<char> propertyName, ReadOnlySpan<char> propertyValue,
        int postfix = 0)
    {
        while (true)
        {
            var n = postfix > 0 ? $"{propertyName}{postfix}" : propertyName.ToString();

            if (tjEvent.Properties.ContainsKey(n))
            {
                postfix += 1;
                continue;
            }

            tjEvent.Properties.Add(n, propertyValue.ToString());

            break;
        }
    }

    private static bool TryReadProperties(ReadOnlySpan<char> content, TjEvent tjEvent)
    {
        while (true)
        {
            if (content.Length == 0)
                return true;

            var equalIndex = content.IndexOf('=');

            if (equalIndex == -1)
                return false;

            var propertyName = content[..equalIndex];

            var valueStartIndex = equalIndex + 1;
            content = content[valueStartIndex..];

            if (content.Length == 0)
                return true;

            switch (content[0])
            {
                case '\'':
                case '"':
                {
                    if (TryGetTextValue(content, out var value, out var length))
                    {
                        AddProperty(tjEvent, propertyName, value);
                        content = content[length..];

                        if (content.Length == 0)
                            return true;

                        if (content[0] == ',')
                            content = content[1..];
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }
                case ',':
                    AddProperty(tjEvent, propertyName, ReadOnlySpan<char>.Empty);
                    content = content[1..];
                    break;
                default:
                {
                    var valueEndIndex = content.IndexOf(',');

                    if (valueEndIndex == -1)
                    {
                        AddProperty(tjEvent, propertyName, content);
                        return true;
                    }

                    AddProperty(tjEvent, propertyName, content[..valueEndIndex]);
                    content = content[(valueEndIndex + 1)..];

                    break;
                }
            }
        }
    }
}