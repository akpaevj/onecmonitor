using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

        var result = await TryRotateRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (!result.Success)
            return Unauthorized();

        return Ok(new LoginResponse(
            result.AccessToken,
            result.ExpiresAtUtc,
            result.RefreshToken,
            new AuthUserDto(result.User!.Id, result.User.UserName ?? string.Empty, result.User.DisplayName, result.Roles!)));
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
        [FromForm(Name = "code")] string? code,
        [FromForm(Name = "redirect_uri")] string? redirectUri,
        [FromForm(Name = "code_verifier")] string? codeVerifier,
        [FromForm(Name = "refresh_token")] string? oauthRefreshToken,
        CancellationToken cancellationToken)
    {
        return grantType switch
        {
            "client_credentials" => await HandleClientCredentialsGrant(formClientId, formClientSecret, cancellationToken),
            "authorization_code" when IsMcpEnabled() =>
                await HandleAuthorizationCodeGrant(formClientId, code, redirectUri, codeVerifier, cancellationToken),
            "refresh_token" when IsMcpEnabled() => await HandleOAuthRefreshTokenGrant(oauthRefreshToken, cancellationToken),
            _ => BadRequest(new { error = "unsupported_grant_type" })
        };
    }

    private bool IsMcpEnabled() => configuration.GetValue("Mcp:Enabled", false);

    private async Task<IActionResult> HandleClientCredentialsGrant(
        string? formClientId, string? formClientSecret, CancellationToken cancellationToken)
    {
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

    private async Task<IActionResult> HandleAuthorizationCodeGrant(
        string? clientId, string? code, string? redirectUri, string? codeVerifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(code) ||
            string.IsNullOrEmpty(redirectUri) || string.IsNullOrEmpty(codeVerifier))
            return BadRequest(new { error = "invalid_request" });

        if (!Guid.TryParse(clientId, out var clientGuid))
            return BadRequest(new { error = "invalid_client" });

        var codeHash = HashToken(code);
        var authCode = await dbContext.OAuthAuthorizationCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.CodeHash == codeHash, cancellationToken);

        if (authCode == null || authCode.UsedAtUtc != null || authCode.ExpiresAtUtc <= DateTime.UtcNow ||
            authCode.ClientId != clientGuid || !string.Equals(authCode.RedirectUri, redirectUri, StringComparison.Ordinal))
            return BadRequest(new { error = "invalid_grant" });

        if (!VerifyCodeChallenge(authCode.CodeChallenge, authCode.CodeChallengeMethod, codeVerifier))
            return BadRequest(new { error = "invalid_grant" });

        var user = await userManager.FindByIdAsync(authCode.UserId.ToString());
        if (user == null || !await signInManager.CanSignInAsync(user))
            return BadRequest(new { error = "invalid_grant" });

        var userRoles = await userManager.GetRolesAsync(user);
        var roles = ExpandRoles(userRoles).ToArray();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var claimed = await dbContext.OAuthAuthorizationCodes
                .Where(c => c.CodeHash == codeHash && c.UsedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAtUtc, DateTime.UtcNow), cancellationToken);

            if (claimed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RefreshRotationResult.Failed;
            }

            var (accessToken, expiresAtUtc, refreshToken) = await IssueTokensAsync(user, roles, clientGuid);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RefreshRotationResult(true, user, roles, accessToken, expiresAtUtc, refreshToken);
        });

        if (!result.Success)
            return BadRequest(new { error = "invalid_grant" });

        return Ok(new
        {
            access_token = result.AccessToken,
            token_type = "Bearer",
            expires_in = (int)Math.Max(0, (result.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds),
            refresh_token = result.RefreshToken
        });
    }

    private async Task<IActionResult> HandleOAuthRefreshTokenGrant(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { error = "invalid_request" });

        var result = await TryRotateRefreshTokenAsync(refreshToken, cancellationToken);
        if (!result.Success)
            return BadRequest(new { error = "invalid_grant" });

        return Ok(new
        {
            access_token = result.AccessToken,
            token_type = "Bearer",
            expires_in = (int)Math.Max(0, (result.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds),
            refresh_token = result.RefreshToken
        });
    }

    private async Task<RefreshRotationResult> TryRotateRefreshTokenAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var existing = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing == null || existing.RevokedAtUtc != null || existing.ExpiresAtUtc <= DateTime.UtcNow)
            return RefreshRotationResult.Failed;

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user == null || !await signInManager.CanSignInAsync(user))
            return RefreshRotationResult.Failed;

        var userRoles = await userManager.GetRolesAsync(user);
        var roles = ExpandRoles(userRoles).ToArray();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var claimed = await dbContext.RefreshTokens
                .Where(t => t.TokenHash == tokenHash && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow), cancellationToken);

            if (claimed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RefreshRotationResult.Failed;
            }

            var (accessToken, expiresAtUtc, newRefreshToken) = await IssueTokensAsync(user, roles, existing.OAuthClientId);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RefreshRotationResult(true, user, roles, accessToken, expiresAtUtc, newRefreshToken);
        });
    }

    private sealed record RefreshRotationResult(
        bool Success,
        ApplicationUser? User,
        string[]? Roles,
        string AccessToken = "",
        DateTime ExpiresAtUtc = default,
        string RefreshToken = "")
    {
        public static readonly RefreshRotationResult Failed = new(false, null, null);
    }

    private static bool VerifyCodeChallenge(string codeChallenge, string codeChallengeMethod, string codeVerifier)
    {
        if (!string.Equals(codeChallengeMethod, "S256", StringComparison.OrdinalIgnoreCase))
            return false;

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var computedChallenge = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return string.Equals(computedChallenge, codeChallenge, StringComparison.Ordinal);
    }

    [AllowAnonymous]
    [HttpGet("/.well-known/oauth-protected-resource")]
    [HttpGet("/.well-known/oauth-protected-resource/{*path}")]
    public IActionResult ProtectedResourceMetadata()
    {
        if (!IsMcpEnabled())
            return NotFound();

        var baseUrl = GetIssuerBaseUrl();

        return Ok(new
        {
            resource = $"{baseUrl}/mcp",
            authorization_servers = new[] { baseUrl }
        });
    }

    [AllowAnonymous]
    [HttpGet("/.well-known/oauth-authorization-server")]
    public IActionResult AuthorizationServerMetadata()
    {
        if (!IsMcpEnabled())
            return NotFound();

        var baseUrl = GetIssuerBaseUrl();

        return Ok(new
        {
            issuer = baseUrl,
            authorization_endpoint = $"{baseUrl}/oauth/authorize",
            token_endpoint = $"{baseUrl}/api/auth/token",
            registration_endpoint = $"{baseUrl}/oauth/register",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            code_challenge_methods_supported = new[] { "S256" },
            token_endpoint_auth_methods_supported = new[] { "none" }
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("oauth-register")]
    [HttpPost("/oauth/register")]
    public async Task<IActionResult> RegisterClient([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        if (!IsMcpEnabled())
            return NotFound();

        if (!payload.TryGetProperty("redirect_uris", out var redirectUrisElement) ||
            redirectUrisElement.ValueKind != JsonValueKind.Array || redirectUrisElement.GetArrayLength() == 0)
            return BadRequest(new { error = "invalid_redirect_uri" });

        var redirectUris = new List<string>();
        foreach (var item in redirectUrisElement.EnumerateArray())
        {
            var value = item.GetString();
            if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                !IsAllowedRedirectUri(uri))
                return BadRequest(new { error = "invalid_redirect_uri" });

            redirectUris.Add(value);
        }

        string? clientName = null;
        if (payload.TryGetProperty("client_name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
            clientName = nameElement.GetString();

        var client = new OAuthClient
        {
            ClientName = string.IsNullOrWhiteSpace(clientName) ? null : clientName.Trim(),
            RedirectUrisJson = JsonSerializer.Serialize(redirectUris),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.OAuthClients.Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            client_id = client.Id.ToString(),
            client_id_issued_at = ((DateTimeOffset)client.CreatedAtUtc).ToUnixTimeSeconds(),
            redirect_uris = redirectUris,
            token_endpoint_auth_method = "none",
            grant_types = new[] { "authorization_code", "refresh_token" },
            response_types = new[] { "code" }
        });
    }

    private static bool IsAllowedRedirectUri(Uri uri)
    {
        if (uri.Scheme == Uri.UriSchemeHttps)
            return true;

        return uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
    }

    [AllowAnonymous]
    [HttpGet("/oauth/authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery(Name = "response_type")] string? responseType,
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "redirect_uri")] string? redirectUri,
        [FromQuery(Name = "state")] string? state,
        [FromQuery(Name = "code_challenge")] string? codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod,
        [FromQuery(Name = "scope")] string? scope,
        CancellationToken cancellationToken)
    {
        if (!IsMcpEnabled())
            return NotFound();

        if (!string.Equals(responseType, "code", StringComparison.Ordinal) ||
            string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri) || string.IsNullOrEmpty(codeChallenge))
            return BadRequest("Некорректный запрос авторизации");

        if (!Guid.TryParse(clientId, out var clientGuid))
            return BadRequest("Неизвестный клиент");

        var client = await dbContext.OAuthClients.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clientGuid, cancellationToken);
        if (client == null)
            return BadRequest("Неизвестный клиент");

        var redirectUris = JsonSerializer.Deserialize<List<string>>(client.RedirectUrisJson) ?? [];
        if (!redirectUris.Contains(redirectUri, StringComparer.Ordinal))
            return BadRequest("redirect_uri не зарегистрирован для этого клиента");

        var reactUrl = configuration.GetValue<string>("Ui:ReactUrl") ?? "http://localhost:3000";

        var queryParams = new List<string>
        {
            $"client_id={Uri.EscapeDataString(clientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
            "response_type=code",
            $"code_challenge={Uri.EscapeDataString(codeChallenge)}",
            $"code_challenge_method={Uri.EscapeDataString(string.IsNullOrEmpty(codeChallengeMethod) ? "S256" : codeChallengeMethod)}"
        };
        if (!string.IsNullOrEmpty(state))
            queryParams.Add($"state={Uri.EscapeDataString(state)}");
        if (!string.IsNullOrEmpty(scope))
            queryParams.Add($"scope={Uri.EscapeDataString(scope)}");
        if (!string.IsNullOrEmpty(client.ClientName))
            queryParams.Add($"client_name={Uri.EscapeDataString(client.ClientName)}");

        return Redirect($"{reactUrl}/oauth/authorize?{string.Join("&", queryParams)}");
    }

    [Authorize]
    [HttpPost("oauth/authorize")]
    public async Task<ActionResult<OAuthAuthorizeCompleteResponse>> CompleteOAuthAuthorize(
        [FromBody] OAuthAuthorizeCompleteRequest request, CancellationToken cancellationToken)
    {
        if (!IsMcpEnabled())
            return NotFound();

        if (!Guid.TryParse(request.ClientId, out var clientGuid))
            return BadRequest("Неизвестный клиент");

        var client = await dbContext.OAuthClients.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clientGuid, cancellationToken);
        if (client == null)
            return BadRequest("Неизвестный клиент");

        var redirectUris = JsonSerializer.Deserialize<List<string>>(client.RedirectUrisJson) ?? [];
        if (!redirectUris.Contains(request.RedirectUri, StringComparer.Ordinal))
            return BadRequest("redirect_uri не зарегистрирован для этого клиента");

        if (request.Deny)
        {
            var denySeparator = request.RedirectUri.Contains('?') ? '&' : '?';
            var denyUrl = $"{request.RedirectUri}{denySeparator}error=access_denied";
            if (!string.IsNullOrEmpty(request.State))
                denyUrl += $"&state={Uri.EscapeDataString(request.State)}";

            return Ok(new OAuthAuthorizeCompleteResponse(denyUrl));
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var rawCode = GenerateRefreshToken();

        dbContext.OAuthAuthorizationCodes.Add(new OAuthAuthorizationCode
        {
            CodeHash = HashToken(rawCode),
            ClientId = clientGuid,
            UserId = userId,
            RedirectUri = request.RedirectUri,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = string.IsNullOrEmpty(request.CodeChallengeMethod) ? "S256" : request.CodeChallengeMethod,
            Scope = request.Scope,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var separator = request.RedirectUri.Contains('?') ? '&' : '?';
        var redirectUrl = $"{request.RedirectUri}{separator}code={Uri.EscapeDataString(rawCode)}";
        if (!string.IsNullOrEmpty(request.State))
            redirectUrl += $"&state={Uri.EscapeDataString(request.State)}";

        return Ok(new OAuthAuthorizeCompleteResponse(redirectUrl));
    }

    private string GetIssuerBaseUrl() => $"{Request.Scheme}://{Request.Host}";

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

    private async Task<(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken)> IssueTokensAsync(
        ApplicationUser user, IEnumerable<string> roles, Guid? oauthClientId = null)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes());
        var accessToken = BuildAccessToken(BuildUserClaims(user, roles), expiresAtUtc);

        var rawRefreshToken = GenerateRefreshToken();
        await dbContext.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays()),
            OAuthClientId = oauthClientId
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

    public sealed record OAuthAuthorizeCompleteRequest(
        string ClientId,
        string RedirectUri,
        string CodeChallenge,
        string? CodeChallengeMethod,
        string? Scope,
        string? State,
        bool Deny = false);

    public sealed record OAuthAuthorizeCompleteResponse(string RedirectUrl);
}
