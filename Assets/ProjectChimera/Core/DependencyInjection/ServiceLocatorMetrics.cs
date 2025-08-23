using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.DependencyInjection
{
    public class ServiceLocatorMetrics
    {
        public int TotalResolutions { get; set; }
        public int CacheHits { get; set; }
        public float CacheHitRate { get; set; }
        public int RegisteredServices { get; set; }
        public int SingletonInstances { get; set; }
        public int ActiveScopes { get; set; }
        public Dictionary<Type, int> ResolutionCounts { get; set; }
        public int DiscoveryAttempts { get; set; }
        public int ValidationErrors { get; set; }
        public int CachedTypes { get; set; }
        public int DiscoveredTypes { get; set; }
        
        public string GetSummary()
        {
            return $"ServiceLocator Metrics:\n" +
                   $"Resolutions: {TotalResolutions} (Cache: {CacheHits}, Rate: {CacheHitRate:P1})\n" +
                   $"Services: {RegisteredServices} registered, {SingletonInstances} singletons\n" +
                   $"Discovery: {DiscoveryAttempts} attempts, {DiscoveredTypes} discovered\n" +
                   $"Validation: {ValidationErrors} errors, {CachedTypes} cached types";
        }
    }
    
    // ServiceValidationResult moved to DITypes.cs to avoid namespace conflicts
}