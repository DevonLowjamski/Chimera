using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Initialization
{
    /// <summary>
    /// REFACTORED: System Validation Service - Focused system validation and health checks
    /// Single Responsibility: Validating initialized systems and performing health checks
    /// Extracted from GameSystemInitializer for better SRP compliance
    /// </summary>
    public class SystemValidationService
    {
        private readonly bool _enableLogging;
        private readonly bool _validateDependenciesAfterInit;
        private readonly bool _attemptServiceRecovery;

        // Validation state
        private readonly Dictionary<ChimeraManager, ValidationResult> _validationResults = new Dictionary<ChimeraManager, ValidationResult>();
        private readonly List<string> _validationErrors = new List<string>();

        // Events
        public event System.Action<ChimeraManager, bool> OnManagerValidated;
        public event System.Action<ValidationSummary> OnValidationCompleted;

        public SystemValidationService(bool enableLogging = true, bool validateDependenciesAfterInit = true,
                                     bool attemptServiceRecovery = true)
        {
            _enableLogging = enableLogging;
            _validateDependenciesAfterInit = validateDependenciesAfterInit;
            _attemptServiceRecovery = attemptServiceRecovery;
        }

        #region Validation Operations

        /// <summary>
        /// Validate all initialized systems
        /// </summary>
        public ValidationSummary ValidateAllSystems(IEnumerable<ChimeraManager> managers)
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", "Starting comprehensive system validation", null);

            var startTime = DateTime.Now;
            var summary = new ValidationSummary { StartTime = startTime };
            _validationErrors.Clear();

            var managerList = managers.ToList();
            summary.TotalSystems = managerList.Count;

            // Validate each manager
            foreach (var manager in managerList)
            {
                var result = ValidateManager(manager);
                _validationResults[manager] = result;

                if (result.IsValid)
                {
                    summary.ValidSystems++;
                    OnManagerValidated?.Invoke(manager, true);
                }
                else
                {
                    summary.InvalidSystems++;
                    _validationErrors.AddRange(result.Errors);
                    OnManagerValidated?.Invoke(manager, false);
                }
            }

            // Perform dependency validation if enabled
            if (_validateDependenciesAfterInit)
            {
                var dependencyValidation = ValidateSystemDependencies(managerList);
                summary.DependencyValidationPassed = dependencyValidation.IsValid;
                if (!dependencyValidation.IsValid)
                {
                    _validationErrors.AddRange(dependencyValidation.Errors);
                }
            }

            // Perform service container validation
            var serviceValidation = ValidateServiceContainer();
            summary.ServiceContainerValid = serviceValidation.IsValid;
            if (!serviceValidation.IsValid)
            {
                _validationErrors.AddRange(serviceValidation.Errors);
            }

            summary.ValidationTime = DateTime.Now - startTime;
            summary.AllErrors = new List<string>(_validationErrors);
            summary.OverallValid = summary.InvalidSystems == 0 && summary.DependencyValidationPassed && summary.ServiceContainerValid;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("INIT",
                    $"System validation completed: {summary.ValidSystems}/{summary.TotalSystems} valid in {summary.ValidationTime.TotalMilliseconds:F1}ms",
                    null);
            }

            OnValidationCompleted?.Invoke(summary);
            return summary;
        }

        /// <summary>
        /// Validate a single manager
        /// </summary>
        private ValidationResult ValidateManager(ChimeraManager manager)
        {
            var result = new ValidationResult { Manager = manager };

            if (manager == null)
            {
                result.Errors.Add("Manager is null");
                return result;
            }

            try
            {
                var managerType = manager.GetType();

                // Basic existence check
                if (manager.gameObject == null)
                {
                    result.Errors.Add($"{managerType.Name}: GameObject is null");
                }

                // Component validation
                if (!manager.enabled)
                {
                    result.Warnings.Add($"{managerType.Name}: Component is disabled");
                }

                // Check if manager implements required interfaces
                if (manager is IValidatable validatable)
                {
                    var customValidation = validatable.Validate();
                    if (!customValidation.IsValid)
                    {
                        result.Errors.AddRange(customValidation.Errors.Select(e => $"{managerType.Name}: {e}"));
                    }
                }

                // Service registration validation
                if (ServiceContainerFactory.Instance != null)
                {
                    var isRegistered = ServiceContainerFactory.Instance.IsRegistered(managerType);
                    if (!isRegistered)
                    {
                        result.Warnings.Add($"{managerType.Name}: Not registered in ServiceContainer");
                    }
                }

                result.IsValid = result.Errors.Count == 0;

                if (_enableLogging && result.IsValid)
                    ChimeraLogger.LogInfo("INIT", $"Manager {managerType.Name} validation passed", null);
                else if (_enableLogging)
                    ChimeraLogger.LogWarning("INIT", $"Manager {managerType.Name} validation failed: {string.Join(", ", result.Errors)}", null);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Validation exception: {ex.Message}");
                ChimeraLogger.LogError("INIT", $"Exception validating {manager?.GetType()?.Name}: {ex.Message}", null);
            }

            return result;
        }

        /// <summary>
        /// Validate system dependencies
        /// </summary>
        private ValidationResult ValidateSystemDependencies(List<ChimeraManager> managers)
        {
            var result = new ValidationResult();

            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", "Validating system dependencies", null);

            try
            {
                // Check for circular dependencies (simplified check)
                var dependencyMap = BuildDependencyMap(managers);
                var circularDependencies = DetectCircularDependencies(dependencyMap);

                if (circularDependencies.Any())
                {
                    result.Errors.AddRange(circularDependencies.Select(cd => $"Circular dependency detected: {cd}"));
                }

                // Check for missing dependencies
                var missingDependencies = DetectMissingDependencies(managers);
                if (missingDependencies.Any())
                {
                    result.Errors.AddRange(missingDependencies.Select(md => $"Missing dependency: {md}"));
                }

                result.IsValid = result.Errors.Count == 0;

                if (_enableLogging)
                {
                    if (result.IsValid)
                        ChimeraLogger.LogInfo("INIT", "Dependency validation passed", null);
                    else
                        ChimeraLogger.LogWarning("INIT", $"Dependency validation failed: {string.Join(", ", result.Errors)}", null);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Dependency validation exception: {ex.Message}");
                ChimeraLogger.LogError("INIT", $"Exception during dependency validation: {ex.Message}", null);
            }

            return result;
        }

        /// <summary>
        /// Validate the service container state
        /// </summary>
        private ValidationResult ValidateServiceContainer()
        {
            var result = new ValidationResult();

            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", "Validating ServiceContainer state", null);

            try
            {
                if (ServiceContainerFactory.Instance == null)
                {
                    result.Errors.Add("ServiceContainer instance is null");
                    return result;
                }

                // Validate container integrity
                var container = ServiceContainerFactory.Instance;
                // We don't have the generic type context here; query all registrations instead
                var registrationCount = container.GetRegistrations()?.Count ?? 0;

                if (registrationCount == 0)
                {
                    result.Warnings.Add("ServiceContainer has no registered services");
                }

                // Validate that core services are registered
                var coreServiceTypes = new[] { typeof(IEventManager), typeof(ITimeManager), typeof(IDataManager) };
                foreach (var serviceType in coreServiceTypes)
                {
                    if (!container.IsRegistered(serviceType))
                    {
                        result.Warnings.Add($"Core service {serviceType.Name} is not registered");
                    }
                }

                result.IsValid = result.Errors.Count == 0;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("INIT",
                        $"ServiceContainer validation: {registrationCount} services registered, " +
                        $"{result.Errors.Count} errors, {result.Warnings.Count} warnings", null);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"ServiceContainer validation exception: {ex.Message}");
                ChimeraLogger.LogError("INIT", $"Exception validating ServiceContainer: {ex.Message}", null);
            }

            return result;
        }

        #endregion

        #region Dependency Analysis

        /// <summary>
        /// Build a dependency map for managers
        /// PHASE 0: Use interface pattern instead of reflection
        /// </summary>
        private Dictionary<Type, List<Type>> BuildDependencyMap(List<ChimeraManager> managers)
        {
            // PHASE 0: Zero-reflection dependency mapping
            return DependencyValidator.BuildDependencyMap(managers);
        }

        /// <summary>
        /// Detect circular dependencies
        /// </summary>
        private List<string> DetectCircularDependencies(Dictionary<Type, List<Type>> dependencyMap)
        {
            var circularDependencies = new List<string>();
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();

            foreach (var kvp in dependencyMap)
            {
                if (HasCircularDependency(kvp.Key, dependencyMap, visited, recursionStack))
                {
                    circularDependencies.Add($"{kvp.Key.Name} -> {string.Join(" -> ", recursionStack.Select(t => t.Name))}");
                }
            }

            return circularDependencies;
        }

        /// <summary>
        /// Check for circular dependencies using DFS
        /// </summary>
        private bool HasCircularDependency(Type type, Dictionary<Type, List<Type>> dependencyMap,
                                         HashSet<Type> visited, HashSet<Type> recursionStack)
        {
            if (recursionStack.Contains(type))
                return true;

            if (visited.Contains(type))
                return false;

            visited.Add(type);
            recursionStack.Add(type);

            if (dependencyMap.TryGetValue(type, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (HasCircularDependency(dependency, dependencyMap, visited, recursionStack))
                        return true;
                }
            }

            recursionStack.Remove(type);
            return false;
        }

        /// <summary>
        /// Detect missing dependencies
        /// PHASE 0: Use interface pattern instead of reflection
        /// </summary>
        private List<string> DetectMissingDependencies(List<ChimeraManager> managers)
        {
            // PHASE 0: Zero-reflection missing dependency detection
            return DependencyValidator.FindMissingDependencies(managers);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get validation results for all managers
        /// </summary>
        public Dictionary<ChimeraManager, ValidationResult> GetValidationResults()
        {
            return new Dictionary<ChimeraManager, ValidationResult>(_validationResults);
        }

        /// <summary>
        /// Get managers that failed validation
        /// </summary>
        public IEnumerable<ChimeraManager> GetInvalidManagers()
        {
            return _validationResults
                .Where(kvp => !kvp.Value.IsValid)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Reset validation state
        /// </summary>
        public void ResetValidationState()
        {
            _validationResults.Clear();
            _validationErrors.Clear();

            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", "Reset validation state", null);
        }

        #endregion
    }

    /// <summary>
    /// Interface for managers that support custom validation
    /// </summary>
    public interface IValidatable
    {
        ValidationResult Validate();
    }

    /// <summary>
    /// Validation result for a single component
    /// </summary>
    [System.Serializable]
    public class ValidationResult
    {
        public ChimeraManager Manager;
        public bool IsValid = false;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// Overall validation summary
    /// </summary>
    [System.Serializable]
    public struct ValidationSummary
    {
        public DateTime StartTime;
        public TimeSpan ValidationTime;
        public int TotalSystems;
        public int ValidSystems;
        public int InvalidSystems;
        public bool DependencyValidationPassed;
        public bool ServiceContainerValid;
        public bool OverallValid;
        public List<string> AllErrors;
    }
}
