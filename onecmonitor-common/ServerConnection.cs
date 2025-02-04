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
            logger.LogWarning("Disconnected from the server");
            _ = TryConnectInLoop(token);
        };

        _ = TryConnectInLoop(token);
    }
        
    private async Task TryConnectInLoop(CancellationToken cancellationToken)
    {
        await ConnectInLoop(cancellationToken);

        RunStreamLoops();

        _logger.LogTrace("Stream loops started");
    }

    private async Task ConnectInLoop(CancellationToken cancellationToken)
    {
        do
        {
            try
            {
                await Reconnect(cancellationToken);

                _logger.LogTrace("Connection to server is established");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
            {
                _logger.LogTrace("Failed to connect to the server");
            }

            if (Socket?.Connected == true)
                break;
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    private async Task Reconnect(CancellationToken cancellationToken)
    {
        _logger.LogTrace($"Trying connect to {_host}:{_port}");

        Socket?.Dispose();

        var addresses = await Dns.GetHostAddressesAsync(_host, AddressFamily.InterNetwork, cancellationToken);
        if (addresses.Length == 0)
            throw new Exception("Couldn't resolve server address");
        var endPoint = new IPEndPoint(addresses[0], _port);

        _logger.LogTrace($"Server's resolved address: {endPoint.Address}");

        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
        };

        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(10 * 1000);
            cancellationToken.Register(cts.Cancel);

            var connectAsync = Socket.ConnectAsync(endPoint, cancellationToken);
            var connectTask = connectAsync.AsTask();
            await connectTask.WaitAsync(cts.Token);
        }
        catch (Exception)
        {
            // ignored
        }

        cts.Dispose();

        if (!Socket.Connected)
            throw new SocketException((int)SocketError.NotConnected);
            
        Stream = new NetworkStream(Socket);
        _logger.LogInformation("Connected to the server");
            
        Connected?.Invoke(this, EventArgs.Empty);
    }
}