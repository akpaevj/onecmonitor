using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models
{
    public enum TechLogSeanceStartMode
    {
        [Display(Name = "При создании")]
        Immediately,
        [Display(Name = "Мониторинг")]
        Monitor,
        [Display(Name = "Запланирован")]
        Scheduled
    }
}
