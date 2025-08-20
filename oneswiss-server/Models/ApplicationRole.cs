using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace OneSwiss.Server.Models;

public class ApplicationRole : IdentityRole<Guid>, IHasId
{
    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;
    public List<AccessGroup> AccessGroups { get; set; } = [];
    
    public ApplicationRole()
    {}
    
    public ApplicationRole(string role, string description) : base(role)
    {
        Description = description;
    }
}