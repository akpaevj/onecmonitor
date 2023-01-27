using Microsoft.EntityFrameworkCore;
using OnecMonitor.Agent.Converters;
using OnecMonitor.Agent.Models;
using OnecMonitor.Common.Models;
using System.Collections.Generic;
using TechLogSeance = OnecMonitor.Agent.Models.TechLogSeance;

namespace OnecMonitor.Agent
{
    public class AppDbContext : DbContext
    {
        public string DbPath { get; }

        public DbSet<AgentInstance> AgentInstance { get; set; }
        public DbSet<TechLogSeance> TechLogSeances { get; set; }

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
