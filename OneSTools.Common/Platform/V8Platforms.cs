using System.Text.RegularExpressions;
using OneSTools.Common.Extensions;

namespace OneSTools.Common.Platform;

public static partial class V8Platforms
{
    public static V8Platform? GetInstalledPlatformByPath(string platformPath)
        => GetInstalledPlatforms()
            .FirstOrDefault(c => c.PlatformPath.Equals(platformPath, StringComparison.OrdinalIgnoreCase));
    
    public static IReadOnlyList<V8Platform> GetInstalledPlatforms()
        => GetInstalledPlatforms(GetDefaultInstallationPaths());

    private static IReadOnlyList<V8Platform> GetInstalledPlatforms((Arch, string)[] paths)
    {
        var platforms = new List<V8Platform>();
        
        foreach (var (arch, path) in paths)
        {
            if (!Directory.Exists(path)) continue;

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
                        
                        return new V8Platform
                        {
                            Arch = arch,
                            PlatformPath = directory,
                            Version = Path.GetFileName(directory),
                            HasOnecV8 = onecV8.Exists,
                            OnecV8Path = onecV8.Path,
                            HasRac = rac.Exists,
                            RacPath = rac.Path,
                            HasRas = ras.Exists,
                            RasPath = ras.Path
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

    private static (Arch, string)[] GetDefaultInstallationPaths()
        => Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => [
                (Arch.X64, Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "1cv8")),
                (Arch.X32, Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "1cv8"))
            ],
            _ => [
                (Arch.X64, "/opt/1cv8/x86_64"),
                (Arch.X32, "/opt/1cv8/x86")
            ]
        };
    
    [GeneratedRegex(@"\d+\.\d+\.\d+\.\d+")]
    private static partial Regex V8VersionFolderRegex();
}