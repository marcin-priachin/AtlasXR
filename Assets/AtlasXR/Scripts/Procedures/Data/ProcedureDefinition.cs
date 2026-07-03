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

        public ProcedureDefinition Clone()
        {
            var copy = new ProcedureDefinition
            {
                id = id,
                title = title,
                description = description,
                version = version,
                estimatedDurationMinutes = estimatedDurationMinutes,
                steps = new List<ProcedureStepDefinition>()
            };

            if (steps == null)
            {
                return copy;
            }

            foreach (var step in steps)
            {
                copy.steps.Add(step?.Clone());
            }

            return copy;
        }
    }
}
