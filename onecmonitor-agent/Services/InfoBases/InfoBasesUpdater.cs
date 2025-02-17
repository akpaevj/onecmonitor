using System.Reflection;
using System.Text;
using OnecMonitor.Agent.Extensions;
using OnecMonitor.Common.DTO;
using OneSTools.Common.Designer.Batch;
using OneSTools.Common.Extensions;
using OneSTools.Common.Platform;
using Org.BouncyCastle.Asn1.X509;

namespace OnecMonitor.Agent.Services.InfoBases;

public sealed class InfoBasesUpdater : IDisposable
{
    private readonly AsyncServiceScope _scope;
    private readonly OnecMonitorConnection _server;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly RasHolder _rasHolder;
    private readonly ILogger<InfoBasesUpdater> _logger;
    private bool _disposed;
    
    public InfoBasesUpdater(IServiceProvider serviceProvider, RasHolder rasHolder, IHostApplicationLifetime applicationLifetime, ILogger<InfoBasesUpdater> logger) 
    {
        _scope = serviceProvider.CreateAsyncScope();
        _server = _scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
        _applicationLifetime = applicationLifetime;
        _rasHolder = rasHolder;
        _logger = logger;
    }

    public void RequestInfoBasesUpdateTask()
    {
        Task.Run(async () =>
        {
            var accessCode = "12345";
            var message = "Технические работы";
            
            var task = await _server.Get<UpdateInfoBaseTaskDto>(
                MessageType.UpdateInfoBasesTaskRequest,
                MessageType.UpdateInfoBasesTask,
                _applicationLifetime.ApplicationStopping);
            
            var config = task.Configurations.FirstOrDefault(c => c.IsUpdate || c.IsConfiguration);
            var extensions = task.Configurations.Where(c => c.IsExtension).ToList();

            var configurationsPaths = new Dictionary<string, string>();
            task.Configurations.ForEach(i =>
            {
                var path = Path.Join(Path.GetTempPath(), $"{i.Id}.cfu") ;

                if (!File.Exists(path))
                {
                    using var file = File.Create(path);
                    file.Write(i.Data);
                    file.Close();
                }
                
                configurationsPaths.Add(i.Id.ToString(), path);
            });

            await Parallel.ForEachAsync(task.InfoBases, async (infoBase, cancellationToken) =>
            {
                var log = new List<UpdateInfoBaseTaskLogItemDto>();
                
                try
                {
                    var ragent = V8Services.GetActiveRagentForClusterPort(infoBase.Cluster.Port);
                    var ras = _rasHolder.GetActiveRasForRagent(ragent);
                    var rac = Rac.GetRacForRasService(ras);

                    var platform = ragent.Platform;
                    
                    if (!platform.HasOnecV8)
                        throw new Exception("Для платформы агента не установлен конфигуратор");

                    OnecV8BatchMode GetBatchDesigner() 
                        => new(platform, $"{infoBase.Cluster.Host}:{infoBase.Cluster.Port}", infoBase.InfoBaseName);
                    
                    OnecV8BatchMode GetBatchEnterprise() 
                        => new(platform, $"{infoBase.Cluster.Host}:{infoBase.Cluster.Port}", infoBase.InfoBaseName, false);

                    await AddLogItemAndSend(task, infoBase, log, "Блокировка соединений и регламентных заданий", cancellationToken);
                    
                    rac.BlockConnections(
                        infoBase.Cluster.Id, 
                        infoBase.InfoBaseInternalId.ToString(),
                        infoBase.Credentials.User,
                        infoBase.Credentials.Password,
                        accessCode,
                        message);
                    
                    await AddLogItemAndSend(task, infoBase, log, "Завершение сессий", cancellationToken);
                    
                    var sessions = rac.GetInfoBaseSessions(infoBase.Cluster.Id, infoBase.InfoBaseInternalId.ToString());
                    sessions
                        .Where(c => c.AppId != "RAS")
                        .ToList()
                        .ForEach(s => rac.TerminateSession(infoBase.Cluster.Id, s.Id));

                    if (config != null)
                    {
                        if (config.IsConfiguration)
                        {
                            await AddLogItemAndSend(task, infoBase, log, "Загрузка файла конфигурации", cancellationToken);
                            
                            using var loadCfgBatch = GetBatchDesigner();
                            loadCfgBatch.LoadConfiguration(
                                configurationsPaths[config.Id.ToString()], 
                                infoBase.Credentials.User, 
                                infoBase.Credentials.Password, 
                                accessCode,
                                true);

                            await AddLogItemAndSend(task, infoBase, log, loadCfgBatch.OutFileContent, cancellationToken);
                        }
                        else
                        {
                            await AddLogItemAndSend(task, infoBase, log, "Обновление ИБ файлом обновления конфигурации", cancellationToken);
                            
                            using var updateCfgBatch = GetBatchDesigner();
                            updateCfgBatch.UpdateConfiguration(
                                configurationsPaths[config.Id.ToString()], 
                                infoBase.Credentials.User, 
                                infoBase.Credentials.Password, 
                                accessCode,
                                true);
                            
                            await AddLogItemAndSend(task, infoBase, log, updateCfgBatch.OutFileContent, cancellationToken);
                        }
                    }
                    
                    if (extensions.Count > 0)
                        await AddLogItemAndSend(task, infoBase, log, "Загрузка расширений ИБ", cancellationToken);

                    foreach (var extension in extensions)
                    {
                        await AddLogItemAndSend(task, infoBase, log, $"Загрузка расширения {extension.Name}", cancellationToken);
                        
                        using var loadExtBatch = GetBatchDesigner();
                        loadExtBatch.LoadExtension(
                            extension.Name, 
                            configurationsPaths[extension.Id.ToString()], 
                            infoBase.Credentials.User, 
                            infoBase.Credentials.Password, 
                            accessCode,
                            true);
                        
                        await AddLogItemAndSend(task, infoBase, log, loadExtBatch.OutFileContent, cancellationToken);
                    }

                    var needAcceptLegalUsing = config != null;
                    if (needAcceptLegalUsing)
                    {
                        await AddLogItemAndSend(task, infoBase, log, "Подтверждение легальности получения и запуск обработчиков обновления", cancellationToken);
                        
                        var acceptLegalBatch = GetBatchEnterprise();
                        var epfPath = GetExternalDataProcessorPath("ПодтверждениеЛегальности.epf");
                        acceptLegalBatch.ExecuteExternalDataProcessor(
                            epfPath, 
                            infoBase.Credentials.User, 
                            infoBase.Credentials.Password, 
                            accessCode,
                            true);
                    }
                    
                    await AddLogItemAndSend(task, infoBase, log, "Разблокировка соединений и регламентных заданий", cancellationToken);
                    
                    rac.UnblockConnections(
                        infoBase.Cluster.Id, 
                        infoBase.InfoBaseInternalId.ToString(),
                        infoBase.Credentials.User,
                        infoBase.Credentials.Password);
                    
                    await AddLogItemAndSend(task, infoBase, log, "Обновление завершено", cancellationToken, false, true);
                }
                catch (Exception e)
                {
                    await AddLogItemAndSend(task, infoBase, log, e.ToString(), cancellationToken, true, true);
                }
            });
        });
    }

    private async Task AddLogItemAndSend(
        UpdateInfoBaseTaskDto task,
        InfoBaseDto infoBase,
        List<UpdateInfoBaseTaskLogItemDto> log, 
        string message,
        CancellationToken cancellationToken,
        bool isError = false,
        bool isFinish = false)
    {
        log.Add(new UpdateInfoBaseTaskLogItemDto
        {
            Id = Guid.NewGuid(),
            Message = message,
            IsError = isError,
            IsFinish = isFinish,
            TimeStamp = DateTime.Now,
            InfoBaseId = infoBase.Id,
            TaskId = task.Id,
        });
        await SendLog(log, cancellationToken);
    }
    
    private async Task SendLog(List<UpdateInfoBaseTaskLogItemDto> log, CancellationToken cancellationToken)
        => await _server.Send(MessageType.UpdateInfoBaseTaskLog, log, cancellationToken);

    private string GetExternalDataProcessorPath(string fileName)
        => Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Asserts", fileName);

    private void Dispose(bool disposing)
    {
        if (_disposed) 
            return;
        
        if (disposing)
        {
            _scope.Dispose();
            _server.Dispose();
        }
            
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~InfoBasesUpdater()
    {
        Dispose(false);
    }
}