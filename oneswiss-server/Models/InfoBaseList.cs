using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models;

public class InfoBaseList : DatabaseObject
{
    public string ListId { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public List<InfoBasesListItem> InfoBases { get; set; } = [];
    public List<OnecClient> OnecClients { get; set; } = [];
}