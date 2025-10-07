using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Malfunction Generator (Coordinator)
    /// Single Responsibility: Orchestrate malfunction generation components and manage lifecycle
    /// Refactored from 717 lines → 291 lines (4 files total, all <500 lines)
    /// Dependencies: MalfunctionTypeSelector, MalfunctionRecordFactory
    /// </summary>
    public class MalfunctionGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _useRealisticGeneration = true;
        [SerializeField] private float _severityVariabilityFactor = 0.2f;
        [SerializeField] private bool _generateDetailedSymptoms = true;

        [Header("Severity Modifiers")]
        [SerializeField] private float _wearSeverityModifier = 0.3f;
        [SerializeField] private float _stressSeverityModifier = 0.2f;
        [SerializeField] private float _randomSeverityVariance = 0.1f;

        // Component dependencies
        private MalfunctionTypeSelector _typeSelector;
        private MalfunctionRecordFactory _recordFactory;
        private MalfunctionGenerationParameters _parameters;

        // Generation tracking
        private readonly Dictionary<string, EquipmentMalfunction> _generatedMalfunctions = new Dictionary<string, EquipmentMalfunction>();
        private readonly List<string> _malfunctionHistory = new List<string>();

        // Statistics
        private MalfunctionGeneratorStats _stats = MalfunctionGeneratorStats.CreateEmpty();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event Action<EquipmentMalfunction> OnMalfunctionGenerated;
        public event Action<string, MalfunctionType> OnMalfunctionTypeSelected;

        public bool IsInitialized => _isInitialized;
        public MalfunctionGeneratorStats Stats => _stats;
        public int ActiveMalfunctionCount => _generatedMalfunctions.Count;

        /// <summary>
        /// Initialize generator and components
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Build generation parameters
            _parameters = new MalfunctionGenerationParameters
            {
                SeverityVariabilityFactor = _severityVariabilityFactor,
                WearSeverityModifier = _wearSeverityModifier,
                StressSeverityModifier = _stressSeverityModifier,
                RandomSeverityVariance = _randomSeverityVariance,
                GenerateDetailedSymptoms = _generateDetailedSymptoms,
                UseRealisticGeneration = _useRealisticGeneration
            };

            // Initialize components
            _typeSelector = new MalfunctionTypeSelector(_parameters, _enableLogging);
            _recordFactory = new MalfunctionRecordFactory(_parameters, _enableLogging);

            _generatedMalfunctions.Clear();
            _malfunctionHistory.Clear();
            _stats = MalfunctionGeneratorStats.CreateEmpty();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Malfunction Generator initialized", this);
            }
        }

        #region Generation

        /// <summary>
        /// Generate realistic malfunction for equipment
        /// </summary>
        public EquipmentMalfunction GenerateMalfunction(
            string equipmentId,
            EquipmentType equipmentType,
            float wearLevel,
            EnvironmentalStressFactors stressFactors,
            OperationalStatus currentStatus)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("EQUIPMENT", "Cannot generate malfunction - generator not initialized", this);
                }
                return null;
            }

            var generationStartTime = Time.realtimeSinceStartup;

            try
            {
                var profile = EquipmentTypes.GetReliabilityProfile(equipmentType);
                if (profile == null)
                {
                    _stats.GenerationsWithoutProfile++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogWarning("EQUIPMENT", $"No reliability profile found for {equipmentType}, using default malfunction", this);
                    }
                    return GenerateDefaultMalfunction(equipmentId, equipmentType);
                }

                // Select malfunction type using type selector component
                var malfunctionType = _typeSelector.SelectMalfunctionType(profile, wearLevel, stressFactors);
                OnMalfunctionTypeSelected?.Invoke(equipmentId, malfunctionType);

                // Determine severity using type selector component
                var severity = _typeSelector.DetermineMalfunctionSeverity(equipmentType, wearLevel, stressFactors, currentStatus);

                // Create malfunction record using record factory component
                var malfunction = _recordFactory.CreateMalfunctionRecord(
                    equipmentId,
                    equipmentType,
                    malfunctionType,
                    severity,
                    wearLevel,
                    stressFactors);

                // Register malfunction
                RegisterMalfunction(malfunction);

                // Update statistics
                var generationTime = Time.realtimeSinceStartup - generationStartTime;
                UpdateGenerationStats(generationTime, malfunctionType, severity);

                OnMalfunctionGenerated?.Invoke(malfunction);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT", $"Generated {severity} {malfunctionType} malfunction {malfunction.MalfunctionId} for {equipmentId}", this);
                }

                return malfunction;
            }
            catch (Exception ex)
            {
                _stats.GenerationErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error generating malfunction for {equipmentId}: {ex.Message}", this);
                }

                return null;
            }
        }

        /// <summary>
        /// Generate malfunction from risk assessment
        /// </summary>
        public EquipmentMalfunction GenerateFromRiskAssessment(
            MalfunctionRiskAssessment riskAssessment,
            EquipmentType equipmentType,
            float wearLevel,
            EnvironmentalStressFactors stressFactors)
        {
            if (!_isInitialized || riskAssessment == null)
                return null;

            // Force the malfunction type from risk assessment
            var severity = _typeSelector.ConvertRiskToSeverity(riskAssessment.RiskLevel);

            var malfunction = _recordFactory.CreateMalfunctionRecord(
                riskAssessment.EquipmentId,
                equipmentType,
                riskAssessment.MostLikelyMalfunction,
                severity,
                wearLevel,
                stressFactors);

            RegisterMalfunction(malfunction);
            OnMalfunctionGenerated?.Invoke(malfunction);

            return malfunction;
        }

        /// <summary>
        /// Generate default malfunction when profile unavailable
        /// </summary>
        private EquipmentMalfunction GenerateDefaultMalfunction(string equipmentId, EquipmentType equipmentType)
        {
            var malfunction = _recordFactory.CreateDefaultMalfunction(equipmentId, equipmentType);
            RegisterMalfunction(malfunction);
            return malfunction;
        }

        #endregion

        #region Malfunction Management

        /// <summary>
        /// Register generated malfunction
        /// </summary>
        private void RegisterMalfunction(EquipmentMalfunction malfunction)
        {
            if (malfunction == null) return;

            _generatedMalfunctions[malfunction.MalfunctionId] = malfunction;
            _malfunctionHistory.Add(malfunction.MalfunctionId);

            // Keep history limited to last 100 entries
            if (_malfunctionHistory.Count > 100)
            {
                _malfunctionHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get generated malfunction by ID
        /// </summary>
        public EquipmentMalfunction GetMalfunction(string malfunctionId)
        {
            return _generatedMalfunctions.TryGetValue(malfunctionId, out var malfunction) ? malfunction : null;
        }

        /// <summary>
        /// Get all active malfunctions
        /// </summary>
        public List<EquipmentMalfunction> GetActiveMalfunctions()
        {
            return _generatedMalfunctions.Values.ToList();
        }

        /// <summary>
        /// Get malfunctions for specific equipment
        /// </summary>
        public List<EquipmentMalfunction> GetMalfunctionsForEquipment(string equipmentId)
        {
            return _generatedMalfunctions.Values
                .Where(m => m.EquipmentId == equipmentId)
                .ToList();
        }

        /// <summary>
        /// Remove malfunction (e.g., after repair)
        /// </summary>
        public bool RemoveMalfunction(string malfunctionId)
        {
            if (_generatedMalfunctions.Remove(malfunctionId))
            {
                _stats.MalfunctionsRemoved++;
                return true;
            }
            return false;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Set generation parameters
        /// </summary>
        public void SetGenerationParameters(float severityVariability, float wearModifier, float stressModifier)
        {
            _severityVariabilityFactor = Mathf.Clamp01(severityVariability);
            _wearSeverityModifier = Mathf.Clamp01(wearModifier);
            _stressSeverityModifier = Mathf.Clamp01(stressModifier);

            // Rebuild parameters and components
            _parameters.SeverityVariabilityFactor = _severityVariabilityFactor;
            _parameters.WearSeverityModifier = _wearSeverityModifier;
            _parameters.StressSeverityModifier = _stressSeverityModifier;

            _typeSelector = new MalfunctionTypeSelector(_parameters, _enableLogging);
            _recordFactory = new MalfunctionRecordFactory(_parameters, _enableLogging);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Generation parameters updated: Variability={_severityVariabilityFactor:F2}, Wear={_wearSeverityModifier:F2}, Stress={_stressSeverityModifier:F2}", this);
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Update generation statistics
        /// </summary>
        private void UpdateGenerationStats(float generationTime, MalfunctionType type, MalfunctionSeverity severity)
        {
            _stats.TotalMalfunctionsGenerated++;
            _stats.TotalGenerationTime += generationTime;
            _stats.MaxGenerationTime = Mathf.Max(_stats.MaxGenerationTime, generationTime);
            _stats.AverageGenerationTime = _stats.TotalGenerationTime / _stats.TotalMalfunctionsGenerated;

            // Update type counters
            switch (type)
            {
                case MalfunctionType.MechanicalFailure:
                    _stats.MechanicalFailures++;
                    break;
                case MalfunctionType.ElectricalFailure:
                    _stats.ElectricalFailures++;
                    break;
                case MalfunctionType.SensorDrift:
                    _stats.SensorFailures++;
                    break;
                case MalfunctionType.OverheatingProblem:
                    _stats.OverheatingIssues++;
                    break;
                case MalfunctionType.SoftwareError:
                    _stats.SoftwareErrors++;
                    break;
            }

            // Update severity counters
            switch (severity)
            {
                case MalfunctionSeverity.Minor:
                    _stats.MinorSeverity++;
                    break;
                case MalfunctionSeverity.Moderate:
                    _stats.ModerateSeverity++;
                    break;
                case MalfunctionSeverity.Major:
                    _stats.MajorSeverity++;
                    break;
                case MalfunctionSeverity.Critical:
                    _stats.CriticalSeverity++;
                    break;
                case MalfunctionSeverity.Catastrophic:
                    _stats.CatastrophicSeverity++;
                    break;
            }
        }

        #endregion
    }
}

