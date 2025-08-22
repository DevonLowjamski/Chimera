using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Trading;
using ProjectChimera.Core;
using TradingTransactionType = ProjectChimera.Data.Economy.TradingTransactionType;
using FinancialRecord = ProjectChimera.Data.Economy.FinancialRecord;
using PaymentMethodType = ProjectChimera.Data.Economy.PaymentMethodType;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Manages player finances, payment processing, and financial tracking.
    /// Extracted from TradingManager for modular architecture.
    /// Handles cash flow, payment methods, and financial record keeping.
    /// </summary>
    public class TradingFinancialManager : MonoBehaviour
    {
        [Header("Financial Configuration")]
        [SerializeField] private bool _enableFinancialLogging = true;
        [SerializeField] private float _financialUpdateInterval = 5f; // Update metrics every 5 seconds
        [SerializeField] private int _maxFinancialHistory = 1000;
        
        // Financial data
        private PlayerFinances _playerFinances;
        private float _lastFinancialUpdate = 0f;
        
        // Events
        public System.Action<float, float> OnCashChanged; // oldAmount, newAmount
        public System.Action<float, float> OnBankBalanceChanged; // oldAmount, newAmount
        public System.Action<float, float> OnDebtChanged; // oldAmount, newAmount
        public System.Action<FinancialRecord> OnTransactionRecorded;
        public System.Action<PlayerFinances> OnFinancialMetricsUpdated;
        
        // Properties
        public PlayerFinances PlayerFinances => _playerFinances;
        public float CashOnHand => _playerFinances?.CashOnHand ?? 0f;
        public float BankBalance => _playerFinances?.BankBalance ?? 0f;
        public float TotalDebt => _playerFinances?.TotalDebt ?? 0f;
        public float CreditLimit => _playerFinances?.CreditLimit ?? 0f;
        public float AvailableCredit => CreditLimit - TotalDebt;
        public float NetWorth => CashOnHand + BankBalance - TotalDebt;
        public float MonthlyProfit => _playerFinances?.MonthlyProfit ?? 0f;
        
        /// <summary>
        /// Initialize financial manager with starting configuration
        /// </summary>
        public void Initialize(TradingSettings tradingSettings)
        {
            if (_playerFinances == null)
            {
                _playerFinances = new PlayerFinances
                {
                    CashOnHand = tradingSettings?.StartingCash ?? 10000f,
                    BankBalance = 0f,
                    TotalDebt = 0f,
                    CreditLimit = tradingSettings?.StartingCreditLimit ?? 5000f,
                    MonthlyProfit = 0f,
                    AccountsReceivable = 0f,
                    AccountsPayable = 0f,
                    MonthlyCosts = 0f,
                    TransactionHistory = new List<FinancialRecord>()
                };
            }
            
            LogDebug($"Trading financial manager initialized - Starting Cash: ${CashOnHand:F2}, Credit Limit: ${CreditLimit:F2}");
        }
        
        private void Update()
        {
            _lastFinancialUpdate += Time.deltaTime;
            
            if (_lastFinancialUpdate >= _financialUpdateInterval)
            {
                UpdateFinancialMetrics();
                _lastFinancialUpdate = 0f;
            }
        }
        
        #region Payment Processing
        
        /// <summary>
        /// Check if player can afford a transaction
        /// </summary>
        public bool CanAffordTransaction(float cost, PaymentMethod paymentMethod)
        {
            if (cost <= 0) return true;
            
            switch (paymentMethod.PaymentType)
            {
                case PaymentMethodType.Cash:
                    return CashOnHand >= cost;
                case PaymentMethodType.Bank_Transfer:
                    return BankBalance >= cost;
                case PaymentMethodType.Credit:
                    return (TotalDebt + cost) <= CreditLimit;
                case PaymentMethodType.Cryptocurrency:
                    // For now, treat as cash equivalent
                    return CashOnHand >= cost;
                default:
                    LogError($"Unsupported payment type: {paymentMethod.PaymentType}");
                    return false;
            }
        }
        
        /// <summary>
        /// Process payment for a transaction
        /// </summary>
        public bool ProcessPayment(float amount, PaymentMethod paymentMethod)
        {
            if (amount <= 0)
            {
                LogError("Cannot process payment for zero or negative amount");
                return false;
            }
            
            switch (paymentMethod.PaymentType)
            {
                case PaymentMethodType.Cash:
                    return ProcessCashPayment(amount);
                case PaymentMethodType.Bank_Transfer:
                    return ProcessBankPayment(amount);
                case PaymentMethodType.Credit:
                    return ProcessCreditPayment(amount);
                case PaymentMethodType.Cryptocurrency:
                    return ProcessCryptocurrencyPayment(amount);
                default:
                    LogError($"Unsupported payment type: {paymentMethod.PaymentType}");
                    return false;
            }
        }
        
        /// <summary>
        /// Process cash payment
        /// </summary>
        private bool ProcessCashPayment(float amount)
        {
            if (CashOnHand >= amount)
            {
                float oldCash = _playerFinances.CashOnHand;
                _playerFinances.CashOnHand -= amount;
                OnCashChanged?.Invoke(oldCash, _playerFinances.CashOnHand);
                
                RecordTransaction(new FinancialRecord
                {
                    TransactionType = FinancialTransactionType.Purchase,
                    Amount = -amount,
                    Description = $"Cash payment of ${amount:F2}",
                    Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                    Category = "Trading"
                });
                
                LogDebug($"Cash payment processed: ${amount:F2} (Remaining: ${CashOnHand:F2})");
                return true;
            }
            
            LogError($"Insufficient cash: Need ${amount:F2}, have ${CashOnHand:F2}");
            return false;
        }
        
        /// <summary>
        /// Process bank transfer payment
        /// </summary>
        private bool ProcessBankPayment(float amount)
        {
            if (BankBalance >= amount)
            {
                float oldBalance = _playerFinances.BankBalance;
                _playerFinances.BankBalance -= amount;
                OnBankBalanceChanged?.Invoke(oldBalance, _playerFinances.BankBalance);
                
                RecordTransaction(new FinancialRecord
                {
                    TransactionType = FinancialTransactionType.Transfer,
                    Amount = -amount,
                    Description = $"Bank transfer of ${amount:F2}",
                    Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                    Category = "Trading"
                });
                
                LogDebug($"Bank payment processed: ${amount:F2} (Remaining: ${BankBalance:F2})");
                return true;
            }
            
            LogError($"Insufficient bank balance: Need ${amount:F2}, have ${BankBalance:F2}");
            return false;
        }
        
        /// <summary>
        /// Process credit payment
        /// </summary>
        private bool ProcessCreditPayment(float amount)
        {
            if ((TotalDebt + amount) <= CreditLimit)
            {
                float oldDebt = _playerFinances.TotalDebt;
                _playerFinances.TotalDebt += amount;
                OnDebtChanged?.Invoke(oldDebt, _playerFinances.TotalDebt);
                
                RecordTransaction(new FinancialRecord
                {
                    TransactionType = FinancialTransactionType.Purchase,
                    Amount = -amount,
                    Description = $"Credit payment of ${amount:F2}",
                    Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                    Category = "Credit"
                });
                
                LogDebug($"Credit payment processed: ${amount:F2} (Available Credit: ${AvailableCredit:F2})");
                return true;
            }
            
            LogError($"Credit limit exceeded: Need ${amount:F2}, available credit: ${AvailableCredit:F2}");
            return false;
        }
        
        /// <summary>
        /// Process cryptocurrency payment
        /// </summary>
        private bool ProcessCryptocurrencyPayment(float amount)
        {
            // For now, treat cryptocurrency as cash equivalent
            // In a full implementation, this would handle crypto-specific logic
            return ProcessCashPayment(amount);
        }
        
        /// <summary>
        /// Refund a payment (used when transaction fails)
        /// </summary>
        public bool RefundPayment(float amount, PaymentMethod paymentMethod)
        {
            switch (paymentMethod.PaymentType)
            {
                case PaymentMethodType.Cash:
                case PaymentMethodType.Cryptocurrency:
                    return RefundToCash(amount);
                case PaymentMethodType.Bank_Transfer:
                    return RefundToBank(amount);
                case PaymentMethodType.Credit:
                    return RefundCredit(amount);
                default:
                    LogError($"Cannot refund unsupported payment type: {paymentMethod.PaymentType}");
                    return false;
            }
        }
        
        /// <summary>
        /// Refund to cash
        /// </summary>
        private bool RefundToCash(float amount)
        {
            float oldCash = _playerFinances.CashOnHand;
            _playerFinances.CashOnHand += amount;
            OnCashChanged?.Invoke(oldCash, _playerFinances.CashOnHand);
            
            RecordTransaction(new FinancialRecord
            {
                TransactionType = FinancialTransactionType.Sale, // Refunds are treated as incoming money
                Amount = amount,
                Description = $"Refund of ${amount:F2}",
                Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                Category = "Refund"
            });
            
            LogDebug($"Cash refund processed: ${amount:F2}");
            return true;
        }
        
        /// <summary>
        /// Refund to bank account
        /// </summary>
        private bool RefundToBank(float amount)
        {
            float oldBalance = _playerFinances.BankBalance;
            _playerFinances.BankBalance += amount;
            OnBankBalanceChanged?.Invoke(oldBalance, _playerFinances.BankBalance);
            
            RecordTransaction(new FinancialRecord
            {
                TransactionType = FinancialTransactionType.Transfer,
                Amount = amount,
                Description = $"Bank refund of ${amount:F2}",
                Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                Category = "Refund"
            });
            
            LogDebug($"Bank refund processed: ${amount:F2}");
            return true;
        }
        
        /// <summary>
        /// Refund credit (reduce debt)
        /// </summary>
        private bool RefundCredit(float amount)
        {
            float oldDebt = _playerFinances.TotalDebt;
            _playerFinances.TotalDebt = Mathf.Max(0f, _playerFinances.TotalDebt - amount);
            OnDebtChanged?.Invoke(oldDebt, _playerFinances.TotalDebt);
            
            RecordTransaction(new FinancialRecord
            {
                TransactionType = FinancialTransactionType.Loan_Payment,
                Amount = -amount,
                Description = $"Credit refund of ${amount:F2}",
                Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                Category = "Refund"
            });
            
            LogDebug($"Credit refund processed: ${amount:F2}");
            return true;
        }
        
        #endregion
        
        #region Payment Receipt
        
        /// <summary>
        /// Receive payment for a sale
        /// </summary>
        public void ReceivePayment(float amount, PaymentMethod paymentMethod)
        {
            if (amount <= 0)
            {
                LogError("Cannot receive zero or negative payment");
                return;
            }
            
            // For now, all received payments go to cash
            // In a full implementation, this might vary based on payment method
            float oldCash = _playerFinances.CashOnHand;
            _playerFinances.CashOnHand += amount;
            OnCashChanged?.Invoke(oldCash, _playerFinances.CashOnHand);
            
            RecordTransaction(new FinancialRecord
            {
                TransactionType = FinancialTransactionType.Sale,
                Amount = amount,
                Description = $"Payment received: ${amount:F2}",
                Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                Category = "Trading"
            });
            
            LogDebug($"Payment received: ${amount:F2} via {paymentMethod.PaymentType}");
        }
        
        #endregion
        
        #region Cash Transfers
        
        /// <summary>
        /// Transfer cash between accounts
        /// </summary>
        public bool TransferCash(float amount, CashTransferType transferType)
        {
            if (amount <= 0)
            {
                LogError("Cannot transfer zero or negative amount");
                return false;
            }
            
            switch (transferType)
            {
                case CashTransferType.Cash_To_Bank:
                    return TransferCashToBank(amount);
                case CashTransferType.Bank_To_Cash:
                    return TransferBankToCash(amount);
                default:
                    LogError($"Unsupported transfer type: {transferType}");
                    return false;
            }
        }
        
        /// <summary>
        /// Transfer cash to bank
        /// </summary>
        private bool TransferCashToBank(float amount)
        {
            if (_playerFinances.CashOnHand >= amount)
            {
                float oldCash = _playerFinances.CashOnHand;
                float oldBank = _playerFinances.BankBalance;
                
                _playerFinances.CashOnHand -= amount;
                _playerFinances.BankBalance += amount;
                
                OnCashChanged?.Invoke(oldCash, _playerFinances.CashOnHand);
                OnBankBalanceChanged?.Invoke(oldBank, _playerFinances.BankBalance);
                
                RecordTransaction(new FinancialRecord
                {
                    TransactionType = FinancialTransactionType.Transfer,
                    Amount = -amount,
                    Description = $"Cash to bank transfer: ${amount:F2}",
                    Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                    Category = "Transfer"
                });
                
                LogDebug($"Transferred ${amount:F2} from cash to bank");
                return true;
            }
            
            LogError($"Insufficient cash for transfer: Need ${amount:F2}, have ${CashOnHand:F2}");
            return false;
        }
        
        /// <summary>
        /// Transfer bank to cash
        /// </summary>
        private bool TransferBankToCash(float amount)
        {
            if (_playerFinances.BankBalance >= amount)
            {
                float oldCash = _playerFinances.CashOnHand;
                float oldBank = _playerFinances.BankBalance;
                
                _playerFinances.BankBalance -= amount;
                _playerFinances.CashOnHand += amount;
                
                OnCashChanged?.Invoke(oldCash, _playerFinances.CashOnHand);
                OnBankBalanceChanged?.Invoke(oldBank, _playerFinances.BankBalance);
                
                RecordTransaction(new FinancialRecord
                {
                    TransactionType = FinancialTransactionType.Transfer,
                    Amount = amount,
                    Description = $"Bank to cash transfer: ${amount:F2}",
                    Timestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds,
                    Category = "Transfer"
                });
                
                LogDebug($"Transferred ${amount:F2} from bank to cash");
                return true;
            }
            
            LogError($"Insufficient bank balance for transfer: Need ${amount:F2}, have ${BankBalance:F2}");
            return false;
        }
        
        #endregion
        
        #region Financial Metrics
        
        /// <summary>
        /// Update financial metrics and calculations
        /// </summary>
        private void UpdateFinancialMetrics()
        {
            if (_playerFinances?.TransactionHistory == null) return;
            
            // Calculate monthly profit from recent transactions
            var currentTimestamp = (float)(System.DateTime.Now - new System.DateTime(1970, 1, 1)).TotalSeconds;
            var thirtyDaysInSeconds = 30 * 24 * 60 * 60; // 30 days in seconds
            var recentTransactions = _playerFinances.TransactionHistory
                .Where(t => (currentTimestamp - t.Timestamp) <= thirtyDaysInSeconds)
                .ToList();
            
            float monthlyRevenue = recentTransactions
                .Where(t => t.TransactionType == FinancialTransactionType.Sale && t.Amount > 0)
                .Sum(t => t.Amount);
            
            float monthlyExpenses = recentTransactions
                .Where(t => (t.TransactionType == FinancialTransactionType.Purchase || 
                            t.TransactionType == FinancialTransactionType.Fee_Paid) && t.Amount < 0)
                .Sum(t => Mathf.Abs(t.Amount));
            
            _playerFinances.MonthlyProfit = monthlyRevenue - monthlyExpenses;
            _playerFinances.MonthlyCosts = monthlyExpenses;
            
            OnFinancialMetricsUpdated?.Invoke(_playerFinances);
        }
        
        /// <summary>
        /// Get financial summary for a specific time period
        /// </summary>
        public FinancialSummary GetFinancialSummary(int days = 30)
        {
            var cutoffDate = (float)(System.DateTime.Now.AddDays(-days) - new System.DateTime(1970, 1, 1)).TotalSeconds;
            var relevantTransactions = _playerFinances.TransactionHistory
                .Where(t => t.Timestamp >= cutoffDate)
                .ToList();
            
            return new FinancialSummary
            {
                PeriodDays = days,
                TotalRevenue = relevantTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalExpenses = relevantTransactions.Where(t => t.Amount < 0).Sum(t => Mathf.Abs(t.Amount)),
                NetProfit = relevantTransactions.Sum(t => t.Amount),
                TransactionCount = relevantTransactions.Count(),
                AverageTransactionSize = relevantTransactions.Count() > 0 ? relevantTransactions.Average(t => Mathf.Abs(t.Amount)) : 0f,
                CurrentNetWorth = NetWorth,
                CashFlow = relevantTransactions.Sum(t => t.Amount)
            };
        }
        
        #endregion
        
        #region Transaction Recording
        
        /// <summary>
        /// Record a financial transaction
        /// </summary>
        private void RecordTransaction(FinancialRecord record)
        {
            _playerFinances.TransactionHistory.Add(record);
            OnTransactionRecorded?.Invoke(record);
            
            // Keep history manageable
            if (_playerFinances.TransactionHistory.Count > _maxFinancialHistory)
            {
                var cutoffDate = (float)(System.DateTime.Now.AddDays(-365) - new System.DateTime(1970, 1, 1)).TotalSeconds; // Keep 1 year of history
                _playerFinances.TransactionHistory.RemoveAll(r => r.Timestamp < cutoffDate);
            }
        }
        
        /// <summary>
        /// Get recent transaction history
        /// </summary>
        public List<FinancialRecord> GetRecentTransactions(int count = 20)
        {
            return _playerFinances.TransactionHistory
                .OrderByDescending(t => t.Timestamp)
                .Take(count)
                .ToList();
        }
        
        /// <summary>
        /// Clear transaction history
        /// </summary>
        public void ClearTransactionHistory(bool keepCurrent = true)
        {
            if (keepCurrent)
            {
                var cutoffDate = (float)(System.DateTime.Now.AddDays(-30) - new System.DateTime(1970, 1, 1)).TotalSeconds; // Keep last 30 days
                _playerFinances.TransactionHistory.RemoveAll(r => r.Timestamp < cutoffDate);
            }
            else
            {
                _playerFinances.TransactionHistory.Clear();
            }
            
            LogDebug("Financial transaction history cleared");
        }
        
        #endregion
        
        private void LogDebug(string message)
        {
            if (_enableFinancialLogging)
                Debug.Log($"[TradingFinancialManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[TradingFinancialManager] {message}");
        }
    }
    
    [System.Serializable]
    public class FinancialSummary
    {
        public int PeriodDays;
        public float TotalRevenue;
        public float TotalExpenses;
        public float NetProfit;
        public int TransactionCount;
        public float AverageTransactionSize;
        public float CurrentNetWorth;
        public float CashFlow;
        public float ProfitMargin => TotalRevenue > 0 ? (NetProfit / TotalRevenue) * 100f : 0f;
    }
}