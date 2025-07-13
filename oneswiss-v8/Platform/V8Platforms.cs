using System.Text.RegularExpressions;

namespace OneSwiss.V8.Platform;

public static partial class V8Platforms
{
    public static IReadOnlyList<V8Platform> GetInstalledPlatforms(string[]? additionalPaths = null)
    {
        var paths = GetDefaultInstallationPaths();
        if (additionalPaths != null)
            paths = paths.Concat(additionalPaths).Distinct().ToArray();
        
        return GetInstalledPlatformsInternal(paths);
    }

    private static List<V8Platform> GetInstalledPlatformsInternal(string[] paths)
    {
        var platforms = new List<V8Platform>();
        
        foreach (var path in paths)
        {
            if (!Directory.Exists(path)) 
                continue;

            platforms.AddRange(
                Directory.GetDirectories(
                    path, 
                    "*", 
                    SearchOption.TopDirectoryOnly
                    )
                    .Where(i => V8VersionFolderRegex().IsMatch(Path.GetFileName(i)))
                    .Select(directory =>
                    {
                        var onecV8 = ExecutableExists(directory, "1cv8");
                        var ras = ExecutableExists(directory, "ras");
                        var rac = ExecutableExists(directory, "rac");
                        var ibcmd = ExecutableExists(directory, "ibcmd");
                        
                        return new V8Platform
                        {
                            PlatformPath = directory,
                            Version = Path.GetFileName(directory),
                            HasOnecV8 = onecV8.Exists,
                            OnecV8Path = onecV8.Path,
                            HasRac = rac.Exists,
                            RacPath = rac.Path,
                            HasRas = ras.Exists,
                            RasPath = ras.Path,
                            HasIbcmd = ibcmd.Exists,
                            IbcmdPath = ibcmd.Path
                        };
                    })
                );
        }

        return platforms;
    }
    
    private static (bool Exists, string Path) ExecutableExists(string platformPath, string name)
    {
        var binPath = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? Path.Join(platformPath, "bin")
            : platformPath;
        
        var path = Path.Join(binPath, name + (Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : ""));
        return (File.Exists(path), path);
    }

    public static string[] GetDefaultInstallationPaths()
        => Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => [
                Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "1cv8"),
                Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "1cv8")
            ],
            _ => [
                "/opt/1cv8/x86_64",
                "/opt/1cv8/x86"
            ]
        };
    
    [GeneratedRegex(@"\d+\.\d+\.\d+\.\d+")]
    private static partial Regex V8VersionFolderRegex();
}