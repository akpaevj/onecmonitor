namespace OnecMonitor.Common.Models
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
