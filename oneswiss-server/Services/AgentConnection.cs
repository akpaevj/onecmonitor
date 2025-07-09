using System.Net.Sockets;
using AutoMapper;
using MessagePack;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using OneSwiss.Common;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Models;
using OneSwiss.Server.Models.MaintenanceTasks;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.RemoteAdministration;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Server.Services
{
    public class AgentConnection : FastConnection
    {
        private readonly IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentConnection> _logger;

        public Guid ConnectionId { get; }
        public AgentInstanceDto? AgentInstance { get; private set; }

        public delegate void AgentConnectedHandler(AgentConnection agentConnection);
        public event AgentConnectedHandler? AgentConnected;

        public delegate void AgentDisconnectedHandler(AgentConnection agentConnection);
        public event AgentDisconnectedHandler? AgentDisconnected;

        public AgentConnection(
            Socket socket, 
            IServiceProvider serviceProvider, 
            ILogger<AgentConnection> logger)
            : base(logger)
        {
            Socket = socket;
            
            Disconnected += (_, _) =>
            {
                AgentDisconnected?.Invoke(this);
            };

            ConnectionId = Guid.NewGuid();
            _mapper = serviceProvider.GetRequiredService<IMapper>();
            _serviceProvider = serviceProvider;
            _logger = logger;
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
                        case MessageType.MaintenanceStepNodeLog:
                            await HandleMaintenanceStepLog(message, cancellationToken);
                            break;
                        case MessageType.SettingsRequest:
                            await HandleSettingsRequest(message, cancellationToken);
                            break;
                        case MessageType.V8FileRequest:
                            await SendFile(message, cancellationToken);
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
        
        private async Task<SettingsDto> GetSettings(CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var eventLogSettings =
                await dbContext.EventLogSettings
                    .AsNoTracking()
                    .Include(c => c.Dbms)
                    .Include(c => c.Credentials)
                    .FirstOrDefaultAsync(cancellationToken) ?? new EventLogSettings();

            var techLogSettings =
                await dbContext.TechLogSettings
                    .AsNoTracking()
                    .Include(c => c.Dbms)
                    .Include(c => c.Credentials)
                    .FirstOrDefaultAsync(cancellationToken) ?? new TechLogSettings();
            
            var techLogSettingsDto = _mapper.Map<TechLogSettingsDto>(techLogSettings);
            techLogSettingsDto.Seances = await GetTechLogSeances(cancellationToken);
            
            var settings = new SettingsDto
            {
                EventLogSettings = _mapper.Map<EventLogSettingsDto>(eventLogSettings),
                TechLogSettings = techLogSettingsDto
            };

            return settings;
        }

        private async Task HandleSettingsRequest(Message message, CancellationToken cancellationToken)
            => await Send(MessageType.Settings, await GetSettings(cancellationToken), message, cancellationToken);

        public async Task SendSettingsRequest(CancellationToken cancellationToken)
            => await Send(MessageType.Settings, await GetSettings(cancellationToken), cancellationToken);

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
        
        public async Task<List<CrServer>> GetCrServerServices(CancellationToken cancellationToken)
            => await Get<List<CrServer>>(
                MessageType.CrServerServicesRequest, 
                MessageType.CrServerServices,
                cancellationToken);
        
        public async Task<List<RasService>> GetRasServices(CancellationToken cancellationToken)
            => await Get<List<RasService>>(
                MessageType.RasServicesRequest, 
                MessageType.RasServices,
                cancellationToken);
        
        public async Task<SystemInfoDto> GetSystemInfo(CancellationToken cancellationToken)
            => await Get<SystemInfoDto>(
                MessageType.SystemInfoRequest, 
                MessageType.SystemInfo,
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
                        Port = cluster.Port,
                        Credentials = cluster.Credentials == null ? null : _mapper.Map<CredentialsDto>(cluster.Credentials)
                    }
                },
                cancellationToken);
        
        public async Task StartMaintenanceTask(MaintenanceTask task, CancellationToken cancellationToken = default)
            => await Send(MessageType.MaintenanceTask, _mapper.Map<MaintenanceTaskDto>(task), cancellationToken);
        
        private async Task<List<TechLogSeanceDto>> GetTechLogSeances(CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var agent = await dbContext.Agents
                .AsNoTracking()
                .Include(c => c.TechLogSeances).ThenInclude(c => c.Templates)
                .FirstOrDefaultAsync(c => c.Id == AgentInstance!.Id, cancellationToken);

            var seances = new List<TechLogSeanceDto>();

            agent!.TechLogSeances.ForEach(seance =>
            {
                seance.Templates.ForEach(template =>
                {
                    seances.Add(new TechLogSeanceDto
                    {
                        Id = seance.Id,
                        StartDateTime = seance.StartDateTime,
                        FinishDateTime = seance.FinishDateTime,
                        TemplateId = template.Id,
                        Template = template.Content
                    }); 
                });
            });

            return seances;
        }

        private async Task HandleInitMessage(Message message, CancellationToken cancellationToken)
        {
            AgentInstance = ParseMessageData<AgentInstanceDto>(message.Data, cancellationToken);

            await using var scope = _serviceProvider.CreateAsyncScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var foundItem = await dbContext.Agents.AsNoTracking().FirstOrDefaultAsync(c => c.Id == AgentInstance.Id, cancellationToken);

                if (foundItem == null)
                {
                    var agent = new Agent
                    {
                        Id = AgentInstance.Id,
                        InstanceName = AgentInstance.InstanceName
                    };
                    dbContext.Agents.Add(agent);

                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                else if (foundItem.InstanceName != AgentInstance.InstanceName)
                {
                    foundItem.InstanceName = AgentInstance.InstanceName;

                    dbContext.Entry(foundItem).State = EntityState.Modified;

                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                await dbContext.Database.CommitTransactionAsync(cancellationToken);

                AgentConnected?.Invoke(this);
            }
            catch (Exception ex)
            {
                await dbContext.Database.RollbackTransactionAsync(cancellationToken);
            }
        }
        
        private async Task HandleMaintenanceStepLog(Message requestMessage, CancellationToken cancellationToken)
        {
            var result = ParseMessageData<List<MaintenanceStepLogItemDto>>(requestMessage.Data, cancellationToken);
            
            var log = _mapper.Map<List<MaintenanceStepLogItem>>(result);

            if (log.Count > 0)
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var taskLogHub = scope.ServiceProvider.GetRequiredService<IHubContext<MaintenanceTaskLogHub>>();
                
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    log.ForEach(c => c.TimeStamp = c.TimeStamp.AddSeconds(AgentInstance!.UtcOffset));
                    await dbContext.MaintenanceStepLogs.AddRangeAsync(log, cancellationToken);
                    
                    await dbContext.SaveChangesAsync(cancellationToken);
                    
                    var task = await dbContext.MaintenanceTasks
                        .Include(c => c.InfoBases)
                        .FirstOrDefaultAsync(c => c.Id == result[0].TaskId, cancellationToken);

                    var infoBasesCount = task!.InfoBases.Count;
                    var finishedCount = await dbContext.MaintenanceStepLogs
                        .AsNoTracking()
                        .Where(c => c.Step.MaintenanceTask.Id == task.Id && c.IsFinish)
                        .CountAsync(cancellationToken);
                    
                    task.IsFaulted = await dbContext.MaintenanceStepLogs
                        .AsNoTracking()
                        .AnyAsync(c => c.Step.MaintenanceTask.Id == task.Id && c.IsError, cancellationToken);
                    
                    if (infoBasesCount == finishedCount)
                        task.FinishDateTime = DateTime.Now;
                    
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await dbContext.Database.CommitTransactionAsync(cancellationToken);
                    
                    await SendOk(requestMessage, cancellationToken);

                    await taskLogHub.Clients.Group(task.Id.ToString()).SendAsync("LogUpdated", cancellationToken: cancellationToken);
                }
                catch (Exception e)
                {
                    await dbContext.Database.RollbackTransactionAsync(cancellationToken);
                    _logger.LogError(e, "Ошибка записи лога шага обслуживания");
                    
                    await SendError(requestMessage, e.Message, cancellationToken);
                }
            }
            else
                await SendOk(requestMessage, cancellationToken);
        }

        private async Task SendFile(Message message, CancellationToken cancellationToken)
        {
            var request = ParseMessageData<FileRequestDto>(message.Data, cancellationToken);
            
            await using var scope = _serviceProvider.CreateAsyncScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var filesProvider = scope.ServiceProvider.GetRequiredService<FilesProvider>();

            var file = await dbContext.Files.FindAsync([request.Id], cancellationToken);
            
            await using var stream = filesProvider.OpenDataStream(file!.DataPath, FileMode.Open);
            var sent = 0L;
            var buffer = new byte[100 * 1024 * 1024];
            
            while (sent < stream.Length)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken);
                
                var chunk = new FileChunkDto
                {
                    Id = request.Id,
                    Data = buffer[..read]
                };
                
                await Send(MessageType.V8FileChunk, chunk, cancellationToken);
                sent += read;
            }
            
            await SendOk(message, cancellationToken);
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
