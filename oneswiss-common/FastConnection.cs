using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Channels;
using MessagePack;
using Microsoft.Extensions.Logging;
using OneSwiss.Common.DTO;

namespace OneSwiss.Common;

public abstract class FastConnection(ILogger<FastConnection> logger) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> _calls = new();
    private readonly SemaphoreSlim _disconnectingEventSemaphore = new(0);
    private readonly Channel<Message> _messagesChannel = Channel.CreateUnbounded<Message>();
    private readonly ConcurrentDictionary<Guid, Stream> _streams = new();
    private CancellationToken _cancellationToken;
    private bool _disposedValue;
    protected WebSocket? Socket;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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

    protected async Task SendOk(Message callMessage, CancellationToken cancellationToken)
    {
        ThrowIfDisconnected();

        var header = new MessageHeader(MessageType.Ok, 0, callMessage.Header.CallId);
        var message = new Message(header);

        await WriteMessage(message, false, cancellationToken);
    }

    protected async Task SendError(Message callMessage, string messageText, CancellationToken cancellationToken)
    {
        ThrowIfDisconnected();

        var data = MessagePackSerializer
            .Serialize(new ErrorDto { Message = messageText }, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(MessageType.Error, data.Length, callMessage.Header.CallId);
        var message = new Message(header, data);

        await WriteMessage(message, false, cancellationToken);
    }

    protected async Task Send<T>(MessageType messageType, T item, Message callMessage,
        CancellationToken cancellationToken)
    {
        ThrowIfDisconnected();

        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length, callMessage.Header.CallId);
        var message = new Message(header, data);

        await WriteMessage(message, false, cancellationToken);
    }

    protected async Task Send<T>(MessageType messageType, T item, CancellationToken cancellationToken)
    {
        ThrowIfDisconnected();

        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length, Guid.NewGuid());
        var message = new Message(header, data);

        await WriteMessage(message, true, cancellationToken);
    }

    protected async Task<TResult> Get<TResult>(
        MessageType messageType,
        MessageType responseMessageType,
        CancellationToken cancellationToken)
    {
        ThrowIfDisconnected();

        var header = new MessageHeader(messageType, 0, Guid.NewGuid());
        var message = new Message(header);

        return await WriteMessageAndWaitResult<TResult>(message, responseMessageType, cancellationToken);
    }

    protected async Task<TResult> Get<T, TResult>(
        MessageType messageType,
        MessageType responseMessageType,
        T item,
        CancellationToken cancellationToken)
    {
        ThrowIfDisconnected();

        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length, Guid.NewGuid());
        var message = new Message(header, data);

        return await WriteMessageAndWaitResult<TResult>(message, responseMessageType, cancellationToken);
    }

    protected async Task ToStream<T>(
        Stream stream,
        MessageType messageType,
        T item,
        CancellationToken cancellationToken)
    {
        ThrowIfDisconnected();

        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length, Guid.NewGuid());
        var message = new Message(header, data);

        _streams.TryAdd(header.CallId, stream);

        await WriteMessage(message, true, cancellationToken);
    }

    private async Task<TResult> WriteMessageAndWaitResult<TResult>(Message message, MessageType responseMessageType,
        CancellationToken cancellationToken)
    {
        var result = await WriteMessageAndGetResult(message, cancellationToken);

        if (result.Header.Type == responseMessageType)
            return MessagePackSerializer.Deserialize<TResult>(result.Data, null, cancellationToken);

        throw new Exception($"Получено неожиданное сообщение. Ожидаемый тип: {responseMessageType}");
    }

    private async Task WriteMessage(Message message, bool needWait, CancellationToken cancellationToken)
    {
        if (needWait)
        {
            var result = await WriteMessageAndGetResult(message, cancellationToken);

            if (result.Header.Type != MessageType.Ok)
                throw new Exception(
                    $"Получено неожиданное сообщение ({message.Header.Type}). Ожидаемый тип: {MessageType.Ok}");
        }
        else
        {
            await _messagesChannel.Writer.WriteAsync(message, cancellationToken);
        }
    }

    private async Task<Message> WriteMessageAndGetResult(Message message, CancellationToken cancellationToken)
    {
        var cts = new TaskCompletionSource<Message>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_calls.TryAdd(message.Header.CallId, cts))
            throw new InvalidOperationException("Duplicate call ID detected");

        logger.LogTrace(
            $"Постановка сообщения в очередь отправки. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
        await _messagesChannel.Writer.WriteAsync(message, cancellationToken);

        logger.LogTrace(
            $"Ожидание подтверждения получения сообщения. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
        var result = await cts.Task.WaitAsync(cancellationToken);

        logger.LogTrace(
            $"Подтверждение получения сообщения получено. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
        if (result == null)
            throw new TimeoutException("Ошибка получения ответа на вызов");

        ThrowIfError(result, cancellationToken);

        _calls.TryRemove(message.Header.CallId, out _);

        return result;
    }

    private async Task StartWritingToStream()
    {
        try
        {
            while (!_cancellationToken.IsCancellationRequested && Socket?.State == WebSocketState.Open)
            {
                var message = await _messagesChannel.Reader.ReadAsync(_cancellationToken);

                // Отправляем заголовок
                await Socket.SendAsync(
                    message.Header.AsMemory(),
                    WebSocketMessageType.Binary,
                    message.Data.Length == 0, // not end of message - ждем данные
                    _cancellationToken);

                // Отправляем данные если есть
                if (message.Data.Length > 0)
                    await Socket.SendAsync(
                        message.Data,
                        WebSocketMessageType.Binary,
                        true, // end of message
                        _cancellationToken);

                logger.LogTrace(
                    $"Отправлено сообщение. Тип: {message.Header.Type}. Идентификатор: {message.Header.CallId}");
            }
        }
        catch
        {
            RaiseDisconnected();
        }
    }

    protected async Task WriteMessageToStream<T>(MessageType messageType, T item, CancellationToken cancellationToken)
    {
        ThrowIfDisconnected();

        var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
        var header = new MessageHeader(messageType, data.Length);

        try
        {
            // Отправляем заголовок
            await Socket!.SendAsync(
                header.AsMemory(),
                WebSocketMessageType.Binary,
                data.Length == 0,
                cancellationToken);

            // Отправляем данные если есть
            if (data.Length > 0)
                await Socket.SendAsync(
                    data,
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken);

            logger.LogTrace($"Отправлено сообщение. Тип: {header.Type}. Идентификатор: {header.CallId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing message to stream");
            RaiseDisconnected();
            throw;
        }
    }

    private async Task StartReadingFromStream()
    {
        try
        {
            while (!_cancellationToken.IsCancellationRequested && Socket?.State == WebSocketState.Open)
            {
                // Читаем заголовок (первое сообщение)
                var headerBuffer = await ReadMessagePart(MessageHeader.HeaderLength, _cancellationToken);
                var header = MessageHeader.FromSpan(headerBuffer.Span);

                Message message;

                if (header.Length > 0)
                {
                    // Читаем данные (второе сообщение)
                    var dataBuffer = await ReadMessagePart(header.Length, _cancellationToken);
                    message = new Message(header, dataBuffer);
                }
                else
                {
                    message = new Message(header);
                }

                logger.LogTrace($"Получено сообщение из потока. Тип: {header.Type}. Идентификатор: {header.CallId}");

                if (message.Header.Type is MessageType.DataStreamChunk or MessageType.DataStreamHeader)
                {
                    if (!_streams.TryGetValue(message.Header.CallId, out var stream))
                    {
                        await SendError(message, $"Поток чтения для вызова {header.CallId} не зарегистрирован",
                            _cancellationToken);
                    }
                    else if (message.Header.Type is MessageType.DataStreamChunk)
                    {
                        logger.LogTrace(
                            $"Для потока вызова {header.CallId} получена часть бинарных данных: {message.Data.Length} байт");
                        await stream.WriteAsync(message.Data, _cancellationToken);
                    }
                }
                else if (_calls.TryGetValue(message.Header.CallId, out var cts))
                {
                    if (_streams.TryRemove(message.Header.CallId, out _))
                        logger.LogTrace($"Поток вызова {header.CallId} удален из очереди ожидающих приема данных");

                    cts.TrySetResult(message);
                }
                else if (MessageReceived is not null)
                {
                    MessageReceived.Invoke(this, message);
                }
                else
                {
                    await SendError(message, "Не найдено подключенных обработчиков сообщений", _cancellationToken);
                }
            }
        }
        catch
        {
            RaiseDisconnected();
        }
    }

    private async Task<Memory<byte>> ReadMessagePart(int expectedLength, CancellationToken cancellationToken)
    {
        var memory = new Memory<byte>(new byte[expectedLength]);
        var totalRead = 0;

        while (totalRead < expectedLength && !cancellationToken.IsCancellationRequested)
        {
            if (Socket?.State != WebSocketState.Open)
                throw new WebSocketException($"WebSocket is not open (State: {Socket?.State})");

            var result = await Socket.ReceiveAsync(
                memory[totalRead..],
                cancellationToken);

            // Если получен close frame, закрываем соединение
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    "Connection closed by remote", cancellationToken);
                throw new WebSocketException("Connection closed by remote");
            }

            totalRead += result.Count;

            // Если это конец сообщения, но мы прочитали не все данные - ошибка
            if (result.EndOfMessage && totalRead < expectedLength)
                throw new WebSocketException(
                    $"Unexpected end of message. Expected: {expectedLength}, Received: {totalRead}");

            // Если это конец сообщения и мы прочитали все данные - выходим
            if (result.EndOfMessage && totalRead == expectedLength)
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

    private void ThrowIfDisconnected()
    {
        if (Socket?.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected");
    }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;

        if (disposing)
        {
            // Cancel all pending calls
            foreach (var cts in _calls.Values) cts.TrySetCanceled();
            _calls.Clear();

            Socket?.Dispose();
            _disconnectingEventSemaphore?.Dispose();
            _messagesChannel.Writer.TryComplete();
        }

        _disposedValue = true;
    }
}