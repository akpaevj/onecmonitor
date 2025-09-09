using System.Text;
using OneSwiss.Agent.Extensions;

namespace OneSwiss.Agent.Services.TechLog;

internal class TechLogReader : IDisposable
{
    private readonly StringBuilder _eventContentBuffer = new();
    private readonly int _prefixLength;
    private readonly StreamReader _reader;

    public TechLogReader(string path, long position = 0)
    {
        FilePath = path;

        var noBomEncoding = new UTF8Encoding(false);
        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096,
            FileOptions.SequentialScan);
        _reader = new StreamReader(fileStream, noBomEncoding);
        _reader.Peek();

        var fileName = Path.GetFileNameWithoutExtension(path);
        var fileNameParts = fileName.Split('_');
        var fileNameDate = fileNameParts[2];
        var prefix = $"20{fileNameDate[..2]}-{fileNameDate[2..4]}-{fileNameDate[4..6]} {fileNameDate[6..8]}:";
        _prefixLength = prefix.Length;
        _eventContentBuffer.Append(prefix);

        if (position > 0)
            _reader.SetPosition(position);
        else if (!Equals(_reader.CurrentEncoding, noBomEncoding))
            _reader.SetPosition(3);

        Position = _reader.GetPosition();
    }

    public string FilePath { get; private set; }
    public long Position { get; private set; }
    public string EventContent { get; private set; } = string.Empty;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool MoveNext()
    {
        EventContent = string.Empty;

        var eventRead = false;

        while (true)
        {
            var line = _reader.ReadLine();
            var lineContainsData = line != null;

            if (!lineContainsData)
                eventRead = true;

            if (EventContentBufferHasData())
            {
                if (lineContainsData)
                    eventRead = IsEventBeginning(line);

                if (eventRead)
                {
                    EventContent = _eventContentBuffer.TrimEnd()!.ToString();
                    _eventContentBuffer.Remove(_prefixLength, _eventContentBuffer.Length - _prefixLength);
                }
            }
            else if (lineContainsData && !IsEventBeginning(line)) // skip lines till the next event beginning
            {
                continue;
            }

            if (lineContainsData)
                _eventContentBuffer.AppendLine(line);

            if (eventRead)
                break;

            Position = _reader.GetPosition();
        }

        return !string.IsNullOrEmpty(EventContent);
    }

    private static bool IsEventBeginning(ReadOnlySpan<char> line)
    {
        return line.Length > 5
               && char.IsDigit(line[0])
               && char.IsDigit(line[1])
               && line[2] == ':'
               && char.IsDigit(line[3])
               && char.IsDigit(line[4])
               && line[5] == '.';
    }

    private bool EventContentBufferHasData()
    {
        return _eventContentBuffer.Length > _prefixLength;
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _reader.Dispose();
        _eventContentBuffer.Clear();
    }

    ~TechLogReader()
    {
        Dispose(false);
    }
}