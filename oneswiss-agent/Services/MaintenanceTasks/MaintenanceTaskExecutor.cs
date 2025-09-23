using System.Text.RegularExpressions;
using Google.Protobuf;
using OneSwiss.Agent.Extensions;
using OneSwiss.Agent.Helpers;
using OneSwiss.Agent.Oscript;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Extensions;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.Common.Services;
using OneSwiss.OneScript;
using OneSwiss.V8.Designer.Agent;
using OneSwiss.V8.Designer.Batch;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.RemoteAdministration;
using ScriptEngine.Hosting;

namespace OneSwiss.Agent.Services.MaintenanceTasks;

public class MaintenanceTaskExecutor : BackgroundService
{
    private readonly AgentsResourcesProvider _agentsResourcesProvider;
    private readonly ILogger<MaintenanceTaskExecutor> _logger;
    private readonly OscriptIntegrationGlobalContext _oscriptIntegrationGlobalContext;
    private readonly MonitorQueue<MaintenanceTaskDto> _queue;
    private readonly ILogger<Rac> _racLogger;
    private readonly RasHolder _rasHolder;
    private readonly AsyncServiceScope _scope;
    private readonly OneSwissConnection _serverConnection;
    private readonly IServiceProvider _serviceProvider;
    private readonly V8ServicesProvider _v8ServicesProvider;

    public MaintenanceTaskExecutor(
        IServiceProvider serviceProvider,
        MonitorQueue<MaintenanceTaskDto> queue,
        RasHolder rasHolder,
        V8ServicesProvider v8ServicesProvider,
        OscriptIntegrationGlobalContext oscriptIntegrationGlobalContext,
        ILogger<MaintenanceTaskExecutor> logger,
        ILogger<Rac> racLogger, AgentsResourcesProvider agentsResourcesProvider)
    {
        _racLogger = racLogger;
        _agentsResourcesProvider = agentsResourcesProvider;
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

    private async Task StartCommonDestinationMaintenanceTask(MaintenanceTaskDto task,
        CancellationToken cancellationToken)
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
                DeleteV8Files(context.Files, false);
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
            var configRepositoriesPlatforms = await GetConfigRepositoriesPlatforms(task, cancellationToken);
            var v8Files = await SaveTaskV8Files(task, cancellationToken);
            await DumpConfigRepositories(configRepositoriesPlatforms, v8Files, task, cancellationToken);

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
                    var ragent = _v8ServicesProvider.GetActiveRagentByPort(infoBase.Cluster.RagentPort);
                    var ras = _rasHolder.GetActiveRasForRagent(ragent);

                    context.Rac = Rac.GetRacForRasService(_racLogger, ras);
                    context.Platform = ragent.Platform;

                    if (!context.Platform.HasOnecV8)
                        throw new Exception("Для платформы агента не установлен конфигуратор");

                    var localFilesFolder = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString(), "0");
                    Directory.CreateDirectory(localFilesFolder);

