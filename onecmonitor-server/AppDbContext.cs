using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OnecMonitor.Server.Converters.Sqlite;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;

namespace OnecMonitor.Server
{
    public class AppDbContext : DbContext
    {
        public string DbPath { get; }

        public DbSet<Agent> Agents { get; set; }
        public DbSet<LogTemplate> LogTemplates { get; set; }
        public DbSet<TechLogSeance> TechLogSeances { get; set; }
        public DbSet<TechLogFilter> TechLogFilters { get; set; }
        public DbSet<V8File> V8Files { get; set; }
        public DbSet<Credentials> Credentials { get; set; }
        public DbSet<InfoBase> InfoBases { get; set; }
        public DbSet<Cluster> Clusters { get; set; }
        public DbSet<Dbms> Dbms { get; set; }
        public DbSet<TechLogSettings> TechLogSettings { get; set; }
        public DbSet<MaintenanceTask> MaintenanceTasks { get; set; }
        public DbSet<MaintenanceStep> MaintenanceSteps { get; set; }
        public DbSet<MaintenanceStepLogItem> MaintenanceStepLogs { get; set; }
        public DbSet<EventLogSettings> EventLogSettings { get; set; }
        public DbSet<ErrorLoggingServiceSettings> ErrorLoggingServiceSettings { get; set; }
        public DbSet<ErrorReport> ErrorReports { get; set; }

        public AppDbContext(IHostEnvironment hostEnvironment)
            => DbPath = Path.Join(hostEnvironment.ContentRootPath, "om-server.db");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<Guid>()
                .HaveConversion<GuidStringConverter>();

            configurationBuilder.Properties<DateTime>()
                .HaveConversion<DateTimeToBinaryConverter>();
        }
        
        public static void ManyToMany<T>(List<T> newCollection, List<T> oldCollection)
        {
            newCollection
                .Except(oldCollection)
                .ToList()
                .ForEach(x => newCollection.Remove(x));

            oldCollection
                .Except(newCollection)
                .ToList()
                .ForEach(newCollection.Add);
        }

        public static void AddBuiltInLogTemplate(MigrationBuilder migrationBuilder, Guid id, string name, string content)
        {
            migrationBuilder.Sql(
                $"""
                INSERT INTO LogTemplates 
                VALUES (
                    '{id}', 
                    '{name}',
                    '{content}'
                    )
                """);
        }
    }
}
