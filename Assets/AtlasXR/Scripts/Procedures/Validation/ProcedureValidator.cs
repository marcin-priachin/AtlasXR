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

                if (string.IsNullOrWhiteSpace(step.confirmationPrompt))
                {
                    result.AddError($"Step '{step.id}' confirmation prompt is required.");
                }

                ValidateListValues(step.id, "target component id", step.targetComponentIds, result);
                ValidateListValues(step.id, "safety note", step.safetyNotes, result);

                ValidateDeprecatedToolIds(step, result);
            }
        }

#pragma warning disable 618
        private static void ValidateDeprecatedToolIds(
            ProcedureStepDefinition step,
            ProcedureValidationResult result)
        {
            if (step.toolIds != null && step.toolIds.Count > 0)
            {
                result.AddError($"Step '{step.id}' uses deprecated toolIds. Use targetComponentIds for procedure targets and agent tool calls for actions.");
            }
        }
#pragma warning restore 618

        private static void ValidateListValues(
            string stepId,
            string itemName,
            IReadOnlyList<string> values,
            ProcedureValidationResult result)
        {
            if (values == null)
            {
                return;
            }

            var uniqueValues = new HashSet<string>();
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (string.IsNullOrWhiteSpace(value))
                {
                    result.AddError($"Step '{stepId}' {itemName} {i} is empty.");
                    continue;
                }

                if (!uniqueValues.Add(value.Trim()))
                {
                    result.AddError($"Step '{stepId}' has duplicate {itemName}: {value}");
                }
            }
        }
    }
}
