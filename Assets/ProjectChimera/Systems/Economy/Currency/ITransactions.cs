using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Interface for transaction processing, history, and validation
    /// </summary>
    public interface ITransactions
    {
        List<Transaction> RecentTransactions { get; }
        List<Transaction> TransactionHistory { get; }
        
        bool TransferCurrency(CurrencyType fromType, CurrencyType toType, float amount, string reason = "");
        bool PurchaseWithSkillPoints(float cost, string skillName, string description = "");
        
        void RecordTransaction(Transaction transaction);
        void UpdateCategoryStatistics(TransactionCategory category, float amount, bool isIncome);
        
        bool ValidateTransaction(CurrencyType currencyType, float amount);
        
        // Events for transaction completion
        Action<Transaction> OnTransactionCompleted { get; set; }
        Action<float, string> OnInsufficientFunds { get; set; }
        
        void Initialize(int maxTransactionHistory, bool enableValidation, bool enableFraudDetection);
        void Shutdown();
    }
}
