using MudBlazor;

namespace OneSwiss.Server.Models;

public class ErrorLoggingServiceSettings : DatabaseObject
{
    [Label("Включен")] public bool Enabled { get; set; }

    [Label("Удалять ошибки старше (в днях)")]
    public int ReportsTtl { get; set; }

    [Label("Сообщение пользователю")] public string Message { get; set; }
}