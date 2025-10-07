using UnityEngine;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Cost Calculation Engine (Coordinator)
    /// Single Responsibility: Orchestrate cost calculations and manage calculation lifecycle
    /// Refactored from 687 lines → 236 lines (4 files total, all <500 lines)
    /// Dependencies: CostCalculationProfileManager, CostComponentCalculator
    /// </summary>
    public class CostCalculationEngine
    {
        [Header("Calculation Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _useAdvancedCalculations = true;
        [SerializeField] private float _calculationPrecision = 0.01f;

        // Component dependencies
        private CostCalculationProfileManager _profileManager;
        private CostComponentCalculator _componentCalculator;

        // Calculation statistics
        private CostCalculationStats _stats = new CostCalculationStats();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event Action<CostBreakdown> OnCostCalculationComplete;
        public event Action<string, float> OnCostComponentCalculated;
        public event Action<CostCalculationStats> OnStatsUpdated;

        public bool IsInitialized => _isInitialized;
        public CostCalculationStats Stats => _stats;
        public CostCalculationParameters Parameters => _profileManager?.Parameters ?? default;

        /// <summary>
        /// Initialize calculation engine with default configuration
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize profile manager
            _profileManager = new CostCalculationProfileManager(_enableLogging);
            _profileManager.Initialize();

            // Initialize component calculator
            _componentCalculator = new CostComponentCalculator(_profileManager, _profileManager.Parameters, _enableLogging);

            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Cost Calculation Engine initialized successfully");
            }
        }

        /// <summary>
        /// Calculate comprehensive cost breakdown
        /// </summary>
        public CostBreakdown CalculateCostBreakdown(CostCalculationRequest request)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Cost calculation engine not initialized");
            }

            var startTime = DateTime.Now;
            _stats.CalculationsPerformed++;

            try
            {
                var breakdown = new CostBreakdown
                {
                    CalculationId = GenerateCalculationId(),
                    Request = request,
                    CalculationTime = startTime
                };

                // Calculate each cost component using component calculator
                breakdown.LaborCost = _componentCalculator.CalculateLaborCost(request);
                OnCostComponentCalculated?.Invoke("Labor", breakdown.LaborCost);

                breakdown.PartsCost = _componentCalculator.CalculatePartsCost(request);
                OnCostComponentCalculated?.Invoke("Parts", breakdown.PartsCost);

                breakdown.OverheadCost = _componentCalculator.CalculateOverheadCost(request);
                OnCostComponentCalculated?.Invoke("Overhead", breakdown.OverheadCost);

                breakdown.EmergencySurcharge = request.IsEmergency
                    ? _componentCalculator.CalculateEmergencySurcharge(request)
                    : 0f;
                if (breakdown.EmergencySurcharge > 0)
                    OnCostComponentCalculated?.Invoke("Emergency", breakdown.EmergencySurcharge);

                breakdown.TaxesFees = _componentCalculator.CalculateTaxesAndFees(request);
                OnCostComponentCalculated?.Invoke("TaxesFees", breakdown.TaxesFees);

                breakdown.DiscountAdjustment = _componentCalculator.CalculateDiscounts(request);
                if (breakdown.DiscountAdjustment > 0)
                    OnCostComponentCalculated?.Invoke("Discount", -breakdown.DiscountAdjustment);

                // Calculate subtotal and final total
                breakdown.Subtotal = breakdown.LaborCost + breakdown.PartsCost + breakdown.OverheadCost + breakdown.EmergencySurcharge;
                breakdown.TotalCost = breakdown.Subtotal + breakdown.TaxesFees - breakdown.DiscountAdjustment;

                // Apply precision rounding
                breakdown = ApplyCalculationPrecision(breakdown);

                // Calculate confidence score
                breakdown.ConfidenceScore = CalculateConfidenceScore(request);

                breakdown.CalculationDuration = (float)(DateTime.Now - startTime).TotalMilliseconds;

                _stats.SuccessfulCalculations++;
                _stats.TotalCalculationTime += breakdown.CalculationDuration;

                OnCostCalculationComplete?.Invoke(breakdown);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT",
                        $"Cost calculated: ${breakdown.TotalCost:F2} " +
                        $"(Labor: ${breakdown.LaborCost:F2}, Parts: ${breakdown.PartsCost:F2}, " +
                        $"Confidence: {breakdown.ConfidenceScore:P1}, Duration: {breakdown.CalculationDuration:F1}ms)");
                }

                return breakdown;
            }
            catch (Exception ex)
            {
                _stats.FailedCalculations++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Cost calculation failed: {ex.Message}");
                }

                throw new CostCalculationException($"Failed to calculate cost breakdown: {ex.Message}", ex);
            }
            finally
            {
                OnStatsUpdated?.Invoke(_stats);
            }
        }

        /// <summary>
        /// Calculate confidence score for estimate
        /// </summary>
        private float CalculateConfidenceScore(CostCalculationRequest request)
        {
            var baseConfidence = 0.8f;
            var profile = _profileManager.GetCalculationProfile(request.MalfunctionType);

            // Adjust based on data quality
            if (profile.HistoricalDataPoints > 50)
                baseConfidence += 0.1f;
            else if (profile.HistoricalDataPoints < 10)
                baseConfidence -= 0.2f;

            // Adjust based on complexity
            if (request.RequiresSpecialist)
                baseConfidence -= 0.1f;

            // Adjust based on emergency status
            if (request.IsEmergency)
                baseConfidence -= 0.15f;

            // Adjust based on parts availability
            if (request.PartsAvailability == PartsAvailability.SpecialOrder)
                baseConfidence -= 0.1f;

            return Mathf.Clamp01(baseConfidence);
        }

        /// <summary>
        /// Apply calculation precision rounding
        /// </summary>
        private CostBreakdown ApplyCalculationPrecision(CostBreakdown breakdown)
        {
            var precision = _calculationPrecision;

            breakdown.LaborCost = Mathf.Round(breakdown.LaborCost / precision) * precision;
            breakdown.PartsCost = Mathf.Round(breakdown.PartsCost / precision) * precision;
            breakdown.OverheadCost = Mathf.Round(breakdown.OverheadCost / precision) * precision;
            breakdown.EmergencySurcharge = Mathf.Round(breakdown.EmergencySurcharge / precision) * precision;
            breakdown.TaxesFees = Mathf.Round(breakdown.TaxesFees / precision) * precision;
            breakdown.DiscountAdjustment = Mathf.Round(breakdown.DiscountAdjustment / precision) * precision;
            breakdown.Subtotal = Mathf.Round(breakdown.Subtotal / precision) * precision;
            breakdown.TotalCost = Mathf.Round(breakdown.TotalCost / precision) * precision;

            return breakdown;
        }

        /// <summary>
        /// Generate unique calculation ID
        /// </summary>
        private string GenerateCalculationId()
        {
            return $"CALC_{DateTime.Now:yyyyMMdd}_{UnityEngine.Random.Range(1000, 9999)}";
        }

        /// <summary>
        /// Reset calculation statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new CostCalculationStats();
        }

        /// <summary>
        /// Set custom calculation parameters
        /// </summary>
        public void SetCalculationParameters(CostCalculationParameters parameters)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Engine must be initialized before setting parameters");
            }

            _profileManager.SetCalculationParameters(parameters);

            // Recreate component calculator with new parameters
            _componentCalculator = new CostComponentCalculator(_profileManager, parameters, _enableLogging);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Cost calculation parameters updated");
            }
        }

        /// <summary>
        /// Update specific calculation profile
        /// </summary>
        public void UpdateCalculationProfile(MalfunctionType type, CostCalculationProfile profile)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Engine must be initialized before updating profiles");
            }

            _profileManager.UpdateCalculationProfile(type, profile);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Calculation profile updated for {type}");
            }
        }

        /// <summary>
        /// Get current calculation statistics
        /// </summary>
        public CostCalculationStats GetStats()
        {
            return _stats;
        }

        /// <summary>
        /// Reset engine to default configuration
        /// </summary>
        public void ResetToDefaults()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Engine must be initialized before resetting");
            }

            _profileManager.ResetToDefaults();
            _componentCalculator = new CostComponentCalculator(_profileManager, _profileManager.Parameters, _enableLogging);
            ResetStats();

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Cost calculation engine reset to defaults");
            }
        }
    }
}

