using Anthropic;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace RecipeRecommender;

public sealed class AnthropicAgentFactory : IAgentFactory
{
    private readonly AnthropicClient _client = new();

    public AIAgent CreateAgent(AgentDefinition definition, IList<AITool>? tools)
    {
        return _client.AsAIAgent(
            model: definition.Model,
            name: definition.Name,
            instructions: definition.Instructions,
            tools: tools);
    }
}
