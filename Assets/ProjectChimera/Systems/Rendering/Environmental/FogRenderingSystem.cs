using UnityEngine;
using UnityEngine.Rendering;
using ProjectChimera.Core.Logging;
using WeatherType = ProjectChimera.Systems.Rendering.WeatherType;

namespace ProjectChimera.Systems.Rendering.Environmental
{
    /// <summary>
    /// REFACTORED: Focused Fog Rendering System
    /// Handles only atmospheric fog rendering and optimization
    /// </summary>
    public class FogRenderingSystem : MonoBehaviour
    {
        [Header("Fog Settings")]
        [SerializeField] private bool _enableFog = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private Color _fogColor = new Color(0.5f, 0.6f, 0.7f, 1f);
        [SerializeField] private float _fogDensity = 0.01f;
        [SerializeField] private float _fogStart = 10f;
        [SerializeField] private float _fogEnd = 200f;
        [SerializeField] private AnimationCurve _fogFalloff = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Dynamic Fog")]
        [SerializeField] private bool _enableDynamicFog = true;
        [SerializeField] private float _fogTransitionSpeed = 1f;
        [SerializeField] private bool _enableHeightFog = true;
        [SerializeField] private float _heightFogStart = 0f;
        [SerializeField] private float _heightFogEnd = 50f;

        // Fog state
        private FogSettings _targetFogSettings;
        private FogSettings _currentFogSettings;
        private UnityEngine.Camera _mainCamera;

        // Properties
        public bool IsEnabled => _enableFog;
        public float CurrentFogDensity => _currentFogSettings.density;
        public Color CurrentFogColor => _currentFogSettings.color;

        // Events
        public System.Action<FogSettings> OnFogChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _mainCamera = UnityEngine.Camera.main;

            // Initialize fog settings
            _currentFogSettings = new FogSettings
            {
                color = _fogColor,
                density = _fogDensity,
                start = _fogStart,
                end = _fogEnd
            };
            _targetFogSettings = _currentFogSettings;

            ApplyFogSettings();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Fog rendering system initialized", this);
        }

        /// <summary>
        /// Update fog rendering - called by EnvironmentalRendererCore
        /// </summary>
        public void UpdateFog(float deltaTime)
        {
            if (!_enableFog) return;

            if (_enableDynamicFog)
            {
                UpdateDynamicFog(deltaTime);
            }

            if (_enableHeightFog && _mainCamera != null)
            {
                UpdateHeightFog();
            }
        }

        /// <summary>
        /// Set fog parameters
        /// </summary>
        public void SetFog(Color color, float density, float start, float end, bool immediate = false)
        {
            _targetFogSettings = new FogSettings
            {
                color = color,
                density = density,
                start = start,
                end = end
            };

            if (immediate)
            {
                _currentFogSettings = _targetFogSettings;
                ApplyFogSettings();
            }

            OnFogChanged?.Invoke(_targetFogSettings);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Fog settings updated - density: {density}, color: {color}", this);
        }

        /// <summary>
        /// Set fog for specific weather type
        /// </summary>
        public void SetWeatherFog(WeatherType weatherType)
        {
            var fogSettings = GetWeatherFogSettings(weatherType);
            SetFog(fogSettings.color, fogSettings.density, fogSettings.start, fogSettings.end);
        }

        /// <summary>
        /// Enable/disable fog rendering
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enableFog = enabled;
            RenderSettings.fog = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Fog rendering: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set fog density directly
        /// </summary>
        public void SetFogDensity(float density, bool immediate = false)
        {
            _targetFogSettings.density = density;

            if (immediate)
            {
                _currentFogSettings.density = density;
                ApplyFogSettings();
            }
        }

        /// <summary>
        /// Set fog color directly
        /// </summary>
        public void SetFogColor(Color color, bool immediate = false)
        {
            _targetFogSettings.color = color;

            if (immediate)
            {
                _currentFogSettings.color = color;
                ApplyFogSettings();
            }
        }

