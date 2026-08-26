using System.ComponentModel.DataAnnotations;
using OneSwiss.Common.Models;

namespace OneSwiss.Server.Models;

public class Dbms : DatabaseObject
{
    [Required(ErrorMessage = "Не указано наименование")]
    
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Не указан тип СУБД")]
    
    public DbmsType Type { get; set; }

    [Required(ErrorMessage = "Не указан адрес сервера баз данных")]
    
    [MaxLength(100)]
    public string Host { get; set; } = string.Empty;

    [Required(ErrorMessage = "Не указан порт сервера баз данных")]
    
    public int Port { get; set; }
}