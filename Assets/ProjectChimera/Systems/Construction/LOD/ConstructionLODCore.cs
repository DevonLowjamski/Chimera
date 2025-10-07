using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Construction.LOD
{
    /// <summary>
    /// REFACTORED: Construction LOD Core - Central coordination for construction LOD subsystems
    /// Coordinates distance calculation, LOD level management, and renderer control
    /// Single Responsibility: Central LOD system coordination
    /// </summary>
    public class ConstructionLODCore : MonoBehaviour, ITickable
    {
        [Header("Core Settings")]
        [SerializeField] private bool _enableLODCoordination = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 0.2f;

        // Subsystem references
        private ConstructionLODDistanceCalculator _distanceCalculator;
        private ConstructionLODLevelManager _levelManager;
        private ConstructionLODRendererController _rendererController;

        // Timing
        private float _lastUpdate;

        // System state
        private ConstructionLODSystemHealth _systemHealth = ConstructionLODSystemHealth.Healthy;
        private ConstructionLODCoreStats _stats = new ConstructionLODCoreStats();

        // Object tracking
        private readonly Dictionary<string, ConstructionLODObjectInfo> _managedObjects = new Dictionary<string, ConstructionLODObjectInfo>();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public ConstructionLODSystemHealth SystemHealth => _systemHealth;
        public ConstructionLODCoreStats Stats => _stats;
        public int ManagedObjectCount => _managedObjects.Count;

        // Events for backward compatibility
        public System.Action<string, ConstructionLODLevel> OnLODChanged;
        public System.Action<string> OnObjectCulled;
        public System.Action<string> OnObjectUnculled;
        public System.Action<ConstructionLODSystemHealth> OnHealthChanged;

        private void Start()
        {
            Initialize();
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }

        private void Initialize()
        {
            InitializeSubsystems();
            ConnectEventHandlers();

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", "⚡ ConstructionLODCore initialized", this);
        }

        #region ITickable Implementation

        /// <summary>
        /// LOD coordination priority - runs before rendering
        /// </summary>
        public int TickPriority => -10;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        /// <summary>
        /// Should tick when LOD coordination is enabled
        /// </summary>
        public bool ShouldTick => IsEnabled && _enableLODCoordination && enabled;

        public void Tick(float deltaTime)
        {
            if (Time.time - _lastUpdate >= _updateInterval)
            {
                ProcessLODCoordination();
                UpdateSystemHealth();
                UpdateStatistics();

                _lastUpdate = Time.time;
            }
        }

        #endregion

        /// <summary>
        /// Initialize all LOD subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Get or create subsystem components
            _distanceCalculator = GetOrCreateComponent<ConstructionLODDistanceCalculator>();
            _levelManager = GetOrCreateComponent<ConstructionLODLevelManager>();
            _rendererController = GetOrCreateComponent<ConstructionLODRendererController>();

            // Configure subsystems
            _distanceCalculator?.SetEnabled(_enableLODCoordination);
            _levelManager?.SetEnabled(_enableLODCoordination);
            _rendererController?.SetEnabled(_enableLODCoordination);
        }

        /// <summary>
        /// Connect event handlers between subsystems
        /// </summary>
        private void ConnectEventHandlers()
        {
            if (_distanceCalculator != null)
            {
                _distanceCalculator.OnDistanceCalculated += HandleDistanceCalculated;
                _distanceCalculator.OnSignificantDistanceChange += HandleSignificantDistanceChange;
            }

            if (_levelManager != null)
            {
                _levelManager.OnLODLevelChanged += HandleLODLevelChanged;
                _levelManager.OnObjectCulled += HandleObjectCulled;
                _levelManager.OnObjectUnculled += HandleObjectUnculled;
            }

            if (_rendererController != null)
            {
                _rendererController.OnLODConfigurationApplied += HandleLODConfigurationApplied;
                _rendererController.OnRenderersToggled += HandleRenderersToggled;
            }
        }

        /// <summary>
        /// Get or create subsystem component
        /// </summary>
        private T GetOrCreateComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Register construction object for LOD management
        /// </summary>
        public void RegisterObject(string objectId, GameObject constructionObject, ConstructionObjectType objectType)
        {
            if (!IsEnabled || constructionObject == null) return;

            if (_managedObjects.ContainsKey(objectId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONSTRUCTION", $"Object already registered: {objectId}", this);
                return;
            }

            var objectInfo = new ConstructionLODObjectInfo
            {
                ObjectId = objectId,
                GameObject = constructionObject,
                ObjectType = objectType,
                CurrentLODLevel = ConstructionLODLevel.High,
                LastDistance = 0f,
                RegistrationTime = Time.time
            };

            _managedObjects[objectId] = objectInfo;

            // Register with all subsystems
            _distanceCalculator?.RegisterObject(objectId, constructionObject.transform);
            _levelManager?.RegisterObject(objectId, objectType);
            _rendererController?.RegisterObject(objectId, constructionObject);

            _stats.TotalRegistrations++;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Registered construction object: {objectId} (type: {objectType})", this);
        }

        /// <summary>
        /// Unregister construction object from LOD management
        /// </summary>
        public void UnregisterObject(string objectId)
        {
            if (!IsEnabled) return;

            if (!_managedObjects.ContainsKey(objectId)) return;

            // Unregister from all subsystems
            _distanceCalculator?.UnregisterObject(objectId);
            _levelManager?.UnregisterObject(objectId);
            _rendererController?.UnregisterObject(objectId);

            _managedObjects.Remove(objectId);

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Unregistered construction object: {objectId}", this);
        }

        /// <summary>
        /// Get object LOD information
        /// </summary>
        public ConstructionLODObjectInfo? GetObjectInfo(string objectId)
        {
            return _managedObjects.TryGetValue(objectId, out var info) ? info : null;
        }

        /// <summary>
        /// Get objects by LOD level
        /// </summary>
        public ConstructionLODObjectInfo[] GetObjectsByLODLevel(ConstructionLODLevel lodLevel)
        {
            var result = new List<ConstructionLODObjectInfo>();
            foreach (var info in _managedObjects.Values)
            {
                if (info.CurrentLODLevel == lodLevel)
                {
                    result.Add(info);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Set LOD distance thresholds
        /// </summary>
        public void SetDistanceThresholds(float high, float medium, float low, float max)
        {
            _levelManager?.SetDistanceThresholds(high, medium, low, max);

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Updated LOD distance thresholds: H={high}, M={medium}, L={low}, Max={max}", this);
        }

        /// <summary>
        /// Set type-specific distance multiplier
        /// </summary>
        public void SetTypeDistanceMultiplier(ConstructionObjectType objectType, float multiplier)
        {
            _levelManager?.SetTypeDistanceMultiplier(objectType, multiplier);

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Set type multiplier: {objectType} = {multiplier}", this);
        }

        /// <summary>
        /// Force LOD level for object
        /// </summary>
        public void ForceLODLevel(string objectId, ConstructionLODLevel level)
        {
            if (!IsEnabled || !_managedObjects.ContainsKey(objectId)) return;

            _levelManager?.ForceLODLevel(objectId, level);

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Forced LOD level: {objectId} -> {level}", this);
        }

        /// <summary>
        /// Force update all LOD calculations
        /// </summary>
        public void ForceUpdateAllLOD()
        {
            if (!IsEnabled) return;

            _distanceCalculator?.ForceUpdateAllDistances();

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", "Forced update of all LOD calculations", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            // Update all subsystems
            _distanceCalculator?.SetEnabled(enabled);
            _levelManager?.SetEnabled(enabled);
            _rendererController?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"ConstructionLODCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Process LOD coordination
        /// </summary>
        private void ProcessLODCoordination()
        {
            // Distance calculator updates automatically
            // Level manager responds to distance changes
            // Renderer controller responds to level changes
            // This method handles any additional coordination needed
        }

        /// <summary>
        /// Update system health based on subsystem status
        /// </summary>
        private void UpdateSystemHealth()
        {
            var previousHealth = _systemHealth;
            _systemHealth = DetermineSystemHealth();

            if (_systemHealth != previousHealth)
            {
                OnHealthChanged?.Invoke(_systemHealth);

                if (_enableLogging)
                    ChimeraLogger.Log("CONSTRUCTION", $"LOD system health changed: {previousHealth} -> {_systemHealth}", this);
            }
        }

        /// <summary>
        /// Determine overall system health
        /// </summary>
        private ConstructionLODSystemHealth DetermineSystemHealth()
        {
            // Check if subsystems are available and functioning
            if (_distanceCalculator == null || _levelManager == null || _rendererController == null)
                return ConstructionLODSystemHealth.Critical;

            // Check if any subsystems are disabled
            if (!_distanceCalculator.IsEnabled || !_levelManager.IsEnabled || !_rendererController.IsEnabled)
                return ConstructionLODSystemHealth.Degraded;

            // Check performance metrics
            var distanceStats = _distanceCalculator.GetStats();
            if (distanceStats.TrackedObjects > 1000)
                return ConstructionLODSystemHealth.Warning;

            return ConstructionLODSystemHealth.Healthy;
        }

        /// <summary>
        /// Update coordination statistics
        /// </summary>
        private void UpdateStatistics()
        {
            _stats.ManagedObjects = _managedObjects.Count;
            _stats.SystemHealth = _systemHealth;

            if (_distanceCalculator != null)
            {
                var distanceStats = _distanceCalculator.GetStats();
                _stats.AverageDistance = distanceStats.AverageDistance;
            }

            if (_levelManager != null)
            {
                var levelStats = _levelManager.Stats;
                _stats.HighLODObjects = levelStats.HighLODObjects;
                _stats.MediumLODObjects = levelStats.MediumLODObjects;
                _stats.LowLODObjects = levelStats.LowLODObjects;
                _stats.CulledObjects = levelStats.CulledObjects;
                _stats.LODTransitions = levelStats.LODTransitions;
            }

            _stats.LODEfficiency = CalculateLODEfficiency();
        }

        /// <summary>
        /// Calculate LOD efficiency metric
        /// </summary>
        private float CalculateLODEfficiency()
        {
            if (_managedObjects.Count == 0) return 1f;

            // Efficiency based on LOD distribution - lower LOD levels are more efficient
            float efficiency = 0f;
            efficiency += _stats.HighLODObjects * 0.25f;    // High LOD = less efficient
            efficiency += _stats.MediumLODObjects * 0.5f;   // Medium LOD = balanced
            efficiency += _stats.LowLODObjects * 0.75f;     // Low LOD = more efficient
            efficiency += _stats.CulledObjects * 1f;        // Culled = most efficient

            return efficiency / _managedObjects.Count;
        }

        #endregion

        #region Event Handlers

        private void HandleDistanceCalculated(string objectId, float distance)
        {
            if (_managedObjects.TryGetValue(objectId, out var info))
            {
                info.LastDistance = distance;
                _managedObjects[objectId] = info;

                // Trigger LOD level update
                var transition = _levelManager?.UpdateLODLevel(objectId, distance, info.ObjectType);
                if (transition?.Changed == true)
                {
                    info.CurrentLODLevel = transition.Value.NewLevel;
                    _managedObjects[objectId] = info;
                }
            }
        }

        private void HandleSignificantDistanceChange(string objectId, float oldDistance, float newDistance)
        {
            // Distance changes are automatically handled by HandleDistanceCalculated
        }

        private void HandleLODLevelChanged(string objectId, ConstructionLODLevel oldLevel, ConstructionLODLevel newLevel)
        {
            // Apply new LOD configuration to renderers
            _rendererController?.ApplyLODConfiguration(objectId, newLevel);

            OnLODChanged?.Invoke(objectId, newLevel);
        }

        private void HandleObjectCulled(string objectId)
        {
            OnObjectCulled?.Invoke(objectId);
        }

        private void HandleObjectUnculled(string objectId)
        {
            OnObjectUnculled?.Invoke(objectId);
        }

        private void HandleLODConfigurationApplied(string objectId, ConstructionLODLevel lodLevel)
        {
            // Configuration applied successfully
        }

        private void HandleRenderersToggled(string objectId, int activeRenderers)
        {
            _stats.RendererStateChanges++;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Construction LOD system health enumeration
    /// </summary>
    public enum ConstructionLODSystemHealth
    {
        Healthy,
        Warning,
        Degraded,
        Critical
    }

    /// <summary>
    /// Construction LOD object information
    /// </summary>
    [System.Serializable]
    public struct ConstructionLODObjectInfo
    {
        public string ObjectId;
        public GameObject GameObject;
        public ConstructionObjectType ObjectType;
        public ConstructionLODLevel CurrentLODLevel;
        public float LastDistance;
        public float RegistrationTime;
    }

    /// <summary>
    /// Construction LOD core statistics
    /// </summary>
    [System.Serializable]
    public struct ConstructionLODCoreStats
    {
        public ConstructionLODSystemHealth SystemHealth;
        public int ManagedObjects;
        public int TotalRegistrations;
        public int HighLODObjects;
        public int MediumLODObjects;
        public int LowLODObjects;
        public int CulledObjects;
        public int LODTransitions;
        public int RendererStateChanges;
        public float AverageDistance;
        public float LODEfficiency;
    }

    #endregion
}