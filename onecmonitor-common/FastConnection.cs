using MessagePack;
using OnecMonitor.Common.DTO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace OnecMonitor.Common;

public abstract class FastConnection(ILogger<FastConnection> logger) : IDisposable
{
    protected Socket? Socket;
    private CancellationToken _cancellationToken;
    
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> _calls = new();
    private readonly Channel<Message> _messagesChannel = Channel.CreateUnbounded<Message>();
    private readonly SemaphoreSlim _disconnectingEventSemaphore = new(0);
    private bool _disposedValue;
    
    public event EventHandler<Message>? MessageReceived;
    protected internal event EventHandler? Disconnected;

    private void RaiseDisconnected()
    {
        if (_disconnectingEventSemaphore.CurrentCount == 0)
            return;
        
        _disconnectingEventSemaphore.Wait();
        
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    protected void RunStreamLoops(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        
        Task.Factory.StartNew(StartWritingToStream, TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(StartReadingFromStream, TaskCreationOptions.LongRunning);
        
        _disconnectingEventSemaphore.Release();
    }

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
        var cts = new TaskCompletionSource<Message>(TaskCreationOptions.RunContinuationsAsynchronously);
        _calls.TryAdd(message.Header.CallId, cts);
        
        logger.LogTrace($"Постановка сообщения в очередь отправки. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
        await _messagesChannel.Writer.WriteAsync(message, cancellationToken);
        
        logger.LogTrace($"Ожидание подтверждения получения сообщения. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
        var result = await cts.Task.WaitAsync(cancellationToken);
        
        logger.LogTrace($"Подтверждение получения сообщения получено. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
        if (result == null)
            throw new TimeoutException("Ошибка получения ответа на вызов");
            
        ThrowIfError(result, cancellationToken);

        _calls.TryRemove(message.Header.CallId, out _);
        
        if (result.Header.Type == responseMessageType) 
            return MessagePackSerializer.Deserialize<TResult>(result.Data, null, cancellationToken);
            
        throw new Exception($"Получено неожиданное сообщение. Ожидаемый тип: {responseMessageType}");
    }
    
    private async Task WriteMessage(Message message, bool needWait, CancellationToken cancellationToken)
    {
        if (!needWait)
            await _messagesChannel.Writer.WriteAsync(message, cancellationToken);
        else
        {
            var cts = new TaskCompletionSource<Message>(TaskCreationOptions.RunContinuationsAsynchronously);
            _calls.TryAdd(message.Header.CallId, cts);
            
            logger.LogTrace($"Постановка сообщения в очередь отправки. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
            await _messagesChannel.Writer.WriteAsync(message, cancellationToken);
            
            logger.LogTrace($"Ожидание подтверждения на сообщение. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
            var result = await cts.Task.WaitAsync(cancellationToken);
            
            logger.LogTrace($"Подтверждение сообщения получено. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
            if (result == null)
                throw new TimeoutException("Ошибка получения ответа на вызов");
            
            ThrowIfError(result, cancellationToken);

            _calls.TryRemove(message.Header.CallId, out _);
            
            if (result.Header.Type != MessageType.Ok)
                throw new Exception($"Получено неожиданное сообщение. Ожидаемый тип: {MessageType.Ok}");
        }
    }
        
    private async Task StartWritingToStream()
    {
        try
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                var message = await _messagesChannel.Reader.ReadAsync(_cancellationToken);
                
                await Socket!.SendAsync(message.Header.AsMemory(), _cancellationToken);

                if (message.Data.Length > 0)
                    await Socket!.SendAsync(message.Data, _cancellationToken);
                
                logger.LogTrace($"Отправлено сообщение. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
            }
        }
        catch
        {
            RaiseDisconnected();
        }
    }
    
    protected async Task WriteMessageToStream<T>(MessageType messageType, T item, CancellationToken cancellationToken)
    {
        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length);

        try
        {
            await Socket!.SendAsync(header.ToBytesArray(), cancellationToken);

            if (header.Length > 0)
                await Socket!.SendAsync(data, cancellationToken);
            
            logger.LogTrace($"Отправлено сообщение. Тип: {header.Type}. Идентификатор: {header.CallId}");
        }
        catch
        {
            RaiseDisconnected();
        }
    }

    private async Task StartReadingFromStream()
    {
        try
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                var headerBuffer = await ReadBytesFromStream(MessageHeader.HeaderLength, _cancellationToken);
                var header = MessageHeader.FromSpan(headerBuffer.Span);

                Message message;

                if (header.Length > 0)
                {
                    var dataBuffer = await ReadBytesFromStream(header.Length, _cancellationToken);
                    message = new Message(header, dataBuffer);
                }
                else
                    message = new Message(header);
                
                logger.LogTrace($"Получено сообщение из потока. Тип: {header.Type}. Идентификатор: {header.CallId}");
                    
                if (_calls.TryGetValue(message.Header.CallId, out var cts))
                    cts.TrySetResult(message);
                else if (MessageReceived is not null)
                    MessageReceived.Invoke(this, message);
                else
                    await SendError(message, "Не найдено подключенных обработчиков сообщений", _cancellationToken);
            }
        }
        catch
        {
            RaiseDisconnected();
        }
    }

    private async Task<Memory<byte>> ReadBytesFromStream(int count, CancellationToken cancellationToken)
    {
        var memory = new Memory<byte>(new byte[count]);

        var read = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (Socket!.Poll(0, SelectMode.SelectRead) && Socket.Available == 0)
                throw new Exception("Disconnected");
            
            read += await Socket!.ReceiveAsync(memory[read..], cancellationToken);

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