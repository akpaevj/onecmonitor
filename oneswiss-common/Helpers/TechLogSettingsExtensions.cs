using OneSwiss.Common.DTO;
using OneSwiss.Common.Models;
using OneSwiss.Common.Storage;
using OneSwiss.Common.TechLog;

namespace OneSwiss.Common.Helpers;

public static class TechLogSettingsExtensions
{
    public static ITechLogRepository GetDbContext(this TechLogSettingsDto settings)
    {
        if (settings == null)
            throw new Exception("Не установлены настройки хранилища технологического журнала");

        if (settings.Dbms?.Type != DbmsType.ClickHouse)
            throw new Exception("Only ClickHouse is supported");

        return new ClickHouseContext(settings.Dbms, settings.Credentials, settings.DatabaseName, settings.Table);
    }
}
