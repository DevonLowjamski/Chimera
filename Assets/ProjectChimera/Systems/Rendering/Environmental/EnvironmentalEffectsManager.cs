using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using WeatherType = ProjectChimera.Systems.Rendering.WeatherType;

namespace ProjectChimera.Systems.Rendering.Environmental
{
    /// <summary>
    /// REFACTORED: Focused Environmental Effects Manager
    /// Handles only particle effects and environmental effect management
    /// </summary>
    public class EnvironmentalEffectsManager : MonoBehaviour
    {
        [Header("Effects Settings")]
        [SerializeField] private bool _enableEffects = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxActiveEffects = 50;
        [SerializeField] private float _effectCullingDistance = 100f;
        [SerializeField] private float _effectUpdateInterval = 0.2f;

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject _dustEffectPrefab;
        [SerializeField] private GameObject _leafEffectPrefab;
        [SerializeField] private GameObject _emberEffectPrefab;
        [SerializeField] private GameObject _moistureEffectPrefab;

        // Effect state
        private readonly List<EnvironmentalEffect> _activeEffects = new List<EnvironmentalEffect>();
        private readonly Queue<EnvironmentalEffect> _effectPool = new Queue<EnvironmentalEffect>();
        private float _lastEffectUpdate;
        private UnityEngine.Camera _mainCamera;

        // Properties
        public bool IsEnabled => _enableEffects;
        public int ActiveEffectCount => _activeEffects.Count;
        public int MaxActiveEffects => _maxActiveEffects;

        // Events
        public System.Action<EnvironmentalEffectType> OnEffectTriggered;
        public System.Action<int> OnActiveEffectCountChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _mainCamera = UnityEngine.Camera.main;
            InitializeEffectPool();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Environmental effects manager initialized", this);
        }

        /// <summary>
        /// Update environmental effects - called by EnvironmentalRendererCore
        /// </summary>
        public void UpdateEffects(float deltaTime)
        {
            if (!_enableEffects) return;

            if (Time.time - _lastEffectUpdate >= _effectUpdateInterval)
            {
                UpdateActiveEffects();
                CullDistantEffects();
                _lastEffectUpdate = Time.time;
            }
        }

        /// <summary>
        /// Trigger environmental effect at position
        /// </summary>
        public void TriggerEffect(EnvironmentalEffectType effectType, Vector3 position, float intensity = 1f)
        {
            if (!_enableEffects || _activeEffects.Count >= _maxActiveEffects) return;

            var effect = GetPooledEffect(effectType);
            if (effect != null)
            {
                effect.transform.position = position;
                effect.SetIntensity(intensity);
                effect.Activate();
                _activeEffects.Add(effect);

                OnEffectTriggered?.Invoke(effectType);
                OnActiveEffectCountChanged?.Invoke(_activeEffects.Count);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Environmental effect triggered: {effectType} at {position}", this);
            }
        }

        /// <summary>
        /// Trigger weather-based environmental effects
        /// </summary>
        public void TriggerWeatherEffects(WeatherType weatherType, Vector3 position)
        {
            switch (weatherType)
            {
                case WeatherType.Storm:
                    TriggerEffect(EnvironmentalEffectType.Dust, position, 0.8f);
                    TriggerEffect(EnvironmentalEffectType.Leaves, position, 1f);
                    break;

                case WeatherType.HeavyRain:
                    TriggerEffect(EnvironmentalEffectType.Moisture, position, 0.9f);
                    break;

                case WeatherType.LightRain:
                    TriggerEffect(EnvironmentalEffectType.Moisture, position, 0.5f);
                    break;

                case WeatherType.Fog:
                    TriggerEffect(EnvironmentalEffectType.Moisture, position, 0.3f);
                    break;

                case WeatherType.Clear:
                    TriggerEffect(EnvironmentalEffectType.Dust, position, 0.2f);
                    break;
            }
        }

