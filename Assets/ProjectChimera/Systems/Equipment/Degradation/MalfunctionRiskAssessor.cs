using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// REFACTORED: Malfunction Risk Assessor
    /// Single Responsibility: Evaluating malfunction risks and providing risk assessments
    /// Extracted from MalfunctionSystem for better separation of concerns
    /// </summary>
    public class MalfunctionRiskAssessor : MonoBehaviour
    {
        [Header("Risk Assessment Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _wearRiskMultiplier = 0.005f;
        [SerializeField] private float _environmentalRiskMultiplier = 0.003f;
        [SerializeField] private float _maintenanceRiskMultiplier = 0.002f;

        [Header("Risk Thresholds")]
        [SerializeField] private float _lowRiskThreshold = 0.001f;
        [SerializeField] private float _mediumRiskThreshold = 0.003f;
        [SerializeField] private float _highRiskThreshold = 0.008f;

        // Risk assessment cache
        private readonly Dictionary<string, MalfunctionRiskAssessment> _riskAssessmentCache = new Dictionary<string, MalfunctionRiskAssessment>();
        private readonly Dictionary<string, float> _lastAssessmentTime = new Dictionary<string, float>();

        // Statistics
        private MalfunctionRiskAssessorStats _stats = new MalfunctionRiskAssessorStats();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event System.Action<MalfunctionRiskAssessment> OnRiskAssessmentCompleted;
        public event System.Action<string, RiskLevel> OnRiskLevelChanged;

        public bool IsInitialized => _isInitialized;
        public MalfunctionRiskAssessorStats Stats => _stats;

        public void Initialize()
        {
            if (_isInitialized) return;

            _riskAssessmentCache.Clear();
            _lastAssessmentTime.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Malfunction Risk Assessor initialized", this);
            }
        }

        /// <summary>
        /// Evaluates equipment for potential malfunctions based on wear and conditions
        /// </summary>
        public MalfunctionRiskAssessment EvaluateMalfunctionRisk(
            string equipmentId,
            EquipmentType equipmentType,
            float wearLevel,
            EnvironmentalStressFactors stressFactors)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("EQUIPMENT", "Cannot evaluate risk - assessor not initialized", this);
                }
                return CreateDefaultRiskAssessment(equipmentId);
            }

            var assessmentStartTime = Time.realtimeSinceStartup;

            try
            {
                var profile = EquipmentTypes.GetReliabilityProfile(equipmentType);
                if (profile == null)
                {
                    _stats.AssessmentsWithoutProfile++;
                    return CreateDefaultRiskAssessment(equipmentId);
                }

                // Calculate component risks
                float wearRisk = CalculateWearRisk(wearLevel);
                float environmentalRisk = CalculateEnvironmentalRisk(equipmentType, stressFactors);
                float maintenanceRisk = CalculateMaintenanceRisk(equipmentId);

                float totalRisk = wearRisk + environmentalRisk + maintenanceRisk;

                // Determine risk level
                var riskLevel = DetermineRiskLevel(totalRisk);

                // Determine most likely malfunction type
                var mostLikelyMalfunction = SelectMostLikelyMalfunction(profile, wearLevel, stressFactors);

                // Create assessment
                var assessment = new MalfunctionRiskAssessment
                {
                    EquipmentId = equipmentId,
                    OverallRisk = totalRisk,
                    RiskLevel = riskLevel,
                    MostLikelyMalfunction = mostLikelyMalfunction,
                    ContributingFactors = IdentifyContributingFactors(wearLevel, stressFactors),
                    RecommendedActions = GenerateRiskMitigationActions(riskLevel, mostLikelyMalfunction),
                    RiskComponents = new RiskComponents
                    {
                        WearRisk = wearRisk,
                        EnvironmentalRisk = environmentalRisk,
                        MaintenanceRisk = maintenanceRisk
                    },
                    AssessmentTime = Time.time
                };

                // Update cache and tracking
                UpdateRiskCache(equipmentId, assessment);

                // Update statistics
                var assessmentTime = Time.realtimeSinceStartup - assessmentStartTime;
                UpdateAssessmentStats(assessmentTime, riskLevel);

                OnRiskAssessmentCompleted?.Invoke(assessment);

                return assessment;
            }
            catch (System.Exception ex)
            {
                _stats.AssessmentErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error evaluating risk for {equipmentId}: {ex.Message}", this);
                }

                return CreateDefaultRiskAssessment(equipmentId);
            }
        }

        /// <summary>
        /// Get cached risk assessment for equipment
        /// </summary>
        public MalfunctionRiskAssessment GetCachedRiskAssessment(string equipmentId)
        {
            return _riskAssessmentCache.TryGetValue(equipmentId, out var assessment) ? assessment : null;
        }

        /// <summary>
        /// Check if risk assessment is recent
        /// </summary>
        public bool IsRiskAssessmentRecent(string equipmentId, float maxAgeSeconds = 300f)
        {
            if (!_lastAssessmentTime.TryGetValue(equipmentId, out var lastTime))
                return false;

            return Time.time - lastTime < maxAgeSeconds;
        }

        /// <summary>
        /// Get all equipment with critical risk level
        /// </summary>
        public List<string> GetCriticalRiskEquipment()
        {
            return _riskAssessmentCache
                .Where(pair => pair.Value.RiskLevel == RiskLevel.Critical)
                .Select(pair => pair.Key)
                .ToList();
        }

        /// <summary>
        /// Set risk assessment parameters
        /// </summary>
        public void SetRiskParameters(float wearMultiplier, float environmentalMultiplier, float maintenanceMultiplier)
        {
            _wearRiskMultiplier = Mathf.Max(0f, wearMultiplier);
            _environmentalRiskMultiplier = Mathf.Max(0f, environmentalMultiplier);
            _maintenanceRiskMultiplier = Mathf.Max(0f, maintenanceMultiplier);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Risk parameters updated: Wear={_wearRiskMultiplier:F3}, Env={_environmentalRiskMultiplier:F3}, Maint={_maintenanceRiskMultiplier:F3}", this);
            }
        }

        /// <summary>
        /// Set risk level thresholds
        /// </summary>
        public void SetRiskThresholds(float low, float medium, float high)
        {
            _lowRiskThreshold = Mathf.Max(0f, low);
            _mediumRiskThreshold = Mathf.Max(_lowRiskThreshold, medium);
            _highRiskThreshold = Mathf.Max(_mediumRiskThreshold, high);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Risk thresholds updated: Low={_lowRiskThreshold:F3}, Med={_mediumRiskThreshold:F3}, High={_highRiskThreshold:F3}", this);
            }
        }

        #region Private Methods

        /// <summary>
        /// Calculate wear-based risk
        /// </summary>
        private float CalculateWearRisk(float wearLevel)
        {
            return Mathf.Clamp01(wearLevel) * _wearRiskMultiplier;
        }

        /// <summary>
        /// Calculate environmental risk
        /// </summary>
        private float CalculateEnvironmentalRisk(EquipmentType equipmentType, EnvironmentalStressFactors stressFactors)
        {
            var profile = EquipmentTypes.GetReliabilityProfile(equipmentType);
            if (profile?.EnvironmentalSensitivity == null) return 0f;

            float totalRisk = 0f;
            float totalSensitivity = 0f;

            // Temperature stress
            if (profile.EnvironmentalSensitivity.TryGetValue("Temperature", out float tempSensitivity))
            {
                totalRisk += stressFactors.TemperatureStress * tempSensitivity;
                totalSensitivity += tempSensitivity;
            }

            // Humidity stress
            if (profile.EnvironmentalSensitivity.TryGetValue("Humidity", out float humiditySensitivity))
            {
                totalRisk += stressFactors.HumidityStress * humiditySensitivity;
                totalSensitivity += humiditySensitivity;
            }

            // Dust stress
            if (profile.EnvironmentalSensitivity.TryGetValue("Dust", out float dustSensitivity))
            {
                totalRisk += stressFactors.DustAccumulation * dustSensitivity;
                totalSensitivity += dustSensitivity;
            }

            // Electrical stress
            if (profile.EnvironmentalSensitivity.TryGetValue("Electrical", out float electricalSensitivity))
            {
                totalRisk += stressFactors.ElectricalStress * electricalSensitivity;
                totalSensitivity += electricalSensitivity;
            }

            float normalizedRisk = totalSensitivity > 0 ? totalRisk / totalSensitivity : totalRisk;
            return normalizedRisk * _environmentalRiskMultiplier;
        }

        /// <summary>
        /// Calculate maintenance-based risk
        /// </summary>
        private float CalculateMaintenanceRisk(string equipmentId)
        {
            var equipment = EquipmentInstance.GetEquipment(equipmentId);
            if (equipment == null) return 0f;

            var timeSinceMaintenance = System.DateTime.Now - equipment.LastMaintenance;
            float daysSinceMaintenance = (float)timeSinceMaintenance.TotalDays;

            // Risk increases with time since maintenance (normalized to 180 days)
            float normalizedDays = Mathf.Clamp01(daysSinceMaintenance / 180f);
            return normalizedDays * _maintenanceRiskMultiplier;
        }

        /// <summary>
        /// Determine risk level based on total risk
        /// </summary>
        private RiskLevel DetermineRiskLevel(float totalRisk)
        {
            if (totalRisk < _lowRiskThreshold) return RiskLevel.Low;
            if (totalRisk < _mediumRiskThreshold) return RiskLevel.Medium;
            if (totalRisk < _highRiskThreshold) return RiskLevel.High;
            return RiskLevel.Critical;
        }

        /// <summary>
        /// Select most likely malfunction type
        /// </summary>
        private MalfunctionType SelectMostLikelyMalfunction(EquipmentReliabilityProfile profile, float wearLevel, EnvironmentalStressFactors stressFactors)
        {
            if (profile.CommonFailureModes == null || profile.CommonFailureModes.Count == 0)
                return MalfunctionType.WearAndTear;

            // Select based on weighted probabilities
            float totalWeight = profile.CommonFailureModes.Values.Sum();
            if (totalWeight <= 0f) return MalfunctionType.WearAndTear;

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var failureMode in profile.CommonFailureModes)
            {
                cumulative += failureMode.Value;
                if (random <= cumulative)
                {
                    return failureMode.Key;
                }
            }

            return MalfunctionType.WearAndTear;
        }

        /// <summary>
        /// Identify contributing factors to risk
        /// </summary>
        private List<string> IdentifyContributingFactors(float wearLevel, EnvironmentalStressFactors stressFactors)
        {
            var factors = new List<string>();

            if (wearLevel > 0.7f)
                factors.Add("High equipment wear");

            if (stressFactors.TemperatureStress > 0.5f)
                factors.Add("Temperature stress");

            if (stressFactors.HumidityStress > 0.5f)
                factors.Add("Humidity stress");

            if (stressFactors.DustAccumulation > 0.5f)
                factors.Add("Dust accumulation");

            if (stressFactors.ElectricalStress > 0.5f)
                factors.Add("Electrical stress");

            if (factors.Count == 0)
                factors.Add("Normal operating conditions");

            return factors;
        }

        /// <summary>
        /// Generate risk mitigation actions
        /// </summary>
        private List<string> GenerateRiskMitigationActions(RiskLevel riskLevel, MalfunctionType malfunctionType)
        {
            var actions = new List<string>();

            // Base actions by risk level
            switch (riskLevel)
            {
                case RiskLevel.Critical:
                    actions.Add("Immediate maintenance scheduling");
                    actions.Add("Monitor equipment closely");
                    actions.Add("Prepare for potential downtime");
                    break;
                case RiskLevel.High:
                    actions.Add("Schedule maintenance within 7 days");
                    actions.Add("Increase monitoring frequency");
                    actions.Add("Prepare replacement parts");
                    break;
                case RiskLevel.Medium:
                    actions.Add("Schedule maintenance within 30 days");
                    actions.Add("Review maintenance history");
                    actions.Add("Monitor key parameters");
                    break;
                case RiskLevel.Low:
                    actions.Add("Continue regular maintenance schedule");
                    actions.Add("Monitor trends");
                    break;
            }

            // Additional actions by malfunction type
            switch (malfunctionType)
            {
                case MalfunctionType.OverheatingProblem:
                    actions.Add("Check cooling systems");
                    actions.Add("Verify ventilation");
                    break;
                case MalfunctionType.ElectricalFailure:
                    actions.Add("Inspect electrical connections");
                    actions.Add("Check power quality");
                    break;
                case MalfunctionType.MechanicalFailure:
                    actions.Add("Lubricate moving parts");
                    actions.Add("Check for misalignment");
                    break;
                case MalfunctionType.SensorDrift:
                    actions.Add("Recalibrate sensors");
                    actions.Add("Verify sensor readings");
                    break;
            }

            return actions;
        }

        /// <summary>
        /// Create default risk assessment for unknown equipment
        /// </summary>
        private MalfunctionRiskAssessment CreateDefaultRiskAssessment(string equipmentId)
        {
            return new MalfunctionRiskAssessment
            {
                EquipmentId = equipmentId,
                OverallRisk = 0f,
                RiskLevel = RiskLevel.Low,
                MostLikelyMalfunction = MalfunctionType.WearAndTear,
                ContributingFactors = new List<string> { "Unknown equipment profile" },
                RecommendedActions = new List<string> { "Establish equipment profile", "Schedule inspection" },
                RiskComponents = new RiskComponents(),
                AssessmentTime = Time.time
            };
        }

        /// <summary>
        /// Update risk assessment cache
        /// </summary>
        private void UpdateRiskCache(string equipmentId, MalfunctionRiskAssessment assessment)
        {
            var previousLevel = _riskAssessmentCache.TryGetValue(equipmentId, out var previous) ? previous.RiskLevel : RiskLevel.Low;

            _riskAssessmentCache[equipmentId] = assessment;
            _lastAssessmentTime[equipmentId] = Time.time;

            // Fire risk level change event if changed
            if (assessment.RiskLevel != previousLevel)
            {
                OnRiskLevelChanged?.Invoke(equipmentId, assessment.RiskLevel);
            }
        }

        /// <summary>
        /// Update assessment statistics
        /// </summary>
        private void UpdateAssessmentStats(float assessmentTime, RiskLevel riskLevel)
        {
            _stats.TotalAssessments++;
            _stats.TotalAssessmentTime += assessmentTime;
            _stats.AverageAssessmentTime = _stats.TotalAssessmentTime / _stats.TotalAssessments;

            if (assessmentTime > _stats.MaxAssessmentTime)
                _stats.MaxAssessmentTime = assessmentTime;

            switch (riskLevel)
            {
                case RiskLevel.Low:
                    _stats.LowRiskAssessments++;
                    break;
                case RiskLevel.Medium:
                    _stats.MediumRiskAssessments++;
                    break;
                case RiskLevel.High:
                    _stats.HighRiskAssessments++;
                    break;
                case RiskLevel.Critical:
                    _stats.CriticalRiskAssessments++;
                    break;
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new MalfunctionRiskAssessorStats
            {
                TotalAssessments = 0,
                LowRiskAssessments = 0,
                MediumRiskAssessments = 0,
                HighRiskAssessments = 0,
                CriticalRiskAssessments = 0,
                AssessmentsWithoutProfile = 0,
                AssessmentErrors = 0,
                TotalAssessmentTime = 0f,
                AverageAssessmentTime = 0f,
                MaxAssessmentTime = 0f
            };
        }

        #endregion
    }
}
