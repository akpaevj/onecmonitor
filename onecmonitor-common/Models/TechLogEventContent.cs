using MessagePack;

namespace OnecMonitor.Common.Models
{
    public class TechLogEventContent
    {
        public Guid AgentId { get; set; }
        public Guid SeanceId { get; set; }
        public Guid TemplateId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long EndPosition { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
