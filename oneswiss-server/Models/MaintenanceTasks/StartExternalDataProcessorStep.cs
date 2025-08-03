namespace OneSwiss.Server.Models.MaintenanceTasks;

public class StartExternalDataProcessorStep : DatabaseObject
{
    public Guid? FileId { get; set; }
    public File? File { get; set; }
}