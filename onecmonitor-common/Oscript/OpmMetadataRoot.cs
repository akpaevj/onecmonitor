using System.Xml.Serialization;

namespace OnecMonitor.Common.Oscript;

public class OpmMetadataRoot
{
    [XmlElement("opm-metadata")] 
    public OpmMetadata Data { get; set; } = null!;
}