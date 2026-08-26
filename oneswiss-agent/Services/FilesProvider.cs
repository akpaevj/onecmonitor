using OneSwiss.Common.DTO;

namespace OneSwiss.Agent.Services;

public class DownloadedFileDescription
{
    public Guid Id { get; init; }
    public string Path { get; init; } = string.Empty;
}

public class FilesProvider
{
    private readonly IConfiguration _configuration;
    private readonly OneSwissConnection _connection;
    private readonly ILogger<FilesProvider> _logger;

    public FilesProvider(
        [FromKeyedServices(OneSwissConnection.CommonKey)]
        OneSwissConnection connection,
        IConfiguration configuration,
        ILogger<FilesProvider> logger)
    {
        _connection = connection;
        _configuration = configuration;
        _logger = logger;

        InitTechlogFolder();
    }

    public string TechLogFolder { get; private set; } = null!;

    public async Task<List<DownloadedFileDescription>> DownloadFiles(List<FileDto> files,
        CancellationToken cancellationToken = default)
    {
        return await DownloadFiles(_connection, files, cancellationToken);
    }

    public static async Task<List<DownloadedFileDescription>> DownloadFiles(
        OneSwissConnection connection,
        List<FileDto> files,
        CancellationToken cancellationToken = default)
    {
        var result = new List<DownloadedFileDescription>();

        foreach (var file in files)
        {
            var path = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}{file.FileExtension}");
            await using var oStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

            await connection.DownloadFile(oStream, file.Id, cancellationToken);

            result.Add(new DownloadedFileDescription
            {
                Id = file.Id,
                Path = path
            });
        }

        return result;
    }

    private void InitTechlogFolder()
    {
        TechLogFolder = _configuration.GetValue<string>("TechLogFolder") ?? "";

        if (string.IsNullOrEmpty(TechLogFolder))
        {
            TechLogFolder = GetTechLogDefaultFolder();
            _logger.LogInformation(
                $"Путь к каталогу сбора технологического журнала не указан, будет использован каталог по умолчанию: {TechLogFolder}");
        }

        if (Directory.Exists(TechLogFolder))
            return;

        try
        {
            Directory.CreateDirectory(TechLogFolder);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка создания каталога сбора технологического журнала");
        }
    }

    private static string GetTechLogDefaultFolder()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "oneswiss", "techlog"),
            _ => Path.Combine("/var", "log", "oneswiss", "techlog")
        };
    }

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