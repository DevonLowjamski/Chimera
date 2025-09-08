using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Data structures for financial analytics, risk assessment, and performance analysis
    /// </summary>

    [System.Serializable]
    public class FinancialAnalysis
    {
        public string AnalysisId;
        public DateTime AnalysisDate;
        public List<FinancialRatio> Ratios = new List<FinancialRatio>();
        public LiquidityAnalysis Liquidity;
        public ProfitabilityAnalysis Profitability;
        public EfficiencyAnalysis Efficiency;
        public LeverageAnalysis Leverage;
        public ValuationAnalysis Valuation;
        public TrendAnalysis Trends;
        public string AnalysisNotes;
        public string Recommendations;
    }





    [System.Serializable]
    public class MarketAnalysis
    {
        public string AnalysisId;
        public DateTime AnalysisDate;
        public float MarketSize;
        public float MarketGrowthRate;
        public Dictionary<string, float> MarketSegments = new Dictionary<string, float>();
        public List<string> KeyTrends = new List<string>();
        public string MarketOutlook;
    }

    [System.Serializable]
    public class LiquidityAnalysis
    {
        public string AnalysisId;
        public float CurrentRatio;
        public float QuickRatio;
        public float CashRatio;
        public float OperatingCashFlowRatio;
        public float DaysInCash;
        public float WorkingCapital;
        public DateTime AnalysisDate;
        public string LiquidityAssessment; // "Strong", "Adequate", "Weak"
    }

    [System.Serializable]
    public class ProfitabilityAnalysis
    {
        public string AnalysisId;
        public float GrossProfitMargin;
        public float OperatingProfitMargin;
        public float NetProfitMargin;
        public float ReturnOnAssets;
        public float ReturnOnEquity;
        public float ReturnOnInvestment;
        public float EarningsPerShare;
        public DateTime AnalysisDate;
        public string ProfitabilityTrend; // "Improving", "Stable", "Declining"
    }

    [System.Serializable]
    public class EfficiencyAnalysis
    {
        public string AnalysisId;
        public float AssetTurnover;
        public float InventoryTurnover;
        public float ReceivablesTurnover;
        public float PayablesTurnover;
        public float DaysInInventory;
        public float DaysInReceivables;
        public float DaysInPayables;
        public float CashConversionCycle;
        public DateTime AnalysisDate;
        public string EfficiencyRating; // "Excellent", "Good", "Average", "Poor"
    }

    [System.Serializable]
    public class LeverageAnalysis
    {
        public string AnalysisId;
        public float DebtToEquityRatio;
        public float DebtToAssetsRatio;
        public float InterestCoverageRatio;
        public float DebtServiceCoverageRatio;
        public float EquityMultiplier;
        public float CapitalizationRatio;
        public DateTime AnalysisDate;
        public string LeverageAssessment; // "Conservative", "Moderate", "Aggressive"
    }

    [System.Serializable]
    public class ValuationAnalysis
    {
        public string AnalysisId;
        public float BookValue;
        public float MarketValue;
        public float IntrinsicValue;
        public float PriceToEarningsRatio;
        public float PriceToBookRatio;
        public float PriceToSalesRatio;
        public float EarningsBeforeInterestTaxes;
        public float EconomicValueAdded;
        public DateTime AnalysisDate;
        public string ValuationConclusion;
    }

    [System.Serializable]
    public class TrendAnalysis
    {
        public string AnalysisId;
        public List<TrendMetric> TrendMetrics = new List<TrendMetric>();
        public DateTime AnalysisPeriodStart;
        public DateTime AnalysisPeriodEnd;
        public string OverallTrend; // "Positive", "Neutral", "Negative"
        public Dictionary<string, string> MetricTrends = new Dictionary<string, string>();
    }

    [System.Serializable]
    public class TrendMetric
    {
        public string MetricName;
        public List<float> HistoricalValues = new List<float>();
        public List<DateTime> ValueDates = new List<DateTime>();
        public float TrendSlope;
        public float RSquared; // Goodness of fit
        public string TrendDirection; // "Increasing", "Decreasing", "Stable"
        public float VolatilityMeasure;
    }

    [System.Serializable]
    public class FinancialRatio
    {
        public string RatioName;
        public float CurrentValue;
        public float BenchmarkValue;
        public float IndustryAverage;
        public string Category; // "Liquidity", "Profitability", "Efficiency", etc.
        public string Interpretation;
        public DateTime CalculationDate;
    }

    [System.Serializable]
    public class InsurancePolicy
    {
        public string PolicyId;
        public string PolicyNumber;
        public InsuranceType Type;
        public string InsuranceProvider;
        public float CoverageAmount;
        public float AnnualPremium;
        public float Deductible;
        public DateTime PolicyStartDate;
        public DateTime PolicyEndDate;
        public PolicyStatus Status;
        public List<InsuranceCoverage> Coverages = new List<InsuranceCoverage>();
        public List<InsuranceClaim> Claims = new List<InsuranceClaim>();
        public bool AutoRenew;
        public DateTime LastReviewDate;
        public string BeneficiaryInformation;
    }

    [System.Serializable]
    public class InsuranceCoverage
    {
        public string CoverageId;
        public string CoverageType;
        public float CoverageLimit;
        public float Deductible;
        public string Description;
        public List<string> Exclusions = new List<string>();
    }

    [System.Serializable]
    public class InsuranceClaim
    {
        public string ClaimId;
        public string PolicyId;
        public DateTime ClaimDate;
        public DateTime IncidentDate;
        public float ClaimAmount;
        public float SettlementAmount;
        public ClaimStatus Status;
        public string ClaimDescription;
        public List<string> SupportingDocuments = new List<string>();
        public DateTime SettlementDate;
    }

    [System.Serializable]
    public class FinancialRecord
    {
        public string RecordId;
        public DateTime RecordDate;
        public FinancialTransactionType TransactionType;
        public string AccountCode;
        public string Description;
        public float DebitAmount;
        public float CreditAmount;
        public float Balance;
        public string Reference;
        public bool IsReconciled;
        public DateTime ReconciliationDate;
        public Dictionary<string, object> AdditionalData = new Dictionary<string, object>();
    }

    // Financial Analytics related enumerations
    public enum InsuranceType
    {
        GeneralLiability,
        PropertyAndCasualty,
        WorkersCompensation,
        ProfessionalLiability,
        ProductLiability,
        CyberLiability,
        KeyPersonLife,
        BusinessInterruption,
        EquipmentBreakdown,
        Other
    }

    public enum PolicyStatus
    {
        Active,
        Expired,
        Cancelled,
        Pending,
        UnderReview,
        Suspended,
        Lapsed
    }

    public enum ClaimStatus
    {
        Reported,
        UnderReview,
        InvestigationInProgress,
        Approved,
        Denied,
        Settled,
        Closed,
        Disputed
    }

    public enum FinancialTransactionType
    {
        Revenue,
        Expense,
        Asset,
        Liability,
        Equity,
        Transfer,
        Adjustment,
        Accrual,
        Depreciation,
        Other
    }
}
