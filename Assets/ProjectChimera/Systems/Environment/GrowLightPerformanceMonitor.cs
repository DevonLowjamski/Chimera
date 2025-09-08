using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Handles performance monitoring, metrics collection, and analytics.
    /// Extracted from AdvancedGrowLightSystem for modular architecture.
    /// Tracks efficiency, energy consumption, and system performance metrics.
    /// </summary>
    public class GrowLightPerformanceMonitor : MonoBehaviour, ITickable
    {
        [Header("Performance Monitor Configuration")]
        [SerializeField] private bool _enablePerformanceLogging = true;
        [SerializeField] private float _metricsCollectionInterval = 30f; // Collect every 30 seconds
        [SerializeField] private int _maxPerformanceHistory = 1000;
        [SerializeField] private bool _enableRealTimeMetrics = true;

        // Dependencies
        private GrowLightController _lightController;
        private GrowLightSpectrumController _spectrumController;
        private GrowLightThermalManager _thermalManager;
        private GrowLightPlantOptimizer _plantOptimizer;

        // Performance data
        private Queue<PerformanceSnapshot> _performanceHistory = new Queue<PerformanceSnapshot>();
        private float _lastMetricsCollection = 0f;
        private PerformanceMetrics _currentMetrics = new PerformanceMetrics();
        private PerformanceMetrics _sessionTotals = new PerformanceMetrics();

        // Energy tracking
        private float _totalEnergyConsumed = 0f; // kWh
        private float _sessionStartTime;
        private Dictionary<string, float> _energyByTimeOfDay = new Dictionary<string, float>();

        // Efficiency tracking
        private float _totalPhotonOutput = 0f; // Total photons delivered (μmol)
        private float _averageEfficiency = 0f;
        private Queue<float> _efficiencyHistory = new Queue<float>();

        // Events
        public System.Action<PerformanceMetrics> OnMetricsUpdated;
        public System.Action<PerformanceSnapshot> OnSnapshotTaken;
        public System.Action<PerformanceAlert> OnPerformanceAlert;
        public System.Action<EfficiencyReport> OnEfficiencyReportGenerated;

        // Properties
        public PerformanceMetrics CurrentMetrics => _currentMetrics;
        public PerformanceMetrics SessionTotals => _sessionTotals;
        public float TotalEnergyConsumed => _totalEnergyConsumed;
        public float AverageEfficiency => _averageEfficiency;
        public int PerformanceHistoryCount => _performanceHistory.Count;

        /// <summary>
        /// Initialize performance monitor with dependencies
        /// </summary>
        public void Initialize(GrowLightController lightController, GrowLightSpectrumController spectrumController,
            GrowLightThermalManager thermalManager, GrowLightPlantOptimizer plantOptimizer)
        {
            _lightController = lightController;
            _spectrumController = spectrumController;
            _thermalManager = thermalManager;
            _plantOptimizer = plantOptimizer;

            _sessionStartTime = Time.time;
            InitializePerformanceTracking();

            // Subscribe to component events
            SubscribeToEvents();

            LogDebug("Grow light performance monitor initialized");
        }

        public void Tick(float deltaTime)
        {
            if (_enableRealTimeMetrics)
            {
                UpdateRealTimeMetrics();
            }

            _lastMetricsCollection += Time.deltaTime;

            if (_lastMetricsCollection >= _metricsCollectionInterval)
            {
                CollectPerformanceSnapshot();
                _lastMetricsCollection = 0f;
            }
        }

        #region Performance Tracking

        /// <summary>
        /// Initialize performance tracking systems
        /// </summary>
        private void InitializePerformanceTracking()
        {
            _currentMetrics = new PerformanceMetrics();
            _sessionTotals = new PerformanceMetrics();

            // Initialize energy tracking by hour
            for (int hour = 0; hour < 24; hour++)
            {
                _energyByTimeOfDay[hour.ToString("D2")] = 0f;
            }
        }

        /// <summary>
        /// Update real-time performance metrics
        /// </summary>
        private void UpdateRealTimeMetrics()
        {
            if (_lightController == null) return;

            // Update current metrics
            _currentMetrics.CurrentIntensity = _lightController.CurrentIntensity;
            _currentMetrics.PowerConsumption = _lightController.PowerConsumption;
            _currentMetrics.Efficiency = _lightController.CalculateCurrentEfficiency();
            _currentMetrics.LightOnTime = _lightController.IsOn ? Time.time - _sessionStartTime : 0f;

            if (_thermalManager != null)
            {
                _currentMetrics.Temperature = _thermalManager.CurrentTemperature;
                _currentMetrics.ThermalThrottling = _thermalManager.ThermalThrottlingActive;
            }

            if (_plantOptimizer != null)
            {
                _currentMetrics.OptimizationScore = _plantOptimizer.CurrentOptimizationScore;
                _currentMetrics.MonitoredPlants = _plantOptimizer.MonitoredPlantsCount;
            }

            // Update energy consumption
            float energyDelta = (_currentMetrics.PowerConsumption / 1000f) * Time.deltaTime / 3600f; // Convert to kWh
            _totalEnergyConsumed += energyDelta;
            _currentMetrics.EnergyConsumed = _totalEnergyConsumed;

            // Track energy by time of day
            string currentHour = System.DateTime.Now.Hour.ToString("D2");
            if (_energyByTimeOfDay.ContainsKey(currentHour))
            {
                _energyByTimeOfDay[currentHour] += energyDelta;
            }

            // Update efficiency tracking
            if (_currentMetrics.Efficiency > 0f)
            {
                _efficiencyHistory.Enqueue(_currentMetrics.Efficiency);
                if (_efficiencyHistory.Count > 100) // Keep last 100 readings
                {
                    _efficiencyHistory.Dequeue();
                }

                _averageEfficiency = _efficiencyHistory.Average();
            }

            // Update photon output
            float photonDelta = _currentMetrics.CurrentIntensity * Time.deltaTime; // μmol/s * s = μmol
            _totalPhotonOutput += photonDelta;
            _currentMetrics.TotalPhotonOutput = _totalPhotonOutput;

            // Check for performance alerts
            CheckPerformanceAlerts();
        }

        /// <summary>
        /// Collect a performance snapshot
        /// </summary>
        private void CollectPerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = System.DateTime.Now,
                Intensity = _currentMetrics.CurrentIntensity,
                PowerConsumption = _currentMetrics.PowerConsumption,
                Efficiency = _currentMetrics.Efficiency,
                Temperature = _currentMetrics.Temperature,
                EnergyConsumed = _currentMetrics.EnergyConsumed,
                PhotonOutput = _currentMetrics.TotalPhotonOutput,
                OptimizationScore = _currentMetrics.OptimizationScore,
                ThermalThrottling = _currentMetrics.ThermalThrottling,
                SpectrumEffectiveness = CalculateSpectrumEffectiveness()
            };

            _performanceHistory.Enqueue(snapshot);
            OnSnapshotTaken?.Invoke(snapshot);

            // Keep history manageable
            while (_performanceHistory.Count > _maxPerformanceHistory)
            {
                _performanceHistory.Dequeue();
            }

            // Update session totals
            UpdateSessionTotals(snapshot);

            OnMetricsUpdated?.Invoke(_currentMetrics);
        }

        /// <summary>
        /// Update session total metrics
        /// </summary>
        private void UpdateSessionTotals(PerformanceSnapshot snapshot)
        {
            _sessionTotals.EnergyConsumed = snapshot.EnergyConsumed;
            _sessionTotals.TotalPhotonOutput = snapshot.PhotonOutput;
            _sessionTotals.LightOnTime = _lightController.IsOn ? Time.time - _sessionStartTime : _sessionTotals.LightOnTime;

            if (_performanceHistory.Count > 0)
            {
                _sessionTotals.AverageIntensity = _performanceHistory.Average(s => s.Intensity);
                _sessionTotals.AveragePowerConsumption = _performanceHistory.Average(s => s.PowerConsumption);
                _sessionTotals.AverageEfficiency = _performanceHistory.Average(s => s.Efficiency);
                _sessionTotals.AverageTemperature = _performanceHistory.Average(s => s.Temperature);
                _sessionTotals.AverageOptimizationScore = _performanceHistory.Average(s => s.OptimizationScore);
            }
        }

        #endregion

        #region Performance Analysis

        /// <summary>
        /// Calculate spectrum effectiveness for current configuration
        /// </summary>
        private float CalculateSpectrumEffectiveness()
        {
            if (_spectrumController == null) return 0.5f;

            var spectrum = _spectrumController.CurrentSpectrum;

            // Calculate overall PAR effectiveness
            float parEffectiveness = (spectrum.Red * 0.85f) + (spectrum.Green * 0.75f) + (spectrum.Blue * 0.85f);

            // Factor in far-red and UV contributions
            float extendedEffectiveness = (spectrum.FarRed * 0.15f) + (spectrum.UV * 0.1f);

            return Mathf.Clamp01(parEffectiveness + extendedEffectiveness);
        }

        /// <summary>
        /// Generate efficiency report
        /// </summary>
        public EfficiencyReport GenerateEfficiencyReport(int hoursBack = 24)
        {
            var cutoffTime = System.DateTime.Now.AddHours(-hoursBack);
            var relevantSnapshots = _performanceHistory
                .Where(s => s.Timestamp >= cutoffTime)
                .ToList();

            if (relevantSnapshots.Count == 0)
                return new EfficiencyReport();

            var report = new EfficiencyReport
            {
                ReportPeriodHours = hoursBack,
                TotalEnergyConsumed = relevantSnapshots.Last().EnergyConsumed - relevantSnapshots.First().EnergyConsumed,
                TotalPhotonOutput = relevantSnapshots.Last().PhotonOutput - relevantSnapshots.First().PhotonOutput,
                AverageEfficiency = relevantSnapshots.Average(s => s.Efficiency),
                MinEfficiency = relevantSnapshots.Min(s => s.Efficiency),
                MaxEfficiency = relevantSnapshots.Max(s => s.Efficiency),
                AverageTemperature = relevantSnapshots.Average(s => s.Temperature),
                ThermalThrottlingTime = relevantSnapshots.Count(s => s.ThermalThrottling) * (_metricsCollectionInterval / 3600f),
                OptimalOperatingTime = relevantSnapshots.Count(s => s.OptimizationScore > 0.8f) * (_metricsCollectionInterval / 3600f),
                EfficiencyTrend = CalculateEfficiencyTrend(relevantSnapshots)
            };

            report.PhotonPerWattHour = report.TotalEnergyConsumed > 0 ? report.TotalPhotonOutput / (report.TotalEnergyConsumed * 1000f) : 0f;

            OnEfficiencyReportGenerated?.Invoke(report);
            return report;
        }

        /// <summary>
        /// Calculate efficiency trend
        /// </summary>
        private float CalculateEfficiencyTrend(List<PerformanceSnapshot> snapshots)
        {
            if (snapshots.Count < 2) return 0f;

            // Simple linear trend calculation
            float firstHalfAvg = snapshots.Take(snapshots.Count / 2).Average(s => s.Efficiency);
            float secondHalfAvg = snapshots.Skip(snapshots.Count / 2).Average(s => s.Efficiency);

            return secondHalfAvg - firstHalfAvg;
        }

        /// <summary>
        /// Get energy consumption breakdown by hour
        /// </summary>
        public Dictionary<string, float> GetEnergyBreakdownByHour()
        {
            return new Dictionary<string, float>(_energyByTimeOfDay);
        }

        /// <summary>
        /// Get performance summary for a specific time period
        /// </summary>
        public PerformanceSummary GetPerformanceSummary(int hoursBack = 24)
        {
            var cutoffTime = System.DateTime.Now.AddHours(-hoursBack);
            var relevantSnapshots = _performanceHistory
                .Where(s => s.Timestamp >= cutoffTime)
                .ToList();

            if (relevantSnapshots.Count == 0)
                return new PerformanceSummary();

            return new PerformanceSummary
            {
                PeriodHours = hoursBack,
                SnapshotCount = relevantSnapshots.Count,
                AverageIntensity = relevantSnapshots.Average(s => s.Intensity),
                AveragePowerConsumption = relevantSnapshots.Average(s => s.PowerConsumption),
                AverageEfficiency = relevantSnapshots.Average(s => s.Efficiency),
                AverageTemperature = relevantSnapshots.Average(s => s.Temperature),
                TotalEnergyConsumed = relevantSnapshots.Last().EnergyConsumed - relevantSnapshots.First().EnergyConsumed,
                TotalPhotonOutput = relevantSnapshots.Last().PhotonOutput - relevantSnapshots.First().PhotonOutput,
                ThermalThrottlingEvents = relevantSnapshots.Count(s => s.ThermalThrottling),
                OptimizationScoreAverage = relevantSnapshots.Average(s => s.OptimizationScore)
            };
        }

        #endregion

        #region Performance Alerts

        /// <summary>
        /// Check for performance alerts and issues
        /// </summary>
        private void CheckPerformanceAlerts()
        {
            // Efficiency degradation alert
            if (_averageEfficiency > 0f && _currentMetrics.Efficiency < _averageEfficiency * 0.8f)
            {
                CreatePerformanceAlert(PerformanceAlertType.EfficiencyDegradation,
                    $"Efficiency dropped to {_currentMetrics.Efficiency:F2} μmol/J (avg: {_averageEfficiency:F2})");
            }

            // High energy consumption alert
            if (_currentMetrics.PowerConsumption > 800f) // Above 800W
            {
                CreatePerformanceAlert(PerformanceAlertType.HighPowerConsumption,
                    $"High power consumption: {_currentMetrics.PowerConsumption:F0}W");
            }

            // Poor optimization alert
            if (_currentMetrics.OptimizationScore < 0.5f && _plantOptimizer.MonitoredPlantsCount > 0)
            {
                CreatePerformanceAlert(PerformanceAlertType.PoorOptimization,
                    $"Low optimization score: {_currentMetrics.OptimizationScore:F2}");
            }

            // Extended thermal throttling alert
            if (_currentMetrics.ThermalThrottling)
            {
                CreatePerformanceAlert(PerformanceAlertType.ThermalThrottling,
                    $"Thermal throttling active - temperature: {_currentMetrics.Temperature:F1}°C");
            }
        }

        /// <summary>
        /// Create and dispatch performance alert
        /// </summary>
        private void CreatePerformanceAlert(PerformanceAlertType alertType, string message)
        {
            var alert = new PerformanceAlert
            {
                AlertType = alertType,
                Message = message,
                Timestamp = System.DateTime.Now,
                CurrentMetrics = _currentMetrics
            };

            OnPerformanceAlert?.Invoke(alert);
            LogDebug($"Performance alert: {alertType} - {message}");
        }

        #endregion

        #region Event Management

        /// <summary>
        /// Subscribe to component events
        /// </summary>
        private void SubscribeToEvents()
        {
            // Light controller events
            if (_lightController != null)
            {
                _lightController.OnLightStateChanged += OnLightStateChanged;
                _lightController.OnIntensityChanged += OnIntensityChanged;
            }

            // Thermal manager events
            if (_thermalManager != null)
            {
                _thermalManager.OnThermalThrottlingChanged += OnThermalThrottlingChanged;
                _thermalManager.OnEmergencyShutdown += OnEmergencyShutdown;
            }
        }

        /// <summary>
        /// Handle light state changes
        /// </summary>
        private void OnLightStateChanged(bool isOn)
        {
            if (isOn)
            {
                LogDebug("Light turned on - performance monitoring active");
            }
            else
            {
                LogDebug("Light turned off - logging final metrics");
                CollectPerformanceSnapshot(); // Capture final state
            }
        }

        /// <summary>
        /// Handle intensity changes
        /// </summary>
        private void OnIntensityChanged(float intensity)
        {
            // Performance metrics will be updated on next cycle
        }

        /// <summary>
        /// Handle thermal throttling changes
        /// </summary>
        private void OnThermalThrottlingChanged(bool throttling)
        {
            CreatePerformanceAlert(
                throttling ? PerformanceAlertType.ThermalThrottling : PerformanceAlertType.ThermalThrottlingResolved,
                $"Thermal throttling {(throttling ? "activated" : "resolved")}");
        }

        /// <summary>
        /// Handle emergency shutdown
        /// </summary>
        private void OnEmergencyShutdown()
        {
            CreatePerformanceAlert(PerformanceAlertType.EmergencyShutdown,
                "Emergency thermal shutdown triggered");

            // Collect final snapshot before shutdown
            CollectPerformanceSnapshot();
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Reset performance metrics and history
        /// </summary>
        public void ResetMetrics()
        {
            _performanceHistory.Clear();
            _efficiencyHistory.Clear();
            _totalEnergyConsumed = 0f;
            _totalPhotonOutput = 0f;
            _sessionStartTime = Time.time;

            foreach (var key in _energyByTimeOfDay.Keys.ToList())
            {
                _energyByTimeOfDay[key] = 0f;
            }

            LogDebug("Performance metrics reset");
        }

        /// <summary>
        /// Export performance data to CSV format
        /// </summary>
        public string ExportPerformanceData(int hoursBack = 24)
        {
            var cutoffTime = System.DateTime.Now.AddHours(-hoursBack);
            var relevantSnapshots = _performanceHistory
                .Where(s => s.Timestamp >= cutoffTime)
                .OrderBy(s => s.Timestamp)
                .ToList();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Timestamp,Intensity,PowerConsumption,Efficiency,Temperature,EnergyConsumed,PhotonOutput,OptimizationScore");

            foreach (var snapshot in relevantSnapshots)
            {
                csv.AppendLine($"{snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                              $"{snapshot.Intensity:F1}," +
                              $"{snapshot.PowerConsumption:F1}," +
                              $"{snapshot.Efficiency:F2}," +
                              $"{snapshot.Temperature:F1}," +
                              $"{snapshot.EnergyConsumed:F3}," +
                              $"{snapshot.PhotonOutput:F0}," +
                              $"{snapshot.OptimizationScore:F2}");
            }

            return csv.ToString();
        }

        #endregion

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            // Unsubscribe from events
            if (_lightController != null)
            {
                _lightController.OnLightStateChanged -= OnLightStateChanged;
                _lightController.OnIntensityChanged -= OnIntensityChanged;
            }

            if (_thermalManager != null)
            {
                _thermalManager.OnThermalThrottlingChanged -= OnThermalThrottlingChanged;
                _thermalManager.OnEmergencyShutdown -= OnEmergencyShutdown;
            }
        }

        private void LogDebug(string message)
        {
            if (_enablePerformanceLogging)
                ChimeraLogger.Log($"[GrowLightPerformanceMonitor] {message}");
        }

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

    protected virtual void Start()
    {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
    }

}

    /// <summary>
    /// Real-time performance metrics
    /// </summary>
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float CurrentIntensity = 0f;
        public float PowerConsumption = 0f;
        public float Efficiency = 0f;
        public float Temperature = 25f;
        public float EnergyConsumed = 0f;
        public float TotalPhotonOutput = 0f;
        public float OptimizationScore = 0f;
        public float LightOnTime = 0f;
        public bool ThermalThrottling = false;
        public int MonitoredPlants = 0;

        // Session averages
        public float AverageIntensity = 0f;
        public float AveragePowerConsumption = 0f;
        public float AverageEfficiency = 0f;
        public float AverageTemperature = 25f;
        public float AverageOptimizationScore = 0f;
    }

    /// <summary>
    /// Performance snapshot at a specific time
    /// </summary>
    [System.Serializable]
    public class PerformanceSnapshot
    {
        public System.DateTime Timestamp;
        public float Intensity;
        public float PowerConsumption;
        public float Efficiency;
        public float Temperature;
        public float EnergyConsumed;
        public float PhotonOutput;
        public float OptimizationScore;
        public bool ThermalThrottling;
        public float SpectrumEffectiveness;
    }

    /// <summary>
    /// Efficiency analysis report
    /// </summary>
    [System.Serializable]
    public class EfficiencyReport
    {
        public int ReportPeriodHours = 0;
        public float TotalEnergyConsumed = 0f;
        public float TotalPhotonOutput = 0f;
        public float PhotonPerWattHour = 0f;
        public float AverageEfficiency = 0f;
        public float MinEfficiency = 0f;
        public float MaxEfficiency = 0f;
        public float AverageTemperature = 25f;
        public float ThermalThrottlingTime = 0f;
        public float OptimalOperatingTime = 0f;
        public float EfficiencyTrend = 0f;
    }

    /// <summary>
    /// Performance summary for a time period
    /// </summary>
    [System.Serializable]
    public class PerformanceSummary
    {
        public int PeriodHours = 0;
        public int SnapshotCount = 0;
        public float AverageIntensity = 0f;
        public float AveragePowerConsumption = 0f;
        public float AverageEfficiency = 0f;
        public float AverageTemperature = 25f;
        public float TotalEnergyConsumed = 0f;
        public float TotalPhotonOutput = 0f;
        public int ThermalThrottlingEvents = 0;
        public float OptimizationScoreAverage = 0f;
    }

    /// <summary>
    /// Performance alert information
    /// </summary>
    [System.Serializable]
    public class PerformanceAlert
    {
        public PerformanceAlertType AlertType;
        public string Message;
        public System.DateTime Timestamp;
        public PerformanceMetrics CurrentMetrics;
    }

    public enum PerformanceAlertType
    {
        EfficiencyDegradation,
        HighPowerConsumption,
        PoorOptimization,
        ThermalThrottling,
        ThermalThrottlingResolved,
        EmergencyShutdown
    }
}
