using MessagePack;

namespace OneSwiss.Common.DTO
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
        public Guid TemplateId { get; set; }
        [Key(4)]
        public string Template { get; set; } = string.Empty;
    }
}
