using UnityEngine;

namespace ProjectChimera.Shared
{
    public abstract class ChimeraScriptableObject : ScriptableObject
    {
        public string UniqueID => name;
        public string DisplayName => name;

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
            Debug.Log($"[Chimera] {DisplayName}: {message}", this);
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[Chimera] {DisplayName}: {message}", this);
        }

        protected void LogError(string message)
        {
            Debug.LogError($"[Chimera] {DisplayName}: {message}", this);
        }
    }
}
