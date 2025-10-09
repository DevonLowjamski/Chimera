// REFACTORED: Environmental Service Delegator
// Extracted from SpeedTreeEnvironmentalService to reduce boilerplate

using ProjectChimera.Core.Logging;
using System;
using UnityEngine;

namespace ProjectChimera.Systems.Services.SpeedTree
{
    /// <summary>
    /// Handles safe delegation to subsystems with validation and error handling
    /// </summary>
    public static class EnvironmentalServiceDelegator
    {
        /// <summary>
        /// Safely delegate an action to a subsystem with validation and error handling
        /// </summary>
        public static void SafeDelegate(bool isInitialized, Action action, string operationName, MonoBehaviour context)
        {
            if (!isInitialized)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", $"{operationName} called before initialization", context);
                return;
            }

            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", $"{operationName} failed: {ex.Message}", context);
            }
        }

        /// <summary>
        /// Safely delegate a function to a subsystem with validation and error handling
        /// </summary>
        public static T SafeDelegateWithReturn<T>(bool isInitialized, Func<T> func, string operationName, MonoBehaviour context, T defaultValue = default)
        {
            if (!isInitialized)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", $"{operationName} called before initialization", context);
                return defaultValue;
            }

            try
            {
                return func != null ? func() : defaultValue;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", $"{operationName} failed: {ex.Message}", context);
                return defaultValue;
            }
        }
    }
}

