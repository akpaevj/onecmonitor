using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using OnecMonitor.Server;
using OnecMonitor.Server.AutoMapper;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Hubs;
using OnecMonitor.Server.Services;
using OneSwiss.Common.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "OnecMonitor";
});
builder.Services.AddSystemd();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.AllowSynchronousIO = true;
    options.Limits.MaxRequestBodySize = long.MaxValue;
    
    // configure http listener
    var host = context.Configuration.GetValue("OnecMonitor:Http:Host", "0.0.0.0");
    var port = context.Configuration.GetValue("OnecMonitor:Http:Port", 7002);

    options.Listen(IPAddress.Parse(host), port, configure =>
    {
        configure.Protocols = HttpProtocols.Http1;
    });
});

builder.Services.AddSignalR(opt =>
{
    opt.EnableDetailedErrors = true;
    opt.MaximumReceiveMessageSize = 20 * 1024 * 1024;
});

builder.Services.AddAutoMapper(typeof(DtoProfile));
builder.Services.AddAutoMapper(typeof(CommonProfile));

builder.Services.AddScoped<AdministrationApi>();

builder.Services.AddSingleton<TechLogRepositoryManager>();
builder.Services.AddSingleton<EventLogRepositoryManager>();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<TechLogAnalyzer>();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddCors();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AgentsConnectionsManager>());
builder.Services.AddSingleton<AgentsConnectionsManager>();
builder.Services.AddHostedService<ClustersInfoBasesDetector>();
builder.Services.AddHostedService<ErrorReportsCleaner>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseDefaultFiles();

app.UseStaticFiles();

var dataPath = Path.Combine(builder.Environment.ContentRootPath, "Data");
if (!Path.Exists(dataPath))
    Directory.CreateDirectory(dataPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(dataPath),
    RequestPath = "/Data"
});

app.UseRouting();
app.UseWebSockets();

app.UseCors(options =>
{
    options.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
           .Build();
});

await using var scope = app.Services.CreateAsyncScope();

var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await appDbContext.Database.MigrateAsync();

// Init techlog repository settings
var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
var techLogManager = scope.ServiceProvider.GetRequiredService<TechLogRepositoryManager>();
TechLogHelper.UpdateTechLogSettings(mapper, techLogManager, appDbContext);

app.MapHub<MaintenanceTaskLogsHub>("/MaintenanceTaskLogs");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Agents}/{action=Index}/{id?}");

app.Run();