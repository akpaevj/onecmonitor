using Microsoft.AspNetCore.Authorization;

namespace OneSwiss.Server.Attributes;

public class ExtAuthorizeAttribute : AuthorizeAttribute
{
    public ExtAuthorizeAttribute(params string[] roles)
    {
        Roles = $"{BuiltInRoles.Administrator},{string.Join(',', roles)}";
    }
}