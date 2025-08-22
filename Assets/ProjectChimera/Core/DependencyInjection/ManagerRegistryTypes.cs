using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Registration information for a manager
    /// </summary>
    public class ManagerRegistration
    {
        public Type ManagerType { get; set; }
        public int Priority { get; set; }
        public List<Type> Dependencies { get; set; } = new List<Type>();
        public float RegistrationTime { get; set; }
        public ChimeraManager Instance { get; set; }
        public bool IsLateRegistration { get; set; }
        public string RegistrationContext { get; set; }
    }
    
    /// <summary>
    /// Status information for a manager
    /// </summary>
    public class ManagerStatus
    {
        public bool IsRegistered { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public int DependencyCount { get; set; }
        public float RegistrationTime { get; set; }
        public string Status => IsActive ? "Active" : (IsRegistered ? "Registered" : "Unknown");
    }
    
    /// <summary>
    /// Result of manager dependency validation
    /// </summary>
    public class ManagerValidationResult
    {
        public bool IsValid { get; set; } = true;
        public int TotalManagers { get; set; }
        public int ValidatedManagers { get; set; }
        public List<string> MissingDependencies { get; set; } = new List<string>();
        public bool HasCircularDependencies { get; set; }
        public string CircularDependencyError { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        
        public string GetSummary()
        {
            return $"Validation: {(IsValid ? "PASSED" : "FAILED")} | " +
                   $"Managers: {ValidatedManagers}/{TotalManagers} | " +
                   $"Missing: {MissingDependencies.Count} | " +
                   $"Circular: {(HasCircularDependencies ? "YES" : "NO")}";
        }
    }
    
    /// <summary>
    /// Comprehensive metrics for the manager registry
    /// </summary>
    public class ManagerRegistryMetrics
    {
        public int RegisteredManagers { get; set; }
        public int ActiveManagers { get; set; }
        public bool IsInitialized { get; set; }
        public List<Type> InitializationOrder { get; set; } = new List<Type>();
        public int DependencyCount { get; set; }
        public ManagerValidationResult ValidationResult { get; set; }
        
        public string GetSummary()
        {
            return $"ManagerRegistry Metrics:\n" +
                   $"Managers: {ActiveManagers}/{RegisteredManagers} active\n" +
                   $"Initialized: {IsInitialized}\n" +
                   $"Dependencies: {DependencyCount}\n" +
                   $"Validation: {ValidationResult?.GetSummary() ?? "Not validated"}";
        }
    }
    
    /// <summary>
    /// Attribute for automatic manager registration with dependencies
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ManagerRegistrationAttribute : Attribute
    {
        public int Priority { get; set; } = 0;
        public Type[] Dependencies { get; set; } = new Type[0];
        public bool AutoRegister { get; set; } = true;
        public string RegistrationContext { get; set; }
        
        public ManagerRegistrationAttribute(int priority = 0)
        {
            Priority = priority;
        }
        
        public ManagerRegistrationAttribute(params Type[] dependencies)
        {
            Dependencies = dependencies ?? new Type[0];
        }
        
        public ManagerRegistrationAttribute(int priority, params Type[] dependencies)
        {
            Priority = priority;
            Dependencies = dependencies ?? new Type[0];
        }
    }
    
    /// <summary>
    /// Interface for managers that need to be notified about dependency injection completion
    /// </summary>
    public interface IManagerDependencyAware
    {
        /// <summary>
        /// Called after all dependencies have been resolved and injected
        /// </summary>
        void OnDependenciesResolved();
        
        /// <summary>
        /// Gets the list of manager types this manager depends on
        /// </summary>
        Type[] GetDependencies();
    }
    
    /// <summary>
    /// Interface for managers that need custom initialization logic
    /// </summary>
    public interface IManagerCustomInitialization
    {
        /// <summary>
        /// Called during manager initialization phase
        /// </summary>
        void InitializeManager();
        
        /// <summary>
        /// Gets the initialization priority (higher = earlier)
        /// </summary>
        int GetInitializationPriority();
    }
    
    /// <summary>
    /// Base exception for manager registry errors
    /// </summary>
    public class ManagerRegistryException : Exception
    {
        public Type ManagerType { get; }
        
        public ManagerRegistryException(string message) : base(message) { }
        
        public ManagerRegistryException(string message, Exception innerException) : base(message, innerException) { }
        
        public ManagerRegistryException(Type managerType, string message) : base(message)
        {
            ManagerType = managerType;
        }
        
        public ManagerRegistryException(Type managerType, string message, Exception innerException) : base(message, innerException)
        {
            ManagerType = managerType;
        }
    }
    
    /// <summary>
    /// Exception thrown when circular dependencies are detected
    /// </summary>
    public class CircularDependencyException : ManagerRegistryException
    {
        public List<Type> DependencyChain { get; }
        
        public CircularDependencyException(List<Type> dependencyChain) 
            : base($"Circular dependency detected: {string.Join(" → ", dependencyChain.ConvertAll(t => t.Name))}")
        {
            DependencyChain = dependencyChain ?? new List<Type>();
        }
    }
    
    /// <summary>
    /// Exception thrown when a required dependency is missing
    /// </summary>
    public class MissingDependencyException : ManagerRegistryException
    {
        public Type DependencyType { get; }
        
        public MissingDependencyException(Type managerType, Type dependencyType) 
            : base(managerType, $"Manager {managerType.Name} depends on {dependencyType.Name} which is not registered")
        {
            DependencyType = dependencyType;
        }
    }
}