namespace OneSwiss.V8.Platform.RemoteAdministration;

[AttributeUsage(AttributeTargets.Property)]
public class RacFieldAttribute(string name) : Attribute
{
    public string Name { get; set; } = name;
    public RacFieldTrueFalseForm TrueFalseForm { get; set; } = RacFieldTrueFalseForm.TrueFalse;
}