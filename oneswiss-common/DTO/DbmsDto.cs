using MessagePack;
using OneSwiss.Common.Models;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class DbmsDto
{
    [Key(0)] public DbmsType Type { get; set; }

    [Key(1)] public string Host { get; set; } = string.Empty;

    [Key(2)] public int Port { get; set; }
}