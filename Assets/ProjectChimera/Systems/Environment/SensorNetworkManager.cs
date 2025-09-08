using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Simulation;
// using ProjectChimera.Data.Automation; // Removed - namespace deleted during cleanup
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
// using SensorType = ProjectChimera.Data.Automation.SensorType; // Removed - namespace deleted during cleanup

namespace ProjectChimera.Systems.Environment
{
    // Local type definitions to replace deleted automation types
    public enum SensorType
    {
        Temperature, Humidity, TempHumidity, Light, CO2, pH, EC, Pressure, Flow
    }

    [System.Serializable]
    public class SensorReading
    {
        public string SensorId;
        public float Value;
        public System.DateTime Timestamp;
        public SensorReadingStatus Status;
        public string Unit;
    }

    public enum SensorReadingStatus
    {
        Valid, Invalid, OutOfRange, Calibrating, Error
    }

    /// <summary>
    /// Sensor alert severity levels
    /// </summary>
    public enum SensorAlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// PC013-6d: Specialized service for sensor network management
    /// Extracted from monolithic EnvironmentalManager.cs to handle environmental
    /// sensors, data collection, calibration, and sensor network monitoring.
    /// </summary>
    public class SensorNetworkManager : ChimeraManager, ITickable, IEnvironmentalService
    {
        [Header("Sensor Network Configuration")]
        [SerializeField] private float _sensorUpdateInterval = 5f; // 5 seconds
        [SerializeField] private int _maxSensorsPerZone = 10;
        [SerializeField] private bool _enableAutomaticCalibration = true;
        [SerializeField] private float _dataRetentionDays = 30f;

        [Header("Sensor Accuracy")]
        [SerializeField] private float _temperatureAccuracy = 0.1f; // ±0.1°C
        [SerializeField] private float _humidityAccuracy = 1f; // ±1%
        [SerializeField] private float _co2Accuracy = 50f; // ±50ppm
        [SerializeField] private float _lightAccuracy = 10f; // ±10 µmol/m²/s

        [Header("Alert Thresholds")]
        [SerializeField] private float _sensorFailureThreshold = 300f; // 5 minutes
        [SerializeField] private float _calibrationDriftThreshold = 10f; // 10% drift
        [SerializeField] private float _batteryLowThreshold = 20f; // 20% battery

        // Sensor tracking data
        private Dictionary<string, List<EnvironmentalSensorData>> _zoneSensors = new Dictionary<string, List<EnvironmentalSensorData>>();
        private Dictionary<string, SensorDataHistory> _sensorDataHistory = new Dictionary<string, SensorDataHistory>();
        private Dictionary<string, SensorNetworkStatus> _networkStatus = new Dictionary<string, SensorNetworkStatus>();
        private Dictionary<string, SensorAlert> _activeSensorAlerts = new Dictionary<string, SensorAlert>();

        // Timing
        private float _lastSensorUpdate;

        public bool IsInitialized { get; private set; }

        // Properties
        public int TotalSensors => _zoneSensors.Values.Sum(sensors => sensors.Count);
        public int ActiveAlerts => _activeSensorAlerts.Count;
        public bool AutomaticCalibration
        {
            get => _enableAutomaticCalibration;
            set => _enableAutomaticCalibration = value;
        }
        public float DataRetentionDays
        {
            get => _dataRetentionDays;
            set => _dataRetentionDays = Mathf.Clamp(value, 1f, 365f);
        }

        // Events
        public System.Action<string, EnvironmentalSensorReading> OnSensorDataReceived;
        public System.Action<string, SensorStatus> OnSensorStatusChanged;
        public System.Action<string, SensorAlert> OnSensorAlert;
        public System.Action<string> OnSensorCalibrated;
        public System.Action<string, float> OnBatteryLevelChanged;

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("[SensorNetworkManager] Initializing sensor network...");

            _lastSensorUpdate = Time.time;

            IsInitialized = true;
            ChimeraLogger.Log($"[SensorNetworkManager] Initialized. Auto-calibration: {_enableAutomaticCalibration}, Max sensors/zone: {_maxSensorsPerZone}");
        }

