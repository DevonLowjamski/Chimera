using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// REFACTORED: Market Pricing Adapter - Thin MonoBehaviour bridge with ITickable
    /// Delegates to MarketPricingService for all business logic
    /// Single Responsibility: Unity lifecycle management and Time dependency injection
    /// Uses ITickable for centralized update management
    /// </summary>
    public class MarketPricingAdapter : MonoBehaviour, ITickable
    {
        [Header("Market Pricing Configuration")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _useMarketPricing = true;
        [SerializeField] private bool _includeInflation = true;
        [SerializeField] private float _baseInflationRate = 0.03f;
        [SerializeField] private float _marketVolatility = 0.1f;

        [Header("Price Update Settings")]
        [SerializeField] private float _priceUpdateInterval = 3600f;
        [SerializeField] private bool _useRealisticMarketFluctuations = true;
        [SerializeField] private float _marketTrendStrength = 0.05f;
        [SerializeField] private int _priceHistorySize = 168;

        [Header("Inflation Settings")]
        [SerializeField] private bool _useVariableInflation = true;
        [SerializeField] private float _inflationVarianceRange = 0.01f;
        [SerializeField] private float _economicCycleLength = 86400f;
        [SerializeField] private float _inflationCompoundingInterval = 31536000f;

        [Header("External Data Integration")]
        [SerializeField] private bool _useExternalPricingData = false;
        [SerializeField] private string _pricingDataEndpoint = "";
        [SerializeField] private float _externalDataTimeout = 30f;
        [SerializeField] private float _externalDataCacheTime = 1800f;

        private MarketPricingService _service;

        // Events - forwarded from service
        public event Action<MalfunctionType, float> OnMarketPriceUpdated
        {
            add { if (_service != null) _service.OnMarketPriceUpdated += value; }
            remove { if (_service != null) _service.OnMarketPriceUpdated -= value; }
        }

        public event Action<float> OnInflationRateUpdated
        {
            add { if (_service != null) _service.OnInflationRateUpdated += value; }
            remove { if (_service != null) _service.OnInflationRateUpdated -= value; }
        }

        public event Action<MarketConditions> OnMarketConditionsChanged
        {
            add { if (_service != null) _service.OnMarketConditionsChanged += value; }
            remove { if (_service != null) _service.OnMarketConditionsChanged -= value; }
        }

        public event Action<string> OnExternalDataReceived
        {
            add { if (_service != null) _service.OnExternalDataReceived += value; }
            remove { if (_service != null) _service.OnExternalDataReceived -= value; }
        }

        // Properties
        public bool IsInitialized => _service?.IsInitialized ?? false;
        public MarketPricingStats Stats => _service?.Stats ?? new MarketPricingStats();
        public MarketConditions CurrentMarketConditions => _service?.CurrentMarketConditions ?? new MarketConditions();
        public float CurrentInflationRate => _service?.CurrentInflationRate ?? 0f;

        private void Awake()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            _service = new MarketPricingService(
                _enableLogging,
                _useMarketPricing,
                _includeInflation,
                _baseInflationRate,
                _marketVolatility,
                _priceUpdateInterval,
                _useRealisticMarketFluctuations,
                _marketTrendStrength,
                _priceHistorySize,
                _useVariableInflation,
                _inflationVarianceRange,
                _economicCycleLength,
                _inflationCompoundingInterval,
                _useExternalPricingData,
                _pricingDataEndpoint,
                _externalDataTimeout,
                _externalDataCacheTime
            );
        }

        #region Public API (delegates to service)

        public void Initialize()
        {
            _service?.Initialize(
                Time.time,
                () => UnityEngine.Random.value,
                (val, mult) => Mathf.Sin(val) * mult
            );
        }

        public float ApplyMarketPricing(float baseCost, MalfunctionType type)
            => _service?.ApplyMarketPricing(baseCost, type, Time.realtimeSinceStartup) ?? baseCost;

        public float ApplyInflation(float baseCost)
            => _service?.ApplyInflation(baseCost, Time.time) ?? baseCost;

        public PricingAdjustment GetPricingAdjustment(float baseCost, MalfunctionType type)
            => _service?.GetPricingAdjustment(baseCost, type, Time.realtimeSinceStartup, Time.time)
                ?? new PricingAdjustment { OriginalCost = baseCost, AdjustedCost = baseCost };

        public void UpdateMarketConditions()
        {
            _service?.UpdateMarketConditions(
                Time.time,
                (val, mult) => Mathf.Sin(val) * mult,
                (min, max) => UnityEngine.Random.Range(min, max)
            );
        }

        public void SetMarketParameters(bool useMarket, bool useInflation, float inflationRate, float volatility)
            => _service?.SetMarketParameters(useMarket, useInflation, inflationRate, volatility);

        #endregion

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.EconomyManager; // Market pricing affects economy
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            _service?.ProcessPeriodicUpdate(
                Time.time,
                (val, mult) => Mathf.Sin(val) * mult,
                (min, max) => UnityEngine.Random.Range(min, max)
            );
        }

        private void OnEnable()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Market price data for malfunction types
    /// </summary>
    [Serializable]
    public class MarketPriceData
    {
        public MalfunctionType MalfunctionType;
        public float BaseMultiplier;
        public float CurrentMultiplier;
        public float VolatilityFactor;
        public DemandLevel DemandLevel;
        public SupplyLevel SupplyLevel;
        public DateTime LastUpdate;
        public MarketTrend TrendDirection;
        public int PriceUpdateCount;

        // Aliases for backward compatibility
        public float BasePrice { get => BaseMultiplier; set => BaseMultiplier = value; }
        public float CurrentPrice { get => CurrentMultiplier; set => CurrentMultiplier = value; }
        public float Volatility { get => VolatilityFactor; set => VolatilityFactor = value; }
        public DateTime LastUpdateTime { get => LastUpdate; set => LastUpdate = value; }
        public int UpdateCount { get => PriceUpdateCount; set => PriceUpdateCount = value; }
    }

    /// <summary>
    /// Price history entry
    /// </summary>
    [Serializable]
    public class PriceHistoryEntry
    {
        public DateTime Timestamp;
        public float PriceMultiplier;
        public MarketConditions MarketConditions;

        // Alias for backward compatibility
        public float Price { get => PriceMultiplier; set => PriceMultiplier = value; }
    }

    /// <summary>
    /// Inflation tracking
    /// </summary>
    [Serializable]
    public class InflationTracker
    {
        public float BaseRate;
        public float CurrentRate;
        public float VarianceRange;
        public DateTime LastUpdate;
        public float CyclePosition;
        public float CycleLength;
        public float CompoundingInterval;
        public float AccumulatedInflationAmount;

        // Aliases for backward compatibility
        public float AccumulatedInflation { get => AccumulatedInflationAmount; set => AccumulatedInflationAmount = value; }
        public DateTime LastUpdateTime { get => LastUpdate; set => LastUpdate = value; }
    }

    /// <summary>
    /// Market conditions
    /// </summary>
    [Serializable]
    public class MarketConditions
    {
        public MarketTrend TrendDirection;
        public float VolatilityIndex;
        public float DemandPressure;
        public float SupplyConstraints;
        public float EconomicIndicator;
        public DateTime LastUpdate;
        public float ConfidenceLevel;

        // Aliases for backward compatibility
        public float Supply { get => SupplyConstraints; set => SupplyConstraints = value; }
        public float Demand { get => DemandPressure; set => DemandPressure = value; }
        public MarketTrend Trend { get => TrendDirection; set => TrendDirection = value; }
        public float Confidence { get => ConfidenceLevel; set => ConfidenceLevel = value; }
        public DateTime LastUpdateTime { get => LastUpdate; set => LastUpdate = value; }

        public MarketConditions() { }

        public MarketConditions(MarketConditions other)
        {
            TrendDirection = other.TrendDirection;
            VolatilityIndex = other.VolatilityIndex;
            DemandPressure = other.DemandPressure;
            SupplyConstraints = other.SupplyConstraints;
            EconomicIndicator = other.EconomicIndicator;
            LastUpdate = other.LastUpdate;
            ConfidenceLevel = other.ConfidenceLevel;
        }
    }

    /// <summary>
    /// External pricing cache
    /// </summary>
    [Serializable]
    public class ExternalPricingCache
    {
        public float LastUpdate;
        public bool DataValid;
        public Dictionary<string, float> CachedPrices = new Dictionary<string, float>();
    }

    /// <summary>
    /// Comprehensive pricing adjustment result
    /// </summary>
    [Serializable]
    public class PricingAdjustment
    {
        public float OriginalCost;
        public float AdjustedCost;
        public MalfunctionType MalfunctionType;
        public DateTime Timestamp;

        public float MarketAdjustedCost;
        public float MarketAdjustment;
        public float MarketAdjustmentPercent;

        public float InflationAdjustedCost;
        public float InflationAdjustment;
        public float InflationAdjustmentPercent;

        public float TotalAdjustment;
        public float TotalAdjustmentPercent;

        public MarketConditions MarketConditions;
        public float InflationRate;
    }

    /// <summary>
    /// Market trend enumeration
    /// </summary>
    public enum MarketTrend
    {
        Bearish,
        Stable,
        Bullish
    }

    /// <summary>
    /// Demand level enumeration
    /// </summary>
    public enum DemandLevel
    {
        VeryLow,
        Low,
        Normal,
        High,
        VeryHigh
    }

    /// <summary>
    /// Supply level enumeration
    /// </summary>
    public enum SupplyLevel
    {
        VeryLow,
        Low,
        Normal,
        High,
        VeryHigh
    }

    /// <summary>
    /// Market pricing statistics
    /// </summary>
    [Serializable]
    public struct MarketPricingStats
    {
        public int MarketAdjustmentsApplied;
        public int InflationAdjustmentsApplied;
        public int PricingAdjustmentsGenerated;
        public int MarketUpdatesPerformed;
        public int MarketPricingErrors;
        public int InflationCalculationErrors;
        public int MarketUpdateErrors;
        public int PricingWithoutMarketData;
        public int ExternalDataUpdates;

        public int TotalPricingOperations;
        public float TotalPricingOperationTime;
        public float AveragePricingOperationTime;
        public float MaxPricingOperationTime;

        public float TotalPriceAdjustment;
        public float AveragePriceAdjustment;
    }

    #endregion
}
