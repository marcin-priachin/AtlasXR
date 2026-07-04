using System;
using System.Collections.Generic;
using AtlasXR.Core.Logging;
using AtlasXR.Procedures.Data;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Scenarios.Data;
using AtlasXR.XR.Highlighting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AtlasXR.Scenarios.Runtime
{
    public sealed class LearningScenarioService : ILearningScenarioService
    {
        private readonly ProcedureEngine procedureEngine;
        private readonly IAtlasLogger logger;

        public LearningScenarioService(ProcedureEngine procedureEngine, IAtlasLogger logger)
        {
            this.procedureEngine = procedureEngine ?? throw new ArgumentNullException(nameof(procedureEngine));
            this.logger = logger;
        }

        public LearningScenarioDefinition CurrentScenario { get; private set; }

        public ProcedureDefinition CurrentProcedure { get; private set; }

        public GameObject CurrentInstance { get; private set; }

        public void LoadScenario(LearningScenarioDefinition scenario, Transform scenarioRoot)
        {
            ValidateScenarioAsset(scenario);

            var procedure = procedureEngine.LoadFromJson(scenario.procedureJson.text);
            UnloadCurrentScenario();

            var instance = Object.Instantiate(scenario.equipmentPrefab, scenarioRoot, false);
            try
            {
                instance.name = $"{scenario.scenarioId}_Equipment";
                ApplySpawnSettings(instance.transform, scenario.spawnSettings);
                ValidateProcedureTargets(procedure, instance);
            }
            catch
            {
                Object.Destroy(instance);
                throw;
            }

            CurrentScenario = scenario;
            CurrentProcedure = procedure.Clone();
            CurrentInstance = instance;

            procedureEngine.Start(procedure);
            logger?.Info($"Loaded learning scenario '{scenario.scenarioId}'.");
        }

        public void UnloadCurrentScenario()
        {
            procedureEngine.Reset();

            if (CurrentInstance != null)
            {
                Object.Destroy(CurrentInstance);
            }

            CurrentInstance = null;
            CurrentProcedure = null;
            CurrentScenario = null;
        }

        private static void ValidateScenarioAsset(LearningScenarioDefinition scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            if (string.IsNullOrWhiteSpace(scenario.scenarioId))
            {
                throw new InvalidOperationException("Learning scenario id cannot be empty.");
            }

            if (scenario.equipmentPrefab == null)
            {
                throw new InvalidOperationException($"Learning scenario '{scenario.scenarioId}' has no equipment prefab.");
            }

            if (scenario.procedureJson == null || string.IsNullOrWhiteSpace(scenario.procedureJson.text))
            {
                throw new InvalidOperationException($"Learning scenario '{scenario.scenarioId}' has no procedure JSON.");
            }
        }

        private static void ApplySpawnSettings(Transform transform, ScenarioSpawnSettings spawnSettings)
        {
            transform.localPosition = spawnSettings.localPosition;
            transform.localRotation = Quaternion.Euler(spawnSettings.localEulerAngles);
            transform.localScale = spawnSettings.localScale == Vector3.zero
                ? Vector3.one
                : spawnSettings.localScale;
        }

        private static void ValidateProcedureTargets(ProcedureDefinition procedure, GameObject instance)
        {
            var availableComponentIds = new HashSet<string>();
            foreach (var component in instance.GetComponentsInChildren<EquipmentComponent>(true))
            {
                if (!string.IsNullOrWhiteSpace(component.ComponentId))
                {
                    availableComponentIds.Add(component.ComponentId.Trim());
                }
            }

            foreach (var step in procedure.steps)
            {
                if (step?.targetComponentIds == null)
                {
                    continue;
                }

                foreach (var targetComponentId in step.targetComponentIds)
                {
                    if (string.IsNullOrWhiteSpace(targetComponentId))
                    {
                        continue;
                    }

                    var normalizedTargetComponentId = targetComponentId.Trim();
                    if (!availableComponentIds.Contains(normalizedTargetComponentId))
                    {
                        throw new InvalidOperationException(
                            $"Procedure '{procedure.id}' step '{step.id}' targets missing component '{normalizedTargetComponentId}'.");
                    }
                }
            }
        }
    }
}
