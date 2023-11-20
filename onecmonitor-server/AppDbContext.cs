using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using OnecMonitor.Server.Converters.Sqlite;
using OnecMonitor.Server.Models;
using OnecMonitor.Common.Models;

namespace OnecMonitor.Server
{
    public class AppDbContext : DbContext
    {
        public string DbPath { get; }

        public DbSet<Agent> Agents { get; set; }
        public DbSet<LogTemplate> LogTemplates { get; set; }
        public DbSet<TechLogSeance> TechLogSeances { get; set; }
        public DbSet<TechLogFilter> TechLogFilters { get; set; }

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
                .HaveConversion<DateTimeStringConverter>();
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
