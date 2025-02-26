using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class UpdateInfoBaseTaskDto
{
    [Key(0)]
    public Guid Id { get; set; }
    [Key(1)] 
    public List<InfoBaseDto> InfoBases { get; set; } = [];
    [Key(2)] 
    public List<V8FileDto> Files { get; set; } = [];
}