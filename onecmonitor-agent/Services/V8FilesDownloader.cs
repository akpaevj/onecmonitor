using System.Collections.Concurrent;
using MessagePack;
using OnecMonitor.Common;
using OnecMonitor.Common.DTO;
using OneScript.Commons;

namespace OnecMonitor.Agent.Services;

public class V8FilesDownloader(ILogger<V8FilesDownloader> logger) : IDisposable
{
    private readonly object _locker = new();
    private readonly List<V8FileDto> _filesToDownload = [];
    private TaskCompletionSource<Dictionary<Guid, string>> _downloadTcs = null!;
    private readonly Dictionary<Guid, (string Path, Stream Stream)> _files = new();
    private OnecMonitorConnection _connection;
    
    public async Task<Dictionary<Guid, string>> Download(OnecMonitorConnection connection, List<V8FileDto> files, CancellationToken cancellationToken)
    {
        _connection = connection;
        _filesToDownload.AddRange(files.ToList());
        
        _downloadTcs = new TaskCompletionSource<Dictionary<Guid, string>>();
        cancellationToken.Register(_downloadTcs.SetCanceled);
        _ = _downloadTcs.Task.ContinueWith(_ => connection.MessageReceived -= ConnectionOnMessageReceived, cancellationToken);
        
        CompleteIfAllDownloaded();
        
        connection.MessageReceived += ConnectionOnMessageReceived;

        foreach (var request in files.ToList().Select(fileToDownload => new V8FileRequestDto { Id = fileToDownload.Id }))
            await connection.Send(MessageType.V8FileRequest, request, cancellationToken);

        return await _downloadTcs.Task;
    }

    private async void ConnectionOnMessageReceived(object? sender, Message e)
    {
        try
        {
            if (e.Header.Type == MessageType.V8FileChunk)
            {
                lock (_locker)
                {
                    var chunk = MessagePackSerializer.Deserialize<V8FileChunkDto>(e.Data);
                    var file = _filesToDownload.FirstOrDefault(c => c.Id == chunk.Id);

                    if (!_files.ContainsKey(chunk.Id))
                    {
                        var path = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}{file!.FileExtension}");
                        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    
                        _files.Add(chunk.Id, (path, stream));
                    }

                    if (_files.TryGetValue(chunk.Id, out var tuple))
                        tuple.Stream.Write(chunk.Data);

                    if (tuple.Stream.Length >= file!.Length)
                        _filesToDownload.Remove(file);
                }

                await _connection.SendOk(e, CancellationToken.None)!;
                
                CompleteIfAllDownloaded();
            }
            else
                logger.LogError($"Получено неожиданное сообщение. Ожидаемый тип: {e.Header.Type}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке части файла");
        }
    }

    private void CompleteIfAllDownloaded()
    {
        if (_filesToDownload.Count != 0) 
            return;
        
        logger.LogTrace("Все файлы загружены");
        
        var result = _files.ToDictionary(c => c.Key, c => c.Value.Path);
        ReleaseAllStreams();
        _downloadTcs.TrySetResult(result);
    }

    private void ReleaseAllStreams()
    {
        _files.ForEach(c => c.Value.Stream.Dispose());
        _files.Clear();
    }

    private void ReleaseUnmanagedResources()
    {
        ReleaseAllStreams();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~V8FilesDownloader()
    {
        ReleaseUnmanagedResources();
    }
}