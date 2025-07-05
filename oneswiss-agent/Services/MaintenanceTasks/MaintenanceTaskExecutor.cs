using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using OneSwiss.Agent.Extensions;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Extensions;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.OneScript;
using OneSwiss.V8.Designer.Agent;
using OneSwiss.V8.Designer.Batch;
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

        /*var hasStepsForDesigner = HasStepsForDesigner(task);
        var canUseDesignerAgent = CanUseDesignerAgent(task);*/
        var hasStepsForDesigner = true;
        var canUseDesignerAgent = false;
        
        await Parallel.ForEachAsync(task.InfoBases, cancellationToken, async (infoBase, stoppingToken) =>
        {
            var log = new List<MaintenanceStepLogItemDto>();
            
            var context = new MaintenanceStepContext
            {
                Task = task,
                InfoBase = infoBase,
                Log = log,
                Step = task.Steps.GetRootStep(),
                UseDesignerAgent = canUseDesignerAgent
            };

            OnecV8BatchMode? agent = null;

            try
            {
                var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(infoBase.Cluster.Port);
                var ras = _rasHolder.GetActiveRasForRagent(ragent);

                context.Rac = Rac.GetRacForRasService(ras);
                context.Platform = ragent.Platform;

                if (!context.Platform.HasOnecV8)
                    throw new Exception("Для платформы агента не установлен конфигуратор");
                
                var localFilesFolder = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString(), "0");
                Directory.CreateDirectory(localFilesFolder);
                
                if (hasStepsForDesigner && canUseDesignerAgent)
                {
                    agent = context.StartDesignerAgent(Path.GetDirectoryName(localFilesFolder)!);
                
                    context.DesignerAgentClient = new DesignerAgentClient(
                        context.InfoBase.Credentials?.User ?? "",
                        context.InfoBase.Credentials?.Password ?? "");

                    if (!await context.DesignerAgentClient.WaitAgentAvailable(TimeSpan.FromMinutes(5)))
                        throw new Exception("Таймаут подключения к агенту конфигуратора");

                    await context.DesignerAgentClient.Connect(stoppingToken);
                    await context.DesignerAgentClient.ConnectIb();
                }
                
                // Перекопируем файлы для каждого задания, т.к. 1С на кой-то хрен нужен монопольный доступ к файлам CF, CFE, CFU
                // и сделаем это обязательно после запуска агента конфигуратора, иначе он затрет их при запуске
                var localFiles = CopyFilesForInfoBase(v8Files, localFilesFolder);
                context.Files = localFiles;

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
                            await HandleTaskStepNode(context);

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
                        await HandleTaskStepNode(context);

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
                if (context.UseDesignerAgent)
                {
                    try
                    {
                        context.DesignerAgentClient?.DisconnectIb();
                    }
                    catch
                    {
                        // ignored
                    }

                    context.DesignerAgentClient?.Dispose();
                }
                    
                agent?.Dispose();
                DeleteV8Files(context.Files, true);
            }
        });
        
        DeleteV8Files(v8Files, false);
    }

    private static Dictionary<Guid, string> CopyFilesForInfoBase(Dictionary<Guid, string> files, string folder)
    {
        var result = new Dictionary<Guid, string>();

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.Value);
            var path = Path.Join(folder, $"{Guid.NewGuid()}{extension}");
            File.Copy(file.Value, path);
            
            result.Add(file.Key, path);
        }

        return result;
    }

    private static async Task HandleTaskStepNode(MaintenanceStepContext context)
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
                await LoadExtension(context);
                break;
            case MaintenanceStepKind.UpdateConfiguration:
                UpdateConfiguration(context);
                break;
            case MaintenanceStepKind.LoadConfiguration:
                await LoadConfiguration(context);
                break;
            case MaintenanceStepKind.DeleteExtension:
                await DeleteExtension(context);
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
        using var downloader = scope.ServiceProvider.GetRequiredService<FilesDownloader>();

        var filesToDownload = task.Steps
            .Where(c => c.File is not null)
            .Select(c => c.File!)
            .DistinctBy(c => c.Id)
            .ToList();

        return await downloader.Download(_serverConnection, filesToDownload, cancellationToken);
    }

    private static void DeleteV8Files(Dictionary<Guid, string> files, bool isLocal)
    {
        if (files.Count == 0)
            return;
        
        // Если это локальные файлы, то можем затереть сразу весь каталог
        if (isLocal)
        {
            var folder = Path.GetDirectoryName(Path.GetDirectoryName(files.Values.First()));
            Directory.Delete(folder!, true);
        }
        else
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
    
    private static async Task LoadExtension(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.File!.Id];

        if (context.UseDesignerAgent)
        {
            await context.DesignerAgentClient!.LoadExtension(Path.GetFileName(filePath), context.Step.ExtensionName);
            AddLogItem(context, $"Загрузка расширения \"{context.Step.ExtensionName}\" выполнена");
            
            context.DesignerAgentClient!.UpdateDbCfgExtension(context.Step.ExtensionName);
            await context.DesignerAgentClient.ReadMessagesTillSuccess(message =>
                LogDesignerAgentMessage(context, message));
        }
        else
        {
            using var batch = context.GetBatchDesigner();
            batch.LoadExtension(
                context.Step.ExtensionName, 
                filePath, 
                context.InfoBase.Credentials?.User ?? "", 
                context.InfoBase.Credentials?.Password ?? "", 
                context.AccessCode,
                true);
        
            AddLogItem(context, batch.OutFileContent);
        }
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
    
    private static async Task DeleteExtension(MaintenanceStepContext context)
    {
        if (context.UseDesignerAgent)
        {
            var allExtensions = await context.DesignerAgentClient!.GetAllExtensions();
            var extensionsToDeleting = allExtensions.Where(c => Regex.IsMatch(c.Name, context.Step.ExtensionName)).ToList();

            foreach (var extension in extensionsToDeleting)
            {
                await context.DesignerAgentClient!.DeleteExtension(extension.Name);
                AddLogItem(context, $"Расширение \"{extension.Name}\" удалено");
            }
        }
        else
        {
            using var batchGet = context.GetBatchDesigner();
            var allExtensions = batchGet.GetExtensionsList(
                context.InfoBase.Credentials?.User ?? "", 
                context.InfoBase.Credentials?.Password ?? "", 
                context.AccessCode,
                true);
            
            var extensionsToDeleting = allExtensions.Where(c => Regex.IsMatch(c, context.Step.ExtensionName)).ToList();
            
            foreach (var extension in extensionsToDeleting)
            {
                using var batchDeleting = context.GetBatchDesigner();
                batchDeleting.DeleteExtension(
                    extension, 
                    context.InfoBase.Credentials?.User ?? "", 
                    context.InfoBase.Credentials?.Password ?? "", 
                    context.AccessCode,
                    true);
        
                AddLogItem(context, batchDeleting.OutFileContent);
            }
        }
    }

    private static async Task LoadConfiguration(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.File!.Id];

        if (context.UseDesignerAgent)
        {
            await context.DesignerAgentClient!.LoadCfg(Path.GetFileName(filePath));
            AddLogItem(context, "Загрузка конфигурации выполнена");
            
            context.DesignerAgentClient!.UpdateDbCfg();
            await context.DesignerAgentClient.ReadMessagesTillSuccess(message =>
                LogDesignerAgentMessage(context, message));
        }
        else
        {
            using var batch = context.GetBatchDesigner();
            batch.LoadConfiguration(
                filePath, 
                context.InfoBase.Credentials?.User ?? "", 
                context.InfoBase.Credentials?.Password ?? "", 
                context.AccessCode,
                true);
        
            AddLogItem(context, batch.OutFileContent);
        }
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

    private static bool HasStepsForDesigner(MaintenanceTaskDto task)
    {
        var has = false;

        foreach (var step in task.Steps)
        {
            if (step.Kind is MaintenanceStepKind.DeleteExtension or 
                MaintenanceStepKind.LoadConfiguration or 
                MaintenanceStepKind.LoadExtension or 
                MaintenanceStepKind.UpdateConfiguration)
                has = true;
        }
        
        return has;
    }

    private static void LogDesignerAgentMessage(MaintenanceStepContext context, DesignerAgentMessage message)
    {
        if (message.Type == "log")
        {
            if (message.Message.StartsWith("(!)"))
                AddLogItem(context, message.Message[3..], true);
            else
                AddLogItem(context, message.Message);
        }
        else
            throw new Exception($"Неожиданный тип сообщения: {message.Type}");
    }

    private static bool CanUseDesignerAgent(MaintenanceTaskDto task)
        => task.Steps.FirstOrDefault(c => c.Kind == MaintenanceStepKind.UpdateConfiguration) == null;

    public override void Dispose()
    {
        _scope.Dispose();
        base.Dispose();
    }
}