using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Data structures for credit management and assessment
    /// </summary>

    [System.Serializable]
    public class CreditProfile
    {
        public string ProfileId;
        public int CreditScore;
        public string CreditRating;
        public List<CreditAccount> CreditAccounts = new List<CreditAccount>();
        public PaymentHistory PaymentHistory;
        public List<CreditInquiry> CreditInquiries = new List<CreditInquiry>();
        public float TotalAvailableCredit;
        public float TotalUsedCredit;
        public float CreditUtilizationRatio;
        public DateTime LastUpdated;
        public int AccountsInGoodStanding;
        public int TotalAccounts;
        public float AverageAccountAge;
        public DateTime OldestAccountDate;
        public DateTime NewestAccountDate;
    }

    [System.Serializable]
    public class CreditAccount
    {
        public string AccountId;
        public string AccountNumber;
        public string CreditorName;
        public string AccountType; // Credit Card, Loan, Mortgage, etc.
        public float CreditLimit;
        public float CurrentBalance;
        public float AvailableCredit;
        public float UsedCredit;
        public float PaymentDue;
        public float MinimumPayment;
        public DateTime LastPaymentDate;
        public DateTime AccountOpenDate;
        public PaymentStatus PaymentStatus;
        public float InterestRate;
        public bool IsActive;
        public string AccountStatusNotes;
        public List<LoanPayment> PaymentHistory = new List<LoanPayment>();

        public int CreditScore;
        // Note: PaymentDue DateTime property not yet implemented
    }

    [System.Serializable]
    public class PaymentHistory
    {
        public string HistoryId;
        public int TotalPayments;
        public int OnTimePayments;
        public int LatePayments;
        public int MissedPayments;
        public float OnTimePercentage;
        public DateTime LastLatePayment;
        public int ConsecutiveOnTimePayments;
        public List<LoanPayment> PaymentRecords = new List<LoanPayment>();
    }

    [System.Serializable]
    public class CreditInquiry
    {
        public string InquiryId;
        public DateTime InquiryDate;
        public string CreditorName;
        public string InquiryType; // "Hard", "Soft"
        public string InquiryReason;
        public bool ImpactsCreditScore;
    }

    [System.Serializable]
    public class BusinessPlan
    {
        public string PlanId;
        public string BusinessName;
        public BusinessType Type;
        public string Industry;
        public DateTime EstablishedDate;
        public string Description;
        public float RequestedFundingAmount;
        public string FundingPurpose;
        public FinancialProjections Projections;
        public CompetitiveAnalysis CompetitiveAnalysis;
        public List<BusinessRisk> IdentifiedRisks = new List<BusinessRisk>();
        public Dictionary<string, object> BusinessMetrics = new Dictionary<string, object>();
        public List<string> KeyPersonnel = new List<string>();
        public string MarketAnalysisReport;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class CompetitiveAnalysis
    {
        public string AnalysisId;
        public List<string> MainCompetitors = new List<string>();
        public Dictionary<string, float> MarketShare = new Dictionary<string, float>();
        public List<string> CompetitiveAdvantages = new List<string>();
        public List<string> CompetitiveDisadvantages = new List<string>();
        public float MarketSize;
        public string MarketGrowthRate;
        public DateTime AnalysisDate;
    }

    [System.Serializable]
    public class BusinessRisk
    {
        public string RiskId;
        public string RiskDescription;
        public RiskCategory Category;
        public RiskLevel Level;
        public float Probability; // 0.0 to 1.0
        public float Impact; // 0.0 to 1.0
        public List<string> MitigationStrategies = new List<string>();
        public DateTime IdentifiedDate;
        public bool IsActive;
    }

    // Credit-related enumerations
    public enum CreditRating
    {
        Excellent, // 750+
        VeryGood,  // 700-749
        Good,      // 650-699
        Fair,      // 600-649
        Poor,      // 550-599
        VeryPoor   // <550
    }

    public enum BusinessType
    {
        SoleProprietorship,
        Partnership,
        LimitedLiabilityCompany,
        Corporation,
        SCorporation,
        NonProfit,
        Cooperative,
        LimitedPartnership,
        ProfessionalCorporation,
        Other
    }

    public enum RiskCategory
    {
        Financial,
        Operational,
        Strategic,
        Compliance,
        Reputational,
        Market,
        Technology,
        Environmental,
        Political,
        Other
    }

    public enum RiskLevel
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh,
        Critical
    }
}
