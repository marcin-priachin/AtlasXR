using System;
using AtlasXR.Procedures.Data;

namespace AtlasXR.AI.Runtime
{
    [Serializable]
    public sealed class AgentContext
    {
        public string procedureId;
        public string procedureTitle;
        public int currentStepIndex;
        public int totalSteps;
        public string currentStepId;
        public string currentStepTitle;
        public string currentInstruction;

        public static AgentContext FromProcedure(ProcedureDefinition procedure, int currentStepIndex)
        {
            var context = new AgentContext
            {
                procedureId = procedure?.id,
                procedureTitle = procedure?.title,
                currentStepIndex = currentStepIndex,
                totalSteps = procedure?.steps?.Count ?? 0
            };

            if (procedure?.steps != null &&
                currentStepIndex >= 0 &&
                currentStepIndex < procedure.steps.Count)
            {
                var step = procedure.steps[currentStepIndex];
                context.currentStepId = step.id;
                context.currentStepTitle = step.title;
                context.currentInstruction = step.instruction;
            }

            return context;
        }
    }
}
