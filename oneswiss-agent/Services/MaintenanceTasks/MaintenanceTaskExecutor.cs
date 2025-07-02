using System.CommandLine.Parsing;
using OneSwiss.Agent.Extensions;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Extensions;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.OneScript;
using OneSwiss.V8.Platform.RemoteAdministration;

namespace OneSwiss.Agent.Services.MaintenanceTasks;

public class MaintenanceTaskExecutor : BackgroundService
{
    private readonly AsyncServiceScope _scope;
    private readonly OnecMonitorConnection _serverConnection;
    private readonly MonitorQueue<MaintenanceTaskDto> _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly RasHolder _rasHolder;
    private readonly V8ServicesProvider _v8ServicesProvider;
    private readonly ILogger<MaintenanceTaskExecutor> _logger;
    
    public MaintenanceTaskExecutor(
        IServiceProvider serviceProvider, 
        MonitorQueue<MaintenanceTaskDto> queue,
        RasHolder rasHolder,
        V8ServicesProvider v8ServicesProvider,
        ILogger<MaintenanceTaskExecutor> logger) 
    {
        _serviceProvider = serviceProvider;
        _scope = serviceProvider.CreateAsyncScope();
        _queue = queue;
        _serverConnection = _scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
        _rasHolder = rasHolder;
        _v8ServicesProvider = v8ServicesProvider;
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
            // Перекопируем файлы для каждого задания, т.к. 1С на кой-то хрен нужен монопольный доступ к файлам CF, CFE, CFU
            var localFiles = CopyFilesForInfoBase(v8Files);
            
            var log = new List<MaintenanceStepLogItemDto>();
            
            var context = new MaintenanceStepContext
            {
                Task = task,
                InfoBase = infoBase,
                Log = log,
                Files = localFiles,
                Step = task.Steps.GetRootStep()
            };

            try
            {
                var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(infoBase.Cluster.Port);
                var ras = _rasHolder.GetActiveRasForRagent(ragent);

                context.Rac = Rac.GetRacForRasService(ras);
                context.Platform = ragent.Platform;

                if (!context.Platform.HasOnecV8)
                    throw new Exception("Для платформы агента не установлен конфигуратор");

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (log.Count > 0)
                    {
                        await SendLog(log, stoppingToken);
                        log.Clear();
                    }

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
            finally
            {
                DeleteV8Files(localFiles);
            }
        });
        
