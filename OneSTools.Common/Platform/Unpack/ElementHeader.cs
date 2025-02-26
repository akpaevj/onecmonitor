namespace OneSTools.Common.Platform.Unpack;

public struct ElementHeader(string name, DateTime creationDate, DateTime modificationDate)
{
    public readonly DateTime CreationDate = creationDate;
    public readonly DateTime ModificationDate = modificationDate;
    public readonly string Name = name;

    public static DateTime File8Date(ulong serializedDate)
    {
        return new DateTime((long) serializedDate * 1000);
    }

    public static ElementHeader Parse(byte[] buf)
    {
        var serializedCreationDate = BitConverter.ToUInt64(buf, 0);
        var serializedModificationDate = BitConverter.ToUInt64(buf, 8);
        // 4 байта на Reserved
        var enc = new System.Text.UnicodeEncoding(bigEndian: false, byteOrderMark: false);

        const int nameOffset = 8 + 8 + 4;
        var name = enc.GetString(buf, nameOffset, buf.Length - nameOffset - 4).TrimEnd('\0');

        var creationDate = File8Date(serializedCreationDate);
        var modificationDate = File8Date(serializedModificationDate);

        return new ElementHeader(name, creationDate, modificationDate);
    }
}