using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.DI.Validation
{
    /// <summary>
    /// Service Container Validator
    /// Validates service registrations, dependencies, and configurations
    /// Part of Phase 0 - Service Validation implementation
    /// </summary>
    public class ServiceContainerValidator
    {
        private readonly ServiceContainer _container;
        private readonly bool _enableLogging;
        private ValidationResults _lastValidationResults;

        public ServiceContainerValidator(ServiceContainer container, bool enableLogging = true)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _enableLogging = enableLogging;
        }

        public ValidationResults LastValidationResults => _lastValidationResults;

        /// <summary>
        /// Perform comprehensive validation of the service container
        /// </summary>
        public ValidationResults Validate()
        {
            var results = new ValidationResults
            {
                ValidationTime = DateTime.Now,
                ValidatorVersion = "1.0.0"
            };

            try
            {
                // 1. Validate service registrations
                ValidateServiceRegistrations(results);

                // 2. Validate dependencies
                ValidateDependencies(results);

                // 3. Validate circular dependencies
                ValidateCircularDependencies(results);

                // 4. Validate singleton integrity
                ValidateSingletonIntegrity(results);

                // 5. Validate interface implementations
                ValidateInterfaceImplementations(results);

                // 6. Validate lifecycle management
                ValidateLifecycleManagement(results);

                // Determine overall status
                results.IsValid = results.Errors.Count == 0 && results.CriticalWarnings.Count == 0;
                results.ValidationStatus = DetermineValidationStatus(results);

                _lastValidationResults = results;

                if (_enableLogging)
                {
                    LogValidationResults(results);
                }
            }
            catch (Exception ex)
            {
                results.IsValid = false;
                results.ValidationStatus = ValidationStatus.Failed;
                results.Errors.Add(new ValidationError
                {
                    Severity = ErrorSeverity.Critical,
                    ErrorType = "ValidationException",
                    Message = $"Validation process failed: {ex.Message}",
                    StackTrace = ex.StackTrace
                });

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("VALIDATION", $"Service validation failed: {ex.Message}", null);
                }
            }

            return results;
        }

        private void ValidateServiceRegistrations(ValidationResults results)
        {
            var registrations = _container.GetAllRegistrations();
            results.TotalServicesRegistered = registrations.Count;

            if (registrations.Count == 0)
            {
                results.Warnings.Add(new ValidationWarning
                {
                    Severity = WarningSeverity.High,
                    WarningType = "NoServicesRegistered",
                    Message = "No services are registered in the container",
                    Suggestion = "Ensure services are registered during initialization"
                });
            }

            foreach (var registration in registrations)
            {
                // Validate registration has valid type
                if (registration.ServiceType == null)
                {
                    results.Errors.Add(new ValidationError
                    {
                        Severity = ErrorSeverity.Critical,
                        ErrorType = "InvalidServiceType",
                        Message = "Service registration has null ServiceType",
                        Context = $"Implementation: {registration.ImplementationType?.Name ?? "null"}"
                    });
                    continue;
                }

                // Validate implementation type
                if (registration.ImplementationType == null && registration.Instance == null && registration.Factory == null)
                {
                    results.Errors.Add(new ValidationError
                    {
                        Severity = ErrorSeverity.Critical,
                        ErrorType = "InvalidImplementation",
                        Message = $"Service {registration.ServiceType.Name} has no valid implementation",
                        Context = "No ImplementationType, Instance, or Factory provided"
                    });
                }

                // Validate implementation is assignable to service type
                if (registration.ImplementationType != null && 
                    !registration.ServiceType.IsAssignableFrom(registration.ImplementationType))
                {
                    results.Errors.Add(new ValidationError
                    {
                        Severity = ErrorSeverity.Critical,
                        ErrorType = "TypeMismatch",
                        Message = $"{registration.ImplementationType.Name} is not assignable to {registration.ServiceType.Name}",
                        Context = $"Service: {registration.ServiceType.FullName}"
                    });
                }

                results.ValidatedServices++;
            }
        }

        private void ValidateDependencies(ValidationResults results)
        {
            var registrations = _container.GetAllRegistrations();

            foreach (var registration in registrations)
            {
                if (registration.ImplementationType == null) continue;

                // Get constructor dependencies
                var constructors = registration.ImplementationType.GetConstructors();
                if (constructors.Length == 0)
                {
                    results.Warnings.Add(new ValidationWarning
                    {
                        Severity = WarningSeverity.Low,
                        WarningType = "NoPublicConstructor",
                        Message = $"{registration.ImplementationType.Name} has no public constructors",
                        Suggestion = "Consider adding a public constructor for dependency injection"
                    });
                    continue;
                }

                var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
                var parameters = constructor.GetParameters();

                foreach (var param in parameters)
                {
                    if (!_container.IsRegistered(param.ParameterType))
                    {
                        results.Errors.Add(new ValidationError
                        {
                            Severity = ErrorSeverity.High,
                            ErrorType = "MissingDependency",
                            Message = $"{registration.ServiceType.Name} requires {param.ParameterType.Name} which is not registered",
                            Context = $"Constructor parameter: {param.Name}"
                        });
                        results.MissingDependencies.Add(param.ParameterType);
                    }
                }
            }

            results.DependenciesValidated = true;
        }

        private void ValidateCircularDependencies(ValidationResults results)
        {
            var registrations = _container.GetAllRegistrations();
            var dependencyGraph = BuildDependencyGraph(registrations);

            foreach (var serviceType in dependencyGraph.Keys)
            {
                var visited = new HashSet<Type>();
                var recursionStack = new HashSet<Type>();

                if (HasCircularDependency(serviceType, dependencyGraph, visited, recursionStack, out var cycle))
                {
                    results.CircularDependencies.Add(cycle);
                    results.Errors.Add(new ValidationError
                    {
                        Severity = ErrorSeverity.Critical,
                        ErrorType = "CircularDependency",
                        Message = $"Circular dependency detected: {string.Join(" → ", cycle.Select(t => t.Name))}",
                        Context = "This will cause infinite recursion during resolution"
                    });
                }
            }

            results.CircularDependenciesChecked = true;
        }

        private void ValidateSingletonIntegrity(ValidationResults results)
        {
            var registrations = _container.GetAllRegistrations();

            foreach (var registration in registrations)
            {
                if (registration.Lifetime == ServiceLifetime.Singleton)
                {
                    results.SingletonsFound++;

                    // Validate singleton has instance or can be created
                    if (registration.Instance == null && registration.Factory == null && registration.ImplementationType == null)
                    {
                        results.Errors.Add(new ValidationError
                        {
                            Severity = ErrorSeverity.High,
                            ErrorType = "InvalidSingleton",
                            Message = $"Singleton {registration.ServiceType.Name} cannot be instantiated",
                            Context = "No Instance, Factory, or ImplementationType provided"
                        });
                    }

                    // Warn if singleton depends on transient services
                    if (registration.ImplementationType != null)
                    {
                        var dependencies = GetDependencies(registration.ImplementationType);
                        foreach (var dependency in dependencies)
                        {
                            if (_container.IsRegistered(dependency))
                            {
                                var depRegistration = _container.GetRegistration(dependency);
                                if (depRegistration.Lifetime == ServiceLifetime.Transient)
                                {
                                    results.Warnings.Add(new ValidationWarning
                                    {
                                        Severity = WarningSeverity.Medium,
                                        WarningType = "SingletonDependsOnTransient",
                                        Message = $"Singleton {registration.ServiceType.Name} depends on Transient {dependency.Name}",
                                        Suggestion = "Consider changing dependency to Singleton or Scoped to avoid unexpected behavior"
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ValidateInterfaceImplementations(ValidationResults results)
        {
            var registrations = _container.GetAllRegistrations();

            foreach (var registration in registrations)
            {
                if (registration.ServiceType.IsInterface)
                {
                    if (registration.ImplementationType != null)
                    {
                        // Validate implementation implements all interface methods
                        var interfaceMethods = registration.ServiceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        var implementationMethods = registration.ImplementationType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                        foreach (var interfaceMethod in interfaceMethods)
                        {
                            var implemented = implementationMethods.Any(m =>
                                m.Name == interfaceMethod.Name &&
                                m.ReturnType == interfaceMethod.ReturnType &&
                                m.GetParameters().Select(p => p.ParameterType).SequenceEqual(interfaceMethod.GetParameters().Select(p => p.ParameterType)));

                            if (!implemented)
                            {
                                results.Warnings.Add(new ValidationWarning
                                {
                                    Severity = WarningSeverity.Medium,
                                    WarningType = "MissingInterfaceImplementation",
                                    Message = $"{registration.ImplementationType.Name} may not properly implement {interfaceMethod.Name}",
                                    Suggestion = "Verify interface implementation is correct"
                                });
                            }
                        }
                    }
                }
            }
        }

        private void ValidateLifecycleManagement(ValidationResults results)
        {
            var registrations = _container.GetAllRegistrations();

            var lifecycleCounts = new Dictionary<ServiceLifetime, int>
            {
                { ServiceLifetime.Singleton, 0 },
                { ServiceLifetime.Transient, 0 },
                { ServiceLifetime.Scoped, 0 }
            };

            foreach (var registration in registrations)
            {
                lifecycleCounts[registration.Lifetime]++;
            }

            results.SingletonsFound = lifecycleCounts[ServiceLifetime.Singleton];
            results.TransientsFound = lifecycleCounts[ServiceLifetime.Transient];
            results.ScopedServicesFound = lifecycleCounts[ServiceLifetime.Scoped];

            // Warn if too many singletons (potential memory issues)
            if (results.SingletonsFound > 50)
            {
                results.Warnings.Add(new ValidationWarning
                {
                    Severity = WarningSeverity.Medium,
                    WarningType = "HighSingletonCount",
                    Message = $"High number of singleton services registered ({results.SingletonsFound})",
                    Suggestion = "Consider using Scoped lifetime for some services to reduce memory footprint"
                });
            }
        }

        private Dictionary<Type, List<Type>> BuildDependencyGraph(List<ServiceRegistration> registrations)
        {
            var graph = new Dictionary<Type, List<Type>>();

            foreach (var registration in registrations)
            {
                if (registration.ImplementationType == null) continue;

                var dependencies = GetDependencies(registration.ImplementationType);
                graph[registration.ServiceType] = dependencies.ToList();
            }

            return graph;
        }

        private IEnumerable<Type> GetDependencies(Type type)
        {
            var constructors = type.GetConstructors();
            if (constructors.Length == 0) return Enumerable.Empty<Type>();

            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            return constructor.GetParameters().Select(p => p.ParameterType);
        }

        private bool HasCircularDependency(Type serviceType, Dictionary<Type, List<Type>> graph,
            HashSet<Type> visited, HashSet<Type> recursionStack, out List<Type> cycle)
        {
            cycle = new List<Type>();

            if (!graph.ContainsKey(serviceType))
                return false;

            visited.Add(serviceType);
            recursionStack.Add(serviceType);

            foreach (var dependency in graph[serviceType])
            {
                if (!visited.Contains(dependency))
                {
                    if (HasCircularDependency(dependency, graph, visited, recursionStack, out cycle))
                    {
                        cycle.Insert(0, serviceType);
                        return true;
                    }
                }
                else if (recursionStack.Contains(dependency))
                {
                    cycle.Add(serviceType);
                    cycle.Add(dependency);
                    return true;
                }
            }

            recursionStack.Remove(serviceType);
            return false;
        }

        private ValidationStatus DetermineValidationStatus(ValidationResults results)
        {
            if (results.Errors.Count > 0)
                return ValidationStatus.Failed;

            if (results.CriticalWarnings.Count > 0)
                return ValidationStatus.Warning;

            if (results.Warnings.Count > 5)
                return ValidationStatus.Warning;

            return ValidationStatus.Passed;
        }

        private void LogValidationResults(ValidationResults results)
        {
            var status = results.IsValid ? "PASSED" : "FAILED";
            ChimeraLogger.LogInfo("VALIDATION", $"Service Container Validation: {status}", null);
            ChimeraLogger.LogInfo("VALIDATION", $"  Services Registered: {results.TotalServicesRegistered}", null);
            ChimeraLogger.LogInfo("VALIDATION", $"  Services Validated: {results.ValidatedServices}", null);
            ChimeraLogger.LogInfo("VALIDATION", $"  Errors: {results.Errors.Count}", null);
            ChimeraLogger.LogInfo("VALIDATION", $"  Warnings: {results.Warnings.Count}", null);
            ChimeraLogger.LogInfo("VALIDATION", $"  Singletons: {results.SingletonsFound}", null);
            ChimeraLogger.LogInfo("VALIDATION", $"  Transients: {results.TransientsFound}", null);

            if (results.Errors.Count > 0)
            {
                ChimeraLogger.LogError("VALIDATION", "=== VALIDATION ERRORS ===", null);
                foreach (var error in results.Errors)
                {
                    ChimeraLogger.LogError("VALIDATION", $"  [{error.ErrorType}] {error.Message}", null);
                    if (!string.IsNullOrEmpty(error.Context))
                    {
                        ChimeraLogger.LogError("VALIDATION", $"    Context: {error.Context}", null);
                    }
                }
            }

            if (results.Warnings.Count > 0 && _enableLogging)
            {
                ChimeraLogger.LogWarning("VALIDATION", "=== VALIDATION WARNINGS ===");
                foreach (var warning in results.Warnings.Take(5))
                {
                    ChimeraLogger.LogWarning("VALIDATION", $"  [{warning.WarningType}] {warning.Message}");
                    if (!string.IsNullOrEmpty(warning.Suggestion))
                    {
                        ChimeraLogger.LogWarning("VALIDATION", $"    Suggestion: {warning.Suggestion}");
                    }
                }

                if (results.Warnings.Count > 5)
                {
                    ChimeraLogger.LogWarning("VALIDATION", $"  ... and {results.Warnings.Count - 5} more warnings");
                }
            }
        }
    }
}

