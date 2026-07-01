using System;
using System.Collections.Generic;

namespace AtlasXR.Procedures.Data
{
    [Serializable]
    public sealed class ProcedureDefinition
    {
        public string id;
        public string title;
        public string description;
        public string version;
        public int estimatedDurationMinutes;
        public List<ProcedureStepDefinition> steps = new List<ProcedureStepDefinition>();
    }
}
