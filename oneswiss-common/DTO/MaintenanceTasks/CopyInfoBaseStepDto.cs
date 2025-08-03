using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class CopyInfoBaseStepDto
{
    [Key(0)]
    public CredentialsDto SourceCredentials { get; set; }
    [Key(1)]
    public InfoBaseDto SourceInfoBase { get; set; }
    [Key(2)]
    public CredentialsDto DestinationCredentials { get; set; }
    [Key(3)]
    public InfoBaseDto DestinationInfoBase { get; set; }
}