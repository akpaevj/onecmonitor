namespace OneSTools.Common.Platform.Unpack;

public readonly struct ElementAddress(uint headerAddress, uint dataAddress, uint signature = FileFormat.V8FfSignature)
{
    public uint HeaderAddress { get; } = headerAddress;
    public uint DataAddress { get; } = dataAddress;
    public uint Signature { get; } = signature;
    
    public static IList<ElementAddress> Parse(byte[] buf)
    {
        const int elementSize = 4 + 4 + 4;
        var result = new List<ElementAddress>();
        
        for (var offset = 0; offset + elementSize <= buf.Length; offset += elementSize)
        {
            var headerAddress = BitConverter.ToUInt32(buf, offset);
            var dataAddress = BitConverter.ToUInt32(buf, offset + 4);
            var signature = BitConverter.ToUInt32(buf, offset + 8);

            result.Add(new ElementAddress(headerAddress, dataAddress, signature));
        }

        return result;
    }
    
    public override string ToString()
        => $"{HeaderAddress:x8}:{DataAddress:x8}:{Signature:x8}";
}