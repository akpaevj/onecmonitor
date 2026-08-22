using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;
using OneSwiss.Server.Models.MaintenanceTasks;
using File = OneSwiss.Server.Models.File;

namespace OneSwiss.Server;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly string _connectionString;

    public AppDbContext(DbContextOptions<AppDbContext> options,
        IConfiguration configuration) : base(options)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Строка подключения ConnectionStrings:Default не задана");
    }

    public DbSet<Agent> Agents { get; set; }
    public DbSet<AgentClient> AgentClients { get; set; }
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
    public DbSet<EventLogSettings> EventLogSettings { get; set; }
    public DbSet<EventLogExportItem> EventLogExportItems { get; set; }
    public DbSet<ErrorLoggingServiceSettings> ErrorLoggingServiceSettings { get; set; }
    public DbSet<ErrorReport> ErrorReports { get; set; }
    public DbSet<ConfigurationRepository> ConfigRepositories { get; set; }
    public DbSet<ConfigurationRepositoryUser> ConfigRepositoryUsers { get; set; }
    public DbSet<TelegramBotSettings> TelegramBotSettings { get; set; }
    public DbSet<NotificationRecipient> NotificationRecipients { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<CustomNotification> CustomNotifications { get; set; }
    public DbSet<AccessGroup> AccessGroups { get; set; }
    public DbSet<UsersGroup> UsersGroups { get; set; }
    public DbSet<GitRepository> GitRepositories { get; set; }
    public DbSet<CrServerProxySettings> CrServerProxySettings { get; set; }
    public DbSet<CrServerProxyMiddleware> CrServerProxyMiddlewares { get; set; }
    public DbSet<CrServerProxyLocation> CrServerProxyLocations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(_connectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseAsyncSeeding(SeedLogTemplates);
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