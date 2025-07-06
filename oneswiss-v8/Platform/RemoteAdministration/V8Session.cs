namespace OneSwiss.V8.Platform.RemoteAdministration;

public class V8Session
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public V8InfoBaseSummary InfoBase { get; set; } = null!;
    public string UserName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
}