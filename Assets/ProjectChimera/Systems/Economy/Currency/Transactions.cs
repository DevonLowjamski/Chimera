using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Implementation for transaction processing, history, and validation
    /// </summary>
    public class Transactions : ITransactions
    {
        private List<Transaction> _transactionHistory = new List<Transaction>();
        private TransactionValidator _validator;
        private int _maxTransactionHistory = 1000;
        private bool _enableTransactionValidation = true;
        private bool _enableFraudDetection = true;
        private ICurrencyCore _currencyCore;

        public List<Transaction> RecentTransactions => _transactionHistory.TakeLast(50).ToList();
        public List<Transaction> TransactionHistory => new List<Transaction>(_transactionHistory);

        public Action<Transaction> OnTransactionCompleted { get; set; }
        public Action<float, string> OnInsufficientFunds { get; set; }

        public Transactions(ICurrencyCore currencyCore = null)
        {
            _currencyCore = currencyCore;
        }

        public void Initialize(int maxTransactionHistory, bool enableValidation, bool enableFraudDetection)
        {
            _maxTransactionHistory = maxTransactionHistory;
            _enableTransactionValidation = enableValidation;
            _enableFraudDetection = enableFraudDetection;
            _validator = new TransactionValidator();
            
            ChimeraLogger.Log("[Transactions] Transaction system initialized");
        }

        public void Shutdown()
        {
            _transactionHistory.Clear();
            _validator = null;
            ChimeraLogger.Log("[Transactions] Transaction system shutdown");
        }

        public bool TransferCurrency(CurrencyType fromType, CurrencyType toType, float amount, string reason = "")
        {
            if (_currencyCore == null)
            {
                ChimeraLogger.LogError("[Transactions] CurrencyCore not available for transfer");
                return false;
            }

            if (fromType == toType)
            {
                ChimeraLogger.LogWarning("[Transactions] Cannot transfer to same currency type");
                return false;
            }

            if (!ValidateTransaction(fromType, amount))
            {
                return false;
            }

            // Check if we have enough of the source currency
            if (_currencyCore.GetCurrencyAmount(fromType) < amount)
            {
                ChimeraLogger.LogWarning($"[Transactions] Insufficient {fromType} for transfer: {_currencyCore.GetCurrencyAmount(fromType):F2} < {amount:F2}");
                OnInsufficientFunds?.Invoke(amount - _currencyCore.GetCurrencyAmount(fromType), $"Transfer from {fromType} to {toType}");
                return false;
            }

            // Perform the transfer
            bool spendSuccess = _currencyCore.SpendCurrency(fromType, amount, $"Transfer to {toType}: {reason}", TransactionCategory.Transfer);
            if (!spendSuccess)
            {
                return false;
            }

            bool addSuccess = _currencyCore.AddCurrency(toType, amount, $"Transfer from {fromType}: {reason}", TransactionCategory.Transfer);
            if (!addSuccess)
            {
                // Rollback the spend if add failed
                _currencyCore.AddCurrency(fromType, amount, $"Rollback failed transfer: {reason}", TransactionCategory.System);
                return false;
            }

            ChimeraLogger.Log($"[Transactions] Transferred {amount:F2} from {fromType} to {toType}: {reason}");
            return true;
        }

        public bool PurchaseWithSkillPoints(float cost, string skillName, string description = "")
        {
            if (_currencyCore == null)
            {
                ChimeraLogger.LogError("[Transactions] CurrencyCore not available for skill point purchase");
                return false;
            }

            if (cost <= 0)
            {
                ChimeraLogger.LogWarning("[Transactions] Invalid skill point cost");
                return false;
            }

            if (!ValidateTransaction(CurrencyType.SkillPoints, cost))
            {
                return false;
            }

            // Check if we have enough skill points
            if (!_currencyCore.HasSufficientSkillPoints(cost))
            {
                float currentSP = _currencyCore.GetSkillPointsBalance();
                ChimeraLogger.LogWarning($"[Transactions] Insufficient Skill Points for {skillName}: {currentSP:F0} < {cost:F0}");
                OnInsufficientFunds?.Invoke(cost - currentSP, $"Purchase {skillName}");
                return false;
            }

            // Perform the purchase
            string purchaseReason = string.IsNullOrEmpty(description) ? $"Purchased {skillName}" : $"Purchased {skillName}: {description}";
            bool success = _currencyCore.SpendSkillPoints(cost, purchaseReason);

            if (success)
            {
                ChimeraLogger.Log($"[Transactions] Purchased {skillName} for {cost:F0} Skill Points");
            }

            return success;
        }

        public void RecordTransaction(Transaction transaction)
        {
            if (transaction == null)
            {
                ChimeraLogger.LogWarning("[Transactions] Attempted to record null transaction");
                return;
            }

            // Add to history
            _transactionHistory.Add(transaction);

            // Maintain history size limit
            if (_transactionHistory.Count > _maxTransactionHistory)
            {
                _transactionHistory.RemoveAt(0);
            }

            // Trigger events
            OnTransactionCompleted?.Invoke(transaction);

            if (ChimeraLogger.EnableDebugLogging)
            {
                ChimeraLogger.Log($"[Transactions] Recorded {transaction.TransactionType}: {transaction.Amount:F2} {transaction.CurrencyType} - {transaction.Description}");
            }
        }

        public void UpdateCategoryStatistics(TransactionCategory category, float amount, bool isIncome)
        {
            // This could be expanded to maintain detailed category statistics
            // For now, we'll just log the category activity
            if (ChimeraLogger.EnableDebugLogging)
            {
                string direction = isIncome ? "Income" : "Expense";
                ChimeraLogger.Log($"[Transactions] Category {category} {direction}: {amount:F2}");
            }
        }

        public bool ValidateTransaction(CurrencyType currencyType, float amount)
        {
            if (!_enableTransactionValidation)
            {
                return true;
            }

            if (_validator == null)
            {
                ChimeraLogger.LogWarning("[Transactions] Transaction validator not initialized");
                return true; // Allow transaction to proceed if validator is not available
            }

            // Basic validation
            if (amount <= 0)
            {
                ChimeraLogger.LogWarning($"[Transactions] Invalid transaction amount: {amount}");
                return false;
            }

            // Fraud detection
            if (_enableFraudDetection)
            {
                // Simple fraud detection: check for unusually large transactions
                float suspiciousThreshold = GetSuspiciousTransactionThreshold(currencyType);
                if (amount > suspiciousThreshold)
                {
                    ChimeraLogger.LogWarning($"[Transactions] Suspicious transaction detected: {amount:F2} {currencyType} exceeds threshold {suspiciousThreshold:F2}");
                    
                    // For now, we'll log but allow the transaction
                    // In a more sophisticated system, this might require additional confirmation
                }
            }

            return true;
        }

        public void SetCurrencyCore(ICurrencyCore currencyCore)
        {
            _currencyCore = currencyCore;
        }

        private float GetSuspiciousTransactionThreshold(CurrencyType currencyType)
        {
            return currencyType switch
            {
                CurrencyType.Cash => 100000f, // $100,000
                CurrencyType.SkillPoints => 1000f, // 1,000 SP
                CurrencyType.Credits => 50000f, // 50,000 Credits
                CurrencyType.ResearchPoints => 5000f, // 5,000 RP
                CurrencyType.ReputationPoints => 1000f, // 1,000 REP
                _ => 10000f // Default threshold
            };
        }
    }
}
