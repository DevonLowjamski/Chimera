using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Data Transfer Objects for Financial System Save/Load Operations
    /// </summary>

    /// <summary>
    /// DTO for financial system state
    /// </summary>
    [System.Serializable]
    public class FinancialStateDTO
    {
        [Header("Economic Indicators")]
        public EconomicIndicatorsDTO EconomicIndicators;
        public EconomicForecastDTO EconomicForecast;

        [Header("Investment Management")]
        public List<InvestmentDTO> AvailableInvestments = new List<InvestmentDTO>();
        public List<InvestmentTransactionDTO> InvestmentTransactions = new List<InvestmentTransactionDTO>();

        [Header("Loan Management")]
        public List<LoanContractDTO> LoanContracts = new List<LoanContractDTO>();
        public List<LoanDTO> AvailableLoans = new List<LoanDTO>();
        public List<LoanDTO> ActiveLoans = new List<LoanDTO>();

        [Header("Financial Settings")]
        public FinancialSettingsDTO FinancialSettings;

        [Header("System Status")]
        public bool IsFinancialSystemActive = true;
        public DateTime LastFinancialUpdate;
    }

    /// <summary>
    /// DTO for economic indicators
    /// </summary>
    [System.Serializable]
    public class EconomicIndicatorsDTO
    {
        [Header("Primary Indicators")]
        public float GDP; // Gross Domestic Product
        public float GDPGrowthRate;
        public float InflationRate;
        public float InterestRate;
        public float UnemploymentRate;

        [Header("Market Indicators")]
        public float StockMarketIndex;
        public float CommodityPriceIndex;
        public float CurrencyExchangeRate;
        public float TradeBalance;

        [Header("Business Climate")]
        public float BusinessConfidenceIndex;
        public float ConsumerConfidenceIndex;
        public float ManufacturingIndex;
        public float ServicesSectorIndex;

        [Header("Financial Sector")]
        public float BankLendingRate;
        public float CorporateBondYield;
        public float GovernmentBondYield;

        [Header("Indicator Metadata")]
        public DateTime LastUpdate;
        public Dictionary<string, float> CustomIndicators = new Dictionary<string, float>();
    }

    /// <summary>
    /// DTO for economic forecasting
    /// </summary>
    [System.Serializable]
    public class EconomicForecastDTO
    {
        [Header("Forecast Period")]
        public DateTime ForecastDate;
        public int ForecastPeriodMonths;

        [Header("Economic Projections")]
        public List<float> GDPProjections = new List<float>();
        public List<float> InflationProjections = new List<float>();
        public List<float> InterestRateProjections = new List<float>();
        public List<float> UnemploymentProjections = new List<float>();

        [Header("Market Projections")]
        public List<float> StockMarketProjections = new List<float>();
        public Dictionary<string, List<float>> SectorProjections = new Dictionary<string, List<float>>();

        [Header("Risk Assessment")]
        public float EconomicRiskLevel; // 0.0 to 1.0
        public List<string> RiskFactors = new List<string>();
        public Dictionary<string, float> RiskProbabilities = new Dictionary<string, float>();

        [Header("Confidence Intervals")]
        public Dictionary<string, float> ForecastConfidence = new Dictionary<string, float>();

        [Header("Scenario Analysis")]
        public Dictionary<string, EconomicScenarioDTO> Scenarios = new Dictionary<string, EconomicScenarioDTO>();
    }

    /// <summary>
    /// DTO for investment opportunities
    /// </summary>
    [System.Serializable]
    public class InvestmentDTO
    {
        [Header("Investment Identity")]
        public string InvestmentId;
        public string InvestmentName;
        public string InvestmentType; // "Stock", "Bond", "RealEstate", "Commodity", etc.
        public string Sector;

        [Header("Financial Details")]
        public float CurrentPrice;
        public float CurrentValue; // Added for compatibility with save system
        public float MinimumInvestment;
        public float ExpectedReturn; // Annual percentage
        public float RiskLevel; // 0.0 to 1.0

        [Header("Performance History")]
        public List<float> PriceHistory = new List<float>();
        public List<DateTime> PriceDates = new List<DateTime>();
        public float YTDReturn;
        public float OneYearReturn;
        public float ThreeYearReturn;

        [Header("Investment Details")]
        public string Description;
        public List<string> KeyFeatures = new List<string>();
        public Dictionary<string, object> AdditionalDetails = new Dictionary<string, object>();

        [Header("Liquidity and Terms")]
        public bool IsLiquid;
        public int LiquidityDays; // Days to convert to cash
        public DateTime MaturityDate;
        public float DividendYield;

        [Header("Investment Status")]
        public bool IsAvailable;
        public bool IsActive;
        public DateTime LastUpdated;
    }

    /// <summary>
    /// DTO for loan contracts and terms
    /// </summary>
    [System.Serializable]
    public class LoanContractDTO
    {
        [Header("Loan Identity")]
        public string LoanId;
        public string LoanType; // "Personal", "Business", "Mortgage", "Equipment"
        public string LenderName;

        [Header("Loan Terms")]
        public LoanTermsDTO LoanTerms;

        [Header("Financial Details")]
        public float PrincipalAmount;
        public float InterestRate;
        public float MonthlyPayment;
        public float TotalInterest;
        public float TotalRepayment;

        [Header("Loan Status")]
        public string LoanStatus; // "Active", "Approved", "Pending", "Rejected", "Paid Off"
        public float OutstandingBalance;
        public DateTime NextPaymentDate;
        public List<LoanPaymentDTO> PaymentHistory = new List<LoanPaymentDTO>();

        [Header("Contract Dates")]
        public DateTime ApplicationDate;
        public DateTime ApprovalDate;
        public DateTime DisbursementDate;
        public DateTime MaturityDate;

        [Header("Collateral and Guarantees")]
        public List<string> CollateralItems = new List<string>();
        public List<string> Guarantors = new List<string>();
        public float CollateralValue;

        [Header("Legal and Compliance")]
        public List<string> ContractTerms = new List<string>();
        public Dictionary<string, object> LegalDocuments = new Dictionary<string, object>();
    }

    /// <summary>
    /// DTO for loan terms and conditions
    /// </summary>
    [System.Serializable]
    public class LoanTermsDTO
    {
        [Header("Basic Terms")]
        public int LoanTermMonths;
        public float InterestRate;
        public string InterestType; // "Fixed", "Variable"
        public string RepaymentFrequency; // "Monthly", "Quarterly", "Annually"

        [Header("Fees and Penalties")]
        public float OriginationFee;
        public float ProcessingFee;
        public float LatePaymentPenalty;
        public float EarlyRepaymentPenalty;

        [Header("Requirements")]
        public float MinimumCreditScore;
        public float MaximumDebtToIncomeRatio;
        public float MinimumIncome;
        public List<string> RequiredDocuments = new List<string>();

        [Header("Special Conditions")]
        public bool AllowsEarlyRepayment;
        public bool AllowsRefinancing;
        public bool RequiresInsurance;
        public Dictionary<string, object> SpecialTerms = new Dictionary<string, object>();
    }

    /// <summary>
    /// DTO for loan payment records
    /// </summary>
    [System.Serializable]
    public class LoanPaymentDTO
    {
        [Header("Payment Details")]
        public string PaymentId;
        public string LoanId;
        public DateTime PaymentDate;
        public DateTime DueDate;

        [Header("Payment Breakdown")]
        public float PaymentAmount;
        public float PrincipalPaid;
        public float InterestPaid;
        public float FeePaid;
        public float PenaltyPaid;

        [Header("Balance Information")]
        public float RemainingBalance;
        public int RemainingPayments;

        [Header("Payment Status")]
        public string PaymentStatus; // "Scheduled", "Paid", "Late", "Missed"
        public string PaymentMethod;
        public bool IsAutoPayment;
    }

    /// <summary>
    /// DTO for investment transactions
    /// </summary>
    [System.Serializable]
    public class InvestmentTransactionDTO
    {
        [Header("Transaction Details")]
        public string TransactionId;
        public string InvestmentId;
        public DateTime TransactionDate;
        public string TransactionType; // "Buy", "Sell", "Dividend", "Split"

        [Header("Financial Information")]
        public float Quantity;
        public float UnitPrice;
        public float TotalAmount;
        public float TransactionFees;
        public float NetAmount;

        [Header("Performance Impact")]
        public float RealizedGain;
        public float UnrealizedGain;
        public float PerformanceImpact;

        [Header("Transaction Context")]
        public string Reason; // Why the transaction was made
        public bool IsAutomated;
        public string ExecutedBy;
    }

    /// <summary>
    /// DTO for performance metrics
    /// </summary>
    [System.Serializable]
    public class PerformanceMetricsDTO
    {
        [Header("Return Metrics")]
        public float TotalReturn;
        public float AnnualizedReturn;
        public float RiskAdjustedReturn;
        public float Volatility;

        [Header("Risk Metrics")]
        public float Beta; // Measure of systematic risk
        public float Alpha; // Excess return over benchmark
        public float SharpeRatio;
        public float MaxDrawdown;

        [Header("Performance Attribution")]
        public Dictionary<string, float> SectorContribution = new Dictionary<string, float>();
        public Dictionary<string, float> AssetClassContribution = new Dictionary<string, float>();

        [Header("Benchmark Comparison")]
        public float BenchmarkReturn;
        public float ExcessReturn;
        public float InformationRatio;
        public float TrackingError;

        [Header("Time Period")]
        public DateTime PerformanceStartDate;
        public DateTime PerformanceEndDate;
        public int PerformancePeriodDays;
    }

    /// <summary>
    /// DTO for financial system settings
    /// </summary>
    [System.Serializable]
    public class FinancialSettingsDTO
    {
        [Header("System Configuration")]
        public bool EnableFinancialSystem = true;
        public bool EnableEconomicForecasting = true;
        public bool EnableInvestmentTracking = true;
        public bool EnableLoanManagement = true;

        [Header("Update Frequencies")]
        public float EconomicDataUpdateFrequency = 24.0f; // Hours
        public float InvestmentDataUpdateFrequency = 1.0f; // Hours
        public float LoanCalculationFrequency = 24.0f; // Hours

        [Header("Risk Management")]
        public float MaximumRiskTolerance = 0.8f;
        public bool EnableAutomaticRebalancing = false;
        public float RebalancingThreshold = 0.05f; // 5% deviation

        [Header("Reporting Settings")]
        public bool GeneratePerformanceReports = true;
        public bool GenerateRiskReports = true;
        public bool GenerateTaxReports = true;
        public string ReportingCurrency = "USD";

        [Header("Compliance Settings")]
        public bool EnableComplianceChecks = true;
        public List<string> ComplianceRules = new List<string>();
        public Dictionary<string, object> RegulatorySettings = new Dictionary<string, object>();
    }

    /// <summary>
    /// DTO for financial planning
    /// </summary>
    [System.Serializable]
    public class FinancialPlanDTO
    {
        [Header("Plan Details")]
        public string PlanId;
        public string PlanName;
        public DateTime PlanStartDate;
        public DateTime PlanEndDate;
        public string PlanType; // "Retirement", "Education", "Emergency", "Wealth Building"

        [Header("Financial Goals")]
        public List<FinancialGoalDTO> Goals = new List<FinancialGoalDTO>();
        public float TotalTargetAmount;

        [Header("Investment Strategy")]
        public FinancialStrategyDTO Strategy;
        public Dictionary<string, float> AssetAllocation = new Dictionary<string, float>();

        [Header("Progress Tracking")]
        public float CurrentProgress; // 0.0 to 1.0
        public float MonthlyContribution;
        public DateTime LastPlanUpdate;
    }

    /// <summary>
    /// DTO for financial strategies
    /// </summary>
    [System.Serializable]
    public class FinancialStrategyDTO
    {
        [Header("Strategy Overview")]
        public string StrategyId;
        public string StrategyName;
        public string StrategyType; // "Conservative", "Moderate", "Aggressive", "Custom"

        [Header("Risk Profile")]
        public float RiskTolerance; // 0.0 to 1.0
        public string RiskProfile;
        public List<string> RiskFactors = new List<string>();

        [Header("Investment Approach")]
        public bool PrefersDiversification = true;
        public bool PrefersLiquidity = false;
        public bool FocusOnGrowth = true;
        public bool FocusOnIncome = false;

        [Header("Strategy Rules")]
        public List<string> InvestmentRules = new List<string>();
        public Dictionary<string, object> StrategyParameters = new Dictionary<string, object>();
    }

    /// <summary>
    /// Supporting DTOs for financial system
    /// </summary>

    [System.Serializable]
    public class EconomicScenarioDTO
    {
        public string ScenarioName;
        public float Probability;
        public Dictionary<string, float> EconomicProjections = new Dictionary<string, float>();
        public List<string> KeyAssumptions = new List<string>();
        public float ImpactScore; // -1.0 to 1.0
    }

    [System.Serializable]
    public class LoanDTO
    {
        public string LoanId;
        public string LoanType;
        public string LenderName;
        public float MaxLoanAmount;
        public float InterestRate;
        public int MaxTermMonths;
        public float MinimumCreditScore;
        public bool IsAvailable;
        public LoanTermsDTO Terms;
    }
}
