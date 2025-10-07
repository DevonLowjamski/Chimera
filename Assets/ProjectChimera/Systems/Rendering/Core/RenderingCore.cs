using UnityEngine;
using UnityEngine.Rendering.Universal;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using RenderingQuality = ProjectChimera.Systems.Rendering.RenderingQuality;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Core Rendering System
    /// Central coordination for all rendering subsystems with focused responsibility
    /// </summary>
    public class RenderingCore : MonoBehaviour, ITickable
    {
        [Header("Core Rendering Settings")]
        [SerializeField] private bool _enableAdvancedRendering = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private RenderingQuality _renderingQuality = RenderingQuality.High;
        [SerializeField] private float _updateInterval = 0.1f;

        // Rendering subsystems
        private RenderPipelineController _pipelineController;
        private PlantRenderingManager _plantRenderingManager;
        private ShaderMaterialManager _shaderMaterialManager;
        private RenderingPerformanceMonitor _performanceMonitor;
        private LightingPostProcessController _lightingController;

        // Core state
        private float _lastUpdate;

        // Properties
        public bool IsInitialized { get; private set; }
        public RenderingQuality CurrentQuality => _renderingQuality;
        public RenderPipelineController PipelineController => _pipelineController;
        public PlantRenderingManager PlantRenderingManager => _plantRenderingManager;
        public ShaderMaterialManager ShaderMaterialManager => _shaderMaterialManager;
        public RenderingPerformanceMonitor PerformanceMonitor => _performanceMonitor;
        public LightingPostProcessController LightingController => _lightingController;

        // ITickable implementation
        public int TickPriority => 200; // After core systems, before specific rendering
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsInitialized;

        // Events
        public System.Action<RenderingQuality> OnQualityChanged;
        public System.Action<RenderingPerformanceStats> OnPerformanceUpdate;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (IsInitialized) return;

            InitializeSubsystems();
            _lastUpdate = Time.unscaledTime;
            IsInitialized = true;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Rendering Core initialized", this);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            if (Time.unscaledTime - _lastUpdate >= _updateInterval)
            {
                UpdateRenderingSystems();
                _lastUpdate = Time.unscaledTime;
            }
        }

        /// <summary>
        /// Set rendering quality level
        /// </summary>
        public void SetRenderingQuality(RenderingQuality quality)
        {
            if (_renderingQuality != quality)
            {
                _renderingQuality = quality;
                _pipelineController?.ApplyRenderingQuality(quality);
                OnQualityChanged?.Invoke(quality);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Rendering quality changed to: {quality}", this);
            }
        }

        /// <summary>
        /// Register plant for rendering
        /// </summary>
        public void RegisterPlant(GameObject plantObject, PlantRenderingData renderData)
        {
            _plantRenderingManager?.RegisterPlant(plantObject, renderData);
        }

        /// <summary>
        /// Unregister plant from rendering
        /// </summary>
        public void UnregisterPlant(GameObject plantObject)
        {
            _plantRenderingManager?.UnregisterPlant(plantObject);
        }

        /// <summary>
        /// Update plant rendering data
        /// </summary>
        public void UpdatePlantRenderingData(GameObject plantObject, PlantRenderingData renderData)
        {
            _plantRenderingManager?.UpdatePlantData(plantObject, renderData);
        }

        /// <summary>
        /// Get shared material for optimization
        /// </summary>
        public Material GetSharedMaterial(string materialKey, Shader shader, params object[] properties)
        {
            return _shaderMaterialManager?.GetSharedMaterial(materialKey, shader, properties);
        }

        /// <summary>
        /// Get rendering performance report
        /// </summary>
        public RenderingReport GetRenderingReport()
        {
            var report = new RenderingReport();

            if (_performanceMonitor != null)
            {
                var perfStats = _performanceMonitor.CurrentStats;
                report.PerformanceStats = perfStats;
            }

            if (_plantRenderingManager != null)
            {
                var plantStats = _plantRenderingManager.GetRenderingStats();
                report.PlantRenderingStats = plantStats;
            }

            if (_shaderMaterialManager != null)
            {
                var shaderStats = _shaderMaterialManager.GetShaderStats();
                report.ShaderStats = shaderStats;
            }

            return report;
        }

        /// <summary>
        /// Enable/disable advanced rendering
        /// </summary>
        public void SetAdvancedRenderingEnabled(bool enabled)
        {
            _enableAdvancedRendering = enabled;
            _pipelineController?.SetAdvancedRenderingEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Advanced rendering: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void InitializeSubsystems()
        {
            // Initialize pipeline controller
            var pipelineGO = new GameObject("RenderPipelineController");
            pipelineGO.transform.SetParent(transform);
            _pipelineController = pipelineGO.AddComponent<RenderPipelineController>();

            // Initialize plant rendering manager
            var plantRenderingGO = new GameObject("PlantRenderingManager");
            plantRenderingGO.transform.SetParent(transform);
            _plantRenderingManager = plantRenderingGO.AddComponent<PlantRenderingManager>();

            // Initialize shader/material manager
            var shaderGO = new GameObject("ShaderMaterialManager");
            shaderGO.transform.SetParent(transform);
            _shaderMaterialManager = shaderGO.AddComponent<ShaderMaterialManager>();

            // Initialize performance monitor
            var performanceGO = new GameObject("RenderingPerformanceMonitor");
            performanceGO.transform.SetParent(transform);
            _performanceMonitor = performanceGO.AddComponent<RenderingPerformanceMonitor>();

            // Initialize lighting/post-process controller
            var lightingGO = new GameObject("LightingPostProcessController");
            lightingGO.transform.SetParent(transform);
            _lightingController = lightingGO.AddComponent<LightingPostProcessController>();
        }

        private void UpdateRenderingSystems()
        {
            // Delegate updates to subsystems
            _performanceMonitor?.UpdatePerformanceStats();

            // Check if quality adjustment is needed based on performance
            var perfStats = _performanceMonitor?.CurrentStats;
            if (perfStats.HasValue && _pipelineController != null)
            {
                _pipelineController.CheckPerformanceQuality(perfStats.Value);
            }

            OnPerformanceUpdate?.Invoke(perfStats ?? new RenderingPerformanceStats());
        }

        private void OnDestroy()
        {
            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "Rendering Core destroyed", this);
        }
    }


    /// <summary>
    /// Plant rendering data structure
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
        public Matrix4x4 TransformMatrix;
        public Vector4 PlantParameters;
        public Color PlantColor;
    }

    /// <summary>
    /// Comprehensive rendering report
    /// </summary>
    [System.Serializable]
    public struct RenderingReport
    {
        public RenderingPerformanceStats PerformanceStats;
        public PlantRenderingStats PlantRenderingStats;
        public ShaderStats ShaderStats;
        public System.DateTime GeneratedAt;

        // Additional properties for AdvancedRenderingManager compatibility
        public RenderingQuality QualityLevel;
        public string[] EnabledFeatures;
        public string SystemConfiguration;
        public System.DateTime LastUpdateTime;
    }

    /// <summary>
    /// Rendering performance statistics
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

        // Additional properties for AdvancedRenderingManager compatibility
        public int PlantInstanceCount;
        public int ManagedLightCount;
        public int MaterialCount;
        public int ShaderSwitches;
        public float GPUMemoryUsageMB => GPUMemoryUsage / (1024f * 1024f);
        public float RenderingEfficiencyScore;
    }

    /// <summary>
    /// Plant rendering statistics
    /// </summary>
    [System.Serializable]
    public struct PlantRenderingStats
    {
        public int RegisteredPlants;
        public int InstancedPlants;
        public int VisiblePlants;
        public int CulledPlants;
        public int LODTransitions;
        public float InstancedRenderingEfficiency;
    }

    /// <summary>
    /// Shader and material statistics
    /// </summary>
    [System.Serializable]
    public struct ShaderStats
    {
        public int LoadedShaders;
        public int SharedMaterials;
        public int MaterialInstances;
        public float MaterialCacheHitRate;
        public long ShaderMemoryUsage;
    }
}