using MessagePack;
using OnecMonitor.Common.DTO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Threading.Channels;

namespace OnecMonitor.Common;

public abstract class FastConnection : IDisposable
{
    private CancellationTokenSource? _loopsCts;
        
    protected Socket? Socket;
    protected NetworkStream? Stream;

    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> _calls = new();
    private readonly Channel<Message> _inputChannel = Channel.CreateBounded<Message>(1000);
    private readonly Channel<Message> _outputChannel = Channel.CreateBounded<Message>(1000);
    private readonly SemaphoreSlim _disconnectingEventSemaphore = new(1);
    private bool _disposedValue;
        
    protected internal event EventHandler? Disconnected;

    private async Task RaiseDisconnected(CancellationToken cancellationToken)
    {
        await _disconnectingEventSemaphore.WaitAsync(cancellationToken);

        if (_loopsCts?.IsCancellationRequested == false)
        {
            StopStreamLoops();
            Disconnected?.Invoke(this, EventArgs.Empty);
            _disconnectingEventSemaphore.Release();
        }
    }

    protected void RunStreamLoops()
    {
        _disconnectingEventSemaphore.Release();

        _loopsCts = new CancellationTokenSource();

        _ = StartWritingToStream(_loopsCts.Token);
        _ = StartReadingFromStream(_loopsCts.Token);
    }

    private void StopStreamLoops()
    {
        _loopsCts?.Cancel();
    }

    public async Task<Message> ReadMessage(CancellationToken cancellationToken)
        => await _inputChannel.Reader.ReadAsync(cancellationToken);

    public async Task SendOk(Message callMessage, CancellationToken cancellationToken)
    {
        var header = new MessageHeader(MessageType.Ok, 0, callMessage.Header.CallId);
        var message = new Message(header);

        await WriteMessage(message, false, cancellationToken);
    }

    public async Task SendError(Message callMessage, string messageText, CancellationToken cancellationToken)
    {
        var data = MessagePackSerializer.Serialize(new ErrorDto { Message = messageText }, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(MessageType.Error, data.Length, callMessage.Header.CallId);
        var message = new Message(header, data);
        
        await WriteMessage(message, false, cancellationToken);
    }
        
    public async Task Send<T>(MessageType messageType, T item, Message callMessage, CancellationToken cancellationToken)
    {
        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length, callMessage.Header.CallId);
        var message = new Message(header, data);
        
        await WriteMessage(message, false, cancellationToken);
    }
    
    public async Task Send<T>(MessageType messageType, T item, CancellationToken cancellationToken)
    {
        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length, Guid.NewGuid());
        var message = new Message(header, data);
        
        await WriteMessage(message, true, cancellationToken);
    }
    
    public async Task Send(MessageType messageType, CancellationToken cancellationToken)
    {
        var header = new MessageHeader(messageType, 0, Guid.NewGuid());
        var message = new Message(header);

        await WriteMessage(message, true, cancellationToken);
    }

    public async Task<TResult> Get<TResult>(
        MessageType messageType, 
        MessageType responseMessageType,
        CancellationToken cancellationToken)
    {
        var header = new MessageHeader(messageType, 0, Guid.NewGuid());
        var message = new Message(header);

        return await WriteMessageAndWaitResult<TResult>(message, responseMessageType, cancellationToken);
    }

    public async Task<TResult> Get<T, TResult>(
        MessageType messageType, 
        MessageType responseMessageType, 
        T item, 
        CancellationToken cancellationToken)
    {
        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length, Guid.NewGuid());
        var message = new Message(header, data);

        return await WriteMessageAndWaitResult<TResult>(message, responseMessageType, cancellationToken);
    }
        
    private async Task<TResult> WriteMessageAndWaitResult<TResult>(Message message, MessageType responseMessageType, CancellationToken cancellationToken)
    {
        var cts = new TaskCompletionSource<Message>();
        _calls.TryAdd(message.Header.CallId, cts);

        await _outputChannel.Writer.WriteAsync(message, cancellationToken);

        var result = await cts.Task.WaitAsync(cancellationToken);

        if (result == null)
            throw new TimeoutException("Failed to get response for the call");
            
        ThrowIfError(result, cancellationToken);

        _calls.TryRemove(message.Header.CallId, out _);
        
        if (result.Header.Type == responseMessageType) 
            return MessagePackSerializer.Deserialize<TResult>(result.Data, null, cancellationToken);
            
        throw new Exception("Received unexpected message type");
    }
    
    private async Task WriteMessage(Message message, bool needWait, CancellationToken cancellationToken)
    {
        if (!needWait)
            await _outputChannel.Writer.WriteAsync(message, cancellationToken);
        else
        {
            var cts = new TaskCompletionSource<Message>();
            _calls.TryAdd(message.Header.CallId, cts);

            await _outputChannel.Writer.WriteAsync(message, cancellationToken);

            var result = await cts.Task.WaitAsync(cancellationToken);

            if (result == null)
                throw new TimeoutException("Failed to get response for the call");
            
            ThrowIfError(result, cancellationToken);

            _calls.TryRemove(message.Header.CallId, out _);
            
            if (result.Header.Type != MessageType.Ok)
                throw new Exception("Received unexpected message type. Expected is Ok message");
        }
    }

    protected async Task WriteMessageToStream<T>(MessageType messageType, T item, CancellationToken cancellationToken)
    {
        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length);

        try
        {
            await Stream!.WriteAsync(header.ToBytesArray(), cancellationToken);

            if (header.Length > 0)
                await Stream!.WriteAsync(data, cancellationToken);
        }
        catch
        {
            await RaiseDisconnected(cancellationToken);
        }
    }
        
    private async Task StartWritingToStream(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var item = await _outputChannel.Reader.ReadAsync(cancellationToken);

                await Stream!.WriteAsync(item.Header.AsMemory(), cancellationToken);

                if (item.Data.Length > 0)
                    await Stream!.WriteAsync(item.Data, cancellationToken);
            }
            catch
            {
                await RaiseDisconnected(cancellationToken);
            }
        }
    }

    private async Task StartReadingFromStream(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var headerBuffer = await ReadBytesFromStream(MessageHeader.HeaderLength, cancellationToken);
                var header = MessageHeader.FromSpan(headerBuffer.Span);

                Message message;

                if (header.Length > 0)
                {
                    var dataBuffer = await ReadBytesFromStream(header.Length, cancellationToken);
                    message = new Message(header, dataBuffer);
                }
                else
                    message = new Message(header);
                    
                if (_calls.TryGetValue(message.Header.CallId, out var cts))
                    cts.TrySetResult(message);
                else
                    await _inputChannel.Writer.WriteAsync(message, cancellationToken);
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
            read += await Stream!.ReadAsync(memory[read..], cancellationToken);

            if (count == read)
                break;
        }

        return memory;
    }

    private static void ThrowIfError(Message message, CancellationToken cancellationToken)
    {
        if (message.Header.Type != MessageType.Error) 
            return;
            
        var error = MessagePackSerializer.Deserialize<ErrorDto>(message.Data, null, cancellationToken);
        throw new Exception(error.Message);
    }
        
    private void Dispose(bool disposing)
    {
        if (_disposedValue) 
            return;
            
        if (disposing)
        {
            _loopsCts?.Cancel();
            Socket?.Dispose();
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}