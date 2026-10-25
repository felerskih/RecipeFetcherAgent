using System.Net.Http.Json;

namespace RecipeRecommender;

public sealed class TavilyServiceClient : ITavilyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public TavilyServiceClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _apiKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY")
            ?? throw new InvalidOperationException("TAVILY_API_KEY is not set.");
    }

    public async Task<string> SearchAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/search")
        {
            Content = JsonContent.Create(new
            {
                query,
                search_depth = "basic",
                max_results = maxResults,
                include_raw_content = false
            })
        };

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TavilySearchResponse>(cancellationToken: cancellationToken);

        if (result is null || result.Results.Count == 0)
            return "No results found.";

        return string.Join("\n\n", result.Results.Select(r =>
            $"Title: {r.Title}\nURL: {r.Url}\nSnippet: {r.Content}"));
    }

    public async Task<string> ExtractAsync(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/extract")
        {
            Content = JsonContent.Create(new
            {
                urls = new[] { url },
                extract_depth = "basic",
                format = "text"
            })
        };

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TavilyExtractResponse>(cancellationToken: cancellationToken);

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
