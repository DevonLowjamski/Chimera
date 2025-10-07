using UnityEngine;
using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Modular Equipment Degradation Manager - Main orchestrator
    /// Coordinates equipment monitoring, degradation, malfunctions, and maintenance
    /// </summary>
    public class EquipmentDegradationManager : ProjectChimera.Core.ChimeraManager, ProjectChimera.Core.Updates.ITickable
    {
        [Header("Core Configuration")]
        public bool EnableEquipmentDegradation = true;
        public bool EnableRandomMalfunctions = true;
        public bool EnableWearBasedFailures = true;
        public bool EnableMaintenanceRewards = true;

        [Header("System Settings")]
        [Range(0f, 1f)] public float BaseDegradationRate = 0.02f;
        [Range(0f, 5f)] public float EnvironmentalStressMultiplier = 2f;
        [Range(1, 50)] public int MaxConcurrentMalfunctions = 10;
        public float MaintenanceEfficiencyBonus = 0.25f;

        // Modular system components
        private EquipmentRegistry _equipmentRegistry;
        private DegradationSimulation _degradationSimulator;
        private MalfunctionDetectionSystem _malfunctionDetector;
        private MaintenanceScheduler _maintenanceScheduler;
        private EquipmentHealthAssessment _healthAssessor;

        // System state
        private List<BasicEquipmentData> _monitoredEquipment = new List<BasicEquipmentData>();
        private List<EquipmentMalfunction> _activeMalfunctions = new List<EquipmentMalfunction>();
        private List<MaintenanceRecord> _maintenanceHistory = new List<MaintenanceRecord>();
        private Dictionary<string, EquipmentReliabilityProfile> _reliabilityProfiles = new Dictionary<string, EquipmentReliabilityProfile>();

        // Events
        public System.Action<BasicEquipmentData> OnEquipmentAdded;
        public System.Action<BasicEquipmentData> OnEquipmentRemoved;
        public System.Action<EquipmentMalfunction> OnMalfunctionDetected;
        public System.Action<EquipmentMalfunction> OnMalfunctionResolved;
        public System.Action<MaintenanceRecord> OnMaintenanceCompleted;

        #region Unity Methods

        protected override void Awake()
        {
            base.Awake();
            InitializeSystems();
        }

        public int TickPriority => 100;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (!EnableEquipmentDegradation) return;

            // Update equipment degradation
            _degradationSimulator?.UpdateDegradation(deltaTime);

            // Check for new malfunctions
            CheckForMalfunctions();

            // Update maintenance schedules
            _maintenanceScheduler?.UpdateSchedules();
    }

        private void OnEnable()
        {
            ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize all modular systems
        /// </summary>
        private void InitializeSystems()
        {
            // Create modular components
            _equipmentRegistry = new EquipmentRegistry();
            _degradationSimulator = new DegradationSimulation(BaseDegradationRate, EnvironmentalStressMultiplier);
            _malfunctionDetector = new MalfunctionDetectionSystem();
            _maintenanceScheduler = new MaintenanceScheduler();
            _healthAssessor = new EquipmentHealthAssessment();

            // Initialize malfunction detection with default rules
            _malfunctionDetector.InitializeDefaultRules();

            // Set up event handlers
            SetupEventHandlers();

            ChimeraLogger.Log("EQUIPMENT", "Equipment degradation systems initialized", this);
        }

        /// <summary>
        /// Set up event handlers between systems
        /// </summary>
        private void SetupEventHandlers()
        {
            _degradationSimulator.OnEquipmentDegraded += HandleEquipmentDegraded;
            _malfunctionDetector.OnMalfunctionDetected += (malfunction) => {
                _activeMalfunctions.Add(malfunction);
                OnMalfunctionDetected?.Invoke(malfunction);
            };

            _maintenanceScheduler.OnMaintenanceDue += HandleMaintenanceDue;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Register equipment for monitoring
        /// </summary>
        public void RegisterEquipment(BasicEquipmentData equipment)
        {
            if (string.IsNullOrEmpty(equipment?.EquipmentId)) return;

            if (_monitoredEquipment.Any(e => e.EquipmentId == equipment.EquipmentId)) return;

            _monitoredEquipment.Add(equipment);
            _equipmentRegistry.RegisterEquipment(equipment);

            // Set up reliability profile if available
            if (!_reliabilityProfiles.ContainsKey(equipment.Type.ToString()))
            {
                _reliabilityProfiles[equipment.Type.ToString()] = CreateDefaultReliabilityProfile(equipment.Type);
            }

            // BasicEquipmentData has no ReliabilityProfile field; registry uses profiles internally

            OnEquipmentAdded?.Invoke(equipment);
            ChimeraLogger.Log("EQUIPMENT", $"Registered equipment {equipment.EquipmentId}", this);
        }

        /// <summary>
        /// Unregister equipment from monitoring
        /// </summary>
        public void UnregisterEquipment(string equipmentId)
        {
            var equipment = _monitoredEquipment.Find(e => e.EquipmentId == equipmentId);
            if (equipment != null)
            {
                _monitoredEquipment.Remove(equipment);
                _equipmentRegistry.UnregisterEquipment(equipmentId);
                OnEquipmentRemoved?.Invoke(equipment);
            }
        }

        /// <summary>
        /// Get equipment by ID
        /// </summary>
        public BasicEquipmentData GetEquipment(string equipmentId)
        {
            return _equipmentRegistry.GetEquipment(equipmentId);
        }

        /// <summary>
        /// Get all monitored equipment
        /// </summary>
        public List<BasicEquipmentData> GetAllEquipment()
        {
            return new List<BasicEquipmentData>(_monitoredEquipment);
        }

        /// <summary>
        /// Perform maintenance on equipment
        /// </summary>
        public void PerformMaintenance(string equipmentId, MaintenanceType type, string technicianId,
                                     List<string> tasks, List<string> parts, float cost, TimeSpan duration)
        {
            var equipment = GetEquipment(equipmentId);
            if (equipment == null) return;

            EquipmentInstance.PerformMaintenance(equipmentId);

            // Basic maintenance record
            var record = new MaintenanceRecord
            {
                RecordId = System.Guid.NewGuid().ToString("N").Substring(0,8),
                EquipmentId = equipment.EquipmentId,
                MaintenanceDate = DateTime.Now,
                Type = type,
                Cost = cost,
                EffectivenessScore = 1f
            };
            _maintenanceHistory.Add(record);

            OnMaintenanceCompleted?.Invoke(record);
            ChimeraLogger.Log("EQUIPMENT", $"Performed maintenance on {equipment.EquipmentId}", this);
        }

        /// <summary>
        /// Resolve malfunction
        /// </summary>
        public void ResolveMalfunction(string malfunctionId, DateTime resolutionTime)
        {
            var malfunction = _activeMalfunctions.Find(m => m.MalfunctionId == malfunctionId);
            if (malfunction != null)
            {
                // Minimal resolve: remove from active list
                _activeMalfunctions.Remove(malfunction);
                OnMalfunctionResolved?.Invoke(malfunction);
            }
        }

        /// <summary>
        /// Get current system health assessment
        /// </summary>
        public EquipmentHealthAssessment GetHealthAssessment()
        {
            return HealthAssessmentSystem.PerformSystemHealthAssessment();
        }

        /// <summary>
        /// Get active malfunctions
        /// </summary>
        public List<EquipmentMalfunction> GetActiveMalfunctions()
        {
            return new List<EquipmentMalfunction>(_activeMalfunctions);
        }

        /// <summary>
        /// Get maintenance history
        /// </summary>
        public List<MaintenanceRecord> GetMaintenanceHistory(string equipmentId = null)
        {
            if (string.IsNullOrEmpty(equipmentId))
                return new List<MaintenanceRecord>(_maintenanceHistory);

            return _maintenanceHistory.Where(r => r.EquipmentId == equipmentId).ToList();
        }

        /// <summary>
        /// Set degradation settings
        /// </summary>
        public void SetDegradationSettings(float baseRate, float stressMultiplier)
        {
            BaseDegradationRate = baseRate;
            EnvironmentalStressMultiplier = stressMultiplier;
            _degradationSimulator?.UpdateSettings(baseRate, stressMultiplier);
        }

        /// <summary>
        /// Enable/disable system features
        /// </summary>
        public void SetSystemFeatures(bool degradation, bool malfunctions, bool wearFailures, bool maintenanceRewards)
        {
            EnableEquipmentDegradation = degradation;
            EnableRandomMalfunctions = malfunctions;
            EnableWearBasedFailures = wearFailures;
            EnableMaintenanceRewards = maintenanceRewards;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Check for new malfunctions
        /// </summary>
        private void CheckForMalfunctions()
        {
            if (!EnableRandomMalfunctions && !EnableWearBasedFailures) return;

            var analyses = new List<MalfunctionAnalysis>();

            foreach (var equipment in _monitoredEquipment)
            {
                var equipmentAnalyses = _malfunctionDetector.AnalyzeEquipment(equipment);
                analyses.AddRange(equipmentAnalyses);
            }

            // Process analyses and create malfunctions
            foreach (var analysis in analyses.OrderByDescending(a => a.Confidence))
            {
                if (_activeMalfunctions.Count >= MaxConcurrentMalfunctions)
                    break;

                var equipment = GetEquipment(analysis.EquipmentId);
                if (equipment == null) continue;

                // Check if this type of malfunction already exists for this equipment
                bool alreadyExists = _activeMalfunctions.Any(m =>
                    m.EquipmentId == analysis.EquipmentId &&
                    m.Type == analysis.MalfunctionType);

                if (!alreadyExists)
                {
                    var malfunction = _malfunctionDetector.GenerateMalfunction(analysis, equipment);
                    _activeMalfunctions.Add(malfunction);
                    OnMalfunctionDetected?.Invoke(malfunction);
                }
            }
        }

        /// <summary>
        /// Handle equipment degradation event
        /// </summary>
        private void HandleEquipmentDegraded(BasicEquipmentData equipment)
        {
            // Placeholder: integrate with future wear tracking
            ChimeraLogger.Log("EQUIPMENT", $"Degradation updated for {equipment.EquipmentId}", this);
        }

        /// <summary>
        /// Handle maintenance due event
        /// </summary>
        private void HandleMaintenanceDue(BasicEquipmentData equipment)
        {
            if (EnableMaintenanceRewards)
            {
                // Could implement maintenance reward system here
                ChimeraLogger.Log("EQUIPMENT", $"Maintenance due for {equipment.EquipmentId}", this);
            }
        }

        /// <summary>
        /// Create default reliability profile for equipment type
        /// </summary>
        private EquipmentReliabilityProfile CreateDefaultReliabilityProfile(EquipmentType type)
        {
            return new EquipmentReliabilityProfile
            {
                Type = type,
                MeanTimeBetweenFailures = GetDefaultMTBF(type),
                AverageLifespan = GetDefaultLifespan(type),
                FailureRate = GetDefaultFailureRate(type),
                WearProgressionRate = BaseDegradationRate,
                CriticalWearThreshold = 0.8f
            };
        }

        /// <summary>
        /// Get default MTBF for equipment type
        /// </summary>
        private float GetDefaultMTBF(Data.Equipment.EquipmentType type)
        {
            switch (type)
            {
                case Data.Equipment.EquipmentType.LED_Light:
                case Data.Equipment.EquipmentType.HPS_Light:
                case Data.Equipment.EquipmentType.GrowLight:
                    return 8760f; // 1 year
                case Data.Equipment.EquipmentType.Exhaust_Fan:
                case Data.Equipment.EquipmentType.Intake_Fan:
                case Data.Equipment.EquipmentType.Air_Circulator:
                    return 4380f; // 6 months
                case Data.Equipment.EquipmentType.Watering_System:
                case Data.Equipment.EquipmentType.Drip_System:
                    return 2190f; // 3 months
                case Data.Equipment.EquipmentType.Climate_Controller:
                case Data.Equipment.EquipmentType.Environmental_Controller:
                    return 2920f; // 4 months
                case Data.Equipment.EquipmentType.Reservoir:
                    return 8760f; // 1 year
                default: return 4380f; // 6 months
            }
        }

        /// <summary>
        /// Get default lifespan for equipment type
        /// </summary>
        private float GetDefaultLifespan(Data.Equipment.EquipmentType type)
        {
            switch (type)
            {
                case Data.Equipment.EquipmentType.LED_Light:
                case Data.Equipment.EquipmentType.HPS_Light:
                case Data.Equipment.EquipmentType.GrowLight:
                    return 3f; // 3 years
                case Data.Equipment.EquipmentType.Exhaust_Fan:
                case Data.Equipment.EquipmentType.Intake_Fan:
                case Data.Equipment.EquipmentType.Air_Circulator:
                    return 8f; // 8 years
                case Data.Equipment.EquipmentType.Watering_System:
                case Data.Equipment.EquipmentType.Drip_System:
                    return 5f; // 5 years
                case Data.Equipment.EquipmentType.Climate_Controller:
                case Data.Equipment.EquipmentType.Environmental_Controller:
                    return 10f; // 10 years
                case Data.Equipment.EquipmentType.Reservoir:
                    return 15f; // 15 years
                default: return 7f; // 7 years
            }
        }

        /// <summary>
        /// Get default failure rate for equipment type
        /// </summary>
        private float GetDefaultFailureRate(Data.Equipment.EquipmentType type)
        {
            switch (type)
            {
                case Data.Equipment.EquipmentType.LED_Light:
                case Data.Equipment.EquipmentType.HPS_Light:
                case Data.Equipment.EquipmentType.GrowLight:
                    return 0.001f; // Low failure rate
                case Data.Equipment.EquipmentType.Exhaust_Fan:
                case Data.Equipment.EquipmentType.Intake_Fan:
                case Data.Equipment.EquipmentType.Air_Circulator:
                    return 0.005f; // Higher failure rate for mechanical components
                case Data.Equipment.EquipmentType.Watering_System:
                case Data.Equipment.EquipmentType.Drip_System:
                    return 0.003f; // Medium failure rate
                case Data.Equipment.EquipmentType.Climate_Controller:
                case Data.Equipment.EquipmentType.Environmental_Controller:
                    return 0.004f; // Higher failure rate for complex electronics
                case Data.Equipment.EquipmentType.Reservoir:
                    return 0.0005f; // Very low failure rate
                default: return 0.002f; // Default failure rate
            }
        }

        #endregion

        #region Manager Interface

        protected override void OnManagerInitialize()
        {
            // Manager initialization
            ChimeraLogger.Log("EQUIPMENT", "EquipmentDegradationManager initialized", this);
        }

        protected override void OnManagerShutdown()
        {
            // Clean up resources
            _monitoredEquipment.Clear();
            _activeMalfunctions.Clear();
            _maintenanceHistory.Clear();

            ChimeraLogger.Log("EQUIPMENT", "EquipmentDegradationManager shutdown", this);
        }

        #endregion
    }

    /// <summary>
    /// Equipment registry for managing monitored equipment
    /// </summary>
    public class EquipmentRegistry
    {
        private Dictionary<string, BasicEquipmentData> _equipment = new Dictionary<string, BasicEquipmentData>();

        public void RegisterEquipment(BasicEquipmentData equipment)
        {
            _equipment[equipment.EquipmentId] = equipment;
        }

        public void UnregisterEquipment(string equipmentId)
        {
            _equipment.Remove(equipmentId);
        }

        public BasicEquipmentData GetEquipment(string equipmentId)
        {
            return _equipment.TryGetValue(equipmentId, out var equipment) ? equipment : null;
        }

        public List<BasicEquipmentData> GetAllEquipment()
        {
            return new List<BasicEquipmentData>(_equipment.Values);
        }

        public int EquipmentCount => _equipment.Count;
    }

    /// <summary>
    /// Degradation simulation system
    /// </summary>
    public class DegradationSimulation
    {
        public float BaseDegradationRate;
        public float EnvironmentalStressMultiplier;

        public System.Action<BasicEquipmentData> OnEquipmentDegraded;

        public DegradationSimulation(float baseRate, float stressMultiplier)
        {
            BaseDegradationRate = baseRate;
            EnvironmentalStressMultiplier = stressMultiplier;
        }

        public void UpdateDegradation(float deltaTime)
        {
            // This would be called from the main manager
            // Implementation would iterate through equipment and apply degradation
        }

        public void UpdateSettings(float baseRate, float stressMultiplier)
        {
            BaseDegradationRate = baseRate;
            EnvironmentalStressMultiplier = stressMultiplier;
        }
    }

    /// <summary>
    /// Maintenance scheduling system
    /// </summary>
    public class MaintenanceScheduler
    {
        public System.Action<BasicEquipmentData> OnMaintenanceDue;

        public void UpdateSchedules()
        {
            // Check for overdue maintenance and trigger events
        }
    }
}
