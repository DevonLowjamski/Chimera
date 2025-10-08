using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// REFACTORED: Market Pricing Service Coordinator
    /// Single Responsibility: Coordinate market pricing, inflation, and market conditions through helper classes
    /// Reduced from 678 lines using composition with Calculator, Inflation, and Condition helpers
    /// </summary>
    public class MarketPricingService
    {
        private readonly bool _enableLogging;
        private readonly bool _useMarketPricing;
        private readonly int _priceHistorySize;
        private readonly bool _useExternalPricingData;
        private readonly float _externalDataCacheTime;

        // Market pricing data
        private readonly Dictionary<MalfunctionType, MarketPriceData> _marketPrices = new Dictionary<MalfunctionType, MarketPriceData>();
        private readonly Dictionary<MalfunctionType, List<PriceHistoryEntry>> _priceHistory = new Dictionary<MalfunctionType, List<PriceHistoryEntry>>();

        // Helper components (Composition pattern for SRP)
        private MarketPricingCalculator _pricingCalculator;
        private InflationCalculator _inflationCalculator;
        private MarketConditionUpdater _conditionUpdater;

        // External data cache
        private ExternalPricingCache _externalCache = new ExternalPricingCache();

        // Statistics
        private MarketPricingStats _stats = new MarketPricingStats();

        // State management
        private bool _isInitialized = false;

        // Events
        public event Action<MalfunctionType, float> OnMarketPriceUpdated;
        public event Action<float> OnInflationRateUpdated;
        public event Action<MarketConditions> OnMarketConditionsChanged;
        public event Action<string> OnExternalDataReceived;

        public bool IsInitialized => _isInitialized;
        public MarketPricingStats Stats => _stats;
        public MarketConditions CurrentMarketConditions => _conditionUpdater?.CurrentConditions ?? new MarketConditions();
        public float CurrentInflationRate => _inflationCalculator?.CurrentInflationRate ?? 0f;

        public MarketPricingService(
            bool enableLogging = false,
            bool useMarketPricing = true,
            bool includeInflation = true,
            float baseInflationRate = 0.03f,
            float marketVolatility = 0.1f,
            float priceUpdateInterval = 3600f,
            bool useRealisticMarketFluctuations = true,
            float marketTrendStrength = 0.05f,
            int priceHistorySize = 168,
            bool useVariableInflation = true,
            float inflationVarianceRange = 0.01f,
            float economicCycleLength = 86400f,
            float inflationCompoundingInterval = 31536000f,
            bool useExternalPricingData = false,
            string pricingDataEndpoint = "",
            float externalDataTimeout = 30f,
            float externalDataCacheTime = 1800f)
        {
            _enableLogging = enableLogging;
            _useMarketPricing = useMarketPricing;
            _priceHistorySize = priceHistorySize;
            _useExternalPricingData = useExternalPricingData;
            _externalDataCacheTime = externalDataCacheTime;

            // Initialize helper components
            _inflationCalculator = new InflationCalculator(
                baseInflationRate, useVariableInflation, inflationVarianceRange,
                economicCycleLength, inflationCompoundingInterval);

            _conditionUpdater = new MarketConditionUpdater(priceUpdateInterval);

            _pricingCalculator = new MarketPricingCalculator(
                _marketPrices, _priceHistory, marketVolatility,
                marketTrendStrength, useRealisticMarketFluctuations);
        }

        public void Initialize(float currentTime, System.Func<float> getRandomRange, System.Func<float, float, float> getSin)
        {
            if (_isInitialized) return;

            InitializeMarketPrices(getRandomRange);
            InitializePriceHistory();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
                ChimeraLogger.Log("EQUIPMENT", "Market Pricing Service initialized", null);
        }

        public float ApplyMarketPricing(float baseCost, MalfunctionType type, float realtimeSinceStartup)
        {
            if (!_isInitialized || !_useMarketPricing)
                return baseCost;

            try
            {
                if (!_marketPrices.TryGetValue(type, out var priceData))
                {
                    priceData = _marketPrices[MalfunctionType.WearAndTear];
                    _stats.PricingWithoutMarketData++;
                }

                float adjustedCost = baseCost * priceData.CurrentMultiplier;
                adjustedCost *= GetMarketConditionsMultiplier();
                adjustedCost *= (1f + GetDemandSupplyImpact(priceData));

                _stats.MarketAdjustmentsApplied++;

                if (_enableLogging)
                    ChimeraLogger.Log("EQUIPMENT", $"Applied market pricing for {type}", null);

                return adjustedCost;
            }
            catch (Exception ex)
            {
                _stats.MarketPricingErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("EQUIPMENT", $"Error applying market pricing: {ex.Message}", null);
                return baseCost;
            }
        }

        public float ApplyInflation(float baseCost, float currentTime)
        {
            if (!_isInitialized || _inflationCalculator == null)
                return baseCost;

            try
            {
                float adjustedCost = _inflationCalculator.ApplyInflation(baseCost, currentTime);
                _stats.InflationAdjustmentsApplied++;
                return adjustedCost;
            }
            catch (Exception ex)
            {
                _stats.InflationCalculationErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("EQUIPMENT", $"Error applying inflation: {ex.Message}", null);
                return baseCost;
            }
        }

        public PricingAdjustment GetPricingAdjustment(float baseCost, MalfunctionType type, float realtimeSinceStartup, float currentTime)
        {
            if (!_isInitialized)
                return new PricingAdjustment { OriginalCost = baseCost, AdjustedCost = baseCost };

            try
            {
                var adjustment = new PricingAdjustment
                {
                    OriginalCost = baseCost,
                    MalfunctionType = type,
                    Timestamp = DateTime.Now
                };

                if (_useMarketPricing)
                {
                    adjustment.MarketAdjustedCost = ApplyMarketPricing(baseCost, type, realtimeSinceStartup);
                    adjustment.MarketAdjustment = adjustment.MarketAdjustedCost - baseCost;
                    adjustment.MarketAdjustmentPercent = adjustment.MarketAdjustment / baseCost * 100f;
                }
                else
                {
                    adjustment.MarketAdjustedCost = baseCost;
                }

                if (_inflationCalculator != null)
                {
                    adjustment.InflationAdjustedCost = ApplyInflation(adjustment.MarketAdjustedCost, currentTime);
                    adjustment.InflationAdjustment = adjustment.InflationAdjustedCost - adjustment.MarketAdjustedCost;
                    adjustment.InflationAdjustmentPercent = adjustment.InflationAdjustment / adjustment.MarketAdjustedCost * 100f;
                }
                else
                {
                    adjustment.InflationAdjustedCost = adjustment.MarketAdjustedCost;
                }

                adjustment.AdjustedCost = adjustment.InflationAdjustedCost;
                adjustment.TotalAdjustment = adjustment.AdjustedCost - baseCost;
                adjustment.TotalAdjustmentPercent = adjustment.TotalAdjustment / baseCost * 100f;
                adjustment.MarketConditions = new MarketConditions(CurrentMarketConditions);
                adjustment.InflationRate = CurrentInflationRate;

                _stats.PricingAdjustmentsGenerated++;

                return adjustment;
            }
            catch (Exception ex)
            {
                _stats.MarketPricingErrors++;
                return new PricingAdjustment { OriginalCost = baseCost, AdjustedCost = baseCost };
            }
        }

        public void UpdateMarketConditions(float currentTime, System.Func<float, float, float> getSin, System.Func<float, float, float> getRandomRange)
        {
            if (!_isInitialized || _conditionUpdater == null)
                return;

            try
            {
                _conditionUpdater.UpdateMarketConditions(currentTime, getSin, getRandomRange);

                foreach (var type in _marketPrices.Keys.ToList())
                    UpdateMalfunctionTypePrice(type, getRandomRange);

                if (_inflationCalculator != null)
                    _inflationCalculator.UpdateInflationRate(currentTime);

                if (_useExternalPricingData)
                    UpdateExternalPricingData(currentTime);

                _stats.MarketUpdatesPerformed++;
                OnMarketConditionsChanged?.Invoke(CurrentMarketConditions);
            }
            catch (Exception ex)
            {
                _stats.MarketUpdateErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("EQUIPMENT", $"Error updating market conditions: {ex.Message}", null);
            }
        }

        public void ProcessPeriodicUpdate(float currentTime, System.Func<float, float, float> getSin, System.Func<float, float, float> getRandomRange)
        {
            if (!_isInitialized || _conditionUpdater == null)
                return;

            if (_conditionUpdater.ShouldUpdatePrices(currentTime))
            {
                UpdateMarketConditions(currentTime, getSin, getRandomRange);
                _conditionUpdater.MarkPricesUpdated(currentTime);
            }
        }

        public void SetMarketParameters(bool useMarket, bool useInflation, float inflationRate, float volatility)
        {
            if (_inflationCalculator != null && useInflation)
                _inflationCalculator.SetBaseInflationRate(Clamp(inflationRate, -0.1f, 0.2f));

            if (_enableLogging)
                ChimeraLogger.Log("EQUIPMENT", $"Market parameters updated: Inflation={useInflation}", null);
        }

        #region Private Methods

        private void InitializeMarketPrices(System.Func<float> getRandomRange)
        {
            var malfunctionTypes = Enum.GetValues(typeof(MalfunctionType)).Cast<MalfunctionType>();

            foreach (var type in malfunctionTypes)
            {
                _marketPrices[type] = new MarketPriceData
                {
                    MalfunctionType = type,
                    BaseMultiplier = GetBaseMarketMultiplier(type, getRandomRange),
                    CurrentMultiplier = GetBaseMarketMultiplier(type, getRandomRange),
                    VolatilityFactor = GetVolatilityFactor(type),
                    DemandLevel = DemandLevel.Normal,
                    SupplyLevel = SupplyLevel.Normal,
                    LastUpdate = DateTime.Now,
                    TrendDirection = MarketTrend.Stable
                };
            }
        }

        private void InitializePriceHistory()
        {
            foreach (var type in _marketPrices.Keys)
                _priceHistory[type] = new List<PriceHistoryEntry>();
        }

        private float GetBaseMarketMultiplier(MalfunctionType type, System.Func<float> getRandomRange)
        {
            return type switch
            {
                MalfunctionType.SoftwareError => getRandomRange() * 0.2f + 1.15f,
                MalfunctionType.ElectricalFailure => getRandomRange() * 0.2f + 1.05f,
                MalfunctionType.MechanicalFailure => getRandomRange() * 0.1f + 1.03f,
                MalfunctionType.SensorDrift => getRandomRange() * 0.2f + 1f,
                MalfunctionType.OverheatingProblem => getRandomRange() * 0.2f + 1.02f,
                MalfunctionType.WearAndTear => getRandomRange() * 0.1f + 1.01f,
                _ => 1f
            };
        }

        private float GetVolatilityFactor(MalfunctionType type)
        {
            return type switch
            {
                MalfunctionType.SoftwareError => 0.15f,
                MalfunctionType.ElectricalFailure => 0.12f,
                MalfunctionType.OverheatingProblem => 0.10f,
                MalfunctionType.MechanicalFailure => 0.08f,
                MalfunctionType.SensorDrift => 0.06f,
                MalfunctionType.WearAndTear => 0.05f,
                _ => 0.08f
            };
        }

        private void UpdateMalfunctionTypePrice(MalfunctionType type, System.Func<float, float, float> getRandomRange)
        {
            if (!_marketPrices.TryGetValue(type, out var priceData))
                return;

            var oldMultiplier = priceData.CurrentMultiplier;

            var trendImpact = CurrentMarketConditions.Trend switch
            {
                MarketTrend.Bullish => getRandomRange(0.01f, 0.03f),
                MarketTrend.Bearish => getRandomRange(-0.03f, -0.01f),
                _ => getRandomRange(-0.005f, 0.005f)
            };

            var volatilityImpact = getRandomRange(-priceData.VolatilityFactor, priceData.VolatilityFactor);
            var demandSupplyImpact = GetDemandSupplyImpact(priceData);

            var adjustment = trendImpact + volatilityImpact + demandSupplyImpact;
            priceData.CurrentMultiplier = Clamp(priceData.BaseMultiplier + adjustment, 0.5f, 2f);

            var priceChange = priceData.CurrentMultiplier - oldMultiplier;
            priceData.TrendDirection = Math.Abs(priceChange) > 0.01f ? 
                (priceChange > 0 ? MarketTrend.Bullish : MarketTrend.Bearish) : 
                MarketTrend.Stable;

            priceData.LastUpdate = DateTime.Now;

            AddPriceHistoryEntry(type, priceData.CurrentMultiplier);

            if (Math.Abs(priceChange) > 0.02f)
                OnMarketPriceUpdated?.Invoke(type, priceData.CurrentMultiplier);
        }

        private float GetDemandSupplyImpact(MarketPriceData priceData)
        {
            var demandImpact = priceData.DemandLevel switch
            {
                DemandLevel.VeryLow => -0.05f,
                DemandLevel.Low => -0.02f,
                DemandLevel.Normal => 0f,
                DemandLevel.High => 0.02f,
                DemandLevel.VeryHigh => 0.05f,
                _ => 0f
            };

            var supplyImpact = priceData.SupplyLevel switch
            {
                SupplyLevel.VeryLow => 0.04f,
                SupplyLevel.Low => 0.02f,
                SupplyLevel.Normal => 0f,
                SupplyLevel.High => -0.01f,
                SupplyLevel.VeryHigh => -0.03f,
                _ => 0f
            };

            return demandImpact + supplyImpact;
        }

        private float GetMarketConditionsMultiplier()
        {
            return CurrentMarketConditions.Trend switch
            {
                MarketTrend.Bullish => 1.05f,
                MarketTrend.Bearish => 0.95f,
                _ => 1f
            };
        }

        private void AddPriceHistoryEntry(MalfunctionType type, float multiplier)
        {
            if (!_priceHistory.TryGetValue(type, out var history))
                return;

            history.Add(new PriceHistoryEntry
            {
                Timestamp = DateTime.Now,
                PriceMultiplier = multiplier,
                MarketConditions = new MarketConditions(CurrentMarketConditions)
            });

            while (history.Count > _priceHistorySize)
                history.RemoveAt(0);
        }

        private void UpdateExternalPricingData(float currentTime)
        {
            if (currentTime - _externalCache.LastUpdate < _externalDataCacheTime)
                return;

            _externalCache.LastUpdate = currentTime;
            _externalCache.DataValid = true;

            OnExternalDataReceived?.Invoke("Simulated external pricing data updated");

            _stats.ExternalDataUpdates++;
        }

        private void ResetStats()
        {
            _stats = new MarketPricingStats();
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        #endregion
    }
}
