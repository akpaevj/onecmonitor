using MessagePack;
using System.Net.Sockets;
using System.Numerics;

namespace OnecMonitor.Common.Models
{
    public struct MessageHeader
    {
        public const int HEADER_LENGTH = 21;

        public MessageType Type { get; set; }
        public int Length { get; set; } = 0;
        public Guid CallId { get; set; } = Guid.Empty;

        public MessageHeader(MessageType type, int length)
        {
            Type = type; 
            Length = length;
        }

        public MessageHeader(MessageType type, int length, Guid callId)
        {
            Type = type;
            Length = length;
            CallId = callId;
        }

        public ReadOnlyMemory<byte> ToBytesArray()
        {
            var memory = new Memory<byte>(new byte[HEADER_LENGTH]);

            memory.Span[0] = (byte)Type;
            BitConverter.TryWriteBytes(memory[1..].Span, Length);
            CallId.TryWriteBytes(memory[5..].Span);

            return memory;
        }

        public static MessageHeader FromBytesArray(ReadOnlySpan<byte> bytes)
        {
            var type = (MessageType)bytes[0];
            var length = BitConverter.ToInt32(bytes[1..]);
            var callId = new Guid(bytes[5..]);

            return new MessageHeader(type, length, callId);
        }
    }
}
