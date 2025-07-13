using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class SystemInfoDto
{
    [Key(0)] 
    public string HostName { get; set; } = string.Empty;

    [Key(1)] 
    public string[] IpAddresses { get; set; } = [];
}