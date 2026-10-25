namespace RecipeRecommender;

public static class AgentFactoryResolver
{
    public static IAgentFactory Resolve(string provider)
    {
        return provider.Trim().ToLowerInvariant() switch
        {
            "anthropic" => new AnthropicAgentFactory(),
            _ => throw new InvalidOperationException($"Unsupported LLM provider '{provider}'.")
        };
    }
}
