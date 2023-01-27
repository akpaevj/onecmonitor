using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Agent.Models
{
    public class TechLogSeance
    {
        public Guid Id { get; set; }
        public DateTime StartDateTime { get; set; } = DateTime.MinValue;
        public DateTime FinishDateTime { get; set; } = DateTime.MaxValue;
        public string Template { get; set; } = string.Empty;
        public TechLogSeanceStatus Status { get; set; }
    }
}
