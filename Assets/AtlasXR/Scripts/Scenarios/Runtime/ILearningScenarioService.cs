using AtlasXR.Procedures.Data;
using AtlasXR.Scenarios.Data;
using UnityEngine;

namespace AtlasXR.Scenarios.Runtime
{
    public interface ILearningScenarioService
    {
        LearningScenarioDefinition CurrentScenario { get; }

        ProcedureDefinition CurrentProcedure { get; }

        GameObject CurrentInstance { get; }

        void LoadScenario(LearningScenarioDefinition scenario, Transform scenarioRoot);

        void UnloadCurrentScenario();
    }
}
