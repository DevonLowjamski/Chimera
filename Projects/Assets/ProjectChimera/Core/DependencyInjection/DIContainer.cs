using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Advanced dependency injection container for Project Chimera.
    /// Extracted from DIGameManager.cs to provide dedicated DI functionality with lifetime management,
    /// service registration, resolution, and validation for cannabis cultivation simulation systems.
    /// </summary>
    public partial class ChimeraDIContainer : MonoBehaviour, IChimeraServiceContainer
    {
        [Header("Container Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _enableValidation = true;
        [SerializeField] private bool _enablePerformanceMetrics = false;
        
        // Service registration storage
        private readonly Dictionary<Type, ServiceRegistration> _services = new Dictionary<Type, ServiceRegistration>();
        private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<IChimeraServiceContainer, object>> _factories = new Dictionary<Type, Func<IChimeraServiceContainer, object>>();
        
        // Dependency tracking for validation
        private readonly Dictionary<Type, HashSet<Type>> _dependencyGraph = new Dictionary<Type, HashSet<Type>>();
        private readonly Stack<Type> _resolutionStack = new Stack<Type>();
        
        // Performance metrics
        private int _totalResolutions = 0;
        private int _successfulResolutions = 0;
        private readonly Dictionary<Type, int> _resolutionCounts = new Dictionary<Type, int>();
        
        public bool IsValidated { get; private set; }
        public int ServiceCount => _services.Count;
        public int SingletonCount => _singletonInstances.Count;
        
        /// <summary>
        /// Registers a singleton service instance
        /// </summary>
        public void RegisterSingleton<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            var serviceType = typeof(T);
            RegisterSingleton(serviceType, instance);
        }
        
        /// <summary>
        /// Registers a singleton service by type
        /// </summary>
        public void RegisterSingleton<T>() where T : class, new()
        {
            var serviceType = typeof(T);
            var registration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ImplementationType = serviceType,
                Lifetime = ServiceLifetime.Singleton,
                RegistrationTime = Time.time
            };
            
            _services[serviceType] = registration;
            
            if (_enableDebugLogging)
                Debug.Log($"[ChimeraDIContainer] Registered singleton: {serviceType.Name}");
        }
        
        /// <summary>
        /// Registers a singleton service with interface and implementation types
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>() 
            where TInterface : class 
            where TImplementation : class, TInterface, new()
        {
            var serviceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);
            var registration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = ServiceLifetime.Singleton,
                RegistrationTime = Time.time
            };
            
            _services[serviceType] = registration;
            
            if (_enableDebugLogging)
                Debug.Log($"[ChimeraDIContainer] Registered singleton: {serviceType.Name} -> {implementationType.Name}");
        }
        
        /// <summary>
        /// Registers a singleton service with type and instance
        /// </summary>
        public void RegisterSingleton(Type serviceType, object instance)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            var registration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ImplementationType = instance.GetType(),
                Lifetime = ServiceLifetime.Singleton,
                Instance = instance,
                RegistrationTime = Time.time
            };
            
            _services[serviceType] = registration;
            _singletonInstances[serviceType] = instance;
            
            if (_enableDebugLogging)
                Debug.Log($"[ChimeraDIContainer] Registered singleton instance: {serviceType.Name}");
        }
        
        /// <summary>
        /// Registers a transient service
        /// </summary>
        public void RegisterTransient<T>() where T : class, new()
        {
            var serviceType = typeof(T);
            var registration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ImplementationType = serviceType,
                Lifetime = ServiceLifetime.Transient,
                RegistrationTime = Time.time
            };
            
            _services[serviceType] = registration;
            
            if (_enableDebugLogging)
                Debug.Log($"[ChimeraDIContainer] Registered transient: {serviceType.Name}");
        }
        
        /// <summary>
        /// Registers a transient service with implementation type
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>() 
            where TInterface : class 
            where TImplementation : class, TInterface, new()
        {
            var serviceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);
            
            var registration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = ServiceLifetime.Transient,
                RegistrationTime = Time.time
            };
            
            _services[serviceType] = registration;
            
            if (_enableDebugLogging)
                Debug.Log($"[ChimeraDIContainer] Registered transient: {serviceType.Name} -> {implementationType.Name}");
        }
        
        /// <summary>
        /// Registers a factory function for creating service instances
        /// </summary>
        public void RegisterFactory<T>(Func<IChimeraServiceContainer, T> factory) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            var serviceType = typeof(T);
            _factories[serviceType] = container => factory(container);
            
            var registration = new ServiceRegistration
            {
                ServiceType = serviceType,
                ImplementationType = serviceType,
                Lifetime = ServiceLifetime.Factory,
                RegistrationTime = Time.time
            };
            
            _services[serviceType] = registration;
            
            if (_enableDebugLogging)
                Debug.Log($"[ChimeraDIContainer] Registered factory: {serviceType.Name}");
        }
        
        /// <summary>
        /// Resolves a service instance
        /// </summary>
        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }
        
        /// <summary>
        /// Attempts to resolve a service, returns null if not found
        /// </summary>
        public T TryResolve<T>() where T : class
        {
            try
            {
                return Resolve<T>();
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Resolves all registered services of a given type
        /// </summary>
        public IEnumerable<T> ResolveAll<T>() where T : class
        {
            var results = new List<T>();
            var serviceType = typeof(T);
            
            if (_services.TryGetValue(serviceType, out var registration))
            {
                try
                {
                    var instance = ResolveInternal(serviceType);
                    if (instance is T typedInstance)
                    {
                        results.Add(typedInstance);
                    }
                }
                catch (Exception ex)
                {
                    if (_enableDebugLogging)
                        Debug.LogWarning($"[ChimeraDIContainer] Failed to resolve {serviceType.Name}: {ex.Message}");
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Resolves a service by type
        /// </summary>
        public object Resolve(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            
            _totalResolutions++;
            
            // Track performance metrics
            if (_enablePerformanceMetrics)
            {
                _resolutionCounts[serviceType] = _resolutionCounts.GetValueOrDefault(serviceType) + 1;
            }
            
            // Check for circular dependencies
            if (_resolutionStack.Contains(serviceType))
            {
                var cycle = string.Join(" -> ", _resolutionStack.Reverse().Concat(new[] { serviceType }).Select(t => t.Name));
                throw new InvalidOperationException($"Circular dependency detected: {cycle}");
            }
            
            _resolutionStack.Push(serviceType);
            
            try
            {
                var instance = ResolveInternal(serviceType);
                _successfulResolutions++;
                return instance;
            }
            finally
            {
                _resolutionStack.Pop();
            }
        }
        
        /// <summary>
        /// Internal service resolution logic
        /// </summary>
        private object ResolveInternal(Type serviceType)
        {
            // Check for singleton instances first
            if (_singletonInstances.TryGetValue(serviceType, out var singletonInstance))
            {
                return singletonInstance;
            }
            
            // Check for factory registration
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                return factory(this);
            }
            
            // Check for service registration
            if (!_services.TryGetValue(serviceType, out var registration))
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered");
            }
            
            // Handle existing singleton instance
            if (registration.Instance != null)
            {
                return registration.Instance;
            }
            
            // Create new instance
            var instance = CreateInstance(registration);
            
            // Store singleton instances
            if (registration.Lifetime == ServiceLifetime.Singleton)
            {
                _singletonInstances[serviceType] = instance;
                registration.Instance = instance;
            }
            
            return instance;
        }
        
        /// <summary>
        /// Creates a new service instance with dependency injection
        /// </summary>
        private object CreateInstance(ServiceRegistration registration)
        {
            var implementationType = registration.ImplementationType;
            
            // Find constructor with most parameters (dependency injection pattern)
            var constructors = implementationType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .ToArray();
            
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                var parameterInstances = new object[parameters.Length];
                bool canResolveAll = true;
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;
                    
                    // Track dependency
                    if (!_dependencyGraph.ContainsKey(registration.ServiceType))
                        _dependencyGraph[registration.ServiceType] = new HashSet<Type>();
                    _dependencyGraph[registration.ServiceType].Add(parameterType);
                    
                    try
                    {
                        parameterInstances[i] = Resolve(parameterType);
                    }
                    catch
                    {
                        canResolveAll = false;
                        break;
                    }
                }
                
                if (canResolveAll)
                {
                    return Activator.CreateInstance(implementationType, parameterInstances);
                }
            }
            
            // Fall back to parameterless constructor
            try
            {
                return Activator.CreateInstance(implementationType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot create instance of {implementationType.Name}: {ex.Message}", ex);
            }
        }
    }
}