using System;
using UnityEngine;

namespace AtlasXR.Scenarios.Data
{
    [CreateAssetMenu(menuName = "AtlasXR/Learning Scenario Definition")]
    public sealed class LearningScenarioDefinition : ScriptableObject
    {
        public string scenarioId;
        public string displayName;
        public GameObject equipmentPrefab;
        public TextAsset procedureJson;
        public ScenarioSpawnSettings spawnSettings = ScenarioSpawnSettings.Default;
    }

    [Serializable]
    public struct ScenarioSpawnSettings
    {
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale;

        public static ScenarioSpawnSettings Default => new ScenarioSpawnSettings
        {
            localPosition = Vector3.zero,
            localEulerAngles = Vector3.zero,
            localScale = Vector3.one
        };
    }
}
