using System.Text.Json.Serialization;

public class TavilySearchResponse
{
    [JsonPropertyName("results")]
    public List<TavilyResult> Results { get; set; } = new();
}

public class TavilyResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";   // AI-relevant excerpt, not full raw HTML
}