using Anthropic;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

//Must set ANTHROPIC_API_KEY in env var
AnthropicClient client = new AnthropicClient();

var def = AgentDefinitionLoader.Load("Agents/recipe-recommender.agent.md");

var toolRegistry = ToolRegistry.Build();

var tools = def.Tools
    .Where(t => toolRegistry.ContainsKey(t))
    .Select(t => (AITool)(object)toolRegistry[t])
    .ToList();

AIAgent agent = client.AsAIAgent(
    model: def.Model,          // "claude-sonnet-4-6" from the frontmatter
    name: def.Name,
    instructions: def.Instructions,
    tools: tools);

var result = await agent.RunAsync("household_size: 4, food_preferences: Italian, weeknight-friendly; max_cook_time_minutes: 30; dietary_restrictions: vegetarian");
Console.WriteLine(result.Text);
