using System.Diagnostics;
using System.Threading.Channels;

namespace OneSwiss.V8.Platform;

public class Ibcmd : IDisposable
{
    private readonly List<string> _arguments = [];
    private readonly ProcessStartInfo _processStartInfo;
    private Process? _process;
    
    public Channel<string> EventsChannel { get; } = Channel.CreateBounded<string>(100000);
    
    /// <summary>
    /// Событие завершения процесса ibcmd. В качестве аргумента события передается код возврата
    /// </summary>
    public event EventHandler<(int, string)>? ProcessExited;

    public Ibcmd(V8Platform platform)
    {
        if (!platform.HasIbcmd)
            throw new Exception($"{platform.PlatformPath} doesn't contain ibcmd executable");
        
        _processStartInfo = new ProcessStartInfo
        {
            FileName = platform.IbcmdPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true
        };
    }

    /// <summary>
    /// Начинает чтение журнала регистрации. Записи передаются в событие <see cref="EventLogItemRead"/>
    /// </summary>
    /// <param name="logPath">Каталог журнала регистрации</param>
    /// <param name="startDateTime">Дата, с которой необходимо начать чтение журнала</param>
    public void ExportEventLog(string logPath, DateTime? startDateTime = null)
    {
        var fromArg = startDateTime == null ? "" : $"--from={startDateTime.Value:YYYY-MM-DDTHH:mm:ss.ffffff}";
        _arguments.Add($"eventlog export --format=json {fromArg} --skip-root \"{logPath}\"");
        
        Start();
        
        Task.Run(async () =>
        {
            try
            {
                using var reader = _process!.StandardOutput;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    if (!string.IsNullOrEmpty(line))
                        await EventsChannel.Writer.WriteAsync(line);
                }
            }
            finally
            {
                EventsChannel.Writer.Complete();
            }
        }).ConfigureAwait(false);
    }
    
    private void Start(bool waitForExit = false)
    {
        _processStartInfo.Arguments = string.Join(" ", _arguments);
        _processStartInfo.RedirectStandardOutput = true;
        
        _process = new Process();
        _process.StartInfo = _processStartInfo;

        if (!waitForExit)
            _process.Exited += Exited;
        
        if (!_process.Start())
            throw new Exception($"Failed to start {_processStartInfo.FileName} {_processStartInfo.Arguments}");

        if (!waitForExit) 
            return;
        
        _process.WaitForExit();

        if (_process.ExitCode == 0) 
            return;
        
        using var errorStream = _process.StandardError;
        var error = errorStream.ReadToEnd();
        _process.Close();
            
        throw new Exception(error);
    }
    
    private void Exited(object? sender, EventArgs e)
    {
        var error = string.Empty;

        if (_process?.ExitCode != 0)
        {
            using var errorStream = _process?.StandardError;
            error = errorStream?.ReadToEnd() ?? string.Empty;
        }
        
        ProcessExited?.Invoke(this, (_process?.ExitCode ?? 0, error));
    }

    private void ReleaseUnmanagedResources()
    {
        _process?.Kill();
        _process?.Dispose();
        _process = null;
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        
        if (disposing)
        {
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Ibcmd()
    {
        Dispose(false);
    }
}