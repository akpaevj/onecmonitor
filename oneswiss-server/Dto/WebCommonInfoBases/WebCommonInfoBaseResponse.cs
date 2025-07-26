using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.WebCommonInfoBases;

public class WebCommonInfoBaseResponse<T>(T root) where T : class, new()
{
    [JsonPropertyName("root")] 
    public T Root { get; set; } = root;

    public static string CreateResponse(Action<T> dataFill)
    {
        var data = new T();
        dataFill(data);

        return JsonSerializer.Serialize(new WebCommonInfoBaseResponse<T>(data));
    }
}