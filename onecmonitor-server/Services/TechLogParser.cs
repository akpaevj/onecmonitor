using System;
using OnecMonitor.Common.Models;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Services
{
    public static class TechLogParser
    {
        public static bool TryParse(AgentInstance agentInstance, TechLogEventContentDto item, out TjEvent tjEvent)
        {
            var content = item.Content.AsSpan();

            tjEvent = new TjEvent()
            {
                AgentId = agentInstance.Id.ToString(),
                SeanceId = item.SeanceId.ToString(),
                Folder = item.Folder,
                File = item.File,
                EndPosition = item.EndPosition
            };

            int offset;
            if (TryParseDateTime(content[..26], agentInstance, out var dateTime))
            {
                offset = 27;
                tjEvent.DateTime = dateTime;
            }
            else
                return false;

            if (TryReadLongValue(content[offset..], out var duration, out var dLen))
            {
                offset += dLen + 1;
                tjEvent.Duration = duration;
            }
            else
                return false;

            if (TryReadNamelessValue(content[offset..], out var eventName, out var nLen))
            {
                offset += nLen + 1;
                tjEvent.EventName = eventName.ToString();
            }
            else
                return false;

            if (TryReadIntValue(content[offset..], out var level, out var lLen))
            {
                offset += lLen + 1;
                tjEvent.Level = level;
            }
            else
                return false;

            if (!TryReadProperties(content[offset..], tjEvent))
                return false;

            return true;
        }

        private static bool TryParseDateTime(ReadOnlySpan<char> content, AgentInstance agentInstance, out DateTime dateTime)
        {
            if (DateTime.TryParse(content, out var eventDateTime))
            {
                dateTime = DateTime.SpecifyKind(eventDateTime.AddMinutes(-agentInstance.UtcOffset), DateTimeKind.Utc);
                return true;
            }
            else
            {
                dateTime = DateTime.MinValue;
                return false;
            }
        }

        private static bool TryReadIntValue(ReadOnlySpan<char> content, out int intValue, out int length)
        {
            if (TryReadNamelessValue(content, out var value, out length) && int.TryParse(value, out intValue))
                return true;
            else
            {
                intValue = int.MinValue;
                return false;
            }
        }

        private static bool TryReadLongValue(ReadOnlySpan<char> content, out long longValue, out int length)
        {
            if (TryReadNamelessValue(content, out var value, out length) && long.TryParse(value, out longValue))
                return true;
            else
            {
                longValue = long.MinValue;
                return false;
            }
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
            else
            {
                value = content[..i];
                length = i;
                return true;
            }
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

                if (counted > 1 && counted % 2 == 0 && nextCh != firstChar)
                {
                    value = content[1..(index - 1)];
                    length = index;
                    return true;
                }
            }

            value = ReadOnlySpan<char>.Empty;
            length = 0;
            return false;
        }

        private static void AddProperty(TjEvent tjEvent, ReadOnlySpan<char> propertyName, ReadOnlySpan<char> propertyValue, int postfix = 0)
        {
            var n = postfix > 0 ? $"{propertyName}{postfix}": propertyName.ToString();

            if (tjEvent.Properties.ContainsKey(n))
                AddProperty(tjEvent, propertyName, propertyValue, postfix + 1);
            else
                tjEvent.Properties.Add(n, propertyValue.ToString());
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

                if (content[0] == '\'' || content[0] == '"') 
                {
                    if (TryGetTextValue(content, out var value, out var length))
                    {
                        AddProperty(tjEvent, propertyName, value);
                        content = content[length..];

                        if (content.Length == 0)
                            return true;
                        else if (content[0] == ',')
                            content = content[1..];
                    }
                    else
                        return false;
                }
                else if (content[0] == ',')
                {
                    AddProperty(tjEvent, propertyName, ReadOnlySpan<char>.Empty);
                    content = content[1..];
                }
                else
                {
                    var valueEndIndex = content.IndexOf(',');

                    if (valueEndIndex == -1)
                    {
                        AddProperty(tjEvent, propertyName, content);
                        return true;
                    }
                    else
                    {
                        AddProperty(tjEvent, propertyName, content[..valueEndIndex]);
                        content = content[(valueEndIndex + 1)..];
                    }
                }
            }
        }
    }
}