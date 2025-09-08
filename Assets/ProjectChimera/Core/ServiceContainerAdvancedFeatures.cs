using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Advanced features for ServiceContainer separated to follow Single Responsibility Principle.
    /// Handles complex scenarios like conditional registration, decorators, and collections.
    /// </summary>
    public class ServiceContainerAdvancedFeatures
    {
        private readonly IServiceContainer _container;
        private readonly Dictionary<string, ServiceRegistration> _namedServices = new Dictionary<string, ServiceRegistration>();
        private readonly Dictionary<Type, List<Func<IServiceLocator, bool>>> _conditionalRegistrations = new Dictionary<Type, List<Func<IServiceLocator, bool>>>();
        private readonly Dictionary<Type, List<Type>> _decorators = new Dictionary<Type, List<Type>>();
        private readonly Dictionary<Type, List<Type>> _collections = new Dictionary<Type, List<Type>>();

        public ServiceContainerAdvancedFeatures(IServiceContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Registers a service with a specific name for named resolution.
        /// </summary>
        public void RegisterNamed<TInterface, TImplementation>(string name) 
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            var registration = new ServiceRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Transient, null, null);
            _namedServices[$"{typeof(TInterface).FullName}:{name}"] = registration;
        }

        /// <summary>
        /// Registers a service with a condition that must be met for resolution.
        /// </summary>
        public void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition) 
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            if (!_conditionalRegistrations.ContainsKey(typeof(TInterface)))
            {
                _conditionalRegistrations[typeof(TInterface)] = new List<Func<IServiceLocator, bool>>();
            }
            
            _conditionalRegistrations[typeof(TInterface)].Add(condition);
            _container.RegisterTransient<TInterface, TImplementation>();
        }

        /// <summary>
        /// Registers a decorator for a service interface.
        /// </summary>
        public void RegisterDecorator<TInterface, TDecorator>() 
            where TInterface : class
            where TDecorator : class, TInterface, new()
        {
            if (!_decorators.ContainsKey(typeof(TInterface)))
            {
                _decorators[typeof(TInterface)] = new List<Type>();
            }
            
            _decorators[typeof(TInterface)].Add(typeof(TDecorator));
        }

        /// <summary>
        /// Registers a collection of implementations for a service interface.
        /// </summary>
        public void RegisterCollection<TInterface>(params Type[] implementations) where TInterface : class
        {
            if (!_collections.ContainsKey(typeof(TInterface)))
            {
                _collections[typeof(TInterface)] = new List<Type>();
            }
            
            _collections[typeof(TInterface)].AddRange(implementations);
            
            foreach (var impl in implementations)
            {
                var registration = new ServiceRegistration(typeof(TInterface), impl, ServiceLifetime.Transient, null, null);
                // Register each implementation individually for collection resolution
            }
        }

        /// <summary>
        /// Registers a service with a callback for post-construction initialization.
        /// </summary>
        public void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer) 
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            _container.RegisterTransient<TInterface>(locator =>
            {
                var instance = new TImplementation();
                initializer(instance);
                return instance;
            });
        }

        /// <summary>
        /// Registers an open generic type for resolution.
        /// </summary>
        public void RegisterOpenGeneric(Type serviceType, Type implementationType)
        {
            if (!serviceType.IsGenericTypeDefinition || !implementationType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Both service and implementation types must be open generic types.");
            }

            // Store open generic registration for later closed generic resolution
            var registration = new ServiceRegistration(serviceType, implementationType, ServiceLifetime.Transient, null, null);
            // Advanced implementation would handle this properly
        }

        /// <summary>
        /// Resolves a service by name.
        /// </summary>
        public T ResolveNamed<T>(string name) where T : class
        {
            var key = $"{typeof(T).FullName}:{name}";
            if (_namedServices.TryGetValue(key, out var registration))
            {
                return (T)CreateInstance(registration);
            }
            
            return _container.Resolve<T>(); // Fallback to regular resolution
        }

        /// <summary>
        /// Resolves all services matching a predicate.
        /// </summary>
        public IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class
        {
            return _container.ResolveAll<T>().Where(predicate);
        }

        /// <summary>
        /// Resolves a service or creates one using the provided factory.
        /// </summary>
        public T ResolveOrCreate<T>(Func<T> factory) where T : class
        {
            var service = _container.TryResolve<T>();
            return service ?? factory();
        }

        /// <summary>
        /// Resolves the last registered service of a type.
        /// </summary>
        public T ResolveLast<T>() where T : class
        {
            return _container.ResolveAll<T>().LastOrDefault();
        }

        /// <summary>
        /// Resolves a service with a specific lifetime preference.
        /// </summary>
        public T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class
        {
            // In a full implementation, this would consider lifetime preferences
            return _container.Resolve<T>();
        }

        /// <summary>
        /// Checks if a conditional registration should be resolved.
        /// </summary>
        public bool ShouldResolveConditional<T>(IServiceLocator locator) where T : class
        {
            if (_conditionalRegistrations.TryGetValue(typeof(T), out var conditions))
            {
                return conditions.Any(condition => condition(locator));
            }
            return true; // No conditions, always resolve
        }

        /// <summary>
        /// Gets all decorators for a service type.
        /// </summary>
        public IEnumerable<Type> GetDecorators<T>() where T : class
        {
            return _decorators.TryGetValue(typeof(T), out var decorators) ? decorators : Enumerable.Empty<Type>();
        }

        /// <summary>
        /// Gets all implementations in a collection for a service type.
        /// </summary>
        public IEnumerable<Type> GetCollectionImplementations<T>() where T : class
        {
            return _collections.TryGetValue(typeof(T), out var implementations) ? implementations : Enumerable.Empty<Type>();
        }

        /// <summary>
        /// Creates an instance from a service registration.
        /// </summary>
        private object CreateInstance(ServiceRegistration registration)
        {
            if (registration.Factory != null)
            {
                return registration.Factory(new ServiceLocatorAdapter(_container));
            }

            if (registration.ImplementationType != null)
            {
                return Activator.CreateInstance(registration.ImplementationType);
            }

            throw new InvalidOperationException($"Cannot create instance for service {registration.ServiceType.Name}");
        }

        /// <summary>
        /// Gets statistics about advanced registrations.
        /// </summary>
        public AdvancedRegistrationStats GetStats()
        {
            return new AdvancedRegistrationStats
            {
                NamedServices = _namedServices.Count,
                ConditionalRegistrations = _conditionalRegistrations.Values.Sum(list => list.Count),
                Decorators = _decorators.Values.Sum(list => list.Count),
                Collections = _collections.Count
            };
        }
    }

    /// <summary>
    /// Statistics about advanced service registrations.
    /// </summary>
    public class AdvancedRegistrationStats
    {
        public int NamedServices { get; set; }
        public int ConditionalRegistrations { get; set; }
        public int Decorators { get; set; }
        public int Collections { get; set; }

        public int TotalAdvancedRegistrations => NamedServices + ConditionalRegistrations + Decorators + Collections;
    }
}