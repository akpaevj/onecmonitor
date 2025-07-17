using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public enum NotificationChannel
{
    [Display(Name = "Telegram")]
    Telegram,
    [Display(Name = "Вебхук")]
    WebHook
}