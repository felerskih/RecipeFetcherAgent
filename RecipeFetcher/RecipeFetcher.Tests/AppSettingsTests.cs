using AutoFixture;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecipeFetcher.Tests;

public sealed class AppSettingsTests : IClassFixture<TestDataFixture>
{
    private readonly IFixture _fixture;

    public AppSettingsTests(TestDataFixture fixture)
    {
        _fixture = fixture.Fixture;
    }

    [Fact]
    public void Load_ResidentConfig_PreservesResidentsAndBuildsFlatHouseholdInput()
    {
        var firstResident = _fixture.Build<Resident>()
            .With(resident => resident.Id, "alex")
            .With(resident => resident.Name, "Alex")
            .With(resident => resident.FoodPreferences, new List<string> { "Italian", "weeknight-friendly" })
            .With(resident => resident.MaxCookTimeMinutes, 30)
            .With(resident => resident.DietaryRestrictions, new List<string> { "vegetarian" })
            .With(resident => resident.SkillLevel, SkillLevel.Intermediate)
            .With(resident => resident.Budget, "moderate")
            .Create();
        var secondResident = _fixture.Build<Resident>()
            .With(resident => resident.Id, "jordan")
            .With(resident => resident.Name, "Jordan")
            .With(resident => resident.FoodPreferences, new List<string> { "spicy", "one-pot meals" })
            .With(resident => resident.MaxCookTimeMinutes, 20)
            .With(resident => resident.DietaryRestrictions, new List<string> { "vegetarian", "nut-free" })
            .With(resident => resident.SkillLevel, SkillLevel.Advanced)
            .With(resident => resident.Budget, "low-cost")
            .Create();
        var settings = LoadFromTemporaryFile(JsonSerializer.Serialize(
            new AppSettings { Residents = [firstResident, secondResident] },
            new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            }));

        Assert.Equal(2, settings.Residents.Count);
        Assert.Equal("alex", settings.Residents[0].Id);
        Assert.Equal("Alex", settings.Residents[0].Name);
        Assert.Equal(firstResident.FoodPreferences, settings.Residents[0].FoodPreferences);
        Assert.Equal(30, settings.Residents[0].MaxCookTimeMinutes);
        Assert.Equal(firstResident.DietaryRestrictions, settings.Residents[0].DietaryRestrictions);
        Assert.Equal("jordan", settings.Residents[1].Id);
        Assert.Equal("Jordan", settings.Residents[1].Name);
        Assert.Equal(secondResident.FoodPreferences, settings.Residents[1].FoodPreferences);
        Assert.Equal(20, settings.Residents[1].MaxCookTimeMinutes);
        Assert.Equal(secondResident.DietaryRestrictions, settings.Residents[1].DietaryRestrictions);

        using var input = JsonDocument.Parse(settings.BuildAgentInput());
        var root = input.RootElement;

        Assert.Equal(2, root.GetProperty("household_size").GetInt32());
        Assert.Equal(
            new[] { "Italian", "weeknight-friendly", "spicy", "one-pot meals" },
            ReadStrings(root.GetProperty("food_preferences")));
        Assert.Equal(20, root.GetProperty("max_cook_time_minutes").GetInt32());
        Assert.Equal(new[] { "vegetarian", "nut-free" }, ReadStrings(root.GetProperty("dietary_restrictions")));
        Assert.Equal("advanced", root.GetProperty("skill_level").GetString());
        Assert.Equal("moderate, low-cost", root.GetProperty("budget").GetString());
        Assert.False(root.TryGetProperty("residents", out _));
    }

    [Fact]
    public void BuildAgentInput_DeduplicatesPreferencesAndRestrictionsIgnoringCase()
    {
        var residents = _fixture.CreateMany<Resident>(2)
            .Select((resident, index) => resident with
            {
                FoodPreferences = index == 0 ? ["Italian"] : ["italian", "spicy"],
                DietaryRestrictions = index == 0 ? ["vegetarian"] : ["VEGETARIAN"],
                MaxCookTimeMinutes = null,
                SkillLevel = null,
                Budget = null
            })
            .ToList();
        var settings = new AppSettings { Residents = residents };

        using var input = JsonDocument.Parse(settings.BuildAgentInput());
        var root = input.RootElement;

        Assert.Equal(new[] { "Italian", "spicy" }, ReadStrings(root.GetProperty("food_preferences")));
        Assert.Equal(new[] { "vegetarian" }, ReadStrings(root.GetProperty("dietary_restrictions")));
        Assert.False(root.TryGetProperty("max_cook_time_minutes", out _));
        Assert.False(root.TryGetProperty("skill_level", out _));
        Assert.False(root.TryGetProperty("budget", out _));
    }

    [Fact]
    public void BuildAgentInput_WithNoResidents_ThrowsInvalidOperationException()
    {
        var settings = new AppSettings();

        var exception = Assert.Throws<InvalidOperationException>(
            () => settings.BuildAgentInput());

        Assert.Contains("must define at least one resident", exception.Message);
    }

    [Fact]
    public void Load_EmptyJson_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => LoadFromTemporaryFile("null"));

        Assert.Contains("contains no app settings", exception.Message);
    }

    [Theory]
    [InlineData("""{"residents":null}""", "must define at least one resident")]
    [InlineData("""{"residents":[null]}""", "null resident")]
    [InlineData("""{"residents":[{"id":"alex","name":"Alex","food_preferences":null,"dietary_restrictions":[]}]}""", "food_preferences array")]
    [InlineData("""{"residents":[{"id":"alex","name":"Alex","food_preferences":[],"dietary_restrictions":null}]}""", "dietary_restrictions array")]
    [InlineData("""{"residents":[{"id":"alex","name":"Alex","food_preferences":[],"dietary_restrictions":[],"max_cook_time_minutes":0}]}""", "positive max_cook_time_minutes")]
    [InlineData("""{"residents":[{"id":"alex","name":"Alex","food_preferences":[],"dietary_restrictions":[],"skill_level":100}]}""", "unsupported skill_level")]
    public void Load_InvalidResidentData_ThrowsInvalidOperationException(string json, string expectedMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => LoadFromTemporaryFile(json));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void Load_DuplicateResidentIdsIgnoringCaseAndWhitespace_ThrowsInvalidOperationException()
    {
        const string json = """
            {
              "residents": [
                { "id": "alex", "name": "Alex", "food_preferences": [], "dietary_restrictions": [] },
                { "id": " ALEX ", "name": "Another Alex", "food_preferences": [], "dietary_restrictions": [] }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(
            () => LoadFromTemporaryFile(json));

        Assert.Contains("duplicate resident ID", exception.Message);
    }

    private static List<string> ReadStrings(JsonElement element) =>
        element.EnumerateArray().Select(value => value.GetString()!).ToList();

    private static AppSettings LoadFromTemporaryFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, content);
            return AppSettings.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
