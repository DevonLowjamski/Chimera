using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Economy.Trading;
// using ProjectChimera.Data.Economy; // Removed circular reference

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Comprehensive data structures for trading system decomposition
    /// Supporting TradingManager → 3 specialized services
    /// </summary>

    #region Core Trading Types

    /// <summary>
    /// Trading-specific transaction types to avoid conflicts with currency system
    /// </summary>
    public enum TradingTransactionType
    {
        Purchase,
        Sale,
        Buy,
        Sell,
        Exchange,
        Transfer,
        Barter,
        Consignment,
        Return,
        Partial_Return
    }

    public enum TransactionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public enum PaymentMethodType
    {
        Cash,
        Credit,
        Cryptocurrency,
        Barter,
        Contract,
        Bank_Transfer
    }

    public enum OpportunityType
    {
        All,
        Buy,
        Sell,
        Arbitrage,
        Special,
        Bulk_Discount,
        Quality_Premium,
        Urgent_Sale,
        Seasonal_Special,
        New_Market,
        Liquidation
    }

    public enum CashTransferType
    {
        Income,
        Expense,
        Investment,
        Withdrawal,
        Tax,
        Cash_To_Bank,
        Bank_To_Cash
    }

    public enum TradingPostType
    {
        Dispensary,
        Wholesaler,
        Distributor,
        Processor,
        TestingLab,
        Equipment,
        Seeds,
        Nutrients
    }

    public enum TradingPostStatus
    {
        Open,
        Closed,
        Limited,
        VIP,
        Maintenance
    }

    #endregion

    #region Transaction System

    [System.Serializable]
    public class TransactionResult
    {
        public bool Success;
        public string TransactionId;
        public MarketProductSO Product;
        public float Quantity;
        public float UnitPrice;
        public float TotalValue;
        public TradingPost TradingPost;
        public DateTime EstimatedCompletionTime;
        public string ErrorMessage;
        public Dictionary<string, object> Metadata = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class PendingTransaction
    {
        public string TransactionId;
        public TradingTransactionType TransactionType;
        public MarketProductSO Product;
        public float Quantity;
        public float UnitPrice;
        public float TotalValue;
        public TradingPost TradingPost;
        public PaymentMethod PaymentMethod;
        public DateTime InitiationTime;
        public DateTime EstimatedCompletionTime;
        public TransactionStatus Status;
        public string PlayerId;
        public Dictionary<string, object> TransactionData = new Dictionary<string, object>();
        
        // Additional properties for compatibility
        public InventoryItem SourceInventoryItem;
    }

    [System.Serializable]
    public class CompletedTransaction
    {
        public string TransactionId;
        public TradingTransactionType TransactionType;
        public MarketProductSO Product;
        public float Quantity;
        public float UnitPrice;
        public float TotalValue;
        public TradingPost TradingPost;
        public PaymentMethod PaymentMethod;
        public DateTime CompletionTime;
        public float ProfitLoss;
        public string PlayerId;
        public Dictionary<string, float> Fees = new Dictionary<string, float>();
        public string Notes;
        
        // Additional properties for compatibility
        public bool Success = true;
        public float QualityScore = 1.0f;
    }

    [System.Serializable]
    public class PaymentMethod
    {
        public string PaymentId;
        public string Name;
        public PaymentMethodType Type;
        public float TransactionFeePercentage;
        public float ProcessingTime; // In game hours
        public float MaxTransactionAmount;
        public bool IsAvailable;
        public Dictionary<string, object> MethodSpecificData = new Dictionary<string, object>();
        
        // Compatibility properties
        public PaymentMethodType PaymentType { get; set; }
    }

    [System.Serializable]
    public class TransactionSettings
    {
        public float DefaultProcessingTime = 0.1f;
        public float MaxTransactionValue = 1000000f;
        public float MinTransactionValue = 1f;
        public bool RequireConfirmation = true;
        public bool EnableTransactionFees = true;
        public float BaseFeePercentage = 0.02f;
        public List<string> AllowedPaymentMethods = new List<string>();
    }

    #endregion

    #region Trading Posts

    [System.Serializable]
    public class TradingPost
    {
        public string TradingPostId;
        public string Name;
        public string Description;
        public TradingPostType Type;
        public TradingPostStatus Status;
        public Vector3 Location;
        public float ReputationRequirement;
        public float PriceMarkup;
        public List<string> AvailableProducts = new List<string>();
        public Dictionary<string, float> ProductQuantities = new Dictionary<string, float>();
        public List<string> AcceptedPaymentMethods = new List<string>();
        public TradingPostOperatingHours OperatingHours;
        public string ContactInfo;
        public Dictionary<string, object> Metadata = new Dictionary<string, object>();
        
        // Compatibility properties
        public string TradingPostName => Name;
        public float ProcessingTimeHours = 0.5f;
        public MarketProductSO Product;
        public float MinimumQualityThreshold = 0.5f;
        public List<string> AcceptedProductTypes = new List<string>();
    }

    [System.Serializable]
    public class TradingPostOperatingHours
    {
        public bool IsOpen24Hours = false;
        public TimeSpan OpenTime = new TimeSpan(9, 0, 0); // 9:00 AM
        public TimeSpan CloseTime = new TimeSpan(18, 0, 0); // 6:00 PM
        public List<DayOfWeek> ClosedDays = new List<DayOfWeek>();
        public bool ObservesHolidays = true;
    }

    [System.Serializable]
    public class TradingPostState
    {
        public string TradingPostId;
        public TradingPostStatus CurrentStatus;
        public float PriceMarkup;
        public Dictionary<string, float> CurrentInventory = new Dictionary<string, float>();
        public float DailyRevenue;
        public int DailyTransactions;
        public DateTime LastUpdate;
        public List<string> SpecialOffers = new List<string>();
        public Dictionary<string, object> DynamicData = new Dictionary<string, object>();
        
        // Additional state properties
        public bool IsActive { get; set; } = true;
        public TradingPost TradingPost;
        public float CommissionRate = 0.05f;
        public float ReputationWithPlayer = 0.5f;
        public List<ProjectChimera.Data.Economy.Trading.TradingPostProduct> AvailableProducts = new List<ProjectChimera.Data.Economy.Trading.TradingPostProduct>();
        public DateTime LastRestockDate = DateTime.Now;
    }

    [System.Serializable]
    public class TradingOpportunity
    {
        public string OpportunityId;
        public string Name;
        public string Description;
        public OpportunityType Type;
        public MarketProductSO Product;
        public TradingPost SourcePost;
        public TradingPost TargetPost;
        public float PotentialProfit;
        public float ProfitMargin;
        public float RequiredCapital;
        public DateTime ExpirationTime;
        public float RiskLevel;
        public string RecommendedAction;
        public Dictionary<string, object> OpportunityData = new Dictionary<string, object>();
        
        // Additional properties for compatibility
        public DateTime ExpirationDate { get; set; }
        public OpportunityType OpportunityType = OpportunityType.Buy;
        public float RequiredReputationLevel = 0.0f;
        public float PriceModifier = 1.0f;
        public float QuantityAvailable = 100f;
        public DateTime DiscoveryDate = DateTime.Now;
    }

    #endregion

    #region Financial Management

    [System.Serializable]
    public class PlayerInventory
    {
        public string PlayerId;
        public List<InventoryItem> Items = new List<InventoryItem>();
        public float MaxCapacity;
        public float CurrentCapacity;
        public DateTime LastUpdate;
        public Dictionary<string, float> CategoryCapacities = new Dictionary<string, float>();
        public List<string> UnlockedCategories = new List<string>();
        
        // Alias for compatibility
        public List<InventoryItem> InventoryItems { get; set; }
        public string DefaultStorageLocation { get; set; } = "Warehouse";
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string ItemId;
        public MarketProductSO Product;
        public float Quantity;
        public float PurchasePrice;
        public DateTime PurchaseDate;
        public DateTime ExpirationDate;
        public float Quality;
        public string StorageLocation;
        public bool IsLocked; // For reserved items
        public Dictionary<string, object> ItemData = new Dictionary<string, object>();
        
        // Additional properties for compatibility
        public float QualityScore { get; set; } = 1.0f;
        public float AcquisitionCost { get; set; } = 0f;
        public DateTime AcquisitionDate { get; set; } = DateTime.Now;
        public string BatchId { get; set; } = "";
        public float InitialQualityScore { get; set; } = 1.0f;
        public BatchTrackingInfo Metadata { get; set; } = new BatchTrackingInfo();
        public BatchTrackingInfo BatchInfo { get; set; } = new BatchTrackingInfo();
        
        // Missing methods for TradingInventoryManager
        public float DegradationRate { get; set; } = 0.01f;
        public DateTime LastQualityUpdate { get; set; } = DateTime.Now;
        
        public void RecordQualityDegradation(float degradationAmount)
        {
            QualityScore = Mathf.Max(0f, QualityScore - degradationAmount);
            LastQualityUpdate = DateTime.Now;
        }
        
        public void RecordQualityDegradation(InventoryItem item, float degradationAmount, DateTime timestamp)
        {
            item.QualityScore = Mathf.Max(0f, item.QualityScore - degradationAmount);
            item.LastQualityUpdate = timestamp;
        }
        
        public void UpdateStorageConditions(StorageEnvironment environment)
        {
            // Update item based on storage environment conditions
            if (environment != null)
            {
                // Factor in storage environment effects on quality
                float environmentalFactor = 1.0f;
                if (!environment.IsClimateControlled)
                {
                    environmentalFactor = 0.95f; // Slight quality degradation without climate control
                }
                
                QualityScore = Mathf.Max(0.01f, QualityScore * environmentalFactor);
                LastQualityUpdate = DateTime.Now;
            }
        }
    }

    [System.Serializable]
    public class PlayerFinances
    {
        public string PlayerId;
        public float CashBalance;
        public float CreditLimit;
        public float UsedCredit;
        public List<FinancialAccount> Accounts = new List<FinancialAccount>();
        public List<Investment> Investments = new List<Investment>();
        public FinancialMetrics Metrics;
        public DateTime LastUpdate;
        
        // Additional financial properties
        public float CashOnHand { get; set; } // Made settable for compatibility
        public float BankBalance;
        public float TotalDebt;
        public float MonthlyProfit;
        public float AccountsReceivable;
        public float AccountsPayable;
        public float MonthlyCosts;
        public List<ProjectChimera.Data.Economy.FinancialRecord> TransactionHistory = new List<ProjectChimera.Data.Economy.FinancialRecord>();
    }

    [System.Serializable]
    public class FinancialAccount
    {
        public string AccountId;
        public string AccountName;
        public string AccountType; // Checking, Savings, Business, etc.
        public float Balance;
        public float InterestRate;
        public List<ProjectChimera.Data.Economy.FinancialRecord> TransactionHistory = new List<ProjectChimera.Data.Economy.FinancialRecord>();
        public bool IsActive;
    }

    [System.Serializable]
    public class FinancialTransaction
    {
        public string TransactionId;
        public DateTime Date;
        public float Amount;
        public string Description;
        public string Category;
        public CashTransferType Type;
        public string RelatedTradeId;
    }

    [System.Serializable]
    public class FinancialMetrics
    {
        public float NetWorth;
        public float LiquidAssets;
        public float TotalDebt;
        public float MonthlyIncome;
        public float MonthlyExpenses;
        public float CashFlow;
        public float ROI; // Return on Investment
        public float DebtToIncomeRatio;
        public DateTime LastCalculation;
    }

    #endregion

    #region Analytics and Reports

    [System.Serializable]
    public class TradingProfitabilityAnalysis
    {
        public MarketProductSO Product;
        public float Quantity;
        public TradingTransactionType TransactionType;
        public float EstimatedProfit;
        public float ProfitMargin;
        public float BreakEvenPrice;
        public float RiskAssessment;
        public List<ProfitabilityFactor> Factors = new List<ProfitabilityFactor>();
        public string Recommendation;
        public DateTime AnalysisDate;
        
        // Additional properties for compatibility
        public bool IsAnalysisValid = true;
        public string RecommendationReason = "Analysis completed successfully";
        public float RecommendationScore = 0.5f;
        public float EstimatedCost = 0f;
        public float EstimatedRevenue = 0f;
    }

    [System.Serializable]
    public class ProfitabilityFactor
    {
        public string FactorName;
        public float Impact; // Positive or negative percentage
        public string Description;
        public float Confidence; // 0-1
    }

    [System.Serializable]
    public class ProductTradingData
    {
        public MarketProductSO Product;
        public float TotalVolumeTraded;
        public float AveragePrice;
        public float LastTransactionPrice;
        public int TransactionCount;
        public DateTime LastTradeDate;
        public float PriceVolatility;
        public List<float> PriceHistory = new List<float>();
        public Dictionary<string, float> TradingPostPrices = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class TradingPerformanceMetrics
    {
        public string PlayerId;
        public float TotalProfit;
        public float TotalLoss;
        public float NetProfit;
        public int SuccessfulTrades;
        public int FailedTrades;
        public float SuccessRate;
        public float AverageTradeSize;
        public float LargestProfit;
        public float LargestLoss;
        public DateTime LastUpdate;
        public Dictionary<string, float> ProductPerformance = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class TradingSettings
    {
        public bool EnableAutomaticTrading = false;
        public bool EnableTradingNotifications = true;
        public float RiskTolerance = 0.5f; // 0-1
        public float MaxInvestmentPercentage = 0.2f; // 20% of net worth
        public List<string> PreferredTradingPosts = new List<string>();
        public List<string> PreferredPaymentMethods = new List<string>();
        public bool RequireConfirmation = true;
        public Dictionary<string, object> AdvancedSettings = new Dictionary<string, object>();
        
        // Starting conditions
        public float StartingCash = 10000f;
        public float StartingCreditLimit = 5000f;
        public float StartingInventoryCapacity = 1000f;
    }

    #endregion
    
    #region Product Categories
    
    [System.Serializable]
    public class TradingProductCategory
    {
        public string CategoryId;
        public string CategoryName;
        public string Description;
        
        // Implicit conversion to string
        public static implicit operator string(TradingProductCategory category)
        {
            return category?.CategoryName ?? "";
        }
        
        public override string ToString()
        {
            return CategoryName ?? "";
        }
    }
    
    #endregion
    
    #region Batch Tracking
    
    [System.Serializable]
    public class BatchTrackingInfo
    {
        public string BatchId = "BATCH001";
        public DateTime CreationDate = DateTime.Now;
        public string SourceLocation = "Warehouse";
        public float InitialQuantity = 100f;
        public float CurrentQuantity = 100f;
        public float InitialQuality = 1.0f;
        public float CurrentQuality = 1.0f;
        public List<string> ProcessingSteps = new List<string>();
        public Dictionary<string, object> TrackingMetadata = new Dictionary<string, object>();
        public DateTime HarvestDate { get; set; } = DateTime.Now;
        public float HarvestQuality { get; set; } = 1.0f;
        public string ProcessingMethod { get; set; } = "";
    }
    
    #endregion
}