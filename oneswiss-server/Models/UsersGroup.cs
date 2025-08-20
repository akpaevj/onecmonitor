using Microsoft.AspNetCore.Identity;

namespace OneSwiss.Server.Models;

public class UsersGroup : DatabaseObject
{
    public bool IsBuiltIn { get; set; }
    public string Name { get; set; }
    
    public Guid? ParentId { get; set; }
    public UsersGroup? Parent { get; set; }
    
    public List<UsersGroup> Groups { get; set; } = [];
    public List<ApplicationUser> Users { get; set; } = [];
    public List<AccessGroup> AccessGroups { get; set; } = [];
}