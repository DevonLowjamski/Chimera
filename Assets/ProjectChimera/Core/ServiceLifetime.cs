namespace ProjectChimera.Core
{
    /// <summary>
    /// Service lifetime enumeration for dependency injection
    /// Defines the lifecycle scope of registered services
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// Single instance shared across entire application lifetime
        /// </summary>
        Singleton,

        /// <summary>
        /// New instance created each time service is resolved
        /// </summary>
        Transient,

        /// <summary>
        /// Single instance per scope (e.g., per request, per scene)
        /// </summary>
        Scoped
    }
}

