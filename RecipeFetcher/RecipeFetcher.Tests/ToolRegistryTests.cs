using System.Text.Json;
using AutoFixture;
using Microsoft.Extensions.AI;
using Moq;
using RecipeRecommender;

namespace RecipeFetcher.Tests;

public sealed class ToolRegistryTests : IClassFixture<TestDataFixture>
{
    private readonly IFixture _fixture;

    public ToolRegistryTests(TestDataFixture fixture)
    {
        _fixture = fixture.Fixture;
    }

    [Fact]
    public async Task Build_RegistersToolsAndDelegatesInputsAndResults()
    {
        var query = _fixture.Create<string>();
        var url = _fixture.Create<Uri>().AbsoluteUri;
        var searchResult = _fixture.Create<string>();
        var extractResult = _fixture.Create<string>();
        var client = _fixture.Create<Mock<ITavilyServiceClient>>();
        client
            .Setup(service => service.SearchAsync(query, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResult);
        client
            .Setup(service => service.ExtractAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractResult);

        var tools = ToolRegistry.Build(client.Object);

        Assert.Equal(new[] { "WebSearch", "FetchPage" }, tools.Keys);

        var search = GetFunctionArguments(
            ("query", query),
            ("maxResults", 2));
        var actualSearchResult = await InvokeAsync(tools["WebSearch"], search);
        Assert.Equal(searchResult, actualSearchResult);
        client.Verify(
            service => service.SearchAsync(query, 2, It.IsAny<CancellationToken>()),
            Times.Once);

        var extract = GetFunctionArguments(("url", url));
        var actualExtractResult = await InvokeAsync(tools["FetchPage"], extract);
        Assert.Equal(extractResult, actualExtractResult);
        client.Verify(
            service => service.ExtractAsync(url, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Build_WebSearchWithoutMaxResults_UsesDefaultLimit()
    {
        var query = _fixture.Create<string>();
        var client = _fixture.Create<Mock<ITavilyServiceClient>>();
        client
            .Setup(service => service.SearchAsync(query, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<string>());

        var tools = ToolRegistry.Build(client.Object);
        var arguments = GetFunctionArguments(("query", query));

        await InvokeAsync(tools["WebSearch"], arguments);

        client.Verify(
            service => service.SearchAsync(query, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AIFunctionArguments GetFunctionArguments(
        params (string Name, object Value)[] arguments)
    {
        var result = new AIFunctionArguments();
        foreach (var (name, value) in arguments)
            result[name] = value;
        return result;
    }

    private static async Task<string?> InvokeAsync(AIFunction function, AIFunctionArguments arguments)
    {
        var result = await function.InvokeAsync(arguments);
        return result is JsonElement json ? json.GetString() : result as string;
    }
}
