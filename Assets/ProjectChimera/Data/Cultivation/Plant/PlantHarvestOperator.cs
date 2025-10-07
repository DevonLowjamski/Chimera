using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Harvest Operator Coordinator
    /// Single Responsibility: Orchestrate harvest operations
    /// BEFORE: 785 lines (massive SRP violation)
    /// AFTER: 4 files <500 lines each (HarvestDataStructures, HarvestReadinessCalculator, HarvestExecutionHandler, this coordinator)
    /// </summary>
    [Serializable]
    public class PlantHarvestOperator
    {
        [Header("Harvest Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableQualityAssessment = true;
        [SerializeField] private bool _enablePostHarvestTracking = true;
        [SerializeField] private float _harvestReadinessThreshold = 0.85f;

        [Header("Harvest Timing Parameters")]
        [SerializeField] private float _optimalHarvestWindow = 7f;
        [SerializeField] private float _trichomeReadinessWeight = 0.4f;
        [SerializeField] private float _maturityReadinessWeight = 0.35f;
        [SerializeField] private float _environmentalReadinessWeight = 0.25f;

        [Header("Yield Parameters")]
        [SerializeField] private float _environmentalYieldModifier = 1f;
        [SerializeField] private float _geneticYieldModifier = 1f;
        [SerializeField] private float _careQualityModifier = 1f;

        [Header("Post-Harvest")]
        [SerializeField] private PostHarvestMethod _preferredMethod = PostHarvestMethod.StandardDrying;
        [SerializeField] private float _optimalDryingTemperature = 20f;
        [SerializeField] private float _optimalDryingHumidity = 55f;
        [SerializeField] private float _curingDuration = 14f;

        // PHASE 0: Component-based architecture (SRP)
        private HarvestReadinessCalculator _calculator;
        private HarvestExecutionHandler _executionHandler;

        // Current readiness state
        [SerializeField, Range(0f, 1f)] private float _trichomeReadiness = 0f;
        [SerializeField, Range(0f, 1f)] private float _pistilReadiness = 0f;
        [SerializeField, Range(0f, 1f)] private float _calyxSwelling = 0f;
        [SerializeField, Range(0f, 1f)] private float _overallMaturityScore = 0f;

        [SerializeField] private float _estimatedPotency = 0f;
        [SerializeField] private float _estimatedYield = 0f;
        [SerializeField] private DateTime _optimalHarvestDate = DateTime.MinValue;
        [SerializeField] private DateTime _harvestWindowStart = DateTime.MinValue;
        [SerializeField] private DateTime _harvestWindowEnd = DateTime.MinValue;

        // Statistics
        private PlantHarvestStats _stats;
        private bool _isInitialized = false;

        // Events
        public event Action<float> OnReadinessChanged;
        public event Action<DateTime, DateTime> OnHarvestWindowCalculated;
        public event Action<float, float> OnYieldEstimateUpdated;
        public event Action<HarvestExecutionResult> OnHarvestCompleted;
        public event Action<HarvestRecommendation> OnHarvestRecommendationUpdated;
        public event Action<string> OnHarvestWarning;

        // Public properties
        public bool IsInitialized => _isInitialized;
        public PlantHarvestStats Stats => _stats;
        public bool IsHarvested => _executionHandler?.IsHarvested ?? false;
        public float TrichomeReadiness => _trichomeReadiness;
        public float PistilReadiness => _pistilReadiness;
        public float CalyxSwelling => _calyxSwelling;
        public float OverallMaturityScore => _overallMaturityScore;
        public float EstimatedPotency => _estimatedPotency;
        public float EstimatedYield => _estimatedYield;
        public DateTime OptimalHarvestDate => _optimalHarvestDate;
        public DateTime HarvestWindowStart => _harvestWindowStart;
        public DateTime HarvestWindowEnd => _harvestWindowEnd;
        public bool IsInOptimalHarvestWindow => DateTime.Now >= _harvestWindowStart && DateTime.Now <= _harvestWindowEnd;

        /// <summary>
        /// Initialize harvest operator
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize components
            _calculator = new HarvestReadinessCalculator();
            _calculator.SetParameters(
                _harvestReadinessThreshold,
                _trichomeReadinessWeight,
                _maturityReadinessWeight,
                _environmentalReadinessWeight,
                _optimalHarvestWindow
            );
            _calculator.SetYieldModifiers(
                _environmentalYieldModifier,
                _geneticYieldModifier,
                _careQualityModifier
            );

            _executionHandler = new HarvestExecutionHandler(
                _calculator,
                _enableLogging,
                _enableQualityAssessment,
                _enablePostHarvestTracking
            );
            _executionHandler.SetPostHarvestParameters(
                _preferredMethod,
                _optimalDryingTemperature,
                _optimalDryingHumidity,
                _curingDuration
            );

            _stats = PlantHarvestStats.CreateEmpty();
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT",
                    $"Plant Harvest Operator initialized - Readiness Threshold: {_harvestReadinessThreshold:F2}",
                    null);
            }
        }

        /// <summary>
        /// Update harvest readiness assessment
        /// </summary>
        public HarvestReadinessResult UpdateHarvestReadiness(
            float plantAge,
            float maturityLevel,
            float biomass,
            float healthFactor)
        {
            if (!_isInitialized) Initialize();

            var startTime = DateTime.Now;
            var oldReadiness = _overallMaturityScore;

            // Calculate readiness factors
            var factors = _calculator.CalculateReadinessFactors(plantAge, maturityLevel, biomass, healthFactor);
            _trichomeReadiness = factors.TrichomeReadiness;
            _pistilReadiness = factors.PistilReadiness;
            _calyxSwelling = factors.CalyxSwelling;

            // Calculate overall readiness
            _overallMaturityScore = _calculator.CalculateOverallReadiness(factors);

            // Update estimates
            var oldYield = _estimatedYield;
            _estimatedYield = _calculator.CalculateYieldPotential(biomass, healthFactor, maturityLevel);
            _estimatedPotency = _calculator.CalculatePotencyPotential(maturityLevel, healthFactor);

            // Calculate optimal harvest window
            _optimalHarvestDate = _calculator.CalculateOptimalHarvestDate(plantAge, maturityLevel);
            (_harvestWindowStart, _harvestWindowEnd) = _calculator.CalculateHarvestWindow(_optimalHarvestDate);

            var readinessFactors = new Dictionary<string, float>
            {
                { "Trichome", _trichomeReadiness * _trichomeReadinessWeight },
                { "Maturity", maturityLevel * _maturityReadinessWeight },
                { "Environmental", healthFactor * _environmentalReadinessWeight }
            };

            var result = new HarvestReadinessResult
            {
                OverallReadiness = _overallMaturityScore,
                IsReady = _calculator.IsReadyForHarvest(_overallMaturityScore),
                ReadinessFactors = readinessFactors,
                EstimatedYield = _estimatedYield,
                EstimatedPotency = _estimatedPotency,
                OptimalHarvestDate = _optimalHarvestDate,
                DaysUntilOptimal = (float)(_optimalHarvestDate - DateTime.Now).TotalDays,
                CalculationTime = (float)(DateTime.Now - startTime).TotalMilliseconds
            };

            _stats.ReadinessChecks++;

            // Fire events
            if (Mathf.Abs(_overallMaturityScore - oldReadiness) > 0.01f)
            {
                OnReadinessChanged?.Invoke(_overallMaturityScore);
            }

            if (Mathf.Abs(_estimatedYield - oldYield) > 0.5f)
            {
                OnYieldEstimateUpdated?.Invoke(oldYield, _estimatedYield);
            }

            OnHarvestWindowCalculated?.Invoke(_harvestWindowStart, _harvestWindowEnd);

            return result;
        }

        /// <summary>
        /// Check harvest readiness
        /// </summary>
        public HarvestRecommendation CheckHarvestReadiness()
        {
            if (!_isInitialized) Initialize();

            var recommendation = new HarvestRecommendation
            {
                ReadinessScore = _overallMaturityScore,
                IsReady = _calculator.IsReadyForHarvest(_overallMaturityScore),
                OptimalHarvestDate = _optimalHarvestDate,
                HarvestWindowStart = _harvestWindowStart,
                HarvestWindowEnd = _harvestWindowEnd,
                EstimatedYield = _estimatedYield,
                EstimatedPotency = _estimatedPotency,
                RecommendedMethod = _preferredMethod.ToString(),
                QualityPrediction = _calculator.PredictHarvestQuality(_overallMaturityScore),
                IsInOptimalWindow = IsInOptimalHarvestWindow
            };

            return recommendation;
        }

        /// <summary>
        /// Perform harvest operation
        /// </summary>
        public HarvestExecutionResult Harvest(string harvestMethod = "Standard")
        {
            if (!_isInitialized) Initialize();

            _stats.HarvestAttempts++;

            var result = _executionHandler.ExecuteHarvest(
                harvestMethod,
                _overallMaturityScore,
                _estimatedYield,
                _estimatedPotency,
                IsInOptimalHarvestWindow
            );

            if (result.Success)
            {
                _stats.HarvestsCompleted++;
                OnHarvestCompleted?.Invoke(result);
            }

            return result;
        }

        /// <summary>
        /// Calculate yield potential
        /// </summary>
        public float CalculateYieldPotential(float biomass, float healthFactor = 1f, float maturityLevel = 0f)
        {
            if (!_isInitialized) Initialize();

            if (maturityLevel == 0f) maturityLevel = _overallMaturityScore;

            _stats.YieldCalculations++;
            return _calculator.CalculateYieldPotential(biomass, healthFactor, maturityLevel);
        }

        /// <summary>
        /// Calculate potency potential
        /// </summary>
        public float CalculatePotencyPotential(float maturityLevel = 0f, float healthFactor = 1f)
        {
            if (!_isInitialized) Initialize();

            if (maturityLevel == 0f) maturityLevel = _overallMaturityScore;

            _stats.PotencyCalculations++;
            return _calculator.CalculatePotencyPotential(maturityLevel, healthFactor);
        }

        /// <summary>
        /// Get harvest recommendations
        /// </summary>
        public HarvestRecommendation GetHarvestRecommendations()
        {
            var recommendation = CheckHarvestReadiness();
            OnHarvestRecommendationUpdated?.Invoke(recommendation);
            return recommendation;
        }

        /// <summary>
        /// Get post-harvest processing recommendations
        /// </summary>
        public PostHarvestProcessDetails GetPostHarvestProcess(HarvestExecutionResult harvestResult)
        {
            if (!_isInitialized) Initialize();
            return _executionHandler.GetPostHarvestProcess(harvestResult);
        }

        /// <summary>
        /// Get harvest history
        /// </summary>
        public List<HarvestAttempt> GetHarvestHistory()
        {
            return _executionHandler?.HarvestHistory ?? new List<HarvestAttempt>();
        }

        /// <summary>
        /// Set harvest parameters
        /// </summary>
        public void SetHarvestParameters(bool enableQuality, bool enableTracking, float readinessThreshold)
        {
            _enableQualityAssessment = enableQuality;
            _enablePostHarvestTracking = enableTracking;
            _harvestReadinessThreshold = Mathf.Clamp01(readinessThreshold);

            if (_calculator != null)
            {
                _calculator.SetParameters(
                    _harvestReadinessThreshold,
                    _trichomeReadinessWeight,
                    _maturityReadinessWeight,
                    _environmentalReadinessWeight,
                    _optimalHarvestWindow
                );
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT",
                    $"Harvest parameters updated: Quality={enableQuality}, Tracking={enableTracking}, Threshold={readinessThreshold:F2}",
                    null);
            }
        }
    }
}

