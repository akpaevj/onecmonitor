using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace OnecMonitor.Agent.Services
{
    public class TechLogReader : ITechLogReader, IDisposable
    {
        private readonly FileStream _fileStream;

        private readonly Memory<byte> _buffer;
        private int _bufferSize;
        private int _bufferPos;
        private Memory<byte> _actualBuffer => _buffer[_bufferPos.._bufferSize];

        private readonly int _eventPrefixLength = 0;
        private Memory<byte> _eventContentBuffer;
        private int _eventContentSize;
        private Memory<byte> _actualEventContentBuffer => _eventContentBuffer[.._eventContentSize];

        private bool disposedValue;

        /// <summary>
        /// Actual reader's position
        /// </summary>
        public long Position { get; private set; } = 0;
        /// <summary>
        /// Name of the log file without extension
        /// </summary>
        public string FilePath { get; private set; } = string.Empty;
        /// <summary>
        /// Text representation of the event content
        /// </summary>
        public string EventContent => Encoding.UTF8.GetString(_actualEventContentBuffer.Span).TrimEnd();

        public TechLogReader(string path, long startPosition = 0, int bufferSize = 4096)
        {
            FilePath = path;

            _buffer = new(new byte[bufferSize]);
            _eventContentBuffer = new(new byte[bufferSize]);

            _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize * 4, FileOptions.SequentialScan);
            _fileStream.Seek(startPosition, SeekOrigin.Begin);
            Position = startPosition;

            if (Position == 0)
            {
                FillBuffer();

                // skip bom
                if (_actualBuffer.Length >= 3 && _actualBuffer.Span[0] == 0xef && _actualBuffer.Span[1] == 0xbb && _actualBuffer.Span[2] == 0xbf)
                {
                    Position = 3;
                    _bufferPos = 3;
                }
            }

            var fileName = Path.GetFileNameWithoutExtension(path);
            _eventPrefixLength = Encoding.UTF8.GetBytes($"20{fileName[0..2]}-{fileName[2..4]}-{fileName[4..6]} {fileName[6..8]}:", _eventContentBuffer.Span);
        }

        public bool MoveNext()
        {
            _eventContentSize = _eventContentBuffer[.._eventPrefixLength].Length;

            while (true)
            {
                // check there is available data in the buffer
                if (_actualBuffer.Length == 0 && FillBuffer() == 0)
                    break;

                // try to find line feed in the buffer
                var lineFeedIndex = _actualBuffer.Span.IndexOf((byte)0x0a);
                var lineFeedFound = lineFeedIndex >= 0;
                var chunk = lineFeedFound ? _actualBuffer[..(lineFeedIndex + 1)] : _actualBuffer;

                // check event content buffer has enough size
                var necessaryLength = _actualEventContentBuffer.Length + chunk.Length;
                if (necessaryLength > _eventContentBuffer.Length)
                {
                    var newSize = GetEventContentBufferNewSize(necessaryLength);
                    var newBuffer = new Memory<byte>(new byte[newSize]);
                    _actualEventContentBuffer.CopyTo(newBuffer);
                    _eventContentBuffer = newBuffer;
                }

                // Add event chunk to event content buffer
                chunk.CopyTo(_eventContentBuffer[_actualEventContentBuffer.Length..]);
                _eventContentSize += chunk.Length;
                _bufferPos += chunk.Length;
                Position += chunk.Length;

                if (lineFeedFound)
                {
                    if (_actualBuffer.Length < 6)
                        FillBuffer();

                    if (_actualBuffer.Length >= 6)
                    {
                        if (IsNewEventLine())
                            break;
                    }
                    else
                        break;
                }
            }

            return _actualEventContentBuffer.Length > _eventPrefixLength;
        }

        private int GetEventContentBufferNewSize(int necessaryLength)
        {
            // double event content buffer while target event content buffer size less than calculated one
            var multiplier = 2;

            while (true)
            {
                var newSize = _eventContentBuffer.Length * multiplier;

                if (newSize > necessaryLength)
                    return newSize;
                else
                    multiplier *= 2;
            }
        }

        private bool IsNewEventLine()
        {
            var b0 = _actualBuffer.Span[0];
            var b1 = _actualBuffer.Span[1];
            var b2 = _actualBuffer.Span[2];
            var b3 = _actualBuffer.Span[3];
            var b4 = _actualBuffer.Span[4];
            var b5 = _actualBuffer.Span[5];

            var isNewLine = (0x2f < b0 && b0 < 0x3a) // is digit
                && (0x2f < b1 && b1 < 0x3a) // is digit
                && b2 == 0x3a // is :
                && (0x2f < b3 && b3 < 0x3a) // is digit
                && (0x2f < b4 && b4 < 0x3a) // is digit
                && b5 == 0x2e; // is .

            return isNewLine;
        }

        private int FillBuffer()
        {
            // Shift unread bytes to the left
            for (int i = 0; i < _actualBuffer.Length; i++)
                _buffer.Span[i] = _actualBuffer.Span[i];

            var read = _fileStream.Read(_buffer[_actualBuffer.Length..].Span);
            _bufferSize = _actualBuffer.Length + read;
            _bufferPos = 0;

            return read;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _fileStream?.Dispose();
                _eventContentBuffer = null;

                disposedValue = true;
            }
        }

        ~TechLogReader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
