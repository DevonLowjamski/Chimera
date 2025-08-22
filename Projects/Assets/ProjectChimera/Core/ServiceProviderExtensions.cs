using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Extension methods for IServiceProvider to provide additional functionality
    /// Compatible with Microsoft.Extensions.DependencyInjection patterns
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Get service of type T, or null if not registered
        /// </summary>
        public static T GetService<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return provider.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Get required service of type T, throws if not registered
        /// </summary>
        public static T GetRequiredService<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var service = provider.GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Required service of type {typeof(T).Name} is not registered");
            }

            return service;
        }

        /// <summary>
        /// Get all services of type T
        /// </summary>
        public static IEnumerable<T> GetServices<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return provider.GetServices(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Try to get service of type T, returns true if found
        /// </summary>
        public static bool TryGetService<T>(this IServiceProvider provider, out T service) where T : class
        {
            service = null;
            
            if (provider == null)
                return false;

            try
            {
                service = provider.GetService<T>();
                return service != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get service or create using factory if not registered
        /// </summary>
        public static T GetServiceOrCreate<T>(this IServiceProvider provider, Func<T> factory) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var service = provider.GetService<T>();
            return service ?? factory();
        }

        /// <summary>
        /// Get service with fallback value
        /// </summary>
        public static T GetServiceOrDefault<T>(this IServiceProvider provider, T defaultValue = null) where T : class
        {
            if (provider == null)
                return defaultValue;

            return provider.GetService<T>() ?? defaultValue;
        }

        /// <summary>
        /// Create a new scope if the provider supports it
        /// </summary>
        public static IServiceScope CreateScope(this IServiceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (provider is IServiceScopeFactory scopeFactory)
            {
                return scopeFactory.CreateScope();
            }

            throw new InvalidOperationException("The service provider does not support scopes");
        }

        /// <summary>
        /// Execute an action within a service scope
        /// </summary>
        public static void ExecuteInScope(this IServiceProvider provider, Action<IServiceProvider> action)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var scope = provider.CreateScope())
            {
                action(scope.ServiceProvider);
            }
        }

        /// <summary>
        /// Execute a function within a service scope and return the result
        /// </summary>
        public static TResult ExecuteInScope<TResult>(this IServiceProvider provider, Func<IServiceProvider, TResult> function)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            using (var scope = provider.CreateScope())
            {
                return function(scope.ServiceProvider);
            }
        }

        /// <summary>
        /// Get all services that implement a specific interface
        /// </summary>
        public static IEnumerable<TInterface> GetImplementationsOf<TInterface>(this IServiceProvider provider) 
            where TInterface : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return provider.GetServices<TInterface>();
        }

        /// <summary>
        /// Get services by predicate
        /// </summary>
        public static IEnumerable<T> GetServicesWhere<T>(this IServiceProvider provider, Func<T, bool> predicate) 
            where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return provider.GetServices<T>().Where(predicate);
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool IsServiceRegistered<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                return false;

            return provider.GetService<T>() != null;
        }

        /// <summary>
        /// Check if a service type is registered
        /// </summary>
        public static bool IsServiceRegistered(this IServiceProvider provider, Type serviceType)
        {
            if (provider == null || serviceType == null)
                return false;

            return provider.GetService(serviceType) != null;
        }

        /// <summary>
        /// Get service count for a specific type
        /// </summary>
        public static int GetServiceCount<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                return 0;

            return provider.GetServices<T>().Count();
        }

        /// <summary>
        /// Get the first service that matches a predicate
        /// </summary>
        public static T GetFirstServiceWhere<T>(this IServiceProvider provider, Func<T, bool> predicate) 
            where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return provider.GetServices<T>().FirstOrDefault(predicate);
        }

        /// <summary>
        /// Get the last registered service of type T
        /// </summary>
        public static T GetLastService<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return provider.GetServices<T>().LastOrDefault();
        }

        /// <summary>
        /// Execute action for each service of type T
        /// </summary>
        public static void ForEachService<T>(this IServiceProvider provider, Action<T> action) 
            where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            foreach (var service in provider.GetServices<T>())
            {
                try
                {
                    action(service);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceProviderExtensions] Error executing action on service {typeof(T).Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Try to resolve multiple services with error handling
        /// </summary>
        public static bool TryGetServices<T>(this IServiceProvider provider, out IEnumerable<T> services) 
            where T : class
        {
            services = null;

            if (provider == null)
                return false;

            try
            {
                services = provider.GetServices<T>();
                return services != null && services.Any();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServiceProviderExtensions] Error getting services of type {typeof(T).Name}: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Extension methods for IServiceCollection to provide additional registration patterns
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Try to add a service if it's not already registered
        /// </summary>
        public static IServiceCollection TryAddSingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (!services.Any(s => s.ServiceType == typeof(TService)))
            {
                services.AddSingleton<TService, TImplementation>();
            }

            return services;
        }

        /// <summary>
        /// Try to add a singleton instance if not already registered
        /// </summary>
        public static IServiceCollection TryAddSingleton<TService>(this IServiceCollection services, TService instance)
            where TService : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (!services.Any(s => s.ServiceType == typeof(TService)))
            {
                services.AddSingleton(instance);
            }

            return services;
        }

        /// <summary>
        /// Try to add a transient service if not already registered
        /// </summary>
        public static IServiceCollection TryAddTransient<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (!services.Any(s => s.ServiceType == typeof(TService)))
            {
                services.AddTransient<TService, TImplementation>();
            }

            return services;
        }

        /// <summary>
        /// Try to add a scoped service if not already registered
        /// </summary>
        public static IServiceCollection TryAddScoped<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (!services.Any(s => s.ServiceType == typeof(TService)))
            {
                services.AddScoped<TService, TImplementation>();
            }

            return services;
        }

        /// <summary>
        /// Add a service if condition is met
        /// </summary>
        public static IServiceCollection AddIf<TService, TImplementation>(this IServiceCollection services, bool condition)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (condition)
            {
                services.AddTransient<TService, TImplementation>();
            }

            return services;
        }

        /// <summary>
        /// Configure services using a delegate
        /// </summary>
        public static IServiceCollection Configure(this IServiceCollection services, Action<IServiceCollection> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            configure(services);
            return services;
        }

        /// <summary>
        /// Remove all services of a specific type
        /// </summary>
        public static IServiceCollection RemoveAll<TService>(this IServiceCollection services) where TService : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var toRemove = services.Where(s => s.ServiceType == typeof(TService)).ToList();
            foreach (var service in toRemove)
            {
                services.Remove(service);
            }

            return services;
        }

        /// <summary>
        /// Get registration count for a specific service type
        /// </summary>
        public static int GetRegistrationCount<TService>(this IServiceCollection services) where TService : class
        {
            if (services == null)
                return 0;

            return services.Count(s => s.ServiceType == typeof(TService));
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool IsRegistered<TService>(this IServiceCollection services) where TService : class
        {
            if (services == null)
                return false;

            return services.Any(s => s.ServiceType == typeof(TService));
        }
    }
}