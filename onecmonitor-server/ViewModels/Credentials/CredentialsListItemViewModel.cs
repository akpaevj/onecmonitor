namespace OnecMonitor.Server.ViewModels.Credentials;

public class CredentialsListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public bool DefaultForClusters { get; set; } = false;
    public bool DefaultV8Admin { get; set; } = false;
}