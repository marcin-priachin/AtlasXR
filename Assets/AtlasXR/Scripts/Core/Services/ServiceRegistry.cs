using System;
using System.Collections.Generic;

namespace AtlasXR.Core.Services
{
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> servicesByType = new Dictionary<Type, object>();

        public void Register<TService>(TService service) where TService : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            servicesByType[typeof(TService)] = service;
        }

        public bool TryResolve<TService>(out TService service) where TService : class
        {
            if (servicesByType.TryGetValue(typeof(TService), out var instance))
            {
                service = (TService)instance;
                return true;
            }

            service = null;
            return false;
        }

        public TService Resolve<TService>() where TService : class
        {
            if (TryResolve<TService>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service is not registered: {typeof(TService).FullName}");
        }
    }
}
