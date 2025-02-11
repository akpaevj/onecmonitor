using System.Text;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.Extensions;
using OneSTools.Common.Designer.Batch;
using OneSTools.Common.Platform;
using Org.BouncyCastle.Asn1.X509;

namespace OnecMonitor.Agent.Services.InfoBases;

public sealed class InfoBasesUpdater : IDisposable
{
    private readonly AsyncServiceScope _scope;
    private readonly OnecMonitorConnection _server;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<InfoBasesUpdater> _logger;
    private bool _disposed;
    
    public InfoBasesUpdater(IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime, ILogger<InfoBasesUpdater> logger) 
    {
        _scope = serviceProvider.CreateAsyncScope();
        _server = _scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
        _applicationLifetime = applicationLifetime;
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
                var path = Path.GetTempFileName();
                File.WriteAllBytes(path, i.Data);
                configurationsPaths.Add(i.Id.ToString(), path);
            });

            var rac = Rac.CreateRacForLaunchedAgent();
            var platform = V8Platforms.GetPlatformForLaunchedAgent();

            await Parallel.ForEachAsync(task.InfoBases, async (infoBase, cancellationToken) =>
            {
                var log = new List<UpdateInfoBaseTaskResultLogItemDto>();

                try
                {
                    if (rac == null)
                        throw new Exception("Ошибка определения RAC для запущенного агента сервера 1С");
                    
                    if (!platform!.HasOnecV8)
                        throw new Exception("Для платформы агента не установлен конфигуратор");

                    var getBatchDesigner = () => new DesignerBatchMode(
                        platform,
                        $"{infoBase.ClusterDto.Host}:{infoBase.ClusterDto.Port}",
                        infoBase.InfoBaseName);
                    
                    log.AddLogItem("Блокировка соединений и регламентных заданий");
                    
                    rac.BlockConnections(
                        infoBase.ClusterDto.Id, 
                        infoBase.Id.ToString(),
                        infoBase.Credentials.User,
                        infoBase.Credentials.Password,
                        accessCode,
                        message);
                    
                    log.AddLogItem("Завершение сессий");
                    
                    var sessions = rac.GetInfoBaseSessions(infoBase.ClusterDto.Id, infoBase.Id.ToString());
                    sessions
                        .Where(c => c.AppId != "RAS")
                        .ToList()
                        .ForEach(s => rac.TerminateSession(infoBase.ClusterDto.Id, s.SessionId));

                    if (config != null)
                    {
                        if (config.IsConfiguration)
                        {
                            log.AddLogItem("Обновление ИБ файлом конфигурации");
                            
                            using var batch = getBatchDesigner();
                            batch.LoadConfiguration(
                                configurationsPaths[config.Id.ToString()], 
                                infoBase.Credentials.User, 
                                infoBase.Credentials.Password, 
                                accessCode);
                        }
                        else
                        {
                            log.AddLogItem("Обновление ИБ файлом обновления конфигурации");
                            
                            using var batch = getBatchDesigner();
                            batch.UpdateConfiguration(
                                configurationsPaths[config.Id.ToString()], 
                                infoBase.Credentials.User, 
                                infoBase.Credentials.Password, 
                                accessCode);
                        }
                    }
                    
                    if (extensions.Count > 0)
                        log.AddLogItem("Обновление расширений ИБ");
                    
                    extensions.ForEach(extension =>
                    {
                        using var batch = getBatchDesigner();
                        batch.LoadExtension(
                            extension.Name, 
                            configurationsPaths[extension.Id.ToString()], 
                            infoBase.Credentials.User, 
                            infoBase.Credentials.Password, 
                            accessCode);
                    });
                    
                    log.AddLogItem("Применение изменений");
                    
                    using var uDbCfgBatch = getBatchDesigner();
                    uDbCfgBatch.UpdateDatabaseConfiguration(infoBase.Credentials.User, infoBase.Credentials.Password, accessCode);

                    var needAcceptLegalUsing = config != null;
                    // TODO("Доделать применение изменений и подтверждение легальности")
                    
                    log.AddLogItem("Обновление завершено");
                    
                    await SendResult(task, infoBase, log, true, cancellationToken);
                }
                catch (Exception e)
                {
                    log.AddLogItem(e.Message, true);
                    await SendResult(task, infoBase, log, false, cancellationToken);
                }
            });
        });
    }

    private async Task SendResult(
        UpdateInfoBaseTaskDto task, 
        InfoBaseDto infoBase, 
        List<UpdateInfoBaseTaskResultLogItemDto> log, 
        bool isSucceed, 
        CancellationToken cancellationToken)
    {
        var result = new UpdateInfoBaseTaskResultDto
        {
            TaskId = task.Id,
            InfoBaseId = infoBase.Id,
            IsFaulted = !isSucceed,
            Log = log
        };

        await _server.Send(MessageType.UpdateInfoBaseTaskResult, result, cancellationToken);
    }

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