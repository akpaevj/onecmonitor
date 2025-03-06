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
using OnecMonitor.Common.DTO.MaintenanceTasks;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models.MaintenanceTasks;
using OneSTools.Common.Platform;
using OneSTools.Common.Platform.RemoteAdministration;
using OneSTools.Common.Platform.Services;

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
        public AgentInstanceDto? AgentInstance { get; private set; }

        public delegate void AgentConnectedHandler(AgentConnection agentConnection);
        public event AgentConnectedHandler? AgentConnected;

        public delegate void AgentDisconnectedHandler(AgentConnection agentConnection);
        public event AgentDisconnectedHandler? AgentDisconnected;

        public AgentConnection(Socket socket, TechLogProcessor techLogProcessor, IServiceProvider serviceProvider)
            : base(serviceProvider.GetRequiredService<ILogger<AgentConnection>>())
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

        public void Listen(CancellationToken cancellationToken)
        {
            MessageReceived += async (_, message) =>
            {
                try
                {
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (message.Header.Type)
                    {
                        case MessageType.AgentInfo:
                            await HandleInitMessage(message, cancellationToken);
                            break;
                        case MessageType.TechLogEventContent:
                            await HandleTechLogEventContent(message.Data, cancellationToken);
                            break;
                        case MessageType.LastFilePositionRequest:
                            await HandleLastFilePositionRequest(message, cancellationToken);
                            break;
                        case MessageType.TechLogSeancesRequest:
                            await UpdateTechLogSeances(message, cancellationToken);
                            break;
                        case MessageType.MaintenanceStepNodeLog:
                            await HandleMaintenanceStepLog(message, cancellationToken);
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
            };
            
            RunStreamLoops(cancellationToken);
        }

        private async Task HandleSettingsRequest(Message message, CancellationToken cancellationToken)
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
        
        public async Task StartMaintenanceTask(MaintenanceTask task, CancellationToken cancellationToken)
            => await Send(MessageType.MaintenanceTask, _mapper.Map<MaintenanceTaskDto>(task), cancellationToken);

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

            agentSeances.ForEach(seance =>
            {
                StringBuilder templateBuilder = new();

                seance.Templates.ForEach(template =>
                {
                    // add template id and combine templates
                    templateBuilder.AppendLine(template.Content.Replace("{LOG_PATH}", $"{{LOG_PATH}}{template.Id}"));
                });

                seances.Add(new TechLogSeanceDto()
                {
                    Id = seance.Id,
                    StartDateTime = seance.StartDateTime,
                    FinishDateTime = seance.FinishDateTime,
                    Template = templateBuilder.ToString()
                });
            });

            await Send(MessageType.TechLogSeances, seances, callMessage, cancellationToken);
        }

        private async Task HandleInitMessage(Message message, CancellationToken cancellationToken)
        {
            AgentInstance = ParseMessageData<AgentInstanceDto>(message.Data, cancellationToken);

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
            catch
            {
                await _appDbContext.Database.RollbackTransactionAsync(cancellationToken);
            }
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
        
        private async Task HandleMaintenanceStepLog(Message requestMessage, CancellationToken cancellationToken)
        {
            var result = ParseMessageData<List<MaintenanceStepLogItemDto>>(requestMessage.Data, cancellationToken);
            
            var log = _mapper.Map<List<MaintenanceStepLogItem>>(result);

            if (log.Count > 0)
            {
                await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    log.ForEach(c => c.TimeStamp = c.TimeStamp.AddSeconds(AgentInstance!.UtcOffset));
                    await _appDbContext.MaintenanceStepLogs.AddRangeAsync(log, cancellationToken);
                    
                    await _appDbContext.SaveChangesAsync(cancellationToken);
                    
                    var task = await _appDbContext.MaintenanceTasks
                        .Include(c => c.InfoBases)
                        .FirstOrDefaultAsync(c => c.Id == result[0].TaskId, cancellationToken);

                    var infoBasesCount = task!.InfoBases.Count;
                    var finishedCount = await _appDbContext.MaintenanceStepLogs
                        .AsNoTracking()
                        .Where(c => c.Step.MaintenanceTask.Id == task.Id && c.IsFinish)
                        .CountAsync(cancellationToken);
                    
                    task.IsFaulted = await _appDbContext.MaintenanceStepLogs
                        .AsNoTracking()
                        .AnyAsync(c => c.Step.MaintenanceTask.Id == task.Id && c.IsError, cancellationToken);
                    
                    if (infoBasesCount == finishedCount)
                        task.FinishDateTime = DateTime.Now;
                    
                    await _appDbContext.SaveChangesAsync(cancellationToken);
                    await _appDbContext.Database.CommitTransactionAsync(cancellationToken);
                    
                    await SendOk(requestMessage, cancellationToken);
                }
                catch (Exception e)
                {
                    await _appDbContext.Database.RollbackTransactionAsync(cancellationToken);
                    _logger.LogError(e, "Ошибка записи лога шага обслуживания");
                    
                    await SendError(requestMessage, e.Message, cancellationToken);
                }
            }
            else
                await SendOk(requestMessage, cancellationToken);
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
