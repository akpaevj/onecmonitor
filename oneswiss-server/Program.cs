using System.Net;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Services;
using OneSwiss.Server;
using OneSwiss.Server.AutoMapper;
using OneSwiss.Server.Components;
using OneSwiss.Server.Components.Account;
using OneSwiss.Server.Components.Pages.MaintenanceTasks;
using OneSwiss.Server.Helpers;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Models;
using OneSwiss.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.AllowSynchronousIO = true;
    options.Limits.MaxRequestBodySize = long.MaxValue;
    
    // configure http listener
    var host = context.Configuration.GetValue("OneSwiss:Http:Host", "0.0.0.0");
    var port = context.Configuration.GetValue("OneSwiss:Http:Port", 7002);

    options.Listen(IPAddress.Parse(host), port);
});

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "OneSwiss";
});
builder.Services.AddSystemd();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        [ "application/octet-stream" ]);
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
        options.RootComponents.RegisterForJavaScript<StepWidget>(identifier: "StepWidget");
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AccessGroupsManager>();
builder.Services.AddScoped<UserGroupsManager>();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var authMode = builder.Configuration.GetValue("Auth:Mode", AuthMode.Internal);
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
});
authBuilder.AddIdentityCookies();

if (authMode != AuthMode.Internal)
{
    var oidcSection = builder.Configuration.GetSection("Auth:OIDC");
    
    authBuilder
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.Authority = oidcSection.GetValue<string>("Authority");
            options.ClientId = oidcSection.GetValue<string>("ClientId");
            options.ClientSecret = oidcSection.GetValue<string>("ClientSecret");
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            
            var scopes = oidcSection.GetValue<string[]>("Scopes");
            scopes?.ToList().ForEach(c => options.Scope.Add(c));
        });
}

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.AllowedUserNameCharacters =
            "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.Password = new PasswordOptions
        {
            RequireDigit = false,
            RequiredLength = 5,
            RequireNonAlphanumeric = false,
            RequiredUniqueChars = 1,
            RequireLowercase = false,
            RequireUppercase = false,
        };
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

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

builder.Services.AddHostedService(sp => sp.GetRequiredService<AgentsConnectionsManager>());
builder.Services.AddSingleton<AgentsConnectionsManager>();
builder.Services.AddHostedService<ClustersInfoBasesDetector>();
builder.Services.AddHostedService<ConfigurationRepositoriesDetector>();
builder.Services.AddHostedService<ErrorReportsCleaner>();

builder.Services.AddHostedService<UpdatesChecker>();
builder.Services.AddSingleton<UpdatesChecker.State>();
    
builder.Services.AddControllers();

var app = builder.Build();

app.UseForwardedHeaders();

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
    //await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();

    await SeedRoles(scope.ServiceProvider);
    await SeedAccessGroups(scope.ServiceProvider);
    await SeedUsersGroups(scope.ServiceProvider);
    await SeedUsers(scope.ServiceProvider);
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

return;

async Task SeedUsers(IServiceProvider serviceProvider)
{
    var manager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var groupsManager = serviceProvider.GetRequiredService<UserGroupsManager>();

    if (await manager.Users.AnyAsync())
        return;
    
    await manager.CreateAsync(new ApplicationUser
    {
        UserName = BuiltInDbData.AdminUser.User,
        DisplayName = BuiltInDbData.AdminUser.DisplayName
    }, BuiltInDbData.AdminUser.Password);
    var adminUser = manager.Users.First(c => c.UserName == BuiltInDbData.AdminUser.User);

    await groupsManager.AddUserToGroup(adminUser, BuiltInDbData.AdminsGroup.Id);
}

async Task SeedRoles(IServiceProvider serviceProvider)
{
    var manager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    foreach (var role in BuiltInRoles.Roles)
        if (!await manager.RoleExistsAsync(role.Name))
            await manager.CreateAsync(new ApplicationRole(role.Name, role.Description));
}

async Task SeedAccessGroups(IServiceProvider serviceProvider)
{
    var manager = serviceProvider.GetRequiredService<AccessGroupsManager>();
    var rolesManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    if (await manager.GroupsExists())
        return;

    await manager.Create(new AccessGroup
    {
        Id = BuiltInDbData.AdminsAccessGroup.Id,
        IsBuiltIn = true,
        Name = BuiltInDbData.AdminsAccessGroup.Name,
        Roles = [rolesManager.Roles.First(c => c.Name == "Administrator")]
    });
}

async Task SeedUsersGroups(IServiceProvider serviceProvider)
{
    var manager = serviceProvider.GetRequiredService<UserGroupsManager>();

    if (await manager.GroupsExists())
        return;

    var everyOneGroup = new UsersGroup
    {
        Id = BuiltInDbData.EveryoneGroup.Id,
        IsBuiltIn = true,
        Name = BuiltInDbData.EveryoneGroup.Name
    };
    await manager.Create(everyOneGroup);

    var adminsGroup = new UsersGroup
    {
        Id = BuiltInDbData.AdminsGroup.Id,
        IsBuiltIn = true,
        Name = BuiltInDbData.AdminsGroup.Name,
        ParentId = BuiltInDbData.EveryoneGroup.Id
    };
    await manager.Create(adminsGroup, [BuiltInDbData.AdminsAccessGroup.Id]);
}