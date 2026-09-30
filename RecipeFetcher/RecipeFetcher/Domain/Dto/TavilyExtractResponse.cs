namespace RecipeRecommender;

using System.Text.Json.Serialization;

public class TavilyExtractResponse
{
    [JsonPropertyName("results")]
    public List<TavilyExtractResult> Results { get; set; } = new();

    [JsonPropertyName("failed_results")]
    public List<TavilyFailedResult> FailedResults { get; set; } = new();
}

public class TavilyExtractResult
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("raw_content")]
    public string RawContent { get; set; } = "";
}

public class TavilyFailedResult
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("error")]
    public string Error { get; set; } = "";
}