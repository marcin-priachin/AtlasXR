using System.Text;
using UnityEngine;

namespace AtlasXR.AI.Runtime
{
    public static class MaintenanceAssistantPrompt
    {
        public const string ResponseSchemaJson =
            "{\"type\":\"object\",\"additionalProperties\":false,\"properties\":{" +
            "\"assistantMessage\":{\"type\":\"string\"}," +
            "\"toolCalls\":{\"type\":\"array\",\"items\":{\"type\":\"object\",\"additionalProperties\":false,\"properties\":{" +
            "\"tool\":{\"type\":\"string\",\"enum\":[\"HighlightComponent\",\"ClearHighlight\",\"ShowInstruction\",\"NextStep\",\"RepeatStep\",\"CompleteStep\"]}," +
            "\"componentId\":{\"type\":\"string\"}," +
            "\"instruction\":{\"type\":\"string\"}," +
            "\"stepId\":{\"type\":\"string\"}," +
            "\"reason\":{\"type\":\"string\"}" +
            "},\"required\":[\"tool\",\"componentId\",\"instruction\",\"stepId\",\"reason\"]}}" +
            "},\"required\":[\"assistantMessage\",\"toolCalls\"]}";

        public static string Build(AgentRequest request)
        {
            var context = request?.context;
            var builder = new StringBuilder();
            builder.AppendLine("Interpret the user command for a maintenance procedure.");
            builder.AppendLine("Allowed tools: HighlightComponent, ClearHighlight, ShowInstruction, NextStep, RepeatStep, CompleteStep.");
            builder.AppendLine("Use empty strings for unused tool fields.");
            builder.AppendLine("Only use componentId air_filter when the user asks about the air filter or filter housing.");
            builder.AppendLine("Prefer NextStep only when the user clearly asks to continue or confirms completion.");
            builder.AppendLine();
            builder.AppendLine($"User command: {request?.userCommand}");
            builder.AppendLine($"Procedure id: {context?.procedureId}");
            builder.AppendLine($"Procedure title: {context?.procedureTitle}");
            builder.AppendLine($"Current step index: {context?.currentStepIndex ?? -1}");
            builder.AppendLine($"Total steps: {context?.totalSteps ?? 0}");
            builder.AppendLine($"Current step id: {context?.currentStepId}");
            builder.AppendLine($"Current step title: {context?.currentStepTitle}");
            builder.AppendLine($"Current instruction: {context?.currentInstruction}");
            builder.AppendLine($"Current context JSON: {JsonUtility.ToJson(context)}");
            return builder.ToString();
        }
    }
}
