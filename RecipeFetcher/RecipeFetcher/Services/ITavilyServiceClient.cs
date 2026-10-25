namespace RecipeRecommender;

public interface ITavilyServiceClient
{
    Task<string> SearchAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default);
    Task<string> ExtractAsync(string url, CancellationToken cancellationToken = default);
}
