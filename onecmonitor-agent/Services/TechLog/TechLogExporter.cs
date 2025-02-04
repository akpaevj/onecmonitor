using System.Text;
using Microsoft.Extensions.Caching.Memory;
using OnecMonitor.Common.Models;

namespace OnecMonitor.Agent.Services.TechLog
{
    public class TechLogExporter : IDisposable
    {
        private readonly AsyncServiceScope _scope;
        private readonly OnecMonitorConnection _onecMonitorConnection;
        private readonly TechLogFolderWatcher _techLogWatcher;
        private readonly ILogger<TechLogExporter> _logger;
        private readonly MemoryCache _filesLastPositionCache;
        private CancellationTokenSource? _cts;

        public TechLogExporter(
            IServiceProvider serviceProvider,
            TechLogFolderWatcher techLogWatcher,
            ILogger<TechLogExporter> logger)
        {
            _scope = serviceProvider.CreateAsyncScope();
            _onecMonitorConnection = _scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
            _techLogWatcher = techLogWatcher;
            _logger = logger;
            _filesLastPositionCache = new MemoryCache(new MemoryCacheOptions());

            _techLogWatcher.LogFileChanged += path =>
            {
                _ = StartFileReading(path);
                _logger.LogTrace("Started reading changed file");
            };

            _techLogWatcher.LogFileCreated += path =>
            {
                _ = StartFileReading(path);
                _logger.LogDebug("Started reading the new file");
            };
        }

        public void Start()
        {
            if (!_cts?.IsCancellationRequested == false)
                Stop();

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _cts.Token.Register(() => 
            {
                _techLogWatcher.Stop();
            });

            try
            {
                var existsFiles = _techLogWatcher.GetExistFiles();

                foreach (var filePath in existsFiles)
                {
                    if (_cts.Token.IsCancellationRequested)
                        break;

                    _techLogWatcher.StopWatchFile(filePath);

                    _ = StartFileReading(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch exist log files");
                return;
            }

            _techLogWatcher.Start();
        }

        private async Task StartFileReading(string path)
        {
            try
            {
                var position = await GetLastFilePosition(path, _cts!.Token);

                _logger.LogTrace($"Started reading the new file: {path} from {position} position");

                try
                {
                    using var reader = new NewTechLogReader(path, position);

                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var folder = Path.GetFileName(Path.GetDirectoryName(path)) ?? "";
                    var seanceId = new Guid(Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path))))!);
                    var templateId = new Guid(Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)))!);

                    var cacheKey = GetCacheKey(ref seanceId, ref templateId, folder, fileName);

                    while (!_cts!.IsCancellationRequested)
                    {
                        var read = false;

                        try
                        {
                            _logger.LogTrace("Begin reading the next event item");

                            read = reader.MoveNext();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to read log file");
                        }

                        if (read)
                        {
                            _logger.LogTrace("Event is read");

                            var message = new TechLogEventContentDto
                            {
                                SeanceId = seanceId,
                                TemplateId = templateId,
                                Folder = folder,
                                File = fileName,
                                EndPosition = reader.Position,
                                Content = reader.EventContent
                            };

                            await _onecMonitorConnection.SendTechLogEventContent(message, _cts.Token);

                            CachePosition(cacheKey, message.EndPosition);
                        }
                        else
                            break;
                    }

                    _logger.LogTrace("Stopping reading the file");

                    _techLogWatcher.StartWatchFile(path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read log file");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get last file position");
            }
        }

        public void Stop()
            => _cts?.Cancel();

        private static string GetCacheKey(ref Guid seanceId, ref Guid templateId, string folder, string file)
        {
            var builder = new StringBuilder();

            builder.Append(seanceId.ToString());
            builder.Append('_');
            builder.Append(templateId.ToString());
            builder.Append('_');
            builder.Append(folder);
            builder.Append('_');
            builder.Append(file);

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

        private bool TryGetPositionFromCache(ref Guid seanceId, ref Guid templateId, string folder, string file, out long position)
        {
            var cacheKey = GetCacheKey(ref seanceId,ref templateId, folder, file);

            return TryGetPositionFromCache(cacheKey, out position);
        }

        private bool TryGetPositionFromCache(string cacheKey, out long position)
            => _filesLastPositionCache.TryGetValue(cacheKey, out position);

        public void ClearCache()
            => _filesLastPositionCache.Clear();

        private static (Guid SeanceId, Guid TemplateId, string Folder, string File) GetFileInfo(string path)
        {
            var file = Path.GetFileNameWithoutExtension(path);
            var folder = Directory.GetParent(path)!.Name;
            var templateId = Guid.Parse(Directory.GetParent(path)!.Parent!.Name);
            var seanceId = Guid.Parse(Directory.GetParent(path)!.Parent!.Parent!.Name);

            return (seanceId, templateId, folder, file);
        }

        private async Task<long> GetLastFilePosition(string path, CancellationToken cancellationToken = default)
        {
            var fileInfo = GetFileInfo(path);

            if (TryGetPositionFromCache(ref fileInfo.SeanceId, ref fileInfo.TemplateId, fileInfo.Folder, fileInfo.File, out var position))
                return position;

            try
            {
                return await _onecMonitorConnection.GetLastFilePosition(fileInfo.SeanceId, fileInfo.TemplateId, fileInfo.Folder, fileInfo.File, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get last file position");
                throw;
            }
        }

        public void Dispose()
        {
            _onecMonitorConnection.Dispose();
            _techLogWatcher.Dispose();
        }
    }
}