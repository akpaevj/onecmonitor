namespace OneSwiss.Server.Models;

public class InfoBasesListItem : DatabaseObject
{
    public InfoBaseConnectionType Type { get; set; }
    public string ConnectionString { get; set; }
    public string Name { get; set; }
    public string IBasesContent { get; set; }
}