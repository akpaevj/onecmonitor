using Microsoft.EntityFrameworkCore;
using OneSwiss.Agent.Converters;
using OneSwiss.Agent.Models;
using ScriptEngine.Machine;

namespace OneSwiss.Agent
{
    public class AppDbContext : DbContext
    {
        public string DbPath { get; }

        public DbSet<AgentInstance> AgentInstance { get; set; }

        public AppDbContext(IHostEnvironment hostEnvironment)
            => DbPath = Path.Join(hostEnvironment.ContentRootPath, "om-agent.db");

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<Guid>()
                .HaveConversion<GuidStringConverter>();
            
            configurationBuilder.Properties<DateTime>()
                .HaveConversion<DateTimeStringConverter>();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }
    }
}
