using AutoFixture;
using YamlDotNet.Core;

namespace RecipeFetcher.Tests;

public sealed class AgentDefinitionLoaderTests : IClassFixture<TestDataFixture>
{
    private readonly IFixture _fixture;

    public AgentDefinitionLoaderTests(TestDataFixture fixture)
    {
        _fixture = fixture.Fixture;
    }

    [Fact]
    public void Load_ValidFrontmatter_PreservesDefinitionData()
    {
        var expected = _fixture.Build<AgentDefinition>()
            .With(definition => definition.Tools, new List<string>
            {
                _fixture.Create<string>(),
                _fixture.Create<string>()
            })
            .Create();

        var definition = LoadFromTemporaryFile($"""
            ---
            name: {expected.Name}
            provider: {expected.Provider}
            model: {expected.Model}
            tools: [{string.Join(", ", expected.Tools)}]
            temperature: {expected.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)}
            ---
            {expected.Instructions}
            """);

        Assert.Equal(expected.Name, definition.Name);
        Assert.Equal(expected.Provider, definition.Provider);
        Assert.Equal(expected.Model, definition.Model);
        Assert.Equal(expected.Tools, definition.Tools);
        Assert.Equal(expected.Temperature, definition.Temperature);
        Assert.Equal(expected.Instructions, definition.Instructions);
    }

    [Fact]
    public void Load_MissingFrontmatter_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => LoadFromTemporaryFile("No frontmatter here."));

        Assert.Contains("missing YAML frontmatter", exception.Message);
    }

    [Fact]
    public void Load_MissingRequiredMetadata_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => LoadFromTemporaryFile("""
                ---
                name: RecipeAgent
                provider: anthropic
                ---
                Instructions
                """));

        Assert.Contains("missing required field 'model'", exception.Message);
    }

    [Fact]
    public void Load_OmittedOptionalMetadata_UsesEmptyToolsAndZeroTemperature()
    {
        var name = _fixture.Create<string>();
        var model = _fixture.Create<string>();

        var definition = LoadFromTemporaryFile($"""
            ---
            name: {name}
            provider: anthropic
            model: {model}
            ---
            Instructions
            """);

        Assert.Empty(definition.Tools);
        Assert.Equal(0, definition.Temperature);
    }

    [Fact]
    public void Load_MalformedYaml_ThrowsYamlException()
    {
        Assert.Throws<YamlException>(() => LoadFromTemporaryFile("""
            ---
            name: [unclosed
            provider: anthropic
            model: model-name
            ---
            Instructions
            """));
    }

    [Fact]
    public void Load_ToolsIsNotASequence_ThrowsInvalidCastException()
    {
        Assert.Throws<InvalidCastException>(() => LoadFromTemporaryFile("""
            ---
            name: RecipeAgent
            provider: anthropic
            model: model-name
            tools: WebSearch
            ---
            Instructions
            """));
    }

    [Fact]
    public void Load_TemperatureIsNotNumeric_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LoadFromTemporaryFile("""
            ---
            name: RecipeAgent
            provider: anthropic
            model: model-name
            temperature: warm
            ---
            Instructions
            """));
    }

    private static AgentDefinition LoadFromTemporaryFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.agent.md");
        try
        {
            File.WriteAllText(path, content);
            return AgentDefinitionLoader.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
