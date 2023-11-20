using System.Collections.Concurrent;
using System.IO;

namespace OnecMonitor.Agent.Services
{
    public class TechLogFolderWatcher : IDisposable
    {
        private readonly string _logFolder;
        private readonly ILogger<TechLogFolderWatcher> _logger;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private bool disposedValue;
        private readonly object _locker = new();
        private readonly HashSet<string> _stopList = new();

        public delegate void NewLogFileHandler(string path);
        public event NewLogFileHandler? LogFileCreated;

        public delegate void LogFileChangedHandler(string path);
        public event LogFileChangedHandler? LogFileChanged;

        public TechLogFolderWatcher(IConfiguration configuration, ILogger<TechLogFolderWatcher> logger)
        {
            _logger = logger;
            _logFolder = configuration.GetValue("Techlog:LogFolder", "")!;

            try
            {
                if (!Path.Exists(_logFolder))
                    Directory.CreateDirectory(_logFolder);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create tech log folder", ex);
            }

            _fileSystemWatcher = new FileSystemWatcher(_logFolder, "*.log")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };
            _fileSystemWatcher.Created += FileSystemWatcher_Created;
            _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            _logger = logger;
        }

        public void Start()
        {
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void StartWatchFile(string path)
        {
            lock(_locker)
                _stopList.Remove(path);

            _logger.LogTrace($"{path} is removed from the watching stoplist");
        }

        public string[] GetExistFiles()
        {
            var directoryInfo = new DirectoryInfo(_logFolder);

            var files = directoryInfo.GetFiles("*.log", SearchOption.AllDirectories);

            // Need to avoid reading of latest files first, because it may cause skipping of some files in next reading
            return files.OrderByDescending(c => c.CreationTime).Select(c => c.FullName).ToArray();
        }

        public void StopWatchFile(string path)
        {
            lock (_locker)
                _stopList.Add(path);

            _logger.LogTrace($"{path} is added to the watching stoplist");
        }

        public void Stop()
        {
            lock (_locker)
                _stopList.Clear();

            _logger.LogTrace($"Watching stoplist is cleaned");

            _fileSystemWatcher.EnableRaisingEvents = false;
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            lock (_locker)
                if (!_stopList.Contains(e.FullPath))
                {
                    _stopList.Add(e.FullPath);
                    LogFileCreated?.Invoke(e.FullPath);
                }  
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (_locker)
                if (!_stopList.Contains(e.FullPath))
                {
                    _stopList.Add(e.FullPath);
                    LogFileChanged?.Invoke(e.FullPath);
                }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _fileSystemWatcher?.Dispose();

                disposedValue = true;
            }
        }

        ~TechLogFolderWatcher()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
