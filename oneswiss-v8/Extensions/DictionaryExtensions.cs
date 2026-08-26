using System.Globalization;
using System.Reflection;
using OneSwiss.V8.Platform.RemoteAdministration;

namespace OneSwiss.V8.Extensions;

public static class DictionaryExtensions
{
    public static T CreateRacObject<T>(this Dictionary<string, string> racFields, List<string>? excludeFields = null)
        where T : class, new()
    {
        var properties = typeof(T).GetProperties();
        var racObject = new T();

        foreach (var (racFieldName, racFieldValue) in racFields)
        {
            var property =
                properties.FirstOrDefault(c => c.GetCustomAttribute<RacFieldAttribute>()?.Name == racFieldName);
            if (property == null)
                property = properties.FirstOrDefault(c =>
                    c.Name.Equals(racFieldName, StringComparison.CurrentCultureIgnoreCase));

            if (excludeFields?.Contains(racFieldName) == true)
                continue;

            if (property == null)
                continue;

            if (property.PropertyType == typeof(bool))
                property.SetValue(racObject, racFieldValue.ToLower() is "1" or "on" or "yes" or "allow" or "used");
            else if (property.PropertyType == typeof(string))
                property.SetValue(racObject, racFieldValue.Trim('"'));
            else if (property.PropertyType == typeof(int) && int.TryParse(racFieldValue, out var i))
                property.SetValue(racObject, i);
            else if (property.PropertyType == typeof(long) && long.TryParse(racFieldValue, out var l))
                property.SetValue(racObject, l);
            else if (property.PropertyType == typeof(double) && double.TryParse(racFieldValue, NumberStyles.Any,
                         CultureInfo.InvariantCulture, out var d))
                property.SetValue(racObject, d);
            else if (property.PropertyType == typeof(DateTime) && DateTime.TryParse(racFieldValue, out var dt))
                property.SetValue(racObject, dt);
            else if (property.PropertyType.IsEnum)
                property.SetValue(racObject, Enum.Parse(property.PropertyType, racFieldValue.Replace(" ", ""), true));
            else
                property.SetValue(racObject, racFieldValue);
        }

        return racObject;
    }
}