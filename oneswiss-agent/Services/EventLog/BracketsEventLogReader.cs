using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using OneSwiss.Agent.Services.EventLog.BracketsReader;
using OneSwiss.Common.EventLog;

namespace OneSwiss.Agent.Services.EventLog;

public class BracketsEventLogReader(
    InfoBaseInfo infoBaseInfo,
    EventLogExporter exporter,
    ILogger<IEventLogReader> logger)
    : IEventLogReader
{
    private CancellationTokenSource? _cts;
    private readonly LgfDataProvider _lgfDataProvider = new(Path.Combine(infoBaseInfo.LogPath, "1Cv8.lgf"));

    public event EventHandler? Stopped;

    public void Start(DateTime startDateTime, CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        Task.Run(async () =>
        {
            try
            {
                var currentLgpFileInfo = GetLgpFileInfo(startDateTime);

                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        if (currentLgpFileInfo == null)
                        {
                            await Task.Delay(1000, _cts.Token);
                            currentLgpFileInfo = GetLgpFileInfo(startDateTime);
                        }
                        else
                        {
                            using var lgpReader = new LgpReader(currentLgpFileInfo.FullName, _lgfDataProvider);
                            var stream = lgpReader.ReadStream(infoBaseInfo, startDateTime, _cts.Token);

                            while (!_cts.Token.IsCancellationRequested)
                            {
                                // ReSharper disable once PossibleMultipleEnumeration
                                foreach (var eventLogItem in stream)
                                {
                                    eventLogItem.TtlDate = DateTime.UtcNow.AddDays(infoBaseInfo.Ttl);
                                    await exporter.Send(eventLogItem);
                                }

                                if (NewLgpFilesExist(currentLgpFileInfo!))
                                {
                                    currentLgpFileInfo = GetNextLgpFileInfo(currentLgpFileInfo!);
                                    break;
                                }
                        
                                // Ждем немного и пытаемся читать данные с этого же файла
                                await Task.Delay(1000, _cts.Token);
                            }
                        }
                    }
                }
                catch (OperationCanceledException){}
                catch (Exception e)
                {
                    throw new Exception($"Ошибка разбора файла журнала регистрации - {currentLgpFileInfo?.Name}", e);
                }
            }
            catch (OperationCanceledException){}
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка чтения журнала регистрации - {Name}", infoBaseInfo.Name);
                await _cts.CancelAsync();
            }
            
        }, _cts.Token);
    }

    private (DateTime DateTime, FileInfo FileInfo)[] GetLgpFiles()
    {
        var logDirInfo = new DirectoryInfo(infoBaseInfo.LogPath);
        
        return logDirInfo
            .GetFiles("*.lgp")
            .Select(c => (DateTime: DateTime.ParseExact(Path.GetFileNameWithoutExtension(c.Name), "yyyyMMddHHmmss", null), FileInfo: c))
            .OrderBy(c => c.DateTime)
            .ToArray();
    }
    
    private bool NewLgpFilesExist(FileInfo currentLgpFileInfo)
    {
        var files = GetLgpFiles().ToList();
        var currentFileIndex = files.FindIndex(c => c.FileInfo.FullName == currentLgpFileInfo.FullName);
        
        if (currentFileIndex < 0)
            return files.Count > 0;

        var nextIndex = ++currentFileIndex;
        return nextIndex < files.Count;
    }

    private FileInfo? GetNextLgpFileInfo(FileInfo currentLgpFileInfo)
    {
        var files = GetLgpFiles().ToList();
        var currentFileIndex = files.FindIndex(c => c.FileInfo.FullName == currentLgpFileInfo.FullName);
        
        if (currentFileIndex < 0)
            return files.FirstOrDefault().FileInfo;

        var nextIndex = ++currentFileIndex;
        return nextIndex >= files.Count ? null : files[nextIndex].FileInfo;
    }

    private FileInfo? GetLgpFileInfo(DateTime dateTime)
    {
        var files = GetLgpFiles();
        
        // Берем первый файл с меньшей датой
        var firstFile = files
            .OrderByDescending(c => c.DateTime)
            .FirstOrDefault(c => c.DateTime <= dateTime)
            .FileInfo;
        
        if (firstFile != null)
            return firstFile;
        
        // Если текущий файл не нашелся, то просто берем самый старый
        return files
            .FirstOrDefault(c => c.DateTime >= dateTime).FileInfo;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _lgfDataProvider.Dispose();
    }
}