using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Console;
using MudBlazor.Services;
using MudExtensions.Services;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Services;
using OneSwiss.Server;
using OneSwiss.Server.ApiControllers;
using OneSwiss.Server.AutoMapper;
using OneSwiss.Server.Components;
using OneSwiss.Server.Components.Account;
using OneSwiss.Server.Components.Pages.MaintenanceTasks;
using OneSwiss.Server.Extensions;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Services;
using OneSwiss.Server.Services.CrServerProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(cfg =>
{
    cfg.ColorBehavior = LoggerColorBehavior.Enabled;
    cfg.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.AllowSynchronousIO = true;
    options.Limits.MaxRequestBodySize = long.MaxValue;
    options.Limits.MaxResponseBufferSize = long.MaxValue;
    options.Limits.MaxRequestBufferSize = long.MaxValue;

    // configure http listener
    var host = context.Configuration.GetValue<string>("Http:Host");
    var port = context.Configuration.GetValue("Http:Port", 7002);

    options.Listen(string.IsNullOrEmpty(host) ? IPAddress.Any : IPAddress.Parse(host), port);
});

builder.AddOneSwissAuthentication();

builder.Services.AddWindowsService(options => { options.ServiceName = "OneSwiss"; });
builder.Services.AddSystemd();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    var proxyAddress = builder.Configuration.GetValue("ProxyAddress", "");
    if (!string.IsNullOrEmpty(proxyAddress))
        options.KnownProxies.Add(IPAddress.Parse(proxyAddress));

    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.RootComponents.RegisterForJavaScript<StepWidget>("StepWidget");
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AccessGroupsManager>();
builder.Services.AddScoped<UserGroupsManager>();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

builder.Services.AddSingleton<NotificationsService>();
builder.Services.AddHostedService<NotificationsProcessor>();

builder.Services.AddSingleton<FilesProvider>();

builder.Services.AddAutoMapper(_ => { }, typeof(DtoProfile).Assembly);

builder.Services.AddSingleton<TechLogRepositoryManager>();
builder.Services.AddSingleton<EventLogRepositoryManager>();

builder.Services.AddScoped<TechLogAnalyzer>();
builder.Services.AddDbContextFactory<AppDbContext>();

builder.Services.AddCors();

builder.Services.AddSingleton<AgentsConnectionsManager>();
builder.Services.AddHostedService<ClustersInfoBasesDetector>();
builder.Services.AddHostedService<ConfigurationRepositoriesDetector>();
builder.Services.AddHostedService<ErrorReportsCleaner>();

builder.Services.AddSingleton<InterAgencyCommunicationService>();

builder.Services.AddHostedService<UpdatesChecker>();
builder.Services.AddSingleton<UpdatesChecker.State>();

builder.Services.AddHostedService<CrServerConnectionsDisconnecter>();
builder.Services.AddSingleton<CrServerConnectionsPool>();
builder.Services.AddSingleton<CrServerRequestsHandler>();

builder.Services.AddSingleton<MonitorQueue<(Guid RepoId, int Version)>>();
builder.Services.AddHostedService<NewConfigRepositoryVersionHandler>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var filesProvider = app.Services.GetRequiredService<FilesProvider>();
filesProvider.Init();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(filesProvider.DataFolder),
    RequestPath = "/Data"
});

app.UseWebSockets();
app.UseResponseCompression();
app.UseAntiforgery();
app.MapStaticAssets();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.MapHub<AgentConnectionsHub>("/agentsHub");
app.MapHub<MaintenanceTaskLogHub>("/taskLogHub");
app.MapHub<UpdatesCheckingHub>("/updatesHub");

await using (var scope = app.Services.CreateAsyncScope())
{
    await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.SeedBuiltInData();
}

app.Lifetime.ApplicationStarted.Register(() =>
{
    // Инициализириуем службы при старте приложения
    using var scope = app.Services.CreateScope();

    var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
    using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var techLogRepositoryManager = scope.ServiceProvider.GetRequiredService<TechLogRepositoryManager>();

    var techLogSettings = db.TechLogSettings
        .Include(c => c.Credentials)
        .Include(c => c.Dbms)
        .SingleOrDefault();

    if (techLogSettings != null)
        techLogRepositoryManager.UpdateSettings(mapper.Map<TechLogSettingsDto>(techLogSettings));

    var eventLogRepositoryManager = scope.ServiceProvider.GetRequiredService<EventLogRepositoryManager>();

    var eventLogSettings = db.EventLogSettings
        .Include(c => c.Credentials)
        .Include(c => c.Dbms)
        .SingleOrDefault();

    if (eventLogSettings != null)
        eventLogRepositoryManager.UpdateSettings(mapper.Map<EventLogSettingsDto>(eventLogSettings));
});

await app.RunAsync();