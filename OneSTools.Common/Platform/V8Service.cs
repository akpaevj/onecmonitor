using MessagePack;

namespace OneSTools.Common.Platform;

[MessagePackObject]
public class V8Service
{
    [Key(0)] 
    public V8ServiceType Type { get; set; }
    [Key(1)] 
    public string Name { get; set; }
    [Key(2)] 
    public bool IsActive { get; set; }
    [Key(3)] 
    public int Port { get; set; }
}