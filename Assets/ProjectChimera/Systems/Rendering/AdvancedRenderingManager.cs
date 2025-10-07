using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using ProjectChimera.Core.Memory;
using System.Collections;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Systems.Rendering.Core;
using RenderingQuality = ProjectChimera.Systems.Rendering.RenderingQuality;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using CoreRenderingPerformanceStats = ProjectChimera.Systems.Rendering.Core.RenderingPerformanceStats;
using CorePlantRenderingData = ProjectChimera.Systems.Rendering.Core.PlantRenderingData;
using CoreRenderingReport = ProjectChimera.Systems.Rendering.Core.RenderingReport;

namespace ProjectChimera.Systems.Rendering
{
    /// <summary>
    /// REFACTORED: Advanced Rendering Manager - Legacy Wrapper
    /// Delegates to RenderingCore for focused coordination of rendering subsystems
    /// Maintains backward compatibility while using the new focused architecture
    /// </summary>
    public class AdvancedRenderingManager : MonoBehaviour, ITickable
    {
        [Header("Legacy Rendering Settings")]
        [SerializeField] private bool _enableLogging = false;

        // Core rendering system (new focused architecture)
        private RenderingCore _renderingCore;

        // Legacy singleton pattern support - prefer ServiceContainer resolution
        private static AdvancedRenderingManager _instance;
        public static AdvancedRenderingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Primary: Resolve from ServiceContainer
                    _instance = ServiceContainerFactory.Instance?.TryResolve<AdvancedRenderingManager>();

