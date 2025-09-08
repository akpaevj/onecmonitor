namespace OneSwiss.Server.Services;

public class FilesProvider(IWebHostEnvironment env, ILogger<FilesProvider> logger)
{
    public string DataFolder { get; } = Path.Combine(env.ContentRootPath, "Data");

    public void Init()
    {
        if (!Directory.Exists(DataFolder))
            Directory.CreateDirectory(DataFolder);
    }

    public FileStream OpenDataStream(string path, FileMode mode = FileMode.Create)
    {
        var fullPath = GetDataPath(path);
        return new FileStream(fullPath, mode);
    }

    public FileInfo GetFileInfo(string path)
    {
        return new FileInfo(GetDataPath(path));
    }

    public void DeleteDataFile(string path)
    {
        var fullPath = GetDataPath(path);

        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка удаления файла");
        }
    }

    private string GetDataPath(string path)
    {
        return Path.Combine(DataFolder, path);
    }
}