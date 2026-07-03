using System;
using System.Threading;
using System.Threading.Tasks;

namespace AtlasXR.AI.Runtime
{
    public sealed class MockAgentProvider : IAgentProvider
    {
        public Task<AgentResponse> GenerateResponseAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var command = request?.userCommand?.Trim().ToLowerInvariant() ?? string.Empty;
            var response = new AgentResponse();

            if (command.Contains("next") || command.Contains("done") || command.Contains("complete"))
            {
                response.assistantMessage = "Advancing to the next maintenance step.";
                response.toolCalls.Add(new AgentToolCall { tool = "NextStep", reason = "User asked to continue." });
                return Task.FromResult(response);
            }

            if (command.Contains("repeat") || command.Contains("again"))
            {
                response.assistantMessage = request?.context?.currentInstruction ?? "Repeat the current step.";
                response.toolCalls.Add(new AgentToolCall { tool = "RepeatStep", reason = "User asked to hear the instruction again." });
                return Task.FromResult(response);
            }

            if (command.Contains("clear"))
            {
                response.assistantMessage = "Clearing the current component highlight.";
                response.toolCalls.Add(new AgentToolCall { tool = "ClearHighlight", reason = "User requested a clear highlight." });
                return Task.FromResult(response);
            }

            var componentId = command.Contains("filter") ? "air_filter" : string.Empty;
            response.assistantMessage = "I can guide the air filter replacement. Start with the current instruction and inspect the highlighted component.";
            response.toolCalls.Add(new AgentToolCall
            {
                tool = string.IsNullOrWhiteSpace(componentId) ? "ShowInstruction" : "HighlightComponent",
                componentId = componentId,
                instruction = request?.context?.currentInstruction,
                reason = "Mock response for offline testing."
            });

            return Task.FromResult(response);
        }
    }
}
