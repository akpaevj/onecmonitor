namespace OneSTools.Common.Platform.Unpack;

internal static partial class FileFormat
{
	public const uint V8FfSignature = 0x7fffffff;
	public const uint V8DefaultPageSize = 512;

	public static bool IsContainer(byte[] data)
	{
		var reader = new MemoryStream(data);
		try
		{
			ContainerHeader.Read(reader);
			BlockHeader.Read(reader);
		}
		catch (File8FormatException)
		{
			return false;
		}

		return true;
	}
}