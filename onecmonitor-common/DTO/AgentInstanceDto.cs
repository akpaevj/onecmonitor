using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class AgentInstanceDto
{
    [Key(0)]
    public Guid Id { get; set; } = Guid.Empty;
    [Key(1)]
    public string InstanceName { get; set; } = string.Empty;
    [Key(2)] 
    public bool MainConnection { get; set; }
    [Key(3)]
    public double UtcOffset { get; set; } = 0;
}