        private void UpdateDynamicFog(float deltaTime)
        {
            // Smooth transition to target fog settings
            bool changed = false;

            if (_currentFogSettings.density != _targetFogSettings.density)
            {
                _currentFogSettings.density = Mathf.Lerp(_currentFogSettings.density, _targetFogSettings.density, _fogTransitionSpeed * deltaTime);
                changed = true;
            }

            if (_currentFogSettings.color != _targetFogSettings.color)
            {
                _currentFogSettings.color = Color.Lerp(_currentFogSettings.color, _targetFogSettings.color, _fogTransitionSpeed * deltaTime);
                changed = true;
            }

            if (_currentFogSettings.start != _targetFogSettings.start)
            {
                _currentFogSettings.start = Mathf.Lerp(_currentFogSettings.start, _targetFogSettings.start, _fogTransitionSpeed * deltaTime);
                changed = true;
            }

            if (_currentFogSettings.end != _targetFogSettings.end)
            {
                _currentFogSettings.end = Mathf.Lerp(_currentFogSettings.end, _targetFogSettings.end, _fogTransitionSpeed * deltaTime);
                changed = true;
            }

            if (changed)
            {
                ApplyFogSettings();
            }
        }

        private void UpdateHeightFog()
        {
            // Adjust fog density based on camera height
            float cameraHeight = _mainCamera.transform.position.y;
            float heightFactor = Mathf.InverseLerp(_heightFogEnd, _heightFogStart, cameraHeight);
            float heightAdjustedDensity = _currentFogSettings.density * heightFactor;

            RenderSettings.fogDensity = heightAdjustedDensity;
        }

        private void ApplyFogSettings()
        {
            if (!_enableFog) return;

            RenderSettings.fog = true;
            RenderSettings.fogColor = _currentFogSettings.color;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = _currentFogSettings.density;
            RenderSettings.fogStartDistance = _currentFogSettings.start;
            RenderSettings.fogEndDistance = _currentFogSettings.end;
        }

        private FogSettings GetWeatherFogSettings(WeatherType weatherType)
        {
            switch (weatherType)
            {
                case WeatherType.Clear:
                    return new FogSettings
                    {
                        color = new Color(0.5f, 0.6f, 0.7f, 1f),
                        density = 0.005f,
                        start = 50f,
                        end = 300f
                    };

                case WeatherType.Cloudy:
                    return new FogSettings
                    {
                        color = new Color(0.6f, 0.6f, 0.6f, 1f),
                        density = 0.01f,
                        start = 30f,
                        end = 200f
                    };

                case WeatherType.Fog:
                    return new FogSettings
                    {
                        color = new Color(0.8f, 0.8f, 0.8f, 1f),
                        density = 0.05f,
                        start = 5f,
                        end = 50f
                    };

                case WeatherType.Storm:
                    return new FogSettings
                    {
                        color = new Color(0.3f, 0.3f, 0.4f, 1f),
                        density = 0.02f,
                        start = 10f,
                        end = 100f
                    };

                default:
                    return new FogSettings
                    {
                        color = _fogColor,
                        density = _fogDensity,
                        start = _fogStart,
                        end = _fogEnd
                    };
            }
        }

        /// <summary>
        /// Get current fog performance statistics
        /// </summary>
        public FogPerformanceStats GetPerformanceStats()
        {
            return new FogPerformanceStats
            {
                IsEnabled = _enableFog,
                CurrentDensity = _currentFogSettings.density,
                IsTransitioning = _currentFogSettings.density != _targetFogSettings.density,
                HeightFogEnabled = _enableHeightFog
            };
        }
    }

    /// <summary>
    /// Fog settings data structure
    /// </summary>
    [System.Serializable]
    public struct FogSettings
    {
        public Color color;
        public float density;
        public float start;
        public float end;
    }

    /// <summary>
    /// Fog performance statistics
    /// </summary>
    [System.Serializable]
    public struct FogPerformanceStats
    {
        public bool IsEnabled;
        public float CurrentDensity;
        public bool IsTransitioning;
        public bool HeightFogEnabled;
    }
}
