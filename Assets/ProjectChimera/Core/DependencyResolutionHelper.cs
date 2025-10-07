using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// PHASE 0 ZERO-TOLERANCE: Strict dependency resolution utility
    /// Enforces pure ServiceContainer DI with no FindObjectOfType fallbacks
    /// All dependencies MUST be properly registered in ServiceContainer
    /// </summary>
    public static class DependencyResolutionHelper
    {
        /// <summary>
        /// Strictly resolve a dependency using ServiceContainer ONLY
        /// No fallbacks - enforces proper dependency injection
        /// </summary>
        /// <typeparam name="T">Type of component to resolve</typeparam>
        /// <param name="context">Context object for logging</param>
        /// <param name="logCategory">Category for logging messages</param>
        /// <returns>Resolved component or null if not found</returns>
        public static T SafeResolve<T>(Object context = null, string logCategory = "CORE") where T : Component
        {
            try
            {
                // STRICT: ServiceContainer ONLY - no fallbacks
                var resolved = ServiceContainerFactory.Instance?.TryResolve<T>();
                if (resolved != null)
                {
                    return resolved;
                }

                // ZERO-TOLERANCE: No FindObjectOfType fallback
                // Dependencies MUST be registered properly
                ChimeraLogger.LogError(logCategory,
                    $"DEPENDENCY NOT REGISTERED: {typeof(T).Name} must be registered in ServiceContainer. " +
                    $"FindObjectOfType fallbacks removed for Phase 0 zero-tolerance compliance.",
                    context);
                return null;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError(logCategory,
                    $"Failed to resolve {typeof(T).Name}: {ex.Message}",
                    context);
                return null;
            }
        }

        /// <summary>
        /// Strictly resolve multiple dependencies at once - ServiceContainer ONLY
        /// No fallbacks - enforces proper dependency injection
        /// </summary>
        /// <param name="context">Context object for logging</param>
        /// <param name="logCategory">Category for logging messages</param>
        /// <param name="types">Types to resolve</param>
        /// <returns>Array of resolved components (may contain nulls for unregistered dependencies)</returns>
        public static Component[] SafeResolveMultiple(Object context, string logCategory, params System.Type[] types)
        {
            var results = new Component[types.Length];

            for (int i = 0; i < types.Length; i++)
            {
                try
                {
                    var type = types[i];

                    // STRICT: ServiceContainer ONLY - no fallbacks
                    var resolved = ServiceContainerFactory.Instance?.TryResolve(type) as Component;
                    if (resolved != null)
                    {
                        results[i] = resolved;
                    }
                    else
                    {
                        // ZERO-TOLERANCE: No FindObjectOfType fallback
                        ChimeraLogger.LogError(logCategory,
                            $"DEPENDENCY NOT REGISTERED: {type.Name} must be registered in ServiceContainer. " +
                            $"FindObjectOfType fallbacks removed for Phase 0 zero-tolerance compliance.",
                            context);
                        results[i] = null;
                    }
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogError(logCategory,
                        $"Failed to resolve {types[i].Name}: {ex.Message}",
                        context);
                    results[i] = null;
                }
            }

            return results;
        }

        /// <summary>
        /// Check if ServiceContainer is available and initialized
        /// </summary>
        public static bool IsServiceContainerAvailable => ServiceContainerFactory.Instance != null;

        /// <summary>
        /// Register a component instance with ServiceContainer if available
        /// </summary>
        public static void SafeRegister<T>(T instance, string logCategory = "CORE", Object context = null) where T : class
        {
            if (instance == null) return;

            try
            {
                if (ServiceContainerFactory.Instance != null)
                {
                    if (!ServiceContainerFactory.Instance.IsRegistered<T>())
                    {
                        ServiceContainerFactory.Instance.RegisterInstance<T>(instance);
                        ChimeraLogger.LogInfo(logCategory, $"Registered {typeof(T).Name} with ServiceContainer", context);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError(logCategory, $"Failed to register {typeof(T).Name}: {ex.Message}", context);
            }
        }
    }
}
