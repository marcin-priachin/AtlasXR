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
        public List<string> toolIds = new List<string>();
        public List<string> safetyNotes = new List<string>();
        public string expectedOutcome;
    }
}
