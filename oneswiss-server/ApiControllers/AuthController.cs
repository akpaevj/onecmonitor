using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Логин и пароль обязательны");

        var normalizedUserName = request.UserName.Trim();
        var user = await userManager.FindByNameAsync(normalizedUserName);
        if (user == null)
            return Unauthorized("Неверный логин или пароль");

        var checkResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!checkResult.Succeeded)
            return Unauthorized("Неверный логин или пароль");

        var userRoles = await userManager.GetRolesAsync(user);
        var roles = ExpandRoles(userRoles).ToArray();

        var (accessToken, expiresAtUtc, refreshToken) = await IssueTokensAsync(user, roles);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new LoginResponse(
            accessToken,
            expiresAtUtc,
            refreshToken,
            new AuthUserDto(user.Id, user.UserName ?? string.Empty, user.DisplayName, roles)));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var userRoles = await userManager.GetRolesAsync(user);
        var roles = ExpandRoles(userRoles).ToArray();
        return Ok(new AuthUserDto(user.Id, user.UserName ?? string.Empty, user.DisplayName, roles));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            var tokenHash = HashToken(request.RefreshToken);
            var existing = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (existing is { RevokedAtUtc: null })
            {
                existing.RevokedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("external-providers")]
    public async Task<ActionResult<ExternalProvidersResponse>> ExternalProviders()
    {
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        var scheme = schemes.FirstOrDefault();

        return Ok(new ExternalProvidersResponse(scheme != null, scheme?.DisplayName ?? scheme?.Name));
    }

    [AllowAnonymous]
    [HttpGet("external-login")]
    public async Task<IActionResult> ExternalLogin([FromQuery] string? returnUrl)
    {
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        var scheme = schemes.FirstOrDefault();
        if (scheme == null)
            return NotFound();

        IEnumerable<KeyValuePair<string, StringValues>> query = [new("returnUrl", returnUrl)];
        var redirectUrl = UriHelper.BuildRelative(
            Request.PathBase,
            "/api/auth/external-login-callback",
            QueryString.Create(query));

        var properties = signInManager.ConfigureExternalAuthenticationProperties(scheme.Name, redirectUrl);
        return Challenge(properties, scheme.Name);
    }

    [AllowAnonymous]
    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string? returnUrl, CancellationToken cancellationToken)
    {
        var reactUrl = configuration.GetValue<string>("Ui:ReactUrl") ?? "http://localhost:3000";

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return Redirect($"{reactUrl}/login?error=external-login-failed");

        var nameClaim = configuration.GetValue<string>("Auth:OIDC:NameClaim") ?? "preferred_username";
        var nameValue = info.Principal.FindFirstValue(nameClaim);
        if (string.IsNullOrEmpty(nameValue))
            return Redirect($"{reactUrl}/login?error=external-login-failed");

        var user = await userManager.FindByLoginAsync(info.LoginProvider, nameValue);
        if (user == null || !await signInManager.CanSignInAsync(user))
            return Redirect($"{reactUrl}/login?error=invalid-user");

        var displayNameClaim = configuration.GetValue<string>("Auth:OIDC:DisplayNameClaim") ?? "name";
        var displayNameValue = info.Principal.FindFirstValue(displayNameClaim);
        if (!string.IsNullOrEmpty(displayNameValue) && user.DisplayName != displayNameValue)
        {
            user.DisplayName = displayNameValue;
            await userManager.UpdateAsync(user);
        }

        var userRoles = await userManager.GetRolesAsync(user);
        var roles = ExpandRoles(userRoles).ToArray();

        var (accessToken, expiresAtUtc, refreshToken) = await IssueTokensAsync(user, roles);
        await dbContext.SaveChangesAsync(cancellationToken);

        var fragment = $"token={Uri.EscapeDataString(accessToken)}&expiresAtUtc={Uri.EscapeDataString(expiresAtUtc.ToString("O"))}" +
            $"&refreshToken={Uri.EscapeDataString(refreshToken)}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
            fragment += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";

        return Redirect($"{reactUrl}/login/callback#{fragment}");
    }

    // Ротация refresh-токена: старый отзывается, выдаётся новая пара access+refresh.
    // Так пользователь не вылетает по истечении AccessTokenLifetimeMinutes, пока сессия активна.
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized();

        var tokenHash = HashToken(request.RefreshToken);
        var existing = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing == null || existing.RevokedAtUtc != null || existing.ExpiresAtUtc <= DateTime.UtcNow)
            return Unauthorized();

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user == null || !await signInManager.CanSignInAsync(user))
            return Unauthorized();

        existing.RevokedAtUtc = DateTime.UtcNow;

        var userRoles = await userManager.GetRolesAsync(user);
        var roles = ExpandRoles(userRoles).ToArray();

        var (accessToken, expiresAtUtc, refreshToken) = await IssueTokensAsync(user, roles);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new LoginResponse(
            accessToken,
            expiresAtUtc,
            refreshToken,
            new AuthUserDto(user.Id, user.UserName ?? string.Empty, user.DisplayName, roles)));
    }

    // Свой OAuth2-совместимый client_credentials-эндпоинт для агентов: позволяет требовать
    // аутентификацию агентов (Auth:RequireClientsAuthentication) без внешнего OIDC-провайдера,
    // выдавая JWT тем же Auth:Jwt:SigningKey, что и логин пользователей. Wire-совместим с
    // Duende.IdentityModel.Client (см. oneswiss-agent/Services/TokenRetriever.cs) - агенту менять
    // ничего не нужно, только указать Auth:TokensEndpoint на этот адрес.
    [AllowAnonymous]
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token(
        [FromForm(Name = "grant_type")] string? grantType,
        [FromForm(Name = "client_id")] string? formClientId,
        [FromForm(Name = "client_secret")] string? formClientSecret,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(grantType, "client_credentials", StringComparison.Ordinal))
            return BadRequest(new { error = "unsupported_grant_type" });

        var (clientId, clientSecret) = GetClientCredentials(formClientId, formClientSecret);
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || !Guid.TryParse(clientId, out var clientGuid))
            return BadRequest(new { error = "invalid_client" });

        var client = await dbContext.AgentClients.SingleOrDefaultAsync(c => c.Id == clientGuid, cancellationToken);
        if (client == null)
            return BadRequest(new { error = "invalid_client" });

        var verifyResult = new PasswordHasher<AgentClient>()
            .VerifyHashedPassword(client, client.ClientSecretHash, clientSecret);
        if (verifyResult == PasswordVerificationResult.Failed)
            return BadRequest(new { error = "invalid_client" });

        client.LastUsedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, client.Id.ToString()),
            new("client_id", client.Id.ToString()),
            new("token_use", "agent_client")
        };

        var lifetimeMinutes = GetAccessTokenLifetimeMinutes();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(lifetimeMinutes);
        var accessToken = BuildAccessToken(claims, expiresAtUtc);

        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = lifetimeMinutes * 60
        });
    }

    // Duende.IdentityModel.Client по умолчанию шлёт client_id/client_secret через заголовок
    // Authorization: Basic (RFC 6749 2.3.1) - это основной путь. Поля формы - fallback для
    // ручного тестирования (curl/Postman) и клиентов, не использующих Basic-заголовок.
    private (string? ClientId, string? ClientSecret) GetClientCredentials(string? formClientId, string? formClientSecret)
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader["Basic ".Length..]));
                var separatorIndex = decoded.IndexOf(':');
                if (separatorIndex >= 0)
                    return (
                        Uri.UnescapeDataString(decoded[..separatorIndex]),
                        Uri.UnescapeDataString(decoded[(separatorIndex + 1)..]));
            }
            catch (FormatException)
            {
                // Некорректный base64 в заголовке - пробуем поля формы ниже.
            }
        }

        return (formClientId, formClientSecret);
    }

    private static List<Claim> BuildUserClaims(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            claims.Add(new Claim("display_name", user.DisplayName));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return claims;
    }

    // Выдаёт access-токен и добавляет в контекст новую запись refresh-токена (SaveChanges - на
    // вызывающей стороне, чтобы можно было объединить с отзывом старого токена в одной транзакции).
    private async Task<(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken)> IssueTokensAsync(
        ApplicationUser user, IEnumerable<string> roles)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes());
        var accessToken = BuildAccessToken(BuildUserClaims(user, roles), expiresAtUtc);

        var rawRefreshToken = GenerateRefreshToken();
        await dbContext.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays())
        });

        return (accessToken, expiresAtUtc, rawRefreshToken);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private int GetRefreshTokenLifetimeDays()
    {
        var value = configuration.GetValue<int?>("Auth:Jwt:RefreshTokenLifetimeDays");
        return value is > 0 ? value.Value : 14;
    }

    private string BuildAccessToken(IEnumerable<Claim> claims, DateTime expiresAtUtc)
    {
        var jwtSection = configuration.GetSection("Auth").GetSection("Jwt");
        var signingKey = jwtSection.GetValue<string>("SigningKey");

        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException("Auth:Jwt:SigningKey is required for JWT authentication");

        var issuer = jwtSection.GetValue<string>("Issuer") ?? "OneSwiss";
        var audience = jwtSection.GetValue<string>("Audience") ?? "OneSwiss.Web";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private int GetAccessTokenLifetimeMinutes()
    {
        var value = configuration.GetValue<int?>("Auth:Jwt:AccessTokenLifetimeMinutes");
        return value is > 0 ? value.Value : 480;
    }

    private static IEnumerable<string> ExpandRoles(IEnumerable<string> roles)
    {
        var expanded = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in roles)
            expanded.Add(role);

        if (expanded.Contains(Roles.Administrator))
            foreach (var role in Roles.AllRoles.Select(c => c.Name))
                expanded.Add(role);

        return expanded;
    }

    public sealed record LoginRequest(string UserName, string Password);

    public sealed record AuthUserDto(Guid Id, string UserName, string? DisplayName, string[] Roles);

    public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, AuthUserDto User);

    public sealed record ExternalProvidersResponse(bool Available, string? ProviderName);

    public sealed record RefreshRequest(string RefreshToken);

    public sealed record LogoutRequest(string? RefreshToken);
}
