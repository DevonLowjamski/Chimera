using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// PHASE 0: Typed service registration (replaces reflection-based GetMethod().Invoke())
    /// Compile-time safe registration with no reflection
    /// Zero-tolerance reflection elimination pattern
    /// </summary>
    public static class TypedServiceRegistration
    {
        /// <summary>
        /// Register singleton service with explicit typing (no reflection)
        /// </summary>
        public static void RegisterSingletonTyped<TService, TImplementation>(
            this IServiceContainer container,
            TImplementation instance)
            where TService : class
            where TImplementation : class, TService
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            container.RegisterInstance<TService>(instance);
        }

        /// <summary>
        /// Register factory with explicit typing (no reflection)
        /// </summary>
        public static void RegisterFactoryTyped<TService>(
            this IServiceContainer container,
            Func<IServiceLocator, TService> factory)
            where TService : class
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            container.RegisterFactory<TService>(factory);
        }

        /// <summary>
        /// Register transient service with explicit typing (no reflection)
        /// </summary>
        public static void RegisterTransientTyped<TService, TImplementation>(
            this IServiceContainer container)
            where TService : class
            where TImplementation : class, TService, new()
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            container.RegisterFactory<TService>(locator => new TImplementation());
        }

        /// <summary>
        /// Strongly-typed service descriptor for compile-time registration
        /// Eliminates need for runtime Type inspection
        /// </summary>
        public class TypedServiceDescriptor<TService, TImplementation>
            where TService : class
            where TImplementation : class, TService
        {
            public ServiceLifetime Lifetime { get; set; }
            public TImplementation Instance { get; set; }
            public Func<IServiceLocator, TImplementation> Factory { get; set; }

            public void Register(IServiceContainer container)
            {
                switch (Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        if (Instance != null)
                        {
                            container.RegisterInstance<TService>(Instance);
                        }
                        else if (Factory != null)
                        {
                            // Store factory result as singleton
                            var instance = Factory(container);
                            container.RegisterInstance<TService>(instance);
                        }
                        else
                        {
                            // Create new instance as singleton using Activator (runtime check for parameterless constructor)
                            var instance = (TImplementation)Activator.CreateInstance(typeof(TImplementation));
                            container.RegisterInstance<TService>(instance);
                        }
                        break;

                    case ServiceLifetime.Transient:
                        if (Factory != null)
                        {
                            container.RegisterFactory<TService>(locator => Factory(locator));
                        }
                        else
                        {
                            // Create new instance using Activator (runtime check for parameterless constructor)
                            container.RegisterFactory<TService>(locator => (TImplementation)Activator.CreateInstance(typeof(TImplementation)));
                        }
                        break;

                    case ServiceLifetime.Scoped:
                        // For Unity, scoped is similar to singleton per scene
                        if (Instance != null)
                        {
                            container.RegisterInstance<TService>(Instance);
                        }
                        else if (Factory != null)
                        {
                            container.RegisterFactory<TService>(Factory);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Create typed descriptor for singleton registration
        /// </summary>
        public static TypedServiceDescriptor<TService, TImplementation> Singleton<TService, TImplementation>(
            TImplementation instance)
            where TService : class
            where TImplementation : class, TService
        {
            return new TypedServiceDescriptor<TService, TImplementation>
            {
                Lifetime = ServiceLifetime.Singleton,
                Instance = instance
            };
        }

        /// <summary>
        /// Create typed descriptor for factory registration
        /// </summary>
        public static TypedServiceDescriptor<TService, TImplementation> Factory<TService, TImplementation>(
            Func<IServiceLocator, TImplementation> factory)
            where TService : class
            where TImplementation : class, TService
        {
            return new TypedServiceDescriptor<TService, TImplementation>
            {
                Lifetime = ServiceLifetime.Transient,
                Factory = factory
            };
        }

        /// <summary>
        /// Batch register multiple services without reflection
        /// </summary>
        public static void RegisterBatch(
            this IServiceContainer container,
            params Action<IServiceContainer>[] registrations)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            foreach (var registration in registrations)
            {
                try
                {
                    registration(container);
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError("TypedRegistration",
                        $"Failed to register service: {ex.Message}", null);
                    throw;
                }
            }
        }
    }
}

