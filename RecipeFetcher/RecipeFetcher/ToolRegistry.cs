using Microsoft.Extensions.AI;
using RecipeRecommender;
using System.Net.Http.Json;

public static class ToolRegistry
{
    private static readonly HttpClient _http = new(); //todo makke a nice Tavily service client 😁
    //when you get an API key set as env var
    private static readonly string _tavilyKey =
        Environment.GetEnvironmentVariable("TAVILY_API_KEY")
        ?? throw new InvalidOperationException("TAVILY_API_KEY is not set.");

    public static Dictionary<string, AIFunction> Build()
    {
        return new Dictionary<string, AIFunction>
        {
            ["WebSearch"] = AIFunctionFactory.Create(WebSearch),
            ["FetchPage"] = AIFunctionFactory.Create(FetchPage)
        };
    }

    /// <summary>Searches the web and returns a list of result URLs/titles/snippets.</summary>
    private static async Task<string> WebSearch(string query, int maxResults = 5)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/search")
        {
            Content = JsonContent.Create(new
            {
                query,
                search_depth = "basic",
                max_results = maxResults,
                include_raw_content = false   // keep this tool lightweight; FetchPage gets full detail
            })
        };
        request.Headers.Add("Authorization", $"Bearer {_tavilyKey}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TavilySearchResponse>();

        if (result is null || result.Results.Count == 0)
            return "No results found.";

        // Plain-text summary — easier for the model to reason over than raw JSON
        return string.Join("\n\n", result.Results.Select(r =>
            $"Title: {r.Title}\nURL: {r.Url}\nSnippet: {r.Content}"));
    }

    /// <summary>Fetches a URL and returns its readable text content.</summary>
    private static async Task<string> FetchPage(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/extract")
        {
            Content = JsonContent.Create(new
            {
                urls = new[] { url },
                extract_depth = "basic",   // "advanced" costs more credits; try basic first
                format = "text"
            })
        };
        request.Headers.Add("Authorization", $"Bearer {_tavilyKey}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TavilyExtractResponse>();

        if (result is null)
            return $"Could not extract {url}: empty response from Tavily.";

        var failure = result.FailedResults.FirstOrDefault(f => f.Url == url);
        if (failure is not null)
            return $"Could not extract {url}: {failure.Error}";

        var extracted = result.Results.FirstOrDefault(r => r.Url == url);
        if (extracted is null || string.IsNullOrWhiteSpace(extracted.RawContent))
            return $"No content extracted for {url}.";

        return extracted.RawContent;
    }
}