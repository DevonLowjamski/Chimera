using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Data Transfer Objects for Economy System Save/Load Operations
    /// These DTOs capture the state of the market, trading, financial, and player economy systems.
    /// </summary>
    
    /// <summary>
    /// Main economy state DTO for the entire economic system
    /// </summary>
    [System.Serializable]
    public class EconomyStateDTO
    {
        [Header("Economy System State")]
        public MarketStateDTO MarketState;
        public TradingStateDTO TradingState;
        public PlayerEconomyStateDTO PlayerEconomyState;
        public FinancialStateDTO FinancialState;
        
        [Header("Market Products")]
        public List<MarketProductDTO> AvailableProducts = new List<MarketProductDTO>();
        public Dictionary<string, float> ProductPrices = new Dictionary<string, float>();
        public Dictionary<string, MarketProductPerformanceDTO> ProductPerformance = new Dictionary<string, MarketProductPerformanceDTO>();
        
        [Header("Transaction History")]
        public List<TransactionRecordDTO> TransactionHistory = new List<TransactionRecordDTO>();
        public List<TradingOpportunityDTO> AvailableOpportunities = new List<TradingOpportunityDTO>();
        
        [Header("Economic Indicators")]
        public EconomicIndicatorsDTO EconomicIndicators;
        public MarketConditionsDTO CurrentMarketConditions;
        
        [Header("System Configuration")]
        public bool EnableEconomySystem = true;
        public bool EnableDynamicPricing = true;
        public bool EnableSupplyDemandTracking = true;
        public float PriceUpdateFrequency = 4.0f;
        public float TransactionProcessingInterval = 0.1f;
        
        [Header("Save Metadata")]
        public DateTime SaveTimestamp;
        public string SaveVersion = "1.0";
    }
    
    /// <summary>
    /// DTO for market system state
    /// </summary>
    [System.Serializable]
    public class MarketStateDTO
    {
        [Header("Marketplace Configuration")]
        public MarketplaceConfigDTO MarketplaceConfig;
        
        [Header("Product Catalog")]
        public Dictionary<string, MarketProductDTO> ProductCatalog = new Dictionary<string, MarketProductDTO>();
        public List<string> AvailableProductIds = new List<string>();
        
        [Header("Market Performance")]
        public bool IsMarketActive = true;
        public float TotalMarketVolume;
        public float AverageTransactionValue;
        public int TotalTransactions;
        public DateTime LastMarketUpdate;
        
        [Header("Market System Settings")]
        public bool EnableMarketSystem = true;
        public bool EnableDynamicPricing = true;
        public bool EnableSupplyDemandTracking = true;
        public float MarketUpdateFrequency = 4.0f;
        
        [Header("Market Data")]
        public List<MarketTrendDTO> MarketTrends = new List<MarketTrendDTO>();
        public Dictionary<string, float> ProductPrices = new Dictionary<string, float>();
        
        [Header("Market Events")]
        public List<MarketEventDTO> MarketEvents = new List<MarketEventDTO>();
        
        [Header("Supply and Demand")]
        public Dictionary<string, SupplyDemandDataDTO> SupplyDemandData = new Dictionary<string, SupplyDemandDataDTO>();
        
        [Header("Competitive Analysis")]
        public Dictionary<string, CompetitorDataDTO> CompetitorData = new Dictionary<string, CompetitorDataDTO>();
    }
    
    /// <summary>
    /// DTO for trading system state
    /// </summary>
    [System.Serializable]
    public class TradingStateDTO
    {
        [Header("Trading Posts")]
        public List<TradingPostDTO> TradingPosts = new List<TradingPostDTO>();
        public Dictionary<string, TradingPostStateDTO> TradingPostStates = new Dictionary<string, TradingPostStateDTO>();
        
        [Header("Pending Transactions")]
        public List<PendingTransactionDTO> PendingTransactions = new List<PendingTransactionDTO>();
        
        [Header("Trading Opportunities")]
        public List<TradingOpportunityDTO> CurrentOpportunities = new List<TradingOpportunityDTO>();
        
        [Header("Trading Metrics")]
        public TradingMetricsDTO TradingMetrics;
        
        [Header("Payment Methods")]
        public List<PaymentMethodDTO> AvailablePaymentMethods = new List<PaymentMethodDTO>();
        
        [Header("Trading Settings")]
        public TradingSettingsDTO TradingSettings;
        
        [Header("Trading State")]
        public bool IsTradingEnabled = true;
        public bool EnableTradingSystem = true;
        public bool EnableAutomaticTrading = false;
        public float TradingFeePercentage = 0.02f;
        public DateTime LastTradeTime;
        public List<ContractDTO> ActiveContracts = new List<ContractDTO>();
        public int CompletedTradesCount = 0;
        public float TotalTradeVolume = 0f;
    }
    
    /// <summary>
    /// DTO for player economy state
    /// </summary>
    [System.Serializable]
    public class PlayerEconomyStateDTO
    {
        [Header("Player Finances")]
        public PlayerFinancesDTO PlayerFinances;
        
        [Header("Player Inventory")]
        public PlayerInventoryDTO PlayerInventory;
        
        [Header("Player Reputation")]
        public PlayerReputationDTO PlayerReputation;
        
        [Header("Player Assets")]
        public List<PlayerAssetDTO> PlayerAssets = new List<PlayerAssetDTO>();
        
        [Header("Investment Portfolio")]
        public InvestmentPortfolioDTO InvestmentPortfolio;
        
        [Header("Business Metrics")]
        public BusinessPerformanceDTO BusinessPerformance;
        
        [Header("Financial Summary")]
        public float CurrentCash = 50000f;
        public DateTime LastFinancialUpdate;
        public float MonthlyExpenses = 5000f;
        public float MonthlyRevenue = 0f;
        public float TotalNetWorth = 50000f;
        public float CreditRating = 75f;
        public List<LoanDTO> UnpaidLoans = new List<LoanDTO>();
        public List<InvestmentDTO> Investments = new List<InvestmentDTO>();
        
        [Header("Player Economy Settings")]
        public bool EnablePlayerEconomySystem = true;
        public bool EnableAutomaticBillPayment = false;
        public bool EnableInvestmentTracking = true;
        public float EconomyUpdateFrequency = 1.0f;
    }
    
    /// <summary>
    /// DTO for financial system state
    /// </summary>
    [System.Serializable]
    public class FinancialStateDTO
    {
        [Header("Active Loans")]
        public List<LoanContractDTO> ActiveLoans = new List<LoanContractDTO>();
        
        [Header("Investment History")]
        public List<InvestmentDTO> Investments = new List<InvestmentDTO>();
        
        [Header("Financial Plans")]
        public List<FinancialPlanDTO> FinancialPlans = new List<FinancialPlanDTO>();
        
        [Header("Credit Profile")]
        public CreditProfileDTO CreditProfile;
        
        [Header("Tax Information")]
        public TaxInformationDTO TaxInformation;
        
        [Header("Financial Settings")]
        public FinancialSettingsDTO FinancialSettings;
    }
    
    /// <summary>
    /// DTO for market products
    /// </summary>
    [System.Serializable]
    public class MarketProductDTO
    {
        [Header("Product Identity")]
        public string ProductId;
        public string ProductName;
        public string Category; // "Flower", "Concentrate", "Edible", "Equipment"
        public string ProductType; // "Dried_Flower", "Vape_Cartridge", etc.
        public string Description;
        
        [Header("Pricing")]
        public float BaseWholesalePrice;
        public float BaseRetailPrice;
        public Vector2 PriceVolatility;
        public float QualityPremiumMultiplier;
        public float MinimumQualityThreshold;
        
        [Header("Market Classification")]
        public string MarketTier; // "Budget", "Premium", "Luxury"
        public string LegalStatus; // "Legal_Regulated", "Medical_Only", etc.
        public List<string> TargetMarkets = new List<string>();
        public string Lifecycle; // "Introduction", "Growth", "Maturity", "Decline"
        
        [Header("Quality Specifications")]
        public QualityStandardsDTO QualityStandards;
        public List<QualityMetricDTO> QualityMetrics = new List<QualityMetricDTO>();
        
        [Header("Market Demand")]
        public DemandProfileDTO DemandProfile;
        public List<SeasonalModifierDTO> SeasonalModifiers = new List<SeasonalModifierDTO>();
        
        [Header("Supply Chain")]
        public float ShelfLife; // days
        public float SpoilageRate; // per day
        public StorageRequirementsDTO StorageRequirements;
        
        [Header("Competition")]
        public MarketCompetitionDTO Competition;
        public List<string> DirectCompetitorIds = new List<string>();
        public List<string> SubstituteIds = new List<string>();
        
        [Header("Availability")]
        public bool IsAvailable = true;
        public DateTime LastRestocked;
        public float StockLevel;
        public float MaxStockLevel;
    }
    
    /// <summary>
    /// DTO for marketplace configuration
    /// </summary>
    [System.Serializable]
    public class MarketplaceConfigDTO
    {
        public bool IsActive = true;
        public float TaxRate = 0.05f;
        public float CommissionRate = 0.02f;
        public List<string> AllowedProductTypes = new List<string>();
        public float MinimumTransactionAmount = 1.0f;
        public float MaximumTransactionAmount = 100000.0f;
        public bool RequireIdentityVerification = true;
        public bool EnableRatingSystem = true;
        public int MaxTransactionsPerDay = 100;
    }
    
    /// <summary>
    /// DTO for transaction records
    /// </summary>
    [System.Serializable]
    public class TransactionRecordDTO
    {
        public string TransactionId;
        public string ProductId;
        public string ProductName;
        public float Quantity;
        public float UnitPrice;
        public float TotalValue;
        public string TransactionType; // "Purchase", "Sale"
        public DateTime TransactionDate;
        public bool Success = true;
        public string BuyerId;
        public string SellerId;
        public string TradingPostId;
        public string PaymentMethodId;
        public float TaxAmount;
        public float CommissionAmount;
        public string Status; // "Completed", "Pending", "Failed", "Cancelled"
        public string ErrorMessage;
        public Dictionary<string, object> TransactionData = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for trading opportunities
    /// </summary>
    [System.Serializable]
    public class TradingOpportunityDTO
    {
        public string OpportunityId;
        public string ProductId;
        public string ProductName;
        public string OpportunityType; // "BuyLow", "SellHigh", "Arbitrage", "Bulk"
        public float ProfitPotential; // percentage
        public float RequiredCapital;
        public float RiskLevel; // 0-1
        public DateTime ExpirationTime;
        public string TradingPostId;
        public string Description;
        public bool IsActive = true;
        public float Confidence; // prediction confidence 0-1
        public List<string> RequiredActions = new List<string>();
    }
    
    /// <summary>
    /// DTO for player finances
    /// </summary>
    [System.Serializable]
    public class PlayerFinancesDTO
    {
        [Header("Cash and Assets")]
        public float Cash;
        public float BankBalance;
        public float TotalAssets;
        public float NetWorth;
        
        [Header("Income and Expenses")]
        public float MonthlyIncome;
        public float MonthlyExpenses;
        public float NetCashFlow;
        public float YearToDateIncome;
        public float YearToDateExpenses;
        
        [Header("Financial History")]
        public List<FinancialTransactionDTO> TransactionHistory = new List<FinancialTransactionDTO>();
        public List<IncomeSourceDTO> IncomeSources = new List<IncomeSourceDTO>();
        public List<ExpenseCategoryDTO> ExpenseCategories = new List<ExpenseCategoryDTO>();
        
        [Header("Financial Metrics")]
        public float LiquidityRatio;
        public float DebtToIncomeRatio;
        public float SavingsRate;
        public float InvestmentPercentage;
        
        [Header("Financial Goals")]
        public List<FinancialGoalDTO> FinancialGoals = new List<FinancialGoalDTO>();
        
        [Header("Credit Information")]
        public float CreditScore = 700f;
        public float CreditUtilization = 0.3f;
        public int CreditAccounts = 2;
        public float TotalCreditLimit = 10000f;
    }
    
    /// <summary>
    /// DTO for player inventory
    /// </summary>
    [System.Serializable]
    public class PlayerInventoryDTO
    {
        [Header("Inventory Items")]
        public List<InventoryItemDTO> Items = new List<InventoryItemDTO>();
        
        [Header("Inventory Capacity")]
        public float TotalCapacity;
        public float UsedCapacity;
        public float AvailableCapacity;
        
        [Header("Storage Locations")]
        public List<StorageLocationDTO> StorageLocations = new List<StorageLocationDTO>();
        
        [Header("Inventory Value")]
        public float TotalInventoryValue;
        public float AverageItemValue;
        public DateTime LastValuation;
        
        [Header("Inventory Metrics")]
        public float TurnoverRate; // how often inventory is sold/replaced
        public int TotalItems;
        public int UniqueProducts;
        public float InventoryAge; // average age of items in days
    }
    
    /// <summary>
    /// DTO for individual inventory items
    /// </summary>
    [System.Serializable]
    public class InventoryItemDTO
    {
        public string ItemId;
        public string ProductId;
        public string ProductName;
        public float Quantity;
        public float QualityScore;
        public float UnitValue;
        public float TotalValue;
        public DateTime AcquisitionDate;
        public DateTime ExpirationDate;
        public string StorageLocationId;
        public string Condition; // "Excellent", "Good", "Fair", "Poor"
        public bool IsPerishable;
        public float DeteriorationRate;
        public Dictionary<string, object> ItemProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for player reputation
    /// </summary>
    [System.Serializable]
    public class PlayerReputationDTO
    {
        public float OverallReputation = 0.5f;
        public int TransactionCount = 0;
        public float ReliabilityScore = 0.5f;
        public float QualityScore = 0.5f;
        public float InnovationScore = 0.5f;
        public float ProfessionalismScore = 0.5f;
        public float ComplianceScore = 0.5f;
        public List<ReputationEventDTO> ReputationHistory = new List<ReputationEventDTO>();
        public Dictionary<string, float> CategoryRatings = new Dictionary<string, float>();
        public List<string> Certifications = new List<string>();
        public List<string> Awards = new List<string>();
        public DateTime LastReputationUpdate;
    }
    
    /// <summary>
    /// DTO for trading posts
    /// </summary>
    [System.Serializable]
    public class TradingPostDTO
    {
        public string TradingPostId;
        public string TradingPostName;
        public string Location;
        public string Type; // "Physical", "Online", "Hybrid"
        public List<string> SupportedProductTypes = new List<string>();
        public float CommissionRate;
        public float ReputationScore;
        public bool IsActive = true;
        public List<string> AcceptedPaymentMethods = new List<string>();
        public TradingHoursDTO TradingHours;
        public ContactInformationDTO ContactInfo;
    }
    
    /// <summary>
    /// DTO for trading post state
    /// </summary>
    [System.Serializable]
    public class TradingPostStateDTO
    {
        public string TradingPostId;
        public bool IsOperational = true;
        public float CurrentVolume;
        public int ActiveTransactions;
        public float AverageTransactionValue;
        public DateTime LastActivity;
        public List<string> AvailableProducts = new List<string>();
        public Dictionary<string, float> ProductStock = new Dictionary<string, float>();
        public float BusyLevel; // 0-1, how busy the trading post is
    }
    
    /// <summary>
    /// DTO for pending transactions
    /// </summary>
    [System.Serializable]
    public class PendingTransactionDTO
    {
        public string TransactionId;
        public string ProductId;
        public float Quantity;
        public float UnitPrice;
        public string TransactionType;
        public string BuyerId;
        public string SellerId;
        public string TradingPostId;
        public DateTime CreatedTime;
        public DateTime ScheduledExecutionTime;
        public string Status; // "Pending", "Processing", "AwaitingConfirmation"
        public Dictionary<string, object> TransactionParameters = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for economic indicators
    /// </summary>
    [System.Serializable]
    public class EconomicIndicatorsDTO
    {
        [Header("Market Indicators")]
        public float MarketIndex = 100f; // baseline 100
        public float MarketVolatility = 0.1f;
        public float InflationRate = 0.03f;
        public float InterestRate = 0.05f;
        
        [Header("Supply and Demand")]
        public float OverallSupply = 1.0f;
        public float OverallDemand = 1.0f;
        public float SupplyDemandRatio = 1.0f;
        
        [Header("Economic Health")]
        public float EconomicGrowthRate = 0.02f;
        public float UnemploymentRate = 0.05f;
        public float ConsumerConfidence = 0.7f;
        public float BusinessConfidence = 0.65f;
        
        [Header("Sector Performance")]
        public Dictionary<string, float> SectorPerformance = new Dictionary<string, float>();
        public Dictionary<string, float> RegionalPerformance = new Dictionary<string, float>();
        
        [Header("Trends")]
        public float PriceTrend = 0.0f; // -1 to 1, negative = declining, positive = rising
        public float VolumeTrend = 0.0f;
        public float QualityTrend = 0.0f;
        
        [Header("Forecasts")]
        public EconomicForecastDTO ShortTermForecast; // 1-3 months
        public EconomicForecastDTO MediumTermForecast; // 3-12 months
        public EconomicForecastDTO LongTermForecast; // 1-5 years
    }
    
    /// <summary>
    /// DTO for market conditions
    /// </summary>
    [System.Serializable]
    public class MarketConditionsDTO
    {
        public float DemandLevel = 1.0f;
        public float SupplyLevel = 1.0f;
        public float PriceLevel = 1.0f;
        public float VolatilityLevel = 0.1f;
        public string CurrentSeason; // "Spring", "Summer", "Fall", "Winter"
        public float SeasonalModifier = 1.0f;
        public List<string> ActiveMarketEvents = new List<string>();
        public float MarketSentiment = 0.5f; // 0 = very bearish, 1 = very bullish
        public DateTime LastUpdated;
    }
    
    /// <summary>
    /// DTO for investments
    /// </summary>
    [System.Serializable]
    public class InvestmentDTO
    {
        public string InvestmentId;
        public string InvestmentName;
        public string InvestmentType; // "Stock", "Bond", "RealEstate", "Business"
        public DateTime InvestmentDate;
        public float InitialAmount;
        public float CurrentValue;
        public float ExpectedReturn;
        public float ActualReturn;
        public string RiskLevel; // "Low", "Medium", "High"
        public int DurationMonths;
        public string Status; // "Active", "Matured", "Sold", "Defaulted"
        public List<InvestmentTransactionDTO> Transactions = new List<InvestmentTransactionDTO>();
        public PerformanceMetricsDTO Performance;
        public bool IsLiquid;
        public DateTime MaturityDate;
        public float ManagementFee;
    }
    
    /// <summary>
    /// DTO for loan contracts
    /// </summary>
    [System.Serializable]
    public class LoanContractDTO
    {
        public string LoanId;
        public string ContractNumber;
        public DateTime OriginationDate;
        public DateTime MaturityDate;
        public float PrincipalAmount;
        public float InterestRate;
        public float CurrentBalance;
        public float MonthlyPayment;
        public int PaymentsMade;
        public int TotalPayments;
        public string Status; // "Active", "PaidOff", "Delinquent", "Default"
        public List<LoanPaymentDTO> PaymentHistory = new List<LoanPaymentDTO>();
        public float TotalInterestPaid;
        public DateTime LastPaymentDate;
        public int DaysDelinquent;
        public LoanTermsDTO Terms;
    }
    
    // Supporting DTOs for complex data structures
    
    [System.Serializable]
    public class QualityStandardsDTO
    {
        public float MinimumPotency;
        public float MaximumContaminants;
        public List<string> RequiredTestResults = new List<string>();
        public string CertificationRequired;
    }
    
    [System.Serializable]
    public class QualityMetricDTO
    {
        public string MetricName;
        public float MinimumValue;
        public float TargetValue;
        public float MaximumValue;
        public string UnitOfMeasure;
        public bool IsRequired;
    }
    
    [System.Serializable]
    public class DemandProfileDTO
    {
        public float BaseDemand;
        public float PeakDemand;
        public float MinimumDemand;
        public string DemandPattern; // "Steady", "Seasonal", "Cyclical", "Trending"
        public List<string> DemandDrivers = new List<string>();
    }
    
    [System.Serializable]
    public class SeasonalModifierDTO
    {
        public string Season;
        public float DemandModifier;
        public float PriceModifier;
        public string Description;
    }
    
    [System.Serializable]
    public class StorageRequirementsDTO
    {
        public float TemperatureRange; // Celsius
        public float HumidityRange; // Percentage
        public bool RequiresRefrigeration;
        public bool RequiresControlledAtmosphere;
        public List<string> SpecialRequirements = new List<string>();
    }
    
    [System.Serializable]
    public class MarketCompetitionDTO
    {
        public int CompetitorCount;
        public float CompetitionIntensity; // 0-1
        public float MarketShare; // percentage
        public string CompetitiveAdvantage;
        public List<string> CompetitorStrengths = new List<string>();
        public List<string> CompetitorWeaknesses = new List<string>();
    }
    
    [System.Serializable]
    public class SupplyDemandDataDTO
    {
        public float CurrentSupply;
        public float CurrentDemand;
        public float ForecastedSupply;
        public float ForecastedDemand;
        public float PriceElasticity;
        public DateTime LastUpdated;
    }
    
    [System.Serializable]
    public class CompetitorDataDTO
    {
        public string CompetitorId;
        public string CompetitorName;
        public float MarketShare;
        public float PricingLevel; // relative to market average
        public float QualityLevel;
        public List<string> Strengths = new List<string>();
        public List<string> Weaknesses = new List<string>();
    }
    
    [System.Serializable]
    public class MarketEventDTO
    {
        public string EventId;
        public string EventType; // "PriceChange", "NewProduct", "Competition", "Regulation"
        public DateTime EventTime;
        public string Description;
        public float Impact; // -1 to 1
        public List<string> AffectedProducts = new List<string>();
        public bool IsActive;
        public DateTime ExpirationTime;
    }
    
    [System.Serializable]
    public class MarketProductPerformanceDTO
    {
        public string ProductId;
        public float SalesVolume;
        public float Revenue;
        public float ProfitMargin;
        public float MarketShare;
        public float CustomerSatisfaction;
        public int ReviewCount;
        public float AverageRating;
        public float GrowthRate;
        public DateTime LastUpdated;
    }
    
    [System.Serializable]
    public class TradingMetricsDTO
    {
        public int TotalTransactions;
        public float TotalVolume;
        public float AverageTransactionSize;
        public float SuccessRate;
        public float AverageExecutionTime;
        public int ActiveTraders;
        public float TradingVelocity; // transactions per day
        public Dictionary<string, float> ProductVolumes = new Dictionary<string, float>();
        public DateTime LastCalculated;
    }
    
    [System.Serializable]
    public class PaymentMethodDTO
    {
        public string PaymentMethodId;
        public string PaymentMethodName;
        public string Type; // "Cash", "BankTransfer", "CreditCard", "Cryptocurrency"
        public float ProcessingFee;
        public float ProcessingTime; // in hours
        public bool IsActive;
        public float MinimumAmount;
        public float MaximumAmount;
        public List<string> SupportedCurrencies = new List<string>();
    }
    
    [System.Serializable]
    public class TradingSettingsDTO
    {
        public float MaxTransactionSize;
        public float MinTransactionSize;
        public int MaxDailyTransactions;
        public bool EnableAutomaticExecution;
        public float RiskTolerance;
        public bool EnableNotifications;
        public List<string> PreferredTradingPosts = new List<string>();
    }
    
    [System.Serializable]
    public class InvestmentPortfolioDTO
    {
        public string PortfolioId;
        public float TotalValue;
        public float TotalInvested;
        public float TotalReturn;
        public float UnrealizedGains;
        public float RealizedGains;
        public List<string> InvestmentIds = new List<string>();
        public string RiskProfile; // "Conservative", "Moderate", "Aggressive"
        public DateTime LastRebalanced;
        public float DiversificationScore;
    }
    
    [System.Serializable]
    public class BusinessPerformanceDTO
    {
        public float Revenue;
        public float Expenses;
        public float Profit;
        public float ProfitMargin;
        public float ROI; // Return on Investment
        public float ROE; // Return on Equity
        public float CustomerAcquisitionCost;
        public float CustomerLifetimeValue;
        public int TotalCustomers;
        public float CustomerRetentionRate;
        public float MarketPenetration;
        public DateTime LastCalculated;
        
        [Header("Performance Metrics")]
        public float MonthlyProfit = -5000f;
        public float YearlyRevenue = 0f;
        public float OperationalEfficiency = 0.5f;
        public float MarketShare = 0.01f;
    }
    
    [System.Serializable]
    public class PlayerAssetDTO
    {
        public string AssetId;
        public string AssetName;
        public string AssetType; // "Property", "Equipment", "Vehicle", "Intellectual"
        public float PurchasePrice;
        public float CurrentValue;
        public DateTime PurchaseDate;
        public float DepreciationRate;
        public bool IsLiquid;
        public string Condition;
        public Dictionary<string, object> AssetProperties = new Dictionary<string, object>();
    }
    
    [System.Serializable]
    public class FinancialTransactionDTO
    {
        public string TransactionId;
        public DateTime TransactionDate;
        public string TransactionType; // "Income", "Expense", "Transfer", "Investment"
        public float Amount;
        public string Description;
        public string Category;
        public string Account;
        public bool IsRecurring;
        public Dictionary<string, object> TransactionData = new Dictionary<string, object>();
    }
    
    [System.Serializable]
    public class IncomeSourceDTO
    {
        public string SourceId;
        public string SourceName;
        public string SourceType; // "Business", "Investment", "Employment", "Other"
        public float MonthlyAmount;
        public bool IsRegular;
        public DateTime StartDate;
        public DateTime EndDate;
        public float GrowthRate;
    }
    
    [System.Serializable]
    public class ExpenseCategoryDTO
    {
        public string CategoryId;
        public string CategoryName;
        public float MonthlyAmount;
        public float BudgetedAmount;
        public bool IsFixed;
        public string Priority; // "Essential", "Important", "Optional"
        public List<string> SubCategories = new List<string>();
    }
    
    [System.Serializable]
    public class FinancialGoalDTO
    {
        public string GoalId;
        public string GoalName;
        public float TargetAmount;
        public float CurrentAmount;
        public DateTime TargetDate;
        public string GoalType; // "Savings", "Investment", "Debt", "Purchase"
        public string Priority; // "High", "Medium", "Low"
        public bool IsAchieved;
        public float ProgressPercentage;
    }
    
    [System.Serializable]
    public class ReputationEventDTO
    {
        public string EventId;
        public DateTime EventDate;
        public string EventType; // "Transaction", "Review", "Certification", "Violation"
        public float ReputationChange;
        public string Description;
        public string Category;
    }
    
    [System.Serializable]
    public class TradingHoursDTO
    {
        public Dictionary<string, TimeRangeDTO> WeeklyHours = new Dictionary<string, TimeRangeDTO>();
        public List<HolidayDTO> Holidays = new List<HolidayDTO>();
        public string TimeZone;
    }
    
    [System.Serializable]
    public class TimeRangeDTO
    {
        public TimeSpan OpenTime;
        public TimeSpan CloseTime;
        public bool IsOpen;
    }
    
    [System.Serializable]
    public class HolidayDTO
    {
        public DateTime Date;
        public string HolidayName;
        public bool IsClosed;
    }
    
    [System.Serializable]
    public class ContactInformationDTO
    {
        public string Address;
        public string Phone;
        public string Email;
        public string Website;
        public Dictionary<string, string> SocialMedia = new Dictionary<string, string>();
    }
    
    [System.Serializable]
    public class StorageLocationDTO
    {
        public string LocationId;
        public string LocationName;
        public string LocationType; // "Warehouse", "Retail", "Home", "Vehicle"
        public float Capacity;
        public float UsedCapacity;
        public bool IsClimateControlled;
        public bool IsSecure;
        public List<string> StoredItemIds = new List<string>();
    }
    
    [System.Serializable]
    public class CreditProfileDTO
    {
        public float CreditScore;
        public string CreditRating; // "Excellent", "Good", "Fair", "Poor"
        public float CreditUtilization;
        public int CreditAccounts;
        public float TotalCreditLimit;
        public int PaymentHistory; // months of on-time payments
        public List<string> CreditInquiries = new List<string>();
        public DateTime LastUpdated;
    }
    
    [System.Serializable]
    public class TaxInformationDTO
    {
        public string TaxYear;
        public float TaxableIncome;
        public float TaxesPaid;
        public float TaxesOwed;
        public float Deductions;
        public float Credits;
        public string FilingStatus;
        public List<TaxDocumentDTO> TaxDocuments = new List<TaxDocumentDTO>();
    }
    
    [System.Serializable]
    public class TaxDocumentDTO
    {
        public string DocumentType; // "W2", "1099", "Receipt", "Invoice"
        public float Amount;
        public string Description;
        public DateTime DocumentDate;
        public string Category;
    }
    
    [System.Serializable]
    public class FinancialSettingsDTO
    {
        public float BaseInterestRate;
        public float RiskAdjustmentFactor;
        public float InflationRate;
        public float MinimumInvestment;
        public float MaximumLoanAmount;
        public bool EnableDynamicRates;
        public bool EnableCreditScoring;
        public int MaxActiveLoans;
        public int MaxActiveInvestments;
    }
    
    [System.Serializable]
    public class FinancialPlanDTO
    {
        public string PlanId;
        public string PlanName;
        public DateTime CreatedDate;
        public DateTime LastUpdated;
        public List<FinancialGoalDTO> Goals = new List<FinancialGoalDTO>();
        public List<FinancialStrategyDTO> Strategies = new List<FinancialStrategyDTO>();
        public string PlanType; // "Retirement", "Emergency", "Growth", "Income"
        public int TimeHorizonYears;
        public float TargetReturn;
        public string RiskTolerance;
    }
    
    [System.Serializable]
    public class FinancialStrategyDTO
    {
        public string StrategyId;
        public string StrategyName;
        public string Description;
        public float AllocationPercentage;
        public string AssetClass;
        public float ExpectedReturn;
        public float RiskLevel;
    }
    
    [System.Serializable]
    public class InvestmentTransactionDTO
    {
        public string TransactionId;
        public DateTime TransactionDate;
        public string TransactionType; // "Buy", "Sell", "Dividend", "Fee"
        public float Amount;
        public float SharePrice;
        public float Shares;
        public float Fees;
        public string Description;
    }
    
    [System.Serializable]
    public class PerformanceMetricsDTO
    {
        public float TotalReturn;
        public float AnnualizedReturn;
        public float Volatility;
        public float SharpeRatio;
        public float MaxDrawdown;
        public float Alpha;
        public float Beta;
        public DateTime PerformancePeriodStart;
        public DateTime PerformancePeriodEnd;
    }
    
    [System.Serializable]
    public class LoanPaymentDTO
    {
        public string PaymentId;
        public DateTime PaymentDate;
        public DateTime DueDate;
        public float PaymentAmount;
        public float PrincipalAmount;
        public float InterestAmount;
        public float LateFee;
        public string Status; // "Completed", "Pending", "Late", "Failed"
        public string PaymentMethod;
        public float RemainingBalance;
    }
    
    [System.Serializable]
    public class LoanTermsDTO
    {
        public string LoanType; // "Personal", "Business", "Equipment", "Real Estate"
        public string Purpose;
        public float InterestRate;
        public int TermMonths;
        public string PaymentFrequency; // "Monthly", "Quarterly", "Annually"
        public bool HasPrepaymentPenalty;
        public List<string> Collateral = new List<string>();
        public Dictionary<string, object> SpecialTerms = new Dictionary<string, object>();
    }
    
    [System.Serializable]
    public class EconomicForecastDTO
    {
        public string ForecastPeriod; // "Short", "Medium", "Long"
        public float ExpectedGrowth;
        public float ExpectedInflation;
        public float ExpectedInterestRate;
        public float Confidence; // 0-1
        public List<string> KeyFactors = new List<string>();
        public List<string> Risks = new List<string>();
        public DateTime ForecastDate;
    }
    
    /// <summary>
    /// Result DTO for economy save operations
    /// </summary>
    [System.Serializable]
    public class EconomySaveResult
    {
        public bool Success;
        public DateTime SaveTime;
        public string ErrorMessage;
        public long DataSizeBytes;
        public TimeSpan SaveDuration;
        public int ProductsSaved;
        public int TransactionsSaved;
        public int InvestmentsSaved;
        public int LoansSaved;
        public string SaveVersion;
    }
    
    /// <summary>
    /// Result DTO for economy load operations
    /// </summary>
    [System.Serializable]
    public class EconomyLoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public int ProductsLoaded;
        public int TransactionsLoaded;
        public int InvestmentsLoaded;
        public int LoansLoaded;
        public bool RequiredMigration;
        public string LoadedVersion;
        public EconomyStateDTO EconomyState;
    }
    
    /// <summary>
    /// DTO for economy system validation
    /// </summary>
    [System.Serializable]
    public class EconomyValidationResult
    {
        public bool IsValid;
        public DateTime ValidationTime;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        [Header("Market Validation")]
        public bool MarketDataValid;
        public bool ProductCatalogValid;
        public bool PricingDataValid;
        
        [Header("Trading Validation")]
        public bool TradingPostsValid;
        public bool TransactionHistoryValid;
        public bool InventoryValid;
        
        [Header("Financial Validation")]
        public bool PlayerFinancesValid;
        public bool InvestmentsValid;
        public bool LoansValid;
        
        [Header("Data Integrity")]
        public int TotalProducts;
        public int ValidProducts;
        public int TotalTransactions;
        public int ValidTransactions;
        public float DataIntegrityScore;
    }
    
    /// <summary>
    /// DTO for market trend data
    /// </summary>
    [System.Serializable]
    public class MarketTrendDTO
    {
        public string ProductId;
        public string TrendType; // "Rising", "Falling", "Stable"
        public float TrendStrength;
        public DateTime StartTime;
        public DateTime EndTime;
        public float PriceChange;
    }
    
    /// <summary>
    /// DTO for contract data
    /// </summary>
    [System.Serializable]
    public class ContractDTO
    {
        public string ContractId;
        public string ContractType;
        public string Status; // "Active", "Completed", "Cancelled"
        public float Value;
        public DateTime StartDate;
        public DateTime EndDate;
        public Dictionary<string, object> Terms = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for loan data
    /// </summary>
    [System.Serializable]
    public class LoanDTO
    {
        public string LoanId;
        public string LoanType;
        public float Principal;
        public float InterestRate;
        public float RemainingBalance;
        public DateTime StartDate;
        public DateTime DueDate;
        public string Status; // "Active", "Paid", "Overdue"
    }
    
}