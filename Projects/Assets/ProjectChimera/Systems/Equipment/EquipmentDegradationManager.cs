using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment
{
    /// <summary>
    /// Phase 4.1.b: Equipment Malfunctions and Degradation System
    /// Implements realistic equipment aging, failure modes, and maintenance requirements
    /// Transforms equipment management from static to dynamic strategic challenge
    /// </summary>
    public class EquipmentDegradationManager : ChimeraManager
    {
        [Header("Phase 4.1.b Configuration")]
        public bool EnableEquipmentDegradation = true;
        public bool EnableRandomMalfunctions = true;
        public bool EnableWearBasedFailures = true;
        public bool EnableMaintenanceRewards = true;
        
        [Header("Degradation Settings")]
        [Range(0f, 1f)] public float BaseDegradationRate = 0.02f;
        [Range(0f, 5f)] public float EnvironmentalStressMultiplier = 2f;
        [Range(1, 50)] public int MaxConcurrentMalfunctions = 10;
        public float MaintenanceEfficiencyBonus = 0.25f;
        
        [Header("Equipment Monitoring")]
        [SerializeField] private List<EquipmentInstance> monitoredEquipment = new List<EquipmentInstance>();
        [SerializeField] private List<EquipmentMalfunction> activeMalfunctions = new List<EquipmentMalfunction>();
        [SerializeField] private List<MaintenanceRecord> maintenanceHistory = new List<MaintenanceRecord>();
        [SerializeField] private Dictionary<string, EquipmentReliabilityProfile> reliabilityProfiles = new Dictionary<string, EquipmentReliabilityProfile>();
        
        [Header("Failure Prediction")]
        [SerializeField] private EquipmentHealthAssessment systemHealth = new EquipmentHealthAssessment();
        [SerializeField] private List<string> criticalEquipmentIds = new List<string>();
        [SerializeField] private Dictionary<string, float> predictedFailureWindows = new Dictionary<string, float>();
        
        // Phase 4.1.b Data Structures
        [System.Serializable]
        public class EquipmentInstance
        {
            public string EquipmentId;
            public EquipmentType Type;
            public string ModelName;
            public Vector3 Location;
            public DateTime InstallationDate;
            public DateTime LastMaintenance;
            public float WearLevel;
            public float EfficiencyRating;
            public float ReliabilityScore;
            public List<string> ActiveIssues;
            public OperationalStatus Status;
            public float TotalOperatingHours;
            public float PowerConsumption;
            public EnvironmentalStressFactors StressFactors;
            public MaintenanceSchedule MaintenanceSchedule;
        }
        
        [System.Serializable]
        public class EquipmentMalfunction
        {
            public string MalfunctionId;
            public string EquipmentId;
            public MalfunctionType Type;
            public MalfunctionSeverity Severity;
            public DateTime OccurrenceTime;
            public string CauseAnalysis;
            public List<string> Symptoms;
            public float ImpactOnPerformance;
            public float RepairCost;
            public TimeSpan EstimatedRepairTime;
            public bool RequiresSpecialist;
            public RepairComplexity Complexity;
            public List<string> RequiredParts;
        }
        
        [System.Serializable]
        public class MaintenanceRecord
        {
            public string RecordId;
            public string EquipmentId;
            public DateTime MaintenanceDate;
            public MaintenanceType Type;
            public string TechnicianId;
            public List<string> TasksPerformed;
            public List<string> PartsReplaced;
            public float Cost;
            public TimeSpan Duration;
            public float EffectivenessScore;
            public string Notes;
            public bool PreventiveMaintenance;
        }
        
        [System.Serializable]
        public class EquipmentReliabilityProfile
        {
            public EquipmentType Type;
            public float MeanTimeBetweenFailures;
            public float AverageLifespan;
            public Dictionary<MalfunctionType, float> CommonFailureModes;
            public Dictionary<string, float> EnvironmentalSensitivity;
            public float MaintenanceResponseCurve;
            public float WearProgressionRate;
            public List<string> CriticalComponents;
            public float RedundancyLevel;
        }
        
        [System.Serializable]
        public class EnvironmentalStressFactors
        {
            public float TemperatureStress;
            public float HumidityStress;
            public float VibrationStress;
            public float DustAccumulation;
            public float CorrosionRisk;
            public float ElectricalStress;
            public float ThermalCyclingStress;
            public DateTime LastEnvironmentalAssessment;
        }
        
        [System.Serializable]
        public class MaintenanceSchedule
        {
            public string ScheduleId;
            public string EquipmentId;
            public List<MaintenanceTask> ScheduledTasks;
            public DateTime NextMaintenanceDate;
            public MaintenancePriority Priority;
            public float EstimatedCost;
            public TimeSpan EstimatedDuration;
            public bool RequiresShutdown;
        }
        
        [System.Serializable]
        public class MaintenanceTask
        {
            public string TaskId;
            public string TaskName;
            public string Description;
            public TimeSpan Frequency;
            public float Importance;
            public List<string> RequiredTools;
            public List<string> RequiredSkills;
            public float EstimatedTime;
            public float Cost;
        }
        
        [System.Serializable]
        public class EquipmentHealthAssessment
        {
            public float OverallSystemHealth;
            public float CriticalEquipmentHealth;
            public float MaintenanceBacklogRisk;
            public float UpcomingFailureRisk;
            public DateTime LastAssessment;
            public List<string> SystemVulnerabilities;
            public Dictionary<EquipmentType, float> TypeHealthScores;
            public float MaintenanceEfficiencyScore;
        }
        
        public enum EquipmentType
        {
            HVACSystem,
            LightingSystem,
            IrrigationSystem,
            VentilationFan,
            EnvironmentalSensor,
            WaterPump,
            NutrientDoser,
            SecurityCamera,
            PowerSupply,
            AutomationController,
            ExtractionFan,
            HumidityController,
            TemperatureController,
            CO2Generator,
            OzoneGenerator,
            UVSterilizer
        }
        
        public enum MalfunctionType
        {
            MechanicalFailure,
            ElectricalFailure,
            SensorDrift,
            BlockageOrClogging,
            MotorWear,
            CalibrationLoss,
            SoftwareError,
            PowerSupplyIssue,
            CommunicationFailure,
            OverheatingProblem,
            VibrationDamage,
            CorrosionDamage,
            WearAndTear,
            UserError
        }
        
        public enum MalfunctionSeverity
        {
            Minor,
            Moderate,
            Major,
            Critical,
            Catastrophic
        }
        
        public enum OperationalStatus
        {
            Operational,
            Degraded,
            Maintenance,
            Malfunction,
            Offline,
            Decommissioned
        }
        
        public enum MaintenanceType
        {
            Preventive,
            Corrective,
            Predictive,
            Emergency,
            Upgrade,
            Calibration
        }
        
        public enum RepairComplexity
        {
            Simple,
            Moderate,
            Complex,
            ExpertRequired,
            ManufacturerService
        }
        
        public enum MaintenancePriority
        {
            Low,
            Medium,
            High,
            Critical,
            Emergency
        }
        
        protected override void OnManagerInitialize()
        {
            InitializeReliabilityProfiles();
            SetupEquipmentMonitoring();
            RegisterEventListeners();
            StartDegradationProcessing();
            
            Debug.Log($"[Phase 4.1.b] EquipmentDegradationManager initialized - {monitoredEquipment.Count} equipment units monitored");
        }
        
        protected override void OnManagerShutdown()
        {
            UnregisterEventListeners();
            SaveMaintenanceHistory();
            StopDegradationProcessing();
            
            Debug.Log($"[Phase 4.1.b] EquipmentDegradationManager shutdown - {maintenanceHistory.Count} maintenance records saved");
        }
        
        private void InitializeReliabilityProfiles()
        {
            // Phase 4.1.b: Initialize equipment-specific reliability profiles
            reliabilityProfiles["HVACSystem"] = new EquipmentReliabilityProfile
            {
                Type = EquipmentType.HVACSystem,
                MeanTimeBetweenFailures = 2160f, // 90 days
                AverageLifespan = 43800f, // 5 years
                CommonFailureModes = new Dictionary<MalfunctionType, float>
                {
                    [MalfunctionType.MechanicalFailure] = 0.4f,
                    [MalfunctionType.ElectricalFailure] = 0.3f,
                    [MalfunctionType.SensorDrift] = 0.2f,
                    [MalfunctionType.BlockageOrClogging] = 0.1f
                },
                EnvironmentalSensitivity = new Dictionary<string, float>
                {
                    ["Temperature"] = 0.3f,
                    ["Humidity"] = 0.4f,
                    ["Dust"] = 0.2f,
                    ["Vibration"] = 0.1f
                },
                MaintenanceResponseCurve = 0.7f,
                WearProgressionRate = 0.02f,
                CriticalComponents = new List<string> { "Compressor", "Evaporator", "Condenser", "Thermostat" },
                RedundancyLevel = 0.5f
            };
            
            reliabilityProfiles["LightingSystem"] = new EquipmentReliabilityProfile
            {
                Type = EquipmentType.LightingSystem,
                MeanTimeBetweenFailures = 4320f, // 180 days
                AverageLifespan = 26280f, // 3 years
                CommonFailureModes = new Dictionary<MalfunctionType, float>
                {
                    [MalfunctionType.ElectricalFailure] = 0.5f,
                    [MalfunctionType.OverheatingProblem] = 0.3f,
                    [MalfunctionType.WearAndTear] = 0.2f
                },
                EnvironmentalSensitivity = new Dictionary<string, float>
                {
                    ["Temperature"] = 0.4f,
                    ["Humidity"] = 0.3f,
                    ["Dust"] = 0.2f,
                    ["Vibration"] = 0.1f
                },
                MaintenanceResponseCurve = 0.6f,
                WearProgressionRate = 0.03f,
                CriticalComponents = new List<string> { "LED_Modules", "Driver", "Heatsink", "Optics" },
                RedundancyLevel = 0.8f
            };
            
            reliabilityProfiles["IrrigationSystem"] = new EquipmentReliabilityProfile
            {
                Type = EquipmentType.IrrigationSystem,
                MeanTimeBetweenFailures = 1440f, // 60 days
                AverageLifespan = 21900f, // 2.5 years
                CommonFailureModes = new Dictionary<MalfunctionType, float>
                {
                    [MalfunctionType.BlockageOrClogging] = 0.4f,
                    [MalfunctionType.MechanicalFailure] = 0.3f,
                    [MalfunctionType.SensorDrift] = 0.2f,
                    [MalfunctionType.ElectricalFailure] = 0.1f
                },
                EnvironmentalSensitivity = new Dictionary<string, float>
                {
                    ["WaterQuality"] = 0.5f,
                    ["Temperature"] = 0.2f,
                    ["Pressure"] = 0.2f,
                    ["pH"] = 0.1f
                },
                MaintenanceResponseCurve = 0.8f,
                WearProgressionRate = 0.04f,
                CriticalComponents = new List<string> { "Pumps", "Valves", "Filters", "Sensors" },
                RedundancyLevel = 0.3f
            };
        }
        
        private void SetupEquipmentMonitoring()
        {
            // Phase 4.1.b: Continuous equipment monitoring
            InvokeRepeating(nameof(ProcessEquipmentDegradation), 30f, 120f); // Every 2 minutes
            InvokeRepeating(nameof(EvaluateMalfunctionRisk), 60f, 300f); // Every 5 minutes
            InvokeRepeating(nameof(UpdateSystemHealthAssessment), 300f, 900f); // Every 15 minutes
        }
        
        private void ProcessEquipmentDegradation()
        {
            // Phase 4.1.b: Process wear and degradation for all equipment
            foreach (var equipment in monitoredEquipment.Where(e => e.Status == OperationalStatus.Operational))
            {
                ProcessEquipmentWear(equipment);
                UpdateEquipmentEfficiency(equipment);
                CheckForDegradationThresholds(equipment);
            }
        }
        
        private void ProcessEquipmentWear(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Calculate equipment wear based on usage and environmental factors
            float baseWear = BaseDegradationRate * Time.deltaTime;
            float environmentalMultiplier = CalculateEnvironmentalWearMultiplier(equipment);
            float usageMultiplier = CalculateUsageWearMultiplier(equipment);
            
            float totalWear = baseWear * environmentalMultiplier * usageMultiplier;
            equipment.WearLevel = Mathf.Clamp01(equipment.WearLevel + totalWear);
            
            // Update operating hours
            equipment.TotalOperatingHours += Time.deltaTime / 3600f; // Convert to hours
            
            // Update reliability score based on wear
            equipment.ReliabilityScore = 1f - (equipment.WearLevel * 0.8f);
            
            if (equipment.WearLevel > 0.9f)
            {
                Debug.LogWarning($"[Phase 4.1.b] Equipment {equipment.EquipmentId} approaching critical wear level: {equipment.WearLevel:F2}");
            }
        }
        
        private float CalculateEnvironmentalWearMultiplier(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Environmental stress affects wear rate
            var stress = equipment.StressFactors;
            
            float totalStress = (stress.TemperatureStress + stress.HumidityStress + 
                               stress.VibrationStress + stress.DustAccumulation + 
                               stress.CorrosionRisk + stress.ElectricalStress + 
                               stress.ThermalCyclingStress) / 7f;
            
            return 1f + (totalStress * EnvironmentalStressMultiplier);
        }
        
        private float CalculateUsageWearMultiplier(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Usage intensity affects wear rate
            float normalizedUsage = equipment.PowerConsumption / 1000f; // Normalized to kW
            return 1f + (normalizedUsage * 0.1f);
        }
        
        private void UpdateEquipmentEfficiency(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Efficiency degrades with wear
            float baseEfficiency = 1f;
            float wearPenalty = equipment.WearLevel * 0.3f;
            float malfunctionPenalty = equipment.ActiveIssues.Count * 0.1f;
            
            equipment.EfficiencyRating = Mathf.Clamp01(baseEfficiency - wearPenalty - malfunctionPenalty);
        }
        
        private void CheckForDegradationThresholds(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Check if equipment has crossed degradation thresholds
            if (equipment.WearLevel > 0.7f && equipment.Status == OperationalStatus.Operational)
            {
                equipment.Status = OperationalStatus.Degraded;
                TriggerMaintenanceAlert(equipment, "Equipment degradation threshold exceeded");
            }
            
            if (equipment.EfficiencyRating < 0.6f)
            {
                ScheduleMaintenanceTask(equipment, MaintenanceType.Preventive, MaintenancePriority.High);
            }
        }
        
        private void EvaluateMalfunctionRisk()
        {
            // Phase 4.1.b: Assess malfunction risk for all equipment
            if (!EnableRandomMalfunctions || activeMalfunctions.Count >= MaxConcurrentMalfunctions)
                return;
            
            foreach (var equipment in monitoredEquipment)
            {
                float malfunctionRisk = CalculateMalfunctionRisk(equipment);
                
                if (UnityEngine.Random.Range(0f, 1f) < malfunctionRisk)
                {
                    GenerateMalfunction(equipment);
                }
            }
        }
        
        private float CalculateMalfunctionRisk(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Calculate malfunction probability
            float baseRisk = 0.001f; // 0.1% base risk per evaluation
            float wearRisk = equipment.WearLevel * 0.005f;
            float environmentalRisk = CalculateEnvironmentalRisk(equipment) * 0.003f;
            float maintenanceRisk = CalculateMaintenanceRisk(equipment) * 0.002f;
            
            return baseRisk + wearRisk + environmentalRisk + maintenanceRisk;
        }
        
        private float CalculateEnvironmentalRisk(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Environmental factors increase malfunction risk
            var stress = equipment.StressFactors;
            
            return (stress.TemperatureStress * 0.3f + stress.HumidityStress * 0.2f + 
                   stress.VibrationStress * 0.2f + stress.DustAccumulation * 0.1f + 
                   stress.CorrosionRisk * 0.1f + stress.ElectricalStress * 0.1f) / 6f;
        }
        
        private float CalculateMaintenanceRisk(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Poor maintenance increases malfunction risk
            TimeSpan timeSinceLastMaintenance = DateTime.Now - equipment.LastMaintenance;
            float daysSinceMaintenance = (float)timeSinceLastMaintenance.TotalDays;
            
            // Risk increases exponentially with time since last maintenance
            return Mathf.Clamp01(daysSinceMaintenance / 180f); // 180 days maximum
        }
        
        private void GenerateMalfunction(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Generate realistic malfunction based on equipment type
            var reliability = reliabilityProfiles.GetValueOrDefault(equipment.Type.ToString());
            if (reliability == null) return;
            
            MalfunctionType malfunctionType = SelectMalfunctionType(reliability);
            MalfunctionSeverity severity = DetermineMalfunctionSeverity(equipment, malfunctionType);
            
            var malfunction = new EquipmentMalfunction
            {
                MalfunctionId = Guid.NewGuid().ToString("N")[..8],
                EquipmentId = equipment.EquipmentId,
                Type = malfunctionType,
                Severity = severity,
                OccurrenceTime = DateTime.Now,
                CauseAnalysis = GenerateCauseAnalysis(equipment, malfunctionType),
                Symptoms = GenerateSymptoms(malfunctionType, severity),
                ImpactOnPerformance = CalculatePerformanceImpact(severity),
                RepairCost = EstimateRepairCost(malfunctionType, severity),
                EstimatedRepairTime = EstimateRepairTime(malfunctionType, severity),
                RequiresSpecialist = DetermineSpecialistRequirement(malfunctionType, severity),
                Complexity = DetermineRepairComplexity(malfunctionType, severity),
                RequiredParts = GenerateRequiredParts(malfunctionType, equipment.Type)
            };
            
            activeMalfunctions.Add(malfunction);
            equipment.ActiveIssues.Add(malfunction.MalfunctionId);
            equipment.Status = OperationalStatus.Malfunction;
            
            Debug.LogWarning($"[Phase 4.1.b] Malfunction generated: {equipment.EquipmentId} - {malfunctionType} ({severity})");
            
            // Fire malfunction event
            GameManager.Instance.GetManager<EventManager>()?.TriggerEvent("EquipmentMalfunction", malfunction);
        }
        
        private MalfunctionType SelectMalfunctionType(EquipmentReliabilityProfile reliability)
        {
            // Phase 4.1.b: Select malfunction type based on reliability profile
            float random = UnityEngine.Random.Range(0f, 1f);
            float cumulative = 0f;
            
            foreach (var failureMode in reliability.CommonFailureModes)
            {
                cumulative += failureMode.Value;
                if (random <= cumulative)
                {
                    return failureMode.Key;
                }
            }
            
            return MalfunctionType.WearAndTear;
        }
        
        private MalfunctionSeverity DetermineMalfunctionSeverity(EquipmentInstance equipment, MalfunctionType type)
        {
            // Phase 4.1.b: Determine severity based on equipment condition and malfunction type
            float severityRoll = UnityEngine.Random.Range(0f, 1f);
            float wearModifier = equipment.WearLevel * 0.3f;
            float adjustedRoll = severityRoll + wearModifier;
            
            if (adjustedRoll < 0.4f) return MalfunctionSeverity.Minor;
            if (adjustedRoll < 0.7f) return MalfunctionSeverity.Moderate;
            if (adjustedRoll < 0.9f) return MalfunctionSeverity.Major;
            if (adjustedRoll < 0.98f) return MalfunctionSeverity.Critical;
            return MalfunctionSeverity.Catastrophic;
        }
        
        private void UpdateSystemHealthAssessment()
        {
            // Phase 4.1.b: Comprehensive system health evaluation
            systemHealth = new EquipmentHealthAssessment
            {
                LastAssessment = DateTime.Now,
                SystemVulnerabilities = new List<string>(),
                TypeHealthScores = new Dictionary<EquipmentType, float>()
            };
            
            // Calculate overall system health
            float totalHealth = 0f;
            int equipmentCount = monitoredEquipment.Count;
            
            foreach (var equipment in monitoredEquipment)
            {
                float equipmentHealth = CalculateEquipmentHealth(equipment);
                totalHealth += equipmentHealth;
                
                // Track by equipment type
                if (!systemHealth.TypeHealthScores.ContainsKey(equipment.Type))
                {
                    systemHealth.TypeHealthScores[equipment.Type] = 0f;
                }
                systemHealth.TypeHealthScores[equipment.Type] += equipmentHealth;
            }
            
            systemHealth.OverallSystemHealth = equipmentCount > 0 ? totalHealth / equipmentCount : 1f;
            
            // Calculate critical equipment health
            var criticalEquipment = monitoredEquipment.Where(e => criticalEquipmentIds.Contains(e.EquipmentId));
            systemHealth.CriticalEquipmentHealth = criticalEquipment.Any() ? 
                criticalEquipment.Average(e => CalculateEquipmentHealth(e)) : 1f;
            
            // Calculate maintenance metrics
            systemHealth.MaintenanceBacklogRisk = CalculateMaintenanceBacklogRisk();
            systemHealth.UpcomingFailureRisk = CalculateUpcomingFailureRisk();
            systemHealth.MaintenanceEfficiencyScore = CalculateMaintenanceEfficiencyScore();
            
            // Identify vulnerabilities
            IdentifySystemVulnerabilities();
            
            Debug.Log($"[Phase 4.1.b] System health updated - Overall: {systemHealth.OverallSystemHealth:F2}, Critical: {systemHealth.CriticalEquipmentHealth:F2}");
        }
        
        private float CalculateEquipmentHealth(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Calculate individual equipment health score
            float wearHealth = 1f - equipment.WearLevel;
            float efficiencyHealth = equipment.EfficiencyRating;
            float reliabilityHealth = equipment.ReliabilityScore;
            float malfunctionHealth = equipment.ActiveIssues.Count == 0 ? 1f : 0.5f;
            
            return (wearHealth + efficiencyHealth + reliabilityHealth + malfunctionHealth) / 4f;
        }
        
        private void TriggerMaintenanceAlert(EquipmentInstance equipment, string reason)
        {
            // Phase 4.1.b: Generate maintenance alert
            var alertData = new Dictionary<string, object>
            {
                ["EquipmentId"] = equipment.EquipmentId,
                ["EquipmentType"] = equipment.Type,
                ["Reason"] = reason,
                ["WearLevel"] = equipment.WearLevel,
                ["EfficiencyRating"] = equipment.EfficiencyRating,
                ["Priority"] = DetermineMaintenancePriority(equipment)
            };
            
            GameManager.Instance.GetManager<EventManager>()?.TriggerEvent("MaintenanceAlert", alertData);
        }
        
        private MaintenancePriority DetermineMaintenancePriority(EquipmentInstance equipment)
        {
            // Phase 4.1.b: Determine maintenance priority based on equipment condition
            if (equipment.WearLevel > 0.9f || equipment.ActiveIssues.Count > 2)
                return MaintenancePriority.Emergency;
            if (equipment.WearLevel > 0.7f || equipment.EfficiencyRating < 0.6f)
                return MaintenancePriority.Critical;
            if (equipment.WearLevel > 0.5f || equipment.EfficiencyRating < 0.8f)
                return MaintenancePriority.High;
            if (equipment.WearLevel > 0.3f)
                return MaintenancePriority.Medium;
            return MaintenancePriority.Low;
        }
        
        // Public API methods
        public List<EquipmentInstance> GetMonitoredEquipment() => new List<EquipmentInstance>(monitoredEquipment);
        public List<EquipmentMalfunction> GetActiveMalfunctions() => new List<EquipmentMalfunction>(activeMalfunctions);
        public EquipmentHealthAssessment GetSystemHealth() => systemHealth;
        public List<MaintenanceRecord> GetMaintenanceHistory() => new List<MaintenanceRecord>(maintenanceHistory);
        
        public bool RepairMalfunction(string malfunctionId, float repairQuality)
        {
            var malfunction = activeMalfunctions.FirstOrDefault(m => m.MalfunctionId == malfunctionId);
            if (malfunction == null) return false;
            
            var equipment = monitoredEquipment.FirstOrDefault(e => e.EquipmentId == malfunction.EquipmentId);
            if (equipment == null) return false;
            
            // Remove malfunction
            activeMalfunctions.Remove(malfunction);
            equipment.ActiveIssues.Remove(malfunctionId);
            
            // Apply repair benefits
            float repairBenefit = repairQuality * 0.2f;
            equipment.WearLevel = Mathf.Clamp01(equipment.WearLevel - repairBenefit);
            equipment.ReliabilityScore = Mathf.Clamp01(equipment.ReliabilityScore + repairBenefit);
            
            // Update status
            equipment.Status = equipment.ActiveIssues.Count == 0 ? OperationalStatus.Operational : OperationalStatus.Malfunction;
            
            // Record maintenance
            var record = new MaintenanceRecord
            {
                RecordId = Guid.NewGuid().ToString("N")[..8],
                EquipmentId = equipment.EquipmentId,
                MaintenanceDate = DateTime.Now,
                Type = MaintenanceType.Corrective,
                Cost = malfunction.RepairCost,
                Duration = malfunction.EstimatedRepairTime,
                EffectivenessScore = repairQuality,
                Notes = $"Repaired {malfunction.Type} malfunction"
            };
            
            maintenanceHistory.Add(record);
            equipment.LastMaintenance = DateTime.Now;
            
            Debug.Log($"[Phase 4.1.b] Malfunction repaired: {malfunctionId} (Quality: {repairQuality:F2})");
            
            return true;
        }
        
        public void RegisterEquipment(EquipmentInstance equipment)
        {
            if (!monitoredEquipment.Any(e => e.EquipmentId == equipment.EquipmentId))
            {
                monitoredEquipment.Add(equipment);
                Debug.Log($"[Phase 4.1.b] Equipment registered for monitoring: {equipment.EquipmentId}");
            }
        }
        
        public void TriggerManualInspection()
        {
            ProcessEquipmentDegradation();
            EvaluateMalfunctionRisk();
            UpdateSystemHealthAssessment();
            Debug.Log($"[Phase 4.1.b] Manual inspection completed - System Health: {systemHealth.OverallSystemHealth:F2}");
        }
        
        // Helper methods for malfunction generation
        private string GenerateCauseAnalysis(EquipmentInstance equipment, MalfunctionType type)
        {
            return type switch
            {
                MalfunctionType.MechanicalFailure => "Mechanical component failure due to wear and operational stress",
                MalfunctionType.ElectricalFailure => "Electrical system failure, possibly due to power fluctuations or component aging",
                MalfunctionType.SensorDrift => "Sensor calibration drift due to environmental exposure and age",
                MalfunctionType.BlockageOrClogging => "System blockage caused by debris accumulation or precipitation",
                MalfunctionType.OverheatingProblem => "Overheating due to inadequate cooling or increased load",
                _ => "Equipment malfunction requiring investigation"
            };
        }
        
        private List<string> GenerateSymptoms(MalfunctionType type, MalfunctionSeverity severity)
        {
            var symptoms = new List<string>();
            
            switch (type)
            {
                case MalfunctionType.MechanicalFailure:
                    symptoms.AddRange(new[] { "Unusual vibration", "Grinding noises", "Reduced output" });
                    break;
                case MalfunctionType.ElectricalFailure:
                    symptoms.AddRange(new[] { "Power fluctuations", "Intermittent operation", "Error codes" });
                    break;
                case MalfunctionType.SensorDrift:
                    symptoms.AddRange(new[] { "Inaccurate readings", "Inconsistent data", "Calibration warnings" });
                    break;
                case MalfunctionType.BlockageOrClogging:
                    symptoms.AddRange(new[] { "Reduced flow", "Pressure buildup", "Overflow conditions" });
                    break;
                case MalfunctionType.OverheatingProblem:
                    symptoms.AddRange(new[] { "High temperature warnings", "Thermal shutdowns", "Reduced efficiency" });
                    break;
            }
            
            return symptoms;
        }
        
        private float CalculatePerformanceImpact(MalfunctionSeverity severity)
        {
            return severity switch
            {
                MalfunctionSeverity.Minor => 0.1f,
                MalfunctionSeverity.Moderate => 0.25f,
                MalfunctionSeverity.Major => 0.5f,
                MalfunctionSeverity.Critical => 0.75f,
                MalfunctionSeverity.Catastrophic => 1f,
                _ => 0.1f
            };
        }
        
        private float EstimateRepairCost(MalfunctionType type, MalfunctionSeverity severity)
        {
            float baseCost = type switch
            {
                MalfunctionType.MechanicalFailure => 500f,
                MalfunctionType.ElectricalFailure => 300f,
                MalfunctionType.SensorDrift => 100f,
                MalfunctionType.BlockageOrClogging => 150f,
                MalfunctionType.OverheatingProblem => 400f,
                _ => 200f
            };
            
            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => 1f,
                MalfunctionSeverity.Moderate => 2f,
                MalfunctionSeverity.Major => 4f,
                MalfunctionSeverity.Critical => 8f,
                MalfunctionSeverity.Catastrophic => 15f,
                _ => 1f
            };
            
            return baseCost * severityMultiplier;
        }
        
        private TimeSpan EstimateRepairTime(MalfunctionType type, MalfunctionSeverity severity)
        {
            int baseMinutes = type switch
            {
                MalfunctionType.MechanicalFailure => 120,
                MalfunctionType.ElectricalFailure => 90,
                MalfunctionType.SensorDrift => 30,
                MalfunctionType.BlockageOrClogging => 45,
                MalfunctionType.OverheatingProblem => 60,
                _ => 60
            };
            
            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => 1f,
                MalfunctionSeverity.Moderate => 2f,
                MalfunctionSeverity.Major => 4f,
                MalfunctionSeverity.Critical => 8f,
                MalfunctionSeverity.Catastrophic => 12f,
                _ => 1f
            };
            
            return TimeSpan.FromMinutes(baseMinutes * severityMultiplier);
        }
        
        private bool DetermineSpecialistRequirement(MalfunctionType type, MalfunctionSeverity severity)
        {
            return severity >= MalfunctionSeverity.Major || 
                   type == MalfunctionType.ElectricalFailure || 
                   type == MalfunctionType.SoftwareError;
        }
        
        private RepairComplexity DetermineRepairComplexity(MalfunctionType type, MalfunctionSeverity severity)
        {
            if (severity == MalfunctionSeverity.Catastrophic)
                return RepairComplexity.ManufacturerService;
            if (severity == MalfunctionSeverity.Critical)
                return RepairComplexity.ExpertRequired;
            if (severity == MalfunctionSeverity.Major)
                return RepairComplexity.Complex;
            if (severity == MalfunctionSeverity.Moderate)
                return RepairComplexity.Moderate;
            return RepairComplexity.Simple;
        }
        
        private List<string> GenerateRequiredParts(MalfunctionType type, EquipmentType equipmentType)
        {
            var parts = new List<string>();
            
            switch (type)
            {
                case MalfunctionType.MechanicalFailure:
                    parts.AddRange(new[] { "Bearings", "Seals", "Gaskets" });
                    break;
                case MalfunctionType.ElectricalFailure:
                    parts.AddRange(new[] { "Fuses", "Relays", "Wiring" });
                    break;
                case MalfunctionType.SensorDrift:
                    parts.AddRange(new[] { "Sensor", "Calibration kit" });
                    break;
                case MalfunctionType.BlockageOrClogging:
                    parts.AddRange(new[] { "Filters", "Cleaning supplies" });
                    break;
            }
            
            return parts;
        }
        
        private void ScheduleMaintenanceTask(EquipmentInstance equipment, MaintenanceType type, MaintenancePriority priority)
        {
            // Implementation for scheduling maintenance tasks
            Debug.Log($"[Phase 4.1.b] Maintenance scheduled for {equipment.EquipmentId}: {type} ({priority})");
        }
        
        private float CalculateMaintenanceBacklogRisk()
        {
            // Calculate risk based on overdue maintenance
            return 0.2f; // Placeholder
        }
        
        private float CalculateUpcomingFailureRisk()
        {
            // Calculate risk of upcoming failures
            return 0.15f; // Placeholder
        }
        
        private float CalculateMaintenanceEfficiencyScore()
        {
            // Calculate maintenance efficiency
            return 0.8f; // Placeholder
        }
        
        private void IdentifySystemVulnerabilities()
        {
            // Identify system vulnerabilities
            systemHealth.SystemVulnerabilities.Clear();
            
            if (systemHealth.OverallSystemHealth < 0.7f)
                systemHealth.SystemVulnerabilities.Add("Low overall system health");
            
            if (systemHealth.CriticalEquipmentHealth < 0.8f)
                systemHealth.SystemVulnerabilities.Add("Critical equipment degradation");
        }
        
        private void StartDegradationProcessing()
        {
            // Start degradation processing
        }
        
        private void StopDegradationProcessing()
        {
            // Stop degradation processing
        }
        
        private void RegisterEventListeners()
        {
            // Register event listeners
        }
        
        private void UnregisterEventListeners()
        {
            // Unregister event listeners
        }
        
        private void SaveMaintenanceHistory()
        {
            // Save maintenance history
            Debug.Log($"[Phase 4.1.b] Maintenance history saved: {maintenanceHistory.Count} records");
        }
    }
}