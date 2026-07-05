using System;
using AtlasXR.AI.Runtime;
using AtlasXR.AI.Tools;
using AtlasXR.Core.Events;
using AtlasXR.Core.Logging;
using AtlasXR.Core.Services;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Procedures.Validation;
using AtlasXR.Scenarios.Runtime;
using AtlasXR.Voice.SpeechToText;
using AtlasXR.Voice.TextToSpeech;
using AtlasXR.XR.Highlighting;
using UnityEngine;

namespace AtlasXR.App.Bootstrap
{
    public enum ProviderSelectionMode
    {
        Auto,
        Mock,
        OpenAI
    }

    [DefaultExecutionOrder(-1000)]
    public sealed class AtlasXRBootstrapper : MonoBehaviour
    {
        [Header("Provider Selection")]
        [SerializeField] private ProviderSelectionMode agentProviderMode = ProviderSelectionMode.Auto;
        [SerializeField] private ProviderSelectionMode speechToTextProviderMode = ProviderSelectionMode.Auto;
        [SerializeField] private ProviderSelectionMode textToSpeechProviderMode = ProviderSelectionMode.Auto;

        [Header("XR")]
        [SerializeField] private XRInteractionMode xrInteractionMode = XRInteractionMode.Controllers;

        public static IServiceRegistry Services { get; private set; }

        public static IEventBroker Events { get; private set; }

        private void Awake()
        {
            if (Services != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            BuildServices();
            EnsureQuestOpenXRRuntime();
        }

        private void EnsureQuestOpenXRRuntime()
        {
            var questRuntime = GetComponent<QuestOpenXRRuntimeBootstrapper>();
            if (questRuntime == null)
            {
                questRuntime = gameObject.AddComponent<QuestOpenXRRuntimeBootstrapper>();
            }

            questRuntime.Configure(xrInteractionMode);
        }

        private void BuildServices()
        {
            var services = new ServiceRegistry();
            var logger = new UnityAtlasLogger();
            var eventBroker = new EventBroker();
            var procedureValidator = new ProcedureValidator();
            var procedureEngine = new ProcedureEngine(procedureValidator, eventBroker, logger);
            var highlightService = new HighlightService(logger);
            var learningScenarioService = new LearningScenarioService(procedureEngine, logger);
            var procedureHighlightToolBridge = new ProcedureHighlightToolBridge(eventBroker, highlightService, logger);
            var mockAgentProvider = new MockAgentProvider();
            var agentProvider = CreateAgentProvider(mockAgentProvider, logger);
            var agentService = new AgentService(agentProvider, mockAgentProvider, logger);
            var agentToolExecutor = new AgentToolExecutor(procedureEngine, highlightService, logger);
            var mockSpeechToTextProvider = new MockSpeechToTextProvider();
            var speechToTextProvider = CreateSpeechToTextProvider(mockSpeechToTextProvider, logger);
            var speechToTextService = new SpeechToTextService(speechToTextProvider, mockSpeechToTextProvider, logger);
            var mockTextToSpeechProvider = new MockTextToSpeechProvider();
            var textToSpeechProvider = CreateTextToSpeechProvider(mockTextToSpeechProvider, logger);
            var textToSpeechService =
                new FallbackTextToSpeechService(textToSpeechProvider, mockTextToSpeechProvider, logger);

            services.Register<IServiceRegistry>(services);
            services.Register<IAtlasLogger>(logger);
            services.Register<IEventBroker>(eventBroker);
            services.Register(procedureValidator);
            services.Register(procedureEngine);
            services.Register<ILearningScenarioService>(learningScenarioService);
            services.Register(learningScenarioService);
            services.Register<IHighlightService>(highlightService);
            services.Register(highlightService);
            services.Register(procedureHighlightToolBridge);
            services.Register<MockAgentProvider>(mockAgentProvider);
            services.Register<IAgentProvider>(agentProvider);
            services.Register<IAgentService>(agentService);
            services.Register<IAgentToolExecutor>(agentToolExecutor);
            services.Register<MockSpeechToTextProvider>(mockSpeechToTextProvider);
            services.Register<ISpeechToTextProvider>(speechToTextProvider);
            services.Register<ISpeechToTextService>(speechToTextService);
            services.Register<MockTextToSpeechProvider>(mockTextToSpeechProvider);
            services.Register<ITextToSpeechProvider>(textToSpeechProvider);
            services.Register<ITextToSpeechService>(textToSpeechService);

            Services = services;
            Events = eventBroker;

            logger.Info("Bootstrap complete.");
        }

        private IAgentProvider CreateAgentProvider(MockAgentProvider mockAgentProvider, IAtlasLogger logger)
        {
            if (agentProviderMode == ProviderSelectionMode.Mock)
            {
                logger.Info("Using mock agent provider. Provider mode: Mock.");
                return mockAgentProvider;
            }

            var apiKey = GetEnvironmentVariable("OPENAI_API_KEY", out var apiKeySource);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                if (agentProviderMode == ProviderSelectionMode.OpenAI)
                {
                    logger.Warning("Agent provider mode is OpenAI, but OPENAI_API_KEY is not set. Using mock agent provider.");
                    return mockAgentProvider;
                }

                logger.Warning("OPENAI_API_KEY is not set. Using mock agent provider.");
                return mockAgentProvider;
            }

            var model = GetEnvironmentVariable("OPENAI_MODEL", out _);
            logger.Info(string.IsNullOrWhiteSpace(model)
                ? $"Using OpenAI agent provider with default model. API key source: {apiKeySource}."
                : $"Using OpenAI agent provider with model '{model}'. API key source: {apiKeySource}.");

            return new OpenAIAgentProvider(apiKey, model, logger);
        }

