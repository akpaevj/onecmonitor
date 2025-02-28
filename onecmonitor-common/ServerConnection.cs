using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using OnecMonitor.Common.DTO;

namespace OnecMonitor.Common;

public class ServerConnection(ILogger<ServerConnection> logger) : FastConnection(logger)
{
    private string _host = null!;
    private int _port;

    protected async Task Start(string host, int port, Func<Task> afterConnectCallback, CancellationToken token)
    {
        _host = host;
        _port = port;
        
        Disconnected += (_, _) =>
        {
            logger.LogWarning("Отключен от сервера");
            
            if (!token.IsCancellationRequested)
                _ = TryConnectInLoop(afterConnectCallback, token);
        };

        await TryConnectInLoop(afterConnectCallback, token);
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
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
            {
                logger.LogTrace("Ошибка установки соединения с сервером");
            }

            if (Socket?.Connected == true)
                break;
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    private async Task Reconnect(CancellationToken cancellationToken)
    {
        logger.LogTrace($"Попытка подключения к {_host}:{_port}");

        Socket?.Dispose();
        Socket = null;

        var addresses = await Dns.GetHostAddressesAsync(_host, AddressFamily.InterNetwork, cancellationToken);
        if (addresses.Length == 0)
            throw new Exception("Не удалось определить адрес сервера");
        var endPoint = new IPEndPoint(addresses[0], _port);

        logger.LogTrace($"Адрес сервера: {endPoint.Address}");

        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            await Socket.ConnectAsync(endPoint, cts.Token);
        }
        catch
        {
            // ignore
        }

        if (!Socket.Connected)
            throw new SocketException((int)SocketError.NotConnected);
    }
}