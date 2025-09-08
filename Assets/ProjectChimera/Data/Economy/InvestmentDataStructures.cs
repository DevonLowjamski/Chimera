using System;
using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Economy.Indicators;

namespace ProjectChimera.Data.Economy.Investments
{
    /// <summary>
    /// Investment and financial planning-focused data structures extracted from EconomicDataStructures.cs
    /// Contains portfolio management, investment positions, joint ventures, and financial planning systems
    /// Part of Phase 1 Foundation Data Structures refactoring
    /// </summary>

    #region Portfolio Management

    [System.Serializable]
    public class InvestmentPortfolio
    {
        public string PlayerId;
        public string AccountId;
        public decimal CashPosition = 10000m;
        public Dictionary<string, StockPosition> StockHoldings = new Dictionary<string, StockPosition>();
        public Dictionary<string, CommodityPosition> CommodityHoldings = new Dictionary<string, CommodityPosition>();
        public Dictionary<string, FuturesPosition> FuturesPositions = new Dictionary<string, FuturesPosition>();
        public Dictionary<string, OptionsPosition> OptionsPositions = new Dictionary<string, OptionsPosition>();
        public Dictionary<string, RealEstatePosition> RealEstateHoldings = new Dictionary<string, RealEstatePosition>();
        public Dictionary<string, BondPosition> BondHoldings = new Dictionary<string, BondPosition>();
        public decimal TotalValue = 0m;
        public DateTime LastUpdated;
        public PortfolioStrategy Strategy = PortfolioStrategy.Balanced;
        public RiskTolerance RiskProfile = RiskTolerance.Moderate;

        // Additional property for Holdings collection
        public List<InvestmentHolding> Holdings
        {
            get
            {
                var holdings = new List<InvestmentHolding>();
                foreach (var stock in StockHoldings.Values)
                {
                    holdings.Add(new InvestmentHolding {
                        HoldingId = stock.Symbol,
                        Symbol = stock.Symbol,
                        CurrentValue = (float)stock.CurrentValue,
                        HoldingType = InvestmentAssetType.Stock
                    });
                }
                foreach (var commodity in CommodityHoldings.Values)
                {
                    holdings.Add(new InvestmentHolding {
                        HoldingId = commodity.CommodityType,
                        Symbol = commodity.CommodityType,
                        CurrentValue = (float)commodity.CurrentValue,
                        HoldingType = InvestmentAssetType.Commodity
                    });
                }
                return holdings;
            }
        }
    }

    [System.Serializable]
    public class InvestmentHolding
    {
        public string HoldingId;
        public string Symbol;
        public float CurrentValue = 1000f;
        public int Quantity = 100;
        public DateTime PurchaseDate;
        public float PurchasePrice = 10f;
        public InvestmentAssetType HoldingType = InvestmentAssetType.Stock;
        public float GainLoss = 0f;
        public float GainLossPercentage = 0f;
        public AssetClassification Classification = AssetClassification.Equity;
    }

    [System.Serializable]
    public class StockPosition
    {
        public string Symbol;
        public decimal Shares = 100m;
        public decimal AveragePrice = 50m;
        public decimal CurrentPrice = 55m;
        public decimal TotalValue = 5500m;
        public DateTime LastUpdated;
        public StockSector Sector = StockSector.Technology;
        public DividendInfo DividendInfo;

        public float CurrentValue => (float)TotalValue;
        public decimal UnrealizedGain => (CurrentPrice - AveragePrice) * Shares;
        public float GainLossPercentage => AveragePrice > 0 ? (float)((CurrentPrice - AveragePrice) / AveragePrice * 100) : 0f;
    }

    [System.Serializable]
    public class CommodityPosition
    {
        public string CommodityType;
        public decimal Quantity = 100m;
        public decimal AveragePrice = 25m;
        public decimal CurrentPrice = 28m;
        public decimal TotalValue = 2800m;
        public DateTime LastUpdated;
        public CommodityCategory Category = CommodityCategory.Energy;
        public string Exchange;

