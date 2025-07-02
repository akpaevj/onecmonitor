using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class FileDto
{
    [Key(0)]
    public Guid Id { get; set; }
    [Key(1)]
    public string Name { get; set; }
    [Key(2)]
    public string FileExtension { get; set; }
    [Key(3)]
    public string Version { get; set; }
    [Key(4)] 
    public bool IsUpdate { get; set; } = false;
    [Key(5)] 
    public bool IsExtension { get; set; } = false;
    [Key(6)] 
    public bool IsConfiguration { get; set; } = false;
    [Key(7)] 
    public long Length { get; set; }
}