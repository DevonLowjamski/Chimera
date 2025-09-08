using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Economy;
using DataTransactionType = ProjectChimera.Data.Economy.TransactionType;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Core.Events;
using SimpleGameEventSO = ProjectChimera.Data.Events.SimpleGameEventSO;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Currency Manager - orchestrates all currency components
    /// Maintains original interface while using modular components
    /// Refactored from monolithic 1,022-line class into focused components
    /// </summary>
    public class CurrencyManager : DIChimeraManager, ITickable
    {
        [Header("Currency Configuration")]
        [SerializeField] private float _startingCash = 25000f;
        [SerializeField] private bool _enableMultipleCurrencies = true;
        [SerializeField] private bool _enableCreditSystem = true;
        [SerializeField] private bool _enableTaxation = false;
        [SerializeField] private float _creditLimit = 50000f;
        
        [Header("Transaction Settings")]
        [SerializeField] private bool _enableTransactionHistory = true;
        [SerializeField] private int _maxTransactionHistory = 1000;
        [SerializeField] private bool _enableTransactionValidation = true;
        [SerializeField] private bool _enableFraudDetection = true;
        
        [Header("Financial Analytics")]
        [SerializeField] private bool _enableFinancialReports = true;
        [SerializeField] private bool _enableBudgetTracking = true;
        [SerializeField] private bool _enableCashFlowPrediction = true;
        [SerializeField] private float _reportGenerationInterval = 3600f; // 1 hour
        
        [Header("Event Channels")]
        [SerializeField] private SimpleGameEventSO _onCurrencyChanged;
        [SerializeField] private SimpleGameEventSO _onTransactionCompleted;
        [SerializeField] private SimpleGameEventSO _onInsufficientFunds;
        [SerializeField] private SimpleGameEventSO _onCreditLimitReached;
        [SerializeField] private SimpleGameEventSO _onFinancialMilestone;
        [SerializeField] private SimpleGameEventSO _onBudgetAlert;

        // Currency components
        private ICurrencyCore _currencyCore;
        private ITransactions _transactions;
        private IEconomyBalance _economyBalance;
        private IExchangeRates _exchangeRates;

        // Tracking for change detection
        private Dictionary<CurrencyType, float> _lastKnownBalances = new Dictionary<CurrencyType, float>();

        public override ManagerPriority Priority => ManagerPriority.High;
        
        // Public Properties
        public float Cash => _currencyCore?.Cash ?? 0f;
        public float SkillPoints => _currencyCore?.SkillPoints ?? 0f;
        public float TotalNetWorth => _currencyCore?.TotalNetWorth ?? 0f;
        public float AvailableCredit => _exchangeRates?.AvailableCredit ?? 0f;
        public bool HasSufficientFunds(float amount) => _currencyCore?.HasSufficientFunds(amount) ?? false;
        public bool HasSufficientSkillPoints(float amount) => _currencyCore?.HasSufficientSkillPoints(amount) ?? false;
        public List<Transaction> RecentTransactions => _transactions?.RecentTransactions ?? new List<Transaction>();
        public FinancialStatistics Statistics => _economyBalance?.Statistics ?? new FinancialStatistics();
        public Dictionary<CurrencyType, float> AllCurrencies => _currencyCore?.AllCurrencies ?? new Dictionary<CurrencyType, float>();
        
        // Events - forwarded from components
        public System.Action<CurrencyType, float, float> OnCurrencyChanged; // type, oldAmount, newAmount
        public System.Action<Transaction> OnTransactionCompleted;
        public System.Action<float, string> OnInsufficientFunds; // amount needed, reason
        public System.Action<float> OnFinancialMilestone; // milestone amount
        public System.Action<string, float, float> OnBudgetAlert; // category, spent, budget

        #region Manager Lifecycle

        protected override void OnManagerInitialize()
        {
            InitializeComponents();
            ConfigureComponentIntegrations();
            InitializeAllComponents();
            SetupEventForwarding();
            
            // Register with UpdateOrchestrator for centralized ticking
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.RegisterTickable(this);
            
            LogInfo($"CurrencyManager initialized with ${_startingCash:F2} starting cash");
        }

        protected override void OnManagerShutdown()
        {
            try
            {
                _currencyCore?.Shutdown();
                _transactions?.Shutdown();
                _economyBalance?.Shutdown();
                _exchangeRates?.Shutdown();
            }
            catch (Exception ex)
            {
                LogError($"Error during Currency Manager shutdown: {ex.Message}");
            }

            LogInfo("CurrencyManager shutdown complete");
        }

        #endregion

        #region ITickable Implementation
        
        int ITickable.Priority => TickPriority.EconomyManager;
        bool ITickable.Enabled => IsInitialized;
        
        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;
            
            _economyBalance?.Tick(deltaTime);
            _exchangeRates?.Tick(deltaTime);
            
            // Detect currency changes for events
            _economyBalance?.DetectCurrencyChanges(AllCurrencies, _lastKnownBalances);
        }
        
        public void OnRegistered()
        {
            // Called when registered with UpdateOrchestrator
        }
        
        public void OnUnregistered()
        {
            // Called when unregistered from UpdateOrchestrator
        }

        #endregion

        #region Currency Operations

        public bool AddCurrency(CurrencyType currencyType, float amount, string reason = "", TransactionCategory category = TransactionCategory.Other)
        {
            return _currencyCore?.AddCurrency(currencyType, amount, reason, category) ?? false;
        }

        public bool SpendCurrency(CurrencyType currencyType, float amount, string reason = "", TransactionCategory category = TransactionCategory.Other, bool allowCredit = false)
        {
            return _currencyCore?.SpendCurrency(currencyType, amount, reason, category, allowCredit) ?? false;
        }

        public bool TransferCurrency(CurrencyType fromType, CurrencyType toType, float amount, string reason = "")
        {
            return _transactions?.TransferCurrency(fromType, toType, amount, reason) ?? false;
        }

        public float GetCurrencyAmount(CurrencyType currencyType)
        {
            return _currencyCore?.GetCurrencyAmount(currencyType) ?? 0f;
        }

        public float GetBalance()
        {
            return _currencyCore?.GetBalance() ?? 0f;
        }

        public float GetBalance(CurrencyType currencyType)
        {
            return _currencyCore?.GetBalance(currencyType) ?? 0f;
        }

        public void SetCurrencyAmount(CurrencyType currencyType, float amount, string reason = "System Set")
        {
            _currencyCore?.SetCurrencyAmount(currencyType, amount, reason);
        }

        #endregion

        #region Budget Management

        public void CreateBudget(string categoryName, float monthlyLimit, BudgetPeriod period = BudgetPeriod.Monthly)
        {
            _economyBalance?.CreateBudget(categoryName, monthlyLimit, period);
        }

        #endregion

        #region Credit and Loans

        public bool TakeLoan(float amount, float interestRate, int termDays, string purpose = "")
        {
            return _exchangeRates?.TakeLoan(amount, interestRate, termDays, purpose) ?? false;
        }

        public bool MakeInvestment(string investmentType, float amount, float expectedReturn, int maturityDays)
        {
            return _exchangeRates?.MakeInvestment(investmentType, amount, expectedReturn, maturityDays) ?? false;
        }

        #endregion

        #region Skill Points

        public bool AddSkillPoints(float amount, string reason = "")
        {
            return _currencyCore?.AddSkillPoints(amount, reason) ?? false;
        }

        public bool SpendSkillPoints(float amount, string reason = "")
        {
            return _currencyCore?.SpendSkillPoints(amount, reason) ?? false;
        }

        public float GetSkillPointsBalance()
        {
            return _currencyCore?.GetSkillPointsBalance() ?? 0f;
        }

        public void SetSkillPoints(float amount, string reason = "System Set")
        {
            _currencyCore?.SetSkillPoints(amount, reason);
        }

        public bool AwardSkillPoints(float amount, string achievementReason)
        {
            return _currencyCore?.AwardSkillPoints(amount, achievementReason) ?? false;
        }

        public bool PurchaseWithSkillPoints(float cost, string skillName, string description = "")
        {
            return _transactions?.PurchaseWithSkillPoints(cost, skillName, description) ?? false;
        }

        #endregion

        #region Testing Support

        public void SetCurrencyForTesting(CurrencyType currencyType, float amount)
        {
            _currencyCore?.SetCurrencyAmount(currencyType, amount, "Testing");
        }

        #endregion

        #region Component Management

        private void InitializeComponents()
        {
            // Create currency components
            _currencyCore = new CurrencyCore();
            _transactions = new Transactions(_currencyCore);
            _economyBalance = new EconomyBalance();
            _exchangeRates = new ExchangeRates(_transactions);
        }

        private void ConfigureComponentIntegrations()
        {
            // Set up cross-component dependencies
            (_currencyCore as CurrencyCore)?.SetTransactionHandler(_transactions);
            (_currencyCore as CurrencyCore)?.SetExchangeRatesHandler(_exchangeRates);
            (_transactions as Transactions)?.SetCurrencyCore(_currencyCore);
            (_exchangeRates as ExchangeRates)?.SetTransactionHandler(_transactions);
        }

        private void InitializeAllComponents()
        {
            // Initialize all components with their specific configurations
            _currencyCore.Initialize(_startingCash);
            _transactions.Initialize(_maxTransactionHistory, _enableTransactionValidation, _enableFraudDetection);
            _economyBalance.Initialize(_enableBudgetTracking, _enableFinancialReports, _enableCashFlowPrediction);
            _exchangeRates.Initialize(_enableCreditSystem, _creditLimit);

            // Initialize last known balances for change detection
            _lastKnownBalances = new Dictionary<CurrencyType, float>(AllCurrencies);

            LogInfo("All currency components initialized");
        }

        private void SetupEventForwarding()
        {
            // Forward events from components to this manager's events and ScriptableObject events
            if (_transactions != null)
            {
                _transactions.OnTransactionCompleted = (transaction) => {
                    OnTransactionCompleted?.Invoke(transaction);
                    _onTransactionCompleted?.Invoke();
                    _economyBalance?.UpdateBudgetTracking(transaction);
                };

                _transactions.OnInsufficientFunds = (amount, reason) => {
                    OnInsufficientFunds?.Invoke(amount, reason);
                    _onInsufficientFunds?.Invoke();
                };
            }

            if (_economyBalance != null)
            {
                _economyBalance.OnCurrencyChanged = (type, oldAmount, newAmount) => {
                    OnCurrencyChanged?.Invoke(type, oldAmount, newAmount);
                    _onCurrencyChanged?.Invoke();
                };

                _economyBalance.OnFinancialMilestone = (milestone) => {
                    OnFinancialMilestone?.Invoke(milestone);
                    _onFinancialMilestone?.Invoke();
                };

                _economyBalance.OnBudgetAlert = (category, spent, budget) => {
                    OnBudgetAlert?.Invoke(category, spent, budget);
                    _onBudgetAlert?.Invoke();
                };
            }

            if (_exchangeRates != null)
            {
                _exchangeRates.OnCreditLimitReached = () => {
                    _onCreditLimitReached?.Invoke();
                };
            }
        }

        #endregion

        #region Logging Helpers

        private void LogInfo(string message)
        {
            ChimeraLogger.Log($"[CurrencyManager] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[CurrencyManager] {message}");
        }

        #endregion
    }
}
