using OnecMonitor.Common.Models;
using System.Net;
using System.Net.Sockets;

namespace OnecMonitor.Common.Transport
{
    public abstract class ServerConnectionBase : ConnectionBase
    {
        private readonly string _host;
        private readonly int _port;

        private CancellationTokenSource? _loopsCts;
        private readonly SemaphoreSlim _connectingSemaphore = new(1);

        protected delegate void ReconnectingHandler();
        protected event ReconnectingHandler? Reconnected;

        public ServerConnectionBase(string host, int port)
        {
            _host = host;
            _port = port;
        }

        protected void RunStreamLoops(CancellationToken cancellationToken)
        {
            _loopsCts = new CancellationTokenSource();
            cancellationToken.Register(_loopsCts.Cancel);

            _ = StartReadingFromStream(_loopsCts.Token);
            _ = StartWritingToStream(_loopsCts.Token);
        }

        private void StopStreamLoops()
        {
            _loopsCts?.Cancel();
        }

        private async Task StartReadingFromStream(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    await Reconnect(cancellationToken);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var headerbuffer = await ReadBytesFromStream(MessageHeader.HEADER_LENGTH, cancellationToken);
                        var header = MessageHeader.FromBytesArray(headerbuffer.Span);

                        Message message;

                        if (header.Length > 0)
                        {
                            var dataBuffer = await ReadBytesFromStream(header.Length, cancellationToken);

                            message = new Message(header, dataBuffer);
                        }
                        else
                            message = new Message(header);

                        if (message.Header.CallId == Guid.Empty)
                            await _inputChannel.Writer.WriteAsync(message, cancellationToken);
                        else if (_calls.TryGetValue(message.Header.CallId, out var cts))
                            cts.TrySetResult(message);
                    }

                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
                {
                    
                }
            }
            while (!cancellationToken.IsCancellationRequested);
        }

        private async Task StartWritingToStream(CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    await Reconnect(cancellationToken);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var item = await _outputChannel.Reader.ReadAsync(cancellationToken);

                        await _stream!.WriteAsync(item.Header.ToBytesArray(), cancellationToken);

                        if (item.Data.Length > 0)
                            await _stream!.WriteAsync(item.Data, cancellationToken);
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NotConnected)
                {

                }
            }
            while (!cancellationToken.IsCancellationRequested);
        }

        private async Task Reconnect(CancellationToken cancellationToken)
        {
            await _connectingSemaphore.WaitAsync(cancellationToken);

            if (_socket?.Connected == true)
                return;

            _socket?.Dispose();

            var addresses = await Dns.GetHostAddressesAsync(_host, AddressFamily.InterNetwork, cancellationToken);
            if (addresses.Length == 0)
                throw new Exception("Couldn't resolve server address");
            var endPoint = new IPEndPoint(addresses[0], _port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            await _socket.ConnectAsync(endPoint, cancellationToken);

            _stream = new NetworkStream(_socket);

            _connectingSemaphore.Release();

            Reconnected?.Invoke();
        }
    }
}
