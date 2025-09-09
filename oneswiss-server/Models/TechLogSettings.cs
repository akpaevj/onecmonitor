using System.ComponentModel.DataAnnotations;
using MudBlazor;

namespace OneSwiss.Server.Models;

public class TechLogSettings : DatabaseObject
{
    [Label("Включен")] public bool Enabled { get; set; }

    [Label("СУБД")] public Guid? DbmsId { get; set; }

    [Label("Имя базы данных")]
    [MaxLength(100)]
    public string DatabaseName { get; set; } = string.Empty;

    [Label("Имя таблицы")]
    [MaxLength(100)]
    public string Table { get; set; } = string.Empty;

    [Label("Учетные данные")] public Guid? CredentialsId { get; set; }

    public Dbms? Dbms { get; set; }
    public Credentials? Credentials { get; set; }
}