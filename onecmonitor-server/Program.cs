using System.Net;
using AutoMapper;
using OnecMonitor.Server.Services;
using OnecMonitor.Server;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using OnecMonitor.Common.Storage;
using OnecMonitor.Common.TechLog;
using Grpc.Core;
using Microsoft.Extensions.FileProviders;
using OnecMonitor.Server.AutoMapper;
using OnecMonitor.Server.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "OnecMonitor";
});
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Limits.MaxRequestBodySize = 2000 * 1024 * 1024;
    
    // configure http listener
    var host = context.Configuration.GetValue("OnecMonitor:Http:Host", "0.0.0.0")!;
    var port = context.Configuration.GetValue("OnecMonitor:Http:Port", 7002);

    options.Listen(IPAddress.Parse(host), port, options =>
    {
        options.Protocols = HttpProtocols.Http1;
    });
});

builder.Services.AddSignalR();

builder.Services.AddAutoMapper(typeof(DtoProfile));
builder.Services.AddAutoMapper(typeof(CommonProfile));

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

var dataPath = Path.Combine(builder.Environment.ContentRootPath, "Data");
if (!Path.Exists(dataPath))
    Directory.CreateDirectory(dataPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(dataPath),
    RequestPath = "/Data"
});

app.UseRouting();

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

var settings = await appDbContext.TechLogSettings.FirstOrDefaultAsync();

if (settings?.Enabled ?? false)
{
    var clickHouseContext = scope.ServiceProvider.GetRequiredService<ITechLogStorage>();
    await clickHouseContext.InitDatabase();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Agents}/{action=Index}/{id?}");

app.Run();