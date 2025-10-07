using ProjectChimera.Data.Events;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Interface for gameplay mode controller - enables DI registration and testing
    /// Phase 2 verification requirement for service locator access
    /// </summary>
    public interface IGameplayModeController
    {
        /// <summary>
        /// Current active gameplay mode
        /// </summary>
        GameplayMode CurrentMode { get; }
        
        /// <summary>
        /// Previously active gameplay mode
        /// </summary>
        GameplayMode PreviousMode { get; }
        
        /// <summary>
        /// Whether the controller has been initialized
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// History of mode transitions (read-only)
        /// </summary>
        IReadOnlyList<ModeTransitionRecord> ModeHistory { get; }
        
        /// <summary>
        /// Set the current gameplay mode with optional trigger source context
        /// </summary>
        /// <param name="newMode">The mode to switch to</param>
        /// <param name="triggerSource">Optional context about what triggered the change</param>
        /// <returns>True if the mode was successfully changed</returns>
        bool SetMode(GameplayMode newMode, string triggerSource = "");
        
        /// <summary>
        /// Register a callback to be called when entering a specific mode
        /// </summary>
        /// <param name="mode">The mode to register the callback for</param>
        /// <param name="callback">The callback to execute on mode entry</param>
        void RegisterModeEntryCallback(GameplayMode mode, System.Action callback);
        
        /// <summary>
        /// Register a callback to be called when exiting a specific mode
        /// </summary>
        /// <param name="mode">The mode to register the callback for</param>
        /// <param name="callback">The callback to execute on mode exit</param>
        void RegisterModeExitCallback(GameplayMode mode, System.Action callback);
        
        /// <summary>
        /// Check if a transition between modes is valid
        /// </summary>
        /// <param name="fromMode">Current mode</param>
        /// <param name="toMode">Target mode</param>
        /// <returns>True if the transition is valid</returns>
        bool IsValidModeTransition(GameplayMode fromMode, GameplayMode toMode);
    }
    
    /// <summary>
    /// Record of a mode transition for history tracking
    /// </summary>
    [System.Serializable]
    public struct ModeTransitionRecord
    {
        public GameplayMode FromMode;
        public GameplayMode ToMode;
        public System.DateTime Timestamp;
        public string TriggerSource;
        public bool WasSuccessful;
        
        public ModeTransitionRecord(GameplayMode from, GameplayMode to, string source)
        {
            FromMode = from;
            ToMode = to;
            Timestamp = System.DateTime.Now;
            TriggerSource = source;
            WasSuccessful = true;
        }
    }
}