using System;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Core
{
    public class ServiceRegistration
    {
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
        public ServiceLifetime Lifetime { get; }
        public object Instance { get; set; }
        public Func<IServiceLocator, object> Factory { get; }

        public ServiceRegistration(Type serviceType, Type implementationType, ServiceLifetime lifetime, object instance, Func<IServiceLocator, object> factory)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            Instance = instance;
            Factory = factory;
        }
    }
}
