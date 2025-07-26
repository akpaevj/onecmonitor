using System.Text.Json.Serialization;

namespace OneSwiss.Server.Dto.WebCommonInfoBases;

public class GetInfoBasesResponse
{
    [JsonPropertyName("ClientId")]
    public string ClientId { get; set; }

    [JsonPropertyName("InfoBasesCheckCode")]
    public string InfoBasesCheckCode { get; set; }
    
    [JsonPropertyName("InfoBases")]
    public string InfoBases { get; set; }
}