using OneSwiss.Common.DTO;

namespace OneSwiss.Agent.Services.EventLog;

// EventLogExportManager читает настройки из одиночного MonitorQueue<EventLogSettingsDto> (Channel
// с одним потребителем) - второй сервис не может подписаться на ту же очередь. EventLogExportManager
// публикует сюда каждый новый снимок настроек, а EventLogReductionService читает его отсюда.
public class EventLogSettingsState
{
    private readonly Lock _sync = new();
    private EventLogSettingsDto? _current;

    public EventLogSettingsDto? Current
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
        set
        {
            lock (_sync)
            {
                _current = value;
            }
        }
    }
}
