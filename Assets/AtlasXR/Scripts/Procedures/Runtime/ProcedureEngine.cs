using System;
using AtlasXR.Core.Events;
using AtlasXR.Core.Logging;
using AtlasXR.Procedures.Data;
using AtlasXR.Procedures.Validation;
using UnityEngine;

namespace AtlasXR.Procedures.Runtime
{
    public sealed class ProcedureEngine
    {
        private readonly ProcedureValidator validator;
        private readonly IEventBroker eventBroker;
        private readonly IAtlasLogger logger;
        private ProcedureDefinition currentProcedure;
        private int currentStepIndex = -1;
        private ProcedureEngineState state = ProcedureEngineState.Idle;

        public ProcedureEngine(ProcedureValidator validator, IEventBroker eventBroker = null, IAtlasLogger logger = null)
        {
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
            this.eventBroker = eventBroker;
            this.logger = logger;
        }

        public ProcedureProgress Progress => new ProcedureProgress(currentProcedure?.Clone(), currentStepIndex, state);

        public ProcedureDefinition CurrentProcedure => currentProcedure?.Clone();

        public ProcedureStepDefinition CurrentStep => Progress.CurrentStep;

        public ProcedureDefinition LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Procedure JSON cannot be empty.", nameof(json));
            }

            var procedure = JsonUtility.FromJson<ProcedureDefinition>(json);
            ValidateOrThrow(procedure);
            return procedure.Clone();
        }

        public void Start(ProcedureDefinition procedure)
        {
            ValidateOrThrow(procedure);

            currentProcedure = procedure.Clone();
            currentStepIndex = 0;
            state = ProcedureEngineState.Running;

            logger?.Info($"Started procedure '{procedure.id}'.");
            eventBroker?.Publish(new ProcedureStartedEvent(currentProcedure.Clone()));
            PublishCurrentStep();
        }

        public void CompleteCurrentStep()
        {
            EnsureRunning();

            if (currentStepIndex >= currentProcedure.steps.Count - 1)
            {
                Complete();
                return;
            }

            currentStepIndex++;
            PublishCurrentStep();
        }

        public void MovePrevious()
        {
            EnsureRunning();

            if (currentStepIndex == 0)
            {
                return;
            }

            currentStepIndex--;
            PublishCurrentStep();
        }

        public void Reset()
        {
            currentProcedure = null;
            currentStepIndex = -1;
            state = ProcedureEngineState.Idle;
        }

        private void Complete()
        {
            state = ProcedureEngineState.Completed;
            logger?.Info($"Completed procedure '{currentProcedure.id}'.");
            eventBroker?.Publish(new ProcedureCompletedEvent(currentProcedure.Clone()));
        }

        private void PublishCurrentStep()
        {
            eventBroker?.Publish(new ProcedureStepChangedEvent(
                currentProcedure.Clone(),
                currentProcedure.steps[currentStepIndex].Clone(),
                currentStepIndex));
        }

        private void EnsureRunning()
        {
            if (state != ProcedureEngineState.Running)
            {
                throw new InvalidOperationException("Procedure engine is not running.");
            }
        }

        private void ValidateOrThrow(ProcedureDefinition procedure)
        {
            var result = validator.Validate(procedure);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(result.ToMessage());
            }
        }
    }
}