        public void Shutdown()
        {
            ChimeraLogger.Log("[SensorNetworkManager] Shutting down sensor network...");

            // Deactivate all sensors
            foreach (var sensors in _zoneSensors.Values)
            {
                foreach (var sensor in sensors)
                {
                    DeactivateSensor(sensor);
                }
            }

            _zoneSensors.Clear();
            _sensorDataHistory.Clear();
            _networkStatus.Clear();
            _activeSensorAlerts.Clear();

            IsInitialized = false;
        }

        public void Tick(float deltaTime)


        {
            if (!IsInitialized) return;

            if (Time.time - _lastSensorUpdate >= _sensorUpdateInterval)
            {
                UpdateSensorNetwork();
                _lastSensorUpdate = Time.time;
            }
        }

        /// <summary>
        /// Deploys sensors in a zone
        /// </summary>
        public bool DeploySensor(string zoneId, SensorType sensorType, Vector3 position)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                ChimeraLogger.LogWarning("[SensorNetworkManager] Cannot deploy sensor with null or empty zone ID");
                return false;
            }

            if (!_zoneSensors.ContainsKey(zoneId))
            {
                _zoneSensors[zoneId] = new List<EnvironmentalSensorData>();
                _networkStatus[zoneId] = new SensorNetworkStatus();
            }

            if (_zoneSensors[zoneId].Count >= _maxSensorsPerZone)
            {
                ChimeraLogger.LogWarning($"[SensorNetworkManager] Maximum sensor limit ({_maxSensorsPerZone}) reached for zone '{zoneId}'");
                return false;
            }

            var sensor = new EnvironmentalSensorData
            {
                SensorId = System.Guid.NewGuid().ToString(),
                Type = sensorType,
                Position = position,
                Status = SensorStatus.Active,
                BatteryLevel = 100f,
                LastCalibration = System.DateTime.Now,
                InstallationDate = System.DateTime.Now,
                Accuracy = GetSensorAccuracy(sensorType)
            };

            _zoneSensors[zoneId].Add(sensor);
            _sensorDataHistory[sensor.SensorId] = new SensorDataHistory();

            ChimeraLogger.Log($"[SensorNetworkManager] Deployed {sensorType} sensor in zone '{zoneId}' at {position}");
            return true;
        }

        /// <summary>
        /// Reads data from all sensors in a zone
        /// </summary>
        public EnvironmentalConditions ReadZoneConditions(string zoneId)
        {
            if (!_zoneSensors.TryGetValue(zoneId, out List<EnvironmentalSensorData> sensors))
            {
                ChimeraLogger.LogWarning($"[SensorNetworkManager] No sensors found in zone '{zoneId}'");
                return EnvironmentalConditions.CreateIndoorDefault();
            }

            var activeSensors = sensors.Where(s => s.Status == SensorStatus.Active).ToList();
            if (!activeSensors.Any())
            {
                ChimeraLogger.LogWarning($"[SensorNetworkManager] No active sensors in zone '{zoneId}'");
                return EnvironmentalConditions.CreateIndoorDefault();
            }

            // Aggregate readings from all sensors
            var conditions = AggregateZoneReadings(activeSensors);

            // Record readings for each sensor
            foreach (var sensor in activeSensors)
            {
                var reading = GenerateEnvironmentalSensorReading(sensor, conditions);
                RecordSensorReading(sensor.SensorId, reading);
                OnSensorDataReceived?.Invoke(sensor.SensorId, reading);
            }

            return conditions;
        }

        /// <summary>
        /// Calibrates a specific sensor
        /// </summary>
        public bool CalibrateSensor(string sensorId, EnvironmentalConditions referenceConditions)
        {
            var sensorNullable = FindSensorById(sensorId);
            if (sensorNullable == null)
            {
                ChimeraLogger.LogWarning($"[SensorNetworkManager] Sensor '{sensorId}' not found for calibration");
                return false;
            }

            var sensor = sensorNullable.Value;

            // Simulate calibration process
            var currentReading = GenerateEnvironmentalSensorReading(sensor, referenceConditions);
            var drift = CalculateCalibrationDrift(currentReading, referenceConditions);

            if (drift > _calibrationDriftThreshold)
            {
                ChimeraLogger.LogWarning($"[SensorNetworkManager] High calibration drift detected for sensor '{sensorId}': {drift:F1}%");
                sensor.Status = SensorStatus.CalibrationRequired;
            }
            else
            {
                sensor.LastCalibration = System.DateTime.Now;
                sensor.Status = SensorStatus.Active;
                ChimeraLogger.Log($"[SensorNetworkManager] Successfully calibrated sensor '{sensorId}'");
                OnSensorCalibrated?.Invoke(sensorId);
            }

            UpdateSensorInZone(sensor);
            return drift <= _calibrationDriftThreshold;
        }

        /// <summary>
        /// Updates all sensors in the network
        /// </summary>
        private void UpdateSensorNetwork()
        {
            foreach (var kvp in _zoneSensors)
            {
                string zoneId = kvp.Key;
                var sensors = kvp.Value;

                UpdateZoneSensors(zoneId, sensors);
                UpdateNetworkStatus(zoneId, sensors);
                CheckSensorAlerts(zoneId, sensors);
            }

            // Clean up old data
            CleanupOldSensorData();
        }

        /// <summary>
        /// Updates sensors in a specific zone
        /// </summary>
        private void UpdateZoneSensors(string zoneId, List<EnvironmentalSensorData> sensors)
        {
            for (int i = 0; i < sensors.Count; i++)
            {
                var sensor = sensors[i];

                // Update battery level (simulate drain)
                if (sensor.Status == SensorStatus.Active)
                {
                    sensor.BatteryLevel -= Time.deltaTime * 0.001f; // 0.1% per 100 seconds
                    sensor.BatteryLevel = Mathf.Max(sensor.BatteryLevel, 0f);

                    if (sensor.BatteryLevel <= _batteryLowThreshold)
                    {
                        sensor.Status = SensorStatus.LowBattery;
                        OnBatteryLevelChanged?.Invoke(sensor.SensorId, sensor.BatteryLevel);
                    }

                    if (sensor.BatteryLevel <= 0f)
                    {
                        sensor.Status = SensorStatus.Offline;
                    }
                }

                // Check for sensor failures
                var timeSinceLastReading = (System.DateTime.Now - sensor.LastReading).TotalSeconds;
                if (timeSinceLastReading > _sensorFailureThreshold && sensor.Status == SensorStatus.Active)
                {
                    sensor.Status = SensorStatus.Malfunction;
                    ChimeraLogger.LogWarning($"[SensorNetworkManager] Sensor '{sensor.SensorId}' appears to have malfunctioned");
                }

                // Check calibration schedule
                var daysSinceCalibration = (System.DateTime.Now - sensor.LastCalibration).TotalDays;
                if (daysSinceCalibration > 30 && _enableAutomaticCalibration) // Monthly calibration
                {
                    sensor.Status = SensorStatus.CalibrationRequired;
                }

                sensors[i] = sensor;
                OnSensorStatusChanged?.Invoke(sensor.SensorId, sensor.Status);
            }
        }

        /// <summary>
        /// Updates network status for a zone
        /// </summary>
        private void UpdateNetworkStatus(string zoneId, List<EnvironmentalSensorData> sensors)
        {
            if (!_networkStatus.TryGetValue(zoneId, out SensorNetworkStatus status))
                return;

            status.TotalSensors = sensors.Count;
            status.ActiveSensors = sensors.Count(s => s.Status == SensorStatus.Active);
            status.OfflineSensors = sensors.Count(s => s.Status == SensorStatus.Offline);
            status.MalfunctioningSensors = sensors.Count(s => s.Status == SensorStatus.Malfunction);
            status.LastUpdate = System.DateTime.Now;

            // Calculate network health
            status.NetworkHealth = status.TotalSensors > 0
                ? (float)status.ActiveSensors / status.TotalSensors
                : 0f;

            _networkStatus[zoneId] = status;
        }

        /// <summary>
        /// Checks for sensor alerts
        /// </summary>
        private void CheckSensorAlerts(string zoneId, List<EnvironmentalSensorData> sensors)
        {
            List<string> issues = new List<string>();

            // Check for offline sensors
            var offlineSensors = sensors.Where(s => s.Status == SensorStatus.Offline).Count();
            if (offlineSensors > 0)
            {
                issues.Add($"{offlineSensors} sensors offline");
            }

            // Check for malfunctioning sensors
            var malfunctioningSensors = sensors.Where(s => s.Status == SensorStatus.Malfunction).Count();
            if (malfunctioningSensors > 0)
            {
                issues.Add($"{malfunctioningSensors} sensors malfunctioning");
            }

            // Check for low battery sensors
            var lowBatterySensors = sensors.Where(s => s.Status == SensorStatus.LowBattery).Count();
            if (lowBatterySensors > 0)
            {
                issues.Add($"{lowBatterySensors} sensors with low battery");
            }

            // Check for calibration needed
            var calibrationNeeded = sensors.Where(s => s.Status == SensorStatus.CalibrationRequired).Count();
            if (calibrationNeeded > 0)
            {
                issues.Add($"{calibrationNeeded} sensors need calibration");
            }

            // Check network coverage
            var networkStatus = _networkStatus[zoneId];
            if (networkStatus.NetworkHealth < 0.5f)
            {
                issues.Add($"Poor network coverage: {networkStatus.NetworkHealth:P0}");
            }

            // Create or clear alerts
            string alertKey = $"sensor_{zoneId}";

            if (issues.Count > 0)
            {
                var alert = new SensorAlert
                {
                    ZoneId = zoneId,
                    Issues = issues,
                    Severity = DetermineSensorAlertSeverity(issues),
                    Timestamp = System.DateTime.Now
                };

                _activeSensorAlerts[alertKey] = alert;
                OnSensorAlert?.Invoke(zoneId, alert);
            }
            else if (_activeSensorAlerts.ContainsKey(alertKey))
            {
                _activeSensorAlerts.Remove(alertKey);
            }
        }

        /// <summary>
        /// Aggregates readings from multiple sensors
        /// </summary>
        private EnvironmentalConditions AggregateZoneReadings(List<EnvironmentalSensorData> sensors)
        {
            // Simulate reading from different sensor types
            var conditions = new EnvironmentalConditions();

            // Get temperature sensors
            var tempSensors = sensors.Where(s => s.Type == SensorType.Temperature || s.Type == SensorType.TempHumidity);
            if (tempSensors.Any())
            {
                conditions.Temperature = 22f + Random.Range(-2f, 3f); // Simulate reading
            }

            // Get humidity sensors
            var humiditySensors = sensors.Where(s => s.Type == SensorType.Humidity || s.Type == SensorType.TempHumidity);
            if (humiditySensors.Any())
            {
                conditions.Humidity = 55f + Random.Range(-10f, 10f); // Simulate reading
            }

            // Get CO2 sensors
            var co2Sensors = sensors.Where(s => s.Type == SensorType.CO2);
            if (co2Sensors.Any())
            {
                conditions.CO2Level = 800f + Random.Range(-100f, 300f); // Simulate reading
            }

            // Get light sensors
            var lightSensors = sensors.Where(s => s.Type == SensorType.Light);
            if (lightSensors.Any())
            {
                conditions.DailyLightIntegral = 25f + Random.Range(-5f, 10f); // Simulate reading
            }

            return conditions;
        }

        /// <summary>
        /// Generates a sensor reading with accuracy simulation
        /// </summary>
        private EnvironmentalSensorReading GenerateEnvironmentalSensorReading(EnvironmentalSensorData sensor, EnvironmentalConditions actualConditions)
        {
            var reading = new EnvironmentalSensorReading
            {
                SensorId = sensor.SensorId,
                Timestamp = System.DateTime.Now,
                IsValid = true,
                Status = SensorReadingStatus.Valid
            };

            // Add sensor-specific noise based on accuracy
            switch (sensor.Type)
            {
                case SensorType.Temperature:
                    reading.Temperature = actualConditions.Temperature + Random.Range(-sensor.Accuracy, sensor.Accuracy);
                    break;
                case SensorType.Humidity:
                    reading.Humidity = actualConditions.Humidity + Random.Range(-sensor.Accuracy, sensor.Accuracy);
                    break;
                case SensorType.TempHumidity:
                    reading.Temperature = actualConditions.Temperature + Random.Range(-sensor.Accuracy, sensor.Accuracy);
                    reading.Humidity = actualConditions.Humidity + Random.Range(-sensor.Accuracy, sensor.Accuracy);
                    break;
                case SensorType.CO2:
                    reading.CO2Level = actualConditions.CO2Level + Random.Range(-sensor.Accuracy, sensor.Accuracy);
                    break;
                case SensorType.Light:
                    reading.LightLevel = actualConditions.DailyLightIntegral + Random.Range(-sensor.Accuracy, sensor.Accuracy);
                    break;
            }

            return reading;
        }

        /// <summary>
        /// Records sensor reading in history
        /// </summary>
        private void RecordSensorReading(string sensorId, EnvironmentalSensorReading reading)
        {
            if (!_sensorDataHistory.TryGetValue(sensorId, out SensorDataHistory history))
            {
                history = new SensorDataHistory();
                _sensorDataHistory[sensorId] = history;
            }

            history.AddReading(reading);

            // Update sensor last reading time
            var sensorNullable = FindSensorById(sensorId);
            if (sensorNullable.HasValue)
            {
                var sensor = sensorNullable.Value;
                sensor.LastReading = reading.Timestamp;
                UpdateSensorInZone(sensor);
            }
        }

        /// <summary>
        /// Calculates calibration drift percentage
        /// </summary>
        private float CalculateCalibrationDrift(EnvironmentalSensorReading sensorReading, EnvironmentalConditions reference)
        {
            float totalDrift = 0f;
            int measurements = 0;

            if (sensorReading.Temperature.HasValue && reference.Temperature > 0)
            {
                totalDrift += Mathf.Abs(sensorReading.Temperature.Value - reference.Temperature) / reference.Temperature;
                measurements++;
            }

            if (sensorReading.Humidity.HasValue && reference.Humidity > 0)
            {
                totalDrift += Mathf.Abs(sensorReading.Humidity.Value - reference.Humidity) / reference.Humidity;
                measurements++;
            }

            if (sensorReading.CO2Level.HasValue && reference.CO2Level > 0)
            {
                totalDrift += Mathf.Abs(sensorReading.CO2Level.Value - reference.CO2Level) / reference.CO2Level;
                measurements++;
            }

            return measurements > 0 ? (totalDrift / measurements) * 100f : 0f;
        }

        /// <summary>
        /// Cleans up old sensor data beyond retention period
        /// </summary>
        private void CleanupOldSensorData()
        {
            var cutoffDate = System.DateTime.Now.AddDays(-_dataRetentionDays);

            foreach (var history in _sensorDataHistory.Values)
            {
                history.CleanupOldData(cutoffDate);
            }
        }

        /// <summary>
        /// Finds a sensor by ID across all zones
        /// </summary>
        private EnvironmentalSensorData? FindSensorById(string sensorId)
        {
            foreach (var sensors in _zoneSensors.Values)
            {
                var sensor = sensors.FirstOrDefault(s => s.SensorId == sensorId);
                if (sensor.SensorId != null)
                    return sensor;
            }
            return null;
        }

        /// <summary>
        /// Updates a sensor in its zone
        /// </summary>
        private void UpdateSensorInZone(EnvironmentalSensorData sensor)
        {
            foreach (var kvp in _zoneSensors)
            {
                var sensors = kvp.Value;
                for (int i = 0; i < sensors.Count; i++)
                {
                    if (sensors[i].SensorId == sensor.SensorId)
                    {
                        sensors[i] = sensor;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Deactivates a sensor
        /// </summary>
        private void DeactivateSensor(EnvironmentalSensorData sensor)
        {
            sensor.Status = SensorStatus.Offline;
            UpdateSensorInZone(sensor);
        }

        /// <summary>
        /// Gets sensor accuracy based on type
        /// </summary>
        private float GetSensorAccuracy(SensorType sensorType)
        {
            switch (sensorType)
            {
                case SensorType.Temperature:
                case SensorType.TempHumidity:
                    return _temperatureAccuracy;
                case SensorType.Humidity:
                    return _humidityAccuracy;
                case SensorType.CO2:
                    return _co2Accuracy;
                case SensorType.Light:
                    return _lightAccuracy;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Determines sensor alert severity
        /// </summary>
        private SensorAlertSeverity DetermineSensorAlertSeverity(List<string> issues)
        {
            if (issues.Any(i => i.Contains("offline") || i.Contains("malfunctioning")))
                return SensorAlertSeverity.Critical;
            if (issues.Any(i => i.Contains("coverage")))
                return SensorAlertSeverity.High;
            if (issues.Count >= 2)
                return SensorAlertSeverity.Medium;
            return SensorAlertSeverity.Low;
        }

        /// <summary>
        /// Gets network status for a zone
        /// </summary>
        public SensorNetworkStatus GetNetworkStatus(string zoneId)
        {
            return _networkStatus.TryGetValue(zoneId, out SensorNetworkStatus status)
                ? status
                : new SensorNetworkStatus();
        }

        /// <summary>
        /// Gets sensor data history
        /// </summary>
        public SensorDataHistory GetSensorHistory(string sensorId)
        {
            return _sensorDataHistory.TryGetValue(sensorId, out SensorDataHistory history)
                ? history
                : new SensorDataHistory();
        }

        /// <summary>
        /// Gets all sensors in a zone
        /// </summary>
        public IReadOnlyList<EnvironmentalSensorData> GetZoneSensors(string zoneId)
        {
            return _zoneSensors.TryGetValue(zoneId, out List<EnvironmentalSensorData> sensors)
                ? sensors.AsReadOnly()
                : new List<EnvironmentalSensorData>().AsReadOnly();
        }

        /// <summary>
        /// Gets all active sensor alerts
        /// </summary>
        public IReadOnlyDictionary<string, SensorAlert> GetActiveAlerts()
        {
            return _activeSensorAlerts;
        }

        #region ChimeraManager Implementation

        protected override void OnManagerInitialize()
        {
            // Manager-specific initialization logic (already implemented in Initialize method)
        }

        protected override void OnManagerShutdown()
        {
            // Manager-specific shutdown logic (already implemented in Shutdown method)
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected override void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            base.OnDestroy();
        }

        #endregion

        #region ITickable Implementation

        // ITickable implementation
        int ITickable.Priority => 0;
        bool ITickable.Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }

        #endregion
    }

    /// <summary>
    /// Environmental sensor data
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalSensorData
    {
        public string SensorId;
        public SensorType Type;
        public Vector3 Position;
        public SensorStatus Status;
        public float BatteryLevel;
        public float Accuracy;
        public System.DateTime LastReading;
        public System.DateTime LastCalibration;
        public System.DateTime InstallationDate;
        public float SignalStrength;
        public string SensorModel;
        public string FirmwareVersion;

        // Additional sensor properties
        public bool IsActive => Status == SensorStatus.Active;
        public bool RequiresMaintenance => Status == SensorStatus.Maintenance;
    }

    /// <summary>
    /// Environmental sensor reading data - compatible with environmental system needs
    /// Note: Different from ProjectChimera.Data.Automation.SensorReading which has a single Value property
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalSensorReading
    {
        public string SensorId;
        public System.DateTime Timestamp;
        public float? Temperature;
        public float? Humidity;
        public float? CO2Level;
        public float? LightLevel;
        public float? AirflowRate;
        public bool IsValid;
        public SensorReadingStatus Status;
    }

    /// <summary>
    /// Sensor network status for a zone
    /// </summary>
    [System.Serializable]
    public struct SensorNetworkStatus
    {
        public int TotalSensors;
        public int ActiveSensors;
        public int OfflineSensors;
        public int MalfunctioningSensors;
        public float NetworkHealth; // 0-1
        public System.DateTime LastUpdate;
    }

    // SensorAlert is defined in EnvironmentalSensor.cs

    /// <summary>
    /// Historical sensor data tracking
    /// </summary>
    [System.Serializable]
    public class SensorDataHistory
    {
        [SerializeField] private List<EnvironmentalSensorReading> _readings = new List<EnvironmentalSensorReading>();
        private const int MAX_READINGS = 1000;

        public IReadOnlyList<EnvironmentalSensorReading> Readings => _readings.AsReadOnly();

        public void AddReading(EnvironmentalSensorReading reading)
        {
            _readings.Add(reading);

            // Maintain maximum readings limit
            if (_readings.Count > MAX_READINGS)
            {
                _readings.RemoveAt(0);
            }
        }

        public void CleanupOldData(System.DateTime cutoffDate)
        {
            _readings.RemoveAll(r => r.Timestamp < cutoffDate);
        }

        public float GetAverageTemperature(int lastHours = 24)
        {
            var cutoff = System.DateTime.Now.AddHours(-lastHours);
            var recentReadings = _readings.Where(r => r.Timestamp >= cutoff && r.Temperature.HasValue);
            return recentReadings.Any() ? recentReadings.Average(r => r.Temperature.Value) : 0f;
        }

        public float GetAverageHumidity(int lastHours = 24)
        {
            var cutoff = System.DateTime.Now.AddHours(-lastHours);
            var recentReadings = _readings.Where(r => r.Timestamp >= cutoff && r.Humidity.HasValue);
            return recentReadings.Any() ? recentReadings.Average(r => r.Humidity.Value) : 0f;
        }
    }
}
