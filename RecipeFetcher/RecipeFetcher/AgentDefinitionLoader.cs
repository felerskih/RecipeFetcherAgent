using YamlDotNet.Serialization;

public static class AgentDefinitionLoader
{
    public static AgentDefinition Load(string path)
    {
        var raw = File.ReadAllText(path);

        // Split on the "---" frontmatter delimiters
        var parts = raw.Split("---", 3, StringSplitOptions.None);
        if (parts.Length < 3)
            throw new InvalidOperationException($"'{path}' is missing YAML frontmatter delimited by '---'.");

        var frontmatter = parts[1];
        var instructions = parts[2].Trim();

        var deserializer = new DeserializerBuilder().Build();
        var meta = deserializer.Deserialize<Dictionary<string, object>>(frontmatter)
            ?? throw new InvalidOperationException($"'{path}' has empty or malformed frontmatter.");

        return new AgentDefinition
        {
            Name = RequireString(meta, "name", path),
            Provider = RequireString(meta, "provider", path),
            Model = RequireString(meta, "model", path),
            Tools = meta.TryGetValue("tools", out var toolsObj)
                ? ((IEnumerable<object>)toolsObj).Select(t => t.ToString()!).ToList()
                : new List<string>(),
            Temperature = meta.TryGetValue("temperature", out var tempObj)
                ? Convert.ToDouble(tempObj)
                : 0.0,
            Instructions = instructions
        };
    }

    private static string RequireString(Dictionary<string, object> meta, string key, string path)
    {
        if (!meta.TryGetValue(key, out var value) || value is null)
            throw new InvalidOperationException($"'{path}' frontmatter is missing required field '{key}'.");
        return value.ToString()!;
    }
}