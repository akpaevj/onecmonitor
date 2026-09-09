using System.Net;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AutoMapper;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Console;
using ModelContextProtocol.AspNetCore;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Services;
using OneSwiss.Server;
using OneSwiss.Server.ApiControllers;
using OneSwiss.Server.AutoMapper;
using OneSwiss.Server.Extensions;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Mcp;
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

// По умолчанию ASP.NET Core ограничивает multipart-запросы (загрузку файлов) 128 МБ
// независимо от Kestrel MaxRequestBodySize выше - конфигурации 1С могут весить гигабайты.
builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = long.MaxValue; });

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

builder.Services.AddScoped<AccessGroupsManager>();
builder.Services.AddScoped<UserGroupsManager>();

builder.Services.AddSingleton<NotificationsService>();
builder.Services.AddHostedService<NotificationsProcessor>();

builder.Services.AddSingleton<FilesProvider>();

builder.Services.AddAutoMapper(_ => { }, typeof(DtoProfile).Assembly);

builder.Services.AddSingleton<TechLogRepositoryManager>();

builder.Services.AddScoped<TechLogAnalyzer>();
builder.Services.AddDbContextFactory<AppDbContext>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactSpa", policy =>
    {
        var reactUrl = builder.Configuration.GetValue<string>("Ui:ReactUrl");

        if (!string.IsNullOrWhiteSpace(reactUrl))
        {
            policy.WithOrigins(reactUrl)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();

            return;
        }

        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true);
    });
});

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

builder.Services.AddHostedService<EventLogExportConnector>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("oauth-register", context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

var mcpEnabled = builder.Configuration.GetValue("Mcp:Enabled", false);

if (mcpEnabled)
{
    builder.Services.AddMcpServer()
        .WithHttpTransport(options => { options.SessionMode = HttpServerSessionMode.Stateful; })
        .AddAuthorizationFilters()
        .WithTools<ErrorLoggingMcpTools>()
        .WithTools<MaintenanceTasksMcpTools>()
        .WithTools<SessionsMcpTools>();
}

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRateLimiter();

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
app.UseCors("ReactSpa");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (mcpEnabled)
    app.MapMcp("/mcp").RequireAuthorization();

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
});

await app.RunAsync();