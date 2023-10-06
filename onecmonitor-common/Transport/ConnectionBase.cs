using MessagePack;
using OnecMonitor.Common.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OnecMonitor.Common.Transport
{
    /// <summary>
    /// Base class for client/server connection that encapsulated protocol-specific methods
    /// </summary>
    public abstract class ConnectionBase : IDisposable
    {
        protected internal Socket? _socket;
        protected internal NetworkStream? _stream;

        protected readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> _calls = new();
        protected Channel<Message> _inputChannel = Channel.CreateBounded<Message>(1000);
        protected Channel<Message> _outputChannel = Channel.CreateBounded<Message>(1000);

        public async Task<Message> ReadMessage(CancellationToken cancellationToken)
        {
            return await _inputChannel.Reader.ReadAsync(cancellationToken);
        }

        protected internal async Task WriteMessage(MessageType messageType, Message? request, CancellationToken cancellationToken)
        {
            var header = new MessageHeader(messageType, 0, request?.Header.CallId ?? Guid.Empty);
            var message = new Message(header);

            await WriteMessage(message, cancellationToken);
        }

        protected internal async Task WriteMessage<T>(MessageType messageType, T item, Message? request, CancellationToken cancellationToken)
        {
            var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
            var header = new MessageHeader(messageType, data.Length, request?.Header.CallId ?? Guid.Empty);
            var message = new Message(header, data);

            await WriteMessage(message, cancellationToken);
        }

        protected internal async Task<Message> WriteMessageAndWaitResult(MessageType messageType, CancellationToken cancellationToken)
        {
            var message = new Message(new MessageHeader(messageType, 0, Guid.NewGuid()));

            return await WriteMessageAndWaitResult(message, cancellationToken);
        }

        protected internal async Task<Message> WriteMessageAndWaitResult<T>(MessageType messageType, T item, CancellationToken cancellationToken)
        {
            var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
            var message = new Message(new MessageHeader(messageType, data.Length, Guid.NewGuid()), data);

            return await WriteMessageAndWaitResult(message, cancellationToken);
        }

        protected internal async Task WriteMessageToStream(MessageType messageType, CancellationToken cancellationToken)
        {
            var header = new MessageHeader(messageType, 0);
            var message = new Message(header);

            await WriteMessageToStream(message, cancellationToken);
        }

        protected internal async Task WriteMessageToStream<T>(MessageType messageType, T item, CancellationToken cancellationToken)
        {
            var data = MessagePackSerializer.Serialize(item, cancellationToken: cancellationToken).AsMemory();
            var header = new MessageHeader(messageType, data.Length);
            var message = new Message(header, data);

            await WriteMessageToStream(message, cancellationToken);
        }

        /// <summary>
        /// Writes message to output queue
        /// </summary>
        /// <param name="message">Message that should be sent</param>
        /// <param name="cancellationToken">Operation cancellation token</param>
        /// <returns></returns>
        private async Task WriteMessage(Message message, CancellationToken cancellationToken)
        {
            await _outputChannel.Writer.WriteAsync(message, cancellationToken);
        }

        /// <summary>
        /// Writes message header and data to the network stream
        /// </summary>
        /// <param name="message">Message that should be sent</param>
        /// <param name="cancellationToken">Operation cancellation token</param>
        /// <returns></returns>
        private async Task WriteMessageToStream(Message message, CancellationToken cancellationToken)
        {
            await _stream!.WriteAsync(message.Header.ToBytesArray(), cancellationToken);

            if (message.Header.Length > 0)
                await _stream!.WriteAsync(message.Data, cancellationToken);
        }

        /// <summary>
        /// Writes message to output queue, waits and returns response from remote host
        /// </summary>
        /// <param name="message">Message that should be sent</param>
        /// <param name="cancellationToken">Operation cancellation token</param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        private async Task<Message> WriteMessageAndWaitResult(Message message, CancellationToken cancellationToken)
        {
            var cts = new TaskCompletionSource<Message>();
            _calls.TryAdd(message.Header.CallId, cts);

            await _outputChannel.Writer.WriteAsync(message, cancellationToken);

            var result = await cts.Task.WaitAsync(cancellationToken);

            if (result == null)
                throw new TimeoutException("Failed to get response for the call");

            _calls.TryRemove(message.Header.CallId, out _);

            return result!;
        }

        /// <summary>
        /// Reads specified count of bytes from the network stream
        /// </summary>
        /// <param name="count">Count of bytes must be read</param>
        /// <param name="cancellationToken">Operation cancellation token</param>
        /// <returns></returns>
        protected async Task<Memory<byte>> ReadBytesFromStream(int count, CancellationToken cancellationToken)
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

        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}