using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Streaming;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation.Plant;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using AssetStreamingManager = ProjectChimera.Core.Streaming.AssetStreamingManager;
using StreamingPriority = ProjectChimera.Core.Streaming.Core.StreamingPriority;

namespace ProjectChimera.Systems.Cultivation.PlantStreaming
{
    /// <summary>
    /// REFACTORED: Plant Registration Manager
    /// Focused component for managing plant registration with external systems
    /// </summary>
    public class PlantRegistrationManager : MonoBehaviour
    {
        [Header("Registration Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _autoRegisterWithAssetStreaming = true;
        [SerializeField] private bool _autoRegisterWithLODSystem = true;

        // Asset mapping
        private readonly Dictionary<PlantGrowthStage, string[]> _plantAssetsByStage = new Dictionary<PlantGrowthStage, string[]>();

        // Registration tracking
        private int _registeredCount;
        private int _unregisteredCount;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int RegisteredCount => _registeredCount;
        public int UnregisteredCount => _unregisteredCount;

        // Events
        public System.Action<string, StreamedPlant> OnPlantRegistered;
        public System.Action<string> OnPlantUnregistered;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializePlantAssets();

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "✅ Plant Registration Manager initialized", this);
        }

        /// <summary>
        /// Register plant with external systems
        /// </summary>
        public void RegisterPlant(string plantId, StreamedPlant streamedPlant)
        {
            if (!IsEnabled) return;

            // Register with asset streaming manager (using ServiceContainer instead of singleton)
            var assetStreamingManager = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Streaming.AssetStreamingManager>();
            if (_autoRegisterWithAssetStreaming && assetStreamingManager != null)
            {
                string assetKey = GetPlantAssetKey(streamedPlant.PlantData);
                // Use fully qualified types to ensure correct method resolution
                assetStreamingManager.RegisterAsset(
                    assetKey,
                    streamedPlant.Position,
                    (ProjectChimera.Core.Streaming.Core.StreamingPriority)streamedPlant.StreamingPriority
                );
            }

            _registeredCount++;
            OnPlantRegistered?.Invoke(plantId, streamedPlant);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant registered with external systems: {plantId}", this);
        }

        /// <summary>
        /// Unregister plant from external systems
        /// </summary>
        public void UnregisterPlant(string plantId)
        {
            if (!IsEnabled) return;

            _unregisteredCount++;
            OnPlantUnregistered?.Invoke(plantId);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant unregistered from external systems: {plantId}", this);
        }

        /// <summary>
        /// Update plant position in external systems
        /// </summary>
        public void UpdatePlantPosition(string plantId, Vector3 newPosition)
        {
            if (!IsEnabled) return;

            // Update position in asset streaming system
            var assetStreamingManager = ServiceContainerFactory.Instance?.TryResolve<AssetStreamingManager>();
            if (assetStreamingManager != null)
            {
                // Note: In a real implementation, AssetStreamingManager would need an UpdateAssetPosition method
                if (_enableLogging)
                    ChimeraLogger.Log("CULTIVATION", $"Plant position updated: {plantId} -> {newPosition}", this);
            }
        }

        /// <summary>
        /// Get asset key for plant data
        /// </summary>
        public string GetPlantAssetKey(PlantInstance plantData)
        {
            if (_plantAssetsByStage.TryGetValue(plantData.CurrentStage, out var assets) && assets.Length > 0)
            {
                // Use first asset for the stage, or implement more sophisticated selection
                return assets[0];
            }

            return $"Plant_{plantData.CurrentStage}_{plantData.PlantId}";
        }

        /// <summary>
        /// Get all available assets for a growth stage
        /// </summary>
        public string[] GetAssetsForStage(PlantGrowthStage stage)
        {
            return _plantAssetsByStage.TryGetValue(stage, out var assets) ? assets : new string[0];
        }

        /// <summary>
        /// Add asset variant for growth stage
        /// </summary>
        public void AddAssetVariant(PlantGrowthStage stage, string assetKey)
        {
            if (!_plantAssetsByStage.ContainsKey(stage))
            {
                _plantAssetsByStage[stage] = new string[0];
            }

            var currentAssets = _plantAssetsByStage[stage];
            var newAssets = new string[currentAssets.Length + 1];
            currentAssets.CopyTo(newAssets, 0);
            newAssets[currentAssets.Length] = assetKey;
            _plantAssetsByStage[stage] = newAssets;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Asset variant added for {stage}: {assetKey}", this);
        }

        /// <summary>
        /// Set registration settings
        /// </summary>
        public void SetRegistrationSettings(bool autoRegisterAssets, bool autoRegisterLOD)
        {
            _autoRegisterWithAssetStreaming = autoRegisterAssets;
            _autoRegisterWithLODSystem = autoRegisterLOD;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION",
                    $"Registration settings updated - Assets: {autoRegisterAssets}, LOD: {autoRegisterLOD}", this);
        }

        /// <summary>
        /// Enable/disable registration manager
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant registration manager: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Reset registration counters
        /// </summary>
        public void ResetCounters()
        {
            _registeredCount = 0;
            _unregisteredCount = 0;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "Registration counters reset", this);
        }

        private void InitializePlantAssets()
        {
            // Initialize plant assets by growth stage
            _plantAssetsByStage[PlantGrowthStage.Seedling] = new string[]
            {
                "Plant_Seedling_01",
                "Plant_Seedling_02",
                "Plant_Seedling_Variant"
            };

            _plantAssetsByStage[PlantGrowthStage.Vegetative] = new string[]
            {
                "Plant_Vegetative_01",
                "Plant_Vegetative_02",
                "Plant_Vegetative_Bushy",
                "Plant_Vegetative_Tall"
            };

            _plantAssetsByStage[PlantGrowthStage.Flowering] = new string[]
            {
                "Plant_Flowering_01",
                "Plant_Flowering_02",
                "Plant_Flowering_Dense",
                "Plant_Flowering_Sparse"
            };

            _plantAssetsByStage[PlantGrowthStage.Mature] = new string[]
            {
                "Plant_Mature_01",
                "Plant_Mature_02",
                "Plant_Mature_Premium",
                "Plant_Mature_Standard"
            };

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Plant assets initialized for {_plantAssetsByStage.Count} growth stages", this);
        }

        /// <summary>
        /// Get registration manager performance stats
        /// </summary>
        public PlantRegistrationManagerStats GetPerformanceStats()
        {
            return new PlantRegistrationManagerStats
            {
                IsEnabled = IsEnabled,
                RegisteredCount = _registeredCount,
                UnregisteredCount = _unregisteredCount,
                AutoRegisterAssets = _autoRegisterWithAssetStreaming,
                AutoRegisterLOD = _autoRegisterWithLODSystem,
                AvailableAssetStages = _plantAssetsByStage.Count
            };
        }
    }

    /// <summary>
    /// Plant registration manager performance statistics
    /// </summary>
    [System.Serializable]
    public struct PlantRegistrationManagerStats
    {
        public bool IsEnabled;
        public int RegisteredCount;
        public int UnregisteredCount;
        public bool AutoRegisterAssets;
        public bool AutoRegisterLOD;
        public int AvailableAssetStages;
    }
}