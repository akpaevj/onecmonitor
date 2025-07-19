using System.Text.Json;
using OneSwiss.Common.EventLog;
using OneSwiss.V8.Platform;

namespace OneSwiss.Agent.Services.EventLog;

public class EventLogReader(InfoBaseInfo infoBaseInfo, EventLogExporter exporter, ILogger<EventLogReader> logger) : IDisposable
{
    private Ibcmd? _ibcmd;

    public event EventHandler<(int, string)>? ProcessExited;

    public async Task Start()
    {
        _ibcmd = new Ibcmd(infoBaseInfo.Platform);
        _ibcmd.ProcessExited += (sender, i) => ProcessExited?.Invoke(sender, i);
        
        _ibcmd.ExportEventLog(infoBaseInfo.LogPath);

        while (true)
        {
            if (_ibcmd.EventsChannel.Reader.Completion.IsCompleted && _ibcmd.EventsChannel.Reader.Count == 0)
                return;
            
            var line = await _ibcmd.EventsChannel.Reader.ReadAsync();
            
            try
            {
                var item = JsonSerializer.Deserialize<EventLogItem>(line);
                item!.InfoBaseName = infoBaseInfo.Name;
            
                exporter.Send(item);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Ошибка десериализации события: {line}");
            }
        }
    }

    private void ReleaseUnmanagedResources()
    {
        _ibcmd?.Dispose();
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

    ~EventLogReader()
    {
        Dispose(false);
    }
}