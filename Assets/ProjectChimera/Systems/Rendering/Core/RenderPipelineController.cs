using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using System.Linq;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Render Pipeline Controller
    /// Focused component for managing URP settings and rendering quality
    /// </summary>
    public class RenderPipelineController : MonoBehaviour
    {
        [Header("Pipeline Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableDynamicQuality = true;
        [SerializeField] private float _targetFrameRate = 60f;
        [SerializeField] private float _performanceCheckInterval = 2f;

        // Pipeline components
        private UniversalRenderPipelineAsset _urpAsset;
        private UnityEngine.Camera _mainCamera;
        private Light _mainLight;

        // Quality management
        private RenderingSettings _currentSettings;
        private RenderingSettings _targetSettings;
        private bool _settingsTransitioning = false;
        private float _lastPerformanceCheck;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public RenderingSettings CurrentSettings => _currentSettings;
        public float TargetFrameRate => _targetFrameRate;

        // Events
        public System.Action<RenderingQuality> OnQualityApplied;
        public System.Action<RenderingSettings> OnSettingsChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeRenderPipeline();
            _lastPerformanceCheck = Time.unscaledTime;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Render Pipeline Controller initialized", this);
        }

        /// <summary>
        /// Apply rendering quality settings
        /// </summary>
        public void ApplyRenderingQuality(RenderingQuality quality)
        {
            if (!IsEnabled) return;

            switch (quality)
            {
                case RenderingQuality.Low:
                    ApplyLowQualitySettings();
                    break;
                case RenderingQuality.Medium:
                    ApplyMediumQualitySettings();
                    break;
                case RenderingQuality.High:
                    ApplyHighQualitySettings();
                    break;
                case RenderingQuality.Ultra:
                    ApplyUltraQualitySettings();
                    break;
            }

            OnQualityApplied?.Invoke(quality);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Applied {quality} quality settings", this);
        }

        /// <summary>
        /// Check performance and adjust quality if needed
        /// </summary>
        public void CheckPerformanceQuality(RenderingPerformanceStats stats)
        {
            if (!_enableDynamicQuality || !IsEnabled) return;

            if (Time.unscaledTime - _lastPerformanceCheck < _performanceCheckInterval) return;

            _lastPerformanceCheck = Time.unscaledTime;

            // Determine if quality adjustment is needed
            bool needsOptimization = stats.AverageFrameTime > (1f / _targetFrameRate) * 1.2f; // 20% tolerance
            bool canImprove = stats.AverageFrameTime < (1f / _targetFrameRate) * 0.8f; // Room for improvement

            if (needsOptimization && !stats.IsPerformingWell)
            {
                ApplyEmergencyOptimizations();
            }
            else if (canImprove && stats.IsPerformingWell)
            {
                // Could potentially increase quality (implement if needed)
            }
        }

        /// <summary>
        /// Set advanced rendering enabled/disabled
        /// </summary>
        public void SetAdvancedRenderingEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ApplyLowQualitySettings();
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Advanced rendering pipeline: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Apply emergency performance optimizations
        /// </summary>
        public void ApplyEmergencyOptimizations()
        {
            if (_urpAsset != null)
            {
                // Reduce shadow quality
                _urpAsset.shadowDistance = Mathf.Min(_urpAsset.shadowDistance, 50f);
                _urpAsset.cascade4Split = Vector3.one * 0.1f;

                // Disable expensive features
                _urpAsset.supportsHDR = false;

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", "Applied emergency performance optimizations", this);
            }
        }

        /// <summary>
        /// Get current pipeline statistics
        /// </summary>
        public PipelineStats GetPipelineStats()
        {
            return new PipelineStats
            {
                IsEnabled = IsEnabled,
                CurrentQuality = GetCurrentQualityLevel(),
                ShadowDistance = _urpAsset?.shadowDistance ?? 0f,
                HDREnabled = _urpAsset?.supportsHDR ?? false,
                MSAALevel = (int)(_urpAsset?.msaaSampleCount ?? 1),
                TargetFrameRate = _targetFrameRate
            };
        }

        private void InitializeRenderPipeline()
        {
            // Get URP asset
            _urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            if (_urpAsset == null)
            {
                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", "⚠️ URP Asset not found - using default settings", this);
                return;
            }

            // Get main camera
            _mainCamera = UnityEngine.Camera.main;

            // Get main light via GameObjectRegistry
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var allLights = registry?.GetAll<Light>();
            if (allLights != null && allLights.Any())
            {
                _mainLight = allLights.FirstOrDefault();
            }
            else
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("RENDERING", "No main light found - ensure lights are registered with GameObjectRegistry", this);
            }

            // Initialize current settings
            _currentSettings = GetCurrentRenderingSettings();
        }

        private void ApplyLowQualitySettings()
        {
            if (_urpAsset != null)
            {
                _urpAsset.shadowDistance = 25f;
                _urpAsset.msaaSampleCount = (int)MsaaQuality.Disabled;
                _urpAsset.supportsHDR = false;
                _urpAsset.cascade4Split = Vector3.one * 0.05f;
            }

            if (_mainCamera != null)
            {
                var cameraData = _mainCamera.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.antialiasing = AntialiasingMode.None;
                }
            }

            _currentSettings = new RenderingSettings
            {
                Quality = RenderingQuality.Low,
                ShadowDistance = 25f,
                MSAALevel = 1,
                HDREnabled = false,
                AntiAliasing = false
            };

            OnSettingsChanged?.Invoke(_currentSettings);
        }

        private void ApplyMediumQualitySettings()
        {
            if (_urpAsset != null)
            {
                _urpAsset.shadowDistance = 75f;
                _urpAsset.msaaSampleCount = (int)MsaaQuality._2x;
                _urpAsset.supportsHDR = true;
                _urpAsset.cascade4Split = new Vector3(0.1f, 0.25f, 0.5f);
            }

            if (_mainCamera != null)
            {
                var cameraData = _mainCamera.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                }
            }

            _currentSettings = new RenderingSettings
            {
                Quality = RenderingQuality.Medium,
                ShadowDistance = 75f,
                MSAALevel = 2,
                HDREnabled = true,
                AntiAliasing = true
            };

            OnSettingsChanged?.Invoke(_currentSettings);
        }

        private void ApplyHighQualitySettings()
        {
            if (_urpAsset != null)
            {
                _urpAsset.shadowDistance = 150f;
                _urpAsset.msaaSampleCount = (int)MsaaQuality._4x;
                _urpAsset.supportsHDR = true;
                _urpAsset.cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
            }

            if (_mainCamera != null)
            {
                var cameraData = _mainCamera.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                }
            }

            _currentSettings = new RenderingSettings
            {
                Quality = RenderingQuality.High,
                ShadowDistance = 150f,
                MSAALevel = 4,
                HDREnabled = true,
                AntiAliasing = true
            };

            OnSettingsChanged?.Invoke(_currentSettings);
        }

        private void ApplyUltraQualitySettings()
        {
            if (_urpAsset != null)
            {
                _urpAsset.shadowDistance = 250f;
                _urpAsset.msaaSampleCount = (int)MsaaQuality._8x;
                _urpAsset.supportsHDR = true;
                _urpAsset.cascade4Split = new Vector3(0.05f, 0.15f, 0.3f);
            }

            if (_mainCamera != null)
            {
                var cameraData = _mainCamera.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                }
            }

            _currentSettings = new RenderingSettings
            {
                Quality = RenderingQuality.Ultra,
                ShadowDistance = 250f,
                MSAALevel = 8,
                HDREnabled = true,
                AntiAliasing = true
            };

            OnSettingsChanged?.Invoke(_currentSettings);
        }

        private RenderingQuality GetCurrentQualityLevel()
        {
            return _currentSettings.Quality;
        }

        private RenderingSettings GetCurrentRenderingSettings()
        {
            return new RenderingSettings
            {
                Quality = RenderingQuality.Medium,
                ShadowDistance = _urpAsset?.shadowDistance ?? 100f,
                MSAALevel = _urpAsset?.msaaSampleCount ?? (int)MsaaQuality._2x,
                HDREnabled = _urpAsset?.supportsHDR ?? true,
                AntiAliasing = true
            };
        }
    }

    /// <summary>
    /// Rendering settings data structure
    /// </summary>
    [System.Serializable]
    public struct RenderingSettings
    {
        public RenderingQuality Quality;
        public float ShadowDistance;
        public int MSAALevel;
        public bool HDREnabled;
        public bool AntiAliasing;
    }

    /// <summary>
    /// Pipeline statistics
    /// </summary>
    [System.Serializable]
    public struct PipelineStats
    {
        public bool IsEnabled;
        public RenderingQuality CurrentQuality;
        public float ShadowDistance;
        public bool HDREnabled;
        public int MSAALevel;
        public float TargetFrameRate;
    }
}