        /// <summary>
        /// Stop all environmental effects
        /// </summary>
        public void StopAllEffects()
        {
            foreach (var effect in _activeEffects)
            {
                effect.Deactivate();
                ReturnEffectToPool(effect);
            }
            _activeEffects.Clear();
            OnActiveEffectCountChanged?.Invoke(0);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "All environmental effects stopped", this);
        }

        /// <summary>
        /// Enable/disable environmental effects
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enableEffects = enabled;

            if (!enabled)
            {
                StopAllEffects();
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Environmental effects: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set maximum active effects
        /// </summary>
        public void SetMaxActiveEffects(int maxEffects)
        {
            _maxActiveEffects = maxEffects;

            // Remove excess effects if needed
            while (_activeEffects.Count > _maxActiveEffects)
            {
                var effect = _activeEffects[_activeEffects.Count - 1];
                _activeEffects.RemoveAt(_activeEffects.Count - 1);
                effect.Deactivate();
                ReturnEffectToPool(effect);
            }
        }

        private void InitializeEffectPool()
        {
            // Pre-instantiate effects for pooling
            InitializeEffectType(_dustEffectPrefab, EnvironmentalEffectType.Dust, 10);
            InitializeEffectType(_leafEffectPrefab, EnvironmentalEffectType.Leaves, 8);
            InitializeEffectType(_emberEffectPrefab, EnvironmentalEffectType.Embers, 5);
            InitializeEffectType(_moistureEffectPrefab, EnvironmentalEffectType.Moisture, 12);
        }

        private void InitializeEffectType(GameObject prefab, EnvironmentalEffectType effectType, int poolSize)
        {
            if (prefab == null) return;

            for (int i = 0; i < poolSize; i++)
            {
                var effectObj = Instantiate(prefab, transform);
                var effect = effectObj.GetComponent<EnvironmentalEffect>();
                if (effect == null)
                {
                    effect = effectObj.AddComponent<EnvironmentalEffect>();
                }
                effect.Initialize(effectType);
                effect.Deactivate();
                _effectPool.Enqueue(effect);
            }
        }

        private EnvironmentalEffect GetPooledEffect(EnvironmentalEffectType effectType)
        {
            // Try to get effect of matching type from pool
            var poolCount = _effectPool.Count;
            for (int i = 0; i < poolCount; i++)
            {
                var effect = _effectPool.Dequeue();
                if (effect.EffectType == effectType)
                {
                    return effect;
                }
                _effectPool.Enqueue(effect); // Put back if not matching
            }

            // If no matching type, get any available effect
            if (_effectPool.Count > 0)
            {
                var effect = _effectPool.Dequeue();
                effect.SetEffectType(effectType);
                return effect;
            }

            return null;
        }

        private void UpdateActiveEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                if (effect == null || !effect.IsActive)
                {
                    _activeEffects.RemoveAt(i);
                    if (effect != null)
                    {
                        ReturnEffectToPool(effect);
                    }
                }
                else
                {
                    effect.UpdateEffect(Time.deltaTime);
                }
            }
        }

        private void CullDistantEffects()
        {
            if (_mainCamera == null) return;

            var cameraPosition = _mainCamera.transform.position;
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                var distance = Vector3.Distance(cameraPosition, effect.transform.position);

                if (distance > _effectCullingDistance)
                {
                    _activeEffects.RemoveAt(i);
                    effect.Deactivate();
                    ReturnEffectToPool(effect);
                }
            }
        }

        private void ReturnEffectToPool(EnvironmentalEffect effect)
        {
            effect.Deactivate();
            _effectPool.Enqueue(effect);
        }

        /// <summary>
        /// Get environmental effects performance statistics
        /// </summary>
        public EnvironmentalEffectsPerformanceStats GetPerformanceStats()
        {
            return new EnvironmentalEffectsPerformanceStats
            {
                IsEnabled = _enableEffects,
                ActiveEffectCount = _activeEffects.Count,
                MaxActiveEffects = _maxActiveEffects,
                PooledEffectCount = _effectPool.Count,
                CullingDistance = _effectCullingDistance
            };
        }
    }

    /// <summary>
    /// Environmental effect component
    /// </summary>
    public class EnvironmentalEffect : MonoBehaviour
    {
        private EnvironmentalEffectType _effectType;
        private float _intensity = 1f;
        private bool _isActive;
        private ParticleSystem _particleSystem;
        private AudioSource _audioSource;
        private float _lifetime = 5f;
        private float _currentLifetime;

        public EnvironmentalEffectType EffectType => _effectType;
        public bool IsActive => _isActive;

        public void Initialize(EnvironmentalEffectType effectType)
        {
            _effectType = effectType;
            _particleSystem = GetComponent<ParticleSystem>();
            _audioSource = GetComponent<AudioSource>();
            gameObject.SetActive(false);
        }

        public void SetEffectType(EnvironmentalEffectType effectType)
        {
            _effectType = effectType;
        }

        public void SetIntensity(float intensity)
        {
            _intensity = Mathf.Clamp01(intensity);
            ApplyIntensity();
        }

        public void Activate()
        {
            _isActive = true;
            _currentLifetime = 0f;
            gameObject.SetActive(true);

            if (_particleSystem != null)
            {
                _particleSystem.Play();
            }

            if (_audioSource != null)
            {
                _audioSource.Play();
            }
        }

        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);

            if (_particleSystem != null)
            {
                _particleSystem.Stop();
            }

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        public void UpdateEffect(float deltaTime)
        {
            if (!_isActive) return;

            _currentLifetime += deltaTime;
            if (_currentLifetime >= _lifetime)
            {
                Deactivate();
            }
        }

        private void ApplyIntensity()
        {
            if (_particleSystem != null)
            {
                var emission = _particleSystem.emission;
                var rateOverTime = emission.rateOverTime;
                rateOverTime.constant = rateOverTime.constant * _intensity;
                emission.rateOverTime = rateOverTime;
            }

            if (_audioSource != null)
            {
                _audioSource.volume = _intensity;
            }
        }
    }

    /// <summary>
    /// Environmental effect types
    /// </summary>
    public enum EnvironmentalEffectType
    {
        Dust,
        Leaves,
        Embers,
        Moisture,
        Particles,
        Smoke
    }

    /// <summary>
    /// Environmental effects performance statistics
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalEffectsPerformanceStats
    {
        public bool IsEnabled;
        public int ActiveEffectCount;
        public int MaxActiveEffects;
        public int PooledEffectCount;
        public float CullingDistance;
    }
}