        private ISpeechToTextProvider CreateSpeechToTextProvider(
            MockSpeechToTextProvider mockSpeechToTextProvider,
            IAtlasLogger logger)
        {
            if (speechToTextProviderMode == ProviderSelectionMode.Mock)
            {
                logger.Info("Using mock speech-to-text provider. Provider mode: Mock.");
                return mockSpeechToTextProvider;
            }

            var apiKey = GetEnvironmentVariable("OPENAI_API_KEY", out var apiKeySource);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                if (speechToTextProviderMode == ProviderSelectionMode.OpenAI)
                {
                    logger.Warning("Speech-to-text provider mode is OpenAI, but OPENAI_API_KEY is not set. Using mock speech-to-text provider.");
                    return mockSpeechToTextProvider;
                }

                logger.Warning("OPENAI_API_KEY is not set. Using mock speech-to-text provider.");
                return mockSpeechToTextProvider;
            }

            var model = GetEnvironmentVariable("OPENAI_STT_MODEL", out _);
            logger.Info(string.IsNullOrWhiteSpace(model)
                ? $"Using OpenAI speech-to-text provider with default model. API key source: {apiKeySource}."
                : $"Using OpenAI speech-to-text provider with model '{model}'. API key source: {apiKeySource}.");

            return new OpenAISpeechToTextProvider(apiKey, model, logger);
        }

        private ITextToSpeechProvider CreateTextToSpeechProvider(
            MockTextToSpeechProvider mockTextToSpeechProvider,
            IAtlasLogger logger)
        {
            if (textToSpeechProviderMode == ProviderSelectionMode.Mock)
            {
                logger.Info("Using mock text-to-speech provider. Provider mode: Mock.");
                return mockTextToSpeechProvider;
            }

            var apiKey = GetEnvironmentVariable("OPENAI_API_KEY", out var apiKeySource);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                if (textToSpeechProviderMode == ProviderSelectionMode.OpenAI)
                {
                    logger.Warning("Text-to-speech provider mode is OpenAI, but OPENAI_API_KEY is not set. Using mock text-to-speech provider.");
                    return mockTextToSpeechProvider;
                }

                logger.Warning("OPENAI_API_KEY is not set. Using mock text-to-speech provider.");
                return mockTextToSpeechProvider;
            }

            var model = GetEnvironmentVariable("OPENAI_TTS_MODEL", out _);
            var voice = GetEnvironmentVariable("OPENAI_TTS_VOICE", out _);
            logger.Info(string.IsNullOrWhiteSpace(model)
                ? $"Using OpenAI text-to-speech provider with default model. API key source: {apiKeySource}."
                : $"Using OpenAI text-to-speech provider with model '{model}'. API key source: {apiKeySource}.");

            return new OpenAITextToSpeechProvider(apiKey, model, voice, logger);
        }

        private static string GetEnvironmentVariable(string variableName, out string source)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                source = "Process";
                return value;
            }

            value = TryGetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(value))
            {
                source = "User";
                return value;
            }

            value = TryGetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrWhiteSpace(value))
            {
                source = "Machine";
                return value;
            }

            source = "Not found";
            return null;
        }

        private static string TryGetEnvironmentVariable(string variableName, EnvironmentVariableTarget target)
        {
            try
            {
                return Environment.GetEnvironmentVariable(variableName, target);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
