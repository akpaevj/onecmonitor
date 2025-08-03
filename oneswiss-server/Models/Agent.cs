using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models.MaintenanceTasks;

namespace OneSwiss.Server.Models
{
    public class Agent : DatabaseObject
    {
        [MaxLength(100)]
        public string InstanceName { get; set; } = string.Empty;
        
        public virtual List<Cluster> Clusters { get; set; } = [];
        public virtual List<TechLogSeance> TechLogSeances { get; set; } = [];
        public virtual List<MaintenanceTask> MaintenanceTasks { get; set; } = [];

        public override bool Equals(object? obj)
            => obj is Agent agent &&
               Id.Equals(agent.Id);

        public override int GetHashCode()
            => HashCode.Combine(Id);
    }
}
