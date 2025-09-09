namespace OneSwiss.V8.Edt;

public static class EdtDiscoverer
{
    public static List<EdtInstallation> GetInstalled((bool IsStarter, string Path)[] additionalPaths)
    {
        var items = new List<EdtInstallation>();

        var allPaths = additionalPaths.Concat(GetDefaultPaths());

        foreach (var item in allPaths.Where(c => Directory.Exists(c.Path)))
        {
            var edtRunnerName = "1cedt" + (Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : "");
            var versionsDirectories = Directory.EnumerateDirectories(item.Path);
            if (!item.IsStarter)
                versionsDirectories = versionsDirectories.Where(d => File.Exists(Path.Combine(d, edtRunnerName)));

            foreach (var versionDirectory in versionsDirectories)
            {
                var binPath = item.IsStarter ? Path.Combine(versionDirectory, "1cedt") : versionDirectory;
                var configIniPath = Path.Combine(binPath, "configuration", "config.ini");

                if (!File.Exists(configIniPath))
                    continue;

                using var reader = new StreamReader(configIniPath);

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    if (!line.StartsWith("product.version="))
                        continue;

                    var kv = line.Split('=');
                    var version = kv[1].Trim();

                    var edtCliPath = Path.Join(binPath,
                        "1cedtcli" + (Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : ""));

                    var edtItem = new EdtInstallation
                    {
                        Version = version,
                        HasEdtCli = File.Exists(edtCliPath),
                        Path = binPath,
                        FromStarter = item.IsStarter
                    };

                    if (edtItem.HasEdtCli)
                        edtItem.EdtCliPath = edtCliPath;

                    items.Add(edtItem);

                    break;
                }
            }
        }

        return items;
    }

    private static (bool IsStarter, string Path)[] GetDefaultPaths()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT =>
            [
                (true,
                    Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1C",
                        "1cedtstart", "installations")),
                (false,
                    Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "1C", "1CE",
                        "components"))
            ],
            _ =>
            [
                (true,
                    Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "1C",
                        "1cedtstart", "installations")),
                (false, Path.Join("/opt", "1C", "1CE", "components"))
            ]
        };
    }
}