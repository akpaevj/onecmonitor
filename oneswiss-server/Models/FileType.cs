using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public enum FileType
{
    [Display(Name = "Конфигурация")]
    Cf,
    [Display(Name = "Расширение")]
    Cfe,
    [Display(Name = "Обновление конфигурации")]
    Cfu,
    [Display(Name = "Внешняя обработка")]
    Epf,
    [Display(Name = "Пакет OneScript")]
    Ospx
}