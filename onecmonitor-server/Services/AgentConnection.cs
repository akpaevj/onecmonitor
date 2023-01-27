using Grpc.Core;
using MessagePack;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common;
using OnecMonitor.Common.Models;
using OnecMonitor.Server.Models;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace OnecMonitor.Server.Services
{
    public class AgentConnection : OnecMonitorConnection
    {
        private readonly AsyncServiceScope _agentScope;
        private readonly AppDbContext _appDbContext;
        private readonly IClickHouseContext _clickHouseContext;
        private readonly TechLogProcessor _techLogProcessor;
        private readonly ILogger<AgentConnection> _logger;

        public Guid ConnectionId { get; private set; }
        public AgentInstance? AgentInstance { get; private set; }

        public delegate void AgentConnectedHandler(AgentConnection agentConnection);
        public event AgentConnectedHandler? AgentConnected;

        public delegate void AgentSubscribedForCommandsHandler(AgentConnection agentConnection);
        public event AgentSubscribedForCommandsHandler? SubscribedForCommands;

        public delegate void AgentDisconnectedHandler(AgentConnection agentConnection);
        public event AgentDisconnectedHandler? AgentDisconnected;

        public AgentConnection(Socket socket, IServiceProvider serviceProvider) : base(true)
        {
            Disconnected += () =>
            {
                StopSteamLoops();
                AgentDisconnected?.Invoke(this);
            };

            ConnectionId = Guid.NewGuid();
            _socket = socket;
            _stream = new NetworkStream(socket);
            _agentScope = serviceProvider.CreateAsyncScope();
            _appDbContext = _agentScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _clickHouseContext = _agentScope.ServiceProvider.GetRequiredService<IClickHouseContext>();
            _techLogProcessor = serviceProvider.GetRequiredService<TechLogProcessor>();
            _logger = _agentScope.ServiceProvider.GetRequiredService<ILogger<AgentConnection>>();
        }

        public async Task StartListening(CancellationToken cancellationToken)
        {
            RunSteamLoops();

            // first message must be an init message
            var firstMessage = await ReadMessage(cancellationToken);
            if (firstMessage.Header.Type != MessageType.AgentInfo)
            {
                _socket?.Close();
                throw new Exception("First message must be \"Agent info\" message, connection closed");
            }
            else
                await HandleInitMessage(firstMessage.Data, cancellationToken);

            while (!cancellationToken.IsCancellationRequested) 
            {
                var message = await ReadMessage(cancellationToken);

                switch (message.Header.Type)
                {
                    case MessageType.TechLogEventContent:
                        await HandleTechLogEventContent(message.Data, cancellationToken);
                        break;
                    case MessageType.LastFilePositionRequest:
                        await HandleLastFilePositionRequest(message, cancellationToken);
                        break;
                    case MessageType.TechLogSeancesRequest:
                        await UpdateTechLogSeances(message, cancellationToken);
                        break;
                    case MessageType.SubscribingForCommands:
                        await HandleSubscribingForCommands(cancellationToken);
                        break;
                    default:
                        throw new Exception("Received unexpected message type");
                };
            }
        }

        public async Task UpdateTechLogSeances(Message? callMessage, CancellationToken cancellationToken)
        {
            var agent = await _appDbContext.Agents.FirstOrDefaultAsync(c => c.Id == AgentInstance!.Id, cancellationToken);

            var agentSeances = await _appDbContext.TechLogSeances
                .AsNoTracking()
                .Include(c => c.ConnectedAgents)
                .Where(c => c.ConnectedAgents.Contains(agent!))
                .Include(c => c.ConnectedTemplates)
                .ToListAsync(cancellationToken);

            var seances = new List<TechLogSeanceDto>();

            agentSeances.ForEach(c =>
            {
                StringBuilder templateBuilder = new();

                c.ConnectedTemplates.ForEach(c =>
                {
                    // add template id and combine templates
                    templateBuilder.AppendLine(c.Content.Replace("{LOG_PATH}", $"{{LOG_PATH}}{c.Id}"));
                });

                seances.Add(new TechLogSeanceDto()
                {
                    Id = c.Id,
                    StartDateTime = c.StartDateTime,
                    FinishDateTime = c.FinishDateTime,
                    Template = templateBuilder.ToString()
                });
            });

            await WriteMessage(MessageType.TechLogSeances, seances, callMessage, cancellationToken);
        }

        private async Task HandleInitMessage(ReadOnlyMemory<byte> messageData, CancellationToken cancellationToken)
        {
            AgentInstance = ParseMessageData<AgentInstance>(messageData, cancellationToken);

            await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var foundItem = await _appDbContext.Agents.FirstOrDefaultAsync(c => c.Id == AgentInstance.Id, cancellationToken);

                if (foundItem == null)
                {
                    var agent = new Agent()
                    {
                        Id = AgentInstance.Id,
                        InstanceName = AgentInstance.InstanceName
                    };
                    _appDbContext.Agents.Add(agent);

                    await _appDbContext.SaveChangesAsync(cancellationToken);
                }
                else if (foundItem.InstanceName != AgentInstance.InstanceName)
                {
                    foundItem.InstanceName = AgentInstance.InstanceName;

                    _appDbContext.Entry(foundItem).State = EntityState.Modified;

                    await _appDbContext.SaveChangesAsync(cancellationToken);
                }

                await _appDbContext.Database.CommitTransactionAsync(cancellationToken);

                AgentConnected?.Invoke(this);
            }
            catch (Exception ex)
            {
                await _appDbContext.Database.RollbackTransactionAsync(cancellationToken);

                throw new RpcException(Status.DefaultCancelled, ex.Message);
            }
        }

        private async Task HandleSubscribingForCommands(CancellationToken cancellationToken)
        {
            SubscribedForCommands?.Invoke(this);
            await UpdateTechLogSeances(null, cancellationToken);
        }

        private async Task HandleLastFilePositionRequest(Message requestMessage, CancellationToken cancellationToken)
        {
            var request = ParseMessageData<LastFilePositionRequest>(requestMessage.Data, cancellationToken);

            var response = await _clickHouseContext.GetLastFilePosition(
                AgentInstance!.Id.ToString(),
                request.SeanceId.ToString(),
                request.TemplateId.ToString(),
                request.Folder,
                request.File,
                cancellationToken);

            await WriteMessage(MessageType.LastFilePosition, response, requestMessage, cancellationToken);
        }

        private async Task HandleTechLogEventContent(ReadOnlyMemory<byte> messageData, CancellationToken cancellationToken)
        {
            var item = ParseMessageData<TechLogEventContentDto>(messageData, cancellationToken);

            _logger.LogTrace($"Event with content \"{item.Content}\" from {item.Folder}/{item.File} {item.EndPosition} is read");

            await _techLogProcessor.ProcessTjEventContent(AgentInstance!, item, cancellationToken);
        }

        private static T ParseMessageData<T>(ReadOnlyMemory<byte> messageData, CancellationToken cancellationToken)
        {
            return MessagePackSerializer.Deserialize<T>(messageData, cancellationToken: cancellationToken);
        }

        public override bool Equals(object? obj)
        {
            return obj is AgentConnection connection &&
                   ConnectionId.Equals(connection.ConnectionId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ConnectionId);
        }
    }
}
