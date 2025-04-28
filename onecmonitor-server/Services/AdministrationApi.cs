using OnecMonitor.Server.Dto;
using OnecMonitor.Server.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace OnecMonitor.Server.Services;

public class AdministrationApi : IDisposable
{
    private RestClient? _client = null!;
    
    public void Init(InfoBase infoBase)
    {
        var options = new RestClientOptions($"{infoBase.PublishAddress}/hs/administration")
        {
            ThrowOnAnyError = true,
            Authenticator = new HttpBasicAuthenticator(infoBase.Credentials!.User, infoBase.Credentials!.Password!)
        };

        _client = new RestClient(options);
    }

    public async Task<V8InfoBaseDto> Info(CancellationToken cancellationToken = default)
    {
        ThrowIfNotInitialized();
        return (await _client!.GetAsync<V8InfoBaseDto>("info", cancellationToken))!;
    }

    private void ThrowIfNotInitialized()
    {
        if (_client == null)
            throw new Exception("Клиент API не инициализирован");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}