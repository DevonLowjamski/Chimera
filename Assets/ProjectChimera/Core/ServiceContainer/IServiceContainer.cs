using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Core.ServiceContainer
{
    /// <summary>
    /// PC014-1a: Core interface for dependency injection service container
    /// Provides registration, resolution, and lifecycle management for all Project Chimera services
    /// Supports singleton, transient, and scoped lifetimes with constructor injection
    /// </summary>
    public interface IServiceContainer
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
    /// Service lifetime enumeration
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// Single instance for the entire application lifetime
        /// </summary>
        Singleton,

        /// <summary>
        /// New instance created every time the service is requested
        /// </summary>
        Transient,

        /// <summary>
        /// Single instance per scope (useful for request-scoped services)
        /// </summary>
        Scoped
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