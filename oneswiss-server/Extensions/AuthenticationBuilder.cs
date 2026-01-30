using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Extensions;

public static class AuthenticationBuilder
{
    public static WebApplicationBuilder AddOneSwissAuthentication(this WebApplicationBuilder builder)
    {
        var authSection = builder.Configuration.GetSection("Auth");
        var oidcSection = authSection.GetSection("OIDC");

        var authMode = authSection.GetValue("Mode", AuthMode.Internal);
        var authBuilder = builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
        });
        authBuilder.AddIdentityCookies();
        authBuilder.AddJwtBearer(opt =>
        {
            opt.Authority = oidcSection.GetValue<string>("Authority");
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true
            };
        });

        if (authMode != AuthMode.Internal)
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

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("AgentsAuthenticationPolicy", cfg =>
            {
                cfg.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);

                var requiresAuth = authSection.GetValue("RequireClientsAuthentication", false);
                cfg.RequireAssertion(handler =>
                {
                    var httpContext = handler.Resource as HttpContext;
                    return !requiresAuth || httpContext!.User.Identity!.IsAuthenticated;
                });
            });

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
                    RequireUppercase = false
                };
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return builder;
    }
}