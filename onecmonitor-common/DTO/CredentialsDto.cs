using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class CredentialsDto
{
    [Key(0)]
    public string User { get; set; }
    [Key(1)]
    public string Password { get; set; }
}