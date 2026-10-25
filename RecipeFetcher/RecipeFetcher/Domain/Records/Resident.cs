using System.Text.Json.Serialization;

public sealed record Resident
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("food_preferences")]
    public List<string> FoodPreferences { get; init; } = [];

    [JsonPropertyName("max_cook_time_minutes")]
    public int? MaxCookTimeMinutes { get; init; }

    [JsonPropertyName("dietary_restrictions")]
    public List<string> DietaryRestrictions { get; init; } = [];

    [JsonPropertyName("skill_level")]
    public SkillLevel? SkillLevel { get; init; }

    [JsonPropertyName("budget")]
    public string? Budget { get; init; }
}
