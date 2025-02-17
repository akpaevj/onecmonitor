using Grpc.Core;
using MessagePack;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.Storage;
using OnecMonitor.Common.TechLog;
using OnecMonitor.Server.Models;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using OnecMonitor.Server.Helpers;
using OneSTools.Common.Platform;

namespace OnecMonitor.Server.Services
{
    public class AgentConnection : FastConnection
    {
        private readonly AsyncServiceScope _agentScope;
        private readonly AppDbContext _appDbContext;
        private readonly ITechLogStorage _clickHouseContext;
        private readonly TechLogProcessor _techLogProcessor;
        private readonly IMapper _mapper;
        private readonly ILogger<AgentConnection> _logger;

        public Guid ConnectionId { get; }
        public AgentInstance? AgentInstance { get; private set; }

        public delegate void AgentConnectedHandler(AgentConnection agentConnection);
        public event AgentConnectedHandler? AgentConnected;

        public delegate void AgentSubscribedForCommandsHandler(AgentConnection agentConnection);
        public event AgentSubscribedForCommandsHandler? SubscribedForCommands;

        public delegate void AgentDisconnectedHandler(AgentConnection agentConnection);
        public event AgentDisconnectedHandler? AgentDisconnected;

        public AgentConnection(Socket socket, TechLogProcessor techLogProcessor, IServiceProvider serviceProvider)
        {
            Socket = socket;
            
            Disconnected += (_, _) =>
            {
                AgentDisconnected?.Invoke(this);
            };

            ConnectionId = Guid.NewGuid();
            _agentScope = serviceProvider.CreateAsyncScope();
            _appDbContext = _agentScope.ServiceProvider.GetRequiredService<AppDbContext>();
            _clickHouseContext = serviceProvider.GetRequiredService<ITechLogStorage>();
            _techLogProcessor = techLogProcessor;
            _mapper = serviceProvider.GetRequiredService<IMapper>();
            _logger = _agentScope.ServiceProvider.GetRequiredService<ILogger<AgentConnection>>();
        }

        public async Task Listen(CancellationToken cancellationToken)
        {
            RunStreamLoops(cancellationToken);

            // first message must be an init message
            var firstMessage = await ReadMessage(cancellationToken);
            
            if (firstMessage.Header.Type != MessageType.AgentInfo)
            {
                Socket?.Close();
                throw new Exception("First message must be \"Agent info\", connection closed");
            }

            await HandleInitMessage(firstMessage.Data, cancellationToken);
                
            while (!cancellationToken.IsCancellationRequested) 
            {
                try
                {
                    var message = await ReadMessage(cancellationToken);
                    
                    try
                    {
                        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
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
                                await HandleSubscribingForCommands(message, cancellationToken);
                                break;
                            case MessageType.UpdateInfoBasesTaskRequest:
                                await HandleUpdateInfoBasesTaskRequest(message, cancellationToken);
                                break;
                            case MessageType.UpdateInfoBaseTaskLog:
                                await HandleUpdateInfoBasesTaskLog(message, cancellationToken);
                                break;
                            case MessageType.SettingsRequest:
                                await HandleSettingsRequest(message, cancellationToken);
                                break;
                            default:
                                throw new Exception($"Получено неожиданное сообщение: {message.Header.Type}");
                        }
                    }
                    catch (OperationCanceledException) {}
                    catch (Exception ex)
                    {
                        _logger.LogTrace(ex, "Ошибка обработки сообщения");
                        await SendError(message, ex.ToString(), cancellationToken);
                    }
                }
                catch (OperationCanceledException) {}
            }
        }
        
        public async Task HandleSettingsRequest(Message message, CancellationToken cancellationToken)
        {
            var tjSettings = await _appDbContext.TechLogSettings.FirstOrDefaultAsync(cancellationToken);

            var settings = new SettingsDto
            {
                TechLogEnabled = tjSettings?.Enabled ?? false
            };
            
            await Send(MessageType.Settings, settings, message, cancellationToken);
        }

        public async Task SendUpdateSettingsRequest(CancellationToken cancellationToken)
            => await Send(MessageType.UpdateSettingsRequest, cancellationToken);

        public async Task<List<V8Platform>> GetInstalledPlatforms(CancellationToken cancellationToken)
            => await Get<List<V8Platform>>(
                MessageType.InstalledPlatformsRequest, 
                MessageType.InstalledPlatforms,
                cancellationToken);
        
        public async Task<List<V8Cluster>> GetV8Clusters(CancellationToken cancellationToken)
            => await Get<List<V8Cluster>>(
                MessageType.ClustersRequest, 
                MessageType.ClustersResponse,
                cancellationToken);
        
        public async Task<List<RagentService>> GetRagentServices(CancellationToken cancellationToken)
            => await Get<List<RagentService>>(
                MessageType.RagentServicesRequest, 
                MessageType.RagentServices,
                cancellationToken);
        
