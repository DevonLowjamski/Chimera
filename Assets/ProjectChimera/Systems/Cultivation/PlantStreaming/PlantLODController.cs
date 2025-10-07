using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Streaming;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Core.Updates;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation.PlantStreaming
{
    /// <summary>
    /// REFACTORED: Plant LOD Controller
    /// Focused component for managing Level-of-Detail adjustments for plants
    /// </summary>
    public class PlantLODController : MonoBehaviour, ITickable
    {
        [Header("LOD Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private PlantLODSettings[] _plantLODSettings;
        [SerializeField] private int _maxLODUpdatesPerFrame = 10;

        // LOD update tracking
        private int _lodUpdatesThisFrame;
        private readonly Queue<string> _lodUpdateQueue = new Queue<string>();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int LODUpdateQueueSize => _lodUpdateQueue.Count;

        // Events
        public System.Action<string, int, int> OnPlantLODChanged;

        // ITickable implementation
        public int TickPriority => 8; // LOD updates should happen after performance monitoring
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled;

        private void Start()
        {
            Initialize();
            RegisterWithUpdateOrchestrator();
        }

        private void Initialize()
        {
            InitializeLODSettings();

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "✅ Plant LOD Controller initialized", this);
        }

        public void Tick(float deltaTime)
        {
            ProcessLODUpdateQueue();
        }

        /// <summary>
        /// Update LOD for all streamed plants
        /// </summary>
        public void UpdateLOD(Dictionary<string, StreamedPlant> streamedPlants, Vector3 viewerPosition)
        {
            if (!IsEnabled) return;

            foreach (var kvp in streamedPlants)
            {
                var plantId = kvp.Key;
                var streamedPlant = kvp.Value;

                if (!streamedPlant.IsLoaded || streamedPlant.PlantGameObject == null) continue;

                var newLODLevel = CalculateLODLevel(streamedPlant);

                if (newLODLevel != streamedPlant.CurrentLODLevel)
                {
                    _lodUpdateQueue.Enqueue(plantId);
                }
            }
        }

        /// <summary>
        /// Register plant with LOD system
        /// </summary>
        public void RegisterWithLOD(StreamedPlant streamedPlant)
        {
            if (!IsEnabled || streamedPlant.PlantGameObject == null) return;

            // Register with the core LOD system
            if (LODManager.Instance != null)
            {
                var lodObjectId = LODManager.Instance.RegisterLODObject(
                    streamedPlant.PlantGameObject,
                    LODObjectType.Plant,
                    GetPlantLODBias(streamedPlant.PlantData)
                );

                streamedPlant.LODObjectId = lodObjectId;

                if (_enableLogging)
                    ChimeraLogger.Log("CULTIVATION", $"Plant registered with LOD system: {streamedPlant.PlantData.PlantId}", this);
            }
        }

        /// <summary>
        /// Unregister plant from LOD system
        /// </summary>
        public void UnregisterFromLOD(StreamedPlant streamedPlant)
        {
            if (streamedPlant.LODObjectId >= 0 && LODManager.Instance != null)
            {
                LODManager.Instance.UnregisterLODObject(streamedPlant.LODObjectId);
                streamedPlant.LODObjectId = -1;

                if (_enableLogging)
                    ChimeraLogger.Log("CULTIVATION", $"Plant unregistered from LOD system: {streamedPlant.PlantData.PlantId}", this);
            }
        }

        /// <summary>
        /// Set LOD enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _lodUpdateQueue.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant LOD controller: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Force LOD update for specific plant
        /// </summary>
        public void ForceUpdateLOD(string plantId)
        {
            if (IsEnabled)
            {
                _lodUpdateQueue.Enqueue(plantId);
            }
        }

        /// <summary>
        /// Set LOD settings for specific growth stage
        /// </summary>
        public void SetLODSettings(PlantGrowthStage stage, PlantLODSettings settings)
        {
            if (_plantLODSettings == null) InitializeLODSettings();

            // Find and update settings for the growth stage
            for (int i = 0; i < _plantLODSettings.Length; i++)
            {
                if (_plantLODSettings[i].GrowthStage == stage)
                {
                    _plantLODSettings[i] = settings;
                    if (_enableLogging)
                        ChimeraLogger.Log("CULTIVATION", $"LOD settings updated for stage: {stage}", this);
                    return;
                }
            }
        }

        private void ProcessLODUpdateQueue()
        {
            _lodUpdatesThisFrame = 0;

            while (_lodUpdateQueue.Count > 0 && _lodUpdatesThisFrame < _maxLODUpdatesPerFrame)
            {
                var plantId = _lodUpdateQueue.Dequeue();
                ProcessPlantLODUpdate(plantId);
                _lodUpdatesThisFrame++;
            }
        }

        private void ProcessPlantLODUpdate(string plantId)
        {
            // In a complete implementation, this would update the plant's LOD
            // For now, we'll focus on the architectural structure

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"LOD updated for plant: {plantId}", this);
        }

        private int CalculateLODLevel(StreamedPlant streamedPlant)
        {
            var lodSettings = GetLODSettingsForStage(streamedPlant.PlantData.CurrentStage);
            if (lodSettings.LODDistances == null || lodSettings.LODDistances.Length == 0)
                return 0;

            float distance = streamedPlant.DistanceFromViewer;

            // Apply plant-specific bias
            float plantBias = GetPlantLODBias(streamedPlant.PlantData);
            distance /= plantBias;

            for (int i = 0; i < lodSettings.LODDistances.Length; i++)
            {
                if (distance <= lodSettings.LODDistances[i])
                {
                    return i;
                }
            }

            return lodSettings.LODDistances.Length; // Maximum LOD level
        }

        private float GetPlantLODBias(PlantInstance plantData)
        {
            // More mature plants get better LOD at distance
            switch (plantData.CurrentStage)
            {
                case PlantGrowthStage.Seedling:
                    return 0.5f; // Seedlings are small, can use lower LOD sooner
                case PlantGrowthStage.Vegetative:
                    return 0.8f;
                case PlantGrowthStage.Flowering:
                    return 1.2f; // Flowering plants are important to show detail
                case PlantGrowthStage.Mature:
                    return 1.5f; // Mature plants should maintain quality at distance
                default:
                    return 1f;
            }
        }

        private PlantLODSettings GetLODSettingsForStage(PlantGrowthStage stage)
        {
            if (_plantLODSettings != null)
            {
                foreach (var settings in _plantLODSettings)
                {
                    if (settings.GrowthStage == stage)
                        return settings;
                }
            }

            // Return default settings if not found
            return new PlantLODSettings
            {
                GrowthStage = stage,
                LODDistances = new float[] { 15f, 35f, 70f },
                CullingDistance = 100f,
                EnableShadows = new bool[] { true, false, false },
                ParticleCount = new int[] { 20, 10, 0 }
            };
        }

        private void InitializeLODSettings()
        {
            if (_plantLODSettings == null || _plantLODSettings.Length == 0)
            {
                _plantLODSettings = new PlantLODSettings[]
                {
                    new PlantLODSettings
                    {
                        GrowthStage = PlantGrowthStage.Seedling,
                        LODDistances = new float[] { 10f, 25f, 50f },
                        CullingDistance = 75f,
                        EnableShadows = new bool[] { true, false, false },
                        ParticleCount = new int[] { 10, 5, 0 }
                    },
                    new PlantLODSettings
                    {
                        GrowthStage = PlantGrowthStage.Vegetative,
                        LODDistances = new float[] { 15f, 35f, 70f },
                        CullingDistance = 100f,
                        EnableShadows = new bool[] { true, true, false },
                        ParticleCount = new int[] { 20, 10, 0 }
                    },
                    new PlantLODSettings
                    {
                        GrowthStage = PlantGrowthStage.Flowering,
                        LODDistances = new float[] { 20f, 45f, 90f },
                        CullingDistance = 120f,
                        EnableShadows = new bool[] { true, true, false },
                        ParticleCount = new int[] { 30, 15, 5 }
                    },
                    new PlantLODSettings
                    {
                        GrowthStage = PlantGrowthStage.Mature,
                        LODDistances = new float[] { 25f, 55f, 110f },
                        CullingDistance = 150f,
                        EnableShadows = new bool[] { true, true, true },
                        ParticleCount = new int[] { 40, 20, 10 }
                    }
                };
            }
        }

        /// <summary>
        /// Get LOD controller performance stats
        /// </summary>
        public PlantLODControllerStats GetPerformanceStats()
        {
            return new PlantLODControllerStats
            {
                IsEnabled = IsEnabled,
                LODUpdateQueueSize = _lodUpdateQueue.Count,
                LODUpdatesThisFrame = _lodUpdatesThisFrame,
                MaxLODUpdatesPerFrame = _maxLODUpdatesPerFrame,
                ConfiguredStagesCount = _plantLODSettings?.Length ?? 0
            };
        }

        private void RegisterWithUpdateOrchestrator()
        {
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.RegisterTickable(this);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "Plant LOD Controller registered with UpdateOrchestrator", this);
        }

        private void OnDestroy()
        {
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.UnregisterTickable(this);
        }

        public void OnRegistered() { }
        public void OnUnregistered() { }
    }

    /// <summary>
    /// Plant LOD settings for specific growth stage
    /// </summary>
    [System.Serializable]
    public struct PlantLODSettings
    {
        public PlantGrowthStage GrowthStage;
        public float[] LODDistances;
        public float CullingDistance;
        public bool[] EnableShadows;
        public int[] ParticleCount;
    }

    /// <summary>
    /// Plant LOD controller performance statistics
    /// </summary>
    [System.Serializable]
    public struct PlantLODControllerStats
    {
        public bool IsEnabled;
        public int LODUpdateQueueSize;
        public int LODUpdatesThisFrame;
        public int MaxLODUpdatesPerFrame;
        public int ConfiguredStagesCount;
    }
}