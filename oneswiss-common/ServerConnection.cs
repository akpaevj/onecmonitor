using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace OneSwiss.Common;

public class ServerConnection(ILogger<ServerConnection> logger) : FastConnection(logger)
{
    private string _serverAddress = null!;
    private string? _bearerToken;

    
    protected async Task Start(string address, string? bearerToken, Func<Task> afterConnectCallback, CancellationToken cancellationToken)
    {
        _serverAddress = address;
        _bearerToken = bearerToken;
        
        Disconnected += (_, _) =>
        {
            logger.LogWarning("Отключен от сервера");
            
            if (!cancellationToken.IsCancellationRequested)
                _ = TryConnectInLoop(afterConnectCallback, cancellationToken);
        };

        await TryConnectInLoop(afterConnectCallback, cancellationToken);
    }
        
    private async Task TryConnectInLoop(Func<Task> afterConnectCallback, CancellationToken cancellationToken)
    {
        await ConnectInLoop(afterConnectCallback, cancellationToken);
        
        RunStreamLoops(cancellationToken);

        logger.LogTrace("Циклы потоков чтения/записи запущены");
    }

    private async Task ConnectInLoop(Func<Task> afterConnectCallback, CancellationToken cancellationToken)
    {
        do
        {
            try
            {
                await Reconnect(cancellationToken);

                logger.LogTrace("Установлено соединение с сервером");

                await afterConnectCallback();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка подключения к серверу. Следующая попытка подключения будет выполнена через 10 сек");
                await Task.Delay(10 * 1000, cancellationToken);
            }

            if (Socket?.State == WebSocketState.Open)
                break;
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    private async Task Reconnect(CancellationToken cancellationToken)
    {
        logger.LogTrace($"Попытка подключения к {_serverAddress}");

        Socket?.Dispose();
        Socket = null;
        
        var uri = new Uri($"{_serverAddress}/ws/agents");

        logger.LogTrace($"Адрес сервера: {uri}");
        
        var s = new ClientWebSocket();
        if (_bearerToken != null)
            s.Options.SetRequestHeader("Authorization", $"Bearer {_bearerToken}");
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        await s.ConnectAsync(uri, cts.Token);
        Socket = s;
    }
}