        public float CurrentValue => (float)TotalValue;
        public decimal UnrealizedGain => (CurrentPrice - AveragePrice) * Quantity;
    }

    [System.Serializable]
    public class FuturesPosition
    {
        public string ContractSymbol;
        public decimal Contracts = 10m;
        public decimal EntryPrice = 1500m;
        public decimal CurrentPrice = 1550m;
        public DateTime ExpirationDate;
        public decimal MarginRequired = 5000m;
        public FuturesType Type = FuturesType.Commodity;
        public decimal ContractSize = 100m;

        public decimal UnrealizedPnL => (CurrentPrice - EntryPrice) * Contracts * ContractSize;
        public decimal TotalValue => CurrentPrice * Contracts * ContractSize;
    }

    [System.Serializable]
    public class OptionsPosition
    {
        public string OptionSymbol;
        public decimal Contracts = 10m;
        public decimal Premium = 2.5m;
        public decimal StrikePrice = 100m;
        public DateTime ExpirationDate;
        public OptionType OptionType = OptionType.Call;
        public OptionStyle Style = OptionStyle.American;
        public decimal ImpliedVolatility = 0.25m;
        public decimal Delta = 0.5m;

        public decimal TotalValue => Premium * Contracts * 100; // Options contract multiplier
        public decimal TimeValue => Premium - Math.Max(0, OptionType == OptionType.Call ?
            StrikePrice - CurrentUnderlyingPrice : CurrentUnderlyingPrice - StrikePrice);
        public decimal CurrentUnderlyingPrice = 105m; // Would be fetched from market data
    }

    [System.Serializable]
    public class RealEstatePosition
    {
        public string PropertyId;
        public string PropertyType;
        public decimal PurchasePrice = 200000m;
        public decimal CurrentValue = 220000m;
        public DateTime PurchaseDate;
        public decimal MonthlyIncome = 1500m;
        public PropertyCategory Category = PropertyCategory.Residential;
        public string Location;
        public float CapitalizationRate = 0.06f;

        public decimal UnrealizedGain => CurrentValue - PurchasePrice;
        public decimal AnnualIncome => MonthlyIncome * 12;
        public float ROI => PurchasePrice > 0 ? (float)(AnnualIncome / PurchasePrice * 100) : 0f;
    }

    [System.Serializable]
    public class BondPosition
    {
        public string BondSymbol;
        public decimal FaceValue = 1000m;
        public decimal PurchasePrice = 950m;
        public decimal CurrentPrice = 980m;
        public decimal CouponRate = 0.05m; // 5%
        public DateTime MaturityDate;
        public DateTime LastCouponDate;
        public BondType Type = BondType.Corporate;
        public CreditRating Rating = CreditRating.BBB;

        public decimal YieldToMaturity { get; set; } = 0.055m; // 5.5%
        public decimal AccruedInterest { get; set; } = 25m;
        public decimal TotalValue => CurrentPrice + AccruedInterest;
    }

    [System.Serializable]
    public class DividendInfo
    {
        public decimal DividendPerShare = 0.5m;
        public DividendFrequency Frequency = DividendFrequency.Quarterly;
        public DateTime LastPaymentDate;
        public DateTime NextPaymentDate;
        public float DividendYield = 0.02f; // 2%
        public bool IsDividendEligible = true;
    }

    #endregion

    #region Investment Analysis and Performance

    [System.Serializable]
    public class PortfolioAnalytics
    {
        public string PortfolioId;
        public decimal TotalValue = 100000m;
        public decimal TotalGain = 5000m;
        public float TotalReturn = 0.05f; // 5%
        public float AnnualizedReturn = 0.12f; // 12%
        public float Volatility = 0.15f; // 15%
        public float SharpeRatio = 0.8f;
        public float MaxDrawdown = 0.08f; // 8%
        public DateTime AnalysisDate;
        public List<PerformanceMetric> Metrics = new List<PerformanceMetric>();
    }



