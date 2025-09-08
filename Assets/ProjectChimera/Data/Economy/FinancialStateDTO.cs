using System.Collections.Generic;
using System;

namespace ProjectChimera.Data.Economy
{
    [System.Serializable]
    public class LoanDTO
    {
        public string LoanId { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime MaturityDate { get; set; }
        public decimal OutstandingBalance { get; set; }
        public List<LoanPaymentDTO> PaymentHistory { get; set; } = new List<LoanPaymentDTO>();
        public string Status { get; set; } // Active, Closed, Defaulted
    }

    [System.Serializable]
    public class LoanPaymentDTO
    {
        public string PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal PrincipalPortion { get; set; }
        public decimal InterestPortion { get; set; }
        public decimal RemainingBalance { get; set; }
    }

    [System.Serializable]
    public class TradingPostDTO
    {
        public string TradingPostId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
        public List<PendingTransactionDTO> PendingTransactions { get; set; } = new List<PendingTransactionDTO>();
        public decimal TotalVolume { get; set; }
        public DateTime LastActivityDate { get; set; }
    }

    [System.Serializable]
    public class CreditProfileDTO
    {
        public int CreditScore { get; set; }
        public string CreditRating { get; set; }
        public decimal TotalDebt { get; set; }
        public decimal DebtToIncomeRatio { get; set; }
        public List<string> CreditHistory { get; set; } = new List<string>();
        public DateTime LastUpdated { get; set; }
    }

    [System.Serializable]
    public class PendingTransactionDTO
    {
        public string TransactionId { get; set; }
        public string Type { get; set; } // Buy, Sell, Trade
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; } // Pending, Completed, Cancelled
        public string CounterParty { get; set; }
    }

    [System.Serializable]
    public class TradingOpportunityDTO
    {
        public string OpportunityId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal PotentialProfit { get; set; }
        public decimal RequiredInvestment { get; set; }
        public string RiskLevel { get; set; } // Low, Medium, High
        public DateTime AvailableUntil { get; set; }
        public bool IsActive { get; set; }
    }

    [System.Serializable]
    public class TaxInformationDTO
    {
        public string TaxYear { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal TotalTaxLiability { get; set; }
        public decimal TaxesPaid { get; set; }
        public decimal RefundOwed { get; set; }
        public List<string> TaxDocuments { get; set; } = new List<string>();
        public DateTime LastFilingDate { get; set; }
    }

    [System.Serializable]
    public class FinancialPlanDTO
    {
        public string PlanId { get; set; }
        public string PlanName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime TargetDate { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentSavings { get; set; }
        public List<FinancialGoalDTO> Goals { get; set; } = new List<FinancialGoalDTO>();
        public string Status { get; set; } // Active, Paused, Completed
    }

    [System.Serializable]
    public class FinancialGoalDTO
    {
        public string GoalId { get; set; }
        public string Description { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime TargetDate { get; set; }
        public int Priority { get; set; } // 1 = High, 5 = Low
        public string Category { get; set; }
        public bool IsCompleted { get; set; }
    }

    [System.Serializable]
    public class FinancialStrategyDTO
    {
        public string StrategyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal AllocationPercentage { get; set; }
        public string RiskTolerance { get; set; } // Conservative, Moderate, Aggressive
        public DateTime ImplementationDate { get; set; }
        public bool IsActive { get; set; }
        public List<string> AssociatedAccounts { get; set; } = new List<string>();
    }

    [System.Serializable]
    public class FinancialStateDTO
    {
        public List<LoanDTO> ActiveLoans { get; set; } = new List<LoanDTO>();
        public TradingPostDTO TradingPost { get; set; } = new TradingPostDTO();
        public CreditProfileDTO CreditProfile { get; set; } = new CreditProfileDTO();
        public List<PendingTransactionDTO> PendingTransactions { get; set; } = new List<PendingTransactionDTO>();
        public List<TradingOpportunityDTO> AvailableOpportunities { get; set; } = new List<TradingOpportunityDTO>();
        public TaxInformationDTO TaxInformation { get; set; } = new TaxInformationDTO();
        public List<FinancialPlanDTO> FinancialPlans { get; set; } = new List<FinancialPlanDTO>();
        public List<FinancialStrategyDTO> InvestmentStrategies { get; set; } = new List<FinancialStrategyDTO>();
        public decimal TotalNetWorth { get; set; }
        public decimal LiquidAssets { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
