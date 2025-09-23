namespace OneSwiss.Agent.Services.EventLog;

public interface IEventLogReader : IDisposable
{
    event EventHandler? Stopped;
    void Start(DateTime startDateTime, CancellationToken cancellationToken);
}