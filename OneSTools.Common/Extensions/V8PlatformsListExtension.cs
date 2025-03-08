using OneSTools.Common.Platform;

namespace OneSTools.Common.Extensions;

public static class V8PlatformsListExtension
{
    public static V8Platform? GetByPath(this IReadOnlyList<V8Platform> items, string path)
        => items.FirstOrDefault(c => c.PlatformPath.Equals(path, StringComparison.OrdinalIgnoreCase));
}