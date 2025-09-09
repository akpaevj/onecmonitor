using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class ChangeClusterObjectParametersRequestDto<T>
{
    [Key(0)] public T Item { get; set; }

    [Key(1)] public Dictionary<string, string> Parameters { get; set; }
}