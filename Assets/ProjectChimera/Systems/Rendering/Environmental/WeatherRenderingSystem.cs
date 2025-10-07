using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using WeatherType = ProjectChimera.Systems.Rendering.WeatherType;

namespace ProjectChimera.Systems.Rendering.Environmental
{
    /// <summary>
    /// REFACTORED: Focused Weather Rendering System
    /// Handles only weather effects and transitions
    /// </summary>
    public class WeatherRenderingSystem : MonoBehaviour
    {
        [Header("Weather System Settings")]
        [SerializeField] private bool _enableWeather = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private WeatherType _currentWeather = WeatherType.Clear;
        [SerializeField] private float _weatherTransitionSpeed = 1f;
        [SerializeField] private bool _enableDynamicWeather = false;
        [SerializeField] private float _weatherChangeInterval = 300f; // 5 minutes

        [Header("Weather Effect Prefabs")]
        [SerializeField] private GameObject _rainEffectPrefab;
        [SerializeField] private GameObject _snowEffectPrefab;
        [SerializeField] private GameObject _stormEffectPrefab;
        [SerializeField] private GameObject _cloudEffectPrefab;

        // Weather state
        private WeatherType _targetWeather;
        private float _weatherTransitionProgress;
        private float _lastWeatherChangeTime;
        private GameObject _currentWeatherEffect;

        // Weather effect pool
        private readonly Dictionary<WeatherType, GameObject> _weatherEffectPool = new Dictionary<WeatherType, GameObject>();
        private readonly List<WeatherType> _availableWeatherTypes = new List<WeatherType>();

        // Properties
        public bool IsEnabled => _enableWeather;
        public WeatherType CurrentWeather => _currentWeather;
        public WeatherType TargetWeather => _targetWeather;
        public float TransitionProgress => _weatherTransitionProgress;

        // Events
        public System.Action<WeatherType, WeatherType> OnWeatherChanged;
        public System.Action<WeatherType> OnWeatherTransitionComplete;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _targetWeather = _currentWeather;
            _weatherTransitionProgress = 1f;
            _lastWeatherChangeTime = Time.time;

            InitializeWeatherEffects();
            InitializeAvailableWeathers();
            ActivateWeatherEffect(_currentWeather);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Weather rendering system initialized", this);
        }

        /// <summary>
        /// Update weather rendering - called by EnvironmentalRendererCore
        /// </summary>
        public void UpdateWeather(float deltaTime)
        {
            if (!_enableWeather) return;

            if (_enableDynamicWeather)
            {
                UpdateDynamicWeatherChanges();
            }

            if (_currentWeather != _targetWeather)
            {
                UpdateWeatherTransition(deltaTime);
            }

            UpdateCurrentWeatherEffect(deltaTime);
        }

        /// <summary>
        /// Set weather type
        /// </summary>
        public void SetWeather(WeatherType weatherType, bool immediate = false)
        {
            if (_targetWeather == weatherType) return;

            var previousWeather = _currentWeather;
            _targetWeather = weatherType;

            if (immediate)
            {
                _currentWeather = weatherType;
                _weatherTransitionProgress = 1f;
                ActivateWeatherEffect(weatherType);
                OnWeatherTransitionComplete?.Invoke(weatherType);
            }
            else
            {
                _weatherTransitionProgress = 0f;
            }

            OnWeatherChanged?.Invoke(previousWeather, weatherType);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Weather transition started: {previousWeather} → {weatherType}", this);
        }

        /// <summary>
        /// Enable/disable weather rendering
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enableWeather = enabled;

