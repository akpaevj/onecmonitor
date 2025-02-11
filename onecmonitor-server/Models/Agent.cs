using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.Models
{
    public class Agent : DatabaseObject
    {
        [MaxLength(100)]
        public string InstanceName { get; set; } = string.Empty;
        
        public virtual List<TechLogSeance> Seances { get; set; } = [];
        public virtual List<Cluster> Clusters { get; set; } = [];

        public override bool Equals(object? obj)
            => obj is Agent agent &&
               Id.Equals(agent.Id);

        public override int GetHashCode()
            => HashCode.Combine(Id);
    }
}
