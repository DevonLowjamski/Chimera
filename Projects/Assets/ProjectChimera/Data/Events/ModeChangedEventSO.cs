using UnityEngine;

namespace ProjectChimera.Data.Events
{
    /// <summary>
    /// Event channel for gameplay mode changes
    /// Phase 2 implementation - carries detailed mode change information
    /// </summary>
    [CreateAssetMenu(fileName = "New Mode Changed Event", menuName = "Project Chimera/Events/Mode Changed Event", order = 200)]
    public class ModeChangedEventSO : TypedGameEventSO<ModeChangeEventData>
    {
        [Header("Mode Change Event Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _validateModeTransitions = true;

        public override void Invoke(ModeChangeEventData data)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[ModeChangedEventSO] Mode change event: {data.PreviousMode} → {data.NewMode} at {data.Timestamp:HH:mm:ss}");
            }

            if (_validateModeTransitions && !IsValidModeTransition(data.PreviousMode, data.NewMode))
            {
                Debug.LogWarning($"[ModeChangedEventSO] Potentially invalid mode transition: {data.PreviousMode} → {data.NewMode}");
            }

            base.Invoke(data);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Validate configuration
            if (_enableDebugLogging)
            {
                Debug.Log($"[ModeChangedEventSO] Debug logging enabled for {name}");
            }
        }

        /// <summary>
        /// Validate if a mode transition is allowed (can be extended for business rules)
        /// </summary>
        private bool IsValidModeTransition(GameplayMode from, GameplayMode to)
        {
            // For now, all transitions are allowed
            // This can be extended to enforce business rules like:
            // - Can't switch to Genetics mode without completing Cultivation tutorial
            // - Can't switch to Construction mode while plants are being harvested
            return from != to; // Only invalid if switching to same mode
        }

        /// <summary>
        /// Helper method to create mode change data
        /// </summary>
        public static ModeChangeEventData CreateModeChangeData(GameplayMode newMode, GameplayMode previousMode)
        {
            return new ModeChangeEventData
            {
                NewMode = newMode,
                PreviousMode = previousMode,
                Timestamp = System.DateTime.Now,
                IsValid = newMode != previousMode
            };
        }
    }

    /// <summary>
    /// Data structure for mode change events
    /// Extended from the basic version in GameplayModeController
    /// </summary>
    [System.Serializable]
    public struct ModeChangeEventData
    {
        [Header("Mode Transition")]
        public GameplayMode NewMode;
        public GameplayMode PreviousMode;
        
        [Header("Event Metadata")]
        public System.DateTime Timestamp;
        public bool IsValid;
        
        [Header("Optional Context")]
        public string TriggerSource; // e.g., "Keyboard", "UI Button", "Auto"
        public string UserContext;   // Optional user-specific context
        
        /// <summary>
        /// Get a human-readable description of the mode change
        /// </summary>
        public string GetDescription()
        {
            return $"Changed from {PreviousMode} to {NewMode}";
        }

        /// <summary>
        /// Check if this represents a valid mode change
        /// </summary>
        public bool IsValidTransition => IsValid && NewMode != PreviousMode;

        /// <summary>
        /// Get the time elapsed since the event (for debugging/analytics)
        /// </summary>
        public System.TimeSpan TimeSinceEvent => System.DateTime.Now - Timestamp;
    }

    /// <summary>
    /// Gameplay modes enum - core modes for Project Chimera
    /// Moved to Data.Events to avoid circular dependencies
    /// </summary>
    public enum GameplayMode
    {
        Cultivation = 0,    // Default view - plant care and monitoring
        Construction = 1,   // Blueprint and utility visibility toggles
        Genetics = 2        // Heatmap overlay toggles and genetic tools
    }
}