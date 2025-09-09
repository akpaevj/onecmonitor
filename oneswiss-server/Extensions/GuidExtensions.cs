namespace OneSwiss.Server.Extensions;

public static class GuidExtensions
{
    public static bool EmptyOrNull(this Guid? guid)
    {
        return guid is null || guid == Guid.Empty;
    }
}