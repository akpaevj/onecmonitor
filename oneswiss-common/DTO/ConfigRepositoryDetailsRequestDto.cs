using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class ConfigRepositoryDetailsRequestDto
{
    [Key(1)] public int CrServerPort { get; set; }

    [Key(2)] public string Repository { get; set; }
}