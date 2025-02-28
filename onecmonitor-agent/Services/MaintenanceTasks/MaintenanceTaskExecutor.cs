using System.Collections.Concurrent;
using OnecMonitor.Agent.Services.InfoBases;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.DTO.MaintenanceTasks;
using OnecMonitor.Common.Extensions;
using OnecMonitor.Common.Models.MaintenanceTasks;
using OneSTools.Common.Designer.Batch;
using OneSTools.Common.Platform.RemoteAdministration;
using OneSTools.Common.Platform.Services;

namespace OnecMonitor.Agent.Services.MaintenanceTasks;

public class MaintenanceTaskExecutor : BackgroundService
{
    private readonly MaintenanceTaskExecutorQueue _queue;
    
    private readonly AsyncServiceScope _scope;
    private readonly OnecMonitorConnection _serverConnection;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly RasHolder _rasHolder;
    private readonly ILogger<MaintenanceTaskExecutor> _logger;
    
    public MaintenanceTaskExecutor(
        IServiceProvider serviceProvider, 
        MaintenanceTaskExecutorQueue tasksQueue, 
        RasHolder rasHolder, 
        IHostApplicationLifetime applicationLifetime, 
        ILogger<MaintenanceTaskExecutor> logger) 
    {
        _scope = serviceProvider.CreateAsyncScope();
        _queue = tasksQueue;
        _serverConnection = _scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
        _applicationLifetime = applicationLifetime;
        _rasHolder = rasHolder;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _serverConnection.Start();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var task = await _queue.DequeueAsync(stoppingToken);

            try
            {
                await StartMaintenanceTask(task, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Ошибка обработки задания обслуживания: {e.Message}");
            }
        }
    }

    private async Task StartMaintenanceTask(MaintenanceTaskDto task, CancellationToken cancellationToken)
    {
        var v8Files = await SaveTaskV8Files(task, cancellationToken);

        await Parallel.ForEachAsync(task.InfoBases, cancellationToken, async (infoBase, stoppingToken) =>
        {
            var log = new List<MaintenanceStepNodeLogItemDto>();
            
            var context = new MaintenanceStepNodeContext
            {
                InfoBase = infoBase,
                Log = log,
                V8Files = v8Files,
                Node = task.RootNode
            };

            try
            {
                var ragent = V8Services.GetActiveRagentForClusterPort(infoBase.Cluster.Port);
                var ras = _rasHolder.GetActiveRasForRagent(ragent);
            
                context.Rac = Rac.GetRacForRasService(ras);
                context.Platform = ragent.Platform;
                            
                if (!context.Platform.HasOnecV8)
                    throw new Exception("Для платформы агента не установлен конфигуратор");
            
                var currentNode = task.RootNode;

                while (!stoppingToken.IsCancellationRequested && currentNode != null)
                {
                    context.Node = currentNode;
                
                    if (currentNode.Kind == MaintenanceStepNodeKind.TryCatch)
                    {
                        try
                        {
                            HandleTaskStepNode(context);
                            currentNode = currentNode.LeftNode;
                        }
                        catch (Exception e)
                        {
                            AddLogItem(context, e.ToString(), true);
                            currentNode = currentNode!.RightNode;
                        }
                    }
                    else
                    {
                        HandleTaskStepNode(context);
                        currentNode = currentNode.LeftNode;
                    }
                }

                AddLogItem(context, "Завершено", false, true);
                await SendLog(log, stoppingToken);
            }
            catch (Exception e)
            {
                AddLogItem(context, e.ToString(), true, true);
                await SendLog(log, stoppingToken);
            }
        });
    }

    private static void HandleTaskStepNode(MaintenanceStepNodeContext context)
    {
        AddLogItem(context, $"Обработка шага \"{context.Node.Step.Kind.GetDisplay()}\"");

        switch (context.Node.Step.Kind)
        {
            case MaintenanceStepKind.LockConnections:
                LockConnections(context);
                break;
            case MaintenanceStepKind.CloseConnections:
                CloseConnections(context);
                break;
            case MaintenanceStepKind.UnlockConnections:
                UnlockConnections(context);
                break;
            case MaintenanceStepKind.LoadExtension:
                LoadExtension(context);
                break;
            case MaintenanceStepKind.UpdateConfiguration:
                UpdateConfiguration(context);
                break;
            case MaintenanceStepKind.LoadConfiguration:
                LoadConfiguration(context);
                break;
            case MaintenanceStepKind.StartExternalDataProcessor:
                StartExternalDataProcessor(context);
                break;
            default:
                throw new Exception($"Неизвестный тип шага \"{context.Node.Step.Kind.GetDisplay()}\"");
        }
    }
    
