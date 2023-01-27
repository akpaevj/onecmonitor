using MessagePack;

namespace OnecMonitor.Common.Models
{
    [MessagePackObject]
    public class TechLogSeanceDto
    {
        [Key(0)]
        public Guid Id { get; set; }
        [Key(1)]
        public DateTime StartDateTime { get; set; }
        [Key(2)]
        public DateTime FinishDateTime { get; set; }
        [Key(3)]
        public string Template { get; set; } = string.Empty;
    }
}
