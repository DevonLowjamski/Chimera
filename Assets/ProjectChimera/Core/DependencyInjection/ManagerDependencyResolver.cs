using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Handles dependency validation, resolution, and analysis for the manager registry
    /// </summary>
    public class ManagerDependencyResolver
    {
        private readonly Dictionary<Type, ManagerRegistration> _registrations;
        private readonly Dictionary<Type, List<Type>> _dependencies;
        
        public ManagerDependencyResolver(
            Dictionary<Type, ManagerRegistration> registrations,
            Dictionary<Type, List<Type>> dependencies)
        {
            _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }
        
        /// <summary>
        /// Validates that all dependencies are registered and no circular dependencies exist
        /// </summary>
        public ManagerValidationResult ValidateDependencies()
        {
            var result = new ManagerValidationResult();
            
            // Check for missing dependencies
            foreach (var kvp in _dependencies)
            {
                var managerType = kvp.Key;
                var dependencies = kvp.Value;
                
                foreach (var dependency in dependencies)
                {
                    if (!_registrations.ContainsKey(dependency))
                    {
                        result.MissingDependencies.Add($"{managerType.Name} depends on unregistered {dependency.Name}");
                        result.IsValid = false;
                    }
                }
            }
            
            // Check for circular dependencies
            try
            {
                DetectCircularDependencies();
                result.HasCircularDependencies = false;
            }
            catch (CircularDependencyException ex)
            {
                result.CircularDependencyError = ex.Message;
                result.HasCircularDependencies = true;
                result.IsValid = false;
            }
            
            // Additional validation checks
            ValidateManagerInterfaces(result);
            ValidatePriorityConflicts(result);
            
            result.TotalManagers = _registrations.Count;
            result.ValidatedManagers = result.IsValid ? _registrations.Count : 0;
            
            return result;
        }
        
        /// <summary>
        /// Detects circular dependencies using depth-first search
        /// </summary>
        public void DetectCircularDependencies()
        {
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();
            var path = new List<Type>();
            
            foreach (var managerType in _registrations.Keys)
            {
                if (!visited.Contains(managerType))
                {
                    if (HasCircularDependency(managerType, visited, recursionStack, path))
                    {
                        throw new CircularDependencyException(new List<Type>(path));
                    }
                }
            }
        }
        
        /// <summary>
        /// Recursive helper for circular dependency detection
        /// </summary>
        private bool HasCircularDependency(Type managerType, HashSet<Type> visited, HashSet<Type> recursionStack, List<Type> path)
        {
            visited.Add(managerType);
            recursionStack.Add(managerType);
            path.Add(managerType);
            
            if (_dependencies.TryGetValue(managerType, out var deps))
            {
                foreach (var dependency in deps)
                {
                    if (!visited.Contains(dependency))
                    {
                        if (HasCircularDependency(dependency, visited, recursionStack, path))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        // Found circular dependency
                        var circleStart = path.IndexOf(dependency);
                        path.RemoveRange(0, circleStart);
                        path.Add(dependency); // Complete the circle
                        return true;
                    }
                }
            }
            
            recursionStack.Remove(managerType);
            path.Remove(managerType);
            return false;
        }
        
        /// <summary>
        /// Validates manager interface implementations
        /// </summary>
        private void ValidateManagerInterfaces(ManagerValidationResult result)
        {
            foreach (var registration in _registrations.Values)
            {
                var managerType = registration.ManagerType;
                
                // Check if manager implements IManagerDependencyAware but has dependencies
                if (typeof(IManagerDependencyAware).IsAssignableFrom(managerType))
                {
                    if (_dependencies.TryGetValue(managerType, out var deps) && deps.Count > 0)
                    {
                        // This is actually good - the manager is aware of its dependencies
                        continue;
                    }
                }
                
                // Check if manager has dependencies but doesn't implement awareness interface
                if (_dependencies.TryGetValue(managerType, out var dependencies) && dependencies.Count > 0)
                {
                    if (!typeof(IManagerDependencyAware).IsAssignableFrom(managerType))
                    {
                        result.Warnings.Add($"{managerType.Name} has dependencies but doesn't implement IManagerDependencyAware");
                    }
                }
            }
        }
        
        /// <summary>
        /// Validates priority conflicts and suggests optimizations
        /// </summary>
        private void ValidatePriorityConflicts(ManagerValidationResult result)
        {
            var priorityGroups = _registrations.Values
                .GroupBy(r => r.Priority)
                .Where(g => g.Count() > 1)
                .ToList();
            
            foreach (var group in priorityGroups)
            {
                var managers = group.Select(r => r.ManagerType.Name).ToList();
                result.Warnings.Add($"Multiple managers have same priority {group.Key}: {string.Join(", ", managers)}");
            }
        }
        
        /// <summary>
        /// Gets dependency graph for visualization
        /// </summary>
        public DependencyGraph GetDependencyGraph()
        {
            var graph = new DependencyGraph();
            
            foreach (var kvp in _dependencies)
            {
                var managerType = kvp.Key;
                var dependencies = kvp.Value;
                
                graph.Nodes.Add(managerType);
                
                foreach (var dependency in dependencies)
                {
                    graph.Nodes.Add(dependency);
                    graph.Edges.Add(new DependencyEdge
                    {
                        From = managerType,
                        To = dependency,
                        IsRequired = true
                    });
                }
            }
            
            // Add isolated nodes (managers with no dependencies)
            foreach (var managerType in _registrations.Keys)
            {
                if (!_dependencies.ContainsKey(managerType))
                {
                    graph.Nodes.Add(managerType);
                }
            }
            
            return graph;
        }
        
        /// <summary>
        /// Analyzes dependency complexity and suggests optimizations
        /// </summary>
        public DependencyAnalysis AnalyzeDependencies()
        {
            var analyzer = new DependencyAnalyzer(_registrations, _dependencies);
            return analyzer.PerformAnalysis();
        }
    }
    
    /// <summary>
    /// Represents a dependency graph for visualization
    /// </summary>
    public class DependencyGraph
    {
        public HashSet<Type> Nodes { get; set; } = new HashSet<Type>();
        public List<DependencyEdge> Edges { get; set; } = new List<DependencyEdge>();
    }
    
    /// <summary>
    /// Represents an edge in the dependency graph
    /// </summary>
    public class DependencyEdge
    {
        public Type From { get; set; }
        public Type To { get; set; }
        public bool IsRequired { get; set; } = true;
    }
    
    /// <summary>
    /// Analysis of dependency complexity
    /// </summary>
    public class DependencyAnalysis
    {
        public int TotalDependencies { get; set; }
        public int MaxDependenciesPerManager { get; set; }
        public float AvgDependenciesPerManager { get; set; }
        public Dictionary<Type, int> MostDependentManagers { get; set; } = new Dictionary<Type, int>();
        public List<Type> LongestDependencyChain { get; set; } = new List<Type>();
        public DependencyComplexity ComplexityRating { get; set; }
        
        public string GetSummary()
        {
            return $"Dependency Analysis:\n" +
                   $"Total: {TotalDependencies}, Max per manager: {MaxDependenciesPerManager}\n" +
                   $"Average: {AvgDependenciesPerManager:F1}, Complexity: {ComplexityRating}\n" +
                   $"Longest chain: {LongestDependencyChain.Count} managers";
        }
    }
    
    /// <summary>
    /// Dependency complexity levels
    /// </summary>
    public enum DependencyComplexity
    {
        Low,
        Medium,
        High
    }
}