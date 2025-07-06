using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models
{
    public class Agent : DatabaseObject
    {
        [MaxLength(100)]
        public string InstanceName { get; set; } = string.Empty;
        
        public virtual List<Cluster> Clusters { get; set; } = [];
        public virtual List<TechLogSeance> TechLogSeances { get; set; } = [];

        public override bool Equals(object? obj)
            => obj is Agent agent &&
               Id.Equals(agent.Id);

        public override int GetHashCode()
            => HashCode.Combine(Id);
    }
}
