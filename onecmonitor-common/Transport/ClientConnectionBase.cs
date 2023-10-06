using OnecMonitor.Common.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OnecMonitor.Common.Transport
{
    public abstract class ClientConnectionBase : ConnectionBase
    {
        private CancellationTokenSource? _loopsCts;

        public Guid ConnectionId { get; protected set; }

        protected delegate void DisconnectingHandler();
        protected event DisconnectingHandler? Disconnected;

        public ClientConnectionBase(Socket socket)
        {
            ConnectionId = Guid.NewGuid();

            _socket = socket;
            _stream = new NetworkStream(socket);
        }

        protected void RunStreamLoops(CancellationToken cancellationToken)
        {
            _loopsCts = new CancellationTokenSource();
            cancellationToken.Register(_loopsCts.Cancel);

            _ = StartReadingFromStream(_loopsCts.Token);
            _ = StartWritingToStream(_loopsCts.Token);
        }

        protected void StopStreamLoops()
        {
            _loopsCts?.Cancel();
        }

        private async Task StartWritingToStream(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var item = await _outputChannel.Reader.ReadAsync(cancellationToken);

                try
                {
                    await _stream!.WriteAsync(item.Header.ToBytesArray(), cancellationToken);

                    if (item.Data.Length > 0)
                        await _stream!.WriteAsync(item.Data, cancellationToken);
                }
                catch
                {
                    Disconnected?.Invoke();
                    break;
                }
            }
        }

        private async Task StartReadingFromStream(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
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

                    await _inputChannel.Writer.WriteAsync(message, cancellationToken);
                }
                catch
                {
                    Disconnected?.Invoke();
                    break;
                }
            }
        }
    }
}
