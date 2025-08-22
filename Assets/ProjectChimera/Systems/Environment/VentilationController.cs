using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Controls ventilation and air circulation systems.
    /// Manages CO2 levels, air exchange, and circulation for optimal plant growth.
    /// </summary>
    public class VentilationController : MonoBehaviour
    {
        [Header("Ventilation Configuration")]
        [SerializeField] private string _systemId;
        [SerializeField] private VentilationSystemType _systemType = VentilationSystemType.Exhaust_Fan;
        [SerializeField] private float _maxAirflow = 1000f; // CFM (Cubic Feet per Minute)
        [SerializeField] private float _powerConsumption = 150f; // Watts
        
        [Header("Control Settings")]
        [SerializeField] private float _targetCO2 = 1000f; // ppm
        [SerializeField] private float _currentCO2 = 400f; // ppm
        [SerializeField] private float _co2Tolerance = 100f; // ppm
        [SerializeField] private bool _enableAutomation = true;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _airflowEffect;
        [SerializeField] private AudioSource _fanAudio;
        [SerializeField] private Transform _fanBlades;
        [SerializeField] private float _fanRotationSpeed = 360f; // degrees per second
        
        // System State
        private bool _isOperational = true;
        private bool _isAutomated = false;
        private bool _isRunning = false;
        private float _currentAirflow = 0f;
        private float _currentPowerConsumption = 0f;
        
        // Performance Metrics
        private float _operatingHours = 0f;
        private float _totalEnergyUsed = 0f;
        private int _onOffCycles = 0;
        
        // Events
        public System.Action<VentilationController> OnSystemStateChanged;
        public System.Action<float> OnCO2LevelChanged;
        public System.Action<float> OnAirflowChanged;
        
        // Properties
        public string SystemId => string.IsNullOrEmpty(_systemId) ? name : _systemId;
        public VentilationSystemType Type => _systemType;
        public bool IsOperational => _isOperational;
        public bool IsAutomated => _isAutomated;
        public bool IsRunning => _isRunning;
        public float TargetCO2 => _targetCO2;
        public float CurrentCO2 => _currentCO2;
        public float CurrentAirflow => _currentAirflow;
        public float PowerConsumption => _currentPowerConsumption;
        
        // Compatibility property for AdvancedGrowRoomController
        public float AirFlowRate => _currentAirflow;
        
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
        }
        
        private void Update()
        {
            if (!_isOperational) return;
            
            UpdateSystem();
            UpdateVisualEffects();
            
            // Track operating hours
            if (_isRunning)
                _operatingHours += Time.deltaTime / 3600f;
        }
        
        private void InitializeSystem()
        {
            // Set up audio
            if (_fanAudio != null)
            {
                _fanAudio.loop = true;
                _fanAudio.volume = 0.3f;
            }
            
            Debug.Log($"Ventilation System {SystemId} initialized - Type: {_systemType}");
        }
        
        #region Public Control Methods
        
        /// <summary>
        /// Set target CO2 level
        /// </summary>
        public void SetTargetCO2(float co2Level)
        {
            _targetCO2 = Mathf.Clamp(co2Level, 300f, 2000f);
            
            if (_isAutomated)
                UpdateAutomationLogic();
            
            Debug.Log($"Ventilation {SystemId} target CO2 set to {_targetCO2} ppm");
        }
        
        /// <summary>
        /// Start ventilation system
        /// </summary>
        public void StartVentilation(float intensity = 1f)
        {
            if (!_isOperational) return;
            
            bool wasRunning = _isRunning;
            _isRunning = true;
            _currentAirflow = _maxAirflow * Mathf.Clamp01(intensity);
            
            if (!wasRunning)
                _onOffCycles++;
            
            UpdatePowerConsumption();
            OnSystemStateChanged?.Invoke(this);
            OnAirflowChanged?.Invoke(_currentAirflow);
            
            Debug.Log($"Ventilation {SystemId} started - Airflow: {_currentAirflow:F0} CFM");
        }
        
        /// <summary>
        /// Stop ventilation system
        /// </summary>
        public void StopVentilation()
        {
            _isRunning = false;
            _currentAirflow = 0f;
            
            UpdatePowerConsumption();
            OnSystemStateChanged?.Invoke(this);
            OnAirflowChanged?.Invoke(_currentAirflow);
            
            Debug.Log($"Ventilation {SystemId} stopped");
        }
        
        /// <summary>
        /// Set airflow rate
        /// </summary>
        public void SetAirflow(float airflowCFM)
        {
            if (_isAutomated) return;
            
            _currentAirflow = Mathf.Clamp(airflowCFM, 0f, _maxAirflow);
            _isRunning = _currentAirflow > 0f;
            
            UpdatePowerConsumption();
            OnAirflowChanged?.Invoke(_currentAirflow);
        }
        
        /// <summary>
        /// Set target airflow rate (alias for SetAirflow for compatibility)
        /// </summary>
        public void SetTargetAirFlow(float airflowCFM)
        {
            SetAirflow(airflowCFM);
        }
        
        /// <summary>
        /// Enable or disable automation
        /// </summary>
        public void SetAutomationMode(bool enabled)
        {
            _isAutomated = enabled;
            
            if (enabled)
                UpdateAutomationLogic();
            
            OnSystemStateChanged?.Invoke(this);
            Debug.Log($"Ventilation {SystemId} automation {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// System startup
        /// </summary>
        public void Startup()
        {
            _isOperational = true;
            
            if (_isAutomated)
                UpdateAutomationLogic();
            
            OnSystemStateChanged?.Invoke(this);
            Debug.Log($"Ventilation {SystemId} started up");
        }
        
        /// <summary>
        /// System shutdown
        /// </summary>
        public void Shutdown()
        {
            _isOperational = false;
            StopVentilation();
            
            OnSystemStateChanged?.Invoke(this);
            Debug.Log($"Ventilation {SystemId} shut down");
        }
        
        #endregion
        
        #region System Updates
        
        public void UpdateSystem()
        {
            // Update CO2 levels based on ventilation
            UpdateCO2Levels();
            
            // Update automation logic
            if (_isAutomated)
                UpdateAutomationLogic();
            
            // Update power consumption
            UpdatePowerConsumption();
            
            // Track energy usage
            float deltaTime = Time.deltaTime / 3600f; // Convert to hours
            _totalEnergyUsed += _currentPowerConsumption * deltaTime / 1000f; // Convert to kWh
        }
        
        private void UpdateCO2Levels()
        {
            float deltaTime = Time.deltaTime;
            
            if (_isRunning && _currentAirflow > 0f)
            {
                // Ventilation reduces CO2 levels
                float ventilationEffect = (_currentAirflow / _maxAirflow) * 50f * deltaTime; // Simplified model
                _currentCO2 = Mathf.Max(_currentCO2 - ventilationEffect, 350f); // Minimum outdoor CO2
            }
            else
            {
                // CO2 naturally accumulates
                float accumulation = 20f * deltaTime; // Simplified accumulation
                _currentCO2 = Mathf.Min(_currentCO2 + accumulation, 2000f); // Maximum realistic level
            }
            
            OnCO2LevelChanged?.Invoke(_currentCO2);
        }
        
        private void UpdateAutomationLogic()
        {
            if (!_isOperational || !_isAutomated) return;
            
            float co2Difference = _currentCO2 - _targetCO2;
            
            if (co2Difference > _co2Tolerance)
            {
                // CO2 too high - increase ventilation
                float intensity = Mathf.Clamp01(co2Difference / 500f); // Scale based on difference
                StartVentilation(intensity);
            }
            else if (co2Difference < -_co2Tolerance && _currentCO2 < _targetCO2 - _co2Tolerance)
            {
                // CO2 too low - reduce or stop ventilation
                if (_isRunning)
                {
                    float intensity = Mathf.Clamp01(1f - Mathf.Abs(co2Difference) / 300f);
                    if (intensity < 0.1f)
                        StopVentilation();
                    else
                        StartVentilation(intensity);
                }
            }
        }
        
        private void UpdatePowerConsumption()
        {
            if (_isRunning && _isOperational)
            {
                float loadPercentage = _currentAirflow / _maxAirflow;
                _currentPowerConsumption = _powerConsumption * loadPercentage;
            }
            else
            {
                _currentPowerConsumption = 0f;
            }
        }
        
        #endregion
        
        #region Visual Effects
        
        private void UpdateVisualEffects()
        {
            UpdateAirflowEffect();
            UpdateFanRotation();
            UpdateAudioEffects();
        }
        
        private void UpdateAirflowEffect()
        {
            if (_airflowEffect == null) return;
            
            if (_isRunning && _isOperational)
            {
                if (!_airflowEffect.isPlaying)
                    _airflowEffect.Play();
                
                // Adjust particle system based on airflow
                var main = _airflowEffect.main;
                var emission = _airflowEffect.emission;
                
                float intensity = _currentAirflow / _maxAirflow;
                main.startSpeed = intensity * 5f;
                emission.rateOverTime = intensity * 100f;
            }
            else
            {
                if (_airflowEffect.isPlaying)
                    _airflowEffect.Stop();
            }
        }
        
        private void UpdateFanRotation()
        {
            if (_fanBlades == null || !_isRunning) return;
            
            float rotationSpeed = (_currentAirflow / _maxAirflow) * _fanRotationSpeed;
            _fanBlades.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
        
        private void UpdateAudioEffects()
        {
            if (_fanAudio == null) return;
            
            if (_isRunning && _isOperational)
            {
                if (!_fanAudio.isPlaying)
                    _fanAudio.Play();
                
                // Adjust volume and pitch based on airflow
                float intensity = _currentAirflow / _maxAirflow;
                _fanAudio.volume = 0.2f + (intensity * 0.3f);
                _fanAudio.pitch = 0.8f + (intensity * 0.4f);
            }
            else
            {
                if (_fanAudio.isPlaying)
                    _fanAudio.Stop();
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get current system status
        /// </summary>
        public VentilationStatus GetSystemStatus()
        {
            return new VentilationStatus
            {
                SystemId = SystemId,
                Type = _systemType,
                IsOperational = _isOperational,
                IsAutomated = _isAutomated,
                IsRunning = _isRunning,
                TargetCO2 = _targetCO2,
                CurrentCO2 = _currentCO2,
                CurrentAirflow = _currentAirflow,
                MaxAirflow = _maxAirflow,
                PowerConsumption = _currentPowerConsumption,
                OperatingHours = _operatingHours,
                TotalEnergyUsed = _totalEnergyUsed,
                OnOffCycles = _onOffCycles
            };
        }
        
        /// <summary>
        /// Reset system statistics
        /// </summary>
        public void ResetStatistics()
        {
            _operatingHours = 0f;
            _totalEnergyUsed = 0f;
            _onOffCycles = 0;
        }
        
        #endregion
    }
    
    // Supporting data structures
    [System.Serializable]
    public enum VentilationSystemType
    {
        Exhaust_Fan,
        Intake_Fan,
        Carbon_Filter,
        Heat_Recovery_Ventilator,
        Energy_Recovery_Ventilator
    }
    
    [System.Serializable]
    public class VentilationStatus
    {
        public string SystemId;
        public VentilationSystemType Type;
        public bool IsOperational;
        public bool IsAutomated;
        public bool IsRunning;
        public float TargetCO2;
        public float CurrentCO2;
        public float CurrentAirflow;
        public float MaxAirflow;
        public float PowerConsumption;
        public float OperatingHours;
        public float TotalEnergyUsed;
        public int OnOffCycles;
    }
}