namespace OneSwiss.Server.Models;

public class AccessGroup : DatabaseObject
{
    public string Name { get; set; }
    public bool IsBuiltIn { get; set; }
    public List<ApplicationRole> Roles { get; set; }
    public List<UsersGroup> UsersGroups { get; set; }
}