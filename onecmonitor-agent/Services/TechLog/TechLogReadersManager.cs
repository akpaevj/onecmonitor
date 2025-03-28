using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OnecMonitor.Agent.Models;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Services;
using OnecMonitor.Common.TechLog;

namespace OnecMonitor.Agent.Services.TechLog;

public class TechLogReadersManager : IDisposable
{
    private readonly TechLogRepositoryManager _repositoryManager;
    private readonly TechLogExporter _exporter;
    private readonly ILogger<TechLogReadersManager> _logger;
    
    private readonly AgentInstance _agentInstance;
    private CancellationTokenSource? _cts;
    
    private readonly MemoryCache _filesLastPositionCache = new(new MemoryCacheOptions());
    private ITechLogRepository? _repository;
    
    private readonly SemaphoreSlim _readersLock = new(1, 1);
    private readonly Dictionary<string, (CancellationTokenSource Cts, TechLogReader Reader)> _readers = new();

    public EventHandler<string>? ReadingFinished;

    public TechLogReadersManager(
        IServiceProvider serviceProvider,
        TechLogRepositoryManager repositoryManager,
        TechLogExporter exporter,
        ILogger<TechLogReadersManager> logger)
    {
        _repositoryManager = repositoryManager;
        
        _exporter = exporter;
        _logger = logger;
        
        using var scope = serviceProvider.CreateAsyncScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _agentInstance = dbContext.AgentInstance.AsNoTracking().First();
    }
    
    public async Task Init(TechLogSettingsDto settings, CancellationToken cancellationToken)
    {
        cancellationToken.Register(() =>
        {
            _cts?.Cancel();
            _repository?.Dispose();
        });

        if (settings.Enabled)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _repository = _repositoryManager.GetInstance();
            await _repository.Connect(cancellationToken);
        }
    }

    public async Task AddReader(string path, CancellationToken cancellationToken)
    {
        await _readersLock.WaitAsync(cancellationToken);

        try
        {
            if (_readers.ContainsKey(path))
                return;

            var fileInfo = GetFileInfo(path);
            var cacheKey = GetCacheKey(fileInfo);

            var position = await GetLastFilePosition(fileInfo, cacheKey, cancellationToken);

            using var reader = new TechLogReader(path, position);
            _ = StartReading(reader, fileInfo, cacheKey).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Ошибка добавления читателя файла {path}");
        }
        finally
        {
            _readersLock.Release();
        }
    }
    
    public async Task RemoveReader(string path, CancellationToken cancellationToken)
    {
        await _readersLock.WaitAsync(cancellationToken);

        try
        {
            if (!_readers.TryGetValue(path, out var item))
                return;

            await item.Cts.CancelAsync();
            item.Reader.Dispose();

            _readers.Remove(path);

            _logger.LogTrace($"Читатель файла технологического журнала удален: {path}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _readersLock.Release();
        }
    }

    private async Task StartReading(TechLogReader reader, TechLogFileInfo fileInfo, string cacheKey)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts!.Token);
        _readers.Add(reader.FilePath, (cts, reader));
        
        _logger.LogTrace($"Читатель файла технологического журнала добавлен: {reader.FilePath}");
        
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var read = false;

                try
                {
                    read = reader.MoveNext();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка чтения файла технологического журнала: {reader.FilePath}");
                }

                if (read)
                {
                    var message = new TechLogEventContent
                    {
                        AgentId = _agentInstance.Id,
                        SeanceId = fileInfo.SeanceId,
                        TemplateId = fileInfo.TemplateId,
                        FileName = fileInfo.FileName,
                        EndPosition = reader.Position,
                        Content = reader.EventContent
                    };

                    await _exporter.ProcessTjEventContent(message, cts.Token);

                    CachePosition(cacheKey, message.EndPosition);
                }
                else
                    break;
            }

            _logger.LogTrace($"Окончание чтения файла технологического журнала: {reader.FilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка чтения файла технологического журнала: {reader.FilePath}");
        }
        
        ReadingFinished?.Invoke(this, reader.FilePath);
    }
    
    private static string GetCacheKey(TechLogFileInfo fileInfo)
    {
        var builder = new StringBuilder();
        
        builder.Append(fileInfo.SeanceId);
        builder.Append('_');
        builder.Append(fileInfo.TemplateId);
        builder.Append('_');
        builder.Append(fileInfo.FileName);

        return builder.ToString();
    }

    private void CachePosition(string cacheKey, long newPosition)
    {
        if (!TryGetPositionFromCache(cacheKey, out var position) || position < newPosition)
        {
            if (position < newPosition)
                _filesLastPositionCache.Set(cacheKey, newPosition, TimeSpan.FromHours(1));
        }
        else
            _filesLastPositionCache.Set(cacheKey, newPosition, TimeSpan.FromHours(1));
    }

    private bool TryGetPositionFromCache(string cacheKey, out long position)
        => _filesLastPositionCache.TryGetValue(cacheKey, out position);

    private static TechLogFileInfo GetFileInfo(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var templateId = Guid.Parse(Directory.GetParent(path)!.Name);
        var seanceId = Guid.Parse(Directory.GetParent(path)!.Parent!.Name);

        return new TechLogFileInfo(seanceId, templateId, fileName);
    }

    private async Task<long> GetLastFilePosition(TechLogFileInfo fileInfo, string cacheKey, CancellationToken cancellationToken)
    {
        if (TryGetPositionFromCache(cacheKey, out var position))
            return position;

        try
        {
            _logger.LogTrace($"Запрос последней позиции файла {fileInfo.FileName}");
            
            return await _repository!.GetLastTechLogPosition(
                _agentInstance.Id.ToString(), 
                fileInfo.SeanceId.ToString(), 
                fileInfo.TemplateId.ToString(), 
                fileInfo.FileName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка получения последней позиции файла {fileInfo.FileName}");
            throw;
        }
    }

    public void Dispose()
    {
        _filesLastPositionCache.Dispose();
        _repository?.Dispose();
        _readersLock.Dispose();
    }
}