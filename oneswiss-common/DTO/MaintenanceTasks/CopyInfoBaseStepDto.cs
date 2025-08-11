using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагКопированияИнфоБазы", "CopyInfoBaseStep")]
[MessagePackObject]
public class CopyInfoBaseStepDto
{
    [ContextProperty("УчетныеДанныеИсточника", "SourceCredentials")]
    [Key(0)]
    public CredentialsDto SourceCredentials { get; set; }
    [ContextProperty("ИБИсточник", "SourceInfoBase")]
    [Key(1)]
    public InfoBaseDto SourceInfoBase { get; set; }
    [ContextProperty("УчетныеДанныеПриемника", "DestinationCredentials")]
    [Key(2)]
    public CredentialsDto DestinationCredentials { get; set; }
    [ContextProperty("ИБПриемник", "DestinationInfoBase")]
    [Key(3)]
    public InfoBaseDto DestinationInfoBase { get; set; }
}