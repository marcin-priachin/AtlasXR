using AtlasXR.Core.Events;
using AtlasXR.Core.Logging;
using AtlasXR.Core.Services;
using AtlasXR.Procedures.Runtime;
using AtlasXR.Procedures.Validation;
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
            var procedureHighlightToolBridge = new ProcedureHighlightToolBridge(eventBroker, highlightService, logger);

            services.Register<IServiceRegistry>(services);
            services.Register<IAtlasLogger>(logger);
            services.Register<IEventBroker>(eventBroker);
            services.Register(procedureValidator);
            services.Register(procedureEngine);
            services.Register<IHighlightService>(highlightService);
            services.Register(highlightService);
            services.Register(procedureHighlightToolBridge);

            Services = services;
            Events = eventBroker;

            logger.Info("Bootstrap complete.");
        }
    }
}
