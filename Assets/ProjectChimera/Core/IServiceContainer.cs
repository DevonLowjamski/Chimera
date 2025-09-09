using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.DependencyInjection;
using ContainerVerificationResult = ProjectChimera.Core.DependencyInjection.ContainerVerificationResult;
using AdvancedServiceDescriptor = ProjectChimera.Core.DependencyInjection.AdvancedServiceDescriptor;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Factory interface for creating service containers
    /// </summary>
    public interface IServiceContainerFactory
    {
        IServiceContainer CreateContainer();
    }

    /// <summary>
    /// PC014-1a: Core interface for dependency injection service container
    /// Provides registration, resolution, and lifecycle management for all Project Chimera services
    /// Supports singleton, transient, and scoped lifetimes with constructor injection
    /// </summary>
    public interface IServiceContainer : IDisposable
    {
        /// <summary>
        /// Registers a service with singleton lifetime (single instance for entire application)
        /// </summary>
        void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new();

        /// <summary>
        /// Registers a service with singleton lifetime using a factory method
        /// </summary>
        void RegisterSingleton<TInterface>(Func<IServiceContainer, TInterface> factory);

        /// <summary>
        /// Registers a service with singleton lifetime using an existing instance
        /// </summary>
        void RegisterSingleton<TInterface>(TInterface instance);

        /// <summary>
        /// Registers a service instance (alias for RegisterSingleton for compatibility)
        /// </summary>
        void RegisterInstance<TInterface>(TInterface instance) where TInterface : class;

        /// <summary>
        /// Registers a service with transient lifetime (new instance per resolution)
        /// </summary>
        void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new();

        /// <summary>
        /// Registers a service with transient lifetime using a factory method
        /// </summary>
        void RegisterTransient<TInterface>(Func<IServiceContainer, TInterface> factory);

        /// <summary>
        /// Registers a service with scoped lifetime (single instance per scope/request)
        /// </summary>
        void RegisterScoped<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new();

        /// <summary>
        /// Registers a service with scoped lifetime using a factory method
        /// </summary>
        void RegisterScoped<TInterface>(Func<IServiceContainer, TInterface> factory);

        /// <summary>
        /// Resolves a service by type with constructor injection support
        /// </summary>
        T Resolve<T>();

        /// <summary>
        /// Resolves a service by type with constructor injection support
        /// </summary>
        object Resolve(Type serviceType);

        /// <summary>
        /// Attempts to resolve a service, returns null if not registered
        /// </summary>
        T TryResolve<T>() where T : class;

        /// <summary>
        /// Attempts to resolve a service, returns null if not registered
        /// </summary>
        object TryResolve(Type serviceType);

        /// <summary>
        /// Checks if a service type is registered
        /// </summary>
        bool IsRegistered<T>();

        /// <summary>
        /// Checks if a service type is registered
        /// </summary>
        bool IsRegistered(Type serviceType);

        /// <summary>
        /// Gets all registered service types that implement the specified interface
        /// </summary>
        IEnumerable<Type> GetRegisteredTypes<T>();

        /// <summary>
        /// Gets all registered service types that implement the specified interface
        /// </summary>
        IEnumerable<Type> GetRegisteredTypes(Type interfaceType);

        /// <summary>
        /// Resolves all services that implement the specified interface
        /// </summary>
        IEnumerable<T> ResolveAll<T>();

        /// <summary>
        /// Creates a new service scope for scoped lifetime management
        /// </summary>
        IServiceScope CreateScope();

        /// <summary>
        /// Validates all registered services can be resolved (dependency validation)
        /// </summary>
        void ValidateServices();

        /// <summary>
        /// Gets detailed information about all registered services for debugging
        /// </summary>
        IEnumerable<ServiceRegistrationInfo> GetRegistrationInfo();

        /// <summary>
        /// Clears all service registrations (primarily for testing)
        /// </summary>
        void Clear();

        /// <summary>
        /// Creates a child container for scoped operations
        /// </summary>
        IServiceContainer CreateChildContainer();

        /// <summary>
        /// Registers a factory function for creating service instances
        /// </summary>
        void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class;

        /// <summary>
        /// Gets all registered service registrations for debugging
        /// </summary>
        IDictionary<Type, ServiceRegistration> GetRegistrations();

        /// <summary>
        /// Indicates if the container has been disposed
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Event raised when a service is registered
        /// </summary>
        event Action<ServiceRegistration> ServiceRegistered;

        /// <summary>
        /// Event raised when a service is resolved
        /// </summary>
        event Action<Type, object> ServiceResolved;

        /// <summary>
        /// Event raised when service resolution fails
        /// </summary>
        event Action<Type, Exception> ResolutionFailed;

        /// <summary>
        /// Gets a service by type, returns null if not found
        /// </summary>
        T GetService<T>() where T : class;

        /// <summary>
        /// Gets a service by type, returns null if not found
        /// </summary>
        object GetService(Type serviceType);

        /// <summary>
        /// Tries to get a service, returns true if found
        /// </summary>
        bool TryGetService<T>(out T service) where T : class;

        /// <summary>
        /// Tries to get a service, returns true if found
        /// </summary>
        bool TryGetService(Type serviceType, out object service);

        /// <summary>
        /// Checks if the container contains a service
        /// </summary>
        bool ContainsService<T>() where T : class;

        /// <summary>
        /// Checks if the container contains a service
        /// </summary>
        bool ContainsService(Type serviceType);

        /// <summary>
        /// Registers a singleton service with type and instance
        /// </summary>
        void RegisterSingleton(Type serviceType, object instance);

        /// <summary>
        /// Registers a collection of services
        /// </summary>
        void RegisterCollection<TInterface>(params Type[] implementations) where TInterface : class;

        /// <summary>
        /// Registers a named service
        /// </summary>
        void RegisterNamed<TInterface, TImplementation>(string name)
            where TInterface : class
            where TImplementation : class, TInterface, new();

        /// <summary>
        /// Registers a conditional service
        /// </summary>
        void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition)
            where TInterface : class
            where TImplementation : class, TInterface, new();

        /// <summary>
        /// Registers a decorator service
        /// </summary>
        void RegisterDecorator<TInterface, TDecorator>()
            where TInterface : class
            where TDecorator : class, TInterface, new();

        /// <summary>
        /// Registers a service with callback
        /// </summary>
        void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer)
            where TInterface : class
            where TImplementation : class, TInterface, new();

        /// <summary>
        /// Registers an open generic service
        /// </summary>
        void RegisterOpenGeneric(Type serviceType, Type implementationType);

        /// <summary>
        /// Resolves a named service
        /// </summary>
        T ResolveNamed<T>(string name) where T : class;

        /// <summary>
        /// Resolves services with predicate
        /// </summary>
        IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class;

        /// <summary>
        /// Resolves or creates with factory
        /// </summary>
        T ResolveOrCreate<T>(Func<T> factory) where T : class;

        /// <summary>
        /// Resolves last service of type
        /// </summary>
        T ResolveLast<T>() where T : class;

        /// <summary>
        /// Resolves with specific lifetime
        /// </summary>
        T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class;

        /// <summary>
        /// Verifies container configuration
        /// </summary>
        ContainerVerificationResult Verify();

        /// <summary>
        /// Gets service descriptors
        /// </summary>
        IEnumerable<AdvancedServiceDescriptor> GetServiceDescriptors();

        /// <summary>
        /// Replaces a service registration
        /// </summary>
        void Replace<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new();

        /// <summary>
        /// Unregisters a service
        /// </summary>
        bool Unregister<T>() where T : class;
    }

    /// <summary>
    /// Service registration information for debugging and diagnostics
    /// </summary>
    public class ServiceRegistrationInfo
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public bool HasFactory { get; set; }
        public bool HasInstance { get; set; }
        public DateTime RegistrationTime { get; set; }
    }





    /// <summary>
    /// Exception thrown when service resolution fails
    /// </summary>
    public class ServiceResolutionException : Exception
    {
        public Type ServiceType { get; }

        public ServiceResolutionException(Type serviceType, string message) : base(message)
        {
            ServiceType = serviceType;
        }

        public ServiceResolutionException(Type serviceType, string message, Exception innerException) : base(message, innerException)
        {
            ServiceType = serviceType;
        }
    }

    /// <summary>
    /// Exception thrown when circular dependencies are detected
    /// </summary>
    public class CircularDependencyException : ServiceResolutionException
    {
        public IEnumerable<Type> DependencyChain { get; }

        public CircularDependencyException(Type serviceType, IEnumerable<Type> dependencyChain)
            : base(serviceType, $"Circular dependency detected for service {serviceType.Name}. Dependency chain: {string.Join(" -> ", dependencyChain.Select(t => t.Name))}")
        {
            DependencyChain = dependencyChain;
        }
    }
}
