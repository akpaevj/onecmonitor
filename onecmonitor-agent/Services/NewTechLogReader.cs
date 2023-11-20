using OnecMonitor.Agent.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OnecMonitor.Agent.Services
{
    internal class NewTechLogReader : ITechLogReader, IDisposable
    {
        private readonly StreamReader _reader;
        private readonly StringBuilder _eventContentBuffer = new();
        private readonly int _prefixLength;
        private bool disposedValue;

        public long Position { get; private set; } = 0;
        public string FilePath { get; private set; } = string.Empty;
        public string EventContent { get; private set; } = string.Empty;

        public NewTechLogReader(string path, long position = 0)
        {
            FilePath = path;

            var noBomEncoding = new UTF8Encoding(false);
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            _reader = new StreamReader(fileStream, noBomEncoding);
            var peek = _reader.Peek();

            var fileName = Path.GetFileNameWithoutExtension(path);
            var prefix = $"20{fileName[0..2]}-{fileName[2..4]}-{fileName[4..6]} {fileName[6..8]}:";
            _prefixLength = prefix.Length;
            _eventContentBuffer.Append(prefix);

            if (position > 0)
                _reader.SetPosition(position);
            else if (_reader.CurrentEncoding != noBomEncoding)
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
                && char.IsDigit(line![0])
                && char.IsDigit(line[1])
                && line[2] == ':'
                && char.IsDigit(line[3])
                && char.IsDigit(line[4])
                && line[5] == '.';

        private bool EventContentBufferHasData() => _eventContentBuffer.Length > _prefixLength;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    _reader.Dispose();

                EventContent = string.Empty;
                _eventContentBuffer.Clear();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
