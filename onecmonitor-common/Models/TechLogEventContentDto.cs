using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Common.Models
{
    [MessagePackObject]
    public class TechLogEventContentDto
    {
        [Key(0)]
        public Guid SeanceId { get; set; }
        [Key(1)]
        public Guid TemplateId { get; set; }
        [Key(2)]
        public string Folder { get; set; } = string.Empty;
        [Key(3)]
        public string File { get; set; } = string.Empty;
        [Key(4)]
        public string Content { get; set; } = string.Empty;
        [Key(5)]
        public long EndPosition { get; set; }
    }
}