        public async Task<List<RasService>> GetRasServices(CancellationToken cancellationToken)
            => await Get<List<RasService>>(
                MessageType.RasServicesRequest, 
                MessageType.RasServices,
                cancellationToken);
        
        public async Task<List<V8InfoBaseSummary>> GetV8InfoBasesSummaries(Cluster cluster, CancellationToken cancellationToken)
            => await Get<InfoBasesRequestDto, List<V8InfoBaseSummary>>(
                MessageType.InfoBasesRequest, 
                MessageType.InfoBasesResponse,
                new InfoBasesRequestDto
                {
                    Cluster = new ClusterDto
                    {
                        Id = cluster.ClusterInternalId,
                        Host = cluster.Host,
                        Port = cluster.Port
                    },
                    Credentials = cluster.Credentials switch
                    {
                        null => null,
                        _ => new CredentialsDto
                        {
                            User = cluster.Credentials.User,
                            Password = cluster.Credentials.Password
                        }
                    }
                },
                cancellationToken);
        
        public async Task RequestTechLogSeancesUpdating(CancellationToken cancellationToken)
            => await Send(MessageType.UpdateTechLogSeancesRequest, cancellationToken);
        
        public async Task RequestInfoBasesUpdating(CancellationToken cancellationToken)
            => await Send(MessageType.UpdateInfoBasesRequest, cancellationToken);

        private async Task UpdateTechLogSeances(Message callMessage, CancellationToken cancellationToken)
        {
            var agent = await _appDbContext.Agents.FirstOrDefaultAsync(c => c.Id == AgentInstance!.Id, cancellationToken);

            var agentSeances = await _appDbContext.TechLogSeances
                .AsNoTracking()
                .Include(c => c.Agents)
                .Where(c => c.Agents.Contains(agent!))
                .Include(c => c.Templates)
                .ToListAsync(cancellationToken);

            var seances = new List<TechLogSeanceDto>();

            agentSeances.ForEach(c =>
            {
                StringBuilder templateBuilder = new();

                c.Templates.ForEach(c =>
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

            await Send(MessageType.TechLogSeances, seances, callMessage, cancellationToken);
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
                    var agent = new Agent
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

        private async Task HandleSubscribingForCommands(Message requestMessage, CancellationToken cancellationToken)
        {
            SubscribedForCommands?.Invoke(this);
            await SendOk(requestMessage, cancellationToken);
        }

        private async Task HandleLastFilePositionRequest(Message requestMessage, CancellationToken cancellationToken)
        {
            var request = ParseMessageData<LastFilePositionRequestDto>(requestMessage.Data, cancellationToken);

            var response = await _clickHouseContext.GetLastFilePosition(
                AgentInstance!.Id.ToString(),
                request.SeanceId.ToString(),
                request.TemplateId.ToString(),
                request.Folder,
                request.File,
                cancellationToken);

            await Send(MessageType.LastFilePosition, response, requestMessage, cancellationToken);
        }

        private async Task HandleUpdateInfoBasesTaskLog(Message requestMessage, CancellationToken cancellationToken)
        {
            var result = ParseMessageData<List<UpdateInfoBaseTaskLogItemDto>>(requestMessage.Data, cancellationToken);
            
            var log = _mapper.Map<List<UpdateInfoBaseTaskLogItem>>(result);

            if (log.Count > 0)
            {
                var oldIds = await _appDbContext.UpdateInfoBaseTaskLogItems
                    .Where(c => c.TaskId == log[0].TaskId && c.InfoBaseId == log[0].InfoBaseId)
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);
                
                var logIds = log.Select(c => c.Id).ToList();
                var newIds = logIds.Except(oldIds).ToList();
                
                var newLogItems = log.Where(c => newIds.Contains(c.Id)).ToList();
                
                await _appDbContext.UpdateInfoBaseTaskLogItems.AddRangeAsync(newLogItems, cancellationToken);
                await _appDbContext.SaveChangesAsync(cancellationToken);
            }
            
            await SendOk(requestMessage, cancellationToken);
        }
        
        private async Task HandleUpdateInfoBasesTaskRequest(Message requestMessage, CancellationToken cancellationToken)
        {
            var task = await _appDbContext.UpdateInfoBaseTasks
                .Where(c => c.InfoBases.Any(i => i.Cluster.Agent.Id == AgentInstance!.Id))
                .Include(c => c.Configurations)
                .Include(c => c.InfoBases)
                .ThenInclude(c => c.Credentials)
                .Include(c => c.InfoBases)
                .ThenInclude(c => c.Cluster)
                .ThenInclude(c => c.Credentials)
                .Include(c => c.InfoBases)
                .ThenInclude(c => c.Cluster)
                .ThenInclude(c => c.Agent)
                .OrderByDescending(c => c.StartDateTime)
                .ProjectTo<UpdateInfoBaseTaskDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);
                
            await Send(MessageType.UpdateInfoBasesTask, task!, requestMessage, cancellationToken);
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
