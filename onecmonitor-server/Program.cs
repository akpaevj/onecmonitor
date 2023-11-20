using System.Net;
using OnecMonitor.Server.Services;
using OnecMonitor.Server;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using OnecMonitor.Common.Storage;
using OnecMonitor.Common.TechLog;
using Grpc.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "OnecMonitor";
});
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
builder.Services.AddSingleton<ITechLogStorage, ClickHouseContext>();
builder.Services.AddHostedService((sp) => sp.GetRequiredService<TechLogProcessor>());
builder.Services.AddSingleton<TechLogProcessor>();
builder.Services.AddHostedService((sp) => sp.GetRequiredService<AgentsConnectionsManager>());
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

var clickHouseContext = scope.ServiceProvider.GetRequiredService<ITechLogStorage>();
await clickHouseContext.InitDatabase();

var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await appDbContext.Database.MigrateAsync();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TechLogSeances}/{action=Index}/{id?}");

app.Run();