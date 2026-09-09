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
    private Task? _runTask;
    private readonly LgfDataProvider _lgfDataProvider = new(Path.Combine(infoBaseInfo.LogPath, "1Cv8.lgf"));
    private readonly HashSet<string> _invalidLgpFileNames = [];

    public InfoBaseInfo InfoBaseInfo => infoBaseInfo;

    public event EventHandler? Stopped;

    public void Start(DateTime startDateTime, CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _runTask = Task.Run(async () =>
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
                                    await exporter.SendAsync(eventLogItem, _cts.Token);
                                }

                                if (TryGetNextLgpFileInfo(currentLgpFileInfo!, out var nextLgpFileInfo))
                                {
                                    currentLgpFileInfo = nextLgpFileInfo;
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
                    // Штатная остановка (смена настроек/завершение работы) может привести к ошибке
                    // отправки события уже после того, как она была инициирована - это не ошибка чтения
                    if (_cts.IsCancellationRequested) return;

                    throw new Exception($"Ошибка разбора файла журнала регистрации - {currentLgpFileInfo?.Name}", e);
                }
            }
            catch (OperationCanceledException){}
            catch (Exception e)
            {
                if (_cts.IsCancellationRequested) return;

                logger.LogError(e, "Ошибка чтения журнала регистрации - {Name}", infoBaseInfo.Name);

                try
                {
                    await _cts.CancelAsync();
                }
                catch (ObjectDisposedException) { }

                Stopped?.Invoke(this, EventArgs.Empty);
            }

        }, _cts.Token);
    }

    private (DateTime DateTime, FileInfo FileInfo)[] GetLgpFiles()
    {
        var logDirInfo = new DirectoryInfo(infoBaseInfo.LogPath);

        var result = new List<(DateTime DateTime, FileInfo FileInfo)>();

        foreach (var file in logDirInfo.GetFiles("*.lgp"))
        {
            if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file.Name), "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                result.Add((dateTime, file));
            }
            else if (_invalidLgpFileNames.Add(file.Name))
            {
                logger.LogWarning("Не удалось разобрать имя файла журнала регистрации - {Name}, файл пропущен",
                    file.Name);
            }
        }

        return result.OrderBy(c => c.DateTime).ToArray();
    }

    private bool TryGetNextLgpFileInfo(FileInfo currentLgpFileInfo, out FileInfo? nextLgpFileInfo)
    {
        var files = GetLgpFiles();
        var currentFileIndex = Array.FindIndex(files, c => c.FileInfo.FullName == currentLgpFileInfo.FullName);

        if (currentFileIndex < 0)
        {
            nextLgpFileInfo = files.Length > 0 ? files[0].FileInfo : null;
            return files.Length > 0;
        }

        var nextIndex = currentFileIndex + 1;
        if (nextIndex >= files.Length)
        {
            nextLgpFileInfo = null;
            return false;
        }

        nextLgpFileInfo = files[nextIndex].FileInfo;
        return true;
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

        try
        {
            _runTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // задача уже завершается по токену отмены, ошибки её ожидания не важны
        }

        _cts?.Dispose();
        _lgfDataProvider.Dispose();
    }
}
