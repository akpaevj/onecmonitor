namespace OneSTools.Common.Platform.Unpack;

public struct BlockHeader(
    uint dataSize = 0,
    uint pageSize = FileFormat.V8DefaultPageSize,
    uint nextPageAddr = FileFormat.V8FfSignature)
{
    public uint DataSize { get; } = dataSize;
    public uint PageSize { get; } = pageSize;
    public uint NextPageAddr { get; } = nextPageAddr;

    private static void ReadExpectedByte(Stream reader, int expectedValue)
    {
        if (reader.ReadByte() != expectedValue)
            throw new File8FormatException();
    }

    private static uint ReadHexData(Stream reader)
    {
        var hex = new byte[8];
			
        if (reader.Read(hex, 0, 8) < 8)
        {
            throw new File8FormatException();
        }

        try
        {
            return Convert.ToUInt32(System.Text.Encoding.ASCII.GetString(hex), 16);
        }
        catch
        {
            throw new File8FormatException();
        }
    }

    public static BlockHeader Read(Stream reader)
    {
        ReadExpectedByte(reader, 0x0D);
        ReadExpectedByte(reader, 0x0A);

        var dataSize = ReadHexData(reader);
        ReadExpectedByte(reader, 0x20);
        var pageSize = ReadHexData(reader);
        ReadExpectedByte(reader, 0x20);
        var nextPageAddr = ReadHexData(reader);
        ReadExpectedByte(reader, 0x20);

        ReadExpectedByte(reader, 0x0D);
        ReadExpectedByte(reader, 0x0A);

        return new BlockHeader(dataSize, pageSize, nextPageAddr);
    }

}