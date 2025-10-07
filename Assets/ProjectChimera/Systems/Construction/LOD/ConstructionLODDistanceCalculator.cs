using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Construction.LOD
{
    /// <summary>
    /// REFACTORED: Construction LOD Distance Calculator - Focused distance calculation and viewer management
    /// Handles distance calculations between viewer and construction objects for LOD determination
    /// Single Responsibility: Distance calculation and viewer tracking
    /// </summary>
    public class ConstructionLODDistanceCalculator : MonoBehaviour, ITickable
    {
        [Header("Distance Calculation Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 0.2f;
        [SerializeField] private bool _useSquaredDistance = true; // More performant

        [Header("Viewer Settings")]
        [SerializeField] private bool _autoFindViewer = true;
        [SerializeField] private Transform _customViewer;

        // Distance tracking
        private readonly Dictionary<string, float> _objectDistances = new Dictionary<string, float>();
        private readonly Dictionary<string, Transform> _objectTransforms = new Dictionary<string, Transform>();
        private readonly List<string> _distanceUpdateQueue = new List<string>();

        // Viewer management
        private Transform _viewerTransform;
        private Vector3 _lastViewerPosition;
        private float _lastDistanceUpdate;

        // Performance optimization
        private readonly Dictionary<string, float> _lastCalculatedDistances = new Dictionary<string, float>();
        private const float DISTANCE_CHANGE_THRESHOLD = 2f; // Only update if distance changed significantly

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public Transform ViewerTransform => _viewerTransform;
        public Vector3 ViewerPosition => _viewerTransform != null ? _viewerTransform.position : Vector3.zero;
        public int TrackedObjectCount => _objectTransforms.Count;

        // Events
        public System.Action<string, float> OnDistanceCalculated;
        public System.Action<Vector3> OnViewerPositionChanged;
        public System.Action<string, float, float> OnSignificantDistanceChange;

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
            InitializeViewerReference();
            _lastViewerPosition = ViewerPosition;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", "📏 ConstructionLODDistanceCalculator initialized", this);
        }

        #region ITickable Implementation

        /// <summary>
        /// Distance calculation priority - runs early for LOD decisions
        /// </summary>
        public int TickPriority => -15;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        /// <summary>
        /// Should tick when distance calculation is enabled
        /// </summary>
        public bool ShouldTick => IsEnabled && enabled;

        public void Tick(float deltaTime)
        {
            if (Time.time - _lastDistanceUpdate >= _updateInterval)
            {
                UpdateDistanceCalculations();
                _lastDistanceUpdate = Time.time;
            }
        }

        #endregion

        /// <summary>
        /// Register object for distance tracking
        /// </summary>
        public void RegisterObject(string objectId, Transform objectTransform)
        {
            if (!IsEnabled || objectTransform == null) return;

            if (_objectTransforms.ContainsKey(objectId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONSTRUCTION", $"Object already registered for distance tracking: {objectId}", this);
                return;
            }

            _objectTransforms[objectId] = objectTransform;
            _objectDistances[objectId] = CalculateDistance(objectTransform.position);
            _lastCalculatedDistances[objectId] = _objectDistances[objectId];

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Registered object for distance tracking: {objectId}", this);
        }

        /// <summary>
        /// Unregister object from distance tracking
        /// </summary>
        public void UnregisterObject(string objectId)
        {
            if (!IsEnabled) return;

            _objectTransforms.Remove(objectId);
            _objectDistances.Remove(objectId);
            _lastCalculatedDistances.Remove(objectId);

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Unregistered object from distance tracking: {objectId}", this);
        }

        /// <summary>
        /// Get current distance to object
        /// </summary>
        public float GetDistance(string objectId)
        {
            return _objectDistances.TryGetValue(objectId, out var distance) ? distance : float.MaxValue;
        }

        /// <summary>
        /// Get all object distances
        /// </summary>
        public Dictionary<string, float> GetAllDistances()
        {
            return new Dictionary<string, float>(_objectDistances);
        }

        /// <summary>
        /// Force distance update for specific object
        /// </summary>
        public void ForceUpdateDistance(string objectId)
        {
            if (!IsEnabled || !_objectTransforms.TryGetValue(objectId, out var objectTransform)) return;

            var newDistance = CalculateDistance(objectTransform.position);
            var oldDistance = _objectDistances.GetValueOrDefault(objectId, float.MaxValue);

            _objectDistances[objectId] = newDistance;
            _lastCalculatedDistances[objectId] = newDistance;

            OnDistanceCalculated?.Invoke(objectId, newDistance);

            if (Mathf.Abs(newDistance - oldDistance) > DISTANCE_CHANGE_THRESHOLD)
            {
                OnSignificantDistanceChange?.Invoke(objectId, oldDistance, newDistance);
            }
        }

        /// <summary>
        /// Force distance update for all objects
        /// </summary>
        public void ForceUpdateAllDistances()
        {
            if (!IsEnabled) return;

            foreach (var objectId in _objectTransforms.Keys)
            {
                ForceUpdateDistance(objectId);
            }
        }

        /// <summary>
        /// Set custom viewer transform
        /// </summary>
        public void SetViewer(Transform viewer)
        {
            _viewerTransform = viewer;
            _customViewer = viewer;
            _autoFindViewer = false;

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"Custom viewer set: {viewer?.name ?? "null"}", this);

            // Force distance recalculation
            ForceUpdateAllDistances();
        }

        /// <summary>
        /// Reset to automatic viewer finding
        /// </summary>
        public void ResetToAutoViewer()
        {
            _autoFindViewer = true;
            _customViewer = null;
            InitializeViewerReference();

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", "Reset to automatic viewer detection", this);

            // Force distance recalculation
            ForceUpdateAllDistances();
        }

        /// <summary>
        /// Get distance calculation statistics
        /// </summary>
        public DistanceCalculatorStats GetStats()
        {
            var stats = new DistanceCalculatorStats
            {
                TrackedObjects = _objectTransforms.Count,
                ViewerPosition = ViewerPosition,
                HasValidViewer = _viewerTransform != null,
                LastUpdateTime = _lastDistanceUpdate,
                UpdateInterval = _updateInterval
            };

            // Calculate average distance
            if (_objectDistances.Count > 0)
            {
                float totalDistance = 0f;
                foreach (var distance in _objectDistances.Values)
                {
                    totalDistance += distance;
                }
                stats.AverageDistance = totalDistance / _objectDistances.Count;
            }

            return stats;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _objectDistances.Clear();
                _lastCalculatedDistances.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("CONSTRUCTION", $"ConstructionLODDistanceCalculator: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize viewer reference
        /// </summary>
        private void InitializeViewerReference()
        {
            if (_customViewer != null)
            {
                _viewerTransform = _customViewer;
            }
            else if (_autoFindViewer)
            {
                var mainCamera = UnityEngine.Camera.main;
                _viewerTransform = mainCamera != null ? mainCamera.transform : transform;
            }

            if (_viewerTransform == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONSTRUCTION", "No valid viewer found for distance calculation", this);
            }
        }

        /// <summary>
        /// Update distance calculations for all objects
        /// </summary>
        private void UpdateDistanceCalculations()
        {
            if (_viewerTransform == null)
            {
                InitializeViewerReference();
                return;
            }

            var currentViewerPosition = _viewerTransform.position;

            // Check if viewer moved significantly
            if (Vector3.Distance(currentViewerPosition, _lastViewerPosition) > 0.1f)
            {
                OnViewerPositionChanged?.Invoke(currentViewerPosition);
                _lastViewerPosition = currentViewerPosition;

                // Update all distances if viewer moved
                foreach (var kvp in _objectTransforms)
                {
                    UpdateObjectDistance(kvp.Key, kvp.Value, currentViewerPosition);
                }
            }
            else
            {
                // Update distances in batches for performance
                ProcessDistanceUpdateQueue(currentViewerPosition);
            }
        }

        /// <summary>
        /// Update distance for specific object
        /// </summary>
        private void UpdateObjectDistance(string objectId, Transform objectTransform, Vector3 viewerPosition)
        {
            if (objectTransform == null)
            {
                UnregisterObject(objectId);
                return;
            }

            var newDistance = CalculateDistanceFast(objectTransform.position, viewerPosition);
            var oldDistance = _lastCalculatedDistances.GetValueOrDefault(objectId, float.MaxValue);

            // Only update if distance changed significantly
            if (Mathf.Abs(newDistance - oldDistance) > DISTANCE_CHANGE_THRESHOLD)
            {
                _objectDistances[objectId] = newDistance;
                _lastCalculatedDistances[objectId] = newDistance;

                OnDistanceCalculated?.Invoke(objectId, newDistance);
                OnSignificantDistanceChange?.Invoke(objectId, oldDistance, newDistance);
            }
        }

        /// <summary>
        /// Process distance update queue in batches
        /// </summary>
        private void ProcessDistanceUpdateQueue(Vector3 viewerPosition)
        {
            _distanceUpdateQueue.Clear();
            _distanceUpdateQueue.AddRange(_objectTransforms.Keys);

            // Process a subset each frame for performance
            int maxUpdatesPerFrame = Mathf.Max(1, _distanceUpdateQueue.Count / 10);
            int processed = 0;

            foreach (var objectId in _distanceUpdateQueue)
            {
                if (processed >= maxUpdatesPerFrame) break;

                if (_objectTransforms.TryGetValue(objectId, out var objectTransform))
                {
                    UpdateObjectDistance(objectId, objectTransform, viewerPosition);
                    processed++;
                }
            }
        }

        /// <summary>
        /// Calculate distance between two points
        /// </summary>
        private float CalculateDistance(Vector3 objectPosition)
        {
            if (_viewerTransform == null) return float.MaxValue;

            return CalculateDistanceFast(objectPosition, _viewerTransform.position);
        }

        /// <summary>
        /// Fast distance calculation (uses squared distance if enabled)
        /// </summary>
        private float CalculateDistanceFast(Vector3 objectPosition, Vector3 viewerPosition)
        {
            if (_useSquaredDistance)
            {
                // Use squared distance for performance, convert to actual distance only when needed
                var distanceSquared = (objectPosition - viewerPosition).sqrMagnitude;
                return Mathf.Sqrt(distanceSquared);
            }
            else
            {
                return Vector3.Distance(objectPosition, viewerPosition);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Distance calculator statistics
    /// </summary>
    [System.Serializable]
    public struct DistanceCalculatorStats
    {
        public int TrackedObjects;
        public Vector3 ViewerPosition;
        public bool HasValidViewer;
        public float LastUpdateTime;
        public float UpdateInterval;
        public float AverageDistance;
    }

    #endregion
}