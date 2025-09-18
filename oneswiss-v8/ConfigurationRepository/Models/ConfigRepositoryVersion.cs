namespace OneSwiss.V8.ConfigurationRepository.Models;

public class ConfigRepositoryVersion
{
    public string ConfigVersion { get; set; }
    public int Number { get; set; }
    public DateTime DateTime { get; set; }
    public ConfigRepositoryUser User { get; set; }
    public string Comment { get; set; } = string.Empty;
}