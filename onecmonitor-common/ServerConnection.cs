using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace OnecMonitor.Common;

public class ServerConnection : FastConnection
{
    private ILogger<ServerConnection> _logger = null!;
    private string _host = null!;
    private int _port;
    
    public event EventHandler? Connected;

    protected void Start(string host, int port, ILogger<ServerConnection> logger, CancellationToken token)
    {
        _logger = logger;
        _host = host;
        _port = port;
        
        Disconnected += (_, _) =>
        {
            logger.LogWarning("Отключен от сервера");
            
            if (!token.IsCancellationRequested)
                _ = TryConnectInLoop(token);
        };

        _ = TryConnectInLoop(token);
    }
        
    private async Task TryConnectInLoop(CancellationToken cancellationToken)
    {
        await ConnectInLoop(cancellationToken);

        RunStreamLoops(cancellationToken);

        _logger.LogTrace("Циклы потоков чтения/записи запущены");
    }

    private async Task ConnectInLoop(CancellationToken cancellationToken)
    {
        do
        {
            try
            {
                await Reconnect(cancellationToken);

                _logger.LogTrace("Установлено соединение с сервером");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
            {
                _logger.LogTrace("Ошибка установки соединения с сервером");
            }

            if (Socket?.Connected == true)
                break;
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    private async Task Reconnect(CancellationToken cancellationToken)
    {
        _logger.LogTrace($"Попытка подключения к {_host}:{_port}");

        Socket?.Dispose();

        var addresses = await Dns.GetHostAddressesAsync(_host, AddressFamily.InterNetwork, cancellationToken);
        if (addresses.Length == 0)
            throw new Exception("Не удалось определить адрес сервера");
        var endPoint = new IPEndPoint(addresses[0], _port);

        _logger.LogTrace($"Адрес сервера: {endPoint.Address}");

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
        
        _logger.LogInformation("Подключен к серверу");
            
        Connected?.Invoke(this, EventArgs.Empty);
    }
}