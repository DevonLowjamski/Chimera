using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy.Pricing
{
    /// <summary>
    /// Pricing and valuation-focused data structures extracted from EconomicDataStructures.cs
    /// Contains price tracking, company valuations, cost calculations, and pricing strategy systems
    /// Part of Phase 1 Foundation Data Structures refactoring
    /// </summary>

    #region Price Tracking and History

    [System.Serializable]
    public class PriceHistory
    {
        public string AssetId;
        public List<PricePoint> PricePoints = new List<PricePoint>();
        
        public void AddPrice(decimal price, DateTime time)
        {
            PricePoints.Add(new PricePoint { Price = price, Timestamp = time });
        }
    }

    [System.Serializable]
    public class PricePoint
    {
        public decimal Price;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class PriceDataPoint
    {
        public DateTime Timestamp;
        public decimal Price;
        public decimal Volume;
        public string Market;
        public string Symbol;
    }

    [System.Serializable]
    public class TradingVolume
    {
        public string AssetId;
        public List<VolumePoint> VolumePoints = new List<VolumePoint>();
        
        public void AddVolume(decimal volume, DateTime time)
        {
            VolumePoints.Add(new VolumePoint { Volume = volume, Timestamp = time });
        }
    }

    [System.Serializable]
    public class VolumePoint
    {
        public decimal Volume;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class VolumeDataPoint
    {
        public DateTime Timestamp;
        public decimal Volume;
        public decimal Value;
        public string Market;
        public string Symbol;
    }

    #endregion

    #region Valuation Systems

    [System.Serializable]
    public class CompanyValuation
    {
        public string ValuationId;
        public decimal EstimatedValue;
        public string ValuationMethod;
        public DateTime ValuationDate;
        public float ConfidenceLevel;
    }

    [System.Serializable]
    public class ValuationMethod
    {
        public string MethodName;
        public string Description;
        public float WeightingFactor;
        public decimal EstimatedValue;
    }

    [System.Serializable]
    public class ValuationEngine
    {
        public bool IsActive = true;
        public List<ValuationMethod> AvailableMethods = new List<ValuationMethod>();
        public DateTime LastValuation;
        
        public void Initialize() 
        { 
            IsActive = true;
            SetupDefaultMethods();
        }
        
        public void Initialize(bool enableValuation) 
        { 
            IsActive = enableValuation;
            if (IsActive)
            {
                SetupDefaultMethods();
            }
        }
        
        public void Shutdown() 
        { 
            IsActive = false;
            AvailableMethods.Clear();
        }
        
        public CompanyValuation PerformValuation(List<ValuationMethod> methods) 
        { 
            LastValuation = DateTime.Now;
            return new CompanyValuation
            {
                ValuationId = Guid.NewGuid().ToString(),
                ValuationDate = LastValuation,
                ConfidenceLevel = 0.75f
            };
        }
        
        private void SetupDefaultMethods()
        {
            AvailableMethods.Add(new ValuationMethod
            {
                MethodName = "Discounted Cash Flow",
                Description = "DCF valuation method",
                WeightingFactor = 0.4f
            });
            AvailableMethods.Add(new ValuationMethod
            {
                MethodName = "Market Multiple",
                Description = "Comparable company analysis",
                WeightingFactor = 0.3f
            });
            AvailableMethods.Add(new ValuationMethod
            {
                MethodName = "Asset Based",
                Description = "Net asset value approach",
                WeightingFactor = 0.3f
            });
        }
    }

    [System.Serializable]
    public class IntelligenceValue
    {
        public string ValueId;
        public float StrategicValue;
        public float TacticalValue;
        public float Actionability;
        public float Reliability;
        public DateTime ExpirationDate;
    }

    #endregion

    #region Pricing Warfare and Competition

    [System.Serializable]
    public class PriceWarfareResult
    {
        public float Effectiveness;
        public decimal PriceReduction;
        public decimal MarketShareGained;
        public decimal RevenueImpact;
        public List<string> CompetitorResponses = new List<string>();
    }

    [System.Serializable]
    public class SupplyDisruptionResult
    {
        public float Effectiveness;
        public float SupplyChainImpact;
        public decimal CostIncrease;
        public int DelayDays;
        public List<string> AffectedSuppliers = new List<string>();
    }

    [System.Serializable]
    public class TalentPoachingResult
    {
        public float Effectiveness;
        public int TalentAcquired;
        public decimal CompensationCost;
        public float CompetitorWeakening;
        public List<string> KeyHires = new List<string>();
    }

    #endregion

    #region Pricing Strategy and Negotiation

    [System.Serializable]
    public class PricingStrategy
    {
        public string StrategyId;
        public string StrategyName;
        public PricingModel Model = PricingModel.MarketBased;
        public decimal BasePrice = 100m;
        public float MarginPercentage = 0.2f;
        public List<PricingRule> Rules = new List<PricingRule>();
        public bool IsActive = true;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class PricingRule
    {
        public string RuleId;
        public string RuleName;
        public PricingCondition Condition = PricingCondition.Volume;
        public float Threshold = 100f;
        public PricingAction Action = PricingAction.Discount;
        public float ActionValue = 0.1f;
        public bool IsEnabled = true;
    }

    [System.Serializable]
    public class NegotiationStrategy
    {
        public string StrategyId;
        public string Approach;
        public decimal TargetPrice;
        public decimal MaxPrice;
        public decimal MinPrice;
        public List<string> KeyTerms = new List<string>();
        public NegotiationTactic PrimaryTactic = NegotiationTactic.Competitive;
        public float FlexibilityFactor = 0.5f;
    }

    [System.Serializable]
    public class CostStructure
    {
        public string ProductId;
        public decimal DirectMaterialCost = 50m;
        public decimal DirectLaborCost = 30m;
        public decimal ManufacturingOverhead = 20m;
        public decimal TotalCost = 100m;
        public float ProfitMargin = 0.25f;
        public decimal SellingPrice = 125m;
        public DateTime LastCalculated;
    }

    [System.Serializable]
    public class PriceOptimization
    {
        public string OptimizationId;
        public string ProductId;
        public decimal CurrentPrice = 100m;
        public decimal OptimalPrice = 110m;
        public float DemandElasticity = -1.2f;
        public float RevenueIncrease = 0.08f;
        public float ConfidenceLevel = 0.8f;
        public List<string> Constraints = new List<string>();
        public DateTime CalculationDate;
    }

    #endregion

    #region Financial Structure and Costs

    [System.Serializable]
    public class FinancingStructure
    {
        public string StructureId;
        public decimal CashComponent;
        public decimal StockComponent;
        public decimal DebtComponent;
        public string FinancingSource;
        public FinancingMethod Method = FinancingMethod.Mixed;
        public float InterestRate = 0.05f;
        public int TermMonths = 60;
    }

    [System.Serializable]
    public class IntegrationPlan
    {
        public string PlanId;
        public int TimelineMonths;
        public List<string> KeyMilestones = new List<string>();
        public decimal IntegrationCost;
        public float SuccessProbability;
        public List<CostCategory> CostBreakdown = new List<CostCategory>();
    }

    [System.Serializable]
    public class CostCategory
    {
        public string CategoryName;
        public decimal EstimatedCost;
        public decimal ActualCost;
        public CostType Type = CostType.OneTime;
        public string Description;
    }

    [System.Serializable]
    public class FeeStructure
    {
        public string FeeId;
        public string FeeName;
        public FeeType Type = FeeType.Fixed;
        public decimal Amount = 10m;
        public float Percentage = 0.01f;
        public bool IsRecurring = false;
        public FrequencyType Frequency = FrequencyType.Monthly;
        public DateTime EffectiveDate;
        public DateTime ExpirationDate;
    }

    #endregion

    #region Supporting Enums

    public enum PricingModel
    {
        CostPlus,
        MarketBased,
        ValueBased,
        Competitive,
        Dynamic,
        Penetration,
        Skimming,
        Bundle
    }

    public enum PricingCondition
    {
        Volume,
        CustomerType,
        TimeOfDay,
        Season,
        Inventory,
        Competition,
        Demand,
        Geography
    }

    public enum PricingAction
    {
        Discount,
        Premium,
        Fixed,
        Percentage,
        Bundling,
        Loyalty,
        Seasonal,
        Clearance
    }

    public enum NegotiationTactic
    {
        Competitive,
        Collaborative,
        Accommodating,
        Avoiding,
        Compromising,
        Aggressive,
        Analytical,
        Emotional
    }

    public enum FinancingMethod
    {
        Cash,
        Stock,
        DebtFinancing,
        Mixed,
        LeveragedBuyout,
        VentureCapital,
        PrivateEquity,
        Crowdfunding
    }

    public enum CostType
    {
        OneTime,
        Recurring,
        Variable,
        Fixed,
        SemiVariable,
        Overhead,
        Direct,
        Indirect
    }

    public enum FeeType
    {
        Fixed,
        Percentage,
        Tiered,
        Flat,
        Transaction,
        Subscription,
        Usage,
        Performance
    }

    public enum FrequencyType
    {
        OneTime,
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annually,
        PerTransaction,
        PerUse
    }

    public enum OrderType
    {
        Market,
        Limit,
        Stop,
        StopLimit,
        TrailingStop,
        FillOrKill,
        ImmediateOrCancel,
        AllOrNone
    }

    public enum TradeStatus
    {
        Pending,
        Executed,
        Rejected,
        Cancelled,
        Failed,
        PartiallyFilled,
        Expired
    }

    public enum OrderStatus
    {
        Pending,
        PartiallyFilled,
        Filled,
        Cancelled,
        Rejected,
        Expired,
        Open,
        Closed
    }

    public enum AssetType
    {
        Stock,
        Bond,
        Commodity,
        Currency,
        CryptoCurrency,
        RealEstate,
        Derivative,
        MutualFund,
        ETF,
        Option,
        Future,
        Cannabis
    }

    #endregion

    #region Price Analytics and Calculations

    [System.Serializable]
    public class PriceAnalytics
    {
        public string AnalyticsId;
        public string AssetId;
        public decimal MovingAverage20 = 100m;
        public decimal MovingAverage50 = 105m;
        public decimal MovingAverage200 = 110m;
        public decimal Volatility = 0.15m;
        public decimal Beta = 1.2m;
        public decimal RSI = 50m; // Relative Strength Index
        public decimal MACD = 0m; // Moving Average Convergence Divergence
        public DateTime LastCalculated;
    }

    [System.Serializable]
    public class PriceLevel
    {
        public string LevelId;
        public decimal Price;
        public PriceLevelType Type = PriceLevelType.Support;
        public float Strength = 0.5f;
        public int TouchCount = 1;
        public DateTime FirstTouch;
        public DateTime LastTouch;
        public bool IsActive = true;
    }

    [System.Serializable]
    public class PriceForecast
    {
        public string ForecastId;
        public string AssetId;
        public decimal CurrentPrice = 100m;
        public decimal PredictedPrice = 105m;
        public decimal UpperBound = 110m;
        public decimal LowerBound = 100m;
        public float Confidence = 0.7f;
        public int ForecastDays = 30;
        public ForecastMethod Method = ForecastMethod.TechnicalAnalysis;
        public DateTime CreatedDate;
        public DateTime TargetDate;
    }

    public enum PriceLevelType
    {
        Support,
        Resistance,
        Pivot,
        FibonacciRetracement,
        MovingAverage,
        TrendLine,
        Breakout,
        Breakdown
    }

    public enum ForecastMethod
    {
        TechnicalAnalysis,
        FundamentalAnalysis,
        MachineLearning,
        StatisticalModel,
        ExpertOpinion,
        Hybrid,
        Momentum,
        MeanReversion
    }

    #endregion

    #region Helper Data Structures

    [System.Serializable]
    public class PriceRange
    {
        public decimal MinPrice;
        public decimal MaxPrice;
        public decimal CurrentPrice;
        public DateTime ValidFrom;
        public DateTime ValidUntil;
    }

    [System.Serializable]
    public class MarketPrice
    {
        public string Symbol;
        public decimal BidPrice;
        public decimal AskPrice;
        public decimal LastPrice;
        public decimal Volume;
        public DateTime LastUpdate;
        public string Exchange;
    }

    [System.Serializable]
    public class PriceAlert
    {
        public string AlertId;
        public string AssetId;
        public decimal TriggerPrice;
        public PriceAlertType Type = PriceAlertType.Above;
        public bool IsActive = true;
        public DateTime CreatedDate;
        public DateTime TriggeredDate;
        public string NotificationMessage;
    }

    public enum PriceAlertType
    {
        Above,
        Below,
        Change,
        PercentChange,
        Volume,
        MovingAverage,
        RSI,
        MACD
    }

    #endregion
}