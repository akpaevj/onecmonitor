using System.Text.Json;
using OneSwiss.Common.EventLog;
using OneSwiss.V8.Platform;

namespace OneSwiss.Agent.Services.EventLog;

public class EventLogReader(InfoBaseInfo infoBaseInfo, EventLogExporter exporter, ILogger<EventLogReader> logger)
    : IDisposable
{
    private Ibcmd? _ibcmd;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public event EventHandler<(int, string)>? ProcessExited;

    public void Start()
    {
        _ibcmd = new Ibcmd(infoBaseInfo.Platform);
        _ibcmd.EventLogItemRead += EventLogItemHandler;
        _ibcmd.ProcessExited += (sender, i) => ProcessExited?.Invoke(sender, i);

        _ibcmd.ExportEventLog(infoBaseInfo.LogPath);
    }

    private void EventLogItemHandler(object? sender, string eventData)
    {
        try
        {
            var item = JsonSerializer.Deserialize<EventLogItem>(eventData);
            item!.InfoBaseName = infoBaseInfo.Name;

            exporter.Send(item);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Ошибка десериализации события: {eventData}");
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

    ~EventLogReader()
    {
        Dispose(false);
    }
}