using System.Text.Json;
using OneSwiss.Common.EventLog;
using OneSwiss.V8.Platform;

namespace OneSwiss.Agent.Services.EventLog;

public class IbcmdEventLogReader(InfoBaseInfo infoBaseInfo, EventLogExporter exporter, ILogger<IEventLogReader> logger)
    : IEventLogReader
{
    private Ibcmd? _ibcmd;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public event EventHandler? Stopped;

    public void Start(DateTime startDateTime, CancellationToken cancellationToken)
    {
        _ibcmd = new Ibcmd(infoBaseInfo.Platform);
        _ibcmd.EventLogItemRead += EventLogItemHandler;
        _ibcmd.ProcessExited += (sender, i) => Stopped?.Invoke(this, EventArgs.Empty);

        _ibcmd.ExportEventLog(infoBaseInfo.LogPath);
    }

    private void EventLogItemHandler(object? sender, string eventData)
    {
        try
        {
            var item = JsonSerializer.Deserialize<EventLogItem>(eventData);
            item!.InfoBaseId = infoBaseInfo.InfoBaseId;

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

    ~IbcmdEventLogReader()
    {
        Dispose(false);
    }
}