using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Services;
using OneSwiss.Server;
using OneSwiss.Server.AutoMapper;
using OneSwiss.Server.Components;
using OneSwiss.Server.Helpers;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.AllowSynchronousIO = true;
    options.Limits.MaxRequestBodySize = long.MaxValue;
    
    // configure http listener
    var host = context.Configuration.GetValue("OnecMonitor:Http:Host", "0.0.0.0");
    var port = context.Configuration.GetValue("OnecMonitor:Http:Port", 7002);

    options.Listen(IPAddress.Parse(host), port);
});

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "OnecMonitor";
});
builder.Services.AddSystemd();

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        [ "application/octet-stream" ]);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddSingleton<FilesProvider>();

builder.Services.AddAutoMapper(typeof(DtoProfile).Assembly);

builder.Services.AddScoped<AdministrationApi>();

builder.Services.AddSingleton<TechLogRepositoryManager>();
builder.Services.AddSingleton<EventLogRepositoryManager>();

builder.Services.AddScoped<TechLogAnalyzer>();
builder.Services.AddDbContextFactory<AppDbContext>();

builder.Services.AddCors();

builder.Services.AddHostedService(sp => sp.GetRequiredService<AgentsConnectionsManager>());
builder.Services.AddSingleton<AgentsConnectionsManager>();
builder.Services.AddHostedService<ClustersInfoBasesDetector>();
builder.Services.AddHostedService<ErrorReportsCleaner>();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
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

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<AgentConnectionsHub>("/agentsHub");
app.MapHub<MaintenanceTaskLogHub>("/taskLogHub");

await using (var scope = app.Services.CreateAsyncScope())
{
    await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();
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
        .FirstOrDefault();
    
    if (techLogSettings != null)
        techLogRepositoryManager.UpdateSettings(mapper.Map<TechLogSettingsDto>(techLogSettings));
    
    var eventLogRepositoryManager = scope.ServiceProvider.GetRequiredService<EventLogRepositoryManager>();
    
    var eventLogSettings = db.EventLogSettings
        .Include(c => c.Credentials)
        .Include(c => c.Dbms)
        .FirstOrDefault();
    
    if (eventLogSettings != null)
        eventLogRepositoryManager.UpdateSettings(mapper.Map<EventLogSettingsDto>(eventLogSettings));
});

await app.RunAsync();