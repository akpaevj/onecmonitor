using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OneSwiss.Common.Converters.Sqlite;
using OneSwiss.Server.Models;
using OneSwiss.Server.Models.MaintenanceTasks;
using File = OneSwiss.Server.Models.File;

namespace OneSwiss.Server
{
    public class AppDbContext : DbContext
    {
        public string DbPath { get; private set; } = null!;

        public DbSet<Agent> Agents { get; set; }
        public DbSet<LogTemplate> LogTemplates { get; set; }
        public DbSet<TechLogSeance> TechLogSeances { get; set; }
        public DbSet<TechLogFilter> TechLogFilters { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<Credentials> Credentials { get; set; }
        public DbSet<InfoBase> InfoBases { get; set; }
        public DbSet<Cluster> Clusters { get; set; }
        public DbSet<Dbms> Dbms { get; set; }
        public DbSet<TechLogSettings> TechLogSettings { get; set; }
        public DbSet<MaintenanceTask> MaintenanceTasks { get; set; }
        public DbSet<MaintenanceTaskLogItem> MaintenanceTaskLogs { get; set; }
        public DbSet<MaintenanceStep> MaintenanceSteps { get; set; }
        public DbSet<LockConnectionsStep> LockConnectionsSteps { get; set; }
        public DbSet<ExecuteOneScriptStep> ExecuteOneScriptSteps { get; set; }
        public DbSet<StartExternalDataProcessorStep> StartExternalDataProcessorSteps { get; set; }
        public DbSet<UpdateConfigurationStep> UpdateConfigurationSteps { get; set; }
        public DbSet<LoadConfigurationStep> LoadConfigurationSteps { get; set; }
        public DbSet<LoadExtensionStep> LoadExtensionSteps { get; set; }
        public DbSet<DeleteExtensionStep> DeleteExtensionSteps { get; set; }
        public DbSet<CopyInfoBaseStep> CopyInfoBaseSteps { get; set; }
        public DbSet<EventLogSettings> EventLogSettings { get; set; }
        public DbSet<ErrorLoggingServiceSettings> ErrorLoggingServiceSettings { get; set; }
        public DbSet<ErrorReport> ErrorReports { get; set; }
        public DbSet<ConfigurationRepository> ConfigRepositories { get; set; }
        public DbSet<ConfigurationRepositoryUser> ConfigRepositoryUsers { get; set; }
        public DbSet<TelegramBotSettings> TelegramBotSettings { get; set; }
        public DbSet<NotificationRecipient> NotificationRecipients { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CustomNotification> CustomNotifications { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options, IHostEnvironment hostEnvironment) : base(options)
            => SetDbPath(hostEnvironment);
        
        private void SetDbPath(IHostEnvironment hostEnvironment)
            => DbPath = Path.Join(hostEnvironment.ContentRootPath, "om-server.db");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseSqlite($"Data Source={DbPath}")
                .UseAsyncSeeding(SeedLogTemplates);

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<Guid>()
                .HaveConversion<GuidStringConverter>();

            configurationBuilder.Properties<DateTime>()
                .HaveConversion<DateTimeToBinaryConverter>();
        }

        private static async Task SeedLogTemplates(DbContext context, bool seed, CancellationToken cancellationToken)
        {
            foreach (var templateDef in BuiltInDbData.LogTemplates)
            {
                var id = Guid.Parse(templateDef.Id);
                var item = await context.Set<LogTemplate>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                if (item == null)
                    await context.Set<LogTemplate>().AddAsync(new LogTemplate
                    {
                        Id = id,
                        Content = templateDef.Content,
                        Name = templateDef.Name
                    }, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}