using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions AgentInputSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [JsonPropertyName("residents")]
    public List<Resident> Residents { get; init; } = [];

    public static AppSettings Load(string path)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), AgentInputSerializerOptions);
        if (settings is null)
            throw new InvalidOperationException($"'{path}' contains no app settings.");

        settings.Validate(path);
        return settings;
    }

    public string BuildAgentInput()
    {
        Validate("app settings");

        var input = new AgentInput
        {
            HouseholdSize = Residents.Count,
            FoodPreferences = Residents
                .SelectMany(resident => resident.FoodPreferences)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            MaxCookTimeMinutes = Residents
                .Where(resident => resident.MaxCookTimeMinutes.HasValue)
                .Select(resident => resident.MaxCookTimeMinutes)
                .Min(),
            DietaryRestrictions = Residents
                .SelectMany(resident => resident.DietaryRestrictions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            SkillLevel = GetHighestSkillLevel(Residents.Select(resident => resident.SkillLevel)),
            Budget = JoinDistinct(Residents.Select(resident => resident.Budget))
        };

        return JsonSerializer.Serialize(input, AgentInputSerializerOptions);
    }

    private void Validate(string source)
    {
        if (Residents is null || Residents.Count == 0)
            throw new InvalidOperationException($"'{source}' must define at least one resident.");

        var residentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < Residents.Count; index++)
        {
            var resident = Residents[index];
            if (resident is null)
                throw new InvalidOperationException($"'{source}' contains a null resident at index {index}.");

            if (string.IsNullOrWhiteSpace(resident.Id))
                throw new InvalidOperationException($"'{source}' resident at index {index} is missing an ID.");

            if (!residentIds.Add(resident.Id.Trim()))
                throw new InvalidOperationException($"'{source}' contains duplicate resident ID '{resident.Id}'.");

            if (string.IsNullOrWhiteSpace(resident.Name))
                throw new InvalidOperationException($"'{source}' resident '{resident.Id}' is missing a name.");

            ValidateValues(resident.FoodPreferences, source, resident.Id, "food_preferences");
            ValidateValues(resident.DietaryRestrictions, source, resident.Id, "dietary_restrictions");

            if (resident.MaxCookTimeMinutes is <= 0)
                throw new InvalidOperationException(
                    $"'{source}' resident '{resident.Id}' must have a positive max_cook_time_minutes value.");

            if (resident.SkillLevel is { } skillLevel && !Enum.IsDefined(skillLevel))
                throw new InvalidOperationException(
                    $"'{source}' resident '{resident.Id}' has unsupported skill_level '{skillLevel}'. " +
                    "Expected beginner, intermediate, or advanced.");
        }
    }

    private static void ValidateValues(
        List<string>? values,
        string source,
        string residentId,
        string fieldName)
    {
        if (values is null)
            throw new InvalidOperationException(
                $"'{source}' resident '{residentId}' must have a {fieldName} array; use an empty array when none apply.");

        if (values.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException(
                $"'{source}' resident '{residentId}' has an empty value in {fieldName}.");
    }

    private static SkillLevel? GetHighestSkillLevel(IEnumerable<SkillLevel?> values)
    {
        var configuredLevels = values
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .OrderByDescending(value => value)
            .ToList();

        return configuredLevels.Count == 0 ? null : configuredLevels[0];
    }

    private static string? JoinDistinct(IEnumerable<string?> values)
    {
        var distinctValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctValues.Count == 0 ? null : string.Join(", ", distinctValues);
    }

    private sealed record AgentInput
    {
        [JsonPropertyName("household_size")]
        public int HouseholdSize { get; init; }

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
}
