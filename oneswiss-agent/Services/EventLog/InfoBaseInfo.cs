using OneSwiss.V8.Platform;

namespace OneSwiss.Agent.Services.EventLog;

public record InfoBaseInfo
{
    public V8Platform Platform { get; init; }
    public string LogPath { get; init; }
    public string Name { get; init; }
    
    internal InfoBaseInfo(V8Platform platform, string logPath, string name)
    {
        Platform = platform;
        LogPath = logPath;
        Name = name;
    }
}