namespace OneSwiss.Common.DTO;

public class Message
{
    public Message(MessageHeader header)
    {
        Header = header;
        Data = Memory<byte>.Empty;
    }

    public Message(MessageHeader header, Memory<byte> data)
    {
        Header = header;
        Data = data;
    }

    public MessageHeader Header { get; set; }
    public ReadOnlyMemory<byte> Data { get; set; }
}