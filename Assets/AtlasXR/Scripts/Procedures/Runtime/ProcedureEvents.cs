using AtlasXR.Core.Events;
using AtlasXR.Procedures.Data;

namespace AtlasXR.Procedures.Runtime
{
    public readonly struct ProcedureStartedEvent : IEvent
    {
        public ProcedureStartedEvent(ProcedureDefinition procedure)
        {
            Procedure = procedure;
        }

        public ProcedureDefinition Procedure { get; }
    }

    public readonly struct ProcedureStepChangedEvent : IEvent
    {
        public ProcedureStepChangedEvent(ProcedureDefinition procedure, ProcedureStepDefinition step, int stepIndex)
        {
            Procedure = procedure;
            Step = step;
            StepIndex = stepIndex;
        }

        public ProcedureDefinition Procedure { get; }

        public ProcedureStepDefinition Step { get; }

        public int StepIndex { get; }
    }

    public readonly struct ProcedureCompletedEvent : IEvent
    {
        public ProcedureCompletedEvent(ProcedureDefinition procedure)
        {
            Procedure = procedure;
        }

        public ProcedureDefinition Procedure { get; }
    }
}