    [System.Serializable]
    public class InvestmentObjective
    {
        public string ObjectiveId;
        public string Name;
        public ObjectiveType Type = ObjectiveType.Growth;
        public decimal TargetAmount = 50000m;
        public DateTime TargetDate;
        public decimal CurrentAmount = 25000m;
        public float Progress = 0.5f; // 50%
        public InvestmentTimeHorizon TimeHorizon = InvestmentTimeHorizon.LongTerm;
    }



    [System.Serializable]
    public class RiskRecommendation
    {
        public string RecommendationId;
        public string Description;
        public RecommendationType Type = RecommendationType.Diversification;
        public PriorityLevel Priority = PriorityLevel.Medium;
        public decimal EstimatedImpact = 1000m;
        public bool IsImplemented = false;
    }

    [System.Serializable]
    public class FundamentalSignal
    {
        public string SignalId;
        public string AssetSymbol;
        public SignalType Type = SignalType.Buy;
        public float Strength = 0.7f;
        public float Confidence = 0.8f;
        public DateTime GeneratedDate;
        public string Source;
        public Dictionary<string, float> Indicators = new Dictionary<string, float>();
        public string Rationale;
    }

    #endregion

    #region Joint Ventures and Strategic Investments

    [System.Serializable]
    public class JointVenture
    {
        public string VentureId;
        public string VentureName;
        public List<string> PartnerIds = new List<string>();
        public string BusinessObjective;
        public DateTime StartDate;
        public DateTime? EndDate;
        public decimal TotalInvestment = 100000m;
        public Dictionary<string, decimal> PartnerContributions = new Dictionary<string, decimal>();
        public JointVentureStatus Status = JointVentureStatus.Active;
        public VentureType Type = VentureType.Business;
        public decimal CurrentValue = 120000m;
        public float ROI => TotalInvestment > 0 ? (float)((CurrentValue - TotalInvestment) / TotalInvestment * 100) : 0f;

        // Additional properties needed by EnhancedEconomicGamingManager
        public string InitiatorId;
        public List<string> Partners = new List<string>();
        public Dictionary<string, decimal> ResourceAllocation = new Dictionary<string, decimal>();
        public DateTime CreationDate;
        public bool IsActive = true;
    }

    [System.Serializable]
    public class JointVentureResult
    {
        public bool Success = false;
        public string Reason;
        public JointVenture JointVenture;
        public string VentureId;
        public decimal FinalValue = 0m;
        public DateTime CompletionDate;
        public Dictionary<string, decimal> PartnerReturns = new Dictionary<string, decimal>();
    }

    [System.Serializable]
    public class JointVentureProposal
    {
        public string ProposalId;
        public string ProposingPlayerId;
        public List<string> TargetPartners = new List<string>();
        public string BusinessObjective;
        public decimal RequiredInvestment = 50000m;
        public Dictionary<string, decimal> ProposedContributions = new Dictionary<string, decimal>();
        public int ProjectedDurationMonths = 12;
        public decimal ExpectedReturns = 65000m;
        public RiskAssessment Risks;
        public DateTime ProposalDate;
        public ProposalStatus Status = ProposalStatus.Pending;
        public VentureCategory Category = VentureCategory.Technology;

        // Additional properties needed by EnhancedEconomicGamingManager
        public string VentureName;
        public string Objective;
        public List<string> Partners = new List<string>();
        public Dictionary<string, decimal> ResourceContributions = new Dictionary<string, decimal>();

        public decimal ProjectedROI => RequiredInvestment > 0 ? (ExpectedReturns - RequiredInvestment) / RequiredInvestment * 100 : 0m;
    }

