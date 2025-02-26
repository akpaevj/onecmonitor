namespace OneSTools.Common.Platform.Unpack;

public class File8
{
    internal File8(ElementHeader header, uint dataOffset)
    {
        DataOffset = (int)dataOffset;

        Name = header.Name;
        ModificationTime = header.ModificationDate;
        CreationTime = header.CreationDate;
    }
    
    public string Name { get; }
    public DateTime ModificationTime { get; }
    public DateTime CreationTime { get; }
    public int DataOffset { get; }
}