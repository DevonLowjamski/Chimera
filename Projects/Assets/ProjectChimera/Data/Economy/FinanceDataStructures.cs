using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Shared;
// using ProjectChimera.Data.Economy; // Removed circular reference

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Comprehensive financial data structures for investment and finance management in Project Chimera.
    /// Handles loans, investments, financial planning, cash flow analysis, and business growth funding.
    /// </summary>

    [System.Serializable]
    public class FinancialSettings
    {
        [Range(0.01f, 0.5f)] public float BaseInterestRate = 0.05f; // 5% annual
        [Range(0.01f, 0.2f)] public float RiskAdjustmentFactor = 0.02f;
        [Range(0.1f, 2f)] public float InflationRate = 0.03f; // 3% annual
        [Range(1000f, 1000000f)] public float MinimumInvestment = 5000f;
        [Range(10000f, 10000000f)] public float MaximumLoanAmount = 500000f;
        public bool EnableDynamicRates = true;
        public bool EnableCreditScoring = true;
        public int MaxActiveLoans = 5;
        public int MaxActiveInvestments = 10;
    }

    [System.Serializable]
    public class LoanApplication
    {
        public string ApplicationId;
        public string ApplicantName;
        public DateTime ApplicationDate;
        public float RequestedAmount;
        public LoanType LoanType;
        public LoanPurpose Purpose;
        public int TermMonths;
        public float ProposedInterestRate;
        public LoanApplicationStatus Status;
        public CreditProfile CreditProfile;
        public BusinessPlan BusinessPlan;
        public List<Collateral> ProposedCollateral = new List<Collateral>();
        public string LenderName;
        public DateTime? ReviewDate;
        public string ReviewNotes;
        public LoanDecision Decision;
    }

    [System.Serializable]
    public class LoanContract
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
        public LoanStatus Status;
        public PaymentSchedule PaymentSchedule;
        public List<LoanPayment> PaymentHistory = new List<LoanPayment>();
        public List<Collateral> Collateral = new List<Collateral>();
        public LoanTerms Terms;
        public float TotalInterestPaid;
        public DateTime? LastPaymentDate;
        public int DaysDelinquent;
    }

    [System.Serializable]
    public class LoanPayment
    {
        public string PaymentId;
        public DateTime PaymentDate;
        public DateTime DueDate;
        public float PaymentAmount;
        public float PrincipalAmount;
        public float InterestAmount;
        public float LateFee;
        public PaymentStatus Status;
        public PaymentType PaymentMethodType;
        public string TransactionId;
        public float RemainingBalance;
    }

    [System.Serializable]
    public class PerformanceMetrics
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
    public class Investment
    {
        public string InvestmentId;
        public string InvestmentName;
        public InvestmentType InvestmentType;
        public DateTime InvestmentDate;
        public float InitialAmount;
        public float CurrentValue;
        public float ExpectedReturn;
        public float ActualReturn;
        public InvestmentRisk RiskLevel;
        public int DurationMonths;
        public InvestmentStatus Status;
        public List<InvestmentTransaction> Transactions = new List<InvestmentTransaction>();
        public PerformanceMetrics Performance;
        public string InvestmentDescription;
        public bool IsLiquid;
        public DateTime? MaturityDate;
        public float ManagementFee;
        
        // Compatibility properties
        public InvestmentType Type => InvestmentType;
        public float Amount => InitialAmount;
        public DateTime StartDate => InvestmentDate;
        public bool IsActive => Status == InvestmentStatus.Active;
    }

    [System.Serializable]
    public class InvestmentTransaction
    {
        public string TransactionId;
        public DateTime TransactionDate;
        public InvestmentTransactionType TransactionType;
        public float Amount;
        public float SharePrice;
        public float Shares;
        public float Fees;
        public string Description;
        public float TotalValue;
    }

    [System.Serializable]
    public class FinancialPlan
    {
        public string PlanId;
        public string PlanName;
        public DateTime CreatedDate;
        public DateTime LastUpdated;
        public PlanningHorizon Horizon;
        public List<FinancialGoal> Goals = new List<FinancialGoal>();
        public CashFlowProjection CashFlowProjection;
        public RiskAssessment RiskAssessment;
        public List<FinancialStrategy> Strategies = new List<FinancialStrategy>();
        public PerformanceTracking Performance;
        public bool IsActive;
        public float ConfidenceLevel;
    }

    [System.Serializable]
    public class FinancialGoal
    {
        public string GoalId;
        public string GoalName;
        public GoalType GoalType;
        public float TargetAmount;
        public DateTime TargetDate;
        public float CurrentProgress;
        public GoalPriority Priority;
        public List<string> RequiredActions = new List<string>();
        public float MonthlyContribution;
        public bool IsAchievable;
        public float CompletionPercentage;
    }

    [System.Serializable]
    public class CashFlowProjection
    {
        public DateTime ProjectionDate;
        public List<CashFlowPeriod> Periods = new List<CashFlowPeriod>();
        public float TotalProjectedInflow;
        public float TotalProjectedOutflow;
        public float NetCashFlow;
        public List<CashFlowScenario> Scenarios = new List<CashFlowScenario>();
        public float ConfidenceInterval;
    }

    [System.Serializable]
    public class CashFlowPeriod
    {
        public DateTime PeriodStart;
        public DateTime PeriodEnd;
        public float OpeningBalance;
        public float OperatingInflows;
        public float InvestmentInflows;
        public float FinancingInflows;
        public float OperatingOutflows;
        public float InvestmentOutflows;
        public float FinancingOutflows;
        public float NetCashFlow;
        public float ClosingBalance;
        public List<CashFlowItem> DetailedItems = new List<CashFlowItem>();
    }

    [System.Serializable]
    public class CashFlowItem
    {
        public string ItemName;
        public CashFlowCategory Category;
        public float Amount;
        public bool IsRecurring;
        public float Confidence;
        public string Notes;
    }

    [System.Serializable]
    public class CreditProfile
    {
        public int CreditScore;
        public CreditRating CreditRating;
        public float DebtToIncomeRatio;
        public float PaymentHistoryScore;
        public float CreditUtilization;
        public int CreditHistoryLength; // months
        public List<CreditAccount> CreditAccounts = new List<CreditAccount>();
        public List<CreditInquiry> RecentInquiries = new List<CreditInquiry>();
        public DateTime LastUpdated;
        public bool HasBankruptcy;
        public bool HasForeclosure;
        public int LatePayments12Months;
    }

    [System.Serializable]
    public class BusinessPlan
    {
        public string PlanId;
        public string BusinessName;
        public BusinessType BusinessType;
        public string ExecutiveSummary;
        public float StartupCosts;
        public float ProjectedRevenue1Year;
        public float ProjectedRevenue3Year;
        public float ProjectedRevenue5Year;
        public MarketAnalysis MarketAnalysis;
        public CompetitiveAnalysis CompetitiveAnalysis;
        public FinancialProjections FinancialProjections;
        public List<BusinessRisk> IdentifiedRisks = new List<BusinessRisk>();
        public float FeasibilityScore;
        public DateTime LastReviewed;
    }

    [System.Serializable]
    public class InvestmentOpportunity
    {
        public string OpportunityId;
        public string OpportunityName;
        public InvestmentType InvestmentType;
        public float MinimumInvestment;
        public float MaximumInvestment;
        public float ExpectedReturn;
        public InvestmentRisk RiskLevel;
        public int TimeHorizonMonths;
        public string Description;
        public List<string> KeyFeatures = new List<string>();
        public List<InvestmentRisk> Risks = new List<InvestmentRisk>();
        public float ManagementFee;
        public bool IsAvailable;
        public DateTime AvailableUntil;
        public InvestmentProvider Provider;
        public float MinimumCreditScore;
    }

    [System.Serializable]
    public class FinancialAnalysis
    {
        public DateTime AnalysisDate;
        public LiquidityAnalysis Liquidity;
        public ProfitabilityAnalysis Profitability;
        public EfficiencyAnalysis Efficiency;
        public LeverageAnalysis Leverage;
        public ValuationAnalysis Valuation;
        public TrendAnalysis Trends;
        public BenchmarkAnalysis Benchmarks;
        public List<FinancialRatio> KeyRatios = new List<FinancialRatio>();
        public List<string> Recommendations = new List<string>();
        public float OverallFinancialHealth;
    }

    [System.Serializable]
    public class BudgetPlan
    {
        public string BudgetId;
        public string BudgetName;
        public BudgetType BudgetType;
        public DateTime PeriodStart;
        public DateTime PeriodEnd;
        public List<BudgetCategory> Categories = new List<BudgetCategory>();
        public float TotalBudgetedIncome;
        public float TotalBudgetedExpenses;
        public float TotalActualIncome;
        public float TotalActualExpenses;
        public float BudgetVariance;
        public BudgetStatus Status;
        public List<BudgetAlert> Alerts = new List<BudgetAlert>();
    }

    [System.Serializable]
    public class BudgetCategory
    {
        public string CategoryName;
        public BudgetCategoryType CategoryType;
        public float BudgetedAmount;
        public float ActualAmount;
        public float Variance;
        public float VariancePercentage;
        public List<BudgetLineItem> LineItems = new List<BudgetLineItem>();
        public bool HasAlert;
        public string Notes;
    }

    [System.Serializable]
    public class InsurancePolicy
    {
        public string PolicyId;
        public string PolicyNumber;
        public InsuranceType InsuranceType;
        public string Provider;
        public DateTime EffectiveDate;
        public DateTime ExpirationDate;
        public float CoverageAmount;
        public float Premium;
        public PaymentFrequency PremiumFrequency;
        public float Deductible;
        public List<InsuranceCoverage> Coverages = new List<InsuranceCoverage>();
        public PolicyStatus Status;
        public List<InsuranceClaim> Claims = new List<InsuranceClaim>();
        public DateTime LastReviewDate;
        public bool AutoRenewal;
    }

    // Supporting classes
    [System.Serializable]
    public class LoanTerms
    {
        public bool RequiresCollateral;
        public bool AllowsEarlyPayment;
        public float EarlyPaymentPenalty;
        public bool HasGracePeriod;
        public int GracePeriodDays;
        public float LateFeeAmount;
        public float LateFeePercentage;
        public bool RequiresInsurance;
        public List<LoanCovenants> Covenants = new List<LoanCovenants>();
    }

    [System.Serializable]
    public class Collateral
    {
        public string CollateralId;
        public string Description;
        public CollateralType CollateralType;
        public float EstimatedValue;
        public float LoanToValueRatio;
        public DateTime LastAppraisal;
        public string AppraisalCompany;
        public CollateralStatus Status;
    }

    [System.Serializable]
    public class RiskAssessment
    {
        public RiskTolerance RiskTolerance;
        public float RiskScore;
        public List<RiskFactor> IdentifiedRisks = new List<RiskFactor>();
        public List<string> MitigationStrategies = new List<string>();
        public float ConcentrationRisk;
        public float LiquidityRisk;
        public float MarketRisk;
        public float CreditRisk;
        public DateTime LastAssessment;
        
        // Additional properties for Economic Gaming System compatibility
        public string AssetId;
        public decimal InvestmentAmount;
        public DateTime AssessmentDate;
        public RiskLevel RiskLevel;
    }

    [System.Serializable]
    public class MarketAnalysis
    {
        public float MarketSize;
        public float MarketGrowthRate;
        public float TargetMarketShare;
        public string MarketTrends;
        public List<string> CustomerSegments = new List<string>();
        public SeasonalityAnalysis Seasonality;
        public float MarketPenetration;
    }

    [System.Serializable]
    public class FinancialProjections
    {
        public List<ProjectionPeriod> Periods = new List<ProjectionPeriod>();
        public float BreakEvenPoint;
        public float PaybackPeriod;
        public float NetPresentValue;
        public float InternalRateOfReturn;
        public string Assumptions;
        public SensitivityAnalysis Sensitivity;
    }

    // Enums for financial system
    public enum LoanType
    {
        Equipment_Financing,
        Working_Capital,
        Real_Estate,
        Business_Expansion,
        Emergency_Fund,
        Refinancing,
        Bridge_Loan,
        Construction_Loan
    }

    public enum LoanPurpose
    {
        Facility_Construction,
        Equipment_Purchase,
        Working_Capital,
        Business_Expansion,
        Debt_Consolidation,
        Emergency_Funding,
        Research_Development,
        Market_Entry
    }

    public enum LoanApplicationStatus
    {
        Submitted,
        Under_Review,
        Documentation_Required,
        Credit_Check,
        Underwriting,
        Approved,
        Rejected,
        Withdrawn,
        Expired
    }

    public enum LoanStatus
    {
        Active,
        Current,
        Delinquent,
        Default,
        Paid_Off,
        Charged_Off,
        In_Bankruptcy,
        Restructured
    }

    public enum PaymentStatus
    {
        Scheduled,
        Paid,
        Late,
        Missed,
        Partial,
        Returned,
        Waived
    }

    public enum InvestmentType
    {
        Cultivation_Equipment,
        Real_Estate,
        Technology_Upgrade,
        Market_Expansion,
        Research_Development,
        Brand_Development,
        Distribution_Network,
        Cannabis_Stocks,
        Facility_Improvement,
        Renewable_Energy
    }

    public enum InvestmentRisk
    {
        Very_Low,
        Low,
        Moderate,
        High,
        Very_High,
        Speculative
    }

    public enum InvestmentStatus
    {
        Active,
        Pending,
        Matured,
        Liquidated,
        Suspended,
        Under_Review
    }

    public enum InvestmentTransactionType
    {
        Initial_Investment,
        Additional_Investment,
        Dividend_Payment,
        Interest_Payment,
        Capital_Gain,
        Capital_Loss,
        Fee_Payment,
        Withdrawal,
        Reinvestment
    }

    public enum GoalType
    {
        Facility_Expansion,
        Equipment_Upgrade,
        Market_Entry,
        Revenue_Target,
        Profit_Margin,
        Debt_Reduction,
        Emergency_Fund,
        Retirement_Planning
    }

    public enum GoalPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum PlanningHorizon
    {
        Short_Term_1Year,
        Medium_Term_3Year,
        Long_Term_5Year,
        Strategic_10Year
    }

    public enum CashFlowCategory
    {
        Operating_Income,
        Investment_Income,
        Financing_Income,
        Operating_Expense,
        Investment_Expense,
        Financing_Expense,
        Tax_Payment,
        Loan_Payment
    }

    public enum CreditRating
    {
        Excellent_800_Plus,
        Very_Good_740_799,
        Good_670_739,
        Fair_580_669,
        Poor_Below_580
    }

    public enum BusinessType
    {
        Cultivation_Facility,
        Processing_Lab,
        Dispensary,
        Distribution_Company,
        Technology_Provider,
        Consulting_Service,
        Testing_Laboratory,
        Equipment_Manufacturer
    }

    public enum BudgetType
    {
        Operating_Budget,
        Capital_Budget,
        Cash_Budget,
        Master_Budget,
        Flexible_Budget,
        Zero_Based_Budget
    }

    public enum BudgetCategoryType
    {
        Revenue,
        Cost_of_Goods_Sold,
        Operating_Expense,
        Capital_Expenditure,
        Financing_Cost,
        Tax_Expense
    }

    public enum InsuranceType
    {
        General_Liability,
        Product_Liability,
        Property_Insurance,
        Business_Interruption,
        Workers_Compensation,
        Cyber_Liability,
        Directors_Officers,
        Key_Person_Life
    }

    public enum PolicyStatus
    {
        Active,
        Pending,
        Lapsed,
        Cancelled,
        Expired,
        Under_Review
    }

    public enum PaymentFrequency
    {
        Monthly,
        Quarterly,
        Semi_Annual,
        Annual,
        One_Time
    }

    public enum LoanDecision
    {
        Pending,
        Approved,
        Approved_With_Conditions,
        Rejected_Credit,
        Rejected_Collateral,
        Rejected_Income,
        Rejected_Other,
        Counter_Offer
    }

    public enum CollateralType
    {
        Real_Estate,
        Equipment,
        Inventory,
        Accounts_Receivable,
        Securities,
        Personal_Guarantee,
        Cash_Deposit
    }

    public enum CollateralStatus
    {
        Pledged,
        Released,
        Liquidated,
        Under_Review,
        Disputed
    }

    // Additional supporting classes
    [System.Serializable]
    public class CreditAccount
    {
        public string AccountName;
        public string AccountType;
        public float CreditLimit;
        public float CurrentBalance;
        public float UsedCredit;
        public float PaymentDue;
        public float MinimumPayment;
        public DateTime LastPayment;
        public int MonthsOnRecord;
        public PaymentHistory PaymentHistory;
        
        // Additional properties for compatibility
        public float InterestRate;
        public float CreditScore;
        public DateTime LastPaymentDate;
    }

    [System.Serializable]
    public class PaymentHistory
    {
        public int OnTimePayments;
        public int LatePayments30Days;
        public int LatePayments60Days;
        public int LatePayments90Plus;
        public float PaymentHistoryPercentage;
    }

    [System.Serializable]
    public class CreditInquiry
    {
        public DateTime InquiryDate;
        public string InquiryType;
        public string CreditorName;
        public string InquiryReason;
    }

    [System.Serializable]
    public class CompetitiveAnalysis
    {
        public List<string> MainCompetitors = new List<string>();
        public string CompetitiveAdvantage;
        public float MarketPosition;
        public string DifferentiationStrategy;
        public List<string> CompetitiveThreats = new List<string>();
    }

    [System.Serializable]
    public class BusinessRisk
    {
        public string RiskName;
        public RiskCategory Category;
        public RiskLevel Level;
        public string Description;
        public string MitigationStrategy;
        public float Impact;
        public float Probability;
    }

    [System.Serializable]
    public class RiskFactor
    {
        public string FactorName;
        public RiskLevel Level;
        public string Description;
        public float Weight;
    }

    [System.Serializable]
    public class FinancialStrategy
    {
        public string StrategyName;
        public StrategyType Type;
        public string Description;
        public List<string> Actions = new List<string>();
        public float ExpectedImpact;
        public DateTime ImplementationDate;
        public StrategyStatus Status;
    }

    [System.Serializable]
    public class PerformanceTracking
    {
        public float ActualVsPlanned;
        public List<PerformanceMetric> Metrics = new List<PerformanceMetric>();
        public DateTime LastUpdate;
        public string Summary;
    }

    [System.Serializable]
    public class PerformanceMetric
    {
        public string MetricName;
        public float TargetValue;
        public float ActualValue;
        public float Variance;
        public string Unit;
    }

    [System.Serializable]
    public class CashFlowScenario
    {
        public string ScenarioName;
        public ScenarioType Type;
        public float Probability;
        public List<CashFlowPeriod> Periods = new List<CashFlowPeriod>();
        public string Description;
    }

    [System.Serializable]
    public class LiquidityAnalysis
    {
        public float CurrentRatio;
        public float QuickRatio;
        public float CashRatio;
        public float WorkingCapital;
        public int CashConversionCycle;
    }

    [System.Serializable]
    public class ProfitabilityAnalysis
    {
        public float GrossMargin;
        public float OperatingMargin;
        public float NetMargin;
        public float ReturnOnAssets;
        public float ReturnOnEquity;
    }

    [System.Serializable]
    public class EfficiencyAnalysis
    {
        public float AssetTurnover;
        public float InventoryTurnover;
        public float ReceivablesTurnover;
        public float PayablesTurnover;
        public int DaysInInventory;
    }

    [System.Serializable]
    public class LeverageAnalysis
    {
        public float DebtToEquity;
        public float DebtToAssets;
        public float InterestCoverage;
        public float DebtService;
        public float FinancialLeverage;
    }

    [System.Serializable]
    public class ValuationAnalysis
    {
        public float BookValue;
        public float MarketValue;
        public float EarningsMultiple;
        public float RevenueMultiple;
        public float AssetBasedValue;
    }

    [System.Serializable]
    public class TrendAnalysis
    {
        public List<TrendMetric> Metrics = new List<TrendMetric>();
        public string OverallTrend;
        public float TrendStrength;
        public List<string> KeyInsights = new List<string>();
    }

    [System.Serializable]
    public class TrendMetric
    {
        public string MetricName;
        public List<float> Values = new List<float>();
        public List<DateTime> Dates = new List<DateTime>();
        public float TrendSlope;
        public string TrendDirection;
    }

    [System.Serializable]
    public class BenchmarkAnalysis
    {
        public string IndustryBenchmark;
        public List<BenchmarkMetric> Metrics = new List<BenchmarkMetric>();
        public float OverallPerformance;
        public string Summary;
    }

    [System.Serializable]
    public class BenchmarkMetric
    {
        public string MetricName;
        public float YourValue;
        public float IndustryAverage;
        public float TopQuartile;
        public string Performance;
    }

    [System.Serializable]
    public class FinancialRatio
    {
        public string RatioName;
        public float Value;
        public string Category;
        public string Interpretation;
        public bool IsHealthy;
    }

    [System.Serializable]
    public class BudgetLineItem
    {
        public string ItemName;
        public float BudgetedAmount;
        public float ActualAmount;
        public string Description;
        public bool IsRecurring;
    }

    [System.Serializable]
    public class BudgetAlert
    {
        public string AlertMessage;
        public AlertSeverity Severity;
        public string CategoryName;
        public float ThresholdExceeded;
        public DateTime AlertDate;
    }

    [System.Serializable]
    public class InsuranceCoverage
    {
        public string CoverageName;
        public float CoverageLimit;
        public float Deductible;
        public string Description;
        public bool IsActive;
    }

    [System.Serializable]
    public class InsuranceClaim
    {
        public string ClaimNumber;
        public DateTime ClaimDate;
        public string ClaimType;
        public float ClaimAmount;
        public ClaimStatus Status;
        public string Description;
        public DateTime? SettlementDate;
        public float SettlementAmount;
    }

    [System.Serializable]
    public class LoanCovenants
    {
        public string CovenantName;
        public CovenantType Type;
        public string Description;
        public float Threshold;
        public bool IsCompliant;
    }

    [System.Serializable]
    public class SeasonalityAnalysis
    {
        public List<SeasonalFactor> Factors = new List<SeasonalFactor>();
        public float SeasonalityIndex;
        public string PeakSeason;
        public string LowSeason;
    }

    [System.Serializable]
    public class SeasonalFactor
    {
        public string Month;
        public float Factor;
        public string Description;
    }

    [System.Serializable]
    public class ProjectionPeriod
    {
        public int Year;
        public float Revenue;
        public float Expenses;
        public float Profit;
        public float CashFlow;
        public string Assumptions;
    }

    [System.Serializable]
    public class SensitivityAnalysis
    {
        public List<SensitivityVariable> Variables = new List<SensitivityVariable>();
        public string Summary;
        public float OverallSensitivity;
    }

    [System.Serializable]
    public class SensitivityVariable
    {
        public string VariableName;
        public float BaseCase;
        public float WorstCase;
        public float BestCase;
        public float Impact;
    }

    [System.Serializable]
    public class InvestmentProvider
    {
        public string ProviderName;
        public string ContactInfo;
        public ProviderType Type;
        public float MinimumInvestment;
        public List<InvestmentType> SpecializedTypes = new List<InvestmentType>();
        public float SuccessRate;
    }

    [System.Serializable]
    public class PaymentSchedule
    {
        public List<ScheduledPayment> Payments = new List<ScheduledPayment>();
        public PaymentFrequency Frequency;
        public float PaymentAmount;
        public DateTime FirstPaymentDate;
        public DateTime LastPaymentDate;
    }

    [System.Serializable]
    public class ScheduledPayment
    {
        public int PaymentNumber;
        public DateTime DueDate;
        public float PaymentAmount;
        public float PrincipalAmount;
        public float InterestAmount;
        public float RemainingBalance;
        public bool IsPaid;
    }

    // Additional enums
    public enum RiskCategory
    {
        Market_Risk,
        Credit_Risk,
        Operational_Risk,
        Regulatory_Risk,
        Technology_Risk,
        Reputation_Risk
    }

    public enum RiskLevel
    {
        Very_Low,
        Low,
        Medium,
        High,
        Very_High
    }

    public enum StrategyType
    {
        Growth_Strategy,
        Cost_Reduction,
        Risk_Management,
        Investment_Strategy,
        Debt_Management,
        Cash_Management
    }

    public enum StrategyStatus
    {
        Planned,
        In_Progress,
        Completed,
        On_Hold,
        Cancelled
    }

    public enum ScenarioType
    {
        Best_Case,
        Most_Likely,
        Worst_Case,
        Stress_Test
    }

    public enum BudgetStatus
    {
        Draft,
        Approved,
        Active,
        Under_Review,
        Closed
    }

    public enum ClaimStatus
    {
        Submitted,
        Under_Investigation,
        Approved,
        Denied,
        Settled,
        Closed
    }

    public enum CovenantType
    {
        Financial_Covenant,
        Operational_Covenant,
        Reporting_Covenant,
        Negative_Covenant
    }

    public enum ProviderType
    {
        Bank,
        Credit_Union,
        Private_Lender,
        Investment_Firm,
        Venture_Capital,
        Angel_Investor,
        Government_Program
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Alert,
        Critical
    }

    public enum PaymentType
    {
        Cash,
        Bank_Transfer,
        Credit,
        Cryptocurrency,
        Barter,
        Consignment
    }

    /// <summary>
    /// Financial record for transaction history
    /// </summary>
    [System.Serializable]
    public class FinancialRecord
    {
        public string TransactionId;
        public FinancialTransactionType TransactionType;
        public float Amount;
        public string Description;
        public DateTime TransactionDate = DateTime.Now;
        public string Category;
        public string Account;
        public float BalanceAfter;
        public string Notes;
        public float Timestamp = 0f; // For compatibility with trading system
    }

    /// <summary>
    /// Financial transaction types
    /// </summary>
    public enum FinancialTransactionType
    {
        Purchase,
        Sale,
        Transfer,
        Loan_Payment,
        Investment,
        Withdrawal,
        Deposit,
        Refund,
        Fee,
        Fee_Paid,
        Interest
    }
}