    private static void AddLogItem(MaintenanceStepNodeContext context, string message, bool isError = false, bool isFinish = false)
    {
        context.Log.Add(new MaintenanceStepNodeLogItemDto
        {
            Id = Guid.NewGuid(),
            Message = message,
            IsError = isError,
            IsFinish = isFinish,
            TimeStamp = DateTime.Now,
            InfoBaseId = context.InfoBase.Id,
            StepNodeId = context.Node.Id
        });
    }
    
    private async Task SendLog(List<MaintenanceStepNodeLogItemDto> log, CancellationToken cancellationToken)
        => await _serverConnection.Send(MessageType.MaintenanceStepNodeLog, log, cancellationToken);

    private static async Task<ConcurrentDictionary<Guid, string>> SaveTaskV8Files(MaintenanceTaskDto task,
        CancellationToken cancellationToken)
    {
        var files = new ConcurrentDictionary<Guid, string>();
        await SaveStepNodeV8File(task.RootNode, files, cancellationToken);
        
        return files;
    }

    private static async Task SaveStepNodeV8File(MaintenanceStepNodeDto node, ConcurrentDictionary<Guid, string> files, CancellationToken cancellationToken)
    {
        if (node.Step.File != null && !files.ContainsKey(node.Step.File.Id))
        {
            var path = Path.Join(Path.GetTempPath(), $"{node.Step.File.Id}{node.Step.File.FileExtension}") ;

            if (!File.Exists(path))
            {
                await using var file = File.Create(path);
                file.Write(node.Step.File.Data);
                file.Close();
            }
            
            files.TryAdd(node.Step.File.Id, path);
            node.Step.File.Data = null!;
        }
        
        if (node.LeftNode != null)
            await SaveStepNodeV8File(node.LeftNode, files, cancellationToken);
        
        if (node.RightNode != null)
            await SaveStepNodeV8File(node.RightNode, files, cancellationToken);
    }

    private static void LockConnections(MaintenanceStepNodeContext context)
    {
        context.Rac.BlockConnections(
            context.InfoBase.Cluster.Id, 
            context.InfoBase.InfoBaseInternalId,
            context.InfoBase.Credentials.User,
            context.InfoBase.Credentials.Password,
            context.Node.Step.AccessCode,
            context.Node.Step.Message);
        
        context.AccessCode = context.Node.Step.AccessCode;
    }
    
    private static void CloseConnections(MaintenanceStepNodeContext context)
    {
        var sessions = context.Rac.GetInfoBaseSessions(context.InfoBase.Cluster.Id, context.InfoBase.InfoBaseInternalId);
        sessions
            .Where(c => !c.AppId.Contains("RAS", StringComparison.CurrentCultureIgnoreCase))
            .ToList()
            .ForEach(s =>
            {
                try
                {
                    context.Rac.TerminateSession(context.InfoBase.Cluster.Id, s.Id);
                }
                catch
                {
                    // Игнорируем, т.к. сеанс уже мог быть закрыт, мог быть повисшим и т.п.
                }
            });
    }
    
    private static void UnlockConnections(MaintenanceStepNodeContext context)
    {
        context.Rac.UnblockConnections(
            context.InfoBase.Cluster.Id, 
            context.InfoBase.InfoBaseInternalId,
            context.InfoBase.Credentials.User,
            context.InfoBase.Credentials.Password);
    }
    
    private static void LoadExtension(MaintenanceStepNodeContext context)
    {
        var filePath = context.V8Files[context.Node.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.LoadExtension(
            context.Node.Step.File.Name, 
            filePath, 
            context.InfoBase.Credentials.User, 
            context.InfoBase.Credentials.Password, 
            context.AccessCode,
            true);
    }

    private static void LoadConfiguration(MaintenanceStepNodeContext context)
    {
        var filePath = context.V8Files[context.Node.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.LoadConfiguration(
            filePath, 
            context.InfoBase.Credentials.User, 
            context.InfoBase.Credentials.Password, 
            context.AccessCode,
            true);
    }
    
    private static void UpdateConfiguration(MaintenanceStepNodeContext context)
    {
        var filePath = context.V8Files[context.Node.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.UpdateConfiguration(
            filePath, 
            context.InfoBase.Credentials.User, 
            context.InfoBase.Credentials.Password, 
            context.AccessCode,
            true);
    }

    private static void StartExternalDataProcessor(MaintenanceStepNodeContext context)
    {
        var filePath = context.V8Files[context.Node.Step.File!.Id];
        
        var batch = context.GetBatchEnterprise();
        batch.ExecuteExternalDataProcessor(
            filePath, 
            context.InfoBase.Credentials.User, 
            context.InfoBase.Credentials.Password, 
            context.AccessCode,
            true);
    }
}