namespace OneSwiss.Agent.Services;

public class FilesProvider
{
    public string TechLogFolder { get; }

    public FilesProvider(IConfiguration configuration, ILogger<FilesProvider> logger)
    {
        TechLogFolder = configuration.GetValue<string>("TechLogFolder") ?? "";

        if (string.IsNullOrEmpty(TechLogFolder))
        {
            TechLogFolder = GetTechLogDefaultFolder();
            logger.LogInformation($"Путь к каталогу сбора технологического журнала не указан, будет использован каталог по умолчанию: {TechLogFolder}");
        }

        if (Directory.Exists(TechLogFolder)) 
            return;
        
        try
        {
            Directory.CreateDirectory(TechLogFolder);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка создания каталога сбора технологического журнала");
        }
    }

    private static string GetTechLogDefaultFolder()
        => Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "oneswiss", "techlog"),
            _ => Path.Combine("/var", "log", "oneswiss", "techlog")
        };
    
    public static bool WritingAvailable(string path)
    {
        var testPath = Path.Combine(path, "test.txt");
        bool writingAvailable;

        try
        {
            File.Create(testPath);
            writingAvailable = true;
        }
        finally
        {
            File.Delete(testPath);
        }

        return writingAvailable;
    }
}