using UnityEngine;
using System.Collections.Generic;
using System;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Type definitions and data structures for the Dependency Injection system.
    /// Contains enums, classes, and interfaces for DI container functionality.
    /// </summary>
    
    /// <summary>
    /// Service lifetime enumeration for dependency injection
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// Service is created once and reused for all requests
        /// </summary>
        Singleton,
        
        /// <summary>
        /// New service instance is created for each request
        /// </summary>
        Transient,
        
        /// <summary>
        /// Service is created using a factory function
        /// </summary>
        Factory,
        
        /// <summary>
        /// Service lifetime is managed by a specific scope
        /// </summary>
        Scoped
    }
    
    /// <summary>
    /// Service registration information
    /// </summary>
    public class ServiceRegistration
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public object Instance { get; set; }
        public float RegistrationTime { get; set; }
        public bool IsLazy { get; set; }
        public string Name { get; set; }
        public Func<object, object> Factory { get; set; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServiceRegistration()
        {
            RegistrationTime = Time.time;
        }
        
        /// <summary>
        /// Constructor with all parameters (for legacy compatibility)
        /// </summary>
        public ServiceRegistration(Type serviceType, Type implementationType, ServiceLifetime lifetime, object instance, Func<object, object> factory)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            Instance = instance;
            Factory = factory;
            RegistrationTime = Time.time;
        }
        
        /// <summary>
        /// Age of the registration in seconds
        /// </summary>
        public float Age => Time.time - RegistrationTime;
    }
    
    /// <summary>
    /// Result of container validation operation
    /// </summary>
    public class ContainerValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public float ValidationTime { get; set; }
        public int ServicesValidated { get; set; }
        public TimeSpan ValidationDuration { get; set; }
        
        public ContainerValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }
        
        /// <summary>
        /// Gets a summary of validation results
        /// </summary>
        public string GetSummary()
        {
            return $"Validation: {(IsValid ? "PASSED" : "FAILED")} | Errors: {Errors.Count} | Warnings: {Warnings.Count}";
        }
    }
    
    /// <summary>
    /// Container performance and usage statistics
    /// </summary>
    public class ContainerStatistics
    {
        public int TotalServices { get; set; }
        public int SingletonServices { get; set; }
        public int TransientServices { get; set; }
        public int FactoryServices { get; set; }
        public int TotalResolutions { get; set; }
        public int SuccessfulResolutions { get; set; }
        public int FailedResolutions { get; set; }
        public float ResolutionSuccessRate { get; set; }
        public Dictionary<Type, int> MostResolvedServices { get; set; }
        public bool IsValidated { get; set; }
        public float AverageResolutionTime { get; set; }
        
        public ContainerStatistics()
        {
            MostResolvedServices = new Dictionary<Type, int>();
        }
        
        /// <summary>
        /// Gets a formatted statistics report
        /// </summary>
        public string GetReport()
        {
            return $"DI Container Stats:\n" +
                   $"Services: {TotalServices} (S:{SingletonServices}, T:{TransientServices}, F:{FactoryServices})\n" +
                   $"Resolutions: {SuccessfulResolutions}/{TotalResolutions} ({ResolutionSuccessRate:P1} success rate)\n" +
                   $"Validated: {IsValidated}";
        }
    }
    
    /// <summary>
    /// Service health status enumeration
    /// </summary>
    public enum ServiceStatus
    {
        Unknown,
        Healthy,
        Warning,
        Failed,
        Initializing,
        Disposed
    }
    
    /// <summary>
    /// Health report for all services in the container
    /// </summary>
    public class ServiceHealthReport
    {
        public Dictionary<Type, ServiceStatus> ServiceStatuses { get; set; }
        public List<string> CriticalErrors { get; set; }
        public List<string> Warnings { get; set; }
        public bool IsHealthy { get; set; }
        public TimeSpan InitializationTime { get; set; }
        public float ReportTime { get; set; }
        
        public ServiceHealthReport()
        {
            ServiceStatuses = new Dictionary<Type, ServiceStatus>();
            CriticalErrors = new List<string>();
            Warnings = new List<string>();
            ReportTime = Time.time;
        }
        
        /// <summary>
        /// Gets count of services by status
        /// </summary>
        public Dictionary<ServiceStatus, int> GetStatusCounts()
        {
            var counts = new Dictionary<ServiceStatus, int>();
            foreach (ServiceStatus status in Enum.GetValues(typeof(ServiceStatus)))
            {
                counts[status] = 0;
            }
            
            foreach (var status in ServiceStatuses.Values)
            {
                counts[status]++;
            }
            
            return counts;
        }
    }
    
    /// <summary>
    /// Interface for Chimera dependency injection container
    /// </summary>
    public interface IChimeraServiceContainer : IDisposable
    {
        // Registration methods
        void RegisterSingleton<T>(T instance) where T : class;
        void RegisterSingleton<T>() where T : class, new();
        void RegisterSingleton<TInterface, TImplementation>() 
            where TInterface : class 
            where TImplementation : class, TInterface, new();
        void RegisterSingleton(Type serviceType, object instance);
        void RegisterTransient<T>() where T : class, new();
        void RegisterTransient<TInterface, TImplementation>() 
            where TInterface : class 
            where TImplementation : class, TInterface, new();
        void RegisterFactory<T>(Func<IChimeraServiceContainer, T> factory) where T : class;
        
        // Resolution methods
        T Resolve<T>() where T : class;
        T TryResolve<T>() where T : class;
        object Resolve(Type serviceType);
        IEnumerable<T> ResolveAll<T>() where T : class;
        
        // Validation and diagnostics
        ContainerValidationResult Verify();
        bool IsRegistered<T>();
        bool IsRegistered(Type serviceType);
        
        // Additional methods needed for compatibility
        ContainerStatistics GetStatistics();
        IEnumerable<Type> GetRegisteredServiceTypes();
        void Clear();
        ChimeraDIContainer CreateChildContainer();
        
        // Properties
        bool IsValidated { get; }
        int ServiceCount { get; }
        int SingletonCount { get; }
    }
    
    /// <summary>
    /// Factory for creating Chimera DI containers with different configurations
    /// </summary>
    public static class ChimeraDIContainerFactory
    {
        /// <summary>
        /// Creates a development container with debug logging enabled
        /// </summary>
        public static IChimeraServiceContainer CreateForDevelopment()
        {
            var containerObject = new GameObject("DI_Container_Development");
            var container = containerObject.AddComponent<ChimeraDIContainer>();
            
            // Configure for development
            var containerType = typeof(ChimeraDIContainer);
            var debugField = containerType.GetField("_enableDebugLogging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var validationField = containerType.GetField("_enableValidation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metricsField = containerType.GetField("_enablePerformanceMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            debugField?.SetValue(container, true);
            validationField?.SetValue(container, true);
            metricsField?.SetValue(container, true);
            
            UnityEngine.Object.DontDestroyOnLoad(containerObject);
            
            Debug.Log("[ChimeraDIContainerFactory] Development container created with full debugging");
            return container;
        }
        
        /// <summary>
        /// Creates a production container optimized for performance
        /// </summary>
        public static IChimeraServiceContainer CreateForProduction()
        {
            var containerObject = new GameObject("DI_Container_Production");
            var container = containerObject.AddComponent<ChimeraDIContainer>();
            
            // Configure for production
            var containerType = typeof(ChimeraDIContainer);
            var debugField = containerType.GetField("_enableDebugLogging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var validationField = containerType.GetField("_enableValidation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metricsField = containerType.GetField("_enablePerformanceMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            debugField?.SetValue(container, false);
            validationField?.SetValue(container, false);
            metricsField?.SetValue(container, false);
            
            UnityEngine.Object.DontDestroyOnLoad(containerObject);
            
            Debug.Log("[ChimeraDIContainerFactory] Production container created with optimized performance");
            return container;
        }
        
        /// <summary>
        /// Creates a testing container with validation enabled
        /// </summary>
        public static IChimeraServiceContainer CreateForTesting()
        {
            var containerObject = new GameObject("DI_Container_Testing");
            var container = containerObject.AddComponent<ChimeraDIContainer>();
            
            // Configure for testing
            var containerType = typeof(ChimeraDIContainer);
            var debugField = containerType.GetField("_enableDebugLogging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var validationField = containerType.GetField("_enableValidation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metricsField = containerType.GetField("_enablePerformanceMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            debugField?.SetValue(container, true);
            validationField?.SetValue(container, true);
            metricsField?.SetValue(container, false);
            
            Debug.Log("[ChimeraDIContainerFactory] Testing container created with validation enabled");
            return container;
        }
    }

    /// <summary>
    /// Advanced service descriptor for compatibility with Phase 0 DI unification
    /// </summary>
    public class AdvancedServiceDescriptor : ServiceRegistration
    {
        public string Name { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsDecorator { get; set; } = false;
        public bool IsOpenGeneric { get; set; } = false;
        public string[] Tags { get; set; } = new string[0];
    }

    /// <summary>
    /// Container verification result for validation
    /// </summary>
    public class ContainerVerificationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> ValidationMessages { get; set; } = new List<string>();
        public int VerifiedServices { get; set; }
        public int TotalServices { get; set; }
        public TimeSpan VerificationTime { get; set; }
    }

    /// <summary>
    /// Service validation result for compatibility
    /// </summary>
    public class ServiceValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public int TotalServices { get; set; }
        public int ValidServiceCount { get; set; }
        public int InvalidServiceCount { get; set; }
        public List<Type> ValidServices { get; set; } = new List<Type>();
        public List<Type> InvalidServices { get; set; } = new List<Type>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> ValidationMessages { get; set; } = new List<string>();
        
        public static ServiceValidationResult Success() => new ServiceValidationResult { IsValid = true };
        public static ServiceValidationResult Failure(string message) => new ServiceValidationResult { IsValid = false, Message = message };
    }
    
    /// <summary>
    /// Attribute for marking classes that should be automatically registered with DI
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegisterAttribute : Attribute
    {
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
        public Type InterfaceType { get; set; }
        public string Name { get; set; }
        
        public AutoRegisterAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            Lifetime = lifetime;
        }
    }
    
    /// <summary>
    /// Attribute for marking constructor parameters that should be injected
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
        public string Name { get; set; }
        public bool Optional { get; set; }
        
        public InjectAttribute(string name = null, bool optional = false)
        {
            Name = name;
            Optional = optional;
        }
    }
    
    /// <summary>
    /// Interface for objects that need to be notified after dependency injection
    /// </summary>
    public interface IInjectableInitializer
    {
        void OnDependenciesInjected();
    }

}