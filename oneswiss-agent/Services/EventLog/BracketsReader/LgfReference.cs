namespace OneSwiss.Agent.Services.EventLog.BracketsReader;

internal class LgfReference(string objectValue, string reference)
{
    public string ObjectValue { get; } = objectValue;
    public string Reference { get; } = reference;
    
    internal static LgfReference EmptyInstance { get; } = new(string.Empty, Guid.Empty.ToString());
}