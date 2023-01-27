using Microsoft.Extensions.Caching.Memory;
using OnecMonitor.Common.Models;
using static System.Formats.Asn1.AsnWriter;

namespace OnecMonitor.Agent.Services
{
    public class TechLogExporter : IDisposable
    {
        private readonly ServerConnection _serverConnection;
        private readonly TechLogFolderWatcher _techLogWatcher;
        private readonly ILogger<TechLogExporter> _logger;
        private readonly MemoryCache _filesLastPositionCache;
        private CancellationTokenSource? _cts;

        public TechLogExporter(
            ServerConnection serverConnection,
            TechLogFolderWatcher techLogWatcher,
            ILogger<TechLogExporter> logger)
        {
            _serverConnection = serverConnection;
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

        public async Task StartFileReading(string path)
        {
            var position = await GetLastFilePosition(path, _cts!.Token);

            _logger.LogTrace($"Started reading the new file: {path} from {position} position");

            try
            {
                using var reader = new TechLogReader(path, position);

                var seanceId = new Guid(reader.SeanceId);
                var templateId = new Guid(reader.TemplateId);

                var shouldWait = true;

                while (!_cts!.IsCancellationRequested)
                {
                    if (!reader.MoveNext())
                    {
                        if (shouldWait)
                        {
                            await Task.Delay(500);
                            shouldWait = false;
                            continue;
                        }
                        else
                            break;
                    }

                    var message = new TechLogEventContentDto
                    {
                        SeanceId = seanceId,
                        TemplateId = templateId,
                        Folder = reader.Folder,
                        File = reader.FileName,
                        EndPosition = reader.Position,
                        Content = reader.EventContent
                    };

                    await _serverConnection.SendTechLogEventContent(message, _cts.Token);

                    UpdateCachedPosition(ref seanceId, ref templateId, message.Folder, message.File, message.EndPosition);

                    _logger.LogTrace($"Event with content \"{message.Content}\" from {message.Folder}/{message.File} {reader.EventContentStartPosition}-{message.EndPosition} is read");
                }

                _techLogWatcher.StartWatchFile(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read log file");
            }
        }

        public void Stop()
            => _cts?.Cancel();

        private static string GetCacheKey(ref Guid seanceId, ref Guid templateId, string folder, string file)
            => $"{seanceId}_{templateId}_{folder}_{file}";

        private void UpdateCachedPosition(ref Guid seanceId, ref Guid templateId, string folder, string file, long newPosition)
        {
            var cacheKey = GetCacheKey(ref seanceId, ref templateId, folder, file);

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
                return await _serverConnection.GetLastFilePosition(fileInfo.SeanceId, fileInfo.TemplateId, fileInfo.Folder, fileInfo.File, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get last file position");

                throw;
            }
        }

        public void Dispose()
        {
            _serverConnection?.Dispose();
            _techLogWatcher?.Dispose();
        }
    }
}
