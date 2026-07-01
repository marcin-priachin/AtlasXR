using AtlasXR.Procedures.Data;

namespace AtlasXR.Procedures.Runtime
{
    public readonly struct ProcedureProgress
    {
        public ProcedureProgress(ProcedureDefinition procedure, int currentStepIndex, ProcedureEngineState state)
        {
            Procedure = procedure;
            CurrentStepIndex = currentStepIndex;
            State = state;
        }

        public ProcedureDefinition Procedure { get; }

        public int CurrentStepIndex { get; }

        public ProcedureEngineState State { get; }

        public int TotalSteps => Procedure?.steps?.Count ?? 0;

        public bool HasCurrentStep => CurrentStepIndex >= 0 && CurrentStepIndex < TotalSteps;

        public ProcedureStepDefinition CurrentStep => HasCurrentStep ? Procedure.steps[CurrentStepIndex] : null;
    }
}