        DeleteV8Files(v8Files);
    }

    private static Dictionary<Guid, string> CopyFilesForInfoBase(Dictionary<Guid, string> files)
    {
        var result = new Dictionary<Guid, string>();

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.Value);
            var path = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
            File.Copy(file.Value, path);
            
            result.Add(file.Key, path);
        }

        return result;
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
            case MaintenanceStepKind.DeleteExtension:
                DeleteExtension(context);
                break;
            case MaintenanceStepKind.StartExternalDataProcessor:
                StartExternalDataProcessor(context);
                break;
            case MaintenanceStepKind.ExecuteOneScript:
                ExecuteOneScript(context);
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
            StepId = context.Step.Id,
            TaskId = context.Task.Id
        });
    }
    
    private async Task SendLog(List<MaintenanceStepLogItemDto> log, CancellationToken cancellationToken)
        => await _serverConnection.Send(MessageType.MaintenanceStepNodeLog, log, cancellationToken);

    private async Task<Dictionary<Guid, string>> SaveTaskV8Files(MaintenanceTaskDto task,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        using var downloader = scope.ServiceProvider.GetRequiredService<V8FilesDownloader>();

        var filesToDownload = task.Steps
            .Where(c => c.File is not null)
            .Select(c => c.File!)
            .DistinctBy(c => c.Id)
            .ToList();

        return await downloader.Download(_serverConnection, filesToDownload, cancellationToken);
    }

    private static void DeleteV8Files(Dictionary<Guid, string> files)
    {
        foreach (var file in files.Values)
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

    private static void LockConnections(MaintenanceStepContext context)
    {
        context.Rac.BlockConnections(
            context.InfoBase.Cluster.ClusterInternalId, 
            context.InfoBase.InfoBaseInternalId,
            context.Step.AccessCode,
            context.Step.Message,
            context.InfoBase.Cluster.Credentials?.User ?? "",
            context.InfoBase.Cluster.Credentials?.Password ?? "",
            context.InfoBase.Credentials?.User ?? "",
            context.InfoBase.Credentials?.Password ?? "");
        
        context.AccessCode = context.Step.AccessCode;
    }
    
    private static void CloseConnections(MaintenanceStepContext context)
    {
        var sessions = context.Rac.GetInfoBaseSessions(
            context.InfoBase.Cluster.ClusterInternalId, 
            context.InfoBase.InfoBaseInternalId,
            context.InfoBase.Cluster.Credentials?.User ?? "",
            context.InfoBase.Cluster.Credentials?.Password ?? "");
        
        sessions
            .Where(c => !c.AppId.Contains("RAS", StringComparison.CurrentCultureIgnoreCase))
            .ToList()
            .ForEach(s =>
            {
                try
                {
                    context.Rac.TerminateSession(
                        context.InfoBase.Cluster.ClusterInternalId, 
                        s.Id,
                        context.InfoBase.Cluster.Credentials?.User ?? "",
                        context.InfoBase.Cluster.Credentials?.Password ?? "");
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
            context.InfoBase.Cluster.ClusterInternalId, 
            context.InfoBase.InfoBaseInternalId,
            context.InfoBase.Cluster.Credentials?.User ?? "",
            context.InfoBase.Cluster.Credentials?.Password ?? "",
            context.InfoBase.Credentials?.User ?? "",
            context.InfoBase.Credentials?.Password ?? "");
    }
    
    private static void LoadExtension(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.LoadExtension(
            context.Step.File!.Name, 
            filePath, 
            context.InfoBase.Credentials?.User ?? "", 
            context.InfoBase.Credentials?.Password ?? "", 
            context.AccessCode,
            true);
        
        AddLogItem(context, batch.OutFileContent);
    }
    
    private static void ExecuteOneScript(MaintenanceStepContext context)
    {
        var scriptPath = Directory.CreateTempSubdirectory().FullName;
        
        var filePath = context.Files[context.Step.File!.Id];
        var opmMetadata = OneScriptPackageReader.Unzip(filePath, scriptPath);

        var scriptHost = new OneScriptExecutor();
        scriptHost.OnEcho += (_, tuple) =>
        {
            AddLogItem(context, tuple.Message);
        };
        scriptHost.OnError += (_, ex) =>
        {
            AddLogItem(context, ex.Message, true);
        };

        var cmdParser = new Parser();
        var parsingResult = cmdParser.Parse(context.Step.CommandLineArguments);
        
        scriptHost.ExecutePackageScript(scriptPath, opmMetadata!, []);
        
        Directory.Delete(scriptPath, true);
    }
    
    private static void DeleteExtension(MaintenanceStepContext context)
    {
        using var batch = context.GetBatchDesigner();
        batch.DeleteExtension(
            context.Step.ExtensionName, 
            context.InfoBase.Credentials?.User ?? "", 
            context.InfoBase.Credentials?.Password ?? "", 
            context.AccessCode,
            true);
        
        AddLogItem(context, batch.OutFileContent);
    }

    private static void LoadConfiguration(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.LoadConfiguration(
            filePath, 
            context.InfoBase.Credentials?.User ?? "", 
            context.InfoBase.Credentials?.Password ?? "", 
            context.AccessCode,
            true);
        
        AddLogItem(context, batch.OutFileContent);
    }
    
    private static void UpdateConfiguration(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.File!.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.UpdateConfiguration(
            filePath, 
            context.InfoBase.Credentials?.User ?? "", 
            context.InfoBase.Credentials?.Password ?? "", 
            context.AccessCode,
            true);
        
        AddLogItem(context, batch.OutFileContent);
    }

    private static void StartExternalDataProcessor(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.File!.Id];
        
        var batch = context.GetBatchEnterprise();
        batch.ExecuteExternalDataProcessor(
            filePath, 
            context.InfoBase.Credentials?.User ?? "", 
            context.InfoBase.Credentials?.Password ?? "", 
            context.AccessCode,
            true);
    }

    public override void Dispose()
    {
        _scope.Dispose();
        base.Dispose();
    }
}