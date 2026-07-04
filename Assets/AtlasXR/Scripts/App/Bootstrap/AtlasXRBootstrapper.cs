using System;
using AtlasXR.AI.Runtime;
using AtlasXR.AI.Tools;
using AtlasXR.Core.Events;
using AtlasXR.Core.Logging;
using AtlasXR.Core.Services;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Procedures.Validation;
using AtlasXR.Scenarios.Runtime;
using AtlasXR.XR.Highlighting;
using UnityEngine;

namespace AtlasXR.App.Bootstrap
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AtlasXRBootstrapper : MonoBehaviour
    {
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
        }

        private static void BuildServices()
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

            Services = services;
            Events = eventBroker;

            logger.Info("Bootstrap complete.");
        }

        private static IAgentProvider CreateAgentProvider(MockAgentProvider mockAgentProvider, IAtlasLogger logger)
        {
            var apiKey = GetEnvironmentVariable("OPENAI_API_KEY", out var apiKeySource);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.Warning("OPENAI_API_KEY is not set. Using mock agent provider.");
                return mockAgentProvider;
            }

            var model = GetEnvironmentVariable("OPENAI_MODEL", out _);
            logger.Info(string.IsNullOrWhiteSpace(model)
                ? $"Using OpenAI agent provider with default model. API key source: {apiKeySource}."
                : $"Using OpenAI agent provider with model '{model}'. API key source: {apiKeySource}.");

            return new OpenAIAgentProvider(apiKey, model, logger);
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
