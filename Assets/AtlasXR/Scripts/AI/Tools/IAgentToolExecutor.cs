using System.Collections.Generic;
using AtlasXR.AI.Runtime;

namespace AtlasXR.AI.Tools
{
    public interface IAgentToolExecutor
    {
        AgentToolResult Execute(AgentToolCall toolCall);

        IReadOnlyList<AgentToolResult> ExecuteAll(IReadOnlyList<AgentToolCall> toolCalls);
    }
}
