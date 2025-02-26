namespace OneSTools.Common.Platform.Unpack;

public struct ContainerHeader
{
    public readonly uint NextPageAddr;
    public readonly uint PageSize;
    public readonly uint StorageVer;
    public readonly uint Reserved;
		
    private ContainerHeader(uint nextPageAddr = FileFormat.V8FfSignature, uint pageSize = FileFormat.V8DefaultPageSize, uint storageVer = 0, uint reserved = 0)
    {
        NextPageAddr = nextPageAddr;
        PageSize = pageSize;
        StorageVer = 0;
        Reserved = 0;
    }

    public static ContainerHeader Read(Stream reader)
    {
        const int headerSize = 16;
        var buf = new byte[headerSize];
        if (reader.Read(buf, 0, headerSize) < headerSize)
        {
            throw new File8FormatException();
        }

        return new ContainerHeader(
            nextPageAddr: BitConverter.ToUInt32(buf, 0),
            pageSize: BitConverter.ToUInt32(buf, 4),
            storageVer: BitConverter.ToUInt32(buf, 8),
            reserved: BitConverter.ToUInt32(buf, 12)
        );
    }
}