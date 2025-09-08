namespace OneSwiss.Server.Models;

public class ConfigurationRepositoryMiddlewareArgument : DatabaseObject
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}