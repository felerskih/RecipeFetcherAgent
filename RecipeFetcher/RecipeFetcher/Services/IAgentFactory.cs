using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace RecipeRecommender;

public interface IAgentFactory
{
    AIAgent CreateAgent(AgentDefinition definition, IList<AITool>? tools);
}
