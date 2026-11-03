using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using RecipeRecommender;

var settings = AppSettings.Load(Path.Combine(AppContext.BaseDirectory, "config.json"));
var agentDefinitionPath = Path.Combine(
    AppContext.BaseDirectory,
    "Agents",
    "recipe-recommender.agent.md");
var def = AgentDefinitionLoader.Load(agentDefinitionPath);

var tavilyServiceClient = new TavilyServiceClient();
var toolRegistry = ToolRegistry.Build(tavilyServiceClient);

var tools = def.Tools
    .Where(t => toolRegistry.ContainsKey(t))
    .Select(t => (AITool)(object)toolRegistry[t])
    .ToList();

var agentFactory = AgentFactoryResolver.Resolve(def.Provider);
AIAgent agent = agentFactory.CreateAgent(def, tools);

var result = await agent.RunAsync(settings.BuildAgentInput());
Console.WriteLine(result.Text);
