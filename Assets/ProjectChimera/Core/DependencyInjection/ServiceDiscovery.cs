using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Handles automatic service discovery and validation for the ServiceLocator
    /// </summary>
    public class ServiceDiscovery
    {
        private readonly Dictionary<Type, List<Type>> _implementationCache = new Dictionary<Type, List<Type>>();
        private readonly Dictionary<Type, DateTime> _cacheTimestamps = new Dictionary<Type, DateTime>();
        private readonly HashSet<Type> _discoveredTypes = new HashSet<Type>();
        private readonly TimeSpan _cacheExpirationTime = TimeSpan.FromMinutes(5);
        
        private int _discoveryAttempts = 0;
        private int _validationErrors = 0;
        
        public int DiscoveryAttempts => _discoveryAttempts;
        public int ValidationErrors => _validationErrors;
        public int DiscoveredTypesCount => _discoveredTypes.Count;
        
        /// <summary>
        /// Attempts to discover and create an instance of the specified service type
        /// </summary>
        public bool TryDiscoverService(Type serviceType, out object instance)
        {
            instance = null;
            _discoveryAttempts++;
            
            try
            {
                // Skip if already attempted discovery for this type
                if (_discoveredTypes.Contains(serviceType))
                {
                    return false;
                }
                
                _discoveredTypes.Add(serviceType);
                
                // Try to find implementations in loaded assemblies
                var implementations = DiscoverImplementations(serviceType);
                
                if (implementations.Any())
                {
                    var bestImplementation = SelectBestImplementation(serviceType, implementations);
                    
                    if (bestImplementation != null && ValidateImplementation(serviceType, bestImplementation))
                    {
                        // Create instance of the discovered service
                        instance = Activator.CreateInstance(bestImplementation);
                        
                        ChimeraLogger.Log($"[ServiceDiscovery] Auto-discovered: {serviceType.Name} -> {bestImplementation.Name}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _validationErrors++;
                ChimeraLogger.LogWarning($"[ServiceDiscovery] Service discovery failed for {serviceType.Name}: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Discovers all implementations of a given service type
        /// </summary>
        public List<Type> DiscoverImplementations(Type serviceType)
        {
            // Check cache first
            if (_implementationCache.TryGetValue(serviceType, out var cachedImplementations) &&
                IsCacheValid(serviceType))
            {
                return cachedImplementations;
            }
            
            var implementations = new List<Type>();
            
            try
            {
                // Search in all loaded assemblies
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes()
                            .Where(t => !t.IsAbstract && !t.IsInterface && serviceType.IsAssignableFrom(t))
                            .ToList();
                        
                        implementations.AddRange(types);
                    }
                    catch (Exception ex)
                    {
                        // Skip assemblies that can't be reflected
                        ChimeraLogger.LogWarning($"[ServiceDiscovery] Failed to reflect assembly {assembly.FullName}: {ex.Message}");
                    }
                }
                
                // Cache the results
                _implementationCache[serviceType] = implementations;
                _cacheTimestamps[serviceType] = DateTime.Now;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[ServiceDiscovery] Error during implementation discovery: {ex.Message}");
            }
            
            return implementations;
        }
        
        /// <summary>
        /// Selects the best implementation from available options
        /// </summary>
        public Type SelectBestImplementation(Type serviceType, List<Type> implementations)
        {
            if (!implementations.Any()) return null;
            
            // Prioritize concrete classes over abstract
            var concreteTypes = implementations.Where(t => !t.IsAbstract).ToList();
            if (concreteTypes.Any())
            {
                // Prefer types with parameterless constructors
                var withDefaultConstructor = concreteTypes
                    .Where(t => t.GetConstructors().Any(c => c.GetParameters().Length == 0))
                    .ToList();
                
                if (withDefaultConstructor.Any())
                {
                    // Return the first valid one (could be enhanced with more sophisticated selection)
                    return withDefaultConstructor.First();
                }
                
                return concreteTypes.First();
            }
            
            return implementations.First();
        }
        
        /// <summary>
        /// Validates that an implementation can be used for a service type
        /// </summary>
        public bool ValidateImplementation(Type serviceType, Type implementationType)
        {
            try
            {
                // Check if type is assignable
                if (!serviceType.IsAssignableFrom(implementationType))
                {
                    _validationErrors++;
                    return false;
                }
                
                // Check if type can be instantiated
                if (implementationType.IsAbstract || implementationType.IsInterface)
                {
                    _validationErrors++;
                    return false;
                }
                
                // Check for parameterless constructor
                var constructors = implementationType.GetConstructors();
                if (!constructors.Any(c => c.GetParameters().Length == 0))
                {
                    _validationErrors++;
                    ChimeraLogger.LogWarning($"[ServiceDiscovery] No parameterless constructor found for {implementationType.Name}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _validationErrors++;
                ChimeraLogger.LogError($"[ServiceDiscovery] Validation failed for {implementationType.Name}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validates all provided service registrations
        /// </summary>
        public ServiceValidationResult ValidateServices(Dictionary<Type, ServiceRegistration> services)
        {
            var result = new ServiceValidationResult();
            
            foreach (var kvp in services)
            {
                try
                {
                    var registration = kvp.Value;
                    
                    // Validate that implementation type is compatible
                    if (!ValidateImplementation(registration.ServiceType, registration.ImplementationType))
                    {
                        result.InvalidServices.Add(registration.ServiceType);
                        result.Errors.Add($"Invalid implementation for {registration.ServiceType.Name}");
                    }
                    else
                    {
                        result.ValidServices.Add(registration.ServiceType);
                    }
                }
                catch (Exception ex)
                {
                    result.InvalidServices.Add(kvp.Key);
                    result.Errors.Add($"Validation error for {kvp.Key.Name}: {ex.Message}");
                }
            }
            
            result.TotalServices = services.Count;
            result.ValidServiceCount = result.ValidServices.Count;
            result.InvalidServiceCount = result.InvalidServices.Count;
            result.IsValid = result.InvalidServiceCount == 0;
            
            return result;
        }
        
        /// <summary>
        /// Checks if cached data is still valid
        /// </summary>
        private bool IsCacheValid(Type serviceType)
        {
            if (_cacheTimestamps.TryGetValue(serviceType, out var timestamp))
            {
                return DateTime.Now - timestamp < _cacheExpirationTime;
            }
            return false;
        }
        
        /// <summary>
        /// Clears all discovery caches
        /// </summary>
        public void ClearCache()
        {
            _implementationCache.Clear();
            _cacheTimestamps.Clear();
            _discoveredTypes.Clear();
            ChimeraLogger.Log("[ServiceDiscovery] Discovery cache cleared");
        }
    }
}