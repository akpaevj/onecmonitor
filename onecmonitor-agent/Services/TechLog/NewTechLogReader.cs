using System.Text;
using OnecMonitor.Agent.Extensions;

namespace OnecMonitor.Agent.Services.TechLog
{
    internal sealed class NewTechLogReader : ITechLogReader, IDisposable
    {
        private readonly StreamReader _reader;
        private readonly StringBuilder _eventContentBuffer = new();
        private readonly int _prefixLength;
        private bool _disposedValue;

        public long Position { get; private set; }
        public string FilePath { get; }
        public string EventContent { get; private set; } = string.Empty;

        public NewTechLogReader(string path, long position = 0)
        {
            FilePath = path;

            var noBomEncoding = new UTF8Encoding(false);
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            _reader = new StreamReader(fileStream, noBomEncoding);
            _reader.Peek();

            var fileName = Path.GetFileNameWithoutExtension(path);
            var prefix = $"20{fileName[..2]}-{fileName[2..4]}-{fileName[4..6]} {fileName[6..8]}:";
            _prefixLength = prefix.Length;
            _eventContentBuffer.Append(prefix);

            if (position > 0)
                _reader.SetPosition(position);
            else if (!Equals(_reader.CurrentEncoding, noBomEncoding))
                _reader.SetPosition(3);

            Position = _reader.GetPosition();
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
                        EventContent = _eventContentBuffer.TrimEnd().ToString();
                        _eventContentBuffer.Remove(_prefixLength, _eventContentBuffer.Length - _prefixLength);
                    }
                }
                else if (lineContainsData && !IsEventBeginning(line)) // skip lines till the next event beginning
                    continue;

                if (lineContainsData)
                    _eventContentBuffer.AppendLine(line);

                if (eventRead)
                    break;
                else
                    Position = _reader.GetPosition();
            }

            return !string.IsNullOrEmpty(EventContent);
        }

        private static bool IsEventBeginning(ReadOnlySpan<char> line)
            => line.Length > 5
                && char.IsDigit(line[0])
                && char.IsDigit(line[1])
                && line[2] == ':'
                && char.IsDigit(line[3])
                && char.IsDigit(line[4])
                && line[5] == '.';

        private bool EventContentBufferHasData() => _eventContentBuffer.Length > _prefixLength;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    _reader.Dispose();

                EventContent = string.Empty;
                _eventContentBuffer.Clear();
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