                    if (_instance == null)
                    {
                        var go = new GameObject("AdvancedRenderingManager");
                        _instance = go.AddComponent<AdvancedRenderingManager>();
                        DontDestroyOnLoad(go);

                        // Register in ServiceContainer
                        ServiceContainerFactory.Instance?.RegisterSingleton<AdvancedRenderingManager>(_instance);
                    }
                }
                return _instance;
            }
        }

        public bool IsInitialized => _renderingCore?.IsInitialized ?? false;
        public RenderingPerformanceStats Stats => ConvertPerformanceStats(_renderingCore?.GetRenderingReport().PerformanceStats ?? default);
        public RenderingQuality CurrentQuality => _renderingCore?.CurrentQuality ?? default;

        // Events - delegates to RenderingCore
        public System.Action<RenderingQuality> OnQualityChanged
        {
            get => _renderingCore?.OnQualityChanged;
            set { if (_renderingCore != null) _renderingCore.OnQualityChanged = value; }
        }

        public System.Action<RenderingPerformanceStats> OnPerformanceUpdate
        {
            get => ConvertPerformanceStatsAction(_renderingCore?.OnPerformanceUpdate);
            set { if (_renderingCore != null) _renderingCore.OnPerformanceUpdate = ConvertToCorePerformanceStatsAction(value); }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }

            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize advanced rendering system (LEGACY - delegates to RenderingCore)
        /// </summary>
        public void Initialize()
        {
            InitializeRenderingCore();

            if (_enableLogging && _renderingCore?.IsInitialized == true)
            {
                ChimeraLogger.Log("RENDERING", "✅ Advanced Rendering Manager initialized (legacy wrapper)", this);
            }
        }

        /// <summary>
        /// Set rendering quality level (LEGACY - delegates to RenderingCore)
        /// </summary>
        public void SetRenderingQuality(RenderingQuality quality)
        {
            _renderingCore?.SetRenderingQuality(quality);
        }

        /// <summary>
        /// Register plant for instanced rendering (LEGACY - delegates to RenderingCore)
        /// </summary>
        public void RegisterPlantForRendering(GameObject plantObject, ProjectChimera.Systems.Rendering.PlantRenderingData renderData)
        {
            _renderingCore?.RegisterPlant(plantObject, ConvertToCorePlantRenderingData(renderData));
        }

        /// <summary>
        /// Unregister plant from instanced rendering (LEGACY - delegates to RenderingCore)
        /// </summary>
        public void UnregisterPlantFromRendering(GameObject plantObject)
        {
            _renderingCore?.UnregisterPlant(plantObject);
        }

        /// <summary>
        /// Update plant rendering data (LEGACY - delegates to RenderingCore)
        /// </summary>
        public void UpdatePlantRenderingData(GameObject plantObject, ProjectChimera.Systems.Rendering.PlantRenderingData renderData)
        {
            _renderingCore?.UpdatePlantRenderingData(plantObject, ConvertToCorePlantRenderingData(renderData));
        }

        /// <summary>
        /// Create shared material for optimization (LEGACY - delegates to RenderingCore)
        /// </summary>
        public Material GetSharedMaterial(string materialKey, Shader shader, params object[] properties)
        {
            return _renderingCore?.GetSharedMaterial(materialKey, shader, properties);
        }

        /// <summary>
        /// Get comprehensive rendering statistics (LEGACY - delegates to RenderingCore)
        /// </summary>
        public RenderingReport GetRenderingReport()
        {
            return ConvertRenderingReport(_renderingCore?.GetRenderingReport() ?? default);
        }

        /// <summary>
        /// Set advanced rendering enabled/disabled (LEGACY - delegates to RenderingCore)
        /// </summary>
        public void SetAdvancedRenderingEnabled(bool enabled)
        {
            _renderingCore?.SetAdvancedRenderingEnabled(enabled);
        }

        // ITickable implementation - delegates to RenderingCore
        public int TickPriority => _renderingCore?.TickPriority ?? 200;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && (_renderingCore?.IsTickable ?? false);

        public void Tick(float deltaTime)
        {
            // RenderingCore handles its own ticking automatically
            // This wrapper doesn't need to do anything
        }

        #region Private Methods

        /// <summary>
        /// Initialize the RenderingCore system
        /// </summary>
        private void InitializeRenderingCore()
        {
            if (_renderingCore != null) return;

            // Create core rendering system
            var coreGO = new GameObject("RenderingCore");
            coreGO.transform.SetParent(transform);
            _renderingCore = coreGO.AddComponent<RenderingCore>();
        }

        #endregion

        #region Type Conversion Methods

        /// <summary>
        /// Convert Core RenderingPerformanceStats to legacy format
        /// </summary>
        private RenderingPerformanceStats ConvertPerformanceStats(CoreRenderingPerformanceStats coreStats)
        {
            return new RenderingPerformanceStats
            {
                AverageFrameTime = coreStats.AverageFrameTime,
                MinFrameTime = coreStats.MinFrameTime,
                MaxFrameTime = coreStats.MaxFrameTime,
                DroppedFrames = coreStats.DroppedFrames,
                PlantInstanceCount = coreStats.PlantInstanceCount,
                ManagedLightCount = coreStats.ManagedLightCount,
                MaterialCount = coreStats.MaterialCount,
                ShaderSwitches = coreStats.ShaderSwitches,
                GPUMemoryUsageMB = coreStats.GPUMemoryUsageMB,
                RenderingEfficiencyScore = coreStats.RenderingEfficiencyScore
            };
        }

        /// <summary>
        /// Convert legacy PlantRenderingData to Core format
        /// </summary>
        private CorePlantRenderingData ConvertToCorePlantRenderingData(ProjectChimera.Systems.Rendering.PlantRenderingData legacyData)
        {
            return new CorePlantRenderingData
            {
                Position = legacyData.Position,
                Rotation = legacyData.Rotation,
                Scale = legacyData.Scale,
                LODLevel = legacyData.LODLevel,
                Health = legacyData.Health,
                GrowthStage = legacyData.GrowthStage,
                CastShadows = legacyData.CastShadows,
                ReceiveShadows = legacyData.ReceiveShadows,
                TransformMatrix = legacyData.TransformMatrix,
                PlantParameters = legacyData.PlantParameters,
                PlantColor = legacyData.PlantColor
            };
        }

        /// <summary>
        /// Convert Core RenderingReport to legacy format
        /// </summary>
        private RenderingReport ConvertRenderingReport(CoreRenderingReport coreReport)
        {
            return new RenderingReport
            {
                PerformanceStats = ConvertPerformanceStats(coreReport.PerformanceStats),
                QualityLevel = coreReport.QualityLevel,
                EnabledFeatures = coreReport.EnabledFeatures,
                SystemConfiguration = coreReport.SystemConfiguration,
                LastUpdateTime = coreReport.LastUpdateTime
            };
        }

        /// <summary>
        /// Convert Core performance stats action to legacy format
        /// </summary>
        private System.Action<RenderingPerformanceStats> ConvertPerformanceStatsAction(System.Action<CoreRenderingPerformanceStats> coreAction)
        {
            if (coreAction == null) return null;
            return (legacyStats) => coreAction(ConvertToCorePerfStats(legacyStats));
        }

        /// <summary>
        /// Convert legacy performance stats action to Core format
        /// </summary>
        private System.Action<CoreRenderingPerformanceStats> ConvertToCorePerformanceStatsAction(System.Action<RenderingPerformanceStats> legacyAction)
        {
            if (legacyAction == null) return null;
            return (coreStats) => legacyAction(ConvertPerformanceStats(coreStats));
        }

        /// <summary>
        /// Convert legacy performance stats to Core format
        /// </summary>
        private CoreRenderingPerformanceStats ConvertToCorePerfStats(RenderingPerformanceStats legacyStats)
        {
            return new CoreRenderingPerformanceStats
            {
                AverageFrameTime = legacyStats.AverageFrameTime,
                MinFrameTime = legacyStats.MinFrameTime,
                MaxFrameTime = legacyStats.MaxFrameTime,
                DroppedFrames = legacyStats.DroppedFrames,
                PlantInstanceCount = legacyStats.PlantInstanceCount,
                ManagedLightCount = legacyStats.ManagedLightCount,
                MaterialCount = legacyStats.MaterialCount,
                ShaderSwitches = legacyStats.ShaderSwitches,
                GPUMemoryUsage = legacyStats.GPUMemoryUsage,
                RenderingEfficiencyScore = legacyStats.RenderingEfficiencyScore
            };
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);

            if (_enableLogging)
            {
                ChimeraLogger.Log("RENDERING", "Advanced Rendering Manager destroyed (legacy wrapper)", this);
            }
        }
    }

    #region Legacy Data Structure Compatibility

    /// <summary>
    /// LEGACY: Plant rendering data - use RenderingCore for new implementations
    /// </summary>
    [System.Serializable]
    public struct PlantRenderingData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public int LODLevel;
        public float Health;
        public PlantGrowthStage GrowthStage;
        public bool CastShadows;
        public bool ReceiveShadows;

        // Additional properties needed by PlantInstancedRenderer
        public Matrix4x4 TransformMatrix;
        public Vector4 PlantParameters;
        public Color PlantColor;
    }


    /// <summary>
    /// LEGACY: Rendering performance statistics - use RenderingCore for new implementations
    /// </summary>
    [System.Serializable]
    public struct RenderingPerformanceStats
    {
        public float AverageFrameTime;
        public float MinFrameTime;
        public float MaxFrameTime;
        public int DroppedFrames;
        public float GPUMemoryUsage;
        public float CPUFrameTime;
        public float GPUFrameTime;
        public int DrawCalls;
        public int Triangles;
        public bool IsPerformingWell;
        public int RegisteredPlants;
        public int LoadedShaders;
        public float MonitoringStartTime;
        public int OptimizationEvents;

        // Additional properties for compatibility with Core
        public int PlantInstanceCount;
        public int ManagedLightCount;
        public int MaterialCount;
        public int ShaderSwitches;
        public float GPUMemoryUsageMB { get; set; }
        public float RenderingEfficiencyScore;
    }

    /// <summary>
    /// LEGACY: Rendering report - use RenderingCore for new implementations
    /// </summary>
    [System.Serializable]
    public struct RenderingReport
    {
        public RenderingPerformanceStats CurrentStats;
        public RenderingQuality CurrentQuality;
        public float AverageFrameTime;
        public float TargetFrameTime;
        public int RegisteredShaders;
        public int SharedMaterials;
        public bool IsPerformanceHealthy;
        public RenderingPerformanceStats PerformanceStats;
        public PlantRenderingStats PlantRenderingStats;
        public ShaderStats ShaderStats;
        public System.DateTime GeneratedAt;

        // Additional properties for compatibility
        public RenderingQuality QualityLevel;
        public string[] EnabledFeatures;
        public string SystemConfiguration;
        public System.DateTime LastUpdateTime;
    }

    // LEGACY: PlantGrowthStage enum now uses shared data structures from ProjectChimera.Data.Shared

    #endregion
}
