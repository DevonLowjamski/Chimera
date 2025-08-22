using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Validation and verification functionality for ChimeraDIContainer.
    /// Split for maintainability and line count compliance (≤300 lines per file).
    /// </summary>
    public partial class ChimeraDIContainer
    {
        /// <summary>
        /// Validates the container configuration and dependencies
        /// </summary>
        public ContainerValidationResult Verify()
        {
            var result = new ContainerValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                ValidationTime = Time.time
            };
            
            try
            {
                // Check for circular dependencies
                ValidateCircularDependencies(result);
                
                // Check for missing dependencies
                ValidateMissingDependencies(result);
                
                // Check for duplicate registrations
                ValidateDuplicateRegistrations(result);
                
                // Validate singleton lifetime consistency
                ValidateSingletonConsistency(result);
                
                IsValidated = result.IsValid;
                
                if (_enableDebugLogging)
                {
                    if (result.IsValid)
                        Debug.Log("[ChimeraDIContainer] Container validation passed");
                    else
                        Debug.LogError($"[ChimeraDIContainer] Container validation failed with {result.Errors.Count} errors");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation failed with exception: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Validates for circular dependencies in the dependency graph
        /// </summary>
        private void ValidateCircularDependencies(ContainerValidationResult result)
        {
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();
            
            foreach (var serviceType in _services.Keys)
            {
                if (!visited.Contains(serviceType))
                {
                    if (HasCircularDependency(serviceType, visited, recursionStack))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Circular dependency detected involving {serviceType.Name}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Recursive helper for circular dependency detection
        /// </summary>
        private bool HasCircularDependency(Type serviceType, HashSet<Type> visited, HashSet<Type> recursionStack)
        {
            visited.Add(serviceType);
            recursionStack.Add(serviceType);
            
            if (_dependencyGraph.TryGetValue(serviceType, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        if (HasCircularDependency(dependency, visited, recursionStack))
                            return true;
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        return true;
                    }
                }
            }
            
            recursionStack.Remove(serviceType);
            return false;
        }
        
        /// <summary>
        /// Validates that all dependencies can be resolved
        /// </summary>
        private void ValidateMissingDependencies(ContainerValidationResult result)
        {
            foreach (var registration in _services.Values)
            {
                if (registration.Lifetime == ServiceLifetime.Factory)
                    continue; // Skip factory registrations
                
                var constructors = registration.ImplementationType.GetConstructors();
                var hasValidConstructor = false;
                
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    bool canResolveAll = true;
                    
                    foreach (var parameter in parameters)
                    {
                        if (!_services.ContainsKey(parameter.ParameterType) && 
                            !_factories.ContainsKey(parameter.ParameterType))
                        {
                            canResolveAll = false;
                            break;
                        }
                    }
                    
                    if (canResolveAll)
                    {
                        hasValidConstructor = true;
                        break;
                    }
                }
                
                if (!hasValidConstructor && constructors.All(c => c.GetParameters().Length > 0))
                {
                    result.IsValid = false;
                    result.Errors.Add($"No resolvable constructor found for {registration.ServiceType.Name}");
                }
            }
        }
        
        /// <summary>
        /// Validates for duplicate service registrations
        /// </summary>
        private void ValidateDuplicateRegistrations(ContainerValidationResult result)
        {
            var duplicateTypes = _services.Keys
                .GroupBy(type => type)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);
            
            foreach (var duplicateType in duplicateTypes)
            {
                result.Warnings.Add($"Multiple registrations found for {duplicateType.Name}");
            }
        }
        
        /// <summary>
        /// Validates singleton lifetime consistency
        /// </summary>
        private void ValidateSingletonConsistency(ContainerValidationResult result)
        {
            foreach (var registration in _services.Values)
            {
                if (registration.Lifetime == ServiceLifetime.Singleton)
                {
                    // Check if singleton is properly instantiated
                    if (registration.Instance == null && !_singletonInstances.ContainsKey(registration.ServiceType))
                    {
                        result.Warnings.Add($"Singleton {registration.ServiceType.Name} is registered but not instantiated");
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets detailed container statistics for debugging
        /// </summary>
        public ContainerStatistics GetStatistics()
        {
            return new ContainerStatistics
            {
                TotalServices = _services.Count,
                SingletonServices = _services.Values.Count(s => s.Lifetime == ServiceLifetime.Singleton),
                TransientServices = _services.Values.Count(s => s.Lifetime == ServiceLifetime.Transient),
                FactoryServices = _services.Values.Count(s => s.Lifetime == ServiceLifetime.Factory),
                TotalResolutions = _totalResolutions,
                SuccessfulResolutions = _successfulResolutions,
                FailedResolutions = _totalResolutions - _successfulResolutions,
                ResolutionSuccessRate = _totalResolutions > 0 ? (float)_successfulResolutions / _totalResolutions : 0f,
                MostResolvedServices = _enablePerformanceMetrics ? 
                    _resolutionCounts.OrderByDescending(kvp => kvp.Value).Take(5).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) :
                    new Dictionary<Type, int>(),
                IsValidated = IsValidated
            };
        }
        
        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }
        
        /// <summary>
        /// Checks if a service type is registered
        /// </summary>
        public bool IsRegistered(Type serviceType)
        {
            return _services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType);
        }
        
        /// <summary>
        /// Gets all registered service types
        /// </summary>
        public IEnumerable<Type> GetRegisteredServiceTypes()
        {
            return _services.Keys.Concat(_factories.Keys).Distinct();
        }
        
        /// <summary>
        /// Clears all registrations (for testing)
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _singletonInstances.Clear();
            _factories.Clear();
            _dependencyGraph.Clear();
            _resolutionCounts.Clear();
            
            _totalResolutions = 0;
            _successfulResolutions = 0;
            IsValidated = false;
            
            if (_enableDebugLogging)
                Debug.Log("[ChimeraDIContainer] Container cleared");
        }
        
        /// <summary>
        /// Creates a child container for scoped registrations
        /// </summary>
        public ChimeraDIContainer CreateChildContainer()
        {
            var childContainerObject = new GameObject("ChildContainer");
            var childContainer = childContainerObject.AddComponent<ChimeraDIContainer>();
            
            // Copy configuration settings using reflection since fields are private
            var containerType = typeof(ChimeraDIContainer);
            var debugField = containerType.GetField("_enableDebugLogging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var validationField = containerType.GetField("_enableValidation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metricsField = containerType.GetField("_enablePerformanceMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            debugField?.SetValue(childContainer, _enableDebugLogging);
            validationField?.SetValue(childContainer, _enableValidation);
            metricsField?.SetValue(childContainer, _enablePerformanceMetrics);
            
            // Copy parent registrations
            foreach (var service in _services)
            {
                childContainer._services[service.Key] = service.Value;
            }
            
            foreach (var factory in _factories)
            {
                childContainer._factories[factory.Key] = factory.Value;
            }
            
            if (_enableDebugLogging)
                Debug.Log("[ChimeraDIContainer] Child container created");
            
            return childContainer;
        }
        
        /// <summary>
        /// Disposes the container and cleans up resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Dispose singleton instances that implement IDisposable
                foreach (var instance in _singletonInstances.Values)
                {
                    if (instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                Clear();
                
                if (_enableDebugLogging)
                    Debug.Log("[ChimeraDIContainer] Container disposed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChimeraDIContainer] Error during disposal: {ex.Message}");
            }
        }
    }
}