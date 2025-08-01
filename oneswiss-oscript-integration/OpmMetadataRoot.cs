using System.Xml.Serialization;

namespace OneSwiss.OneScript;

public class OpmMetadataRoot
{
    [XmlElement("opm-metadata")] 
    public OpmMetadata Data { get; set; } = null!;
}