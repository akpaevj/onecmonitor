using OneSwiss.V8.Platform;

namespace OneSwiss.V8.Extensions;

public static class V8PlatformsListExtension
{
    public static V8Platform? GetByPath(this IReadOnlyList<V8Platform> items, string path)
        => items.FirstOrDefault(c => c.PlatformPath.Equals(path, StringComparison.OrdinalIgnoreCase));
}