using System;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Interface for service modules that configure dependency injection
    /// Allows modular registration of related services for different game systems
    /// </summary>
    public interface IServiceModule
    {
        /// <summary>
        /// Module name for identification and debugging
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// Module version for compatibility tracking
        /// </summary>
        Version ModuleVersion { get; }

        /// <summary>
        /// Dependencies this module requires (other module names)
        /// </summary>
        string[] Dependencies { get; }

        /// <summary>
        /// Configure services for this module
        /// </summary>
        void ConfigureServices(IServiceLocator serviceLocator);

        /// <summary>
        /// Initialize the module after all services are registered
        /// </summary>
        void Initialize(IServiceLocator serviceLocator);

        /// <summary>
        /// Shutdown the module during cleanup
        /// </summary>
        void Shutdown(IServiceLocator serviceLocator);

        /// <summary>
        /// Validate that all required services are available
        /// </summary>
        bool ValidateServices(IServiceLocator serviceLocator);
    }

    /// <summary>
    /// Base implementation of service module with common functionality
    /// </summary>
    public abstract class ServiceModuleBase : IServiceModule
    {
        public abstract string ModuleName { get; }
        public virtual Version ModuleVersion => new Version(1, 0, 0);
        public virtual string[] Dependencies => new string[0];

        public abstract void ConfigureServices(IServiceLocator serviceLocator);

        public virtual void Initialize(IServiceLocator serviceLocator)
        {
            // Override in derived classes if initialization is needed
        }

        public virtual void Shutdown(IServiceLocator serviceLocator)
        {
            // Override in derived classes if cleanup is needed
        }

        public virtual bool ValidateServices(IServiceLocator serviceLocator)
        {
            // Override in derived classes for custom validation
            return true;
        }

        protected void LogModuleAction(string action)
        {
            UnityEngine.Debug.Log($"[ServiceModule:{ModuleName}] {action}");
        }
    }

    /// <summary>
    /// Module registration information
    /// </summary>
    public class ModuleRegistration
    {
        public IServiceModule Module { get; set; }
        public DateTime RegistrationTime { get; set; } = DateTime.Now;
        public bool IsInitialized { get; set; } = false;
        public bool IsValidated { get; set; } = false;
    }
}