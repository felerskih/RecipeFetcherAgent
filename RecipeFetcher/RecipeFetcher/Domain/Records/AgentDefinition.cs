public record AgentDefinition
{
    public string Name { get; init; }
    public string Provider { get; init; }
    public string Model { get; init; }
    public List<string> Tools { get; init; }
    public double Temperature { get; init; }
    public string Instructions { get; init; }
}
