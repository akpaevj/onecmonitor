using Microsoft.Extensions.Logging.EventLog;
using OneSwiss.Common.DTO;
using OneSwiss.Common.EventLog;
using OneSwiss.Common.Models;
using OneSwiss.Common.Storage;

namespace OneSwiss.Common.Helpers;

public static class EventLogSettingsExtensions
{
    public static IEventLogRepository GetDbContext(this EventLogSettingsDto settings)
    {
        if (settings == null)
            throw new Exception("Не установлены настройки хранилища журнала регистрации");

        if (settings.Dbms.Type != DbmsType.ClickHouse)
            throw new Exception("Only ClickHouse is supported");

        return new ClickHouseContext(settings.Dbms, settings.Credentials, settings.DatabaseName, settings.Table);
    }
}