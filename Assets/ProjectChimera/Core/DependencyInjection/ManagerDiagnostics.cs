using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Provides diagnostic and metrics functionality for the manager registry
    /// </summary>
    public class ManagerDiagnostics
    {
        private readonly Dictionary<Type, ManagerRegistration> _registrations;
        private readonly Dictionary<Type, ChimeraManager> _instances;
        private readonly Dictionary<Type, List<Type>> _dependencies;
        private readonly ManagerInitializer _initializer;
        private readonly ManagerDependencyResolver _dependencyResolver;
        
        public ManagerDiagnostics(
            Dictionary<Type, ManagerRegistration> registrations,
            Dictionary<Type, ChimeraManager> instances,
            Dictionary<Type, List<Type>> dependencies,
            ManagerInitializer initializer,
            ManagerDependencyResolver dependencyResolver)
        {
            _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
            _instances = instances ?? throw new ArgumentNullException(nameof(instances));
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _initializer = initializer;
            _dependencyResolver = dependencyResolver;
        }
        
        /// <summary>
        /// Gets comprehensive metrics about the manager registry
        /// </summary>
        public ManagerRegistryMetrics GetMetrics()
        {
            return new ManagerRegistryMetrics
            {
                RegisteredManagers = _registrations.Count,
                ActiveManagers = _instances.Count,
                IsInitialized = _initializer?.IsInitialized ?? false,
                InitializationOrder = _initializer?.InitializationOrder ?? new List<Type>(),
                DependencyCount = _dependencies.Values.Sum(deps => deps.Count),
                ValidationResult = _dependencyResolver?.ValidateDependencies() ?? new ManagerValidationResult { IsValid = false }
            };
        }
        
        /// <summary>
        /// Gets all registered manager types and their status
        /// </summary>
        public Dictionary<Type, ManagerStatus> GetManagerStatus()
        {
            var status = new Dictionary<Type, ManagerStatus>();
            
            foreach (var kvp in _registrations)
            {
                var managerType = kvp.Key;
                var registration = kvp.Value;
                var isActive = _instances.ContainsKey(managerType);
                
                status[managerType] = new ManagerStatus
                {
                    IsRegistered = true,
                    IsActive = isActive,
                    Priority = registration.Priority,
                    DependencyCount = _dependencies.TryGetValue(managerType, out var deps) ? deps.Count : 0,
                    RegistrationTime = registration.RegistrationTime
                };
            }
            
            return status;
        }
        
        /// <summary>
        /// Generates a comprehensive system health report
        /// </summary>
        public ManagerHealthReport GenerateHealthReport()
        {
            var report = new ManagerHealthReport();
            var metrics = GetMetrics();
            var validation = _dependencyResolver?.ValidateDependencies() ?? new ManagerValidationResult();
            
            // Overall health assessment
            report.OverallHealth = DetermineOverallHealth(metrics, validation);
            report.HealthScore = CalculateHealthScore(metrics, validation);
            
            // Detailed status
            report.ManagerStatuses = GetManagerStatus();
            report.ValidationResult = validation;
            report.Metrics = metrics;
            
            // Issues and recommendations
            report.CriticalIssues = FindCriticalIssues(validation);
            report.Warnings = FindWarnings(metrics, validation);
            report.Recommendations = GenerateRecommendations(metrics, validation);
            
            return report;
        }
        
        /// <summary>
        /// Determines overall system health
        /// </summary>
        private ManagerHealthStatus DetermineOverallHealth(ManagerRegistryMetrics metrics, ManagerValidationResult validation)
        {
            if (!validation.IsValid)
                return ManagerHealthStatus.Critical;
            
            if (validation.Warnings.Count > 0 || metrics.RegisteredManagers != metrics.ActiveManagers)
                return ManagerHealthStatus.Warning;
            
            return ManagerHealthStatus.Healthy;
        }
        
        /// <summary>
        /// Calculates a numeric health score (0-100)
        /// </summary>
        private float CalculateHealthScore(ManagerRegistryMetrics metrics, ManagerValidationResult validation)
        {
            float score = 100f;
            
            // Deduct for validation issues
            if (!validation.IsValid)
                score -= 30f;
            
            score -= validation.MissingDependencies.Count * 10f;
            score -= validation.Warnings.Count * 5f;
            
            if (validation.HasCircularDependencies)
                score -= 25f;
            
            // Deduct for inactive managers
            var inactiveManagers = metrics.RegisteredManagers - metrics.ActiveManagers;
            score -= inactiveManagers * 2f;
            
            return Math.Max(0f, Math.Min(100f, score));
        }
        
        /// <summary>
        /// Finds critical issues that need immediate attention
        /// </summary>
        private List<string> FindCriticalIssues(ManagerValidationResult validation)
        {
            var issues = new List<string>();
            
            if (validation.HasCircularDependencies)
                issues.Add($"Circular dependency detected: {validation.CircularDependencyError}");
            
            foreach (var missing in validation.MissingDependencies)
            {
                issues.Add($"Missing dependency: {missing}");
            }
            
            return issues;
        }
        
        /// <summary>
        /// Finds warnings that should be addressed
        /// </summary>
        private List<string> FindWarnings(ManagerRegistryMetrics metrics, ManagerValidationResult validation)
        {
            var warnings = new List<string>(validation.Warnings);
            
            var inactiveCount = metrics.RegisteredManagers - metrics.ActiveManagers;
            if (inactiveCount > 0)
            {
                warnings.Add($"{inactiveCount} registered managers are not active");
            }
            
            if (metrics.DependencyCount > metrics.RegisteredManagers * 2)
            {
                warnings.Add("High dependency density detected - consider decoupling");
            }
            
            return warnings;
        }
        
        /// <summary>
        /// Generates recommendations for system improvement
        /// </summary>
        private List<string> GenerateRecommendations(ManagerRegistryMetrics metrics, ManagerValidationResult validation)
        {
            var recommendations = new List<string>();
            
            if (metrics.DependencyCount == 0)
            {
                recommendations.Add("Consider adding dependencies to improve system organization");
            }
            else if (metrics.DependencyCount > 20)
            {
                recommendations.Add("High dependency count - consider using event-driven patterns");
            }
            
            if (!metrics.IsInitialized)
            {
                recommendations.Add("Initialize the manager registry to activate all systems");
            }
            
            if (validation.Warnings.Count > 5)
            {
                recommendations.Add("Address validation warnings to improve system stability");
            }
            
            return recommendations;
        }
    }
    
    /// <summary>
    /// Comprehensive health report for the manager system
    /// </summary>
    public class ManagerHealthReport
    {
        public ManagerHealthStatus OverallHealth { get; set; }
        public float HealthScore { get; set; }
        public Dictionary<Type, ManagerStatus> ManagerStatuses { get; set; } = new Dictionary<Type, ManagerStatus>();
        public ManagerValidationResult ValidationResult { get; set; }
        public ManagerRegistryMetrics Metrics { get; set; }
        public List<string> CriticalIssues { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        
        public string GetSummary()
        {
            return $"Manager Health Report:\n" +
                   $"Status: {OverallHealth} (Score: {HealthScore:F1}/100)\n" +
                   $"Critical Issues: {CriticalIssues.Count}\n" +
                   $"Warnings: {Warnings.Count}\n" +
                   $"Recommendations: {Recommendations.Count}";
        }
    }
    
    /// <summary>
    /// Manager system health status
    /// </summary>
    public enum ManagerHealthStatus
    {
        Healthy,
        Warning,
        Critical
    }
}