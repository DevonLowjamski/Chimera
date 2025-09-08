using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Data structures for investment management and opportunities
    /// </summary>

    [System.Serializable]
    public class Investment
    {
        public string InvestmentId;
        public string InvestmentName;
        public InvestmentType InvestmentType;
        public float InitialAmount;
        public float CurrentValue;
        public float ExpectedReturn;
        public DateTime PurchaseDate;
        public bool IsActive;
        public InvestmentRisk RiskLevel;
        public DateTime InvestmentDate;
        public DateTime? MaturityDate;
        public InvestmentStatus Status;
        public InvestmentProvider Provider;
        public List<InvestmentTransaction> TransactionHistory = new List<InvestmentTransaction>();
        public PerformanceMetrics Performance;
        public Dictionary<string, object> InvestmentDetails = new Dictionary<string, object>();
        public bool AutoReinvest;
        public float ManagementFeePercent;
        public DateTime LastValuationDate;
    }

    [System.Serializable]
    public class InvestmentTransaction
    {
        public string TransactionId;
        public string InvestmentId;
        public InvestmentTransactionType TransactionType;
        public DateTime TransactionDate;
        public float Amount;
        public float SharePrice;
        public int SharesTransacted;
        public float TransactionFee;
        public string Description;
        public Dictionary<string, object> TransactionData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class InvestmentOpportunity
    {
        public string OpportunityId;
        public string OpportunityName;
        public InvestmentType Type;
        public float MinimumInvestment;
        public float MaximumInvestment;
        public float ExpectedReturn;
        public InvestmentRisk RiskLevel;
        public DateTime OpportunityStart;
        public DateTime OpportunityEnd;
        public string Description;
        public List<string> KeyFeatures = new List<string>();
        public InvestmentProvider Provider;
        public bool IsAvailable;
        public Dictionary<string, object> OpportunityDetails = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class InvestmentProvider
    {
        public string ProviderId;
        public string ProviderName;
        public ProviderType Type;
        public float CreditRating;
        public string ContactInformation;
        public List<string> AvailableProducts = new List<string>();
        public float ManagementFeeRange;
        public bool IsAccredited;
        public DateTime LastContact;
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
    public class PerformanceTracking
    {
        public string TrackingId;
        public List<PerformanceMetric> Metrics = new List<PerformanceMetric>();
        public DateTime TrackingPeriodStart;
        public DateTime TrackingPeriodEnd;
        public Dictionary<string, float> Benchmarks = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class PerformanceMetric
    {
        public string MetricName;
        public float Value;
        public DateTime MeasurementDate;
        public string MetricType; // "Return", "Risk", "Efficiency"
        public Dictionary<string, object> AdditionalData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class BenchmarkAnalysis
    {
        public string BenchmarkId;
        public List<BenchmarkMetric> Metrics = new List<BenchmarkMetric>();
        public float OverallPerformance;
        public DateTime AnalysisDate;
    }

    [System.Serializable]
    public class BenchmarkMetric
    {
        public string MetricName;
        public float InvestmentValue;
        public float BenchmarkValue;
        public float Variance;
        public string PerformanceRating; // "Outperforming", "Matching", "Underperforming"
    }

    // Investment-related enumerations
    public enum InvestmentType
    {
        Stocks,
        Bonds,
        RealEstate,
        CommoditiesAndResources,
        Cryptocurrency,
        MutualFunds,
        ETFs,
        PrivateEquity,
        VentureCapital,
        Derivatives,
        Cash,
        Alternatives,
        IndexFunds,
        Equity,
        Bond,
        Other
    }

    public enum InvestmentRisk
    {
        Conservative,
        ModeratelyConservative,
        Moderate,
        ModeratelyAggressive,
        Aggressive,
        Speculative,
        HighRisk,
        VeryHighRisk
    }

    public enum InvestmentStatus
    {
        Active,
        Pending,
        Matured,
        Liquidated,
        Suspended,
        DefaultedLoss,
        UnderReview,
        PartiallyLiquidated
    }

    public enum InvestmentTransactionType
    {
        Buy,
        Sell,
        Dividend,
        Interest,
        Reinvestment,
        Fee,
        Transfer,
        Split,
        Merger,
        Spinoff,
        Redemption,
        Distribution,
        CapitalGains,
        TaxWithholding,
        Other
    }

    public enum ProviderType
    {
        Internal,
        Bank,
        BrokerageFirm,
        InvestmentCompany,
        MutualFundCompany,
        InsuranceCompany,
        RealEstateInvestmentTrust,
        PrivateEquityFirm,
        VentureCapitalFirm,
        HedgeFund,
        CommodityTradingAdvisor,
        FinancialAdvisor,
        Other
    }
}
