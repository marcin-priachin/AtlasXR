using System.Collections.Generic;
using AtlasXR.AI.Runtime;
using AtlasXR.Core.Logging;
using AtlasXR.Procedures.Runtime;
using AtlasXR.XR.Highlighting;

namespace AtlasXR.AI.Tools
{
    public sealed class AgentToolExecutor : IAgentToolExecutor
    {
        private readonly ProcedureEngine procedureEngine;
        private readonly IHighlightService highlightService;
        private readonly IAtlasLogger logger;

        public AgentToolExecutor(ProcedureEngine procedureEngine, IHighlightService highlightService, IAtlasLogger logger)
        {
            this.procedureEngine = procedureEngine;
            this.highlightService = highlightService;
            this.logger = logger;
        }

        public IReadOnlyList<AgentToolResult> ExecuteAll(IReadOnlyList<AgentToolCall> toolCalls)
        {
            var results = new List<AgentToolResult>();
            if (toolCalls == null)
            {
                return results;
            }

            foreach (var toolCall in toolCalls)
            {
                results.Add(Execute(toolCall));
            }

            return results;
        }

        public AgentToolResult Execute(AgentToolCall toolCall)
        {
            if (toolCall == null || string.IsNullOrWhiteSpace(toolCall.tool))
            {
                return Fail("Agent returned an empty tool call.");
            }

            switch (toolCall.tool.Trim())
            {
                case "HighlightComponent":
                    if (string.IsNullOrWhiteSpace(toolCall.componentId))
                    {
                        return Fail("HighlightComponent requires componentId.");
                    }

                    return highlightService.HighlightComponent(toolCall.componentId)
                        ? Pass($"Highlighted {toolCall.componentId}.")
                        : Fail($"Could not highlight {toolCall.componentId}.");
                case "ClearHighlight":
                    highlightService.ClearHighlight();
                    return Pass("Cleared highlight.");
                case "NextStep":
                case "CompleteStep":
                    procedureEngine.CompleteCurrentStep();
                    return Pass("Advanced procedure.");
                case "RepeatStep":
                case "ShowInstruction":
                    var instruction = procedureEngine.CurrentStep?.instruction;
                    return Pass(string.IsNullOrWhiteSpace(instruction) ? "No active instruction." : instruction);
                default:
                    return Fail($"Unknown agent tool '{toolCall.tool}'.");
            }
        }

        private AgentToolResult Pass(string message)
        {
            logger?.Info(message);
            return new AgentToolResult { success = true, message = message };
        }

        private AgentToolResult Fail(string message)
        {
            logger?.Warning(message);
            return new AgentToolResult { success = false, message = message };
        }
    }
}
