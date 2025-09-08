using System.ComponentModel.DataAnnotations;

namespace OneSwiss.V8.Platform.RemoteAdministration;

public enum V8SecurityLevel
{
    [Display(Name = "Выключено")] Disabled,
    [Display(Name = "Только соединение")] OnlyConnection,
    [Display(Name = "Включено")] Enabled
}