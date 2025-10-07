using System;
using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// PHASE 0: Interface-based dependency injection (replaces reflection-based field injection)
    /// Components implementing this interface declare their dependencies explicitly
    /// Zero-tolerance reflection elimination pattern
    /// </summary>
    public interface IDependencyInjectable
    {
        /// <summary>
        /// Inject dependencies into this component
        /// Called by ServiceContainer after instantiation
        /// </summary>
        void InjectDependencies(IServiceLocator serviceLocator);

        /// <summary>
        /// Whether dependencies have been injected
        /// </summary>
        bool AreDependenciesInjected { get; }
    }

    /// <summary>
    /// Base implementation for MonoBehaviours requiring dependency injection
    /// Eliminates need for reflection-based field scanning
    /// </summary>
    public abstract class InjectableMonoBehaviour : MonoBehaviour, IDependencyInjectable
    {
        private bool _dependenciesInjected;

        public bool AreDependenciesInjected => _dependenciesInjected;

        /// <summary>
        /// Override this to declare and receive your dependencies
        /// </summary>
        public abstract void InjectDependencies(IServiceLocator serviceLocator);

        protected virtual void Awake()
        {
            // Auto-inject if not already injected
            if (!_dependenciesInjected && ServiceContainerFactory.Instance != null)
            {
                InjectDependencies(ServiceContainerFactory.Instance);
                _dependenciesInjected = true;
            }
        }

        /// <summary>
        /// Helper to safely resolve required dependencies
        /// </summary>
        protected T Resolve<T>() where T : class
        {
            return ServiceContainerFactory.Instance?.Resolve<T>();
        }

        /// <summary>
        /// Helper to safely try resolve optional dependencies
        /// </summary>
        protected T TryResolve<T>() where T : class
        {
            return ServiceContainerFactory.Instance?.TryResolve<T>();
        }
    }

    /// <summary>
    /// Registration helper for injectable services
    /// Replaces reflection-based registration with explicit typed registration
    /// </summary>
    public static class InjectableServiceRegistration
    {
        /// <summary>
        /// Register and inject dependencies for a component
        /// Zero-reflection pattern: Explicit dependency declaration
        /// </summary>
        public static void RegisterAndInject<T>(IServiceContainer container, T instance)
            where T : class, IDependencyInjectable
        {
            // Register the instance
            container.RegisterInstance<T>(instance);

            // Inject dependencies if it implements the interface
            if (instance is IDependencyInjectable injectable && !injectable.AreDependenciesInjected)
            {
                injectable.InjectDependencies(container);
            }
        }

        /// <summary>
        /// Inject dependencies for multiple components
        /// </summary>
        public static void InjectDependencies(IServiceLocator serviceLocator, params IDependencyInjectable[] injectables)
        {
            foreach (var injectable in injectables)
            {
                if (injectable != null && !injectable.AreDependenciesInjected)
                {
                    injectable.InjectDependencies(serviceLocator);
                }
            }
        }
    }
}

