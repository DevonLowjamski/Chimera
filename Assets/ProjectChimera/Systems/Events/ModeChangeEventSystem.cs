using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Events
{
    /// <summary>
    /// SIMPLE: Basic mode change event system aligned with Project Chimera's gameplay vision.
    /// Focuses on essential mode change notifications for Construction, Cultivation, and Genetics modes.
    /// </summary>
    public class ModeChangeEventSystem : MonoBehaviour
    {
        [Header("Basic Event Settings")]
        [SerializeField] private bool _enableEvents = true;
        [SerializeField] private bool _enableLogging = true;

        // Basic event handling
        private GameplayMode _currentMode = GameplayMode.Cultivation;

        /// <summary>
        /// Events for mode changes
        /// </summary>
        public event System.Action<GameplayMode, GameplayMode> OnModeChanged;
        public event System.Action<GameplayMode> OnConstructionModeEntered;
        public event System.Action<GameplayMode> OnCultivationModeEntered;
        public event System.Action<GameplayMode> OnGeneticsModeEntered;

        /// <summary>
        /// Initialize the basic event system
        /// </summary>
        public void Initialize()
        {
            if (_enableLogging)
            {
                ProjectChimera.Core.Logging.ChimeraLogger.Log("EVENTS/MODE", "Mode registered", this);
            }
        }

        /// <summary>
        /// Change to a new gameplay mode
        /// </summary>
        public void ChangeMode(GameplayMode newMode)
        {
            if (!_enableEvents || _currentMode == newMode) return;

            GameplayMode previousMode = _currentMode;
            _currentMode = newMode;

            // Raise general mode change event
            OnModeChanged?.Invoke(previousMode, newMode);

            // Raise specific mode entered events
            switch (newMode)
            {
                case GameplayMode.Construction:
                    OnConstructionModeEntered?.Invoke(previousMode);
                    break;
                case GameplayMode.Cultivation:
                    OnCultivationModeEntered?.Invoke(previousMode);
                    break;
                case GameplayMode.Genetics:
                    OnGeneticsModeEntered?.Invoke(previousMode);
                    break;
            }

            if (_enableLogging)
            {
                ProjectChimera.Core.Logging.ChimeraLogger.Log("EVENTS/MODE", "Mode changed", this);
            }
        }

        /// <summary>
        /// Switch to construction mode
        /// </summary>
        public void SwitchToConstructionMode()
        {
            ChangeMode(GameplayMode.Construction);
        }

        /// <summary>
        /// Switch to cultivation mode
        /// </summary>
        public void SwitchToCultivationMode()
        {
            ChangeMode(GameplayMode.Cultivation);
        }

        /// <summary>
        /// Switch to genetics mode
        /// </summary>
        public void SwitchToGeneticsMode()
        {
            ChangeMode(GameplayMode.Genetics);
        }

        /// <summary>
        /// Get current gameplay mode
        /// </summary>
        public GameplayMode GetCurrentMode()
        {
            return _currentMode;
        }

        /// <summary>
        /// Check if currently in construction mode
        /// </summary>
        public bool IsInConstructionMode()
        {
            return _currentMode == GameplayMode.Construction;
        }

        /// <summary>
        /// Check if currently in cultivation mode
        /// </summary>
        public bool IsInCultivationMode()
        {
            return _currentMode == GameplayMode.Cultivation;
        }

        /// <summary>
        /// Check if currently in genetics mode
        /// </summary>
        public bool IsInGeneticsMode()
        {
            return _currentMode == GameplayMode.Genetics;
        }

        /// <summary>
        /// Get mode display name
        /// </summary>
        public string GetModeDisplayName(GameplayMode mode)
        {
            switch (mode)
            {
                case GameplayMode.Construction:
                    return "Construction";
                case GameplayMode.Cultivation:
                    return "Cultivation";
                case GameplayMode.Genetics:
                    return "Genetics";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Get current mode display name
        /// </summary>
        public string GetCurrentModeDisplayName()
        {
            return GetModeDisplayName(_currentMode);
        }

        /// <summary>
        /// Get event system statistics
        /// </summary>
        public EventSystemStatistics GetStatistics()
        {
            return new EventSystemStatistics
            {
                CurrentMode = _currentMode,
                EventsEnabled = _enableEvents,
                LoggingEnabled = _enableLogging,
                CurrentModeName = GetCurrentModeDisplayName()
            };
        }

        /// <summary>
        /// Set events enabled/disabled
        /// </summary>
        public void SetEventsEnabled(bool enabled)
        {
            _enableEvents = enabled;

            if (_enableLogging)
            {
                ProjectChimera.Core.Logging.ChimeraLogger.Log("EVENTS/MODE", "Listener error", this);
            }
        }
    }

    /// <summary>
    /// Gameplay mode enum
    /// </summary>
    public enum GameplayMode
    {
        Construction,
        Cultivation,
        Genetics
    }

    /// <summary>
    /// Event system statistics
    /// </summary>
    [System.Serializable]
    public class EventSystemStatistics
    {
        public GameplayMode CurrentMode;
        public bool EventsEnabled;
        public bool LoggingEnabled;
        public string CurrentModeName;
    }
}
