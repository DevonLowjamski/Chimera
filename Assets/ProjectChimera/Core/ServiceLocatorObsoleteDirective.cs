// This file contains conditional compilation directives for ServiceLocator deprecation
// Phase 1: Warning mode - ServiceLocator marked as obsolete with warnings
// Phase 2: Error mode - Uncomment CHIMERA_SERVICELOCATOR_ERROR to enforce compile errors

#if CHIMERA_SERVICELOCATOR_ERROR
    // Future phase: This will be enabled to cause compile errors for ServiceLocator usage
    // [assembly: System.ObsoleteAttribute("ServiceLocator usage detected. Use ServiceContainer instead.", false)]
#endif

// Logging directives for tracking ServiceLocator usage
#if CHIMERA_DEV_LOGS || CHIMERA_VERBOSE_INIT
    #define LOG_SERVICELOCATOR_USAGE
#endif

using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Helper class for managing ServiceLocator deprecation process
    /// </summary>
    public static class ServiceLocatorDeprecation
    {
        /// <summary>
        /// Logs usage of deprecated ServiceLocator for tracking migration progress
        /// </summary>
        public static void LogDeprecatedUsage(string context)
        {
#if LOG_SERVICELOCATOR_USAGE
            ChimeraLogger.LogWarning($"[DEPRECATED] ServiceLocator used in {context}. Migrate to ServiceContainer.");
#endif
        }

        /// <summary>
        /// Checks if ServiceLocator should cause compilation errors
        /// </summary>
        public static bool IsErrorModeEnabled
        {
            get
            {
#if CHIMERA_SERVICELOCATOR_ERROR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