    [System.Serializable]
    public class StrategicAlliance
    {
        public string AllianceId;
        public string AllianceName;
        public List<string> MemberIds = new List<string>();
        public AllianceType AllianceType = AllianceType.Business;
        public DateTime FormationDate;
        public List<string> SharedObjectives = new List<string>();
        public AllianceStatus Status = AllianceStatus.Active;
        public decimal TotalInvestment = 0m;
        public AlliangeGoal PrimaryGoal = AlliangeGoal.MarketExpansion;
    }

    #endregion

    #region Financial Planning and Structuring

    [System.Serializable]
    public class FinancialPlan
    {
        public string PlanId;
        public string PlayerId;
        public string PlanName;
        public PlanType Type = PlanType.Retirement;
        public DateTime CreationDate;
        public DateTime TargetDate;
        public decimal TargetAmount = 500000m;
        public decimal CurrentAmount = 150000m;
        public List<InvestmentObjective> Objectives = new List<InvestmentObjective>();
        public List<PlanningStrategy> Strategies = new List<PlanningStrategy>();
        public float Progress => TargetAmount > 0 ? (float)(CurrentAmount / TargetAmount * 100) : 0f;
    }

    [System.Serializable]
    public class PlanningStrategy
    {
        public string StrategyId;
        public string Name;
        public StrategyType Type = StrategyType.AssetAllocation;
        public Dictionary<string, float> AllocationTargets = new Dictionary<string, float>();
        public float RiskTolerance = 0.5f;
        public int RebalancingFrequency = 3; // months
        public bool IsActive = true;
    }

    [System.Serializable]
    public class FinancingOptions
    {
        public List<string> AvailableSources = new List<string>();
        public decimal CashAvailable = 25000m;
        public decimal DebtCapacity = 100000m;
        public decimal EquityCapacity = 75000m;
        public string PreferredStructure;
        public List<FinancingSource> Sources = new List<FinancingSource>();
        public CapitalStructure OptimalStructure;
    }

    [System.Serializable]
    public class FinancingSource
    {
        public string SourceId;
        public string SourceName;
        public FinancingType Type = FinancingType.Debt;
        public decimal Amount = 50000m;
        public float InterestRate = 0.05f; // 5%
        public int TermMonths = 60;
        public List<string> Requirements = new List<string>();
        public bool IsAvailable = true;
    }

    [System.Serializable]
    public class CapitalStructure
    {
        public string StructureId;
        public float DebtRatio = 0.4f; // 40% debt
        public float EquityRatio = 0.6f; // 60% equity
        public decimal TotalCapital = 250000m;
        public float WeightedAverageCostOfCapital = 0.08f; // 8%
        public Dictionary<string, float> ComponentWeights = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class InvestmentPolicy
    {
        public string PolicyId;
        public string PolicyName;
        public List<AssetClassLimit> AssetLimits = new List<AssetClassLimit>();
        public RiskTolerance MaxRiskTolerance = RiskTolerance.Moderate;
        public List<string> ProhibitedInvestments = new List<string>();
        public RebalancingPolicy RebalancingPolicy;
        public bool RequiresDiversification = true;
    }

    [System.Serializable]
    public class AssetClassLimit
    {
        public AssetClassification AssetClass = AssetClassification.Equity;
        public float MinAllocation = 0.0f;
        public float MaxAllocation = 0.7f;
        public float TargetAllocation = 0.6f;
        public bool IsRequired = false;
    }

    [System.Serializable]
    public class RebalancingPolicy
    {
        public RebalancingType Type = RebalancingType.Periodic;
        public int FrequencyMonths = 6;
        public float ThresholdPercentage = 0.05f; // 5% drift
        public bool AutoRebalance = false;
        public List<string> TriggerConditions = new List<string>();
    }

    #endregion

    #region Supporting Enums

    public enum InvestmentAssetType
    {
        Stock,
        Bond,
        Commodity,
        Futures,
        Options,
        RealEstate,
        Cash,
        Cryptocurrency,
        Mutual_Fund,
        ETF
    }

    public enum AssetClassification
    {
        Equity,
        FixedIncome,
        Commodity,
        RealEstate,
        Cash,
        Alternative,
        Derivative
    }

