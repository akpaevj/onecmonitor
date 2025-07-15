using System.ComponentModel.DataAnnotations;
using MudBlazor;

namespace OneSwiss.Server.Models;

public class Credentials : DatabaseObject
{
    [Required(ErrorMessage = "Не указано наименование")]
    [Label("Имя")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Не указан пользователь")]
    [Label("Пользователь")]
    public string User { get; set; } = string.Empty;
    [Required(ErrorMessage = "Не указан пароль")]
    [Label("Пароль")]
    public string? Password { get; set; } = string.Empty;
    [Label("Администратор кластера по умолчанию")]
    public bool DefaultForClusters { get; set; } = false;
    [Label("Администратор инф. баз по умолчанию")]
    public bool DefaultV8Admin { get; set; } = false;
    [Label("Администратор хранилищ конфигураций по умолчанию")]
    public bool DefaultConfigRepositoriesAdmin { get; set; } = false;
    
    public virtual List<Cluster> Clusters { get; set; } = [];
    public virtual List<InfoBase> InfoBases { get; set; } = [];
    public virtual List<ConfigurationRepository> ConfigurationRepositories { get; set; } = [];
}