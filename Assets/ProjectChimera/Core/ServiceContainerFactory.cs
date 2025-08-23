using System;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Factory for creating and managing the global ServiceContainer instance.
    /// Provides unified dependency injection throughout Project Chimera.
    /// </summary>
    public static class ServiceContainerFactory
    {
        private static IServiceContainer _globalInstance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the global ServiceContainer instance (creates if not exists).
        /// This replaces the old ServiceLocator singleton pattern.
        /// </summary>
        public static IServiceContainer Instance
        {
            get
            {
                if (_globalInstance == null)
                {
                    lock (_lock)
                    {
                        if (_globalInstance == null)
                        {
                            _globalInstance = new ServiceContainer();
                            RegisterCoreServices();
                        }
                    }
                }
                return _globalInstance;
            }
        }

        /// <summary>
        /// Creates a new ServiceContainer for development/testing purposes
        /// </summary>
        public static IServiceContainer CreateForDevelopment()
        {
            return new ServiceContainer();
        }

        /// <summary>
        /// Creates a child container for scoped operations
        /// </summary>
        public static IServiceContainer CreateChildContainer()
        {
            return Instance.CreateChildContainer();
        }

        /// <summary>
        /// Resets the global instance (for testing purposes)
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _globalInstance?.Dispose();
                _globalInstance = null;
            }
        }

        private static void RegisterCoreServices()
        {
            // Register the container itself as IServiceContainer
            _globalInstance.RegisterSingleton<IServiceContainer>(_globalInstance);
        }
    }
}
