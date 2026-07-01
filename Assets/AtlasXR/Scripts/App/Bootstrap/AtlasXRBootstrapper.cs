using AtlasXR.Core.Events;
using AtlasXR.Core.Logging;
using AtlasXR.Core.Services;
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

            services.Register<IServiceRegistry>(services);
            services.Register<IAtlasLogger>(logger);
            services.Register<IEventBroker>(eventBroker);

            Services = services;
            Events = eventBroker;

            logger.Info("Bootstrap complete.");
        }
    }
}
