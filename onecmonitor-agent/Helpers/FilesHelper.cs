namespace OnecMonitor.Agent.Helpers;

public static class FilesHelper
{
    public static string OnecMonitorAgentPath { get; } = Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => throw new NotImplementedException(),
        _ => Path.Combine("/var", "log", "onecmonitor")
    };

    public static string TechLogRootPath { get; } = Path.Combine(OnecMonitorAgentPath, "techlog");

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