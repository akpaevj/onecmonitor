using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Common.TechLog
{
    public class TechLogReader : IDisposable
    {
        private readonly FileStream _fileStream;

        private readonly Memory<byte> _buffer;
        private int _bufferSize;
        private int _bufferPos;

        private readonly int _eventPrefixLength = 0;
        private Memory<byte> _eventContentBuffer = new();
        private int _eventContentSize = 0;

        private bool disposedValue;

        /// <summary>
        /// Actual reader's position
        /// </summary>
        public long Position { get; private set; } = 0;
        /// <summary>
        /// Techlog folder. Represents process name that's writing log files
        /// </summary>
        public string Folder { get; private set; } = string.Empty;
        /// <summary>
        /// Name of the log file without extension
        /// </summary>
        public string FileName { get; private set; } = string.Empty;
        /// <summary>
        /// Text representation of the event content
        /// </summary>
        public string EventContent => Encoding.UTF8.GetString(_eventContentBuffer[.._eventContentSize].Span).TrimEnd();
        /// <summary>
        /// Raw bytes that readed from the file. Before reading the next event (if you're gonna work with it) you should copy this buffer to your own one,
        /// otherwise it will be overwritten during reading the next event
        /// </summary>
        public ReadOnlyMemory<byte> RawEventContent => _eventContentBuffer[.._eventContentSize].TrimEnd("\r\n"u8);
        /// <summary>
        /// Event's start position
        /// </summary>
        public long EventContentStartPosition { get; private set; } = 0;
        /// <summary>
        /// EOF
        /// </summary>
        public bool EndOfStream { get; private set; } = false;

        public TechLogReader(string path, long startPosition = 0, int bufferSize = 4096)
        {
            Folder = Path.GetFileName(Path.GetDirectoryName(path)) ?? "";
            FileName = Path.GetFileNameWithoutExtension(path)!;

            _buffer = new Memory<byte>(new byte[bufferSize]);
            _eventContentBuffer = new Memory<byte>(new byte[bufferSize]);

            _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize * 4, FileOptions.SequentialScan);
            _fileStream.Seek(startPosition, SeekOrigin.Begin);
            Position = startPosition;

            if (Position == 0)
            {
                FillBuffer();

                // skip bom
                if (_bufferSize >= 3 && _buffer.Span[0] == 0xef && _buffer.Span[1] == 0xbb && _buffer.Span[2] == 0xbf)
                {
                    Position = 3;
                    _bufferPos = 3;
                }
            }

            _eventPrefixLength = Encoding.UTF8.GetBytes($"20{FileName[0..2]}-{FileName[2..4]}-{FileName[4..6]} {FileName[6..8]}:", _eventContentBuffer.Span);
        }

        public bool MoveNext()
        {
            EndOfStream = false;

            _eventContentSize = _eventPrefixLength;
            EventContentStartPosition = Position;

            while (true)
            {
                // check there is available data in the buffer
                if (AvailableBytes() == 0 && FillBuffer() == 0)
                {
                    EndOfStream = true;
                    break;
                }

                // try to find line feed in the buffer
                var lineFeedIndex = _buffer[_bufferPos..].Span.IndexOf((byte)0x0a);
                var lineFeedFound = lineFeedIndex >= 0;

                var chunkLength = lineFeedFound ? lineFeedIndex + 1 : AvailableBytes();
                var chunk = _buffer.Slice(_bufferPos, chunkLength);

                // check event content buffer has enough size
                var necessaryLength = _eventContentSize + chunkLength;
                if (necessaryLength > _eventContentBuffer.Length)
                {
                    var newSize = GetEventContentBufferNewSize(necessaryLength);
                    var newBuffer = new Memory<byte>(new byte[newSize]);
                    _eventContentBuffer.CopyTo(newBuffer);
                    _eventContentBuffer = newBuffer;
                }

                // Add event chunk to event content buffer
                chunk.CopyTo(_eventContentBuffer[_eventContentSize..]);
                _bufferPos += chunkLength;
                _eventContentSize += chunkLength;
                Position += chunkLength;

                if (lineFeedFound)
                {
                    // it needs 6 bytes to recognize new line as beginning of the next event
                    if (AvailableBytes() < 6)
                        FillBuffer();

                    // Check the next line is an event beginning
                    if (AvailableBytes() >= 6 && IsNewEventLine())
                        break;
                    else
                        continue;
                }
            }

            return EventContentHasData();
        }

        private bool EventContentHasData()
            => _eventContentSize > _eventPrefixLength;

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

        private int AvailableBytes()
            => _bufferSize - _bufferPos;

        private bool IsNewEventLine()
        {
            var bytes = _buffer.Slice(_bufferPos, 6);

            var b0 = bytes.Span[0];
            var b1 = bytes.Span[1];
            var b3 = bytes.Span[3];
            var b4 = bytes.Span[4];

            return
                0x2f < b0 && b0 < 0x3a // is digit
                && 0x2f < b1 && b1 < 0x3a // is digit
                && bytes.Span[2] == 0x3a // is :
                && 0x2f < b3 && b4 < 0x3a // is digit
                && 0x2f < b4 && b4 < 0x3a // is digit
                && bytes.Span[5] == 0x2e; // is .
        }

        private int FillBuffer()
        {
            var offset = 0;
            var needShift = AvailableBytes();

            if (needShift > 0)
            {
                offset = needShift;

                for (int i = 0; i < needShift; i++)
                    _buffer.Span[i] = _buffer.Span[_bufferPos + i];
            }

            var read = _fileStream.Read(_buffer[offset..].Span);
            _bufferSize = read + needShift;
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
