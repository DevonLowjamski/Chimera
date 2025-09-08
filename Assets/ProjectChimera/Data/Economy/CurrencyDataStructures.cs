using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Economy
{
    [System.Serializable]
    public enum CurrencyType
    {
        Cash,
        Credits,
        ResearchPoints,
        ReputationPoints,
        SkillPoints
    }

    [System.Serializable]
    public enum TransactionType
    {
        Income,
        Expense,
        Transfer,
        Credit,
        Investment,
        Loan,
        Sale,
        Purchase
    }

    [System.Serializable]
    public enum TransactionCategory
    {
        Operations,
        Maintenance,
        Research,
        Marketing,
        Equipment,
        Facilities,
        Salary,
        Utilities,
        Investment,
        Loan,
        LoanPayment,
        Tax,
        Transfer,
        System,
        SkillProgression,
        Other
    }

    [System.Serializable]
    public enum BudgetPeriod
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly
    }

    [System.Serializable]
    public enum ReportPeriod
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly,
        Current,
        Custom
    }

    [System.Serializable]
    public class Transaction
    {
        [Header("Transaction Information")]
        public string TransactionId;
        public DateTime Timestamp;
        public CurrencyType CurrencyType;
        public TransactionType TransactionType;
        public float Amount;
        public float BalanceAfter;
        public string Description;
        public TransactionCategory Category;
        public string Reference;
        public bool IsSuccessful = true;
        public string ErrorMessage;

        // Compatibility properties
        public TransactionType Type => TransactionType;
        public float Income => TransactionType == TransactionType.Income ? Amount : 0f;
        public float Expense => TransactionType == TransactionType.Expense ? Amount : 0f;

        public Transaction()
        {
            TransactionId = System.Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
        }
    }

    [System.Serializable]
    public class FinancialStatistics
    {
        [Header("Financial Overview")]
        public float TotalIncome;
        public float TotalExpenses;
        public float NetProfit;
        public float MonthlyRevenue;
        public float MonthlyExpenses;
        public float CashFlow;
        public int TransactionCount;
        public DateTime LastUpdated;
        public DateTime LastMilestone;
        public float LastMilestoneAmount;

        [Header("Category Breakdowns")]
        public Dictionary<TransactionCategory, float> IncomeByCategory = new Dictionary<TransactionCategory, float>();
        public Dictionary<TransactionCategory, float> ExpensesByCategory = new Dictionary<TransactionCategory, float>();
        public Dictionary<CurrencyType, float> BalancesByCurrency = new Dictionary<CurrencyType, float>();

        // Note: MilestonesReached property not yet implemented
    }

    [System.Serializable]
    public class CashFlowData
    {
        [Header("Cash Flow Analysis")]
        public float TotalInflow;
        public float TotalOutflow;
        public float NetCashFlow;
        public DateTime PeriodStart;
        public DateTime PeriodEnd;
        public BudgetPeriod Period;
        public List<Transaction> Transactions = new List<Transaction>();

        // Missing properties that CurrencyManager expects
        public float ProjectedIncome;
        public float ProjectedExpenses;
        public float ProjectedBalance;
        public float ProjectedNetFlow;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class TransactionValidator
    {
        [Header("Validation Rules")]
        public float MinimumBalance = 0f;
        public float MaxTransactionAmount = 1000000f;
        public bool RequireDescription = true;
        public bool AllowNegativeBalance = false;
        public List<TransactionCategory> RestrictedCategories = new List<TransactionCategory>();

        public bool ValidateTransaction(float amount, TransactionCategory category, string description)
        {
            // Check amount limits
            if (amount > MaxTransactionAmount) return false;

            // Check description requirement
            if (RequireDescription && string.IsNullOrEmpty(description)) return false;

            // Check restricted categories
            if (RestrictedCategories.Contains(category)) return false;

            return true;
        }

        public bool ValidateTransaction(Transaction transaction, float currentBalance)
        {
            // Check amount limits
            if (transaction.Amount > MaxTransactionAmount) return false;

            // Check balance requirements
            if (!AllowNegativeBalance && currentBalance - transaction.Amount < MinimumBalance) return false;

            // Check description requirement
            if (RequireDescription && string.IsNullOrEmpty(transaction.Description)) return false;

            // Check restricted categories
            if (RestrictedCategories.Contains(transaction.Category)) return false;

            return true;
        }

        public bool ValidateTransaction(Transaction transaction, float currentBalance, string additionalContext)
        {
            // Use base validation and add any additional context-specific checks
            return ValidateTransaction(transaction, currentBalance);
        }
    }

    [System.Serializable]
    public class CurrencySettings
    {
        [Header("Currency Configuration")]
        public string Name;
        public string Symbol;
        public Sprite Icon;
        public string IconString; // For string-based icon references
        public Color DisplayColor = Color.white;
        public int DecimalPlaces = 2;
        public bool IsExchangeable = true;
    }

    [System.Serializable]
    public class CurrencyDisplayData
    {
        [Header("Display Information")]
        public CurrencyType CurrencyType;
        public string FormattedAmount;
        public string CurrencySymbol;
        public Color DisplayColor;
        public Sprite CurrencyIcon;
        public float Amount;
        public string Icon;
        public Color Color;
        public bool IsPositive;

        // Additional properties for compatibility
        public float ChangeAmount;
        public float ChangePercentage;
    }

    [System.Serializable]
    public class FinancialReport
    {
        [Header("Report Information")]
        public string ReportId;
        public DateTime GeneratedDate;
        public ReportPeriod Period;
        public DateTime PeriodStart;
        public DateTime PeriodEnd;

        // Compatibility property
        public DateTime ReportDate => GeneratedDate;

        [Header("Financial Summary")]
        public float TotalRevenue;
        public float TotalExpenses;
        public float NetProfit;
        public float NetIncome;
        public float TotalAssets;
        public float TotalLiabilities;
        public float NetWorth;
        public float CashBalance;
        public float TotalIncome;
        public float BurnRate;
        public float RunwayDays;
        public float ProfitMargin;

        [Header("Detailed Breakdowns")]
        public Dictionary<string, float> IncomeByCategory = new Dictionary<string, float>();
        public DateTime GeneratedAt;
        public BudgetPeriod ReportPeriod;
        public Dictionary<string, float> CategoryBreakdown = new Dictionary<string, float>();
        public Dictionary<string, float> CashFlowProjection = new Dictionary<string, float>();
        public Dictionary<string, float> ExpensesByCategory = new Dictionary<string, float>();
        public FinancialStatistics Statistics;
        public Dictionary<string, BudgetStatusReport> BudgetMonitoring = new Dictionary<string, BudgetStatusReport>();
        public CashFlowData CashFlowPrediction;
    }

    [System.Serializable]
    public class BudgetStatusReport
    {
        [Header("Budget Information")]
        public string CategoryName;
        public float Spent;
        public float Limit;
        public float Remaining;
        public float Percentage;
        public bool IsOverBudget;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class Budget
    {
        [Header("Budget Information")]
        public string BudgetId;
        public string CategoryName;
        public float Limit;
        public float MonthlyLimit;
        public float CurrentSpent;
        public BudgetPeriod Period;
        public DateTime StartDate;
        public DateTime EndDate;
        public bool IsActive = true;
        public bool HasExceededAlert;

        public float Remaining => Limit - CurrentSpent;
        public float PercentageUsed => Limit > 0 ? (CurrentSpent / Limit) * 100f : 0f;
        public bool IsOverBudget => CurrentSpent > Limit;
    }

    [System.Serializable]
    public class Loan
    {
        [Header("Loan Information")]
        public string LoanId;
        public float PrincipalAmount;
        public float InterestRate;
        public int TermDays;
        public DateTime StartDate;
        public DateTime LastPaymentDate;
        public float RemainingBalance;
        public string Purpose;
        public bool IsActive = true;

        public DateTime EndDate => StartDate.AddDays(TermDays);
        public bool IsOverdue => DateTime.Now > EndDate && RemainingBalance > 0;
        public float TotalInterest => PrincipalAmount * InterestRate * (TermDays / 365f);
    }

    [System.Serializable]
    public enum BudgetMonitoringStatus
    {
        OnTrack,
        Warning,
        OverBudget,
        Inactive
    }

    /// <summary>
    /// Sale transaction data for progression tracking
    /// </summary>
    [System.Serializable]
    public class SaleTransaction
    {
        [Header("Sale Information")]
        public string TransactionId;
        public DateTime SaleDate;
        public string ProductId;
        public string ProductName;
        public float Quantity;
        public float UnitPrice;
        public float TotalAmount;
        public string CustomerId;
        public string CustomerName;

        [Header("Sale Details")]
        public float QualityRating;
        public string SaleChannel;
        public string PaymentMethod;
        public bool IsSuccessful = true;
        public string Notes;

        [Header("Business Metrics")]
        public float ProfitMargin;
        public float CostOfGoods;
        public float NetProfit;
        public TransactionCategory Category = TransactionCategory.Operations;

        public SaleTransaction()
        {
            TransactionId = System.Guid.NewGuid().ToString();
            SaleDate = DateTime.UtcNow;
        }
    }
}
