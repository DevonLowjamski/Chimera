using UnityEngine;
using ProjectChimera.Core.Streaming;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Systems.Cultivation.Pooling;
using System.Collections.Generic;
using System.Collections;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Systems.Cultivation.PlantStreaming;
// Type alias removed - using fully qualified names for clarity

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// REFACTORED: Plant Streaming LOD Integration - Legacy Wrapper
    /// Delegates to PlantStreamingCore for focused coordination of plant streaming and LOD
    /// Maintains backward compatibility while using the new focused architecture
    /// </summary>
    public class PlantStreamingLODIntegration : MonoBehaviour, ITickable
    {
        [Header("Legacy Plant Streaming Settings")]
        [SerializeField] private bool _enableLogging = true;

        // Core streaming system (new focused architecture)
        private PlantStreamingCore _streamingCore;

        public bool IsInitialized => _streamingCore?.IsInitialized ?? false;
        public ProjectChimera.Systems.Cultivation.PlantStreaming.PlantStreamingStats Stats => _streamingCore != null ? _streamingCore.GetStreamingStats() : new ProjectChimera.Systems.Cultivation.PlantStreaming.PlantStreamingStats();

        /// <summary>
        /// Initialize plant streaming and LOD integration (LEGACY - delegates to PlantStreamingCore)
        /// </summary>
        public void Initialize()
        {
            InitializeStreamingCore();

            if (_enableLogging && _streamingCore?.IsInitialized == true)
            {
                ChimeraLogger.Log("CULTIVATION", "✅ Plant Streaming LOD Integration initialized (legacy wrapper)", this);
            }
        }

        /// <summary>
        /// Register plant for streaming and LOD (LEGACY - delegates to PlantStreamingCore)
        /// </summary>
        public void RegisterPlant(PlantInstance plantData, Vector3 position)
        {
            _streamingCore?.RegisterPlant(plantData, position);
        }

        /// <summary>
        /// Unregister plant from streaming and LOD (LEGACY - delegates to PlantStreamingCore)
        /// </summary>
        public void UnregisterPlant(string plantId)
        {
            _streamingCore?.UnregisterPlant(plantId);
        }

        /// <summary>
        /// Update plant position for streaming calculations (LEGACY - delegates to PlantStreamingCore)
        /// </summary>
        public void UpdatePlantPosition(string plantId, Vector3 newPosition)
        {
            _streamingCore?.UpdatePlantPosition(plantId, newPosition);
        }

        /// <summary>
        /// Update plant data (growth stage, health, etc.) (LEGACY - delegates to PlantStreamingCore)
        /// </summary>
        public void UpdatePlantData(string plantId, PlantInstance newData)
        {
            // For now, update position to trigger re-evaluation
            var streamedPlant = _streamingCore?.GetStreamedPlant(plantId);
            if (streamedPlant != null)
            {
                _streamingCore?.UpdatePlantPosition(plantId, streamedPlant.Position);
            }
        }

        /// <summary>
        /// Force load plants in area (LEGACY - delegates to PlantStreamingCore)
        /// </summary>
        public void ForceLoadPlantsInArea(Vector3 center, float radius)
        {
            _streamingCore?.ForceLoadPlantsInArea(center, radius);
        }

        /// <summary>
        /// Get streaming statistics (LEGACY - delegates to PlantStreamingCore)
        /// </summary>
        public ProjectChimera.Systems.Cultivation.PlantStreaming.PlantStreamingStats GetDetailedStats()
        {
            return _streamingCore != null ? _streamingCore.GetStreamingStats() : new ProjectChimera.Systems.Cultivation.PlantStreaming.PlantStreamingStats();
        }

        #region Private Methods

        /// <summary>
        /// Initialize the PlantStreamingCore system
        /// </summary>
        private void InitializeStreamingCore()
        {
            if (_streamingCore != null) return;

            // Create core streaming system
            var coreGO = new GameObject("PlantStreamingCore");
            coreGO.transform.SetParent(transform);
            _streamingCore = coreGO.AddComponent<PlantStreamingCore>();
        }

        #endregion

        // ITickable implementation - delegates to PlantStreamingCore
        public int TickPriority => _streamingCore?.TickPriority ?? 150;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && (_streamingCore?.IsTickable ?? false);

        public void Tick(float deltaTime)
        {
            // PlantStreamingCore handles its own ticking automatically
            // This wrapper doesn't need to do anything
        }

        private void Awake()
        {
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance.UnregisterTickable(this);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Plant Streaming LOD Integration destroyed (legacy wrapper)", this);
            }
        }
    }

    #region Legacy Data Structure Compatibility

    /// <summary>
    /// LEGACY: Plant streaming statistics - use PlantStreamingCore for new implementations
    /// </summary>
    [System.Serializable]
    public struct PlantStreamingStats
    {
        public int RegisteredPlants;
        public int LoadedPlants;
        public int VisiblePlants;
        public int LoadOperations;
        public int UnloadOperations;
        public int[] PlantsByLODLevel;
    }

    #endregion
}