            if (_currentWeatherEffect != null)
            {
                _currentWeatherEffect.SetActive(enabled);
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Weather rendering: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Force weather change for dynamic weather
        /// </summary>
        public void TriggerRandomWeatherChange()
        {
            if (_availableWeatherTypes.Count == 0) return;

            var randomWeather = _availableWeatherTypes[Random.Range(0, _availableWeatherTypes.Count)];
            SetWeather(randomWeather);
        }

        /// <summary>
        /// Get weather intensity (0-1)
        /// </summary>
        public float GetWeatherIntensity()
        {
            switch (_currentWeather)
            {
                case WeatherType.Clear: return 0f;
                case WeatherType.Cloudy: return 0.3f;
                case WeatherType.Overcast: return 0.5f;
                case WeatherType.LightRain: return 0.6f;
                case WeatherType.HeavyRain: return 0.8f;
                case WeatherType.Storm: return 1f;
                case WeatherType.Fog: return 0.7f;
                case WeatherType.Snow: return 0.6f;
                default: return 0f;
            }
        }

        private void InitializeWeatherEffects()
        {
            // Pre-instantiate weather effects for pooling
            if (_rainEffectPrefab != null)
            {
                var rainEffect = Instantiate(_rainEffectPrefab, transform);
                rainEffect.SetActive(false);
                _weatherEffectPool[WeatherType.LightRain] = rainEffect;
                _weatherEffectPool[WeatherType.HeavyRain] = rainEffect;
            }

            if (_snowEffectPrefab != null)
            {
                var snowEffect = Instantiate(_snowEffectPrefab, transform);
                snowEffect.SetActive(false);
                _weatherEffectPool[WeatherType.Snow] = snowEffect;
            }

            if (_stormEffectPrefab != null)
            {
                var stormEffect = Instantiate(_stormEffectPrefab, transform);
                stormEffect.SetActive(false);
                _weatherEffectPool[WeatherType.Storm] = stormEffect;
            }

            if (_cloudEffectPrefab != null)
            {
                var cloudEffect = Instantiate(_cloudEffectPrefab, transform);
                cloudEffect.SetActive(false);
                _weatherEffectPool[WeatherType.Cloudy] = cloudEffect;
                _weatherEffectPool[WeatherType.Overcast] = cloudEffect;
            }
        }

        private void InitializeAvailableWeathers()
        {
            _availableWeatherTypes.Clear();
            _availableWeatherTypes.AddRange(new[]
            {
                WeatherType.Clear,
                WeatherType.Cloudy,
                WeatherType.Overcast,
                WeatherType.LightRain,
                WeatherType.HeavyRain,
                WeatherType.Fog
            });

            // Only add weather types that have effects
            if (_snowEffectPrefab != null) _availableWeatherTypes.Add(WeatherType.Snow);
            if (_stormEffectPrefab != null) _availableWeatherTypes.Add(WeatherType.Storm);
        }

        private void UpdateDynamicWeatherChanges()
        {
            if (Time.time - _lastWeatherChangeTime >= _weatherChangeInterval)
            {
                TriggerRandomWeatherChange();
                _lastWeatherChangeTime = Time.time;
            }
        }

        private void UpdateWeatherTransition(float deltaTime)
        {
            _weatherTransitionProgress += _weatherTransitionSpeed * deltaTime;

            if (_weatherTransitionProgress >= 1f)
            {
                _weatherTransitionProgress = 1f;
                _currentWeather = _targetWeather;
                ActivateWeatherEffect(_currentWeather);
                OnWeatherTransitionComplete?.Invoke(_currentWeather);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Weather transition completed: {_currentWeather}", this);
            }
        }

        private void UpdateCurrentWeatherEffect(float deltaTime)
        {
            if (_currentWeatherEffect == null) return;

            // Update weather effect intensity based on transition progress
            var intensity = GetWeatherIntensity() * _weatherTransitionProgress;
            UpdateWeatherEffectIntensity(_currentWeatherEffect, intensity);
        }

        private void ActivateWeatherEffect(WeatherType weatherType)
        {
            // Deactivate current effect
            if (_currentWeatherEffect != null)
            {
                _currentWeatherEffect.SetActive(false);
            }

            // Activate new effect
            if (_weatherEffectPool.TryGetValue(weatherType, out var weatherEffect))
            {
                _currentWeatherEffect = weatherEffect;
                _currentWeatherEffect.SetActive(true);
            }
            else
            {
                _currentWeatherEffect = null;
            }
        }

        private void UpdateWeatherEffectIntensity(GameObject weatherEffect, float intensity)
        {
            // Update particle systems in the weather effect
            var particleSystems = weatherEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var emission = ps.emission;
                var rateOverTime = emission.rateOverTime;
                rateOverTime.constant = rateOverTime.constant * intensity;
                emission.rateOverTime = rateOverTime;
            }

            // Update audio sources
            var audioSources = weatherEffect.GetComponentsInChildren<AudioSource>();
            foreach (var audio in audioSources)
            {
                audio.volume = intensity;
            }
        }

        /// <summary>
        /// Get weather rendering performance statistics
        /// </summary>
        public WeatherPerformanceStats GetPerformanceStats()
        {
            return new WeatherPerformanceStats
            {
                IsEnabled = _enableWeather,
                CurrentWeather = _currentWeather,
                IsTransitioning = _currentWeather != _targetWeather,
                TransitionProgress = _weatherTransitionProgress,
                EffectsActive = _currentWeatherEffect != null && _currentWeatherEffect.activeInHierarchy
            };
        }
    }

    /// <summary>
    /// Weather performance statistics
    /// </summary>
    [System.Serializable]
    public struct WeatherPerformanceStats
    {
        public bool IsEnabled;
        public WeatherType CurrentWeather;
        public bool IsTransitioning;
        public float TransitionProgress;
        public bool EffectsActive;
    }
}