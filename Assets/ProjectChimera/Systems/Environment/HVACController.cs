using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Environment;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Controls HVAC (Heating, Ventilation, Air Conditioning) systems for environmental management.
    /// Manages temperature and humidity control with automation capabilities.
    /// </summary>
    public class HVACController : MonoBehaviour, ITickable
    {
        [Header("HVAC Configuration")]
        [SerializeField] private string _systemId;
        [SerializeField] private HVACType _hvacType = HVACType.Split_System;
        [SerializeField] private float _maxHeatingCapacity = 10f; // kW
        [SerializeField] private float _maxCoolingCapacity = 12f; // kW
        [SerializeField] private float _maxHumidificationRate = 5f; // L/hr
        [SerializeField] private float _maxDehumidificationRate = 8f; // L/hr
        
        [Header("Target Settings")]
        [SerializeField] private float _targetTemperature = 23f; // °C
        [SerializeField] private float _targetHumidity = 55f; // %
        [SerializeField] private float _temperatureTolerance = 1f; // °C
        [SerializeField] private float _humidityTolerance = 5f; // %
        
        [Header("Performance")]
        [SerializeField] private float _responseTime = 2f; // minutes to reach target
        [SerializeField] private float _energyEfficiency = 0.85f; // 0-1
        [SerializeField] private bool _enableAutomation = true;
        [SerializeField] private float _updateInterval = 30f; // seconds
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _heatingEffect;
        [SerializeField] private ParticleSystem _coolingEffect;
        [SerializeField] private ParticleSystem _humidifierEffect;
        [SerializeField] private ParticleSystem _dehumidifierEffect;
        [SerializeField] private AudioSource _systemAudio;
        
        // System State
        private bool _isOperational = true;
        private bool _isAutomated = false;
        private float _currentTemperature = 20f;
        private float _currentHumidity = 45f;
        private float _powerConsumption = 0f;
        private float _lastUpdateTime;
        
        // Control State
        private bool _heatingActive = false;
        private bool _coolingActive = false;
        private bool _humidifyingActive = false;
        private bool _dehumidifyingActive = false;
        
        // Performance Metrics
        private float _totalEnergyUsed = 0f;
        private float _operatingHours = 0f;
        private int _cycleCount = 0;
        
        // Events
        public System.Action<HVACController> OnSystemStateChanged;
        public System.Action<float, float> OnEnvironmentChanged; // temperature, humidity
        public System.Action<float> OnPowerConsumptionChanged;
        
        // Properties
        public string SystemId => string.IsNullOrEmpty(_systemId) ? name : _systemId;
        public HVACType Type => _hvacType;
        public bool IsOperational => _isOperational;
        public bool IsAutomated => _isAutomated;
        public float TargetTemperature => _targetTemperature;
        public float TargetHumidity => _targetHumidity;
        public float CurrentTemperature => _currentTemperature;
        public float CurrentHumidity => _currentHumidity;
        public float PowerConsumption => _powerConsumption;
        public float EnergyEfficiency => _energyEfficiency;
        public bool IsHeatingActive => _heatingActive;
        public bool IsCoolingActive => _coolingActive;
        public bool IsWithinTargetRange => IsTemperatureInRange && IsHumidityInRange;
        
        private bool IsTemperatureInRange => 
            Mathf.Abs(_currentTemperature - _targetTemperature) <= _temperatureTolerance;
        private bool IsHumidityInRange => 
            Mathf.Abs(_currentHumidity - _targetHumidity) <= _humidityTolerance;
        
        // ITickable implementation
        public int Priority => TickPriority.EnvironmentalManager;
        public bool Enabled => _isOperational;
        
        private void Awake()
        {
            if (string.IsNullOrEmpty(_systemId))
                _systemId = System.Guid.NewGuid().ToString();
        }
        
        private void Start()
        {
            InitializeSystem();
            
            if (_enableAutomation)
                SetAutomationMode(true);
            
            // Register with UpdateOrchestrator for centralized update management
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }
        
        public void Tick(float deltaTime)
        {
            if (!_isOperational) return;
            
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateSystem();
                _lastUpdateTime = Time.time;
            }
            
            // Update operating hours
            _operatingHours += deltaTime / 3600f; // Convert to hours
        }
        
        private void InitializeSystem()
        {
            // Initialize with current environmental conditions
            _currentTemperature = 20f + Random.Range(-5f, 5f);
            _currentHumidity = 50f + Random.Range(-10f, 10f);
            
            // Set up audio
            if (_systemAudio != null)
            {
                _systemAudio.loop = true;
                _systemAudio.volume = 0.3f;
            }
            
            ChimeraLogger.Log($"HVAC System {SystemId} initialized - Type: {_hvacType}");
        }
        
        #region Public Control Methods
        
        /// <summary>
        /// Set target temperature
        /// </summary>
        public void SetTargetTemperature(float temperature)
        {
            _targetTemperature = Mathf.Clamp(temperature, 15f, 35f);
            
            if (_isAutomated)
                UpdateControlLogic();
            
            ChimeraLogger.Log($"HVAC {SystemId} target temperature set to {_targetTemperature}°C");
        }
        
        /// <summary>
        /// Set target humidity
        /// </summary>
        public void SetTargetHumidity(float humidity)
        {
            _targetHumidity = Mathf.Clamp(humidity, 20f, 80f);
            
            if (_isAutomated)
                UpdateControlLogic();
            
            ChimeraLogger.Log($"HVAC {SystemId} target humidity set to {_targetHumidity}%");
        }
        
        /// <summary>
        /// Enable or disable automation mode
        /// </summary>
        public void SetAutomationMode(bool enabled)
        {
            _isAutomated = enabled;
            
            if (enabled)
            {
                UpdateControlLogic();
            }
            else
            {
                // Stop all systems when automation is disabled
                StopAllSystems();
            }
            
            OnSystemStateChanged?.Invoke(this);
            ChimeraLogger.Log($"HVAC {SystemId} automation {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Manual heating control
        /// </summary>
        public void SetHeating(bool enabled, float intensity = 1f)
        {
            if (_isAutomated) return;
            
            _heatingActive = enabled && _isOperational;
            
            if (_heatingActive)
            {
                _coolingActive = false; // Can't heat and cool at the same time
                UpdateVisualEffects();
            }
            
            UpdatePowerConsumption();
        }
        
        /// <summary>
        /// Manual cooling control
        /// </summary>
        public void SetCooling(bool enabled, float intensity = 1f)
        {
            if (_isAutomated) return;
            
            _coolingActive = enabled && _isOperational;
            
            if (_coolingActive)
            {
                _heatingActive = false; // Can't heat and cool at the same time
                UpdateVisualEffects();
            }
            
            UpdatePowerConsumption();
        }
        
        /// <summary>
        /// Manual humidity control
        /// </summary>
        public void SetHumidification(bool humidify, bool dehumidify = false)
        {
            if (_isAutomated) return;
            
            _humidifyingActive = humidify && _isOperational;
            _dehumidifyingActive = dehumidify && _isOperational && !humidify;
            
            UpdateVisualEffects();
            UpdatePowerConsumption();
        }
        
        /// <summary>
        /// System startup
        /// </summary>
        public void Startup()
        {
            _isOperational = true;
            OnSystemStateChanged?.Invoke(this);
            
            if (_isAutomated)
                UpdateControlLogic();
            
            ChimeraLogger.Log($"HVAC {SystemId} started up");
        }
        
        /// <summary>
        /// System shutdown
        /// </summary>
        public void Shutdown()
        {
            _isOperational = false;
            StopAllSystems();
            OnSystemStateChanged?.Invoke(this);
            
            ChimeraLogger.Log($"HVAC {SystemId} shut down");
        }
        
        /// <summary>
        /// Set cooling power level (0-1)
        /// </summary>
        public void SetCoolingPower(float power)
        {
            power = Mathf.Clamp01(power);
            SetCooling(power > 0f, power);
        }
        
        /// <summary>
        /// Set heating power level (0-1)
        /// </summary>
        public void SetHeatingPower(float power)
        {
            power = Mathf.Clamp01(power);
            SetHeating(power > 0f, power);
        }
        
        /// <summary>
        /// Set dehumidifier power level (0-1)
        /// </summary>
        public void SetDehumidifierPower(float power)
        {
            power = Mathf.Clamp01(power);
            SetHumidification(false, power > 0f);
        }
        
        /// <summary>
        /// Set humidifier power level (0-1)
        /// </summary>
        public void SetHumidifierPower(float power)
        {
            power = Mathf.Clamp01(power);
            SetHumidification(power > 0f, false);
        }
        
        /// <summary>
        /// Set eco-friendly mode
        /// </summary>
        public void SetEcoMode(bool enabled)
        {
            if (enabled)
            {
                // Reduce power consumption by lowering efficiency slightly
                _energyEfficiency = Mathf.Max(0.7f, _energyEfficiency * 0.9f);
            }
            else
            {
                _energyEfficiency = 0.85f; // Reset to default
            }
        }
        
        /// <summary>
        /// Make preemptive adjustment to temperature
        /// </summary>
        public void MakePreemptiveAdjustment(float targetTemp)
        {
            SetTargetTemperature(targetTemp);
            // Increase response speed for preemptive adjustments
            _responseTime = Mathf.Max(1f, _responseTime * 0.5f);
        }
        
        /// <summary>
        /// Make preemptive humidity adjustment
        /// </summary>
        public void MakePreemptiveHumidityAdjustment(float targetHumidity)
        {
            SetTargetHumidity(targetHumidity);
        }
        
        /// <summary>
        /// Set emergency mode for immediate response
        /// </summary>
        public void SetEmergencyMode(bool enabled)
        {
            if (enabled)
            {
                _responseTime = 0.5f; // Immediate response
                _energyEfficiency = 1.0f; // Maximum efficiency
                ChimeraLogger.Log("[HVAC] Emergency mode activated - immediate response enabled");
            }
            else
            {
                _responseTime = 5f; // Normal response time
                _energyEfficiency = 0.85f; // Normal efficiency
                ChimeraLogger.Log("[HVAC] Emergency mode deactivated - normal operation resumed");
            }
        }
        
        #endregion
        
        #region System Updates
        
        public void UpdateSystem()
        {
            if (!_isOperational) return;
            
            // Update environmental conditions based on system state
            UpdateEnvironmentalConditions();
            
            // Update automation logic
            if (_isAutomated)
            {
                UpdateControlLogic();
            }
            
            // Update power consumption
            UpdatePowerConsumption();
            
            // Update visual and audio effects
            UpdateVisualEffects();
            UpdateAudioEffects();
            
            // Track energy usage
            _totalEnergyUsed += _powerConsumption * (_updateInterval / 3600f); // kWh
            
            // Notify subscribers
            OnEnvironmentChanged?.Invoke(_currentTemperature, _currentHumidity);
            OnPowerConsumptionChanged?.Invoke(_powerConsumption);
        }
        
        private void UpdateEnvironmentalConditions()
        {
            float deltaTime = _updateInterval / 60f; // Convert to minutes
            
            // Temperature changes
            if (_heatingActive)
            {
                float heatingRate = _maxHeatingCapacity * _energyEfficiency * deltaTime;
                _currentTemperature += heatingRate * 0.5f; // Simplified heating model
            }
            else if (_coolingActive)
            {
                float coolingRate = _maxCoolingCapacity * _energyEfficiency * deltaTime;
                _currentTemperature -= coolingRate * 0.4f; // Simplified cooling model
            }
            else
            {
                // Natural temperature drift toward ambient
                float ambientTemp = 20f; // Simplified ambient temperature
                float drift = (ambientTemp - _currentTemperature) * 0.1f * deltaTime;
                _currentTemperature += drift;
            }
            
            // Humidity changes
            if (_humidifyingActive)
            {
                float humidificationRate = _maxHumidificationRate * deltaTime;
                _currentHumidity += humidificationRate * 2f; // Simplified humidification model
            }
            else if (_dehumidifyingActive)
            {
                float dehumidificationRate = _maxDehumidificationRate * deltaTime;
                _currentHumidity -= dehumidificationRate * 1.5f; // Simplified dehumidification model
            }
            else
            {
                // Natural humidity drift
                float ambientHumidity = 45f; // Simplified ambient humidity
                float drift = (ambientHumidity - _currentHumidity) * 0.05f * deltaTime;
                _currentHumidity += drift;
            }
            
            // Clamp values to realistic ranges
            _currentTemperature = Mathf.Clamp(_currentTemperature, 10f, 40f);
            _currentHumidity = Mathf.Clamp(_currentHumidity, 10f, 90f);
        }
        
        private void UpdateControlLogic()
        {
            if (!_isOperational || !_isAutomated) return;
            
            bool needsHeating = _currentTemperature < _targetTemperature - _temperatureTolerance;
            bool needsCooling = _currentTemperature > _targetTemperature + _temperatureTolerance;
            bool needsHumidification = _currentHumidity < _targetHumidity - _humidityTolerance;
            bool needsDehumidification = _currentHumidity > _targetHumidity + _humidityTolerance;
            
            // Temperature control logic
            if (needsHeating && !_heatingActive)
            {
                _heatingActive = true;
                _coolingActive = false;
                _cycleCount++;
                ChimeraLogger.Log($"HVAC {SystemId} starting heating cycle");
            }
            else if (needsCooling && !_coolingActive)
            {
                _coolingActive = true;
                _heatingActive = false;
                _cycleCount++;
                ChimeraLogger.Log($"HVAC {SystemId} starting cooling cycle");
            }
            else if (IsTemperatureInRange)
            {
                _heatingActive = false;
                _coolingActive = false;
            }
            
            // Humidity control logic
            if (needsHumidification && !_humidifyingActive)
            {
                _humidifyingActive = true;
                _dehumidifyingActive = false;
                ChimeraLogger.Log($"HVAC {SystemId} starting humidification");
            }
            else if (needsDehumidification && !_dehumidifyingActive)
            {
                _dehumidifyingActive = true;
                _humidifyingActive = false;
                ChimeraLogger.Log($"HVAC {SystemId} starting dehumidification");
            }
            else if (IsHumidityInRange)
            {
                _humidifyingActive = false;
                _dehumidifyingActive = false;
            }
        }
        
        private void UpdatePowerConsumption()
        {
            _powerConsumption = 0f;
            
            if (_heatingActive)
                _powerConsumption += _maxHeatingCapacity * 0.8f;
            if (_coolingActive)
                _powerConsumption += _maxCoolingCapacity;
            if (_humidifyingActive)
                _powerConsumption += 0.5f; // Humidifier power
            if (_dehumidifyingActive)
                _powerConsumption += 1.2f; // Dehumidifier power
            
            // Base system power (fans, controls, etc.)
            if (_isOperational)
                _powerConsumption += 0.2f;
        }
        
        #endregion
        
        #region Visual and Audio Effects
        
        private void UpdateVisualEffects()
        {
            // Heating effect
            if (_heatingEffect != null)
            {
                if (_heatingActive && !_heatingEffect.isPlaying)
                    _heatingEffect.Play();
                else if (!_heatingActive && _heatingEffect.isPlaying)
                    _heatingEffect.Stop();
            }
            
            // Cooling effect
            if (_coolingEffect != null)
            {
                if (_coolingActive && !_coolingEffect.isPlaying)
                    _coolingEffect.Play();
                else if (!_coolingActive && _coolingEffect.isPlaying)
                    _coolingEffect.Stop();
            }
            
            // Humidifier effect
            if (_humidifierEffect != null)
            {
                if (_humidifyingActive && !_humidifierEffect.isPlaying)
                    _humidifierEffect.Play();
                else if (!_humidifyingActive && _humidifierEffect.isPlaying)
                    _humidifierEffect.Stop();
            }
            
            // Dehumidifier effect
            if (_dehumidifierEffect != null)
            {
                if (_dehumidifyingActive && !_dehumidifierEffect.isPlaying)
                    _dehumidifierEffect.Play();
                else if (!_dehumidifyingActive && _dehumidifierEffect.isPlaying)
                    _dehumidifierEffect.Stop();
            }
        }
        
        private void UpdateAudioEffects()
        {
            if (_systemAudio == null) return;
            
            bool anySystemActive = _heatingActive || _coolingActive || _humidifyingActive || _dehumidifyingActive;
            
            if (anySystemActive && !_systemAudio.isPlaying)
            {
                _systemAudio.Play();
            }
            else if (!anySystemActive && _systemAudio.isPlaying)
            {
                _systemAudio.Stop();
            }
            
            // Adjust volume based on system load
            if (_systemAudio.isPlaying)
            {
                float volumeMultiplier = Mathf.Clamp01(_powerConsumption / (_maxHeatingCapacity + _maxCoolingCapacity));
                _systemAudio.volume = 0.2f + (volumeMultiplier * 0.3f);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private void StopAllSystems()
        {
            _heatingActive = false;
            _coolingActive = false;
            _humidifyingActive = false;
            _dehumidifyingActive = false;
            
            UpdateVisualEffects();
            UpdatePowerConsumption();
        }
        
        /// <summary>
        /// Get current system status
        /// </summary>
        public HVACStatus GetSystemStatus()
        {
            return new HVACStatus
            {
                SystemId = SystemId,
                Type = _hvacType,
                IsOperational = _isOperational,
                IsAutomated = _isAutomated,
                TargetTemperature = _targetTemperature,
                TargetHumidity = _targetHumidity,
                CurrentTemperature = _currentTemperature,
                CurrentHumidity = _currentHumidity,
                PowerConsumption = _powerConsumption,
                IsHeatingActive = _heatingActive,
                IsCoolingActive = _coolingActive,
                IsHumidifyingActive = _humidifyingActive,
                IsDehumidifyingActive = _dehumidifyingActive,
                IsWithinTargetRange = IsWithinTargetRange,
                EnergyEfficiency = _energyEfficiency,
                TotalEnergyUsed = _totalEnergyUsed,
                OperatingHours = _operatingHours,
                CycleCount = _cycleCount
            };
        }
        
        /// <summary>
        /// Reset system statistics
        /// </summary>
        public void ResetStatistics()
        {
            _totalEnergyUsed = 0f;
            _operatingHours = 0f;
            _cycleCount = 0;
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
    }
    
    // Supporting data structures
    [System.Serializable]
    public enum HVACType
    {
        Central_Air,
        Split_System,
        Heat_Pump,
        Mini_Split,
        Package_Unit
    }
    
    [System.Serializable]
    public class HVACStatus
    {
        public string SystemId;
        public HVACType Type;
        public bool IsOperational;
        public bool IsAutomated;
        public float TargetTemperature;
        public float TargetHumidity;
        public float CurrentTemperature;
        public float CurrentHumidity;
        public float PowerConsumption;
        public bool IsHeatingActive;
        public bool IsCoolingActive;
        public bool IsHumidifyingActive;
        public bool IsDehumidifyingActive;
        public bool IsWithinTargetRange;
        public float EnergyEfficiency;
        public float TotalEnergyUsed;
        public float OperatingHours;
        public int CycleCount;
    }
}