using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using RecipeRecommender;

namespace RecipeFetcher.Tests;

[Collection(EnvironmentVariableTestCollection.Name)]
public sealed class TavilyServiceClientTests : IClassFixture<TestDataFixture>
{
    private readonly IFixture _fixture;

    public TavilyServiceClientTests(
        TestDataFixture fixture,
        EnvironmentVariableFixture environmentVariableFixture)
    {
        _fixture = fixture.Fixture;
    }

    [Fact]
    public async Task SearchAsync_SendsAuthenticatedSearchRequest_AndFormatsResults()
    {
        var query = _fixture.Create<string>();
        var title = _fixture.Create<string>();
        var url = _fixture.Create<Uri>().AbsoluteUri;
        var snippet = _fixture.Create<string>();
        var responseBody = JsonSerializer.Serialize(new
        {
            results = new[] { new { title, url, content = snippet } }
        });

        using var context = new TavilyClientContext((request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.tavily.com/search", request.RequestUri!.ToString());
            Assert.Equal("Bearer test-tavily-key", request.Headers.Authorization!.ToString());

            return AssertRequestBodyAndRespond(request, cancellationToken, new
            {
                query,
                search_depth = "basic",
                max_results = 3,
                include_raw_content = false
            }, responseBody);
        });

        var result = await context.Client.SearchAsync(query, 3);

        Assert.Equal(
            $"Title: {title}\nURL: {url}\nSnippet: {snippet}",
            result);
    }

    [Theory]
    [InlineData("""{"results":[]}""")]
    [InlineData("null")]
    public async Task SearchAsync_EmptyOrNullResponse_ReturnsNoResultsMessage(string responseBody)
    {
        using var context = new TavilyClientContext((_, _) =>
            Task.FromResult(JsonResponse(responseBody)));

        Assert.Equal("No results found.", await context.Client.SearchAsync("pasta"));
    }

    [Fact]
    public async Task SearchAsync_UnsuccessfulHttpResponse_ThrowsHttpRequestException()
    {
        using var context = new TavilyClientContext((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => context.Client.SearchAsync("pasta"));
    }

    [Fact]
    public async Task SearchAsync_MalformedJson_ThrowsJsonException()
    {
        using var context = new TavilyClientContext((_, _) =>
            Task.FromResult(JsonResponse("{not-json")));

        await Assert.ThrowsAsync<JsonException>(
            () => context.Client.SearchAsync("pasta"));
    }

    [Fact]
    public async Task SearchAsync_CanceledRequest_ThrowsOperationCanceledException()
    {
        using var context = new TavilyClientContext((_, cancellationToken) =>
            Task.FromCanceled<HttpResponseMessage>(cancellationToken));
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Client.SearchAsync("pasta", cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ExtractAsync_ValidResult_SendsRequestAndReturnsRawContent()
    {
        var url = _fixture.Create<Uri>().AbsoluteUri;
        var rawContent = _fixture.Create<string>();
        var responseBody = JsonSerializer.Serialize(new
        {
            results = new[] { new { url, raw_content = rawContent } },
            failed_results = Array.Empty<object>()
        });

        using var context = new TavilyClientContext((request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.tavily.com/extract", request.RequestUri!.ToString());
            Assert.Equal("Bearer test-tavily-key", request.Headers.Authorization!.ToString());

            return AssertRequestBodyAndRespond(request, cancellationToken, new
            {
                urls = new[] { url },
                extract_depth = "basic",
                format = "text"
            }, responseBody);
        });

        Assert.Equal(rawContent, await context.Client.ExtractAsync(url));
    }

    [Fact]
    public async Task ExtractAsync_NullResponse_ReturnsEmptyResponseMessage()
    {
        using var context = new TavilyClientContext((_, _) =>
            Task.FromResult(JsonResponse("null")));

        Assert.Equal(
            "Could not extract https://example.test/pasta: empty response from Tavily.",
            await context.Client.ExtractAsync("https://example.test/pasta"));
    }

    [Fact]
    public async Task ExtractAsync_FailedUrl_ReturnsFailureDetails()
    {
        using var context = new TavilyClientContext((_, _) =>
            Task.FromResult(JsonResponse(
                """{"results":[],"failed_results":[{"url":"https://example.test/pasta","error":"blocked"}]}""")));

        Assert.Equal(
            "Could not extract https://example.test/pasta: blocked",
            await context.Client.ExtractAsync("https://example.test/pasta"));
    }

    [Fact]
    public async Task ExtractAsync_UnsuccessfulHttpResponse_ThrowsHttpRequestException()
    {
        using var context = new TavilyClientContext((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => context.Client.ExtractAsync("https://example.test/pasta"));
    }

    [Theory]
    [InlineData("""{"results":[]}""")]
    [InlineData("""{"results":[{"url":"https://example.test/pasta","raw_content":"  "}]}""")]
    public async Task ExtractAsync_MissingOrBlankContent_ReturnsNoContentMessage(string responseBody)
    {
        using var context = new TavilyClientContext((_, _) =>
            Task.FromResult(JsonResponse(responseBody)));

        Assert.Equal(
            "No content extracted for https://example.test/pasta.",
            await context.Client.ExtractAsync("https://example.test/pasta"));
    }

    private static async Task<HttpResponseMessage> AssertRequestBodyAndRespond(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        object expectedBody,
        string responseBody)
    {
        var actualBody = JsonNode.Parse(
            await request.Content!.ReadAsStringAsync(cancellationToken))?.AsObject()
            ?? throw new InvalidOperationException("The request body was not a JSON object.");
        var expectedProperties = JsonSerializer.SerializeToNode(expectedBody)!.AsObject();

        Assert.Equal(expectedProperties.Count, actualBody.Count);
        foreach (var (propertyName, expectedValue) in expectedProperties)
        {
            Assert.True(actualBody.ContainsKey(propertyName), $"Request body is missing '{propertyName}'.");
            Assert.True(
                JsonNode.DeepEquals(expectedValue, actualBody[propertyName]),
                $"Unexpected value for request property '{propertyName}'.");
        }

        return JsonResponse(responseBody);
    }

    private static HttpResponseMessage JsonResponse(string content) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        };
}

internal sealed class TavilyClientContext : IDisposable
{
    private readonly HttpClient _httpClient;

    public TavilyClientContext(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
    {
        _httpClient = new HttpClient(new DelegateHttpMessageHandler(sendAsync));
        Client = new TavilyServiceClient(_httpClient);
    }

    public TavilyServiceClient Client { get; }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

internal sealed class DelegateHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        sendAsync(request, cancellationToken);
}
