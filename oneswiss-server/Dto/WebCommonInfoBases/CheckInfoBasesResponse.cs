using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace OneSwiss.Server.Dto.WebCommonInfoBases;

public class CheckInfoBasesResponse
{
    [JsonPropertyName("InfoBasesChanged")]
    public bool InfoBasesChanged { get; set; }

    [JsonPropertyName("URL")] 
    public string Url { get; set; } = string.Empty;
}