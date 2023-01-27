using MessagePack;

namespace OnecMonitor.Common.Models
{
    [MessagePackObject]
    public class LastFilePositionRequest
    {
        [Key(0)]
        public Guid SeanceId { get; set; }
        [Key(1)]
        public Guid TemplateId { get; set; }
        [Key(2)]
        public string Folder { get; set; } = string.Empty;
        [Key(3)]
        public string File { get; set; } = string.Empty;
    }
}
