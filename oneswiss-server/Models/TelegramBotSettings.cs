using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public class TelegramBotSettings : DatabaseObject
{
    [Required(AllowEmptyStrings = true)]
    public string Token { get; set; } = string.Empty;
}