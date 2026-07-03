using System;
using System.Collections.Generic;

namespace AtlasXR.Procedures.Data
{
    [Serializable]
    public sealed class ProcedureStepDefinition
    {
        public string id;
        public string title;
        public string instruction;
        public string confirmationPrompt;
        public bool required = true;
        public List<string> targetComponentIds = new List<string>();
        public List<string> safetyNotes = new List<string>();
        public string expectedOutcome;

        [Obsolete("Use targetComponentIds. Procedure steps describe data; agent tools execute through validated tool calls.")]
        public List<string> toolIds = new List<string>();

#pragma warning disable 618
        public ProcedureStepDefinition Clone()
        {
            return new ProcedureStepDefinition
            {
                id = id,
                title = title,
                instruction = instruction,
                confirmationPrompt = confirmationPrompt,
                required = required,
                targetComponentIds = targetComponentIds == null
                    ? new List<string>()
                    : new List<string>(targetComponentIds),
                safetyNotes = safetyNotes == null
                    ? new List<string>()
                    : new List<string>(safetyNotes),
                expectedOutcome = expectedOutcome,
                toolIds = toolIds == null
                    ? new List<string>()
                    : new List<string>(toolIds)
            };
        }
#pragma warning restore 618
    }
}
