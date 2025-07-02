using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using OnecMonitor.Server;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

await using var scope = app.Services.CreateAsyncScope();
var filesProvider = scope.ServiceProvider.GetRequiredService<FilesProvider>();
filesProvider.Init();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(filesProvider.DataFolder),
    RequestPath = "/Data"
});

//app.UseHttpsRedirection();

app.UseWebSockets();

app.UseResponseCompression();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<AgentConnectionsHub>("/agentsHub");
app.MapHub<MaintenanceTaskLogHub>("/taskLogHub");

var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await appDbContext.Database.MigrateAsync();

// Init techlog repository settings
var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
var techLogManager = scope.ServiceProvider.GetRequiredService<TechLogRepositoryManager>();
TechLogHelper.UpdateTechLogSettings(mapper, techLogManager, appDbContext);

await app.RunAsync();