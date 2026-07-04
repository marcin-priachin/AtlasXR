using AtlasXR.Core.Logging;
using AtlasXR.Scenarios.Data;
using AtlasXR.Scenarios.Runtime;
using UnityEngine;

namespace AtlasXR.App.Bootstrap
{
    public sealed class LearningScenarioLoader : MonoBehaviour
    {
        [SerializeField] private LearningScenarioDefinition defaultScenario;
        [SerializeField] private Transform scenarioRoot;
        [SerializeField] private bool loadOnStart = true;

        private ILearningScenarioService scenarioService;
        private IAtlasLogger logger;

        private void Start()
        {
            if (AtlasXRBootstrapper.Services == null)
            {
                Debug.LogWarning("AtlasXR services are not ready. Learning scenario was not loaded.");
                return;
            }

            scenarioService = AtlasXRBootstrapper.Services.Resolve<ILearningScenarioService>();
            AtlasXRBootstrapper.Services.TryResolve(out logger);

            if (loadOnStart)
            {
                LoadDefaultScenario();
            }
        }

        public void LoadDefaultScenario()
        {
            if (defaultScenario == null)
            {
                logger?.Warning("No default learning scenario assigned.");
                return;
            }

            scenarioService.LoadScenario(defaultScenario, scenarioRoot);
        }
    }
}
