using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using OneSwiss.Server.Components.Account.Pages;

namespace OneSwiss.Server.Models;

public class ApplicationUser : IdentityUser<Guid>, IHasId
{
    public Guid GroupId { get; set; }
    [ForeignKey(nameof(GroupId))]
    public UsersGroup Group { get; set; }
    public string? ExternalName { get; set; }
    public string? DisplayName { get; set; }

    public override string ToString()
        => (string.IsNullOrEmpty(DisplayName) ? UserName : DisplayName) ?? string.Empty;
}