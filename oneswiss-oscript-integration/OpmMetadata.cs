using System.Xml.Serialization;

namespace OneSwiss.OneScript;

[XmlRoot("opm-metadata", Namespace = "http://oscript.io/schemas/opm-metadata/1.0")]
public class OpmMetadata
{
    [XmlElement("name")] public string Name { get; init; } = string.Empty;

    [XmlElement("version")] public string Version { get; init; } = string.Empty;

    [XmlElement("executable")] public string Executable { get; set; } = string.Empty;
}