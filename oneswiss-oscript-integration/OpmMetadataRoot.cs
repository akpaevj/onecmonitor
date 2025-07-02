using System.Xml.Serialization;

namespace OneSwiss.OneScript.Oscript;

public class OpmMetadataRoot
{
    [XmlElement("opm-metadata")] 
    public OpmMetadata Data { get; set; } = null!;
}