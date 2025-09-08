using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Common.Extensions;

public static class EnumExtension
{
    public static string GetDisplay(this Enum value)
    {
        return value.GetAttributeOfType<DisplayAttribute>()?.Name ?? value.ToString();
    }

    private static T? GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var memInfo = type.GetMember(enumVal.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0 ? (T)attributes[0] : null;
    }
}