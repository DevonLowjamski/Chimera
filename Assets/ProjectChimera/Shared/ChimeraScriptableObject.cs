using UnityEngine;

namespace ProjectChimera.Shared
{
    /// <summary>
    /// Simple logging abstraction for Foundation layer - avoids Core dependencies
    /// </summary>
    public static class SharedLogger
    {
        public static void Log(string message, Object context = null)
        {
            Debug.Log($"[Chimera] {message}", context);
        }
        
        public static void LogWarning(string message, Object context = null)
        {
            Debug.LogWarning($"[Chimera] {message}", context);
        }
        
        public static void LogError(string message, Object context = null)
        {
            Debug.LogError($"[Chimera] {message}", context);
        }
    }

    /// <summary>
    /// Base class for all Project Chimera ScriptableObjects with logging support
    /// </summary>
    public abstract class ChimeraScriptableObject : ScriptableObject
    {
        public string UniqueID => name;
        public string DisplayName => name;
        
        /// <summary>
        /// Sets display name for editor configuration (replaces dangerous reflection access)
        /// </summary>
        public virtual void SetDisplayNameFromAssetName()
        {
            // Base implementation - derived classes can override for specific behavior
            name = string.IsNullOrEmpty(name) ? GetType().Name : name;
        }

        protected virtual void OnValidate()
        {
            // Base validation logic can go here if needed in the future.
        }

        public virtual bool ValidateData()
        {
            return ValidateDataSpecific();
        }

        protected virtual bool ValidateDataSpecific()
        {
            return true;
        }

        protected void LogInfo(string message)
        {
            SharedLogger.Log($"[Chimera] {DisplayName}: {message}", this);
        }

        protected void LogWarning(string message)
        {
            SharedLogger.LogWarning($"[Chimera] {DisplayName}: {message}", this);
        }

        protected void LogError(string message)
        {
            SharedLogger.LogError($"[Chimera] {DisplayName}: {message}", this);
        }
    }
}