    public enum PortfolioStrategy
    {
        Conservative,
        Balanced,
        Growth,
        Aggressive,
        Income,
        Speculation,
        Hedging
    }

    public enum RiskTolerance
    {
        VeryConservative,
        Conservative,
        Moderate,
        Aggressive,
        VeryAggressive
    }

    public enum StockSector
    {
        Technology,
        Healthcare,
        Finance,
        Energy,
        Consumer,
        Industrial,
        Utilities,
        Materials,
        Telecommunications,
        RealEstate
    }

    public enum CommodityCategory
    {
        Energy,
        Metals,
        Agriculture,
        Livestock,
        SoftCommodities
    }

    public enum FuturesType
    {
        Commodity,
        Financial,
        Currency,
        Index,
        Interest_Rate
    }

    public enum OptionType
    {
        Call,
        Put
    }

    public enum OptionStyle
    {
        American,
        European,
        Bermuda
    }

    public enum PropertyCategory
    {
        Residential,
        Commercial,
        Industrial,
        Land,
        REIT
    }

    public enum BondType
    {
        Government,
        Corporate,
        Municipal,
        Treasury,
        High_Yield,
        Convertible
    }

    public enum CreditRating
    {
        AAA,
        AA,
        A,
        BBB,
        BB,
        B,
        CCC,
        CC,
        C,
        D
    }

    public enum DividendFrequency
    {
        Monthly,
        Quarterly,
        SemiAnnual,
        Annual,
        Special
    }

    public enum MetricType
    {
        Return,
        Risk,
        Ratio,
        Performance,
        Volatility
    }

    public enum TimePeriod
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        YearToDate,
        OneYear,
        ThreeYear,
        FiveYear,
        TenYear,
        Inception
    }

    public enum ObjectiveType
    {
        Growth,
        Income,
        Preservation,
        Speculation,
        Retirement,
        Education,
        Emergency
    }

    public enum InvestmentTimeHorizon
    {
        ShortTerm,   // < 2 years
        MediumTerm,  // 2-7 years
        LongTerm     // > 7 years
    }

    public enum RecommendationType
    {
        Diversification,
        RiskReduction,
        AssetAllocation,
        Rebalancing,
        TaxOptimization,
        CostReduction
    }

    public enum PriorityLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum SignalType
    {
        Buy,
        Sell,
        Hold,
        StrongBuy,
        StrongSell
    }

    public enum JointVentureStatus
    {
        Proposed,
        Negotiating,
        Active,
        Suspended,
        Completed,
        Dissolved
    }

    public enum VentureType
    {
        Business,
        Investment,
        Research,
        Technology,
        Real_Estate,
        Infrastructure
    }

    public enum ProposalStatus
    {
        Draft,
        Pending,
        UnderReview,
        Approved,
        Rejected,
        Withdrawn,
        Expired
    }

    public enum VentureCategory
    {
        Technology,
        Manufacturing,
        Services,
        Real_Estate,
        Energy,
        Healthcare,
        Finance,
        Retail
    }

    public enum AllianceType
    {
        Strategic,
        Business,
        Technology,
        Marketing,
        Distribution,
        Research
    }

    public enum AllianceStatus
    {
        Forming,
        Active,
        Suspended,
        Dissolved,
        Expired
    }

    public enum AlliangeGoal
    {
        MarketExpansion,
        CostReduction,
        TechnologySharing,
        RiskMitigation,
        ResourcePooling
    }

    public enum PlanType
    {
        Retirement,
        Education,
        Emergency,
        Investment,
        Estate,
        Tax,
        Insurance
    }



    public enum FinancingType
    {
        Debt,
        Equity,
        Convertible,
        Hybrid,
        Mezzanine,
        Grant
    }

    public enum RebalancingType
    {
        Periodic,
        Threshold,
        Tactical,
        Calendar,
        Opportunistic
    }

    #endregion
}
