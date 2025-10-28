using System.Text;

namespace OneSwiss.V8.Helpers;

public static class PathHelper
{
    public static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var currentPath = Path.GetFullPath(path);

        while (true)
        {
            if (!visited.Add(currentPath))
                throw new InvalidOperationException($"Циклическая ссылка: {currentPath}");

            var resolvedPath = TryResolveLinksInPath(currentPath);
            
            if (resolvedPath is null || resolvedPath == currentPath)
                return currentPath;

            currentPath = resolvedPath;
        }
    }

    private static string? TryResolveLinksInPath(string path)
    {
        var components = SplitPath(path);
        StringBuilder result = new(components.Root);

        foreach (var (component, index) in components.Parts.Select((c, i) => (c, i)))
        {
            var currentSegment = Path.Join(result.ToString(), component);
            var linkTarget = GetLinkTarget(currentSegment);

            if (linkTarget is not null)
            {
                // Собираем новый путь
                var remaining = components.Parts.AsSpan(index + 1);
                var newPath = remaining.Length > 0 
                    ? Path.Join(linkTarget, Path.Join(remaining!)) 
                    : linkTarget;
                
                return newPath;
            }

            result.Append(Path.DirectorySeparatorChar);
            result.Append(component);
        }

        return null;
    }

    private static (string Root, string[] Parts) SplitPath(string path)
    {
        path = Path.GetFullPath(path);
        var root = Path.GetPathRoot(path) ?? "";
        
        var remaining = path.AsSpan(root.Length);
        var parts = new List<string>();

        while (!remaining.IsEmpty)
        {
            var sepIndex = remaining.IndexOf(Path.DirectorySeparatorChar);
            
            if (sepIndex >= 0)
            {
                if (sepIndex > 0)
                    parts.Add(remaining[..sepIndex].ToString());
                
                remaining = remaining[(sepIndex + 1)..];
            }
            else
            {
                parts.Add(remaining.ToString());
                break;
            }
        }

        return (root, [.. parts]);
    }

    private static string? GetLinkTarget(string path)
    {
        try
        {
            FileSystemInfo info = Directory.Exists(path) 
                ? new DirectoryInfo(path) 
                : new FileInfo(path);
            
            return ResolveLinkTarget(info.LinkTarget, Path.GetDirectoryName(path));
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveLinkTarget(string? target, string? baseDir)
    {
        if (string.IsNullOrEmpty(target)) return null;
        
        return Path.IsPathRooted(target) || string.IsNullOrEmpty(baseDir)
            ? Path.GetFullPath(target)
            : Path.GetFullPath(Path.Join(baseDir, target));
    }
}