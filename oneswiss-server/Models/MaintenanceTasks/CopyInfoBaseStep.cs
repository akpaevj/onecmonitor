using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class CopyInfoBaseStep : DatabaseObject
{
    public Guid? SourceCredentialsId { get; set; }
    [ForeignKey(nameof(SourceCredentialsId))]
    public Credentials? SourceCredentials { get; set; }
    
    public Guid? SourceInfoBaseId { get; set; }
    [ForeignKey(nameof(SourceInfoBaseId))]
    public InfoBase? SourceInfoBase { get; set; }
    
    public Guid? DestinationCredentialsId { get; set; }
    [ForeignKey(nameof(DestinationCredentialsId))]
    public Credentials? DestinationCredentials { get; set; }
    
    public Guid? DestinationInfoBaseId { get; set; }
    [ForeignKey(nameof(DestinationInfoBaseId))]
    public InfoBase? DestinationInfoBase { get; set; }
}