namespace OneSwiss.V8.Platform;

public abstract class IbcmdWrapper
{
    public static async Task CreateFileInfoBase(V8Platform platform, string dataPath, string ibPath)
    {
        ThrowIfPlatformHasNoIbcmd(platform);

        var args = new List<string>
        {
            "infobase",
            "create"
        };

        args.AddRange(PrepareCommonArgs(dataPath));
        args.AddRange(PrepareFileInfoBaseArgs(ibPath));
        args.AddRange(["--create-database", "--force"]);

        await StartIbcmd(platform, args);
    }

    public static async Task AddExtension(V8Platform platform, string dataPath, string ibPath, string extensionName,
        string prefix)
    {
        ThrowIfPlatformHasNoIbcmd(platform);

        var args = new List<string>
        {
            "config",
            "extension",
            "create"
        };

        args.AddRange(PrepareCommonArgs(dataPath));
        args.AddRange(PrepareFileInfoBaseArgs(ibPath));
        
        args.Add($"--name={extensionName}");
        args.Add($"--name-prefix={prefix}");
        
        await StartIbcmd(platform, args);
    }

    public static async Task ExportXmlFiles(V8Platform platform, string dataPath, string ibPath, string exportPath,
        string extension = "")
    {
        ThrowIfPlatformHasNoIbcmd(platform);

        var args = new List<string>
        {
            "config",
            "export"
        };

        args.AddRange(PrepareCommonArgs(dataPath));
        args.AddRange(PrepareFileInfoBaseArgs(ibPath));

        if (!string.IsNullOrEmpty(extension))
            args.Add($"--extension={extension}");

        args.Add("--force");

        var configDumpInfoPath = Path.Combine(exportPath, "ConfigDumpInfo.xml");
        if (File.Exists(configDumpInfoPath))
            args.Add("--sync");

        args.Add(exportPath);

        await StartIbcmd(platform, args);
    }
    
    public static async Task ExportXmlFilesFromFile(V8Platform platform, string dataPath, string filePath, string exportPath)
    {
        ThrowIfPlatformHasNoIbcmd(platform);

        var args = new List<string>
        {
            "config",
            "export"
        };

        args.AddRange(PrepareCommonArgs(dataPath));
        
        args.Add($"--file=\"{filePath}\"");

        args.Add("--force");

        var configDumpInfoPath = Path.Combine(exportPath, "ConfigDumpInfo.xml");
        if (File.Exists(configDumpInfoPath))
            args.Add("--sync");

        args.Add(exportPath);

        await StartIbcmd(platform, args);
    }

    public static async Task<string> ExportStatus(V8Platform platform, string dataPath, string ibPath, string xmlPath,
        string extension = "")
    {
        ThrowIfPlatformHasNoIbcmd(platform);

        var args = new List<string>
        {
            "config",
            "export",
            "status"
        };

        args.AddRange(PrepareCommonArgs(dataPath));
        args.AddRange(PrepareFileInfoBaseArgs(ibPath));

        if (!string.IsNullOrEmpty(extension))
            args.Add($"--extension={extension}");

        var configDumpInfoPath = Path.Combine(xmlPath, "ConfigDumpInfo.xml");
        args.Add($"--base=\"{configDumpInfoPath}\"");

        var outPath = Path.Combine(Path.GetTempFileName());
        args.Add($"--out=\"{outPath}\"");

        args.Add(xmlPath);

        await StartIbcmd(platform, args);

        return await File.ReadAllTextAsync(outPath);
    }

    private static async Task StartIbcmd(V8Platform platform, List<string> args)
    {
        var result = await ProcessRunner.RunAsync(platform.IbcmdPath, args);

        if (result.ExitCode != 0)
            throw new Exception(result.Error);
    }

    private static List<string> PrepareCommonArgs(string dataPath)
    {
        return [$"--data={dataPath}"];
    }

    private static List<string> PrepareFileInfoBaseArgs(string ibPath)
    {
        return [$"--database-path={ibPath}"];
    }

    private static void ThrowIfPlatformHasNoIbcmd(V8Platform platform)
    {
        if (!platform.HasIbcmd)
            throw new Exception("Переданная платформа не содержит ibcmd");
    }
}