namespace OneSwiss.Server.Models;

public class TechLogFilter : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
}