using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction.LOD
{
    /// <summary>
    /// REFACTORED: Construction LOD Level Manager - Focused LOD level determination and transitions
    /// Handles LOD level calculation based on distance and manages level transitions
    /// Single Responsibility: LOD level determination and transition logic
    /// </summary>
    public class ConstructionLODLevelManager : MonoBehaviour
    {
        [Header("LOD Level Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _maxViewDistance = 100f;

        [Header("LOD Distance Thresholds")]
        [SerializeField] private float _highQualityDistance = 25f;
        [SerializeField] private float _mediumQualityDistance = 50f;
        [SerializeField] private float _lowQualityDistance = 75f;

        [Header("Transition Settings")]
        [SerializeField] private float _hysteresisBuffer = 2f; // Prevents flickering between levels
        [SerializeField] private bool _enableHysteresis = true;

        // LOD state tracking
        private readonly Dictionary<string, ConstructionLODLevel> _currentLODLevels = new Dictionary<string, ConstructionLODLevel>();
        private readonly Dictionary<string, float> _lastTransitionDistances = new Dictionary<string, float>();

        // Object type specific multipliers
        private readonly Dictionary<ConstructionObjectType, float> _typeDistanceMultipliers = new Dictionary<ConstructionObjectType, float>();

        // Transition statistics
        private LODLevelStats _stats = new LODLevelStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public float MaxViewDistance => _maxViewDistance;
        public int ManagedObjectCount => _currentLODLevels.Count;
        public LODLevelStats Stats => _stats;

        // Events
        public System.Action<string, ConstructionLODLevel, ConstructionLODLevel> OnLODLevelChanged;
        public System.Action<string> OnObjectCulled;
        public System.Action<string> OnObjectUnculled;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeTypeMultipliers();
            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", "📊 ConstructionLODLevelManager initialized", this);
        }

        /// <summary>
        /// Register object for LOD level management
        /// </summary>
        public void RegisterObject(string objectId, ConstructionObjectType objectType)
        {
            if (!IsEnabled) return;

            if (_currentLODLevels.ContainsKey(objectId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONSTRUCTION", $"Object already registered for LOD level management: {objectId}", this);
                return;
            }

            _currentLODLevels[objectId] = ConstructionLODLevel.High;
            _lastTransitionDistances[objectId] = 0f;
            _stats.RegisteredObjects++;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Registered object for LOD level management: {objectId} (type: {objectType})", this);
        }

        /// <summary>
        /// Unregister object from LOD level management
        /// </summary>
        public void UnregisterObject(string objectId)
        {
            if (!IsEnabled) return;

            if (_currentLODLevels.Remove(objectId))
            {
                _lastTransitionDistances.Remove(objectId);
                _stats.RegisteredObjects--;

                if (_enableLogging)
                    ChimeraLogger.Log("CONSTRUCTION", $"Unregistered object from LOD level management: {objectId}", this);
            }
        }

        /// <summary>
        /// Update LOD level for object based on distance
        /// </summary>
        public ConstructionLODTransition UpdateLODLevel(string objectId, float distance, ConstructionObjectType objectType)
        {
            if (!IsEnabled || !_currentLODLevels.TryGetValue(objectId, out var currentLevel))
            {
                return new ConstructionLODTransition
                {
                    ObjectId = objectId,
                    OldLevel = ConstructionLODLevel.High,
                    NewLevel = ConstructionLODLevel.High,
                    Changed = false,
                    Distance = distance
                };
            }

            var adjustedDistance = ApplyTypeMultiplier(distance, objectType);
            var newLevel = DetermineLODLevel(adjustedDistance, currentLevel, objectId);

            var transition = new ConstructionLODTransition
            {
                ObjectId = objectId,
                OldLevel = currentLevel,
                NewLevel = newLevel,
                Changed = newLevel != currentLevel,
                Distance = adjustedDistance,
                ObjectType = objectType
            };

            if (transition.Changed)
            {
                ProcessLODTransition(transition);
            }

            return transition;
        }

        /// <summary>
        /// Get current LOD level for object
        /// </summary>
        public ConstructionLODLevel GetCurrentLODLevel(string objectId)
        {
            return _currentLODLevels.TryGetValue(objectId, out var level) ? level : ConstructionLODLevel.High;
        }

        /// <summary>
        /// Get objects by LOD level
        /// </summary>
        public string[] GetObjectsByLODLevel(ConstructionLODLevel lodLevel)
        {
            return _currentLODLevels.Where(kvp => kvp.Value == lodLevel).Select(kvp => kvp.Key).ToArray();
        }

        /// <summary>
        /// Get LOD level distribution
        /// </summary>
        public Dictionary<ConstructionLODLevel, int> GetLODDistribution()
        {
            var distribution = new Dictionary<ConstructionLODLevel, int>();

            foreach (ConstructionLODLevel level in System.Enum.GetValues(typeof(ConstructionLODLevel)))
            {
                distribution[level] = 0;
            }

            foreach (var level in _currentLODLevels.Values)
            {
                distribution[level]++;
            }

            return distribution;
        }

        /// <summary>
        /// Set distance thresholds
        /// </summary>
        public void SetDistanceThresholds(float high, float medium, float low, float max)
        {
            _highQualityDistance = high;
            _mediumQualityDistance = medium;
            _lowQualityDistance = low;
            _maxViewDistance = max;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Updated distance thresholds: High={high}, Medium={medium}, Low={low}, Max={max}", this);

            // Force recalculation of all LOD levels
            foreach (var objectId in _currentLODLevels.Keys.ToArray())
            {
                _lastTransitionDistances[objectId] = -1f; // Force update
            }
        }

        /// <summary>
        /// Set type-specific distance multiplier
        /// </summary>
        public void SetTypeDistanceMultiplier(ConstructionObjectType objectType, float multiplier)
        {
            _typeDistanceMultipliers[objectType] = multiplier;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Set distance multiplier for {objectType}: {multiplier}", this);
        }

        /// <summary>
        /// Force LOD level for object (ignoring distance)
        /// </summary>
        public void ForceLODLevel(string objectId, ConstructionLODLevel level)
        {
            if (!IsEnabled || !_currentLODLevels.ContainsKey(objectId)) return;

            var oldLevel = _currentLODLevels[objectId];
            if (oldLevel == level) return;

            var transition = new ConstructionLODTransition
            {
                ObjectId = objectId,
                OldLevel = oldLevel,
                NewLevel = level,
                Changed = true,
                Distance = 0f,
                WasForced = true
            };

            ProcessLODTransition(transition);

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Forced LOD level for {objectId}: {oldLevel} -> {level}", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                // Reset all objects to high LOD when disabled
                foreach (var objectId in _currentLODLevels.Keys.ToArray())
                {
                    if (_currentLODLevels[objectId] != ConstructionLODLevel.High)
                    {
                        ForceLODLevel(objectId, ConstructionLODLevel.High);
                    }
                }
            }

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"ConstructionLODLevelManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize type-specific distance multipliers
        /// </summary>
        private void InitializeTypeMultipliers()
        {
            _typeDistanceMultipliers[ConstructionObjectType.Building] = 1.0f;        // Standard distance
            _typeDistanceMultipliers[ConstructionObjectType.Infrastructure] = 1.2f;  // Visible from further
            _typeDistanceMultipliers[ConstructionObjectType.Decoration] = 0.8f;      // Cull closer
            _typeDistanceMultipliers[ConstructionObjectType.Equipment] = 0.9f;       // Slightly closer culling
            _typeDistanceMultipliers[ConstructionObjectType.Utility] = 1.1f;         // Keep visible longer
        }

        /// <summary>
        /// Apply type-specific distance multiplier
        /// </summary>
        private float ApplyTypeMultiplier(float distance, ConstructionObjectType objectType)
        {
            if (_typeDistanceMultipliers.TryGetValue(objectType, out var multiplier))
            {
                return distance * multiplier;
            }
            return distance;
        }

        /// <summary>
        /// Determine LOD level based on distance with hysteresis
        /// </summary>
        private ConstructionLODLevel DetermineLODLevel(float distance, ConstructionLODLevel currentLevel, string objectId)
        {
            // Apply hysteresis to prevent flickering
            float hysteresis = _enableHysteresis ? _hysteresisBuffer : 0f;
            float lastDistance = _lastTransitionDistances.GetValueOrDefault(objectId, distance);

            // Determine new level based on distance thresholds
            ConstructionLODLevel newLevel;

            if (distance > _maxViewDistance + hysteresis)
                newLevel = ConstructionLODLevel.Culled;
            else if (distance > _lowQualityDistance + (distance > lastDistance ? hysteresis : -hysteresis))
                newLevel = ConstructionLODLevel.Low;
            else if (distance > _mediumQualityDistance + (distance > lastDistance ? hysteresis : -hysteresis))
                newLevel = ConstructionLODLevel.Medium;
            else
                newLevel = ConstructionLODLevel.High;

            // Special case: if moving towards viewer and close to threshold, prefer higher LOD
            if (distance < lastDistance && currentLevel > newLevel)
            {
                return newLevel;
            }

            // Special case: if moving away and close to threshold, use hysteresis
            if (distance > lastDistance && currentLevel < newLevel && _enableHysteresis)
            {
                float threshold = GetThresholdForLevel(newLevel);
                if (distance < threshold + hysteresis)
                {
                    return currentLevel; // Stay at current level
                }
            }

            return newLevel;
        }

        /// <summary>
        /// Get distance threshold for LOD level
        /// </summary>
        private float GetThresholdForLevel(ConstructionLODLevel level)
        {
            switch (level)
            {
                case ConstructionLODLevel.High:
                    return 0f;
                case ConstructionLODLevel.Medium:
                    return _highQualityDistance;
                case ConstructionLODLevel.Low:
                    return _mediumQualityDistance;
                case ConstructionLODLevel.Culled:
                    return _maxViewDistance;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Process LOD level transition
        /// </summary>
        private void ProcessLODTransition(ConstructionLODTransition transition)
        {
            _currentLODLevels[transition.ObjectId] = transition.NewLevel;
            _lastTransitionDistances[transition.ObjectId] = transition.Distance;

            // Update statistics
            _stats.LODTransitions++;
            UpdateLevelCounts();

            // Fire events
            OnLODLevelChanged?.Invoke(transition.ObjectId, transition.OldLevel, transition.NewLevel);

            // Handle culling events
            if (transition.NewLevel == ConstructionLODLevel.Culled && transition.OldLevel != ConstructionLODLevel.Culled)
            {
                OnObjectCulled?.Invoke(transition.ObjectId);
                _stats.CulledObjects++;
            }
            else if (transition.OldLevel == ConstructionLODLevel.Culled && transition.NewLevel != ConstructionLODLevel.Culled)
            {
                OnObjectUnculled?.Invoke(transition.ObjectId);
                _stats.CulledObjects--;
            }

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"LOD transition: {transition.ObjectId} {transition.OldLevel} -> {transition.NewLevel} (distance: {transition.Distance:F1})", this);
        }

        /// <summary>
        /// Update level counts in statistics
        /// </summary>
        private void UpdateLevelCounts()
        {
            var distribution = GetLODDistribution();
            _stats.HighLODObjects = distribution[ConstructionLODLevel.High];
            _stats.MediumLODObjects = distribution[ConstructionLODLevel.Medium];
            _stats.LowLODObjects = distribution[ConstructionLODLevel.Low];
            _stats.CulledObjects = distribution[ConstructionLODLevel.Culled];
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new LODLevelStats
            {
                RegisteredObjects = 0,
                HighLODObjects = 0,
                MediumLODObjects = 0,
                LowLODObjects = 0,
                CulledObjects = 0,
                LODTransitions = 0
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Construction LOD levels
    /// </summary>
    public enum ConstructionLODLevel
    {
        High,
        Medium,
        Low,
        Culled
    }

    /// <summary>
    /// Construction object types for LOD calculation
    /// </summary>
    public enum ConstructionObjectType
    {
        Building,
        Infrastructure,
        Decoration,
        Equipment,
        Utility
    }

    /// <summary>
    /// LOD level transition data
    /// </summary>
    [System.Serializable]
    public struct ConstructionLODTransition
    {
        public string ObjectId;
        public ConstructionLODLevel OldLevel;
        public ConstructionLODLevel NewLevel;
        public bool Changed;
        public float Distance;
        public ConstructionObjectType ObjectType;
        public bool WasForced;
    }

    /// <summary>
    /// LOD level manager statistics
    /// </summary>
    [System.Serializable]
    public struct LODLevelStats
    {
        public int RegisteredObjects;
        public int HighLODObjects;
        public int MediumLODObjects;
        public int LowLODObjects;
        public int CulledObjects;
        public int LODTransitions;
    }

    #endregion
}