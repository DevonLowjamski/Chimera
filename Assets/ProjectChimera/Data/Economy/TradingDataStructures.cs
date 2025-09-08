using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy.Trading
{
    /// <summary>
    /// Trading-focused data structures extracted from EconomicDataStructures.cs
    /// Contains trading operations, portfolios, orders, strategies, and execution systems
    /// Part of Phase 1 Foundation Data Structures refactoring
    /// </summary>

    #region Trading Operations and Management

    [System.Serializable]
    public class TradingOperation
    {
        public string OperationId;
        public TradingOperationType Type = TradingOperationType.Buy;
        public string Product;
        public float Quantity = 100f;
        public float PricePerUnit = 10f;
        public float TotalValue = 1000f;
        public TradingStatus Status = TradingStatus.Pending;
        public DateTime CreatedDate;
        public DateTime ExecutedDate;
        public string TraderId;
        public TradingPlatform Platform = TradingPlatform.Internal;
    }

    [System.Serializable]
    public class TradingPortfolio
    {
        public string PortfolioId;
        public string OwnerId;
        public List<TradingPosition> Positions = new List<TradingPosition>();
        public float TotalValue = 0f;
        public float CashBalance = 10000f;
        public float TotalGain = 0f;
        public float TotalLoss = 0f;
        public DateTime LastUpdated;
        public PortfolioStrategy Strategy = PortfolioStrategy.Balanced;
        public RiskProfile RiskProfile = RiskProfile.Moderate;
    }

    [System.Serializable]
    public class TradingPosition
    {
        public string PositionId;
        public string Product;
        public float Quantity = 100f;
        public float AveragePrice = 10f;
        public float CurrentPrice = 11f;
        public float UnrealizedGain = 100f;
        public float RealizedGain = 0f;
        public DateTime EntryDate;
        public PositionType Type = PositionType.Long;
        public bool IsOpen = true;
    }

    [System.Serializable]
    public class TradingOrder
    {
        public string OrderId;
        public string TraderId;
        public string Product;
        public OrderType Type = OrderType.Market;
        public OrderSide Side = OrderSide.Buy;
        public float Quantity = 100f;
        public float Price = 10f;
        public float FilledQuantity = 0f;
        public OrderStatus Status = OrderStatus.Open;
        public DateTime CreatedTime;
        public DateTime ExpiryTime;
        public string ParentOrderId;
        public List<string> ChildOrderIds = new List<string>();
    }

    [System.Serializable]
    public class TradingAccount
    {
        public string AccountId;
        public string UserId;
        public AccountType Type = AccountType.Standard;
        public float Balance = 10000f;
        public float AvailableBalance = 10000f;
        public float MarginUsed = 0f;
        public float MarginAvailable = 0f;
        public AccountStatus Status = AccountStatus.Active;
        public DateTime CreatedDate;
        public DateTime LastActivityDate;
        public TradingPermissions Permissions;
    }

    [System.Serializable]
    public class TradingSession
    {
        public string SessionId;
        public string TraderId;
        public DateTime StartTime;
        public DateTime EndTime;
        public List<TradingOperation> Operations = new List<TradingOperation>();
        public float SessionGain = 0f;
        public float SessionLoss = 0f;
        public int TotalTrades = 0;
        public int WinningTrades = 0;
        public int LosingTrades = 0;
        public SessionStatus Status = SessionStatus.Active;
    }

    #endregion

    #region Trading Strategies and Algorithms

    [System.Serializable]
    public class TradingStrategy
    {
        public string StrategyId;
        public string StrategyName;
        public StrategyType Type = StrategyType.Manual;
        public List<TradingRule> Rules = new List<TradingRule>();
        public List<TradingSignal> Signals = new List<TradingSignal>();
        public StrategyPerformance Performance;
        public bool IsActive = true;
        public float RiskTolerance = 0.05f;
        public DateTime CreatedDate;
        public DateTime LastModified;
    }

    [System.Serializable]
    public class TradingRule
    {
        public string RuleId;
        public string RuleName;
        public RuleType Type = RuleType.Entry;
        public string Condition;
        public string Action;
        public float Priority = 1f;
        public bool IsEnabled = true;
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class TradingSignal
    {
        public string SignalId;
        public SignalType Type = SignalType.Buy;
        public string Product;
        public float Strength = 0.5f;
        public float Confidence = 0.7f;
        public DateTime GeneratedTime;
        public DateTime ExpiryTime;
        public string Source;
        public SignalStatus Status = SignalStatus.Active;
        public Dictionary<string, float> Indicators = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class AlgorithmicTrading
    {
        public string AlgorithmId;
        public string AlgorithmName;
        public AlgorithmType Type = AlgorithmType.MeanReversion;
        public List<TradingParameter> Parameters = new List<TradingParameter>();
        public AlgorithmPerformance Performance;
        public bool IsRunning = false;
        public DateTime StartTime;
        public DateTime LastExecutionTime;
        public ExecutionFrequency Frequency = ExecutionFrequency.Hourly;
    }

    [System.Serializable]
    public class TradingParameter
    {
        public string ParameterName;
        public ParameterType Type = ParameterType.Float;
        public object Value;
        public object MinValue;
        public object MaxValue;
        public object DefaultValue;
        public string Description;
        public bool IsOptimizable = true;
    }

    [System.Serializable]
    public class BacktestingResult
    {
        public string BacktestId;
        public string StrategyId;
        public DateTime StartDate;
        public DateTime EndDate;
        public float InitialCapital = 10000f;
        public float FinalCapital = 11000f;
        public float TotalReturn = 0.1f;
        public float MaxDrawdown = 0.05f;
        public float SharpeRatio = 1.2f;
        public int TotalTrades = 100;
        public float WinRate = 0.65f;
        public List<TradeResult> Trades = new List<TradeResult>();
    }

    #endregion

    #region Market Making and Liquidity

    [System.Serializable]
    public class MarketMaker
    {
        public string MakerId;
        public string MakerName;
        public List<string> SupportedProducts = new List<string>();
        public float MinSpread = 0.01f;
        public float MaxSpread = 0.1f;
        public float InventoryLimit = 1000f;
        public float CurrentInventory = 500f;
        public MakerStatus Status = MakerStatus.Active;
        public MarketMakingStrategy Strategy;
        public float ProfitToday = 0f;
    }

    [System.Serializable]
    public class LiquidityProvider
    {
        public string ProviderId;
        public string ProviderName;
        public List<LiquidityPool> Pools = new List<LiquidityPool>();
        public float TotalLiquidity = 100000f;
        public float AvailableLiquidity = 80000f;
        public float DailyVolume = 50000f;
        public ProviderTier Tier = ProviderTier.Tier1;
        public float RewardRate = 0.02f;
        public ProviderStatus Status = ProviderStatus.Active;
    }

    [System.Serializable]
    public class LiquidityPool
    {
        public string PoolId;
        public string Product;
        public float TotalLiquidity = 10000f;
        public float AvailableLiquidity = 8000f;
        public float UtilizationRate = 0.2f;
        public float RewardRate = 0.025f;
        public List<string> ParticipantIds = new List<string>();
        public PoolStatus Status = PoolStatus.Active;
        public DateTime CreatedDate;
    }

    [System.Serializable]
    public class OrderBook
    {
        public string Product;
        public List<OrderBookEntry> BidOrders = new List<OrderBookEntry>();
        public List<OrderBookEntry> AskOrders = new List<OrderBookEntry>();
        public float BestBid = 0f;
        public float BestAsk = 0f;
        public float Spread = 0f;
        public float MidPrice = 0f;
        public DateTime LastUpdated;
        public float TotalBidVolume = 0f;
        public float TotalAskVolume = 0f;
    }

    [System.Serializable]
    public class OrderBookEntry
    {
        public float Price;
        public float Quantity;
        public int OrderCount;
        public DateTime Timestamp;
        public string MakerId;
    }

    #endregion

    #region Trading Execution and Settlement

    [System.Serializable]
    public class TradeExecution
    {
        public string ExecutionId;
        public string OrderId;
        public string Product;
        public float Quantity = 100f;
        public float Price = 10f;
        public float Commission = 5f;
        public DateTime ExecutionTime;
        public string CounterpartyId;
        public ExecutionVenue Venue = ExecutionVenue.Internal;
        public SettlementStatus SettlementStatus = SettlementStatus.Pending;
        public DateTime SettlementDate;
    }

    [System.Serializable]
    public class TradeSettlement
    {
        public string SettlementId;
        public string TradeId;
        public SettlementType Type = SettlementType.DVP; // Delivery vs Payment
        public float CashAmount = 1000f;
        public float SecurityQuantity = 100f;
        public DateTime SettlementDate;
        public SettlementStatus Status = SettlementStatus.Pending;
        public string ClearingHouse;
        public List<SettlementInstruction> Instructions = new List<SettlementInstruction>();
    }

    [System.Serializable]
    public class SettlementInstruction
    {
        public string InstructionId;
        public InstructionType Type = InstructionType.Payment;
        public string FromAccount;
        public string ToAccount;
        public float Amount = 1000f;
        public string Product;
        public InstructionStatus Status = InstructionStatus.Pending;
        public DateTime CreatedTime;
        public DateTime ExecutionTime;
    }

    [System.Serializable]
    public class ClearingHouse
    {
        public string ClearingHouseId;
        public string Name;
        public List<string> MemberIds = new List<string>();
        public float CollateralRequirement = 0.1f;
        public float MarginRequirement = 0.05f;
        public ClearingStatus Status = ClearingStatus.Active;
        public List<string> ClearedProducts = new List<string>();
        public RiskManagementRules RiskRules;
    }

    [System.Serializable]
    public class TradeConfirmation
    {
        public string ConfirmationId;
        public string TradeId;
        public DateTime TradeDate;
        public DateTime SettlementDate;
        public string Product;
        public float Quantity = 100f;
        public float Price = 10f;
        public string BuyerId;
        public string SellerId;
        public ConfirmationStatus Status = ConfirmationStatus.Pending;
        public DateTime ConfirmationTime;
        public List<string> RequiredSignatures = new List<string>();
    }

    #endregion

    #region Trading Analytics and Performance

    [System.Serializable]
    public class TradingPerformance
    {
        public string TraderId;
        public DateTime PeriodStart;
        public DateTime PeriodEnd;
        public float TotalReturn = 0.1f;
        public float AnnualizedReturn = 0.12f;
        public float MaxDrawdown = 0.05f;
        public float SharpeRatio = 1.2f;
        public float CalmarRatio = 2.4f;
        public int TotalTrades = 100;
        public float WinRate = 0.65f;
        public float AverageWin = 150f;
        public float AverageLoss = 80f;
        public float ProfitFactor = 1.875f;
    }

    [System.Serializable]
    public class StrategyPerformance
    {
        public string StrategyId;
        public float TotalReturn = 0.15f;
        public float Volatility = 0.2f;
        public float SharpeRatio = 0.75f;
        public float MaxDrawdown = 0.08f;
        public int TotalSignals = 250;
        public int SuccessfulSignals = 160;
        public float SignalAccuracy = 0.64f;
        public DateTime LastUpdated;
        public List<TradingPerformanceMetric> Metrics = new List<TradingPerformanceMetric>();
    }

    [System.Serializable]
    public class AlgorithmPerformance
    {
        public string AlgorithmId;
        public float TotalReturn = 0.18f;
        public float AnnualizedReturn = 0.22f;
        public float Volatility = 0.15f;
        public float SharpeRatio = 1.47f;
        public float InformationRatio = 0.9f;
        public float TrackingError = 0.05f;
        public int ExecutionCount = 500;
        public float AverageExecutionTime = 0.05f; // seconds
        public DateTime LastOptimization;
    }

    [System.Serializable]
    public class TradingPerformanceMetric
    {
        public string MetricName;
        public MetricType Type = MetricType.Return;
        public float Value = 0f;
        public DateTime CalculationDate;
        public TimePeriod Period = TimePeriod.Daily;
        public float Benchmark = 0f;
        public string Description;
    }

    [System.Serializable]
    public class TradeResult
    {
        public string TradeId;
        public DateTime EntryDate;
        public DateTime ExitDate;
        public string Product;
        public float EntryPrice = 10f;
        public float ExitPrice = 11f;
        public float Quantity = 100f;
        public float GrossProfit = 100f;
        public float Commission = 5f;
        public float NetProfit = 95f;
        public float ReturnPercent = 0.1f;
        public int HoldingPeriodDays = 5;
        public TradeOutcome Outcome = TradeOutcome.Win;
    }

    #endregion

    #region Risk Management and Compliance

    [System.Serializable]
    public class RiskManagementRules
    {
        public string RuleSetId;
        public float MaxPositionSize = 1000f;
        public float MaxDailyLoss = 500f;
        public float MaxDrawdown = 0.1f;
        public float VaRLimit = 0.05f; // Value at Risk
        public float ConcentrationLimit = 0.2f;
        public List<RiskLimit> Limits = new List<RiskLimit>();
        public bool IsActive = true;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class RiskLimit
    {
        public string LimitId;
        public RiskLimitType Type = RiskLimitType.Position;
        public float Threshold = 1000f;
        public float CurrentValue = 500f;
        public LimitAction Action = LimitAction.Warning;
        public bool IsBreached = false;
        public DateTime LastChecked;
        public string Description;
    }

    [System.Serializable]
    public class ComplianceRule
    {
        public string RuleId;
        public string RuleName;
        public ComplianceType Type = ComplianceType.Trading;
        public string Description;
        public bool IsActive = true;
        public float ViolationPenalty = 100f;
        public DateTime EffectiveDate;
        public DateTime ExpiryDate;
        public List<string> ApplicableProducts = new List<string>();
        public RuleEnforcementLevel EnforcementLevel = RuleEnforcementLevel.Mandatory;
    }

    [System.Serializable]
    public class TradingPermissions
    {
        public string PermissionSetId;
        public List<string> AllowedProducts = new List<string>();
        public List<TradingOperationType> AllowedOperations = new List<TradingOperationType>();
        public float MaxTradeSize = 10000f;
        public float DailyTradeLimit = 50000f;
        public bool CanUseMargin = false;
        public bool CanShortSell = false;
        public bool CanTradeOptions = false;
        public bool CanTradeFutures = false;
        public DateTime PermissionGranted;
        public DateTime PermissionExpiry;
    }

    #endregion

    #region Supporting Enums and Data Types

    public enum TradingOperationType
    {
        Buy,
        Sell,
        ShortSell,
        Cover,
        Exercise,
        Assign,
        Transfer,
        Deposit,
        Withdrawal
    }

    public enum TradingStatus
    {
        Pending,
        PartiallyFilled,
        Filled,
        Cancelled,
        Rejected,
        Expired,
        Suspended
    }

    public enum TradingPlatform
    {
        Internal,
        ExternalExchange,
        OTC,
        DarkPool,
        CrossingNetwork,
        ECN // Electronic Communication Network
    }

    public enum PortfolioStrategy
    {
        Conservative,
        Balanced,
        Aggressive,
        Growth,
        Income,
        Speculative,
        Arbitrage,
        HedgeFund
    }

    public enum RiskProfile
    {
        Conservative,
        Moderate,
        Aggressive,
        HighRisk
    }

    public enum PositionType
    {
        Long,
        Short,
        Neutral
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

    public enum OrderSide
    {
        Buy,
        Sell
    }

    public enum OrderStatus
    {
        Open,
        PartiallyFilled,
        Filled,
        Cancelled,
        Rejected,
        Expired
    }

    public enum AccountType
    {
        Standard,
        Premium,
        Professional,
        Institutional,
        MarginAccount,
        CashAccount
    }

    public enum AccountStatus
    {
        Active,
        Inactive,
        Suspended,
        Closed,
        PendingApproval,
        Restricted
    }

    public enum SessionStatus
    {
        Active,
        Paused,
        Ended,
        Suspended
    }



    public enum RuleType
    {
        Entry,
        Exit,
        RiskManagement,
        PositionSizing,
        StopLoss,
        TakeProfit
    }

    public enum SignalType
    {
        Buy,
        Sell,
        Hold,
        StrongBuy,
        StrongSell,
        Neutral
    }

    public enum SignalStatus
    {
        Active,
        Executed,
        Ignored,
        Expired,
        Cancelled
    }

    public enum AlgorithmType
    {
        MeanReversion,
        TrendFollowing,
        Momentum,
        Arbitrage,
        MarketMaking,
        PairTrading,
        MachineLearning
    }

    public enum ParameterType
    {
        Float,
        Integer,
        Boolean,
        String,
        Enum,
        Array
    }

    public enum ExecutionFrequency
    {
        RealTime,
        Minutely,
        Hourly,
        Daily,
        Weekly,
        Monthly
    }

    public enum MakerStatus
    {
        Active,
        Inactive,
        Suspended,
        UnderReview
    }

    public enum ProviderTier
    {
        Tier1,
        Tier2,
        Tier3,
        Retail
    }

    public enum ProviderStatus
    {
        Active,
        Inactive,
        Suspended,
        Pending
    }

    public enum PoolStatus
    {
        Active,
        Inactive,
        Locked,
        Draining
    }

    public enum ExecutionVenue
    {
        Internal,
        NYSE,
        NASDAQ,
        OTC,
        DarkPool,
        ECN
    }

    public enum SettlementType
    {
        DVP, // Delivery vs Payment
        RVP, // Receive vs Payment
        FOP, // Free of Payment
        Cash
    }

    public enum SettlementStatus
    {
        Pending,
        InProgress,
        Settled,
        Failed,
        Cancelled
    }

    public enum InstructionType
    {
        Payment,
        Delivery,
        Receipt,
        Transfer
    }

    public enum InstructionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public enum ClearingStatus
    {
        Active,
        Inactive,
        Suspended,
        UnderReview
    }

    public enum ConfirmationStatus
    {
        Pending,
        Confirmed,
        Disputed,
        Cancelled,
        Expired
    }

    public enum MetricType
    {
        Return,
        Risk,
        Ratio,
        Volume,
        Performance,
        Efficiency
    }

    public enum TimePeriod
    {
        Intraday,
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annual,
        Inception
    }

    public enum TradeOutcome
    {
        Win,
        Loss,
        Breakeven
    }

    public enum RiskLimitType
    {
        Position,
        Exposure,
        VaR,
        Drawdown,
        Concentration,
        Leverage
    }

    public enum LimitAction
    {
        None,
        Warning,
        Block,
        Reduce,
        Close
    }

    public enum ComplianceType
    {
        Trading,
        Risk,
        Regulatory,
        Internal,
        Reporting
    }

    public enum RuleEnforcementLevel
    {
        Advisory,
        Warning,
        Mandatory,
        Critical
    }

    #endregion

    #region Helper Data Structures

    [System.Serializable]
    public class MarketMakingStrategy
    {
        public string StrategyName;
        public float TargetSpread = 0.02f;
        public float InventoryTurnover = 5f;
        public bool AdaptiveSpread = true;
        public float MaxInventoryRatio = 0.8f;
        public StrategyStatus Status = StrategyStatus.Active;
    }



    #endregion

    #region Missing Types for Trading System

    /// <summary>
    /// Player reputation data for trading systems
    /// </summary>
    [System.Serializable]
    public class PlayerReputation
    {
        public float OverallReputation = 0.5f;
        public Dictionary<string, float> TradingPostReputations = new Dictionary<string, float>();
        public Dictionary<string, float> CategoryReputations = new Dictionary<string, float>();
        public int TotalTransactions = 0;
        public int SuccessfulTransactions = 0;
        public float AverageRating = 0.5f;
        public DateTime LastUpdated = DateTime.Now;

        // Additional reputation metrics
        public int TransactionCount { get; set; }
        public float ReliabilityScore = 0.5f;
        public float QualityScore = 0.5f;
        public float InnovationScore = 0.5f;
        public float ProfessionalismScore = 0.5f;
        public float ComplianceScore = 0.5f;
    }

    /// <summary>
    /// Trading post product definition
    /// </summary>
    [System.Serializable]
    public class TradingPostProduct
    {
        public string ProductId;
        public string ProductName;
        public string Category;
        public float BasePrice = 100f;
        public float CurrentPrice = 100f;
        public int AvailableQuantity = 10;
        public int MaxQuantity = 100;
        public float Quality = 1f;
        public DateTime LastRestocked = DateTime.Now;
        public bool IsAvailable = true;
        public string Description;

        // Additional properties for compatibility
        public MarketProductSO Product;
        public float QualityRange = 1.0f;
        public float PriceModifier = 1.0f;
    }

    /// <summary>
    /// Storage environment conditions for inventory management
    /// </summary>
    [System.Serializable]
    public class StorageEnvironment
    {
        public float Temperature = 20f; // Celsius
        public float Humidity = 50f; // Percentage
        public float LightLevel = 0.5f; // 0-1 scale
        public bool IsClimateControlled = true;
        public bool IsSecure = true;
        public float AirQuality = 1f; // 0-1 scale
        public DateTime LastUpdated = DateTime.Now;
    }

    #endregion
}