                    if (hasStepsForDesigner && canUseDesignerAgent)
                    {
                        agent = await context.StartDesignerAgent(Path.GetDirectoryName(localFilesFolder)!);

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
                await UpdateConfiguration(context);
                break;
            case MaintenanceStepKind.LoadConfiguration:
                await LoadConfiguration(context);
                break;
            case MaintenanceStepKind.DeleteExtension:
                await DeleteExtension(context);
                break;
            case MaintenanceStepKind.StartExternalDataProcessor:
                await StartExternalDataProcessor(context);
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

    private static void AddStepLogItem(MaintenanceStepContext context, string message, bool isError = false,
        bool isFinish = false)
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
    {
        await _serverConnection.SendMaintenanceStepNodeLog(log, cancellationToken);
    }

    private async Task SendTaskLog(MaintenanceTaskDto task, string message, bool isError, bool isFinish,
        CancellationToken cancellationToken)
    {
        await _serverConnection.SendMaintenanceStepNodeLog([
            new MaintenanceTaskLogItemDto
            {
                Id = Guid.NewGuid(),
                Message = message,
                IsError = isError,
                IsFinish = isFinish,
                TimeStamp = DateTime.Now,
                TaskId = task.Id
            }
        ], cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> SaveTaskV8Files(MaintenanceTaskDto task,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var filesToDownload = new List<FileDto>();

        foreach (var maintenanceStepDto in task.Steps)
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (maintenanceStepDto.Kind)
            {
                case MaintenanceStepKind.ExecuteOneScript when
                    !maintenanceStepDto.ExecuteOneScriptStep!.DebugMode:
                    filesToDownload.Add(maintenanceStepDto.ExecuteOneScriptStep!.File);
                    break;
                case MaintenanceStepKind.StartExternalDataProcessor:
                    filesToDownload.Add(maintenanceStepDto.StartExternalDataProcessorStep!.File);
                    break;
                case MaintenanceStepKind.UpdateConfiguration:
                    filesToDownload.Add(maintenanceStepDto.UpdateConfigurationStep!.File);
                    break;
                case MaintenanceStepKind.LoadConfiguration when
                    !maintenanceStepDto.LoadConfigurationStep!.FromConfigRepository:
                    filesToDownload.Add(maintenanceStepDto.LoadConfigurationStep!.File!);
                    break;
                case MaintenanceStepKind.LoadExtension when
                    !maintenanceStepDto.LoadExtensionStep!.FromConfigRepository:
                    filesToDownload.Add(maintenanceStepDto.LoadExtensionStep!.File!);
                    break;
            }

        if (filesToDownload.Count == 0)
            return [];

        await SendTaskLog(task, "Загрузка файлов для выполнения шагов", false, false, cancellationToken);
        var result = await FilesProvider.DownloadFiles(_serverConnection,
            filesToDownload.DistinctBy(c => c.Id).ToList(), cancellationToken);
        await SendTaskLog(task, "Загрузка файлов для выполнения шагов завершена", false, false, cancellationToken);

        return result.ToDictionary(c => c.Id, c => c.Path);
    }

    private async Task<Dictionary<Guid, V8Platform>> GetConfigRepositoriesPlatforms(MaintenanceTaskDto task,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, V8Platform>();

        foreach (var step in task.Steps)
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (step.Kind)
            {
                case MaintenanceStepKind.LoadConfiguration when step.LoadConfigurationStep!.FromConfigRepository:
                {
                    var platform = await _agentsResourcesProvider.GetCrServerPlatform(
                        step.LoadConfigurationStep.ConfigurationRepository!.Agent.Id,
                        step.LoadConfigurationStep.ConfigurationRepository.Port,
                        cancellationToken);

                    result.Add(step.LoadConfigurationStep.ConfigurationRepository.Id, platform);
                    break;
                }
                case MaintenanceStepKind.LoadExtension when step.LoadExtensionStep!.FromConfigRepository:
                {
                    var basePlatform = await _agentsResourcesProvider.GetCrServerPlatform(
                        step.LoadExtensionStep.BaseConfigurationRepository!.Agent.Id,
                        step.LoadExtensionStep.BaseConfigurationRepository.Port,
                        cancellationToken);
                    
                    result.Add(step.LoadExtensionStep.BaseConfigurationRepository.Id, basePlatform);
                    
                    var platform = await _agentsResourcesProvider.GetCrServerPlatform(
                        step.LoadExtensionStep.ConfigurationRepository!.Agent.Id,
                        step.LoadExtensionStep.ConfigurationRepository.Port,
                        cancellationToken);

                    result.Add(step.LoadExtensionStep.ConfigurationRepository.Id, platform);
                    break;
                }
            }

        return result;
    }

    private async Task DumpConfigRepositories(Dictionary<Guid, V8Platform> reposPlatforms,
        Dictionary<Guid, string> files, MaintenanceTaskDto task,
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
                        return (Step: c, c.LoadConfigurationStep!.ConfigurationRepository,
                            Version: c.LoadConfigurationStep!.LoadExactVersion ? c.LoadConfigurationStep!.Version : -1,
                            Extension: string.Empty,
                            BaseConfigurationRepository: null);
                    case MaintenanceStepKind.LoadExtension when c.LoadExtensionStep!.FromConfigRepository:
                        return (Step: c, c.LoadExtensionStep!.ConfigurationRepository,
                            Version: c.LoadExtensionStep!.LoadExactVersion ? c.LoadExtensionStep!.Version : -1,
                            Extension: c.LoadExtensionStep.ExtensionName,
                            c.LoadExtensionStep!.BaseConfigurationRepository);
                    default:
                        throw new NotImplementedException();
                }
            }).ToList();

        if (fromRepsSteps.Count == 0)
            return;

        await SendTaskLog(task, "Выгрузка конфигураций из хранилищ", false, false, cancellationToken);

        await Parallel.ForEachAsync(fromRepsSteps, cancellationToken, async (stepInfo, token) =>
        {
            var isExtension = stepInfo.Step.Kind == MaintenanceStepKind.LoadExtension;
            var extension = isExtension ? "cfe" : "cf";

            var tempIbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempIbPath);

                var platform = reposPlatforms[stepInfo.ConfigurationRepository!.Id];

                await OnecV8BatchMode.CreateFileInfoBase(platform, tempIbPath);

                if (!string.IsNullOrEmpty(stepInfo.Extension) && stepInfo.BaseConfigurationRepository != null)
                {
                    var basePlatform = reposPlatforms[stepInfo.BaseConfigurationRepository!.Id];
                    
                    var baseAddress =
                        $"tcp://{stepInfo.BaseConfigurationRepository!.Host}:{stepInfo.BaseConfigurationRepository.Port}/{stepInfo.BaseConfigurationRepository.Name}";
                    
                    using var baseBatch = OnecV8BatchMode.CreateDesignerBatch(basePlatform, tempIbPath);
                    
                    await baseBatch.UpdateConfigFromRepository(
                        baseAddress,
                        stepInfo.BaseConfigurationRepository.Credentials?.User ?? "",
                        stepInfo.BaseConfigurationRepository.Credentials?.Password ?? "");

                    await IbcmdWrapper.AddExtension(basePlatform, string.Empty, tempIbPath, stepInfo.Extension, "UL");
                }
                
                var configPath = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}.{extension}");
                var address =
                    $"tcp://{stepInfo.ConfigurationRepository.Host}:{stepInfo.ConfigurationRepository.Port}/{stepInfo.ConfigurationRepository.Name}";

                using var batch = OnecV8BatchMode.CreateDesignerBatch(platform, tempIbPath);
                
                await batch.DumpConfigRepository(
                    configPath,
                    address,
                    stepInfo.ConfigurationRepository.Credentials!.User,
                    stepInfo.ConfigurationRepository.Credentials!.Password,
                    stepInfo.Version,
                    stepInfo.Extension);

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
            await context.DesignerAgentClient!.LoadExtension(Path.GetFileName(filePath),
                context.Step.LoadExtensionStep.ExtensionName);
            AddStepLogItem(context,
                $"Загрузка расширения \"{context.Step.LoadExtensionStep.ExtensionName}\" выполнена");

            context.DesignerAgentClient!.UpdateDbCfgExtension(context.Step.LoadExtensionStep.ExtensionName);
            await context.DesignerAgentClient.ReadMessagesTillSuccess(message =>
                LogDesignerAgentMessage(context, message));
        }
        else
        {
            using var batch = context.GetBatchDesigner();
            await batch.LoadExtension(
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
        scriptHost.OnEcho += (_, tuple) => { AddStepLogItem(context, tuple.Message); };
        scriptHost.OnError += (_, ex) => { AddStepLogItem(context, ex.Message, true); };

        scriptHost.ExecutePackageScript(scriptPath, executable, [], e =>
        {
            e.AddAssembly(typeof(OscriptIntegrationGlobalContext).Assembly);
            e.AddAssembly(typeof(V8Platform).Assembly);
            e.AddGlobalContext(_oscriptIntegrationGlobalContext);

            if (!context.Task.CommonDestination)
                e.AddGlobalContext(new MaintenanceStepIntegrationGlobalContext(context));
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
            AddStepLogItem(context,
                $"Тип базы-источника может быть только \"{destinationInfoBaseDetails.Dbms.GetDisplay()}\"", true);
            canCopy = false;
        }

        if (destinationInfoBaseDetails.Dbms != V8InfoBaseDbms.MsSqlServer)
        {
            AddStepLogItem(context,
                $"Тип базы-приемника может быть только \"{destinationInfoBaseDetails.Dbms.GetDisplay()}\"", true);
            canCopy = false;
        }

        if (canCopy)
        {
            var backupInfo =
                SqlHelper.GetLastBackupInfo(sourceInfoBaseDetails, step.SourceCredentials, cancellationToken);
        }
    }

    private async Task<V8InfoBaseDetails> GetInfoBaseDetails(InfoBaseDto infoBase)
    {
        var ragent = _v8ServicesProvider.GetActiveRagentByPort(infoBase.Cluster.Port);
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
            var extensionsToDeleting = allExtensions
                .Where(c => Regex.IsMatch(c.Name, context.Step.DeleteExtensionStep!.ExtensionName)).ToList();

            foreach (var extension in extensionsToDeleting)
            {
                await context.DesignerAgentClient!.DeleteExtension(extension.Name);
                AddStepLogItem(context, $"Расширение \"{extension.Name}\" удалено");
            }
        }
        else
        {
            using var batchGet = context.GetBatchDesigner();
            var allExtensions = await batchGet.GetExtensionsList(
                context.InfoBase!.Credentials?.User ?? "",
                context.InfoBase.Credentials?.Password ?? "",
                context.AccessCode,
                true);

            var extensionsToDeleting = allExtensions
                .Where(c => Regex.IsMatch(c, context.Step.DeleteExtensionStep!.ExtensionName)).ToList();

            foreach (var extension in extensionsToDeleting)
            {
                using var batchDeleting = context.GetBatchDesigner();
                await batchDeleting.DeleteExtension(
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
            await batch.LoadConfiguration(
                filePath,
                context.InfoBase!.Credentials?.User ?? "",
                context.InfoBase.Credentials?.Password ?? "",
                context.AccessCode,
                true);

            AddStepLogItem(context, batch.OutFileContent);
        }
    }

    private static async Task UpdateConfiguration(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.UpdateConfigurationStep!.File.Id];

        using var batch = context.GetBatchDesigner();
        await batch.UpdateConfiguration(
            filePath,
            context.InfoBase!.Credentials?.User ?? "",
            context.InfoBase.Credentials?.Password ?? "",
            context.AccessCode,
            true);

        AddStepLogItem(context, batch.OutFileContent);
    }

    private static async Task StartExternalDataProcessor(MaintenanceStepContext context)
    {
        var filePath = context.Files[context.Step.StartExternalDataProcessorStep!.File.Id];

        using var batch = context.GetBatchEnterprise();
        await batch.ExecuteExternalDataProcessor(
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
            if (step.Kind is MaintenanceStepKind.DeleteExtension or
                MaintenanceStepKind.LoadConfiguration or
                MaintenanceStepKind.LoadExtension or
                MaintenanceStepKind.UpdateConfiguration)
                has = true;

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
        {
            throw new Exception($"Неожиданный тип сообщения: {message.Type}");
        }
    }

    private static bool CanUseDesignerAgent(MaintenanceTaskDto task)
    {
        return task.Steps.FirstOrDefault(c => c.Kind == MaintenanceStepKind.UpdateConfiguration) == null;
    }

    public override void Dispose()
    {
        _scope.Dispose();
        base.Dispose();
    }
}