using AutoFixture;
using RecipeRecommender;

namespace RecipeFetcher.Tests;

[Collection(EnvironmentVariableTestCollection.Name)]
public sealed class AgentFactoryResolverTests : IClassFixture<TestDataFixture>
{
    private readonly IFixture _fixture;

    public AgentFactoryResolverTests(
        TestDataFixture fixture,
        EnvironmentVariableFixture environmentVariableFixture)
    {
        _fixture = fixture.Fixture;
    }

    [Fact]
    public void Resolve_AnthropicProviderIgnoringCaseAndWhitespace_ReturnsAnthropicFactory()
    {
        Assert.IsType<AnthropicAgentFactory>(AgentFactoryResolver.Resolve("  AnThRoPiC  "));
    }

    [Fact]
    public void Resolve_UnsupportedProvider_ThrowsInvalidOperationException()
    {
        var provider = $"{_fixture.Create<string>()}_unsupported";

        var exception = Assert.Throws<InvalidOperationException>(
            () => AgentFactoryResolver.Resolve(provider));

        Assert.Contains($"Unsupported LLM provider '{provider}'", exception.Message);
    }
}
