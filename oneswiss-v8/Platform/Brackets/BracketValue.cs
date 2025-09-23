using OneSTools.BracketsFile;

namespace OneSwiss.V8.Platform.Brackets;

public class BracketValue
{
    public BracketValueType Type { get; init; }
    public string StringValue { get; set; } = string.Empty;
    public List<BracketValue> ObjectValue = [];
    
    public static BracketValue CreateString(string value) => new() { Type = BracketValueType.String, StringValue = value };
    public static BracketValue CreateObject(List<BracketValue> values) => new() { Type = BracketValueType.Object, ObjectValue = values };
    public static BracketValue CreateEmpty() => new() { Type = BracketValueType.Empty };
}