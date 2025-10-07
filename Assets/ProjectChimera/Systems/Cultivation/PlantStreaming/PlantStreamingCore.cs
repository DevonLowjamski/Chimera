using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Data.Shared;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation.PlantStreaming
{
    /// <summary>
    /// REFACTORED: Core Plant Streaming System
    /// Central coordination for plant streaming and LOD with focused responsibility
    /// </summary>
    public class PlantStreamingCore : MonoBehaviour, ITickable
    {
        [Header("Core Streaming Settings")]
        [SerializeField] private bool _enablePlantStreaming = true;
        [SerializeField] private bool _enablePlantLOD = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 0.3f;

        // Plant streaming subsystems
        private PlantStreamingManager _streamingManager;
        private PlantLODController _lodController;
        private PlantRegistrationManager _registrationManager;
        private PlantStreamingPerformanceMonitor _performanceMonitor;

        // Core state
        private readonly Dictionary<string, StreamedPlant> _streamedPlants = new Dictionary<string, StreamedPlant>();
        private Transform _viewerTransform;
        private float _lastUpdate;

        // Properties
        public bool IsInitialized { get; private set; }
        public int RegisteredPlantCount => _streamedPlants.Count;
        public Transform ViewerTransform => _viewerTransform;
        public PlantStreamingManager StreamingManager => _streamingManager;
        public PlantLODController LODController => _lodController;
        public PlantRegistrationManager RegistrationManager => _registrationManager;
        public PlantStreamingPerformanceMonitor PerformanceMonitor => _performanceMonitor;

        // ITickable implementation
        public int TickPriority => 150; // After core systems, before rendering
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsInitialized;

        // Events
        public System.Action<string, StreamedPlant> OnPlantRegistered;
        public System.Action<string> OnPlantUnregistered;
        public System.Action<string, bool> OnPlantStreamingChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (IsInitialized) return;

            InitializeViewerTransform();
            InitializeSubsystems();

            _lastUpdate = Time.time;
            IsInitialized = true;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "✅ Plant Streaming Core initialized", this);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            if (Time.time - _lastUpdate >= _updateInterval)
            {
                UpdatePlantStreaming();
                _lastUpdate = Time.time;
            }
        }

        /// <summary>
        /// Register plant for streaming and LOD
        /// </summary>
        public void RegisterPlant(PlantInstance plantData, Vector3 position)
        {
            if (!IsInitialized || plantData == null) return;

            string plantId = plantData.PlantId;
            if (_streamedPlants.ContainsKey(plantId))
            {
                UpdatePlantPosition(plantId, position);
                return;
            }

            var streamedPlant = new StreamedPlant
            {
                PlantData = plantData,
                Position = position,
                IsLoaded = false,
                IsVisible = false,
                CurrentLODLevel = -1,
                LastUpdateTime = Time.time,
                DistanceFromViewer = float.MaxValue,
                StreamingPriority = ConvertFloatToPriority(CalculateStreamingPriority(plantData, position))
            };

            _streamedPlants[plantId] = streamedPlant;

            // Delegate to registration manager
            _registrationManager?.RegisterPlant(plantId, streamedPlant);

            OnPlantRegistered?.Invoke(plantId, streamedPlant);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant registered: {plantId} at {position}", this);
        }

        /// <summary>
        /// Unregister plant from streaming and LOD
        /// </summary>
        public void UnregisterPlant(string plantId)
        {
            if (!_streamedPlants.ContainsKey(plantId)) return;

            var streamedPlant = _streamedPlants[plantId];

            // Delegate to subsystems for cleanup
            _streamingManager?.UnloadPlant(plantId);
            _lodController?.UnregisterFromLOD(streamedPlant);
            _registrationManager?.UnregisterPlant(plantId);

            _streamedPlants.Remove(plantId);

            OnPlantUnregistered?.Invoke(plantId);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant unregistered: {plantId}", this);
        }

        /// <summary>
        /// Update plant position
        /// </summary>
        public void UpdatePlantPosition(string plantId, Vector3 newPosition)
        {
            if (_streamedPlants.TryGetValue(plantId, out var streamedPlant))
            {
                streamedPlant.Position = newPosition;
                _streamedPlants[plantId] = streamedPlant;

                // Update in subsystems
                _registrationManager?.UpdatePlantPosition(plantId, newPosition);
            }
        }

        /// <summary>
        /// Force load plants in area
        /// </summary>
        public void ForceLoadPlantsInArea(Vector3 center, float radius)
        {
            var plantsToLoad = new List<string>();

            foreach (var kvp in _streamedPlants)
            {
                var streamedPlant = kvp.Value;
                float distance = Vector3.Distance(center, streamedPlant.Position);

                if (distance <= radius && !streamedPlant.IsLoaded)
                {
                    plantsToLoad.Add(kvp.Key);
                }
            }

            _streamingManager?.ForceLoadPlants(plantsToLoad);
        }

        /// <summary>
        /// Get streaming statistics
        /// </summary>
        public PlantStreamingStats GetStreamingStats()
        {
            return _performanceMonitor?.CurrentStats ?? new PlantStreamingStats();
        }

        /// <summary>
        /// Enable/disable plant streaming
        /// </summary>
        public void SetPlantStreamingEnabled(bool enabled)
        {
            _enablePlantStreaming = enabled;
            _streamingManager?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant streaming: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Enable/disable plant LOD
        /// </summary>
        public void SetPlantLODEnabled(bool enabled)
        {
            _enablePlantLOD = enabled;
            _lodController?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant LOD: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Get streamed plant by ID
        /// </summary>
        public StreamedPlant? GetStreamedPlant(string plantId)
        {
            return _streamedPlants.TryGetValue(plantId, out var plant) ? plant : null;
        }

        /// <summary>
        /// Get all streamed plants
        /// </summary>
        public IEnumerable<KeyValuePair<string, StreamedPlant>> GetAllStreamedPlants()
        {
            return _streamedPlants;
        }

        private void InitializeViewerTransform()
        {
            var mainCamera = UnityEngine.Camera.main;
            _viewerTransform = mainCamera != null ? mainCamera.transform : transform;
        }

        private void InitializeSubsystems()
        {
            // Initialize streaming manager
            var streamingGO = new GameObject("PlantStreamingManager");
            streamingGO.transform.SetParent(transform);
            _streamingManager = streamingGO.AddComponent<PlantStreamingManager>();

            // Initialize LOD controller
            var lodGO = new GameObject("PlantLODController");
            lodGO.transform.SetParent(transform);
            _lodController = lodGO.AddComponent<PlantLODController>();

            // Initialize registration manager
            var registrationGO = new GameObject("PlantRegistrationManager");
            registrationGO.transform.SetParent(transform);
            _registrationManager = registrationGO.AddComponent<PlantRegistrationManager>();

            // Initialize performance monitor
            var performanceGO = new GameObject("PlantStreamingPerformanceMonitor");
            performanceGO.transform.SetParent(transform);
            _performanceMonitor = performanceGO.AddComponent<PlantStreamingPerformanceMonitor>();
        }

        private void UpdatePlantStreaming()
        {
            if (_viewerTransform == null) return;

            var viewerPosition = _viewerTransform.position;

            // Update distances and streaming decisions
            foreach (var kvp in _streamedPlants)
            {
                var plantId = kvp.Key;
                var streamedPlant = kvp.Value;

                var distance = Vector3.Distance(viewerPosition, streamedPlant.Position);
                streamedPlant.DistanceFromViewer = distance;
                streamedPlant.LastUpdateTime = Time.time;

                _streamedPlants[plantId] = streamedPlant;
            }

            // Delegate updates to subsystems
            _streamingManager?.UpdateStreaming(_streamedPlants, viewerPosition);
            _lodController?.UpdateLOD(_streamedPlants, viewerPosition);
            _performanceMonitor?.UpdateStats(_streamedPlants);
        }

        private float CalculateStreamingPriority(PlantInstance plantData, Vector3 position)
        {
            // Higher priority for more mature plants
            float stagePriority = (float)plantData.CurrentStage / (float)PlantGrowthStage.Mature;

            // Higher priority for plants closer to typical viewing areas
            float distancePenalty = Vector3.Distance(position, Vector3.zero) / 100f;

            return Mathf.Clamp01(stagePriority - distancePenalty * 0.1f);
        }

        /// <summary>
        /// Convert float priority (0-1) to StreamingPriority enum
        /// </summary>
        private ProjectChimera.Core.Streaming.Core.StreamingPriority ConvertFloatToPriority(float priority)
        {
            if (priority >= 0.8f) return ProjectChimera.Core.Streaming.Core.StreamingPriority.Critical;
            if (priority >= 0.6f) return ProjectChimera.Core.Streaming.Core.StreamingPriority.High;
            if (priority >= 0.4f) return ProjectChimera.Core.Streaming.Core.StreamingPriority.Medium;
            if (priority >= 0.2f) return ProjectChimera.Core.Streaming.Core.StreamingPriority.Low;
            return ProjectChimera.Core.Streaming.Core.StreamingPriority.VeryLow;
        }

        private void OnDestroy()
        {
            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "Plant Streaming Core destroyed", this);
        }
    }
}