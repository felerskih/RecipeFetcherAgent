using Microsoft.Extensions.AI;
using RecipeRecommender;

public static class ToolRegistry
{
    public static Dictionary<string, AIFunction> Build(ITavilyServiceClient? tavilyServiceClient = null)
    {
        var client = tavilyServiceClient ?? new TavilyServiceClient();

        return new Dictionary<string, AIFunction>
        {
            ["WebSearch"] = AIFunctionFactory.Create((string query, int maxResults = 5) =>
                client.SearchAsync(query, maxResults)),
            ["FetchPage"] = AIFunctionFactory.Create((string url) => client.ExtractAsync(url))
        };
    }
}
