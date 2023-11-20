using MessagePack;
using OnecMonitor.Common.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Threading.Channels;

namespace OnecMonitor.Common
{
    public abstract class OnecMonitorConnection : IDisposable
    {
        private readonly bool _serverMode;
        protected internal CancellationTokenSource? _loopsCts;

        protected internal Socket? _socket;
        protected internal NetworkStream? _stream;

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> _calls = new();
        protected internal Channel<Message> _inputChannel = Channel.CreateBounded<Message>(1000);
        protected internal Channel<Message> _outputChannel = Channel.CreateBounded<Message>(1000);
        private bool disposedValue;
        private readonly SemaphoreSlim _disconnectingEventSemaphore = new(1);
        protected internal delegate void DisconnectedHandler();
        protected internal event DisconnectedHandler? Disconnected;

        public OnecMonitorConnection(bool serverMode)
        {
            _serverMode = serverMode;
        }

        private async Task RaiseDisconnected(CancellationToken cancellationToken)
        {
            await _disconnectingEventSemaphore.WaitAsync(cancellationToken);

            if (_loopsCts?.IsCancellationRequested == false)
            {
                StopStreamLoops();
                Disconnected?.Invoke();
                _disconnectingEventSemaphore.Release();
            }
        }

        protected internal void RunStreamLoops()
        {
            _disconnectingEventSemaphore.Release();

            _loopsCts = new CancellationTokenSource();

            _ = StartWritingToStream(_loopsCts.Token);
            _ = StartReadingFromStream(_loopsCts.Token);
        }

        protected internal void StopStreamLoops()
        {
            _loopsCts?.Cancel();
        }

        public async Task<Message> ReadMessage(CancellationToken cancellationToken)
        {
            return await _inputChannel.Reader.ReadAsync(cancellationToken);
        }

        protected internal virtual async Task WriteMessage(MessageType messageType, Message? callMessage, CancellationToken cancellationToken)
        {
            var header = new MessageHeader(messageType, 0, callMessage?.Header.CallId ?? Guid.Empty);
            var message = new Message(header);

            await _outputChannel.Writer.WriteAsync(message, cancellationToken);
        }

        protected internal async Task<Message> WriteMessageAndWaitResult(MessageType messageType, CancellationToken cancellationToken)
        {
            var header = new MessageHeader(messageType, 0, Guid.NewGuid());
            var message = new Message(header);

            var cts = new TaskCompletionSource<Message>();
            _calls.TryAdd(header.CallId, cts);

            await _outputChannel.Writer.WriteAsync(message, cancellationToken);

            var result = await cts.Task.WaitAsync(cancellationToken)
                ?? throw new TimeoutException("Failed to get response for the call");

            _calls.TryRemove(header.CallId, out _);

            return result!;
        }

        protected internal virtual async Task WriteMessage<T>(MessageType messageType, T? item, Message? callMessage, CancellationToken cancellationToken)
        {
            var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
            var header = new MessageHeader(messageType, data.Length, callMessage?.Header.CallId ?? Guid.Empty);
            var message = new Message(header, data);

            await _outputChannel.Writer.WriteAsync(message, cancellationToken);
        }

        protected internal virtual async Task<Message> WriteMessageAndWaitResult<T>(MessageType messageType, T item, CancellationToken cancellationToken)
        {
            var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
            var header = new MessageHeader(messageType, data.Length, Guid.NewGuid());
            var message = new Message(header, data);

            var cts = new TaskCompletionSource<Message>();
            _calls.TryAdd(header.CallId, cts);

            await _outputChannel.Writer.WriteAsync(message, cancellationToken);

            var result = await cts.Task.WaitAsync(cancellationToken);

            if (result == null)
                throw new TimeoutException("Failed to get response for the call");

            _calls.TryRemove(header.CallId, out _);

            return result!;
        }

        protected internal async Task WriteMessageToStream<T>(MessageType messageType, T item, CancellationToken cancellationToken)
        {
            var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
            var header = new MessageHeader(messageType, data.Length);

            try
            {
                await _stream!.WriteAsync(header.ToBytesArray(), cancellationToken);

                if (header.Length > 0)
                    await _stream!.WriteAsync(data, cancellationToken);
            }
            catch
            {
                await RaiseDisconnected(cancellationToken);
            }
        }

        protected internal async Task StartWritingToStream(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var item = await _outputChannel.Reader.ReadAsync(cancellationToken);

                    await _stream!.WriteAsync(item.Header.AsMemory(), cancellationToken);

                    if (item.Data.Length > 0)
                        await _stream!.WriteAsync(item.Data, cancellationToken);
                }
                catch
                {
                    await RaiseDisconnected(cancellationToken);
                }
            }
        }

        protected internal async Task StartReadingFromStream(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var headerbuffer = await ReadBytesFromStream(MessageHeader.HEADER_LENGTH, cancellationToken);
                    var header = MessageHeader.FromSpan(headerbuffer.Span);

                    Message message;

                    if (header.Length > 0)
                    {
                        var dataBuffer = await ReadBytesFromStream(header.Length, cancellationToken);

                        message = new Message(header, dataBuffer);
                    }
                    else
                        message = new Message(header);

                    if (_serverMode)
                        await _inputChannel.Writer.WriteAsync(message, cancellationToken);
                    else
                    {
                        if (message.Header.CallId == Guid.Empty)
                            await _inputChannel.Writer.WriteAsync(message, cancellationToken);
                        else if (_calls.TryGetValue(message.Header.CallId, out var cts))
                            cts.TrySetResult(message);
                    }
                }
                catch
                {
                    await RaiseDisconnected(cancellationToken);
                }
            }
        }

        private async Task<Memory<byte>> ReadBytesFromStream(int count, CancellationToken cancellationToken)
        {
            var memory = new Memory<byte>(new byte[count]);

            var read = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                read += await _stream!.ReadAsync(memory[read..], cancellationToken);

                if (count == read)
                    break;
            }

            return memory;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _loopsCts?.Cancel();
                    _socket?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}