using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Common.Models
{
    [MessagePackObject]
    public class AgentInstance
    {
        [Key(0)]
        public Guid Id { get; set; } = Guid.Empty;
        [Key(1)]
        public string InstanceName { get; set; } = string.Empty;
        [Key(2)]
        public double UtcOffset { get; set; } = 0;
    }
}
