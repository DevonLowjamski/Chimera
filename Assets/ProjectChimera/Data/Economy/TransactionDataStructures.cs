using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy.Transactions
{
    /// <summary>
    /// Transaction and payment-focused data structures extracted from economic data files
    /// Contains transaction processing, payment methods, financial transfers, and settlement systems
    /// Part of Phase 1 Foundation Data Structures refactoring
    /// </summary>

    // Type aliases to resolve conversion issues
    using TradingTransactionType = ProjectChimera.Data.Economy.TradingTransactionType;

    #region Core Transaction System

    [System.Serializable]
    public class Transaction
    {
        public string TransactionId;
        public TransactionType Type = TransactionType.Purchase;
        public TransactionStatus Status = TransactionStatus.Pending;
        public decimal Amount = 100m;
        public string Currency = "USD";
        public string FromAccount;
        public string ToAccount;
        public string Description;
        public DateTime CreatedDate;
        public DateTime ProcessedDate;
        public DateTime CompletedDate;
        public string ProcessedBy;
        public PaymentMethod PaymentMethod;
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(TransactionId) &&
                   Amount > 0 &&
                   !string.IsNullOrEmpty(FromAccount) &&
                   !string.IsNullOrEmpty(ToAccount);
        }
    }

    [System.Serializable]
    public class TransactionResult
    {
        public string TransactionId;
        public bool Success = false;
        public string Message;
        public TransactionStatus FinalStatus = TransactionStatus.Failed;
        public decimal ProcessedAmount = 0m;
        public decimal Fees = 0m;
        public DateTime CompletionTime;
        public List<string> ErrorCodes = new List<string>();
        public Dictionary<string, object> ResultData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class PendingTransaction
    {
        public string TransactionId;
        public decimal Amount = 100m;
        public string SenderId;
        public string ReceiverId;
        public TransactionType Type = TransactionType.Transfer;
        public PaymentMethodType PaymentMethod = PaymentMethodType.Cash;
        public DateTime CreatedAt;
        public DateTime ExpiresAt;
        public string Notes;
        public bool RequiresApproval = false;
        public List<string> ApprovalChain = new List<string>();
    }

    [System.Serializable]
    public class CompletedTransaction
    {
        public string TransactionId;
        public decimal Amount = 100m;
        public string SenderId;
        public string ReceiverId;
        public TransactionType Type = TransactionType.Purchase;
        public PaymentMethodType PaymentMethod = PaymentMethodType.Cash;
        public DateTime CompletedAt;
        public decimal TransactionFee = 0m;
        public string ConfirmationCode;
        public TransactionCategory Category = TransactionCategory.General;
        public Dictionary<string, string> Receipt = new Dictionary<string, string>();
    }

    [System.Serializable]
    public class TransactionValidator
    {
        public bool IsEnabled = true;
        public List<ValidationRule> Rules = new List<ValidationRule>();
        public float MinTransactionAmount = 0.01f;
        public float MaxTransactionAmount = 1000000f;
        public List<string> BlockedAccounts = new List<string>();
        public DateTime LastValidationRun;

        public ValidationResult ValidateTransaction(Transaction transaction)
        {
            var result = new ValidationResult { IsValid = true };

            if (transaction.Amount < (decimal)MinTransactionAmount)
            {
                result.IsValid = false;
                result.Errors.Add("Amount below minimum threshold");
            }

            if (transaction.Amount > (decimal)MaxTransactionAmount)
            {
                result.IsValid = false;
                result.Errors.Add("Amount exceeds maximum threshold");
            }

            return result;
        }
    }

    [System.Serializable]
    public class ValidationResult
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        public DateTime ValidationTime;
        public string ValidatorId;
    }

    [System.Serializable]
    public class ValidationRule
    {
        public string RuleId;
        public string RuleName;
        public ValidationRuleType Type = ValidationRuleType.AmountCheck;
        public bool IsActive = true;
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();
        public string ErrorMessage;
    }

    #endregion

    #region Payment Methods and Processing

    [System.Serializable]
    public class PaymentMethod
    {
        public string PaymentMethodId;
        public string DisplayName;
        public PaymentMethodType Type = PaymentMethodType.Cash;
        public bool IsEnabled = true;
        public decimal ProcessingFee = 0m;
        public float ProcessingTimeMinutes = 0f;
        public decimal MinAmount = 0.01m;
        public decimal MaxAmount = 10000m;
        public PaymentProviderInfo ProviderInfo;
        public List<string> SupportedCurrencies = new List<string>();
        public Dictionary<string, string> Configuration = new Dictionary<string, string>();
    }

    [System.Serializable]
    public class PaymentProviderInfo
    {
        public string ProviderId;
        public string ProviderName;
        public ProviderType Type = ProviderType.Internal;
        public string ApiEndpoint;
        public bool RequiresApiKey = false;
        public float UptimePercentage = 99.9f;
        public decimal TransactionFeePercentage = 0.01m;
        public List<string> SupportedCountries = new List<string>();
    }

    [System.Serializable]
    public class PaymentProcessor
    {
        public string ProcessorId;
        public string ProcessorName;
        public List<PaymentMethod> SupportedMethods = new List<PaymentMethod>();
        public bool IsActive = true;
        public ProcessorCapabilities Capabilities;
        public ProcessingStatistics Statistics;
        public DateTime LastHealthCheck;

        public ProcessingResult ProcessPayment(Transaction transaction)
        {
            return new ProcessingResult
            {
                Success = true,
                ProcessingTime = DateTime.Now,
                TransactionId = transaction.TransactionId
            };
        }
    }

    [System.Serializable]
    public class ProcessorCapabilities
    {
        public bool SupportsRefunds = true;
        public bool SupportsPartialRefunds = true;
        public bool SupportsRecurringPayments = false;
        public bool SupportsMultiCurrency = true;
        public bool SupportsBatchProcessing = false;
        public bool SupportsEscrow = false;
        public decimal MaxTransactionAmount = 100000m;
        public int MaxDailyTransactions = 10000;
    }

    [System.Serializable]
    public class ProcessingStatistics
    {
        public int TotalTransactionsProcessed = 0;
        public decimal TotalVolumeProcessed = 0m;
        public float SuccessRate = 100f;
        public float AverageProcessingTime = 2.5f;
        public DateTime LastUpdated;
        public Dictionary<string, int> TransactionsByType = new Dictionary<string, int>();
    }

    [System.Serializable]
    public class ProcessingResult
    {
        public bool Success = false;
        public string TransactionId;
        public string ProcessorTransactionId;
        public DateTime ProcessingTime;
        public decimal ProcessedAmount = 0m;
        public string StatusMessage;
        public Dictionary<string, object> ProcessorResponse = new Dictionary<string, object>();
    }

    #endregion

    #region Financial Accounts and Management

    [System.Serializable]
    public class FinancialAccount
    {
        public string AccountId;
        public string AccountName;
        public AccountType Type = AccountType.Checking;
        public decimal Balance = 0m;
        public decimal AvailableBalance = 0m;
        public decimal ReservedBalance = 0m;
        public string Currency = "USD";
        public string OwnerId;
        public bool IsActive = true;
        public DateTime CreatedDate;
        public DateTime LastActivity;
        public List<string> TransactionHistory = new List<string>();
        public AccountLimits Limits;
    }

    [System.Serializable]
    public class AccountLimits
    {
        public decimal DailyTransactionLimit = 10000m;
        public decimal WeeklyTransactionLimit = 50000m;
        public decimal MonthlyTransactionLimit = 200000m;
        public int MaxTransactionsPerDay = 100;
        public decimal MinBalance = 0m;
        public decimal OverdraftLimit = 0m;
        public bool AllowNegativeBalance = false;
    }

    [System.Serializable]
    public class FinancialTransaction
    {
        public string TransactionId;
        public string AccountId;
        public TransactionType Type = TransactionType.Debit;
        public decimal Amount = 0m;
        public decimal RunningBalance = 0m;
        public string Description;
        public string Reference;
        public DateTime Timestamp;
        public TransactionCategory Category = TransactionCategory.General;
        public bool IsReconciled = false;
        public string CounterpartyAccount;
    }

    [System.Serializable]
    public class PlayerFinances
    {
        public string PlayerId;
        public decimal CashBalance = 1000m;
        public List<FinancialAccount> Accounts = new List<FinancialAccount>();
        public CreditProfile CreditProfile;
        public List<string> RecentTransactions = new List<string>();
        public FinancialMetrics Metrics;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class CreditProfile
    {
        public string ProfileId;
        public int CreditScore = 650;
        public CreditRating Rating = CreditRating.Fair;
        public decimal CreditLimit = 5000m;
        public decimal AvailableCredit = 5000m;
        public decimal OutstandingDebt = 0m;
        public PaymentBehavior PaymentHistory = PaymentBehavior.Reliable;
        public float DebtToIncomeRatio = 0.3f;
        public DateTime LastUpdated;
    }



    #endregion

    #region Cash Flow and Settlement

    [System.Serializable]
    public class CashFlowData
    {
        public string PeriodId;
        public DateTime StartDate;
        public DateTime EndDate;
        public decimal OpeningBalance = 1000m;
        public decimal ClosingBalance = 1100m;
        public decimal TotalInflows = 500m;
        public decimal TotalOutflows = 400m;
        public decimal NetCashFlow = 100m;
        public List<CashFlowEntry> Entries = new List<CashFlowEntry>();
        public CashFlowCategory Category = CashFlowCategory.Operating;
    }

    [System.Serializable]
    public class CashFlowEntry
    {
        public string EntryId;
        public DateTime Date;
        public decimal Amount = 0m;
        public CashFlowType Type = CashFlowType.Inflow;
        public string Description;
        public string Reference;
        public CashFlowCategory Category = CashFlowCategory.Operating;
        public bool IsRecurring = false;
        public string RecurrencePattern;
    }

    [System.Serializable]
    public class CashFlowProjection
    {
        public string ProjectionId;
        public DateTime ProjectionDate;
        public int ProjectionDays = 30;
        public decimal ProjectedInflow = 0m;
        public decimal ProjectedOutflow = 0m;
        public decimal ProjectedBalance = 0m;
        public float ConfidenceLevel = 0.8f;
        public ProjectionMethod Method = ProjectionMethod.Historical;
        public List<CashFlowScenario> Scenarios = new List<CashFlowScenario>();
    }



    [System.Serializable]
    public class SettlementInstruction
    {
        public string InstructionId;
        public string TransactionId;
        public SettlementType Type = SettlementType.Net;
        public decimal Amount = 0m;
        public string FromAccount;
        public string ToAccount;
        public DateTime SettlementDate;
        public SettlementStatus Status = SettlementStatus.Pending;
        public string ClearingHouse;
        public Dictionary<string, string> Instructions = new Dictionary<string, string>();
    }

    [System.Serializable]
    public class SettlementBatch
    {
        public string BatchId;
        public DateTime BatchDate;
        public List<SettlementInstruction> Instructions = new List<SettlementInstruction>();
        public decimal TotalAmount = 0m;
        public int TotalInstructions = 0;
        public BatchStatus Status = BatchStatus.Pending;
        public DateTime CreatedDate;
        public DateTime ProcessedDate;
        public string ProcessedBy;
    }

    #endregion

    #region Transaction Fees and Billing

    [System.Serializable]
    public class TransactionFee
    {
        public string FeeId;
        public string FeeName;
        public FeeType Type = FeeType.Fixed;
        public decimal Amount = 0m;
        public float Percentage = 0f;
        public decimal MinFee = 0m;
        public decimal MaxFee = 1000m;
        public bool IsActive = true;
        public List<FeeCondition> Conditions = new List<FeeCondition>();
        public DateTime EffectiveDate;
        public DateTime ExpirationDate;
    }

    [System.Serializable]
    public class FeeCondition
    {
        public string ConditionId;
        public ConditionType Type = ConditionType.TransactionAmount;
        public string Operator = ">";
        public string Value;
        public bool IsRequired = true;
    }

    [System.Serializable]
    public class BillingPeriod
    {
        public string PeriodId;
        public DateTime StartDate;
        public DateTime EndDate;
        public decimal TotalFees = 0m;
        public int TransactionCount = 0;
        public decimal TotalVolume = 0m;
        public BillingStatus Status = BillingStatus.Open;
        public List<TransactionFee> AppliedFees = new List<TransactionFee>();
        public DateTime GeneratedDate;
    }

    [System.Serializable]
    public class Invoice
    {
        public string InvoiceId;
        public string InvoiceNumber;
        public DateTime InvoiceDate;
        public DateTime DueDate;
        public string CustomerId;
        public decimal SubTotal = 0m;
        public decimal TaxAmount = 0m;
        public decimal TotalAmount = 0m;
        public InvoiceStatus Status = InvoiceStatus.Draft;
        public List<InvoiceLineItem> LineItems = new List<InvoiceLineItem>();
        public PaymentTerms Terms;
        public string Notes;
    }

    [System.Serializable]
    public class InvoiceLineItem
    {
        public string ItemId;
        public string Description;
        public int Quantity = 1;
        public decimal UnitPrice = 0m;
        public decimal LineTotal = 0m;
        public string ProductCode;
        public TaxCategory TaxCategory = TaxCategory.Standard;
    }

    [System.Serializable]
    public class PaymentTerms
    {
        public int PaymentDays = 30;
        public decimal EarlyPaymentDiscount = 0m;
        public int EarlyPaymentDays = 10;
        public decimal LateFeeAmount = 0m;
        public float LateFeePercentage = 0f;
        public int GracePeriodDays = 5;
        public PaymentMethod PreferredPaymentMethod;
    }

    #endregion

    #region Supporting Enums

    public enum TransactionType
    {
        Purchase,
        Sale,
        Transfer,
        Deposit,
        Withdrawal,
        Refund,
        Fee,
        Interest,
        Dividend,
        Tax,
        Debit,
        Credit,
        Investment,
        Loan,
        Repayment
    }

    public enum TransactionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Rejected,
        OnHold,
        RequiresApproval,
        PartiallyCompleted,
        Reversed
    }

    public enum PaymentMethodType
    {
        Cash,
        Credit,
        Debit,
        BankTransfer,
        Cryptocurrency,
        Barter,
        Contract,
        Check,
        MoneyOrder,
        WireTransfer,
        DigitalWallet,
        GiftCard
    }

    public enum TransactionCategory
    {
        General,
        Business,
        Personal,
        Investment,
        Gaming,
        Subscription,
        OneTime,
        Recurring,
        Emergency,
        Maintenance,
        Equipment,
        Supplies,
        Payment,
        LoanPayment
    }

    public enum AccountType
    {
        Checking,
        Savings,
        Investment,
        Credit,
        Loan,
        Escrow,
        Merchant,
        Corporate,
        Trust,
        Joint
    }

    public enum CreditRating
    {
        Excellent,
        VeryGood,
        Good,
        Fair,
        Poor,
        VeryPoor
    }

    public enum PaymentBehavior
    {
        EarlyPayer,
        Reliable,
        OnTime,
        SlowPayer,
        LateOccasionally,
        ChronicallyLate,
        Delinquent,
        Default
    }

    public enum CashFlowType
    {
        Inflow,
        Outflow
    }



    public enum ProjectionMethod
    {
        Historical,
        Trending,
        Budget,
        Forecast,
        MachineLearning
    }



    public enum SettlementType
    {
        Gross,
        Net,
        Real,
        Batch,
        Continuous
    }

    public enum SettlementStatus
    {
        Pending,
        InProgress,
        Settled,
        Failed,
        Cancelled,
        PartiallySettled
    }

    public enum BatchStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        PartiallyCompleted
    }

    public enum FeeType
    {
        Fixed,
        Percentage,
        Tiered,
        Sliding,
        Minimum,
        Maximum,
        Hybrid
    }

    public enum ConditionType
    {
        TransactionAmount,
        TransactionType,
        AccountType,
        PaymentMethod,
        Volume,
        Frequency,
        Geography,
        Time
    }

    public enum BillingStatus
    {
        Open,
        Closed,
        Processing,
        Billed,
        Paid,
        Overdue
    }

    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Viewed,
        PartiallyPaid,
        Paid,
        Overdue,
        Cancelled,
        Refunded
    }

    public enum TaxCategory
    {
        Standard,
        Reduced,
        Zero,
        Exempt,
        Reverse,
        Import,
        Export
    }

    public enum ValidationRuleType
    {
        AmountCheck,
        AccountVerification,
        FraudDetection,
        ComplianceCheck,
        BalanceCheck,
        LimitCheck,
        KYC,
        AML
    }



    public enum CashTransferType
    {
        Income,
        Expense,
        Investment,
        Withdrawal,
        Tax,
        Fee,
        Bonus,
        Penalty,
        Refund,
        Dividend
    }

    #endregion
}
