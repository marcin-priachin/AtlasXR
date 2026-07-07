using UnityEngine;

namespace AtlasXR.App.Bootstrap
{
    [CreateAssetMenu(
        fileName = "AtlasXRRuntimeSettingsDefinition",
        menuName = "AtlasXR/Runtime Settings Definition")]
    public sealed class AtlasXRRuntimeSettingsDefinition : ScriptableObject
    {
        [Header("Provider Selection")]
        [SerializeField] private ProviderSelectionMode agentProviderMode = ProviderSelectionMode.Auto;
        [SerializeField] private ProviderSelectionMode speechToTextProviderMode = ProviderSelectionMode.Auto;
        [SerializeField] private ProviderSelectionMode textToSpeechProviderMode = ProviderSelectionMode.Auto;

        [Header("XR")]
        [SerializeField] private XRInteractionMode xrInteractionMode = XRInteractionMode.Controllers;

        public ProviderSelectionMode AgentProviderMode => agentProviderMode;
        public ProviderSelectionMode SpeechToTextProviderMode => speechToTextProviderMode;
        public ProviderSelectionMode TextToSpeechProviderMode => textToSpeechProviderMode;
        public XRInteractionMode XRInteractionMode => xrInteractionMode;
    }
}
