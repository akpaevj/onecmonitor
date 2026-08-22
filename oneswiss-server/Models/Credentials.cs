using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public class Credentials : DatabaseObject
{
    [Required(ErrorMessage = "Не указано наименование")]
    
    public string Name { get; set; } = string.Empty;

    public bool IsToken { get; set; }

    [Required(ErrorMessage = "Не указано значение токена")]
    
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Не указан пользователь")]
    
    public string User { get; set; } = string.Empty;

    [Required(ErrorMessage = "Не указан пароль")]
    
    public string? Password { get; set; } = string.Empty;

    
    public bool DefaultForClusters { get; set; } = false;

    
    public bool DefaultV8Admin { get; set; } = false;

    
    public bool DefaultConfigRepositoriesAdmin { get; set; } = false;

    public virtual List<Cluster> Clusters { get; set; } = [];
    public virtual List<InfoBase> InfoBases { get; set; } = [];
    public virtual List<ConfigurationRepository> ConfigurationRepositories { get; set; } = [];
    public virtual List<GitRepository> GitRepositories { get; set; } = [];
}