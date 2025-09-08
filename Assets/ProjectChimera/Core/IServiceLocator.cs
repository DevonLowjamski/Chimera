using System;
using System.Collections.Generic;

namespace ProjectChimera.Core
{
    /// <summary>
    /// DEPRECATED: IServiceLocator is obsolete and should not be used.
    /// Use IServiceContainer from ServiceContainerFactory.Instance instead for dependency injection.
    /// This will be removed in a future version.
    /// </summary>
    [System.Obsolete("IServiceLocator is deprecated. Use IServiceContainer from ServiceContainerFactory.Instance instead for dependency injection. This will cause compile errors in future releases.", false)]
    public interface IServiceLocator
    {
        // Service Registration Methods
        void RegisterSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface, new();
        void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class;
        void RegisterSingleton(Type serviceType, object instance);
        void RegisterTransient<TInterface, TImplementation>() where TImplementation : class, TInterface, new();
        void RegisterScoped<TInterface, TImplementation>() where TImplementation : class, TInterface, new();
        void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class;

        // Service Resolution Methods
        T GetService<T>() where T : class;
        object GetService(System.Type serviceType);
        bool TryGetService<T>(out T service) where T : class;
        bool TryGetService(System.Type serviceType, out object service);
        bool ContainsService<T>() where T : class;
        bool ContainsService(System.Type serviceType);
        
        T Resolve<T>() where T : class;
        T TryResolve<T>() where T : class;
        IEnumerable<T> ResolveAll<T>() where T : class;
        
        // Additional methods to match IServiceProvider compatibility
        T GetRequiredService<T>() where T : class;
        object GetRequiredService(System.Type serviceType);
        IEnumerable<object> GetServices(System.Type serviceType);
        IEnumerable<T> GetServices<T>() where T : class;
    }
}
