namespace RecipeFetcher.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentVariableTestCollection : ICollectionFixture<EnvironmentVariableFixture>
{
    public const string Name = "Environment variable tests";
}

public sealed class EnvironmentVariableFixture : IDisposable
{
    private readonly string? _previousTavilyApiKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
    private readonly string? _previousAnthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

    public EnvironmentVariableFixture()
    {
        Environment.SetEnvironmentVariable("TAVILY_API_KEY", "test-tavily-key");
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "test-anthropic-key");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("TAVILY_API_KEY", _previousTavilyApiKey);
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", _previousAnthropicApiKey);
    }
}
