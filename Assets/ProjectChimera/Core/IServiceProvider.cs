using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Standard service provider interface following Microsoft.Extensions.DependencyInjection patterns
    /// Provides compatibility with standard .NET dependency injection frameworks
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Get service of type serviceType
        /// </summary>
        object GetService(Type serviceType);

        /// <summary>
        /// Get service of type T
        /// </summary>
        T GetService<T>() where T : class;

        /// <summary>
        /// Get required service of type serviceType (throws if not found)
        /// </summary>
        object GetRequiredService(Type serviceType);

        /// <summary>
        /// Get required service of type T (throws if not found)
        /// </summary>
        T GetRequiredService<T>() where T : class;

        /// <summary>
        /// Get all services of type serviceType
        /// </summary>
        IEnumerable<object> GetServices(Type serviceType);

        /// <summary>
        /// Get all services of type T
        /// </summary>
        IEnumerable<T> GetServices<T>() where T : class;
    }

    /// <summary>
    /// Service collection interface for registering services
    /// Compatible with Microsoft.Extensions.DependencyInjection patterns
    /// </summary>
    public interface IServiceCollection : IList<ServiceDescriptor>
    {
        /// <summary>
        /// Add multiple service descriptors
        /// </summary>
        IServiceCollection Add(params ServiceDescriptor[] items);

        /// <summary>
        /// Add singleton service
        /// </summary>
        IServiceCollection AddSingleton<TService>() where TService : class;

        /// <summary>
        /// Add singleton service with implementation
        /// </summary>
        IServiceCollection AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// Add singleton instance
        /// </summary>
        IServiceCollection AddSingleton<TService>(TService instance) where TService : class;

        /// <summary>
        /// Add singleton factory
        /// </summary>
        IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory) where TService : class;

        /// <summary>
        /// Add transient service
        /// </summary>
        IServiceCollection AddTransient<TService>() where TService : class;

        /// <summary>
        /// Add transient service with implementation
        /// </summary>
        IServiceCollection AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// Add transient factory
        /// </summary>
        IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory) where TService : class;

        /// <summary>
        /// Add scoped service
        /// </summary>
        IServiceCollection AddScoped<TService>() where TService : class;

        /// <summary>
        /// Add scoped service with implementation
        /// </summary>
        IServiceCollection AddScoped<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// Add scoped factory
        /// </summary>
        IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory) where TService : class;

        /// <summary>
        /// Replace existing service registration
        /// </summary>
        IServiceCollection Replace<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// Remove service registration
        /// </summary>
        bool Remove<TService>() where TService : class;

        /// <summary>
        /// Build service provider from collection
        /// </summary>
        IServiceProvider BuildServiceProvider();

        /// <summary>
        /// Build service provider with options
        /// </summary>
        IServiceProvider BuildServiceProvider(ServiceProviderOptions options);
    }

    

    /// <summary>
    /// Service scope factory for creating scopes
    /// </summary>
    public interface IServiceScopeFactory
    {
        /// <summary>
        /// Create a new service scope
        /// </summary>
        IServiceScope CreateScope();
    }

    /// <summary>
    /// Service provider options for configuration
    /// </summary>
    public class ServiceProviderOptions
    {
        /// <summary>
        /// Validate scopes during service resolution
        /// </summary>
        public bool ValidateScopes { get; set; } = false;

        /// <summary>
        /// Validate service registrations on build
        /// </summary>
        public bool ValidateOnBuild { get; set; } = false;

        /// <summary>
        /// Enable detailed service resolution logging
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// Maximum depth for circular dependency detection
        /// </summary>
        public int MaxCircularDependencyDepth { get; set; } = 50;
    }

    /// <summary>
    /// Standard service descriptor compatible with Microsoft patterns
    /// </summary>
    public class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public object ImplementationInstance { get; set; }
        public Func<IServiceProvider, object> ImplementationFactory { get; set; }

        /// <summary>
        /// Create descriptor for singleton service
        /// </summary>
        public static ServiceDescriptor Singleton<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Singleton
            };
        }

        /// <summary>
        /// Create descriptor for singleton instance
        /// </summary>
        public static ServiceDescriptor Singleton<TService>(TService instance)
            where TService : class
        {
            return new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TService),
                Lifetime = ServiceLifetime.Singleton,
                ImplementationInstance = instance
            };
        }

        /// <summary>
        /// Create descriptor for singleton factory
        /// </summary>
        public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            return new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TService),
                Lifetime = ServiceLifetime.Singleton,
                ImplementationFactory = provider => factory(provider)
            };
        }

        /// <summary>
        /// Create descriptor for transient service
        /// </summary>
        public static ServiceDescriptor Transient<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Transient
            };
        }

        /// <summary>
        /// Create descriptor for transient factory
        /// </summary>
        public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            return new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TService),
                Lifetime = ServiceLifetime.Transient,
                ImplementationFactory = provider => factory(provider)
            };
        }

        /// <summary>
        /// Create descriptor for scoped service
        /// </summary>
        public static ServiceDescriptor Scoped<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Scoped
            };
        }

        /// <summary>
        /// Create descriptor for scoped factory
        /// </summary>
        public static ServiceDescriptor Scoped<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            return new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationType = typeof(TService),
                Lifetime = ServiceLifetime.Scoped,
                ImplementationFactory = provider => factory(provider)
            };
        }
    }
}