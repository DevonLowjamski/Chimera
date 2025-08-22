using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Analyzes dependency complexity and provides optimization suggestions
    /// </summary>
    public class DependencyAnalyzer
    {
        private readonly Dictionary<Type, ManagerRegistration> _registrations;
        private readonly Dictionary<Type, List<Type>> _dependencies;
        
        public DependencyAnalyzer(
            Dictionary<Type, ManagerRegistration> registrations,
            Dictionary<Type, List<Type>> dependencies)
        {
            _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }
        
        /// <summary>
        /// Performs comprehensive dependency analysis
        /// </summary>
        public DependencyAnalysis PerformAnalysis()
        {
            var analysis = new DependencyAnalysis();
            
            // Calculate basic metrics
            CalculateBasicMetrics(analysis);
            
            // Find problem areas
            FindMostDependentManagers(analysis);
            FindLongestDependencyChain(analysis);
            
            // Rate complexity
            RateComplexity(analysis);
            
            return analysis;
        }
        
        /// <summary>
        /// Calculates basic dependency metrics
        /// </summary>
        private void CalculateBasicMetrics(DependencyAnalysis analysis)
        {
            analysis.TotalDependencies = _dependencies.Values.Sum(deps => deps.Count);
            analysis.MaxDependenciesPerManager = _dependencies.Values.Count > 0 ? _dependencies.Values.Max(deps => deps.Count) : 0;
            analysis.AvgDependenciesPerManager = _registrations.Count > 0 ? (float)analysis.TotalDependencies / _registrations.Count : 0;
        }
        
        /// <summary>
        /// Finds managers with the most dependencies
        /// </summary>
        private void FindMostDependentManagers(DependencyAnalysis analysis)
        {
            analysis.MostDependentManagers = _dependencies
                .Where(kvp => kvp.Value.Count > 0)
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(5)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }
        
        /// <summary>
        /// Finds the longest dependency chain in the system
        /// </summary>
        private void FindLongestDependencyChain(DependencyAnalysis analysis)
        {
            var longestChain = new List<Type>();
            
            foreach (var managerType in _registrations.Keys)
            {
                var chain = new List<Type>();
                var visited = new HashSet<Type>();
                
                BuildDependencyChain(managerType, chain, visited);
                
                if (chain.Count > longestChain.Count)
                {
                    longestChain = new List<Type>(chain);
                }
            }
            
            analysis.LongestDependencyChain = longestChain;
        }
        
        /// <summary>
        /// Recursively builds dependency chain for analysis
        /// </summary>
        private void BuildDependencyChain(Type managerType, List<Type> chain, HashSet<Type> visited)
        {
            if (visited.Contains(managerType))
                return;
            
            visited.Add(managerType);
            chain.Add(managerType);
            
            if (_dependencies.TryGetValue(managerType, out var deps))
            {
                foreach (var dependency in deps)
                {
                    BuildDependencyChain(dependency, chain, visited);
                }
            }
        }
        
        /// <summary>
        /// Rates the complexity of the dependency system
        /// </summary>
        private void RateComplexity(DependencyAnalysis analysis)
        {
            if (analysis.MaxDependenciesPerManager <= 3 && analysis.TotalDependencies <= 10)
                analysis.ComplexityRating = DependencyComplexity.Low;
            else if (analysis.MaxDependenciesPerManager <= 6 && analysis.TotalDependencies <= 25)
                analysis.ComplexityRating = DependencyComplexity.Medium;
            else
                analysis.ComplexityRating = DependencyComplexity.High;
        }
        
        /// <summary>
        /// Suggests optimizations based on analysis
        /// </summary>
        public List<string> SuggestOptimizations(DependencyAnalysis analysis)
        {
            var suggestions = new List<string>();
            
            if (analysis.MaxDependenciesPerManager > 5)
            {
                suggestions.Add($"Consider splitting managers with high dependency counts (max: {analysis.MaxDependenciesPerManager})");
            }
            
            if (analysis.LongestDependencyChain.Count > 4)
            {
                suggestions.Add($"Long dependency chain detected ({analysis.LongestDependencyChain.Count} levels) - consider flattening");
            }
            
            if (analysis.ComplexityRating == DependencyComplexity.High)
            {
                suggestions.Add("High complexity system - consider using event-driven patterns to reduce coupling");
            }
            
            if (analysis.TotalDependencies > analysis.MostDependentManagers.Count * 2)
            {
                suggestions.Add("Consider using mediator pattern for cross-cutting concerns");
            }
            
            return suggestions;
        }
    }
}