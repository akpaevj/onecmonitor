using System.Net;
using OnecMonitor.Server.Services;
using OnecMonitor.Server;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) =>
{
    // configure http listener
    var host = context.Configuration.GetValue("OnecMonitor:Http:Host", "0.0.0.0")!;
    var port = context.Configuration.GetValue("OnecMonitor:Http:Port", 7002);

    options.Listen(IPAddress.Parse(host), port, options =>
    {
        options.Protocols = HttpProtocols.Http1;
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<TechLogAnalyzer>();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddCors();
builder.Services.AddScoped<IClickHouseContext>(sp =>
{
    var host = builder.Configuration.GetValue("ClickHouse:Host", "127.0.0.1");
    var port = builder.Configuration.GetValue("ClickHouse:Port", 9100);

    var channel = GrpcChannel.ForAddress($"http://{host}:{port}", new GrpcChannelOptions()
    {
        // used to avoid performance issues with too many requests per connection
        HttpHandler = new SocketsHttpHandler()
        {
            EnableMultipleHttp2Connections = true
        },
        MaxReceiveMessageSize = 128 * 1024 * 1024
    });

    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ClickHouseContext>>();

    return new ClickHouseContext(channel, configuration, logger);
});
builder.Services.AddSingleton<TechLogProcessor>();
builder.Services.AddSingleton<AgentsConnectionsManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseCors(options =>
{
    options.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
           .Build();
});

using var scope = app.Services.CreateAsyncScope();

var clickHouseContext = scope.ServiceProvider.GetRequiredService<IClickHouseContext>();
await clickHouseContext.InitDatabase();

var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await appDbContext.Database.MigrateAsync();

var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var connectionsManager = app.Services.GetRequiredService<AgentsConnectionsManager>();
appLifetime.ApplicationStarted.Register(() =>
{
    _ = connectionsManager.Start(appLifetime.ApplicationStopping);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TechLogSeances}/{action=Index}/{id?}");

app.Run();