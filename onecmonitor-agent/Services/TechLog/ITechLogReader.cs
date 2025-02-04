namespace OnecMonitor.Agent.Services.TechLog
{
    internal interface ITechLogReader
    {
        bool MoveNext();
        long Position { get; }
        string FilePath { get; }
        string EventContent { get; }
    }
}
