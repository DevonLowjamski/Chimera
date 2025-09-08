using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Economy.Components
{
    /// <summary>
    /// Handles contract requirement validation, quality assessment, and compliance checking
    /// for Project Chimera's game economy system.
    /// </summary>
    public class ContractTrackingValidationService : MonoBehaviour
    {
        [Header("Validation Configuration")]
        [SerializeField] private bool _enableQualityValidation = true;
        [SerializeField] private bool _enableStrainValidation = true;
        [SerializeField] private float _minimumQualityThreshold = 0.5f;
        [SerializeField] private float _premiumQualityThreshold = 0.85f;
        [SerializeField] private bool _useDetailedQualityAnalysis = true;
        [SerializeField] private bool _enableQualityGrading = true;
        [SerializeField] private float _qualityVarianceTolerance = 0.1f;
        [SerializeField] private bool _useWeightedQualityScoring = true;

        [Header("Advanced Validation")]
        [SerializeField] private bool _enableBatchValidation = true;
        [SerializeField] private int _maxPlantsPerBatch = 50;
        [SerializeField] private bool _enableQualityConsistencyChecks = true;
        [SerializeField] private float _qualityConsistencyThreshold = 0.15f;
        [SerializeField] private bool _enableContractSpecificValidation = true;

        // Validation state
        private Dictionary<string, QualityAssessmentProfile> _qualityProfiles = new Dictionary<string, QualityAssessmentProfile>();
        private Dictionary<string, List<QualityValidationResult>> _validationHistory = new Dictionary<string, List<QualityValidationResult>>();
        private Queue<BatchValidationRequest> _validationQueue = new Queue<BatchValidationRequest>();

        // Events
        public event System.Action<string, QualityValidationResult> OnQualityValidationCompleted;
        public event System.Action<string, QualityGrade> OnQualityGradeAssigned;
        public event System.Action<string, float> OnQualityConsistencyAlert;

        // Properties
        public bool QualityValidationEnabled => _enableQualityValidation;
        public bool StrainValidationEnabled => _enableStrainValidation;
        public float MinimumQualityThreshold => _minimumQualityThreshold;

        public void Initialize()
        {
            LogInfo("Contract validation service initialized for game economy");
        }

        #region Contract Validation

        /// <summary>
        /// Validate that a contract meets basic requirements for fulfillment
        /// </summary>
        public ContractCompletionValidation ValidateContractCompletion(ContractProgress progress)
        {
            if (progress == null)
            {
                return new ContractCompletionValidation
                {
                    IsValid = false,
                    Reason = "Contract progress data is null"
                };
            }

            var validation = new ContractCompletionValidation
            {
                IsValid = true,
                ContractId = progress.ContractId
            };

            // Check quantity requirements
            if (progress.CurrentQuantity < progress.Contract.RequiredQuantity)
            {
                validation.IsValid = false;
                validation.Reason = $"Insufficient quantity: {progress.CurrentQuantity}/{progress.Contract.RequiredQuantity}";
                return validation;
            }

            // Check strain requirements
            if (_enableStrainValidation && !ValidateStrainRequirements(progress))
            {
                validation.IsValid = false;
                validation.Reason = "Strain requirements not met";
                return validation;
            }

            // Check quality requirements
            if (_enableQualityValidation && progress.AverageQuality.IsLessThanFloat(progress.Contract.MinimumQuality))
            {
                validation.IsValid = false;
                validation.Reason = $"Quality too low: {progress.AverageQuality.ToFloat():P1} (min: {progress.Contract.MinimumQuality:P1})";
                return validation;
            }

            validation.Reason = "Contract meets all requirements";
            LogInfo($"Contract {progress.ContractId} validation successful");
            
            return validation;
        }

        /// <summary>
        /// Perform advanced contract validation with comprehensive checks
        /// </summary>
        public AdvancedContractValidation ValidateContractAdvanced(ContractProgress progress, List<PlantProductionRecord> production)
        {
            var validation = new AdvancedContractValidation
            {
                ContractId = progress.ContractId,
                ValidationDate = DateTime.Now,
                IsValid = true
            };

            // Perform basic validation first
            var basicValidation = ValidateContractCompletion(progress);
            if (!basicValidation.IsValid)
            {
                validation.IsValid = false;
                validation.FailureReason = basicValidation.Reason;
                return validation;
            }

            // Perform quality assessment
            var qualityAssessment = PerformQualityAssessment(progress.ContractId, production, progress.Contract);
            if (!qualityAssessment.IsValid)
            {
                validation.IsValid = false;
                validation.FailureReason = qualityAssessment.ErrorMessage;
                return validation;
            }

            validation.QualityAssessment = qualityAssessment.QualityAssessment;
            validation.MeetsQualityStandards = qualityAssessment.MeetsRequirements;

            // Contract-specific validation
            if (_enableContractSpecificValidation)
            {
                validation.ContractSpecificChecks = PerformContractSpecificValidation(progress, qualityAssessment);
                validation.IsValid = validation.IsValid && validation.ContractSpecificChecks.All(c => c.Passed);
            }

            // Batch validation if enabled
            if (_enableBatchValidation)
            {
                validation.BatchValidation = PerformBatchValidation(progress.ContractId, production);
                validation.IsValid = validation.IsValid && validation.BatchValidation.IsValid;
            }

            return validation;
        }

        /// <summary>
        /// Check if a plant can fulfill contract requirements
        /// </summary>
        public bool CanPlantFulfillContract(PlantProductionRecord plant, ActiveContractSO contract)
        {
            if (plant == null || contract == null) return false;

            // Check strain type
            if (_enableStrainValidation && plant.StrainType != contract.RequiredStrain)
            {
                return false;
            }

            // Check quality threshold
            if (_enableQualityValidation && plant.Quality.IsLessThanFloat(contract.MinimumQuality))
            {
                return false;
            }

            // Check if plant is already allocated
            if (plant.IsAllocated)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Quality Assessment

        /// <summary>
        /// Perform comprehensive quality assessment for a contract
        /// </summary>
        public QualityAssessmentResult PerformQualityAssessment(string contractId, List<PlantProductionRecord> production, ActiveContractSO contract)
        {
            if (production == null || production.Count == 0)
            {
                return new QualityAssessmentResult
                {
                    IsValid = false,
                    ErrorMessage = "No production data available"
                };
            }

            var result = new QualityAssessmentResult
            {
                ContractId = contractId,
                AssessmentDate = DateTime.Now,
                IsValid = true
            };

            // Perform detailed quality analysis
            if (_useDetailedQualityAnalysis)
            {
                result = PerformDetailedQualityAnalysis(production, contract, result);
            }
            else
            {
                result = PerformBasicQualityAnalysis(production, contract, result);
            }

            // Assign quality grade
            if (_enableQualityGrading)
            {
                result.QualityGrade = CalculateQualityGrade(result);
                OnQualityGradeAssigned?.Invoke(contractId, result.QualityGrade);
            }

            // Check quality consistency
            if (_enableQualityConsistencyChecks)
            {
                result.QualityConsistency = CalculateQualityConsistency(production);
                if (result.QualityConsistency.Variance > _qualityConsistencyThreshold)
                {
                    OnQualityConsistencyAlert?.Invoke(contractId, result.QualityConsistency.Variance);
                }
            }

            // Store validation result
            StoreValidationResult(contractId, result);

            LogInfo($"Quality assessment completed for contract {contractId}: Grade {result.QualityGrade}, Quality {result.OverallQuality:P1}");

            return result;
        }

        /// <summary>
        /// Calculate expected quality score for a plant batch
        /// </summary>
        public float CalculateExpectedQualityScore(List<PlantProductionRecord> plants, ActiveContractSO contract)
        {
            if (plants.Count == 0) return 0f;

            float totalWeight = 0f;
            float weightedQuality = 0f;

            foreach (var plant in plants)
            {
                float weight = _useWeightedQualityScoring ? plant.Quantity : 1f;
                totalWeight += weight;
                weightedQuality += plant.Quality.ToFloat() * weight;
            }

            float baseQuality = totalWeight > 0 ? weightedQuality / totalWeight : 0f;

            // Apply contract-specific adjustments
            float contractMultiplier = CalculateContractQualityMultiplier(contract);

            return Mathf.Clamp01(baseQuality * contractMultiplier);
        }

        /// <summary>
        /// Get quality assessment history for a contract
        /// </summary>
        public List<QualityValidationResult> GetQualityValidationHistory(string contractId)
        {
            return _validationHistory.TryGetValue(contractId, out var history) 
                ? new List<QualityValidationResult>(history) 
                : new List<QualityValidationResult>();
        }

        /// <summary>
        /// Get quality profile for a specific strain type
        /// </summary>
        public QualityAssessmentProfile GetQualityProfile(StrainType strainType)
        {
            string key = strainType.ToString();
            return _qualityProfiles.TryGetValue(key, out var profile) ? profile : null;
        }

        #endregion

        #region Private Validation Methods

        private bool ValidateStrainRequirements(ContractProgress progress)
        {
            // Implementation for strain validation logic
            return progress.Contract.RequiredStrain != StrainType.None;
        }

        private QualityAssessmentResult PerformDetailedQualityAnalysis(List<PlantProductionRecord> production, ActiveContractSO contract, QualityAssessmentResult result)
        {
            // Advanced quality analysis implementation
            var qualityValues = production.Select(p => p.Quality.ToFloat()).ToList();
            float averageQualityFloat = qualityValues.Average();
            result.OverallQuality = QualityGradeExtensions.FromFloat(averageQualityFloat);
            result.MeetsRequirements = result.OverallQuality.IsGreaterThanOrEqual(QualityGradeExtensions.FromFloat(contract.MinimumQuality));

            // Calculate quality metrics
            var qualityGrades = production.Select(p => p.Quality).ToList();
            var qualityMetrics = new QualityMetrics
            {
                AverageQuality = result.OverallQuality,
                MinQuality = qualityGrades.Min(),
                MaxQuality = qualityGrades.Max(),
                StandardDeviation = qualityGrades.CalculateStandardDeviation()
            };
            
            result.QualityMetrics = new Dictionary<string, float>
            {
                ["AverageQuality"] = qualityMetrics.AverageQuality.ToFloat(),
                ["MinQuality"] = qualityMetrics.MinQuality.ToFloat(),
                ["MaxQuality"] = qualityMetrics.MaxQuality.ToFloat(),
                ["StandardDeviation"] = qualityMetrics.StandardDeviation
            };

            return result;
        }

        private QualityAssessmentResult PerformBasicQualityAnalysis(List<PlantProductionRecord> production, ActiveContractSO contract, QualityAssessmentResult result)
        {
            // Basic quality analysis implementation
            var qualityValues = production.Select(p => p.Quality.ToFloat()).ToList();
            float averageQualityFloat = qualityValues.Count > 0 ? qualityValues.Average() : 0f;
            result.OverallQuality = QualityGradeExtensions.FromFloat(averageQualityFloat);
            result.MeetsRequirements = result.OverallQuality.IsGreaterThanOrEqualFloat(contract.MinimumQuality);

            return result;
        }

        private QualityGrade CalculateQualityGrade(QualityAssessmentResult result)
        {
            if (result.OverallQuality.IsGreaterThanOrEqualFloat(_premiumQualityThreshold))
                return QualityGrade.Premium;
            else if (result.OverallQuality.IsGreaterThanOrEqualFloat(_minimumQualityThreshold))
                return QualityGrade.Standard;
            else
                return QualityGrade.Poor;
        }

        private QualityConsistency CalculateQualityConsistency(List<PlantProductionRecord> production)
        {
            var qualities = production.Select(p => p.Quality.ToFloat()).ToList();
            float variance = CalculateVariance(qualities);

            return new QualityConsistency
            {
                Variance = variance,
                StandardDeviation = Mathf.Sqrt(variance),
                IsConsistent = variance <= _qualityConsistencyThreshold
            };
        }

        private List<ContractSpecificCheck> PerformContractSpecificValidation(ContractProgress progress, QualityAssessmentResult qualityAssessment)
        {
            var checks = new List<ContractSpecificCheck>();

            // Add contract-specific validation logic
            checks.Add(new ContractSpecificCheck
            {
                CheckName = "Quality Threshold",
                Passed = qualityAssessment.OverallQuality.IsGreaterThanOrEqualFloat(progress.Contract.MinimumQuality),
                Details = $"Quality: {qualityAssessment.OverallQuality.ToFloat():P1}, Required: {progress.Contract.MinimumQuality:P1}"
            });

            return checks;
        }

        private BatchValidationResult PerformBatchValidation(string contractId, List<PlantProductionRecord> production)
        {
            var result = new BatchValidationResult
            {
                ContractId = contractId,
                IsValid = true,
                BatchSize = production.Count
            };

            // Check batch size limits
            if (production.Count > _maxPlantsPerBatch)
            {
                result.IsValid = false;
                result.Issues.Add($"Batch size exceeds limit: {production.Count}/{_maxPlantsPerBatch}");
            }

            return result;
        }

        private float CalculateContractQualityMultiplier(ActiveContractSO contract)
        {
            // Calculate quality multiplier based on contract requirements
            return 1.0f + (contract.MinimumQuality * 0.1f);
        }

        private void StoreValidationResult(string contractId, QualityAssessmentResult result)
        {
            if (!_validationHistory.ContainsKey(contractId))
            {
                _validationHistory[contractId] = new List<QualityValidationResult>();
            }

            var validation = new QualityValidationResult
            {
                ContractId = contractId,
                ValidationDate = DateTime.Now,
                OverallQuality = result.OverallQuality,
                QualityGrade = result.QualityGrade,
                MeetsRequirements = result.MeetsRequirements,
                Issues = result.QualityNotes
            };

            _validationHistory[contractId].Add(validation);
            OnQualityValidationCompleted?.Invoke(contractId, validation);
        }

        private float CalculateStandardDeviation(List<float> values)
        {
            if (values.Count == 0) return 0f;

            float average = values.Average();
            float sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            return Mathf.Sqrt(sumOfSquaresOfDifferences / values.Count);
        }

        private float CalculateVariance(List<float> values)
        {
            if (values.Count == 0) return 0f;

            float average = values.Average();
            return values.Select(val => (val - average) * (val - average)).Average();
        }

        #endregion

        private void LogInfo(string message)
        {
            ChimeraLogger.Log($"[ContractValidationService] {message}");
        }
    }
}