namespace OneSwiss.Server.Models;

public class OnecClient : DatabaseObject
{
    public string InternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? InfoBasesListId { get; set; }
    
    public InfoBaseList? InfoBasesList { get; set; }
}