using Duende.IdentityModel.Client;

namespace OneSwiss.Agent.Services;

public class TokenRetriever(IConfiguration configuration, ILogger<TokenRetriever> logger)
{
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _locker = new(1);
    private string _cachedToken = string.Empty;
    private DateTime _tokenExpiry;

    private async Task<TokenResponse> GetAccessTokenAsync()
    {
        var tokensEndpoint = configuration["Auth:TokensEndpoint"];
        var clientId = configuration["Auth:ClientId"];
        var clientSecret = configuration["Auth:ClientSecret"];

        var client = new TokenClient(_httpClient, new TokenClientOptions
        {
            Address = tokensEndpoint!,
            ClientId = clientId!,
            ClientSecret = clientSecret
        });

        return await client.RequestTokenAsync("client_credentials");
    }

    public async Task<string?> GetValidTokenAsync()
    {
        await _locker.WaitAsync();

        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            var token = await GetAccessTokenAsync();
            _cachedToken = token.AccessToken!;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 5);

            return token.AccessToken!;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка получения токена аутентификации");
            throw;
        }
        finally
        {
            _locker.Release();
        }
    }
}