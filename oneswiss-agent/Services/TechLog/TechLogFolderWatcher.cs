namespace OneSwiss.Agent.Services.TechLog;

public class TechLogFolderWatcher(string path, ILogger<TechLogFolderWatcher> logger) : IDisposable
{
    private readonly FileSystemWatcher _watcher = new(path, "*.log")
    {
        NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite
    };

    private readonly object _locker = new();
    private readonly HashSet<string> _filesStopList = [];
    
    public event EventHandler<string>? FileChanged;

    public void Watch()
    {
        _watcher.Changed += Changed;
        
        _watcher.EnableRaisingEvents = true;
    }
    
    public void AddFileToStopList(string path)
    {
        lock (_locker)
            _filesStopList.Add(path);
        
        logger.LogTrace($"Файл {path} добавлен в стоплист наблюдателя");
    }
    
    public void RemoveFileFromStopList(string path)
    {
        lock (_locker)
            _filesStopList.Remove(path);
        
        logger.LogTrace($"Файл {path} удален из стоплиста наблюдателя");
    }

    private void Changed(object sender, FileSystemEventArgs e)
    {
        lock (_locker)
            if (!PathInStopList(e.FullPath))
                FileChanged?.Invoke(this, e.FullPath);
    }
    
    private bool PathInStopList(string path)
        => _filesStopList.Contains(path);
    
    public void Dispose()
    {
        _watcher.Dispose();
        GC.SuppressFinalize(this);
    }
}