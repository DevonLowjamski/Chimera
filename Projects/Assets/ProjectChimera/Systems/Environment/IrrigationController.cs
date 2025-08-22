using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Interfaces;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Controls irrigation and nutrient delivery systems.
    /// Manages watering schedules, nutrient mixing, and automated feeding.
    /// </summary>
    public class IrrigationController : MonoBehaviour
    {
        [Header("Irrigation Configuration")]
        [SerializeField] private string _systemId;
        [SerializeField] private IrrigationType _systemType = IrrigationType.Drip_System;
        [SerializeField] private float _maxFlowRate = 10f; // Liters per minute
        [SerializeField] private float _tankCapacity = 200f; // Liters
        [SerializeField] private float _powerConsumption = 50f; // Watts
        
        [Header("Water Management")]
        [SerializeField] private float _currentWaterLevel = 100f; // Liters
        [SerializeField] private float _waterQuality = 95f; // 0-100%
        [SerializeField] private float _waterTemperature = 20f; // °C
        [SerializeField] private float _pH = 6.5f; // pH level
        [SerializeField] private float _ec = 1.2f; // Electrical Conductivity (mS/cm)
        
        [Header("Nutrient System")]
        [SerializeField] private bool _enableNutrientMixing = true;
        [SerializeField] private float _nutrientConcentration = 800f; // ppm
        [SerializeField] private NutrientProfile _currentNutrients;
        
        [Header("Automation")]
        [SerializeField] private bool _enableAutomation = true;
        [SerializeField] private IrrigationSchedule _irrigationSchedule;
        [SerializeField] private float _moistureThreshold = 30f; // %
        [SerializeField] private float _scheduleInterval = 3600f; // 1 hour
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _waterSprayEffect;
        [SerializeField] private ParticleSystem _dripEffect;
        [SerializeField] private AudioSource _pumpAudio;
        [SerializeField] private Transform _waterLevelIndicator;
        
        // System State
        private bool _isOperational = true;
        private bool _isAutomated = false;
        private bool _isIrrigating = false;
        private float _currentFlowRate = 0f;
        private float _currentPowerConsumption = 0f;
        private float _lastScheduleCheck = 0f;
        
        // Performance Tracking
        private float _totalWaterUsed = 0f; // Liters
        private float _operatingHours = 0f;
        private int _irrigationCycles = 0;
        private float _totalEnergyUsed = 0f; // kWh
        
        // Connected Plants
        private List<IWaterableComponent> _connectedPlants = new List<IWaterableComponent>();
        
        // Events
        public System.Action<IrrigationController> OnSystemStateChanged;
        public System.Action<float> OnWaterLevelChanged;
        public System.Action<float> OnFlowRateChanged;
        public System.Action<NutrientProfile> OnNutrientsChanged;
        
        // Properties
        public string SystemId => string.IsNullOrEmpty(_systemId) ? name : _systemId;
        public IrrigationType Type => _systemType;
        public bool IsOperational => _isOperational;
        public bool IsAutomated => _isAutomated;
        public bool IsIrrigating => _isIrrigating;
        public float CurrentWaterLevel => _currentWaterLevel;
        public float WaterPercentage => _currentWaterLevel / _tankCapacity;
        public float CurrentFlowRate => _currentFlowRate;
        public float PowerConsumption => _currentPowerConsumption;
        public NutrientProfile CurrentNutrients => _currentNutrients;
        public bool NeedsRefill => _currentWaterLevel < _tankCapacity * 0.2f;
        
        private void Awake()
        {
            if (string.IsNullOrEmpty(_systemId))
                _systemId = System.Guid.NewGuid().ToString();
            
            InitializeNutrientProfile();
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
            if (_isIrrigating)
                _operatingHours += Time.deltaTime / 3600f;
        }
        
        private void InitializeSystem()
        {
            // Find connected plants
            RefreshConnectedPlants();
            
            // Initialize schedule if none exists
            if (_irrigationSchedule == null)
                CreateDefaultSchedule();
            
            // Set up audio
            if (_pumpAudio != null)
            {
                _pumpAudio.loop = true;
                _pumpAudio.volume = 0.2f;
            }
            
            Debug.Log($"Irrigation System {SystemId} initialized - Type: {_systemType}");
        }
        
        private void InitializeNutrientProfile()
        {
            _currentNutrients = new NutrientProfile
            {
                Nitrogen = 150f,    // ppm
                Phosphorus = 50f,   // ppm
                Potassium = 200f,   // ppm
                Calcium = 160f,     // ppm
                Magnesium = 60f,    // ppm
                Sulfur = 80f,       // ppm
                Iron = 3f,          // ppm
                Manganese = 1f,     // ppm
                Zinc = 0.5f,        // ppm
                Copper = 0.2f,      // ppm
                Boron = 0.5f,       // ppm
                Molybdenum = 0.05f  // ppm
            };
        }
        
        #region Public Control Methods
        
        /// <summary>
        /// Start irrigation cycle
        /// </summary>
        public void StartIrrigation(float flowRate = -1f, float duration = 300f)
        {
            if (!_isOperational || _currentWaterLevel <= 0f) return;
            
            if (flowRate < 0f)
                flowRate = _maxFlowRate;
            
            _currentFlowRate = Mathf.Clamp(flowRate, 0f, _maxFlowRate);
            _isIrrigating = true;
            _irrigationCycles++;
            
            StartCoroutine(IrrigationCycle(duration));
            
            UpdatePowerConsumption();
            OnSystemStateChanged?.Invoke(this);
            OnFlowRateChanged?.Invoke(_currentFlowRate);
            
            Debug.Log($"Irrigation {SystemId} started - Flow: {_currentFlowRate:F1} L/min for {duration}s");
        }
        
        /// <summary>
        /// Stop irrigation
        /// </summary>
        public void StopIrrigation()
        {
            _isIrrigating = false;
            _currentFlowRate = 0f;
            
            UpdatePowerConsumption();
            OnSystemStateChanged?.Invoke(this);
            OnFlowRateChanged?.Invoke(_currentFlowRate);
            
            Debug.Log($"Irrigation {SystemId} stopped");
        }
        
        /// <summary>
        /// Refill water tank
        /// </summary>
        public void RefillWaterTank(float amount = -1f)
        {
            if (amount < 0f)
                amount = _tankCapacity - _currentWaterLevel;
            
            float oldLevel = _currentWaterLevel;
            _currentWaterLevel = Mathf.Min(_currentWaterLevel + amount, _tankCapacity);
            
            OnWaterLevelChanged?.Invoke(_currentWaterLevel);
            
            Debug.Log($"Irrigation {SystemId} refilled: +{_currentWaterLevel - oldLevel:F1}L (Total: {_currentWaterLevel:F1}L)");
        }
        
        /// <summary>
        /// Set nutrient profile
        /// </summary>
        public void SetNutrientProfile(NutrientProfile nutrients)
        {
            _currentNutrients = nutrients;
            OnNutrientsChanged?.Invoke(_currentNutrients);
            
            Debug.Log($"Irrigation {SystemId} nutrient profile updated");
        }
        
        /// <summary>
        /// Adjust pH level
        /// </summary>
        public void AdjustPH(float targetPH)
        {
            targetPH = Mathf.Clamp(targetPH, 4f, 8f);
            _pH = Mathf.Lerp(_pH, targetPH, Time.deltaTime * 0.5f);
            
            Debug.Log($"Irrigation {SystemId} pH adjusting to {targetPH:F1} (Current: {_pH:F1})");
        }
        
        /// <summary>
        /// Set irrigation schedule
        /// </summary>
        public void SetIrrigationSchedule(IrrigationSchedule schedule)
        {
            _irrigationSchedule = schedule;
            
            Debug.Log($"Irrigation {SystemId} schedule updated");
        }
        
        /// <summary>
        /// Enable or disable automation
        /// </summary>
        public void SetAutomationMode(bool enabled)
        {
            _isAutomated = enabled;
            
            OnSystemStateChanged?.Invoke(this);
            Debug.Log($"Irrigation {SystemId} automation {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// System startup
        /// </summary>
        public void Startup()
        {
            _isOperational = true;
            OnSystemStateChanged?.Invoke(this);
            
            Debug.Log($"Irrigation {SystemId} started up");
        }
        
        /// <summary>
        /// System shutdown
        /// </summary>
        public void Shutdown()
        {
            _isOperational = false;
            StopIrrigation();
            
            OnSystemStateChanged?.Invoke(this);
            Debug.Log($"Irrigation {SystemId} shut down");
        }
        
        #endregion
        
        #region System Updates
        
        public void UpdateSystem()
        {
            // Check irrigation schedule
            if (_isAutomated && Time.time - _lastScheduleCheck >= _scheduleInterval)
            {
                CheckIrrigationSchedule();
                CheckPlantMoisture();
                _lastScheduleCheck = Time.time;
            }
            
            // Update water consumption
            if (_isIrrigating)
            {
                float consumption = _currentFlowRate * Time.deltaTime / 60f; // L/min to L/s
                _currentWaterLevel = Mathf.Max(_currentWaterLevel - consumption, 0f);
                _totalWaterUsed += consumption;
                
                OnWaterLevelChanged?.Invoke(_currentWaterLevel);
                
                // Stop if out of water
                if (_currentWaterLevel <= 0f)
                {
                    StopIrrigation();
                    Debug.LogWarning($"Irrigation {SystemId} stopped - out of water");
                }
            }
            
            // Update power consumption
            UpdatePowerConsumption();
            
            // Track energy usage
            float deltaTime = Time.deltaTime / 3600f; // Convert to hours
            _totalEnergyUsed += _currentPowerConsumption * deltaTime / 1000f; // Convert to kWh
            
            // Update water level indicator
            UpdateWaterLevelIndicator();
        }
        
        private void CheckIrrigationSchedule()
        {
            if (_irrigationSchedule?.ScheduleEntries == null) return;
            
            var currentTime = System.DateTime.Now.TimeOfDay;
            
            foreach (var entry in _irrigationSchedule.ScheduleEntries)
            {
                if (IsTimeInRange(currentTime, entry.StartTime, entry.EndTime))
                {
                    if (!_isIrrigating)
                    {
                        StartIrrigation(entry.FlowRate, entry.Duration);
                        break;
                    }
                }
            }
        }
        
        private void CheckPlantMoisture()
        {
            RefreshConnectedPlants();
            
            bool needsWatering = false;
            foreach (var plant in _connectedPlants)
            {
                if (plant.WaterLevel < _moistureThreshold)
                {
                    needsWatering = true;
                    break;
                }
            }
            
            if (needsWatering && !_isIrrigating)
            {
                StartIrrigation(-1f, 180f); // 3 minute cycle
            }
        }
        
        private void UpdatePowerConsumption()
        {
            if (_isIrrigating && _isOperational)
            {
                float pumpLoad = _currentFlowRate / _maxFlowRate;
                _currentPowerConsumption = _powerConsumption * pumpLoad;
                
                // Add nutrient mixing power if enabled
                if (_enableNutrientMixing)
                    _currentPowerConsumption += 20f; // Additional mixing pump power
            }
            else
            {
                _currentPowerConsumption = 0f;
            }
        }
        
        private void UpdateWaterLevelIndicator()
        {
            if (_waterLevelIndicator == null) return;
            
            float percentage = WaterPercentage;
            Vector3 scale = _waterLevelIndicator.localScale;
            scale.y = percentage;
            _waterLevelIndicator.localScale = scale;
        }
        
        #endregion
        
        #region Visual Effects
        
        private void UpdateVisualEffects()
        {
            UpdateWaterEffects();
            UpdateAudioEffects();
        }
        
        private void UpdateWaterEffects()
        {
            // Water spray effect for overhead sprinklers
            if (_waterSprayEffect != null)
            {
                bool shouldSpray = _isIrrigating && (_systemType == IrrigationType.Overhead_Sprinkler);
                
                if (shouldSpray && !_waterSprayEffect.isPlaying)
                    _waterSprayEffect.Play();
                else if (!shouldSpray && _waterSprayEffect.isPlaying)
                    _waterSprayEffect.Stop();
            }
            
            // Drip effect for drip systems
            if (_dripEffect != null)
            {
                bool shouldDrip = _isIrrigating && (_systemType == IrrigationType.Drip_System);
                
                if (shouldDrip && !_dripEffect.isPlaying)
                    _dripEffect.Play();
                else if (!shouldDrip && _dripEffect.isPlaying)
                    _dripEffect.Stop();
                
                // Adjust drip rate
                if (_dripEffect.isPlaying)
                {
                    var emission = _dripEffect.emission;
                    emission.rateOverTime = (_currentFlowRate / _maxFlowRate) * 50f;
                }
            }
        }
        
        private void UpdateAudioEffects()
        {
            if (_pumpAudio == null) return;
            
            if (_isIrrigating && _isOperational)
            {
                if (!_pumpAudio.isPlaying)
                    _pumpAudio.Play();
                
                // Adjust volume based on flow rate
                float intensity = _currentFlowRate / _maxFlowRate;
                _pumpAudio.volume = 0.1f + (intensity * 0.2f);
            }
            else
            {
                if (_pumpAudio.isPlaying)
                    _pumpAudio.Stop();
            }
        }
        
        #endregion
        
        #region Plant Management
        
        private void RefreshConnectedPlants()
        {
            _connectedPlants.Clear();
            
            // Find all components that implement IWaterableComponent
            var allMonoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in allMonoBehaviours)
            {
                if (mb is IWaterableComponent waterableComponent)
                {
                    // Check if within irrigation range (10 units for example)
                    float distance = Vector3.Distance(transform.position, waterableComponent.Transform.position);
                    if (distance <= 10f)
                    {
                        _connectedPlants.Add(waterableComponent);
                    }
                }
            }
            
            Debug.Log($"Irrigation System {SystemId} connected to {_connectedPlants.Count} plants");
        }
        
        /// <summary>
        /// Water connected plants
        /// </summary>
        private void WaterConnectedPlants(float amount)
        {
            if (_connectedPlants.Count == 0) return;
            
            float waterPerPlant = amount / _connectedPlants.Count;
            
            foreach (var plant in _connectedPlants)
            {
                plant.AddWater(waterPerPlant);
            }
            
            Debug.Log($"Irrigation {SystemId} watered {_connectedPlants.Count} plants with {waterPerPlant:F1}L each");
        }
        
        #endregion
        
        #region Irrigation Cycle
        
        private System.Collections.IEnumerator IrrigationCycle(float duration)
        {
            float elapsed = 0f;
            float waterPerSecond = _currentFlowRate / 60f; // Convert L/min to L/s
            
            while (elapsed < duration && _isIrrigating && _currentWaterLevel > 0f)
            {
                elapsed += Time.deltaTime;
                
                // Water plants every 5 seconds during cycle
                if (Mathf.FloorToInt(elapsed) % 5 == 0)
                {
                    WaterConnectedPlants(20f); // 20% water level boost
                }
                
                yield return null;
            }
            
            StopIrrigation();
        }
        
        #endregion
        
        #region Utility Methods
        
        private void CreateDefaultSchedule()
        {
            _irrigationSchedule = new IrrigationSchedule
            {
                Name = "Default Irrigation Schedule",
                ScheduleEntries = new List<IrrigationScheduleEntry>
                {
                    new IrrigationScheduleEntry
                    {
                        StartTime = System.TimeSpan.FromHours(8), // 8 AM
                        EndTime = System.TimeSpan.FromHours(8.1), // 8:06 AM
                        FlowRate = _maxFlowRate,
                        Duration = 300f // 5 minutes
                    },
                    new IrrigationScheduleEntry
                    {
                        StartTime = System.TimeSpan.FromHours(18), // 6 PM
                        EndTime = System.TimeSpan.FromHours(18.1), // 6:06 PM
                        FlowRate = _maxFlowRate,
                        Duration = 300f // 5 minutes
                    }
                }
            };
        }
        
        private bool IsTimeInRange(System.TimeSpan current, System.TimeSpan start, System.TimeSpan end)
        {
            if (start <= end)
                return current >= start && current <= end;
            else
                return current >= start || current <= end; // Handles overnight schedules
        }
        
        /// <summary>
        /// Get current system status
        /// </summary>
        public IrrigationStatus GetSystemStatus()
        {
            return new IrrigationStatus
            {
                SystemId = SystemId,
                Type = _systemType,
                IsOperational = _isOperational,
                IsAutomated = _isAutomated,
                IsIrrigating = _isIrrigating,
                CurrentWaterLevel = _currentWaterLevel,
                WaterPercentage = WaterPercentage,
                CurrentFlowRate = _currentFlowRate,
                MaxFlowRate = _maxFlowRate,
                PowerConsumption = _currentPowerConsumption,
                WaterQuality = _waterQuality,
                pH = _pH,
                EC = _ec,
                NutrientConcentration = _nutrientConcentration,
                ConnectedPlants = _connectedPlants.Count,
                TotalWaterUsed = _totalWaterUsed,
                OperatingHours = _operatingHours,
                IrrigationCycles = _irrigationCycles,
                TotalEnergyUsed = _totalEnergyUsed,
                NeedsRefill = NeedsRefill
            };
        }
        
        #endregion
    }
    
    // Supporting data structures
    [System.Serializable]
    public enum IrrigationType
    {
        Drip_System,
        Overhead_Sprinkler,
        Flood_Table,
        Deep_Water_Culture,
        Nutrient_Film_Technique,
        Aeroponics
    }
    
    [System.Serializable]
    public class NutrientProfile
    {
        [Header("Primary Nutrients (NPK)")]
        public float Nitrogen;      // ppm
        public float Phosphorus;    // ppm
        public float Potassium;     // ppm
        
        [Header("Secondary Nutrients")]
        public float Calcium;       // ppm
        public float Magnesium;     // ppm
        public float Sulfur;        // ppm
        
        [Header("Micronutrients")]
        public float Iron;          // ppm
        public float Manganese;     // ppm
        public float Zinc;          // ppm
        public float Copper;        // ppm
        public float Boron;         // ppm
        public float Molybdenum;    // ppm
    }
    
    [System.Serializable]
    public class IrrigationSchedule
    {
        public string Name;
        public List<IrrigationScheduleEntry> ScheduleEntries;
    }
    
    [System.Serializable]
    public class IrrigationScheduleEntry
    {
        public System.TimeSpan StartTime;
        public System.TimeSpan EndTime;
        public float FlowRate;      // L/min
        public float Duration;      // seconds
    }
    
    [System.Serializable]
    public class IrrigationStatus
    {
        public string SystemId;
        public IrrigationType Type;
        public bool IsOperational;
        public bool IsAutomated;
        public bool IsIrrigating;
        public float CurrentWaterLevel;
        public float WaterPercentage;
        public float CurrentFlowRate;
        public float MaxFlowRate;
        public float PowerConsumption;
        public float WaterQuality;
        public float pH;
        public float EC;
        public float NutrientConcentration;
        public int ConnectedPlants;
        public float TotalWaterUsed;
        public float OperatingHours;
        public int IrrigationCycles;
        public float TotalEnergyUsed;
        public bool NeedsRefill;
    }
}