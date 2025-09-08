using System.ComponentModel.DataAnnotations;
using MudBlazor;
using OneSwiss.Common.Models;

namespace OneSwiss.Server.Models;

public class Dbms : DatabaseObject
{
    [Required(ErrorMessage = "Не указано наименование")]
    [Label("Имя")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Не указан тип СУБД")]
    [Label("Тип СУБД")]
    public DbmsType Type { get; set; }

    [Required(ErrorMessage = "Не указан адрес сервера баз данных")]
    [Label("Хост")]
    [MaxLength(100)]
    public string Host { get; set; } = string.Empty;

    [Required(ErrorMessage = "Не указан порт сервера баз данных")]
    [Label("Порт")]
    public int Port { get; set; }
}