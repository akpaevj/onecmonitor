using System.Collections.Concurrent;
using OnecMonitor.Agent.Extensions;
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
    private readonly RasHolder _rasHolder;
    private readonly ILogger<MaintenanceTaskExecutor> _logger;
    
    public MaintenanceTaskExecutor(
        IServiceProvider serviceProvider, 
        MaintenanceTaskExecutorQueue tasksQueue, 
        RasHolder rasHolder,
        ILogger<MaintenanceTaskExecutor> logger) 
    {
        _scope = serviceProvider.CreateAsyncScope();
        _queue = tasksQueue;
        _serverConnection = _scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
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
            var log = new List<MaintenanceStepLogItemDto>();
            
            var context = new MaintenanceStepContext
            {
                InfoBase = infoBase,
                Log = log,
                V8Files = v8Files,
                Step = task.Steps.GetRootStep()
            };

            try
            {
                var ragent = V8Services.GetActiveRagentForClusterPort(infoBase.Cluster.Port);
                var ras = _rasHolder.GetActiveRasForRagent(ragent);
            
                context.Rac = Rac.GetRacForRasService(ras);
                context.Platform = ragent.Platform;
                            
                if (!context.Platform.HasOnecV8)
                    throw new Exception("Для платформы агента не установлен конфигуратор");

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (context.Step.NodeKind == MaintenanceStepNodeKind.TryCatch)
                    {
                        try
                        {
                            HandleTaskStepNode(context);
                            
                            if (context.Step.LeftStepId is not null)
                                context.Step = task.Steps.GetStep(context.Step.LeftStepId);
                            else
                                break;
                        }
                        catch (Exception e)
                        {
                            AddLogItem(context, e.ToString(), true);
                            
                            if (context.Step.RightStepId is not null)
                                context.Step = task.Steps.GetStep(context.Step.RightStepId);
                            else
                                break;
                        }
                    }
                    else
                    {
                        HandleTaskStepNode(context);
                        
                        if (context.Step.LeftStepId is not null)
                            context.Step = task.Steps.GetStep(context.Step.LeftStepId);
                        else
                            break;
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

        foreach (var file in v8Files.Values)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // ignore
            }
        }
    }

    private static void HandleTaskStepNode(MaintenanceStepContext context)
    {
        AddLogItem(context, $"Обработка шага \"{context.Step.Kind.GetDisplay()}\"");

        switch (context.Step.Kind)
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
                throw new Exception($"Неизвестный тип шага \"{context.Step.Kind.GetDisplay()}\"");
        }
    }
    
    private static void AddLogItem(MaintenanceStepContext context, string message, bool isError = false, bool isFinish = false)
    {
        context.Log.Add(new MaintenanceStepLogItemDto
        {
            Id = Guid.NewGuid(),
            Message = message,
            IsError = isError,
            IsFinish = isFinish,
            TimeStamp = DateTime.Now,
            InfoBaseId = context.InfoBase.Id,
            StepId = context.Step.Id
        });
    }
    
    private async Task SendLog(List<MaintenanceStepLogItemDto> log, CancellationToken cancellationToken)
        => await _serverConnection.Send(MessageType.MaintenanceStepNodeLog, log, cancellationToken);

    private static async Task<ConcurrentDictionary<Guid, string>> SaveTaskV8Files(MaintenanceTaskDto task,
        CancellationToken cancellationToken)
    {
        var files = new ConcurrentDictionary<Guid, string>();
        await SaveStepNodeV8File(task.Steps, files, cancellationToken);
        
        return files;
    }

    private static async Task SaveStepNodeV8File(List<MaintenanceStepDto> steps, ConcurrentDictionary<Guid, string> files, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            if (step.File == null || files.ContainsKey(step.File.Id)) 
                continue;
            
            var path = Path.Join(Path.GetTempPath(), $"{step.File.Id}{step.File.FileExtension}") ;

            if (!File.Exists(path))
            {
                await using var file = File.Create(path);
                await file.WriteAsync(step.File.Data, cancellationToken);
                file.Close();
            }
            
            files.TryAdd(step.File.Id, path);
            step.File.Data = null!;
        }
    }

    private static void LockConnections(MaintenanceStepContext context)
    {
        context.Rac.BlockConnections(
            context.InfoBase.Cluster.Id, 
            context.InfoBase.InfoBaseInternalId,
            context.InfoBase.Credentials.User,
            context.InfoBase.Credentials.Password,
            context.Step.AccessCode,
            context.Step.Message);
        
        context.AccessCode = context.Step.AccessCode;
    }
    
    private static void CloseConnections(MaintenanceStepContext context)
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
    
    private static void UnlockConnections(MaintenanceStepContext context)
    {
        context.Rac.UnblockConnections(
            context.InfoBase.Cluster.Id, 
            context.InfoBase.InfoBaseInternalId,
            context.InfoBase.Credentials.User,
            context.InfoBase.Credentials.Password);
    }
    
    private static void LoadExtension(MaintenanceStepContext context)
    {
        var filePath = context.V8Files[context.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.LoadExtension(
            context.Step.File!.Name, 
            filePath, 
            context.InfoBase.Credentials.User, 
            context.InfoBase.Credentials.Password, 
            context.AccessCode,
            true);
    }

    private static void LoadConfiguration(MaintenanceStepContext context)
    {
        var filePath = context.V8Files[context.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.LoadConfiguration(
            filePath, 
            context.InfoBase.Credentials.User, 
            context.InfoBase.Credentials.Password, 
            context.AccessCode,
            true);
    }
    
    private static void UpdateConfiguration(MaintenanceStepContext context)
    {
        var filePath = context.V8Files[context.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.UpdateConfiguration(
            filePath, 
            context.InfoBase.Credentials.User, 
            context.InfoBase.Credentials.Password, 
            context.AccessCode,
            true);
    }

    private static void StartExternalDataProcessor(MaintenanceStepContext context)
    {
        var filePath = context.V8Files[context.Step.File!.Id];
        
        var batch = context.GetBatchEnterprise();
        batch.ExecuteExternalDataProcessor(
            filePath, 
            context.InfoBase.Credentials.User, 
            context.InfoBase.Credentials.Password, 
            context.AccessCode,
            true);
    }
}