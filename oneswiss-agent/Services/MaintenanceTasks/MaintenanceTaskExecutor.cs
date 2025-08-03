using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using OneSwiss.Agent.Extensions;
using OneSwiss.Agent.Helpers;
using OneSwiss.Agent.Oscript;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Extensions;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.OneScript;
using OneSwiss.V8.Designer.Agent;
using OneSwiss.V8.Designer.Batch;
using OneSwiss.V8.Platform.RemoteAdministration;
using ScriptEngine.Hosting;

namespace OneSwiss.Agent.Services.MaintenanceTasks;

public class MaintenanceTaskExecutor : BackgroundService
{
    private readonly AsyncServiceScope _scope;
    private readonly OneSwissConnection _serverConnection;
    private readonly MonitorQueue<MaintenanceTaskDto> _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly RasHolder _rasHolder;
    private readonly V8ServicesProvider _v8ServicesProvider;
    private readonly OscriptIntegrationGlobalContext _oscriptIntegrationGlobalContext;
    private readonly ILogger<MaintenanceTaskExecutor> _logger;
    private readonly ILogger<Rac> _racLogger;
    
    public MaintenanceTaskExecutor(
        IServiceProvider serviceProvider, 
        MonitorQueue<MaintenanceTaskDto> queue,
        RasHolder rasHolder,
        V8ServicesProvider v8ServicesProvider,
        OscriptIntegrationGlobalContext oscriptIntegrationGlobalContext,
        ILogger<MaintenanceTaskExecutor> logger,
        ILogger<Rac> racLogger)
    {
        _racLogger = racLogger;
        _oscriptIntegrationGlobalContext = oscriptIntegrationGlobalContext;
        _serviceProvider = serviceProvider;
        _scope = serviceProvider.CreateAsyncScope();
        _queue = queue;
        _serverConnection = _scope.ServiceProvider.GetRequiredService<OneSwissConnection>();
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
                if (task.CommonDestination)
                    await StartCommonDestinationMaintenanceTask(task, stoppingToken);
                else
                    await StartMaintenanceTask(task, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Ошибка обработки задания обслуживания: {e.Message}");
            }
        }
    }
    
    private async Task StartCommonDestinationMaintenanceTask(MaintenanceTaskDto task, CancellationToken cancellationToken)
    {
        await SendTaskLog(task, "Начало выполнения задачи", false, false, cancellationToken);

        try
        {
            var v8Files = await SaveTaskV8Files(task, cancellationToken);
            
            var log = new List<MaintenanceTaskLogItemDto>();

            var context = new MaintenanceStepContext
            {
                Task = task,
                Log = log,
                Step = task.Steps.GetRootStep(),
                Files = v8Files
            };

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (log.Count > 0)
                    {
                        await SendStepLog(log, cancellationToken);
                        log.Clear();
                    }

                    if (context.Step.NodeKind == MaintenanceStepNodeKind.TryCatch)
                    {
                        try
                        {
                            await HandleTaskStepNode(context, cancellationToken);

                            if (context.Step.LeftStepId is not null)
                                context.Step = task.Steps.GetStep(context.Step.LeftStepId);
                            else
                                break;
                        }
                        catch (Exception e)
                        {
                            AddStepLogItem(context, e.ToString(), true);

                            if (context.Step.RightStepId is not null)
                                context.Step = task.Steps.GetStep(context.Step.RightStepId);
                            else
                                break;
                        }
                    }
                    else
                    {
                        await HandleTaskStepNode(context, cancellationToken);

                        if (context.Step.LeftStepId is not null)
                            context.Step = task.Steps.GetStep(context.Step.LeftStepId);
                        else
                            break;
                    }
                }

                AddStepLogItem(context, "Завершено", false, true);
                await SendStepLog(log, cancellationToken);
            }
            catch (Exception e)
            {
                AddStepLogItem(context, e.ToString(), true, true);
                await SendStepLog(log, cancellationToken);
            }
            finally
            {
                DeleteV8Files(context.Files, true);
            }
            
            await SendTaskLog(task, "Завершение выполнения задачи", false, true, cancellationToken);
        }
        catch (Exception e)
        {
            await SendTaskLog(task, e.ToString(), true, true, cancellationToken);
        }
    }

    private async Task StartMaintenanceTask(MaintenanceTaskDto task, CancellationToken cancellationToken)
    {
        await SendTaskLog(task, "Начало выполнения задачи", false, false, cancellationToken);

        try
        {
            var v8Files = await SaveTaskV8Files(task, cancellationToken);
            await DumpConfigRepositories(v8Files, task, cancellationToken);

            /*var hasStepsForDesigner = HasStepsForDesigner(task);
            var canUseDesignerAgent = CanUseDesignerAgent(task);*/
            var hasStepsForDesigner = true;
            var canUseDesignerAgent = false;

            await Parallel.ForEachAsync(task.InfoBases, cancellationToken, async (infoBase, stoppingToken) =>
            {
                var log = new List<MaintenanceTaskLogItemDto>();

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

                    context.Rac = Rac.GetRacForRasService(_racLogger, ras);
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
                            await SendStepLog(log, stoppingToken);
                            log.Clear();
                        }

                        if (context.Step.NodeKind == MaintenanceStepNodeKind.TryCatch)
                        {
                            try
                            {
                                await HandleTaskStepNode(context, stoppingToken);

                                if (context.Step.LeftStepId is not null)
                                    context.Step = task.Steps.GetStep(context.Step.LeftStepId);
                                else
                                    break;
                            }
                            catch (Exception e)
                            {
                                AddStepLogItem(context, e.ToString(), true);

                                if (context.Step.RightStepId is not null)
                                    context.Step = task.Steps.GetStep(context.Step.RightStepId);
                                else
                                    break;
                            }
                        }
                        else
                        {
                            await HandleTaskStepNode(context, stoppingToken);

                            if (context.Step.LeftStepId is not null)
                                context.Step = task.Steps.GetStep(context.Step.LeftStepId);
                            else
                                break;
                        }
                    }

                    AddStepLogItem(context, "Завершено", false, true);
                    await SendStepLog(log, stoppingToken);
                }
                catch (Exception e)
                {
                    AddStepLogItem(context, e.ToString(), true, true);
                    await SendStepLog(log, stoppingToken);
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

            await SendTaskLog(task, "Удаление временных файлов", false, false, cancellationToken);
            DeleteV8Files(v8Files, false);
            
            await SendTaskLog(task, "Завершение выполнения задачи", false, true, cancellationToken);
        }
        catch (Exception e)
        {
            await SendTaskLog(task, e.ToString(), true, true, cancellationToken);
        }
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

    private async Task HandleTaskStepNode(MaintenanceStepContext context, CancellationToken cancellationToken)
    {
        AddStepLogItem(context, $"Обработка шага \"{context.Step.Kind.GetDisplay()}\"");

        switch (context.Step.Kind)
        {
            case MaintenanceStepKind.LockConnections:
                await LockConnections(context);
                break;
            case MaintenanceStepKind.CloseConnections:
                await CloseConnections(context);
                break;
            case MaintenanceStepKind.UnlockConnections:
                await UnlockConnections(context);
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
            case MaintenanceStepKind.CopyInfoBase:
                await CopyInfoBase(context, cancellationToken);
                break;
            default:
                throw new Exception($"Неизвестный тип шага \"{context.Step.Kind.GetDisplay()}\"");
        }
    }
    
    private static void AddStepLogItem(MaintenanceStepContext context, string message, bool isError = false, bool isFinish = false)
    {
        context.Log.Add(new MaintenanceTaskLogItemDto
        {
            Id = Guid.NewGuid(),
            Message = message,
            IsError = isError,
            IsFinish = isFinish,
            TimeStamp = DateTime.Now,
            InfoBaseId = context.InfoBase?.Id,
            StepId = context.Step.Id,
            TaskId = context.Task.Id
        });
    }
    
    private async Task SendStepLog(List<MaintenanceTaskLogItemDto> log, CancellationToken cancellationToken)
        => await _serverConnection.Send(MessageType.MaintenanceStepNodeLog, log, cancellationToken);
    
    private async Task SendTaskLog(MaintenanceTaskDto task, string message, bool isError, bool isFinish, CancellationToken cancellationToken)
        => await _serverConnection.Send(MessageType.MaintenanceStepNodeLog, new List<MaintenanceTaskLogItemDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Message = message,
                IsError = isError,
                IsFinish = isFinish,
                TimeStamp = DateTime.Now,
                TaskId = task.Id
            }
        }, cancellationToken);

    private async Task<Dictionary<Guid, string>> SaveTaskV8Files(MaintenanceTaskDto task,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        using var downloader = scope.ServiceProvider.GetRequiredService<FilesDownloader>();

        var filesToDownload = new List<FileDto>();

        foreach (var maintenanceStepDto in task.Steps)
        {
            if (maintenanceStepDto.Kind == MaintenanceStepKind.ExecuteOneScript && !maintenanceStepDto.ExecuteOneScriptStep!.DebugMode)
                filesToDownload.Add(maintenanceStepDto.ExecuteOneScriptStep!.File);
            else if (maintenanceStepDto.Kind == MaintenanceStepKind.StartExternalDataProcessor)
                filesToDownload.Add(maintenanceStepDto.StartExternalDataProcessorStep!.File);
            else if (maintenanceStepDto.Kind == MaintenanceStepKind.UpdateConfiguration)
                filesToDownload.Add(maintenanceStepDto.UpdateConfigurationStep!.File);
            else if (maintenanceStepDto.Kind == MaintenanceStepKind.LoadConfiguration && !maintenanceStepDto.LoadExtensionStep!.FromConfigRepository)
                filesToDownload.Add(maintenanceStepDto.LoadConfigurationStep!.File!);
            else  if (maintenanceStepDto.Kind == MaintenanceStepKind.LoadExtension && !maintenanceStepDto.LoadExtensionStep!.FromConfigRepository)
                filesToDownload.Add(maintenanceStepDto.LoadExtensionStep!.File!);
        }

        if (filesToDownload.Count == 0)
            return [];
        
        await SendTaskLog(task, "Загрузка файлов для выполнения шагов", false, false, cancellationToken);
        var result = await downloader.Download(_serverConnection, filesToDownload.DistinctBy(c => c.Id).ToList(), cancellationToken);
        await SendTaskLog(task, "Загрузка файлов для выполнения шагов завершена", false, false, cancellationToken);

        return result;
    }

    private async Task DumpConfigRepositories(Dictionary<Guid, string> files, MaintenanceTaskDto task, 
        CancellationToken cancellationToken)
    {
        var fromRepsSteps = task.Steps.Where(c =>
        {
            switch (c.Kind)
            {
                case MaintenanceStepKind.LoadConfiguration when c.LoadConfigurationStep!.FromConfigRepository:
                case MaintenanceStepKind.LoadExtension when c.LoadExtensionStep!.FromConfigRepository:
                    return true;
                default:
                    return false;
            }
        })
        .Select(c =>
        {
            switch (c.Kind)
            {
                case MaintenanceStepKind.LoadConfiguration when c.LoadConfigurationStep!.FromConfigRepository:
                    return (Step: c, c.LoadConfigurationStep!.ConfigurationRepository);
                case MaintenanceStepKind.LoadExtension when c.LoadExtensionStep!.FromConfigRepository:
                    return (Step: c, c.LoadExtensionStep!.ConfigurationRepository);
                default:
                    throw new NotImplementedException();
            }
        }).ToList();
        
        if (fromRepsSteps.Count == 0)
            return;
        
        await SendTaskLog(task, "Выгрузка конфигураций из хранилищ", false, false, cancellationToken);
        
        Parallel.ForEach(fromRepsSteps, stepInfo =>
        {
            var isExtension = stepInfo.Step.Kind == MaintenanceStepKind.LoadExtension;
            var extension = isExtension ? "cfe" : "cf";
            
            var crServer = _v8ServicesProvider.GetCrServerForPort(stepInfo.ConfigurationRepository!.Port);
            var tempIbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempIbPath);
                OnecV8BatchMode.CreateFileInfoBase(crServer.Platform, tempIbPath);

                var configPath = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}.{extension}");
                var address = $"tcp://localhost:{crServer.Port}/{stepInfo.ConfigurationRepository.Name}";

                using var batch = OnecV8BatchMode.CreateDesignerBatch(crServer.Platform, tempIbPath);
                batch.DumpConfigRepository(
                    configPath,
                    address,
                    stepInfo.ConfigurationRepository.Credentials!.User,
                    stepInfo.ConfigurationRepository.Credentials!.Password);
                
                var file = new FileDto
                {
                    Id = Guid.NewGuid(),
                    Name = address,
                    Version = "1.0.0.1",
                    FileExtension = $".{extension}",
                    IsConfiguration = !isExtension,
                    IsExtension = isExtension,
                    Length = new FileInfo(configPath).Length
                };
                files.Add(file.Id, configPath);
                
                if (stepInfo.Step.Kind == MaintenanceStepKind.LoadConfiguration)
                    stepInfo.Step.LoadConfigurationStep!.File = file;
                else if (stepInfo.Step.Kind == MaintenanceStepKind.LoadExtension)
                    stepInfo.Step.LoadExtensionStep!.File = file;
            }
            finally
            {
                Directory.Delete(tempIbPath, true);
            }
        });
        
        await SendTaskLog(task, "Выгрузка конфигураций из хранилищ завершена", false, false, cancellationToken);
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

    private static async Task LockConnections(MaintenanceStepContext context)
    {
        await context.Rac.BlockConnections(
            context.InfoBase!.Cluster.ClusterInternalId, 
            context.InfoBase.InfoBaseInternalId,
            context.Step.LockConnectionsStep!.AccessCode,
            context.Step.LockConnectionsStep!.Message,
            context.InfoBase.Cluster.Credentials?.User ?? "",
            context.InfoBase.Cluster.Credentials?.Password ?? "",
            context.InfoBase.Credentials?.User ?? "",
            context.InfoBase.Credentials?.Password ?? "");
        
        context.AccessCode = context.Step.LockConnectionsStep.AccessCode;
    }
    
    private static async Task CloseConnections(MaintenanceStepContext context)
    {
        var sessions = await context.Rac.GetInfoBaseSessions(
            context.InfoBase!.Cluster.ClusterInternalId, 
            context.InfoBase.InfoBaseInternalId,
            context.InfoBase.Cluster.Credentials?.User ?? "",
            context.InfoBase.Cluster.Credentials?.Password ?? "",
            context.InfoBase.Credentials?.User ?? "",
            context.InfoBase.Credentials?.Password ?? "");
        
        var toClose = sessions
            .Where(c => !c.AppId.Contains("RAS", StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        foreach (var v8Session in toClose)
        {
            try
            {
                await context.Rac.TerminateSession(
                    context.InfoBase.Cluster.ClusterInternalId, 
                    v8Session.Id,
                    context.InfoBase.Cluster.Credentials?.User ?? "",
                    context.InfoBase.Cluster.Credentials?.Password ?? "");
            }
            catch
            {
                // Игнорируем, т.к. сеанс уже мог быть закрыт, мог быть повисшим и т.п.
            }
        }
    }
    
    private static async Task UnlockConnections(MaintenanceStepContext context)
    {
        await context.Rac.UnblockConnections(
            context.InfoBase!.Cluster.ClusterInternalId, 
            context.InfoBase.InfoBaseInternalId,
            context.InfoBase.Cluster.Credentials?.User ?? "",
            context.InfoBase.Cluster.Credentials?.Password ?? "",
            context.InfoBase.Credentials?.User ?? "",
            context.InfoBase.Credentials?.Password ?? "");
    }
    
    private static async Task LoadExtension(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.LoadExtensionStep!.File!.Id];

        if (context.UseDesignerAgent)
        {
            await context.DesignerAgentClient!.LoadExtension(Path.GetFileName(filePath), context.Step.LoadExtensionStep.ExtensionName);
            AddStepLogItem(context, $"Загрузка расширения \"{context.Step.LoadExtensionStep.ExtensionName}\" выполнена");
            
            context.DesignerAgentClient!.UpdateDbCfgExtension(context.Step.LoadExtensionStep.ExtensionName);
            await context.DesignerAgentClient.ReadMessagesTillSuccess(message =>
                LogDesignerAgentMessage(context, message));
        }
        else
        {
            using var batch = context.GetBatchDesigner();
            batch.LoadExtension(
                context.Step.LoadExtensionStep.ExtensionName, 
                filePath, 
                context.InfoBase!.Credentials?.User ?? "", 
                context.InfoBase.Credentials?.Password ?? "", 
                context.AccessCode,
                true);
        
            AddStepLogItem(context, batch.OutFileContent);
        }
    }
    
    private void ExecuteOneScript(MaintenanceStepContext context)
    {
        string scriptPath;
        string executable;

        if (context.Step.ExecuteOneScriptStep!.DebugMode)
        {
            executable = context.Step.ExecuteOneScriptStep!.ExecutablePath;
            scriptPath = Path.GetDirectoryName(executable)!;
        }
        else
        {
            scriptPath = Directory.CreateTempSubdirectory().FullName;
        
            var filePath = context.Files[context.Step.ExecuteOneScriptStep!.File.Id];
            var opmMetadata = OneScriptPackageReader.Unzip(filePath, scriptPath);

            executable = opmMetadata!.Executable;
        }

        var scriptHost = new OneScriptExecutor();
        scriptHost.OnEcho += (_, tuple) =>
        {
            AddStepLogItem(context, tuple.Message);
        };
        scriptHost.OnError += (_, ex) =>
        {
            AddStepLogItem(context, ex.Message, true);
        };
        
        scriptHost.ExecutePackageScript(scriptPath, executable, [], e =>
        {
            e.AddAssembly(typeof(OscriptIntegrationGlobalContext).Assembly);
            e.AddGlobalContext(_oscriptIntegrationGlobalContext);
        }, context.Step.ExecuteOneScriptStep!.DebugMode);
        
        if (!context.Step.ExecuteOneScriptStep!.DebugMode)
            Directory.Delete(scriptPath, true);
    }

    private async Task CopyInfoBase(MaintenanceStepContext context, CancellationToken cancellationToken)
    {
        var step = context.Step.CopyInfoBaseStep!;

        var sourceInfoBaseDetails = await GetInfoBaseDetails(step.SourceInfoBase);
        var destinationInfoBaseDetails = await GetInfoBaseDetails(step.DestinationInfoBase);

        var canCopy = true;

        if (sourceInfoBaseDetails.Dbms != V8InfoBaseDbms.MsSqlServer)
        {
            AddStepLogItem(context, $"Тип базы-источника может быть только \"{destinationInfoBaseDetails.Dbms.GetDisplay()}\"", true);
            canCopy = false;
        }

        if (destinationInfoBaseDetails.Dbms != V8InfoBaseDbms.MsSqlServer)
        {
            AddStepLogItem(context, $"Тип базы-приемника может быть только \"{destinationInfoBaseDetails.Dbms.GetDisplay()}\"", true);
            canCopy = false;
        }

        if (canCopy)
        {
            var backupInfo = SqlHelper.GetLastBackupInfo(sourceInfoBaseDetails, step.SourceCredentials, cancellationToken);
        }
    }

    private async Task<V8InfoBaseDetails> GetInfoBaseDetails(InfoBaseDto infoBase)
    {
        var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(infoBase.Cluster.Port);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);
        
        return await rac.GetInfoBase(
            infoBase.Cluster.ClusterInternalId,
            infoBase.InfoBaseInternalId,
            infoBase.Cluster.Credentials?.User ?? "",
            infoBase.Cluster.Credentials?.Password ?? "",
            infoBase.Credentials?.User ?? "",
            infoBase.Credentials?.Password ?? "");
    }
    
    private static async Task DeleteExtension(MaintenanceStepContext context)
    {
        if (context.UseDesignerAgent)
        {
            var allExtensions = await context.DesignerAgentClient!.GetAllExtensions();
            var extensionsToDeleting = allExtensions.Where(c => Regex.IsMatch(c.Name, context.Step.DeleteExtensionStep!.ExtensionName)).ToList();

            foreach (var extension in extensionsToDeleting)
            {
                await context.DesignerAgentClient!.DeleteExtension(extension.Name);
                AddStepLogItem(context, $"Расширение \"{extension.Name}\" удалено");
            }
        }
        else
        {
            using var batchGet = context.GetBatchDesigner();
            var allExtensions = batchGet.GetExtensionsList(
                context.InfoBase!.Credentials?.User ?? "", 
                context.InfoBase.Credentials?.Password ?? "", 
                context.AccessCode,
                true);
            
            var extensionsToDeleting = allExtensions.Where(c => Regex.IsMatch(c, context.Step.DeleteExtensionStep!.ExtensionName)).ToList();
            
            foreach (var extension in extensionsToDeleting)
            {
                using var batchDeleting = context.GetBatchDesigner();
                batchDeleting.DeleteExtension(
                    extension, 
                    context.InfoBase.Credentials?.User ?? "", 
                    context.InfoBase.Credentials?.Password ?? "", 
                    context.AccessCode,
                    true);
        
                AddStepLogItem(context, batchDeleting.OutFileContent);
            }
        }
    }

    private static async Task LoadConfiguration(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.LoadConfigurationStep!.File!.Id];

        if (context.UseDesignerAgent)
        {
            await context.DesignerAgentClient!.LoadCfg(Path.GetFileName(filePath));
            AddStepLogItem(context, "Загрузка конфигурации выполнена");
            
            context.DesignerAgentClient!.UpdateDbCfg();
            await context.DesignerAgentClient.ReadMessagesTillSuccess(message =>
                LogDesignerAgentMessage(context, message));
        }
        else
        {
            using var batch = context.GetBatchDesigner();
            batch.LoadConfiguration(
                filePath, 
                context.InfoBase!.Credentials?.User ?? "", 
                context.InfoBase.Credentials?.Password ?? "", 
                context.AccessCode,
                true);
        
            AddStepLogItem(context, batch.OutFileContent);
        }
    }
    
    private static void UpdateConfiguration(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.UpdateConfigurationStep!.File.Id];
        
        using var batch = context.GetBatchDesigner();
        batch.UpdateConfiguration(
            filePath, 
            context.InfoBase!.Credentials?.User ?? "", 
            context.InfoBase.Credentials?.Password ?? "", 
            context.AccessCode,
            true);
        
        AddStepLogItem(context, batch.OutFileContent);
    }

    private static void StartExternalDataProcessor(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.StartExternalDataProcessorStep!.File.Id];
        
        var batch = context.GetBatchEnterprise();
        batch.ExecuteExternalDataProcessor(
            filePath, 
            context.InfoBase!.Credentials?.User ?? "", 
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
                AddStepLogItem(context, message.Message[3..], true);
            else
                AddStepLogItem(context, message.Message);
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