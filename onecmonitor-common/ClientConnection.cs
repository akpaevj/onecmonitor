using System.Net.Sockets;

namespace OnecMonitor.Common;

public class ClientConnection : FastConnection
{
    protected internal new event EventHandler? Disconnected;

    protected ClientConnection(Socket socket)
    {
        Socket = socket;
        Stream = new NetworkStream(socket);
        
        base.Disconnected += (sender, args) =>
            Disconnected?.Invoke(sender, args);
    }
    
    protected void Listen()
        => RunStreamLoops();
}