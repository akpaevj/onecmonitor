using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OnecMonitor.Server.Converters.Json;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace OnecMonitor.Server.Models
{
    public class Agent
    {
        public Guid Id { get; init; }
        public string InstanceName { get; set; } = string.Empty;

        public List<TechLogSeance> Seances { get; set; } = new();

        public override bool Equals(object? obj)
        {
            return obj is Agent agent &&
                   Id.Equals(agent.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}
