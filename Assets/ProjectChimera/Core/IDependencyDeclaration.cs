using System;
using System.Collections.Generic;

namespace ProjectChimera.Core
{
    /// <summary>
    /// PHASE 0: Interface for managers to explicitly declare their dependencies
    /// Replaces reflection-based dependency scanning with compile-time safe declarations
    /// Zero-tolerance reflection elimination pattern
    /// </summary>
    public interface IDependencyDeclaration
    {
        /// <summary>
        /// Declare required dependencies for this manager
        /// Returns types of managers this manager depends on
        /// </summary>
        IEnumerable<Type> GetRequiredDependencies();

        /// <summary>
        /// Declare optional dependencies for this manager
        /// Returns types of managers this manager can use but doesn't require
        /// </summary>
        IEnumerable<Type> GetOptionalDependencies() => Array.Empty<Type>();
    }

    /// <summary>
    /// Helper to validate dependencies without reflection
    /// </summary>
    public static class DependencyValidator
    {
        /// <summary>
        /// Build dependency map from explicit declarations (no reflection)
        /// </summary>
        public static Dictionary<Type, List<Type>> BuildDependencyMap(IEnumerable<ChimeraManager> managers)
        {
            var dependencyMap = new Dictionary<Type, List<Type>>();

            foreach (var manager in managers)
            {
                var managerType = manager.GetType();
                var dependencies = new List<Type>();

                // PHASE 0: Use interface pattern instead of reflection
                if (manager is IDependencyDeclaration declaringManager)
                {
                    dependencies.AddRange(declaringManager.GetRequiredDependencies());
                }

                dependencyMap[managerType] = dependencies;
            }

            return dependencyMap;
        }

        /// <summary>
        /// Find missing dependencies without reflection
        /// </summary>
        public static List<string> FindMissingDependencies(IEnumerable<ChimeraManager> managers)
        {
            var missingDependencies = new List<string>();
            var availableTypes = new HashSet<Type>();

            // Build set of available types
            foreach (var manager in managers)
            {
                availableTypes.Add(manager.GetType());
            }

            // Check declared dependencies
            foreach (var manager in managers)
            {
                if (manager is IDependencyDeclaration declaringManager)
                {
                    var managerType = manager.GetType();

                    foreach (var requiredType in declaringManager.GetRequiredDependencies())
                    {
                        if (!availableTypes.Contains(requiredType))
                        {
                            missingDependencies.Add($"{managerType.Name} requires {requiredType.Name}");
                        }
                    }
                }
            }

            return missingDependencies;
        }

        /// <summary>
        /// Detect circular dependencies using explicit declarations
        /// </summary>
        public static List<string> DetectCircularDependencies(Dictionary<Type, List<Type>> dependencyMap)
        {
            var circularDependencies = new List<string>();
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();

            foreach (var managerType in dependencyMap.Keys)
            {
                if (!visited.Contains(managerType))
                {
                    DetectCircularDependenciesRecursive(
                        managerType,
                        dependencyMap,
                        visited,
                        recursionStack,
                        circularDependencies);
                }
            }

            return circularDependencies;
        }

        private static void DetectCircularDependenciesRecursive(
            Type currentType,
            Dictionary<Type, List<Type>> dependencyMap,
            HashSet<Type> visited,
            HashSet<Type> recursionStack,
            List<string> circularDependencies)
        {
            visited.Add(currentType);
            recursionStack.Add(currentType);

            if (dependencyMap.TryGetValue(currentType, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        DetectCircularDependenciesRecursive(
                            dependency,
                            dependencyMap,
                            visited,
                            recursionStack,
                            circularDependencies);
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        circularDependencies.Add($"Circular dependency: {currentType.Name} -> {dependency.Name}");
                    }
                }
            }

            recursionStack.Remove(currentType);
        }
    }
}

