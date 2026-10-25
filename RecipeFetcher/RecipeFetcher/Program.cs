using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using RecipeRecommender;

var def = AgentDefinitionLoader.Load("Agents/recipe-recommender.agent.md");

var tavilyServiceClient = new TavilyServiceClient();
var toolRegistry = ToolRegistry.Build(tavilyServiceClient);

var tools = def.Tools
    .Where(t => toolRegistry.ContainsKey(t))
    .Select(t => (AITool)(object)toolRegistry[t])
    .ToList();

var agentFactory = AgentFactoryResolver.Resolve(def.Provider);
AIAgent agent = agentFactory.CreateAgent(def, tools);

var result = await agent.RunAsync("household_size: 4, food_preferences: Italian, weeknight-friendly; max_cook_time_minutes: 30; dietary_restrictions: vegetarian");
Console.WriteLine(result.Text);
