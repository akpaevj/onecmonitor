using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public enum InfoBaseConnectionType
{
    [Display(Name = "Файловая ИБ")]
    File,
    [Display(Name = "Серверная ИБ")]
    Server,
    [Display(Name = "Веб публикация")]
    WebServer
}