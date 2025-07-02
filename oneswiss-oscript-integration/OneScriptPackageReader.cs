using System.IO.Compression;
using System.Xml.Serialization;
using OneSwiss.OneScript.Oscript;

namespace OneSwiss.OneScript;

public abstract class OneScriptPackageReader
{
    public static OpmMetadata? Unzip(string filePath, string destination)
    {
        var temp = Directory.CreateTempSubdirectory().FullName;
        
        using var ospxStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ZipFile.ExtractToDirectory(ospxStream, temp);

        var opmMetadataPath = Path.Combine(temp, "opm-metadata.xml");
        if (!File.Exists(opmMetadataPath))
            throw new Exception("opm-metadata.xml не обнаружен");
        
        using var metadataStream = new FileStream(opmMetadataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); 
        var serializer = new XmlSerializer(typeof(OpmMetadata));
        var metadata = serializer.Deserialize(metadataStream) as OpmMetadata;

        var contentPath = Path.Combine(temp, "content.zip");
        if (!File.Exists(contentPath))
            throw new Exception("content.zip не обнаружен");
        
        using var contentStream = new FileStream(contentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ZipFile.ExtractToDirectory(contentStream, destination);

        return metadata;
    }
}