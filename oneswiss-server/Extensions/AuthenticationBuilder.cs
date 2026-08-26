using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Extensions;

public static class AuthenticationBuilder
{
    private const string AuthScheme = "OneSwissAuth";

    public static WebApplicationBuilder AddOneSwissAuthentication(this WebApplicationBuilder builder)
    {
        var authSection = builder.Configuration.GetSection("Auth");
        var oidcSection = authSection.GetSection("OIDC");
        var jwtSection = authSection.GetSection("Jwt");

        var authMode = authSection.GetValue("Mode", AuthMode.Internal);
        var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = AuthScheme;
                options.DefaultAuthenticateScheme = AuthScheme;
                options.DefaultChallengeScheme = AuthScheme;
            })
            .AddPolicyScheme(AuthScheme, displayName: null, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (!string.IsNullOrWhiteSpace(authHeader) &&
                        authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        return JwtBearerDefaults.AuthenticationScheme;

                    return IdentityConstants.ApplicationScheme;
                };
            });

        authBuilder.AddIdentityCookies();
        authBuilder.AddJwtBearer(opt =>
        {
            var signingKey = jwtSection.GetValue<string>("SigningKey");
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                opt.Authority = oidcSection.GetValue<string>("Authority");
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };

                return;
            }

            var issuer = jwtSection.GetValue<string>("Issuer") ?? "OneSwiss";
            var audience = jwtSection.GetValue<string>("Audience") ?? "OneSwiss.Web";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
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

                    var scopes = oidcSection.GetSection("Scopes").Get<string[]>();
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
