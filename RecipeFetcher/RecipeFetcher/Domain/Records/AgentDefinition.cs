public record AgentDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public List<string> Tools { get; init; } = [];
    public double Temperature { get; init; }
    public string Instructions { get; init; } = string.Empty;
}
