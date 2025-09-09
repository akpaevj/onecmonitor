namespace OneSwiss.V8.Extensions;

public static class ListExtensions
{
    public static List<T> ToRacObjects<T>(
        this List<Dictionary<string, string>> racFieldsList,
        List<string>? excludeFields = null,
        Action<Dictionary<string, string>, T>? transformAction = null) where T : class, new()
    {
        return racFieldsList.Select(c =>
        {
            var item = c.CreateRacObject<T>(excludeFields);
            transformAction?.Invoke(c, item);
            return item;
        }).ToList();
    }
}