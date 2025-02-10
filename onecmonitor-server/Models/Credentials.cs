using OneSTools.Common.Platform;

namespace OnecMonitor.Server.Models;

public class Credentials : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool DefaultForClusters { get; set; } = false;
    public bool DefaultV8Admin { get; set; } = false;
    
    public List<Cluster> Clusters { get; set; } = [];
    public List<InfoBase> InfoBases { get; set; } = [];
}