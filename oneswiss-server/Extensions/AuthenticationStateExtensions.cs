using Microsoft.AspNetCore.Components.Authorization;

namespace OneSwiss.Server.Extensions;

public static class AuthenticationStateExtensions
{
    public static bool IsInRoles(this AuthenticationState state, params string[] roles)
    {
        return state.IsAdmin() || roles.Any(role => state.User.IsInRole(role));
    }

    public static bool IsReadOnly(this AuthenticationState state, string editorRole)
    {
        var result = state.IsAdmin();

        if (result)
            return !result;

        return !state.IsInRoles(editorRole);
    }

    public static bool IsAdmin(this AuthenticationState state)
    {
        return state.User.IsInRole(Roles.Administrator);
    }
}