namespace OneSwiss.Server.Services.CrServerProxy;

public class RequestDetails
{
    public string RequestName { get; set; }
    public bool IsDepotOpen { get; set; }
    public bool IsCommit { get; set; }
    public bool IsChangeVersion { get; set; }
    public string? Comment { get; set; }
}