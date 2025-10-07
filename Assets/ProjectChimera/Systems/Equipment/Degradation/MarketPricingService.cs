using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// REFACTORED: Market Pricing Service (POCO - Unity-independent)
    /// Single Responsibility: Handle market pricing, inflation, and external pricing data logic
    /// Extracted from MarketPricingAdapter for better separation of concerns
    /// </summary>
    public class MarketPricingService
    {
        private readonly bool _enableLogging;
        private readonly bool _useMarketPricing;
        private bool _includeInflation;
        private float _baseInflationRate;
        private float _marketVolatility;
        private readonly float _priceUpdateInterval;
        private readonly bool _useRealisticMarketFluctuations;
        private readonly float _marketTrendStrength;
        private readonly int _priceHistorySize;
        private readonly bool _useVariableInflation;
        private readonly float _inflationVarianceRange;
        private readonly float _economicCycleLength;
        private readonly float _inflationCompoundingInterval;
        private readonly bool _useExternalPricingData;
        private readonly string _pricingDataEndpoint;
        private readonly float _externalDataTimeout;
        private readonly float _externalDataCacheTime;

        // Market pricing data
        private readonly Dictionary<MalfunctionType, MarketPriceData> _marketPrices = new Dictionary<MalfunctionType, MarketPriceData>();
        private readonly Dictionary<MalfunctionType, List<PriceHistoryEntry>> _priceHistory = new Dictionary<MalfunctionType, List<PriceHistoryEntry>>();

        // Inflation tracking
        private InflationTracker _inflationTracker = new InflationTracker();
        private float _lastInflationUpdate;

        // Market state
        private MarketConditions _currentMarketConditions = new MarketConditions();
        private float _lastMarketUpdate;
        private float _lastPriceUpdate;

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
        public MarketConditions CurrentMarketConditions => _currentMarketConditions;
        public float CurrentInflationRate => _inflationTracker.CurrentRate;

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
            _includeInflation = includeInflation;
            _baseInflationRate = baseInflationRate;
            _marketVolatility = marketVolatility;
            _priceUpdateInterval = priceUpdateInterval;
            _useRealisticMarketFluctuations = useRealisticMarketFluctuations;
            _marketTrendStrength = marketTrendStrength;
            _priceHistorySize = priceHistorySize;
            _useVariableInflation = useVariableInflation;
            _inflationVarianceRange = inflationVarianceRange;
            _economicCycleLength = economicCycleLength;
            _inflationCompoundingInterval = inflationCompoundingInterval;
            _useExternalPricingData = useExternalPricingData;
            _pricingDataEndpoint = pricingDataEndpoint;
            _externalDataTimeout = externalDataTimeout;
            _externalDataCacheTime = externalDataCacheTime;
        }

        public void Initialize(float currentTime, System.Func<float> getRandomRange, System.Func<float, float, float> getSin)
        {
            if (_isInitialized) return;

            InitializeMarketPrices(getRandomRange);
            InitializePriceHistory();
            InitializeInflationTracker();
            InitializeMarketConditions();
            ResetStats();

            _lastMarketUpdate = currentTime;
            _lastPriceUpdate = currentTime;
            _lastInflationUpdate = currentTime;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Market Pricing Adapter initialized", null);
            }
        }

        public float ApplyMarketPricing(float baseCost, MalfunctionType type, float realtimeSinceStartup)
        {
            if (!_isInitialized || !_useMarketPricing)
                return baseCost;

            var pricingStartTime = realtimeSinceStartup;

            try
            {
                if (!_marketPrices.TryGetValue(type, out var priceData))
                {
                    priceData = _marketPrices[MalfunctionType.WearAndTear];
                    _stats.PricingWithoutMarketData++;
                }

                float adjustedCost = baseCost * priceData.CurrentMultiplier;
                adjustedCost = ApplyMarketConditions(adjustedCost, type);
                adjustedCost = ApplySupplyDemandAdjustments(adjustedCost, type);

                if (_useRealisticMarketFluctuations)
                {
                    adjustedCost = ApplyMarketVolatility(adjustedCost, type, () => UnityEngine.Random.Range(-1f, 1f));
                }

                var pricingTime = realtimeSinceStartup - pricingStartTime;
                UpdatePricingStats(pricingTime, adjustedCost - baseCost);

                _stats.MarketAdjustmentsApplied++;

                if (_enableLogging)
                {
                    var adjustment = (adjustedCost - baseCost) / baseCost * 100f;
                    ChimeraLogger.Log("EQUIPMENT", $"Applied market pricing for {type}: {adjustment:+0.0;-0.0}%", null);
                }

                return adjustedCost;
            }
            catch (Exception ex)
            {
                _stats.MarketPricingErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error applying market pricing: {ex.Message}", null);
                }

                return baseCost;
            }
        }

        public float ApplyInflation(float baseCost, float currentTime)
        {
            if (!_isInitialized || !_includeInflation)
                return baseCost;

            try
            {
                float inflationMultiplier = CalculateInflationMultiplier(currentTime);
                float adjustedCost = baseCost * inflationMultiplier;

                _stats.InflationAdjustmentsApplied++;

                if (_enableLogging && Math.Abs(inflationMultiplier - 1f) > 0.001f)
                {
                    var adjustment = (inflationMultiplier - 1f) * 100f;
                    ChimeraLogger.Log("EQUIPMENT", $"Applied inflation adjustment: {adjustment:+0.00;-0.00}%", null);
                }

                return adjustedCost;
            }
            catch (Exception ex)
            {
                _stats.InflationCalculationErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error applying inflation: {ex.Message}", null);
                }

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

                if (_includeInflation)
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

                adjustment.MarketConditions = new MarketConditions(_currentMarketConditions);
                adjustment.InflationRate = _inflationTracker.CurrentRate;

                _stats.PricingAdjustmentsGenerated++;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT", $"Generated pricing adjustment for {type}: {adjustment.TotalAdjustmentPercent:+0.0;-0.0}% (${adjustment.AdjustedCost:F2})", null);
                }

                return adjustment;
            }
            catch (Exception ex)
            {
                _stats.MarketPricingErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error generating pricing adjustment: {ex.Message}", null);
                }

                return new PricingAdjustment { OriginalCost = baseCost, AdjustedCost = baseCost };
            }
        }

        public void UpdateMarketConditions(float currentTime, System.Func<float, float, float> getSin, System.Func<float, float, float> getRandomRange)
        {
            if (!_isInitialized)
                return;

            try
            {
                UpdateMarketState(currentTime, getSin, getRandomRange);

                foreach (var type in _marketPrices.Keys.ToList())
                {
                    UpdateMalfunctionTypePrice(type, getRandomRange);
                }

                UpdateInflation(currentTime, getSin, getRandomRange);

                if (_useExternalPricingData)
                {
                    UpdateExternalPricingData(currentTime);
                }

                _lastMarketUpdate = currentTime;
                _stats.MarketUpdatesPerformed++;

                OnMarketConditionsChanged?.Invoke(_currentMarketConditions);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT", $"Updated market conditions: Trend={_currentMarketConditions.TrendDirection}, Volatility={_currentMarketConditions.VolatilityIndex:F2}", null);
                }
            }
            catch (Exception ex)
            {
                _stats.MarketUpdateErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error updating market conditions: {ex.Message}", null);
                }
            }
        }

        public void ProcessPeriodicUpdate(float currentTime, System.Func<float, float, float> getSin, System.Func<float, float, float> getRandomRange)
        {
            if (!_isInitialized)
                return;

            if (currentTime - _lastPriceUpdate > _priceUpdateInterval)
            {
                UpdateMarketConditions(currentTime, getSin, getRandomRange);
                _lastPriceUpdate = currentTime;
            }
        }

        public void SetMarketParameters(bool useMarket, bool useInflation, float inflationRate, float volatility)
        {
            _includeInflation = useInflation;
            _baseInflationRate = Clamp(inflationRate, -0.1f, 0.2f);
            _marketVolatility = Clamp01(volatility);

            if (_inflationTracker != null)
            {
                _inflationTracker.BaseRate = _baseInflationRate;
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Market parameters updated: Market={_useMarketPricing}, Inflation={_includeInflation}, Rate={_baseInflationRate:F3}, Volatility={_marketVolatility:F2}", null);
            }
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
            {
                _priceHistory[type] = new List<PriceHistoryEntry>();
            }
        }

        private void InitializeInflationTracker()
        {
            _inflationTracker = new InflationTracker
            {
                BaseRate = _baseInflationRate,
                CurrentRate = _baseInflationRate,
                VarianceRange = _inflationVarianceRange,
                LastUpdate = DateTime.Now,
                CyclePosition = 0f,
                CycleLength = _economicCycleLength,
                CompoundingInterval = _inflationCompoundingInterval
            };
        }

        private void InitializeMarketConditions()
        {
            _currentMarketConditions = new MarketConditions
            {
                TrendDirection = MarketTrend.Stable,
                VolatilityIndex = 0.5f,
                DemandPressure = 0f,
                SupplyConstraints = 0f,
                EconomicIndicator = 1f,
                LastUpdate = DateTime.Now,
                ConfidenceLevel = 0.8f
            };
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

        private void UpdateMarketState(float currentTime, System.Func<float, float, float> getSin, System.Func<float, float, float> getRandomRange)
        {
            var cycleTime = currentTime % _economicCycleLength;
            var cyclePosition = cycleTime / _economicCycleLength;

            var trendValue = getSin(cyclePosition * 2f * (float)Math.PI, 1f) * _marketTrendStrength;

            if (trendValue > 0.02f) _currentMarketConditions.TrendDirection = MarketTrend.Bullish;
            else if (trendValue < -0.02f) _currentMarketConditions.TrendDirection = MarketTrend.Bearish;
            else _currentMarketConditions.TrendDirection = MarketTrend.Stable;

            var volatilityNoise = getRandomRange(-0.1f, 0.1f);
            _currentMarketConditions.VolatilityIndex = Clamp01(0.5f + volatilityNoise + trendValue);

            _currentMarketConditions.DemandPressure = getRandomRange(-0.2f, 0.2f);
            _currentMarketConditions.SupplyConstraints = getRandomRange(-0.15f, 0.15f);

            var economicTrend = 1f + trendValue + getRandomRange(-0.05f, 0.05f);
            _currentMarketConditions.EconomicIndicator = Clamp(economicTrend, 0.8f, 1.2f);

            _currentMarketConditions.LastUpdate = DateTime.Now;
        }

        private void UpdateMalfunctionTypePrice(MalfunctionType type, System.Func<float, float, float> getRandomRange)
        {
            if (!_marketPrices.TryGetValue(type, out var priceData))
                return;

            var oldMultiplier = priceData.CurrentMultiplier;

            var trendImpact = _currentMarketConditions.TrendDirection switch
            {
                MarketTrend.Bullish => getRandomRange(0.01f, 0.03f),
                MarketTrend.Bearish => getRandomRange(-0.03f, -0.01f),
                _ => getRandomRange(-0.005f, 0.005f)
            };

            var volatilityImpact = getRandomRange(-priceData.VolatilityFactor, priceData.VolatilityFactor) * _currentMarketConditions.VolatilityIndex;
            var demandSupplyImpact = CalculateDemandSupplyImpact(priceData);

            var adjustment = trendImpact + volatilityImpact + demandSupplyImpact;
            priceData.CurrentMultiplier = Clamp(priceData.BaseMultiplier + adjustment, 0.5f, 2f);

            var priceChange = priceData.CurrentMultiplier - oldMultiplier;
            if (Math.Abs(priceChange) > 0.01f)
            {
                priceData.TrendDirection = priceChange > 0 ? MarketTrend.Bullish : MarketTrend.Bearish;
            }
            else
            {
                priceData.TrendDirection = MarketTrend.Stable;
            }

            priceData.LastUpdate = DateTime.Now;

            AddPriceHistoryEntry(type, priceData.CurrentMultiplier);

            if (Math.Abs(priceChange) > 0.02f)
            {
                OnMarketPriceUpdated?.Invoke(type, priceData.CurrentMultiplier);
            }
        }

        private float CalculateDemandSupplyImpact(MarketPriceData priceData)
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

        private void UpdateInflation(float currentTime, System.Func<float, float, float> getSin, System.Func<float, float, float> getRandomRange)
        {
            var timeSinceUpdate = currentTime - _lastInflationUpdate;

            if (timeSinceUpdate > 86400f)
            {
                var oldRate = _inflationTracker.CurrentRate;

                var variance = getRandomRange(-_inflationTracker.VarianceRange, _inflationTracker.VarianceRange);
                _inflationTracker.CurrentRate = _inflationTracker.BaseRate + variance;

                var cycleInfluence = getSin(currentTime / _inflationTracker.CycleLength * 2f * (float)Math.PI, 1f) * 0.01f;
                _inflationTracker.CurrentRate += cycleInfluence;

                _inflationTracker.CurrentRate = Clamp(_inflationTracker.CurrentRate, -0.1f, 0.2f);
                _inflationTracker.LastUpdate = DateTime.Now;

                _lastInflationUpdate = currentTime;

                if (Math.Abs(_inflationTracker.CurrentRate - oldRate) > 0.005f)
                {
                    OnInflationRateUpdated?.Invoke(_inflationTracker.CurrentRate);
                }

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT", $"Updated inflation rate: {_inflationTracker.CurrentRate:F4} ({_inflationTracker.CurrentRate * 100:+0.00;-0.00}%)", null);
                }
            }
        }

        private float CalculateInflationMultiplier(float currentTime)
        {
            var yearsElapsed = currentTime / _inflationTracker.CompoundingInterval;
            return Pow(1f + _inflationTracker.CurrentRate, yearsElapsed);
        }

        private float ApplyMarketConditions(float cost, MalfunctionType type)
        {
            var conditionsMultiplier = 1f;

            conditionsMultiplier *= _currentMarketConditions.TrendDirection switch
            {
                MarketTrend.Bullish => 1f + (_marketTrendStrength * _currentMarketConditions.VolatilityIndex),
                MarketTrend.Bearish => 1f - (_marketTrendStrength * _currentMarketConditions.VolatilityIndex),
                _ => 1f
            };

            conditionsMultiplier *= _currentMarketConditions.EconomicIndicator;

            return cost * conditionsMultiplier;
        }

        private float ApplySupplyDemandAdjustments(float cost, MalfunctionType type)
        {
            if (!_marketPrices.TryGetValue(type, out var priceData))
                return cost;

            var adjustment = CalculateDemandSupplyImpact(priceData);
            return cost * (1f + adjustment);
        }

        private float ApplyMarketVolatility(float cost, MalfunctionType type, System.Func<float> getRandomNormalized)
        {
            if (!_marketPrices.TryGetValue(type, out var priceData))
                return cost;

            var volatilityRange = priceData.VolatilityFactor * _marketVolatility;
            var volatilityAdjustment = getRandomNormalized() * volatilityRange;

            return cost * (1f + volatilityAdjustment);
        }

        private void AddPriceHistoryEntry(MalfunctionType type, float multiplier)
        {
            if (!_priceHistory.TryGetValue(type, out var history))
                return;

            history.Add(new PriceHistoryEntry
            {
                Timestamp = DateTime.Now,
                PriceMultiplier = multiplier,
                MarketConditions = new MarketConditions(_currentMarketConditions)
            });

            while (history.Count > _priceHistorySize)
            {
                history.RemoveAt(0);
            }
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

        private void UpdatePricingStats(float operationTime, float adjustment)
        {
            _stats.TotalPricingOperationTime += operationTime;
            _stats.TotalPricingOperations++;
            _stats.AveragePricingOperationTime = _stats.TotalPricingOperationTime / _stats.TotalPricingOperations;

            if (operationTime > _stats.MaxPricingOperationTime)
                _stats.MaxPricingOperationTime = operationTime;

            _stats.TotalPriceAdjustment += Math.Abs(adjustment);
            _stats.AveragePriceAdjustment = _stats.TotalPriceAdjustment / _stats.TotalPricingOperations;
        }

        private void ResetStats()
        {
            _stats = new MarketPricingStats
            {
                MarketAdjustmentsApplied = 0,
                InflationAdjustmentsApplied = 0,
                PricingAdjustmentsGenerated = 0,
                MarketUpdatesPerformed = 0,
                MarketPricingErrors = 0,
                InflationCalculationErrors = 0,
                MarketUpdateErrors = 0,
                PricingWithoutMarketData = 0,
                ExternalDataUpdates = 0,
                TotalPricingOperations = 0,
                TotalPricingOperationTime = 0f,
                AveragePricingOperationTime = 0f,
                MaxPricingOperationTime = 0f,
                TotalPriceAdjustment = 0f,
                AveragePriceAdjustment = 0f
            };
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private float Pow(float baseValue, float exponent)
        {
            return (float)Math.Pow(baseValue, exponent);
        }

        #endregion
    }
}
