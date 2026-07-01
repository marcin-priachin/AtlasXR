using System.Collections.Generic;

namespace AtlasXR.Procedures.Validation
{
    public sealed class ProcedureValidationResult
    {
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;

        public bool IsValid => errors.Count == 0;

        public void AddError(string error)
        {
            errors.Add(error);
        }

        public string ToMessage()
        {
            return IsValid ? "Procedure is valid." : string.Join("\n", errors);
        }
    }
}
