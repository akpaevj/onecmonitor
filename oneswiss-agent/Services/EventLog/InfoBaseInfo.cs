using OneSwiss.V8.Platform;

namespace OneSwiss.Agent.Services.EventLog;

public record InfoBaseInfo
{
    public V8Platform Platform { get; init; }
    public string LogPath { get; init; }
    public string Name { get; init; }
    public string InfoBaseId { get; init; }
    public int Ttl { get; init; }
    
    internal InfoBaseInfo(V8Platform platform, string logPath, string name, string infoBaseId, int ttl)
    {
        Platform = platform;
        LogPath = logPath;
        Name = name;
        InfoBaseId = infoBaseId;
        Ttl = ttl;
    }
}