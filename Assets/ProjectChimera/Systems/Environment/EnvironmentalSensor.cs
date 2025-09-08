using ProjectChimera.Core.Logging;
using UnityEngine;
using UnityEngine.UI;
// using TMPro;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Systems.Environment;

// Removed dependency on Systems.Cultivation to avoid cross-assembly reference
using System.Collections.Generic;
using System.Linq;
using System;
// using AutomationSensorType = ProjectChimera.Data.Automation.SensorType; // Removed: Data.Automation namespace not available
using UnityCamera = UnityEngine.Camera;

using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Environmental sensor component that provides accurate readings
    /// and supports IoT-style monitoring and alerts.
    /// </summary>
    public class EnvironmentalSensor : MonoBehaviour, ITickable
    {
        [Header("Sensor Configuration")]
        [SerializeField] private LocalSensorType _sensorType;
        [SerializeField] private string _sensorId;
        [SerializeField] private float _readingInterval = 1f;
        [SerializeField] private float _accuracy = 0.95f;
        [SerializeField] private float _noiseLevel = 0.02f;
        
        [Header("Calibration")]
        [SerializeField] private float _calibrationOffset = 0f;
        [SerializeField] private float _calibrationMultiplier = 1f;
        [SerializeField] private bool _needsCalibration = false;
        
        [Header("Alert System")]
        [SerializeField] private bool _enableAlerts = true;
        [SerializeField] private float _alertThresholdLow = 0f;
        [SerializeField] private float _alertThresholdHigh = 100f;
        [SerializeField] private float _criticalThresholdLow = 0f;
        [SerializeField] private float _criticalThresholdHigh = 100f;
        
        [Header("Visual Indicators")]
        [SerializeField] private Renderer _sensorIndicator;
        [SerializeField] private Light _sensorLight;
        [SerializeField] private ParticleSystem _alertParticles;
        [SerializeField] private Canvas _readingDisplay;
        // [SerializeField] private TMPro.TextMeshProUGUI _readingText;
        [SerializeField] private UnityEngine.UI.Text _readingText; // Fallback to standard Text component
        
        // Sensor State
        private float _currentReading = 0f;
        private float _rawReading = 0f;
        private float _lastReading = 0f;
        private List<float> _readingHistory = new List<float>();
        private const int MAX_HISTORY_SIZE = 600; // 10 minutes at 1-second intervals
        
        // Timing
        private float _lastReadingTime = 0f;
        private DateTime _lastAlertTime = DateTime.MinValue;
        private float _alertCooldown = 30f; // 30 seconds between alerts
        
        // Status
        private SensorStatus _status = SensorStatus.Initializing;
        private SensorAlert _currentAlert = null;
        private MonoBehaviour _parentRoom;
        
        // Network/IoT Simulation
        private bool _isOnline = true;
        private float _lastNetworkUpdate = 0f;
        private float _networkUpdateInterval = 10f;
        private Queue<SensorDataPacket> _dataBuffer = new Queue<SensorDataPacket>();
        
        // Events
        public System.Action<EnvironmentalSensor, float> OnReadingChanged;
        public System.Action<EnvironmentalSensor, SensorAlert> OnAlertTriggered;
        public System.Action<EnvironmentalSensor, SensorStatus> OnStatusChanged;
        
        // Properties
        public LocalSensorType SensorType => _sensorType;
        public string SensorId => _sensorId;
        public float CurrentReading => _currentReading;
        public float RawReading => _rawReading;
        public SensorStatus Status => _status;
        public bool IsOnline => _isOnline;
        public bool HasAlert => _currentAlert != null;
        public SensorAlert CurrentAlert => _currentAlert;
        public List<float> ReadingHistory => _readingHistory.ToList();
        
        // Compatibility property removed: ProjectChimera.Data.Automation namespace no longer exists in refactor
        
        private void Awake()
        {
            InitializeSensor();
        }
        
        private void Start()
        {
            SetupSensorVisualization();
            SetStatus(SensorStatus.Active);
            StartReading();
            
            // Register with UpdateOrchestrator for centralized update management
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }
        
        #region ITickable Implementation

        public int Priority => TickPriority.EnvironmentalManager;
        public bool Enabled => !_needsCalibration && _isOnline && _status == SensorStatus.Active;

        public void Tick(float deltaTime)
        {
            if (!Enabled) return;
            
            float currentTime = Time.time;
            
            // Take readings at specified interval
            if (currentTime - _lastReadingTime >= _readingInterval)
            {
                TakeReading();
                _lastReadingTime = currentTime;
            }
            
            // Update network status
            if (currentTime - _lastNetworkUpdate >= _networkUpdateInterval)
            {
                UpdateNetworkStatus();
                _lastNetworkUpdate = currentTime;
            }
            
            // Update visual indicators
            UpdateVisualization();
        }

        #endregion
        
        #region Initialization
        
        private void InitializeSensor()
        {
            // Generate sensor ID if not set
            if (string.IsNullOrEmpty(_sensorId))
            {
                _sensorId = $"{_sensorType}_{Guid.NewGuid().ToString("N")[..8]}";
            }
            
            // Set default thresholds based on sensor type
            SetDefaultThresholds();
            
            // Initialize reading history
            _readingHistory = new List<float>();
        }
        
        private void SetDefaultThresholds()
        {
            switch (_sensorType)
            {
                case LocalSensorType.Temperature:
                    _alertThresholdLow = 18f;
                    _alertThresholdHigh = 28f;
                    _criticalThresholdLow = 15f;
                    _criticalThresholdHigh = 32f;
                    break;
                    
                case LocalSensorType.Humidity:
                    _alertThresholdLow = 40f;
                    _alertThresholdHigh = 80f;
                    _criticalThresholdLow = 30f;
                    _criticalThresholdHigh = 90f;
                    break;
                    
                case LocalSensorType.LightLevel:
                    _alertThresholdLow = 100f;
                    _alertThresholdHigh = 1000f;
                    _criticalThresholdLow = 50f;
                    _criticalThresholdHigh = 1200f;
                    break;
                    
                case LocalSensorType.CO2Level:
                    _alertThresholdLow = 300f;
                    _alertThresholdHigh = 1500f;
                    _criticalThresholdLow = 200f;
                    _criticalThresholdHigh = 2000f;
                    break;
                    
                case LocalSensorType.AirFlow:
                    _alertThresholdLow = 0.2f;
                    _alertThresholdHigh = 2f;
                    _criticalThresholdLow = 0.1f;
                    _criticalThresholdHigh = 3f;
                    break;
            }
        }
        
        private void SetupSensorVisualization()
        {
            // Setup indicator colors and materials
            if (_sensorIndicator == null)
            {
                _sensorIndicator = GetComponent<Renderer>();
            }
            
            if (_sensorLight == null)
            {
                _sensorLight = GetComponent<Light>();
            }
            
            // Setup reading display
            if (_readingDisplay != null)
            {
                _readingDisplay.worldCamera = UnityCamera.main;
                _readingDisplay.gameObject.SetActive(false);
            }
            
            // Initialize particle system
            if (_alertParticles != null)
            {
                _alertParticles.Stop();
            }
        }
        
        #endregion
        
        #region Sensor Initialization Interface
        
        public void Initialize(LocalSensorType sensorType, MonoBehaviour parentRoom)
        {
            _sensorType = sensorType;
            _parentRoom = parentRoom;
            
            // Update sensor ID with room context
            if (parentRoom != null)
            {
                // Use GameObject name for ID to avoid dependency on a specific controller type
                string roomName = parentRoom.gameObject != null ? parentRoom.gameObject.name : "Room";
                _sensorId = $"{roomName}_{_sensorType}_{Guid.NewGuid().ToString("N")[..6]}";
            }
            
            SetDefaultThresholds();
        }
        
        // Deprecated automation overload removed (kept stub for compatibility)
        public void Initialize(int deprecatedAutomationType, MonoBehaviour parentRoom)
        {
            Initialize(_sensorType, parentRoom);
        }
        
        // Deprecated automation mapping removed
        
        #endregion
        
        #region Reading System
        
        private void StartReading()
        {
            _lastReadingTime = Time.time;
        }
        
        private void TakeReading()
        {
            if (_status != SensorStatus.Active && _status != SensorStatus.Warning)
                return;
            
            // Get environmental data from parent room
            EnvironmentalConditions conditions = GetEnvironmentalConditions();
            
            // Extract relevant reading based on sensor type
            _rawReading = ExtractReading(conditions);
            
            // Apply calibration and noise
            _currentReading = ProcessReading(_rawReading);
            
            // Store in history
            AddToHistory(_currentReading);
            
            // Check for alerts
            CheckForAlerts(_currentReading);
            
            // Buffer data for network transmission
            BufferDataForTransmission();
            
            // Notify listeners
            if (Mathf.Abs(_currentReading - _lastReading) > GetReadingThreshold())
            {
                OnReadingChanged?.Invoke(this, _currentReading);
                _lastReading = _currentReading;
            }
        }
        
        public void UpdateReading(EnvironmentalConditions conditions)
        {
            if (_status != SensorStatus.Active && _status != SensorStatus.Warning)
                return;
            
            _rawReading = ExtractReading(conditions);
            _currentReading = ProcessReading(_rawReading);
            
            AddToHistory(_currentReading);
            CheckForAlerts(_currentReading);
        }
        
        private EnvironmentalConditions GetEnvironmentalConditions()
        {
            if (_parentRoom != null)
            {
                // Attempt to fetch conditions via EnvironmentManager globally
                var envManager = GameManager.Instance?.GetManager<EnvironmentManager>();
                if (envManager != null)
                {
                    return envManager.GetEnvironmentForPosition(transform.position);
                }
            }
            
            // Fallback to simulated conditions
            return new EnvironmentalConditions
            {
                Temperature = 24f + UnityEngine.Random.Range(-2f, 2f),
                Humidity = 60f + UnityEngine.Random.Range(-10f, 10f),
                LightIntensity = 600f + UnityEngine.Random.Range(-100f, 100f),
                CO2Level = 1200f + UnityEngine.Random.Range(-200f, 200f),
                AirFlow = 0.8f + UnityEngine.Random.Range(-0.2f, 0.2f)
            };
        }
        
        private float ExtractReading(EnvironmentalConditions conditions)
        {
            return _sensorType switch
            {
                LocalSensorType.Temperature => conditions.Temperature,
                LocalSensorType.Humidity => conditions.Humidity,
                LocalSensorType.LightLevel => conditions.LightIntensity,
                LocalSensorType.CO2Level => conditions.CO2Level,
                LocalSensorType.AirFlow => conditions.AirFlow,
                _ => 0f
            };
        }
        
        private float ProcessReading(float rawValue)
        {
            // Apply calibration
            float calibratedValue = (rawValue + _calibrationOffset) * _calibrationMultiplier;
            
            // Add noise to simulate real sensor behavior
            float noise = UnityEngine.Random.Range(-_noiseLevel, _noiseLevel);
            calibratedValue += calibratedValue * noise;
            
            // Apply accuracy degradation
            float accuracyFactor = _accuracy + UnityEngine.Random.Range(-0.05f, 0.05f);
            calibratedValue *= accuracyFactor;
            
            // Clamp to reasonable values
            return ClampReading(calibratedValue);
        }
        
        private float ClampReading(float value)
        {
            return _sensorType switch
            {
                LocalSensorType.Temperature => Mathf.Clamp(value, -10f, 50f),
                LocalSensorType.Humidity => Mathf.Clamp(value, 0f, 100f),
                LocalSensorType.LightLevel => Mathf.Clamp(value, 0f, 2000f),
                LocalSensorType.CO2Level => Mathf.Clamp(value, 0f, 5000f),
                LocalSensorType.AirFlow => Mathf.Clamp(value, 0f, 5f),
                _ => value
            };
        }
        
        private void AddToHistory(float reading)
        {
            _readingHistory.Add(reading);
            
            // Maintain maximum history size
            while (_readingHistory.Count > MAX_HISTORY_SIZE)
            {
                _readingHistory.RemoveAt(0);
            }
        }
        
        private float GetReadingThreshold()
        {
            return _sensorType switch
            {
                LocalSensorType.Temperature => 0.1f,
                LocalSensorType.Humidity => 1f,
                LocalSensorType.LightLevel => 10f,
                LocalSensorType.CO2Level => 20f,
                LocalSensorType.AirFlow => 0.05f,
                _ => 0.01f
            };
        }
        
        #endregion
        
        #region Alert System
        
        private void CheckForAlerts(float reading)
        {
            if (!_enableAlerts) return;
            
            SensorAlert newAlert = null;
            
            // Check critical thresholds first
            if (reading <= _criticalThresholdLow || reading >= _criticalThresholdHigh)
            {
                newAlert = new SensorAlert
                {
                    SensorId = _sensorId,
                    SensorType = _sensorType,
                    AlertLevel = AlertLevel.Critical,
                    Reading = reading,
                    Message = $"{_sensorType} critical: {reading:F1} {GetUnitString()}",
                    Timestamp = DateTime.Now,
                    ThresholdLow = _criticalThresholdLow,
                    ThresholdHigh = _criticalThresholdHigh
                };
            }
            // Check warning thresholds
            else if (reading <= _alertThresholdLow || reading >= _alertThresholdHigh)
            {
                newAlert = new SensorAlert
                {
                    SensorId = _sensorId,
                    SensorType = _sensorType,
                    AlertLevel = AlertLevel.Warning,
                    Reading = reading,
                    Message = $"{_sensorType} warning: {reading:F1} {GetUnitString()}",
                    Timestamp = DateTime.Now,
                    ThresholdLow = _alertThresholdLow,
                    ThresholdHigh = _alertThresholdHigh
                };
            }
            
            // Process new alert
            if (newAlert != null)
            {
                ProcessAlert(newAlert);
            }
            else if (_currentAlert != null)
            {
                // Clear existing alert if reading is back to normal
                ClearAlert();
            }
        }
        
        private void ProcessAlert(SensorAlert alert)
        {
            // Check cooldown period
            if ((DateTime.Now - _lastAlertTime).TotalSeconds < _alertCooldown)
                return;
            
            // Update current alert
            _currentAlert = alert;
            _lastAlertTime = DateTime.Now;
            
            // Update sensor status
            SensorStatus newStatus = alert.AlertLevel == AlertLevel.Critical ? 
                SensorStatus.Critical : SensorStatus.Warning;
            SetStatus(newStatus);
            
            // Trigger alert
            OnAlertTriggered?.Invoke(this, alert);
            
            ChimeraLogger.LogWarning($"Sensor Alert [{_sensorId}]: {alert.Message}");
        }
        
        private void ClearAlert()
        {
            _currentAlert = null;
            SetStatus(SensorStatus.Active);
        }
        
        private string GetUnitString()
        {
            return _sensorType switch
            {
                LocalSensorType.Temperature => "°C",
                LocalSensorType.Humidity => "%",
                LocalSensorType.LightLevel => "PPFD",
                LocalSensorType.CO2Level => "ppm",
                LocalSensorType.AirFlow => "m/s",
                _ => ""
            };
        }
        
        #endregion
        
        #region Network/IoT Simulation
        
        private void BufferDataForTransmission()
        {
            var dataPacket = new SensorDataPacket
            {
                SensorId = _sensorId,
                SensorType = _sensorType,
                Reading = _currentReading,
                Status = _status,
                Timestamp = DateTime.Now,
                BatteryLevel = GetBatteryLevel(),
                SignalStrength = GetSignalStrength()
            };
            
            _dataBuffer.Enqueue(dataPacket);
            
            // Maintain buffer size
            while (_dataBuffer.Count > 100)
            {
                _dataBuffer.Dequeue();
            }
        }
        
        private void UpdateNetworkStatus()
        {
            // Simulate network connectivity
            float networkStability = UnityEngine.Random.Range(0.8f, 1f);
            _isOnline = networkStability > 0.85f;
            
            if (!_isOnline && _status == SensorStatus.Active)
            {
                SetStatus(SensorStatus.Offline);
            }
            else if (_isOnline && _status == SensorStatus.Offline)
            {
                SetStatus(SensorStatus.Active);
            }
            
            // Transmit buffered data if online
            if (_isOnline)
            {
                TransmitBufferedData();
            }
        }
        
        private void TransmitBufferedData()
        {
            // Simulate data transmission
            int packetsToTransmit = Mathf.Min(_dataBuffer.Count, 10);
            
            for (int i = 0; i < packetsToTransmit; i++)
            {
                if (_dataBuffer.Count > 0)
                {
                    var packet = _dataBuffer.Dequeue();
                    // Simulate transmission delay
                    StartCoroutine(SimulateDataTransmission(packet));
                }
            }
        }
        
        private System.Collections.IEnumerator SimulateDataTransmission(SensorDataPacket packet)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.5f));
            
            // Data transmitted successfully
            // In a real implementation, this would send to a cloud service or local server
            ChimeraLogger.Log($"Data transmitted: {packet.SensorId} - {packet.Reading:F2} {GetUnitString()}");
        }
        
        private float GetBatteryLevel()
        {
            // Simulate battery drain
            float batteryDrain = Time.time * 0.001f; // Very slow drain for demo
            return Mathf.Clamp01(1f - batteryDrain);
        }
        
        private float GetSignalStrength()
        {
            // Simulate signal strength variation
            return UnityEngine.Random.Range(0.7f, 1f);
        }
        
        #endregion
        
        #region Status Management
        
        private void SetStatus(SensorStatus newStatus)
        {
            if (_status != newStatus)
            {
                _status = newStatus;
                OnStatusChanged?.Invoke(this, newStatus);
            }
        }
        
        #endregion
        
        #region Visualization
        
        private void UpdateVisualization()
        {
            UpdateIndicatorColor();
            UpdateStatusLight();
            UpdateAlertParticles();
            UpdateReadingDisplay();
        }
        
        private void UpdateIndicatorColor()
        {
            if (_sensorIndicator == null) return;
            
            Color indicatorColor = _status switch
            {
                SensorStatus.Initializing => Color.yellow,
                SensorStatus.Active => Color.green,
                SensorStatus.Warning => Color.orange,
                SensorStatus.Critical => Color.red,
                SensorStatus.Offline => Color.gray,
                SensorStatus.Maintenance => Color.blue,
                _ => Color.white
            };
            
            _sensorIndicator.material.color = indicatorColor;
        }
        
        private void UpdateStatusLight()
        {
            if (_sensorLight == null) return;
            
            _sensorLight.enabled = _status != SensorStatus.Offline;
            
            if (_sensorLight.enabled)
            {
                Color lightColor = _status switch
                {
                    SensorStatus.Active => Color.green,
                    SensorStatus.Warning => Color.yellow,
                    SensorStatus.Critical => Color.red,
                    _ => Color.white
                };
                
                _sensorLight.color = lightColor;
                
                // Pulse effect for alerts
                if (_status == SensorStatus.Critical)
                {
                    _sensorLight.intensity = 1f + Mathf.Sin(Time.time * 4f) * 0.5f;
                }
                else
                {
                    _sensorLight.intensity = 0.5f;
                }
            }
        }
        
        private void UpdateAlertParticles()
        {
            if (_alertParticles == null) return;
            
            bool shouldPlay = _status == SensorStatus.Warning || _status == SensorStatus.Critical;
            
            if (shouldPlay && !_alertParticles.isPlaying)
            {
                var main = _alertParticles.main;
                main.startColor = _status == SensorStatus.Critical ? Color.red : Color.orange;
                _alertParticles.Play();
            }
            else if (!shouldPlay && _alertParticles.isPlaying)
            {
                _alertParticles.Stop();
            }
        }
        
        private void UpdateReadingDisplay()
        {
            if (_readingText == null) return;
            
            bool showDisplay = _status == SensorStatus.Warning || _status == SensorStatus.Critical || 
                              Input.GetKey(KeyCode.LeftShift);
            
            if (_readingDisplay != null)
            {
                _readingDisplay.gameObject.SetActive(showDisplay);
            }
            
            if (showDisplay)
            {
                string displayText = $"{_sensorType}\n{_currentReading:F1} {GetUnitString()}";
                
                if (_currentAlert != null)
                {
                    displayText += $"\n<color=red>ALERT</color>";
                }
                
                if (!_isOnline)
                {
                    displayText += $"\n<color=gray>OFFLINE</color>";
                }
                
                _readingText.text = displayText;
            }
        }
        
        #endregion
        
        #region Calibration
        
        public void CalibrateOffset(float knownValue)
        {
            _calibrationOffset = knownValue - _rawReading;
            _needsCalibration = false;
            
            ChimeraLogger.Log($"Sensor {_sensorId} calibrated with offset: {_calibrationOffset:F2}");
        }
        
        public void CalibrateMultiplier(float knownValue)
        {
            if (_rawReading != 0f)
            {
                _calibrationMultiplier = knownValue / _rawReading;
                _needsCalibration = false;
                
                ChimeraLogger.Log($"Sensor {_sensorId} calibrated with multiplier: {_calibrationMultiplier:F2}");
            }
        }
        
        public void ResetCalibration()
        {
            _calibrationOffset = 0f;
            _calibrationMultiplier = 1f;
            _needsCalibration = true;
        }
        
        #endregion
        
        #region Analytics
        
        public SensorAnalytics GetAnalytics()
        {
            if (_readingHistory.Count == 0)
                return new SensorAnalytics();
            
            return new SensorAnalytics
            {
                SensorId = _sensorId,
                SensorType = _sensorType,
                CurrentReading = _currentReading,
                AverageReading = _readingHistory.Average(),
                MinReading = _readingHistory.Min(),
                MaxReading = _readingHistory.Max(),
                StandardDeviation = CalculateStandardDeviation(),
                TotalReadings = _readingHistory.Count,
                Status = _status,
                IsOnline = _isOnline,
                LastUpdate = DateTime.Now
            };
        }
        
        private float CalculateStandardDeviation()
        {
            if (_readingHistory.Count < 2)
                return 0f;
            
            float average = _readingHistory.Average();
            float sumOfSquaredDeviations = _readingHistory.Sum(r => Mathf.Pow(r - average, 2));
            
            return Mathf.Sqrt(sumOfSquaredDeviations / _readingHistory.Count);
        }
        
        public List<float> GetRecentReadings(int count)
        {
            return _readingHistory.TakeLast(count).ToList();
        }
        
        #endregion
        
        #region Public Interface
        
        public void SetThresholds(float alertLow, float alertHigh, float criticalLow, float criticalHigh)
        {
            _alertThresholdLow = alertLow;
            _alertThresholdHigh = alertHigh;
            _criticalThresholdLow = criticalLow;
            _criticalThresholdHigh = criticalHigh;
        }
        
        public void SetAlertsEnabled(bool enabled)
        {
            _enableAlerts = enabled;
            
            if (!enabled)
            {
                ClearAlert();
            }
        }
        
        public void SetReadingInterval(float interval)
        {
            _readingInterval = Mathf.Max(0.1f, interval);
        }
        
        public void TriggerMaintenance()
        {
            SetStatus(SensorStatus.Maintenance);
            ClearAlert();
        }
        
        public void CompleteMaintenance()
        {
            SetStatus(SensorStatus.Active);
        }
        
        #endregion
        
        private void OnMouseEnter()
        {
            if (_readingDisplay != null)
            {
                _readingDisplay.gameObject.SetActive(true);
            }
        }
        
        private void OnMouseExit()
        {
            if (_readingDisplay != null && _status == SensorStatus.Active)
            {
                _readingDisplay.gameObject.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
            
            StopAllCoroutines();
        }
    }
    
    // Supporting data structures
    [System.Serializable]
    public enum LocalSensorType
    {
        Temperature,
        Humidity,
        LightLevel,
        CO2Level,
        AirFlow,
        SoilMoisture,
        pH,
        EC,
        Pressure
    }
    
    [System.Serializable]
    public enum SensorStatus
    {
        Initializing,
        Active,
        CalibrationRequired,
        LowBattery,
        Malfunction,
        Warning,
        Critical,
        Offline,
        Maintenance
    }
    
    [System.Serializable]
    public enum AlertLevel
    {
        Info,
        Warning,
        Critical
    }
    
    [System.Serializable]
    public class SensorAlert
    {
        public string SensorId;
        public LocalSensorType SensorType;
        public AlertLevel AlertLevel;
        // Extended fields used by SensorNetworkManager aggregated alerts
        public string ZoneId;
        public System.Collections.Generic.List<string> Issues;
        public SensorAlertSeverity Severity;
        public float Reading;
        public string Message;
        public DateTime Timestamp;
        public float ThresholdLow;
        public float ThresholdHigh;
    }

    // SensorAlertSeverity enum defined in SensorNetworkManager.cs to avoid duplicates
    
    [System.Serializable]
    public class SensorDataPacket
    {
        public string SensorId;
        public LocalSensorType SensorType;
        public float Reading;
        public SensorStatus Status;
        public DateTime Timestamp;
        public float BatteryLevel;
        public float SignalStrength;
    }
    
    [System.Serializable]
    public class SensorAnalytics
    {
        public string SensorId;
        public LocalSensorType SensorType;
        public float CurrentReading;
        public float AverageReading;
        public float MinReading;
        public float MaxReading;
        public float StandardDeviation;
        public int TotalReadings;
        public SensorStatus Status;
        public bool IsOnline;
        public DateTime LastUpdate;
    }
}