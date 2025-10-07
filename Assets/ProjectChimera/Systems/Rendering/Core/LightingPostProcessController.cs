using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using System.Linq;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Lighting & Post-Process Controller
    /// Focused component for managing custom lighting and post-processing effects
    /// </summary>
    public class LightingPostProcessController : MonoBehaviour
    {
        [Header("Lighting Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableCustomLighting = true;
        [SerializeField] private bool _enableVolumetricLighting = false;
        [SerializeField] private ShadowQuality _shadowQuality = ShadowQuality.High;
        [SerializeField] private float _shadowDistance = 150f;

        [Header("Post-Processing Settings")]
        [SerializeField] private bool _enableCustomPostProcessing = true;
        [SerializeField] private bool _enableBloom = true;
        [SerializeField] private bool _enableToneMapping = true;
        [SerializeField] private bool _enableColorGrading = true;
        [SerializeField] private bool _enableVignette = false;

        // Lighting components
        private Light _mainLight;
        private Light[] _additionalLights;
        private LightingSettings _currentLightingSettings;

        // Post-processing components
        private Volume _postProcessVolume;
        private VolumeProfile _customVolumeProfile;
        private PostProcessingSettings _currentPostProcessSettings;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public ShadowQuality CurrentShadowQuality => _shadowQuality;
        public bool CustomLightingEnabled => _enableCustomLighting;
        public bool PostProcessingEnabled => _enableCustomPostProcessing;

        // Events
        public System.Action<LightingSettings> OnLightingSettingsChanged;
        public System.Action<PostProcessingSettings> OnPostProcessingSettingsChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeLighting();
            InitializePostProcessing();
            ApplyCurrentSettings();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Lighting & Post-Process Controller initialized", this);
        }

        /// <summary>
        /// Apply lighting quality settings
        /// </summary>
        public void ApplyLightingQuality(RenderingQuality quality)
        {
            if (!IsEnabled) return;

            switch (quality)
            {
                case RenderingQuality.Low:
                    ApplyLowQualityLighting();
                    break;
                case RenderingQuality.Medium:
                    ApplyMediumQualityLighting();
                    break;
                case RenderingQuality.High:
                    ApplyHighQualityLighting();
                    break;
                case RenderingQuality.Ultra:
                    ApplyUltraQualityLighting();
                    break;
            }

            OnLightingSettingsChanged?.Invoke(_currentLightingSettings);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Applied {quality} lighting quality", this);
        }

        /// <summary>
        /// Apply post-processing quality settings
        /// </summary>
        public void ApplyPostProcessingQuality(RenderingQuality quality)
        {
            if (!IsEnabled || !_enableCustomPostProcessing) return;

            switch (quality)
            {
                case RenderingQuality.Low:
                    ApplyLowQualityPostProcessing();
                    break;
                case RenderingQuality.Medium:
                    ApplyMediumQualityPostProcessing();
                    break;
                case RenderingQuality.High:
                    ApplyHighQualityPostProcessing();
                    break;
                case RenderingQuality.Ultra:
                    ApplyUltraQualityPostProcessing();
                    break;
            }

            OnPostProcessingSettingsChanged?.Invoke(_currentPostProcessSettings);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Applied {quality} post-processing quality", this);
        }

        /// <summary>
        /// Set custom lighting enabled/disabled
        /// </summary>
        public void SetCustomLightingEnabled(bool enabled)
        {
            _enableCustomLighting = enabled;

            if (_mainLight != null)
            {
                _mainLight.enabled = enabled;
            }

            foreach (var light in _additionalLights ?? new Light[0])
            {
                if (light != null)
                {
                    light.enabled = enabled;
                }
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Custom lighting: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set post-processing enabled/disabled
        /// </summary>
        public void SetPostProcessingEnabled(bool enabled)
        {
            _enableCustomPostProcessing = enabled;

            if (_postProcessVolume != null)
            {
                _postProcessVolume.enabled = enabled;
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Post-processing: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Update lighting for specific growth environment
        /// </summary>
        public void UpdateEnvironmentalLighting(EnvironmentType environmentType)
        {
            if (!_enableCustomLighting) return;

            switch (environmentType)
            {
                case EnvironmentType.Indoor:
                    ApplyIndoorLighting();
                    break;
                case EnvironmentType.Greenhouse:
                    ApplyGreenhouseLighting();
                    break;
                case EnvironmentType.Outdoor:
                    ApplyOutdoorLighting();
                    break;
            }
        }

        /// <summary>
        /// Get current lighting and post-processing report
        /// </summary>
        public LightingPostProcessReport GetReport()
        {
            return new LightingPostProcessReport
            {
                LightingSettings = _currentLightingSettings,
                PostProcessingSettings = _currentPostProcessSettings,
                IsEnabled = IsEnabled,
                CustomLightingEnabled = _enableCustomLighting,
                PostProcessingEnabled = _enableCustomPostProcessing,
                ActiveLightCount = (_additionalLights?.Length ?? 0) + (_mainLight != null ? 1 : 0)
            };
        }

        /// <summary>
        /// Set lighting and post-processing enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                SetCustomLightingEnabled(false);
                SetPostProcessingEnabled(false);
            }
            else
            {
                SetCustomLightingEnabled(_enableCustomLighting);
                SetPostProcessingEnabled(_enableCustomPostProcessing);
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Lighting & Post-Process Controller: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void InitializeLighting()
        {
            // Get lights via GameObjectRegistry
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var allLights = registry?.GetAll<Light>();

            if (allLights != null && allLights.Any())
            {
                _mainLight = allLights.FirstOrDefault();
                _additionalLights = allLights.ToArray();
            }
            else
            {
                _mainLight = null;
                _additionalLights = new Light[0];

                if (_enableLogging)
                    ChimeraLogger.LogWarning("RENDERING", "No lights found - ensure lights are registered with GameObjectRegistry in Awake()", this);
            }

            _currentLightingSettings = new LightingSettings
            {
                ShadowQuality = _shadowQuality,
                ShadowDistance = _shadowDistance,
                VolumetricLightingEnabled = _enableVolumetricLighting,
                MainLightIntensity = _mainLight?.intensity ?? 1f,
                AmbientLightIntensity = RenderSettings.ambientIntensity
            };
        }

        private void InitializePostProcessing()
        {
            // Get post-process volume via GameObjectRegistry
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var volumes = registry?.GetAll<Volume>();

            if (volumes != null && volumes.Any())
            {
                _postProcessVolume = volumes.FirstOrDefault();
            }
            else
            {
                // Create post-process volume if none exists
                var volumeGO = new GameObject("CustomPostProcessVolume");
                volumeGO.transform.SetParent(transform);
                _postProcessVolume = volumeGO.AddComponent<Volume>();
                _postProcessVolume.isGlobal = true;

                // Register with GameObjectRegistry
                registry?.RegisterObject(_postProcessVolume);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", "Created and registered CustomPostProcessVolume", this);
            }

            _currentPostProcessSettings = new PostProcessingSettings
            {
                BloomEnabled = _enableBloom,
                ToneMappingEnabled = _enableToneMapping,
                ColorGradingEnabled = _enableColorGrading,
                VignetteEnabled = _enableVignette,
                PostProcessingQuality = RenderingQuality.High
            };
        }

        private void ApplyCurrentSettings()
        {
            SetCustomLightingEnabled(_enableCustomLighting);
            SetPostProcessingEnabled(_enableCustomPostProcessing);
        }

        private void ApplyLowQualityLighting()
        {
            if (_mainLight != null)
            {
                _mainLight.shadows = LightShadows.Hard;
                _mainLight.shadowStrength = 0.5f;
            }

            _shadowDistance = 50f;
            QualitySettings.shadowDistance = _shadowDistance;

            _currentLightingSettings.ShadowQuality = ShadowQuality.Low;
            _currentLightingSettings.ShadowDistance = _shadowDistance;
        }

        private void ApplyMediumQualityLighting()
        {
            if (_mainLight != null)
            {
                _mainLight.shadows = LightShadows.Soft;
                _mainLight.shadowStrength = 0.75f;
            }

            _shadowDistance = 100f;
            QualitySettings.shadowDistance = _shadowDistance;

            _currentLightingSettings.ShadowQuality = ShadowQuality.Medium;
            _currentLightingSettings.ShadowDistance = _shadowDistance;
        }

        private void ApplyHighQualityLighting()
        {
            if (_mainLight != null)
            {
                _mainLight.shadows = LightShadows.Soft;
                _mainLight.shadowStrength = 0.9f;
            }

            _shadowDistance = 150f;
            QualitySettings.shadowDistance = _shadowDistance;

            _currentLightingSettings.ShadowQuality = ShadowQuality.High;
            _currentLightingSettings.ShadowDistance = _shadowDistance;
        }

        private void ApplyUltraQualityLighting()
        {
            if (_mainLight != null)
            {
                _mainLight.shadows = LightShadows.Soft;
                _mainLight.shadowStrength = 1f;
            }

            _shadowDistance = 250f;
            QualitySettings.shadowDistance = _shadowDistance;

            _currentLightingSettings.ShadowQuality = ShadowQuality.Ultra;
            _currentLightingSettings.ShadowDistance = _shadowDistance;
        }

        private void ApplyLowQualityPostProcessing()
        {
            // Disable most post-processing effects for performance
            _currentPostProcessSettings.BloomEnabled = false;
            _currentPostProcessSettings.ColorGradingEnabled = false;
            _currentPostProcessSettings.VignetteEnabled = false;
            _currentPostProcessSettings.PostProcessingQuality = RenderingQuality.Low;
        }

        private void ApplyMediumQualityPostProcessing()
        {
            _currentPostProcessSettings.BloomEnabled = _enableBloom;
            _currentPostProcessSettings.ColorGradingEnabled = true;
            _currentPostProcessSettings.VignetteEnabled = false;
            _currentPostProcessSettings.PostProcessingQuality = RenderingQuality.Medium;
        }

        private void ApplyHighQualityPostProcessing()
        {
            _currentPostProcessSettings.BloomEnabled = _enableBloom;
            _currentPostProcessSettings.ToneMappingEnabled = _enableToneMapping;
            _currentPostProcessSettings.ColorGradingEnabled = _enableColorGrading;
            _currentPostProcessSettings.VignetteEnabled = _enableVignette;
            _currentPostProcessSettings.PostProcessingQuality = RenderingQuality.High;
        }

        private void ApplyUltraQualityPostProcessing()
        {
            _currentPostProcessSettings.BloomEnabled = _enableBloom;
            _currentPostProcessSettings.ToneMappingEnabled = _enableToneMapping;
            _currentPostProcessSettings.ColorGradingEnabled = _enableColorGrading;
            _currentPostProcessSettings.VignetteEnabled = _enableVignette;
            _currentPostProcessSettings.PostProcessingQuality = RenderingQuality.Ultra;
        }

        private void ApplyIndoorLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientIntensity = 0.3f;
            RenderSettings.ambientLight = new Color(0.8f, 0.9f, 1f);
        }

        private void ApplyGreenhouseLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientIntensity = 0.5f;
            RenderSettings.ambientSkyColor = new Color(0.9f, 1f, 0.9f);
        }

        private void ApplyOutdoorLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
        }
    }

    /// <summary>
    /// Environment types for lighting adaptation
    /// </summary>
    public enum EnvironmentType
    {
        Indoor,
        Greenhouse,
        Outdoor
    }

    /// <summary>
    /// Shadow quality levels
    /// </summary>
    public enum ShadowQuality
    {
        Disabled = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    /// <summary>
    /// Lighting settings data structure
    /// </summary>
    [System.Serializable]
    public struct LightingSettings
    {
        public ShadowQuality ShadowQuality;
        public float ShadowDistance;
        public bool VolumetricLightingEnabled;
        public float MainLightIntensity;
        public float AmbientLightIntensity;
    }

    /// <summary>
    /// Post-processing settings data structure
    /// </summary>
    [System.Serializable]
    public struct PostProcessingSettings
    {
        public bool BloomEnabled;
        public bool ToneMappingEnabled;
        public bool ColorGradingEnabled;
        public bool VignetteEnabled;
        public RenderingQuality PostProcessingQuality;
    }

    /// <summary>
    /// Lighting and post-processing report
    /// </summary>
    [System.Serializable]
    public struct LightingPostProcessReport
    {
        public LightingSettings LightingSettings;
        public PostProcessingSettings PostProcessingSettings;
        public bool IsEnabled;
        public bool CustomLightingEnabled;
        public bool PostProcessingEnabled;
        public int ActiveLightCount;
    }
}