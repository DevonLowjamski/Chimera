using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Handles basic light control, intensity management, and state transitions.
    /// Extracted from AdvancedGrowLightSystem for modular architecture.
    /// Manages light on/off, intensity ramping, and power consumption.
    /// </summary>
    public class GrowLightController : MonoBehaviour, ITickable
    {
        [Header("Light Control Configuration")]
        [SerializeField] private bool _enableLightLogging = true;
        [SerializeField] private float _intensityTransitionSpeed = 50f; // PPFD per second
        [SerializeField] private float _smoothTransitionTime = 2f;
        
        // Light configuration
        private float _maxIntensity = 1000f;
        private float _maxPowerConsumption = 600f;
        private float _efficiency = 2.5f; // μmol/J
        
        // Light components
        private Light _primaryLight;
        private Light[] _supplementalLights;
        
        // Light state
        private bool _isOn = false;
        private float _currentIntensity = 0f;
        private float _targetIntensity = 0f;
        private float _currentPowerConsumption = 0f;
        private bool _thermalThrottling = false;
        private Coroutine _transitionCoroutine;
        
        // Events
        public System.Action<bool> OnLightStateChanged;
        public System.Action<float> OnIntensityChanged;
        public System.Action<float> OnPowerConsumptionChanged;
        
        // Properties
        public bool IsOn => _isOn;
        public float CurrentIntensity => _currentIntensity;
        public float TargetIntensity => _targetIntensity;
        public float PowerConsumption => _currentPowerConsumption;
        public float MaxIntensity => _maxIntensity;
        public float MaxPowerConsumption => _maxPowerConsumption;
        
        // ITickable implementation
        public int Priority => TickPriority.EnvironmentalManager;
        public bool Enabled => _primaryLight != null;
        
        /// <summary>
        /// Initialize light controller with configuration
        /// </summary>
        public void Initialize(float maxIntensity, float maxPowerConsumption, float efficiency, Light primaryLight, Light[] supplementalLights)
        {
            _maxIntensity = maxIntensity;
            _maxPowerConsumption = maxPowerConsumption;
            _efficiency = efficiency;
            _primaryLight = primaryLight;
            _supplementalLights = supplementalLights;
            
            LogDebug("Grow light controller initialized");
        }
        
        public void Tick(float deltaTime)
        {
            // Update light intensity smoothly when not in coroutine transition
            if (_transitionCoroutine == null)
            {
                UpdateLightIntensity();
            }
        }
        
        #region Light Control
        
        /// <summary>
        /// Turn light on with smooth transition
        /// </summary>
        public void TurnOn()
        {
            if (_isOn) return;
            
            _isOn = true;
            _targetIntensity = _maxIntensity * 0.8f; // Start at 80% intensity
            
            OnLightStateChanged?.Invoke(true);
            
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            
            _transitionCoroutine = StartCoroutine(SmoothLightTransition());
            
            LogDebug("Light turned ON");
        }
        
        /// <summary>
        /// Turn light off with smooth transition
        /// </summary>
        public void TurnOff()
        {
            if (!_isOn) return;
            
            _isOn = false;
            _targetIntensity = 0f;
            
            OnLightStateChanged?.Invoke(false);
            
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            
            _transitionCoroutine = StartCoroutine(SmoothLightTransition());
            
            LogDebug("Light turned OFF");
        }
        
        /// <summary>
        /// Set target light intensity with thermal throttling protection
        /// </summary>
        public void SetIntensity(float intensity)
        {
            intensity = Mathf.Clamp(intensity, 0f, _maxIntensity);
            
            if (!_thermalThrottling)
            {
                _targetIntensity = intensity;
                OnIntensityChanged?.Invoke(intensity);
                LogDebug($"Target intensity set to {intensity:F1} PPFD");
            }
            else
            {
                float maxSafeIntensity = _maxIntensity * 0.7f; // 70% when thermal throttling
                _targetIntensity = Mathf.Min(intensity, maxSafeIntensity);
                
                if (intensity > maxSafeIntensity)
                {
                    LogDebug($"Intensity limited due to thermal throttling: {_targetIntensity:F1} PPFD");
                }
            }
        }
        
        /// <summary>
        /// Set thermal throttling state
        /// </summary>
        public void SetThermalThrottling(bool enabled)
        {
            _thermalThrottling = enabled;
            
            if (enabled && _targetIntensity > _maxIntensity * 0.7f)
            {
                SetIntensity(_targetIntensity); // This will apply throttling
            }
            
            LogDebug($"Thermal throttling {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Force immediate intensity change without transition
        /// </summary>
        public void SetIntensityImmediate(float intensity)
        {
            intensity = Mathf.Clamp(intensity, 0f, _maxIntensity);
            _currentIntensity = intensity;
            _targetIntensity = intensity;
            
            ApplyLightIntensity();
            OnIntensityChanged?.Invoke(intensity);
            
            LogDebug($"Intensity set immediately to {intensity:F1} PPFD");
        }
        
        #endregion
        
        #region Light Intensity Management
        
        /// <summary>
        /// Smooth light transition coroutine
        /// </summary>
        private IEnumerator SmoothLightTransition()
        {
            float startIntensity = _currentIntensity;
            float transitionTime = _isOn ? _smoothTransitionTime : _smoothTransitionTime * 0.5f; // Faster ramp down
            float elapsed = 0f;
            
            while (elapsed < transitionTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionTime;
                t = Mathf.SmoothStep(0f, 1f, t);
                
                _currentIntensity = Mathf.Lerp(startIntensity, _targetIntensity, t);
                ApplyLightIntensity();
                
                yield return null;
            }
            
            _currentIntensity = _targetIntensity;
            ApplyLightIntensity();
            
            _transitionCoroutine = null;
        }
        
        /// <summary>
        /// Update light intensity gradually
        /// </summary>
        private void UpdateLightIntensity()
        {
            if (Mathf.Abs(_currentIntensity - _targetIntensity) > 1f)
            {
                _currentIntensity = Mathf.MoveTowards(_currentIntensity, _targetIntensity, 
                                                     _intensityTransitionSpeed * Time.deltaTime);
                ApplyLightIntensity();
            }
        }
        
        /// <summary>
        /// Apply current intensity to light components
        /// </summary>
        private void ApplyLightIntensity()
        {
            // Update primary light
            if (_primaryLight != null)
            {
                // Convert PPFD to Unity light intensity (approximate conversion)
                float unityIntensity = _currentIntensity / 1000f * 2f; // Rough conversion
                _primaryLight.intensity = unityIntensity;
                _primaryLight.enabled = _currentIntensity > 0f;
            }
            
            // Update supplemental lights proportionally
            UpdateSupplementalLightIntensities();
            
            // Update power consumption
            float previousPowerConsumption = _currentPowerConsumption;
            _currentPowerConsumption = (_currentIntensity / _maxIntensity) * _maxPowerConsumption;
            
            if (Mathf.Abs(_currentPowerConsumption - previousPowerConsumption) > 1f)
            {
                OnPowerConsumptionChanged?.Invoke(_currentPowerConsumption);
            }
        }
        
        /// <summary>
        /// Update supplemental light intensities based on current intensity
        /// </summary>
        private void UpdateSupplementalLightIntensities()
        {
            if (_supplementalLights == null) return;
            
            float baseIntensity = _currentIntensity / _maxIntensity;
            
            foreach (var light in _supplementalLights)
            {
                if (light != null)
                {
                    light.intensity = baseIntensity * 0.5f; // 50% of base intensity
                    light.enabled = light.intensity > 0.01f;
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get light status information
        /// </summary>
        public LightControlStatus GetStatus()
        {
            return new LightControlStatus
            {
                IsOn = _isOn,
                CurrentIntensity = _currentIntensity,
                TargetIntensity = _targetIntensity,
                PowerConsumption = _currentPowerConsumption,
                Efficiency = CalculateCurrentEfficiency(),
                ThermalThrottling = _thermalThrottling
            };
        }
        
        /// <summary>
        /// Calculate current efficiency (PPFD per watt)
        /// </summary>
        public float CalculateCurrentEfficiency()
        {
            if (_currentPowerConsumption <= 0f) return _efficiency;
            
            return _currentIntensity / _currentPowerConsumption;
        }
        
        /// <summary>
        /// Check if light can increase intensity
        /// </summary>
        public bool CanIncreaseIntensity(float additionalIntensity)
        {
            float newIntensity = _targetIntensity + additionalIntensity;
            
            if (_thermalThrottling)
            {
                return newIntensity <= _maxIntensity * 0.7f;
            }
            
            return newIntensity <= _maxIntensity;
        }
        
        /// <summary>
        /// Get remaining intensity capacity
        /// </summary>
        public float GetRemainingCapacity()
        {
            float maxAllowedIntensity = _thermalThrottling ? _maxIntensity * 0.7f : _maxIntensity;
            return Mathf.Max(0f, maxAllowedIntensity - _targetIntensity);
        }
        
        /// <summary>
        /// Reset light to default state
        /// </summary>
        public void Reset()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
            
            _isOn = false;
            _currentIntensity = 0f;
            _targetIntensity = 0f;
            _currentPowerConsumption = 0f;
            _thermalThrottling = false;
            
            ApplyLightIntensity();
            OnLightStateChanged?.Invoke(false);
            
            LogDebug("Light controller reset");
        }
        
        #endregion
        
        private void Start()
        {
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }
        
        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
            
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableLightLogging)
                ChimeraLogger.Log($"[GrowLightController] {message}");
        }
    }
    
    /// <summary>
    /// Light control status information
    /// </summary>
    [System.Serializable]
    public class LightControlStatus
    {
        public bool IsOn;
        public float CurrentIntensity;
        public float TargetIntensity;
        public float PowerConsumption;
        public float Efficiency;
        public bool ThermalThrottling;
    }
}