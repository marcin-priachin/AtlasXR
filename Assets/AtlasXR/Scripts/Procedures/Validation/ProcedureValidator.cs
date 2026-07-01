using System.Collections.Generic;
using AtlasXR.Procedures.Data;

namespace AtlasXR.Procedures.Validation
{
    public sealed class ProcedureValidator
    {
        public ProcedureValidationResult Validate(ProcedureDefinition procedure)
        {
            var result = new ProcedureValidationResult();
            if (procedure == null)
            {
                result.AddError("Procedure is missing.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(procedure.id))
            {
                result.AddError("Procedure id is required.");
            }

            if (string.IsNullOrWhiteSpace(procedure.title))
            {
                result.AddError("Procedure title is required.");
            }

            if (procedure.steps == null || procedure.steps.Count == 0)
            {
                result.AddError("Procedure must contain at least one step.");
                return result;
            }

            ValidateSteps(procedure, result);
            return result;
        }

        private static void ValidateSteps(ProcedureDefinition procedure, ProcedureValidationResult result)
        {
            var stepIds = new HashSet<string>();
            for (var i = 0; i < procedure.steps.Count; i++)
            {
                var step = procedure.steps[i];
                if (step == null)
                {
                    result.AddError($"Step {i} is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(step.id))
                {
                    result.AddError($"Step {i} id is required.");
                }
                else if (!stepIds.Add(step.id))
                {
                    result.AddError($"Step id must be unique: {step.id}");
                }

                if (string.IsNullOrWhiteSpace(step.title))
                {
                    result.AddError($"Step '{step.id}' title is required.");
                }

                if (string.IsNullOrWhiteSpace(step.instruction))
                {
                    result.AddError($"Step '{step.id}' instruction is required.");
                }
            }
        }
    }
}
