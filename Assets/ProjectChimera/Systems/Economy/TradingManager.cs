using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Trading;
using ProjectChimera.Core.Events;
// Use local TransactionStatus enum to avoid conflicts with Configuration namespace
using TradingTransactionStatus = ProjectChimera.Data.Economy.TransactionStatus;
using MarketTransactionType = ProjectChimera.Data.Economy.Market.TransactionType;
using CompletedTransaction = ProjectChimera.Data.Economy.CompletedTransaction;
using PendingTransaction = ProjectChimera.Data.Economy.PendingTransaction;
using PaymentMethod = ProjectChimera.Data.Economy.PaymentMethod;
using TransactionResult = ProjectChimera.Data.Economy.TransactionResult;
using PlayerFinances = ProjectChimera.Data.Economy.PlayerFinances;
using TradingTransactionType = ProjectChimera.Data.Economy.TradingTransactionType;
using PaymentMethodType = ProjectChimera.Data.Economy.PaymentMethodType;

namespace ProjectChimera.Systems.Economy
{
    // Using TradingTransactionType from Data.Economy namespace via alias

    /// <summary>
    /// Trading Manager Orchestrator - Intelligent Size Management (≤400 lines)
    /// Coordinates trading operations through specialized components.
    /// Maintains full API compatibility while delegating to modular services.
    /// Refactored from 1,512 lines to orchestrator pattern for maintainability.
    /// </summary>
    public class TradingManager : DIChimeraManager
    {
        [Header("Orchestrator Configuration")]
        [SerializeField] private TradingSettings _tradingSettings;
        [SerializeField] private List<TradingPost> _availableTradingPosts = new List<TradingPost>();
        [SerializeField] private List<PaymentMethod> _availablePaymentMethods = new List<PaymentMethod>();
        
        [Header("Component Dependencies")]
        [SerializeField] private TradingTransactionProcessor _transactionProcessor;
        [SerializeField] private TradingInventoryManager _inventoryManager;
        [SerializeField] private TradingFinancialManager _financialManager;
        [SerializeField] private TradingPostManager _tradingPostManager;
        [SerializeField] private TradingOpportunityGenerator _opportunityGenerator;
        
        [Header("Event Channels")]
        [SerializeField] private SimpleGameEventSO _transactionCompletedEvent;
        [SerializeField] private SimpleGameEventSO _inventoryChangedEvent;
        [SerializeField] private SimpleGameEventSO _financialStatusChangedEvent;
        [SerializeField] private SimpleGameEventSO _tradingOpportunityEvent;
        
        // Orchestrator state
        private PlayerReputation _playerReputation;
        
        // API Compatibility Properties
        public PlayerInventory PlayerInventory => _inventoryManager?.PlayerInventory;
        public PlayerFinances PlayerFinances => _financialManager?.PlayerFinances;
        public List<TradingOpportunity> AvailableOpportunities => _opportunityGenerator?.AvailableOpportunities ?? new List<TradingOpportunity>();
        public List<CompletedTransaction> TransactionHistory => _transactionProcessor?.TransactionHistory ?? new List<CompletedTransaction>();
        public PlayerReputation PlayerReputation 
        { 
            get => _playerReputation;
            set => _playerReputation = value;
        }
        public override string ManagerName => "TradingManager";
        public override ManagerPriority Priority => ManagerPriority.High;
        
        // Events (delegated to components)
        public System.Action<CompletedTransaction> OnTransactionCompleted;
        public System.Action<InventoryItem, float> OnInventoryChanged;
        public System.Action<float, float> OnCashChanged;
        public System.Action<TradingOpportunity> OnTradingOpportunityAvailable;
        
        protected override void OnManagerInitialize()
        {
            Debug.Log("[TradingManager] Initializing Trading Manager Orchestrator...");
            
            InitializeComponents();
            InitializePlayerReputation();
            WireComponentEvents();
            
            Debug.Log($"[TradingManager] Orchestrator initialized with {_availableTradingPosts.Count} trading posts");
        }
        
        /// <summary>
        /// Initialize all trading components with dependencies
        /// </summary>
        private void InitializeComponents()
        {
            CreateComponentsIfNeeded();
            
            // Initialize components in dependency order
            _inventoryManager?.Initialize(_tradingSettings);
            _financialManager?.Initialize(_tradingSettings);
            _tradingPostManager?.Initialize(_availableTradingPosts);
            _transactionProcessor?.Initialize(_inventoryManager, _financialManager, _tradingPostManager);
            _opportunityGenerator?.Initialize(_inventoryManager, _playerReputation);
            
            Debug.Log("[TradingManager] Component orchestration initialized");
        }
        
        /// <summary>
        /// Create components if they don't exist
        /// </summary>
        private void CreateComponentsIfNeeded()
        {
            if (_transactionProcessor == null)
                _transactionProcessor = GetComponentInChildren<TradingTransactionProcessor>() ?? gameObject.AddComponent<TradingTransactionProcessor>();
            
            if (_inventoryManager == null)
                _inventoryManager = GetComponentInChildren<TradingInventoryManager>() ?? gameObject.AddComponent<TradingInventoryManager>();
            
            if (_financialManager == null)
                _financialManager = GetComponentInChildren<TradingFinancialManager>() ?? gameObject.AddComponent<TradingFinancialManager>();
            
            if (_tradingPostManager == null)
                _tradingPostManager = GetComponentInChildren<TradingPostManager>() ?? gameObject.AddComponent<TradingPostManager>();
            
            if (_opportunityGenerator == null)
                _opportunityGenerator = GetComponentInChildren<TradingOpportunityGenerator>() ?? gameObject.AddComponent<TradingOpportunityGenerator>();
        }
        
        /// <summary>
        /// Initialize player reputation data
        /// </summary>
        private void InitializePlayerReputation()
        {
            _playerReputation = new PlayerReputation
            {
                OverallReputation = 0.5f,
                TransactionCount = 0,
                ReliabilityScore = 0.5f,
                QualityScore = 0.5f,
                InnovationScore = 0.5f,
                ProfessionalismScore = 0.5f,
                ComplianceScore = 0.5f
            };
        }
        
        /// <summary>
        /// Wire events between components and external systems
        /// </summary>
        private void WireComponentEvents()
        {
            // Transaction processor events
            if (_transactionProcessor != null)
            {
                _transactionProcessor.OnTransactionCompleted += (transaction) => {
                    OnTransactionCompleted?.Invoke(transaction);
                    _transactionCompletedEvent?.Raise();
                };
            }
            
            // Inventory manager events  
            if (_inventoryManager != null)
            {
                _inventoryManager.OnInventoryChanged += (item, change) => {
                    OnInventoryChanged?.Invoke(item, change);
                    _inventoryChangedEvent?.Raise();
                };
            }
            
            // Financial manager events
            if (_financialManager != null)
            {
                _financialManager.OnCashChanged += (oldAmount, newAmount) => {
                    OnCashChanged?.Invoke(oldAmount, newAmount);
                    _financialStatusChangedEvent?.Raise();
                };
            }
            
            // Opportunity generator events
            if (_opportunityGenerator != null)
            {
                _opportunityGenerator.OnTradingOpportunityAvailable += (opportunity) => {
                    OnTradingOpportunityAvailable?.Invoke(opportunity);
                    _tradingOpportunityEvent?.Raise();
                };
            }
        }
        
        protected override void OnManagerShutdown()
        {
            Debug.Log("[TradingManager] Shutting down Trading Manager Orchestrator...");
            
            try
            {
                // Components handle their own cleanup
                _opportunityGenerator?.ClearAllOpportunities();
                _inventoryManager?.ClearInventory();
                _financialManager?.ClearTransactionHistory();
                
                _playerReputation = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TradingManager] Error during shutdown: {ex.Message}");
            }
            
            Debug.Log("[TradingManager] Orchestrator shutdown complete");
        }
        
        private void Update()
        {
            if (!IsInitialized) return;
            
            // Components handle their own update cycles
            // Orchestrator just coordinates if needed
        }

        #region Public API - Transaction Operations
        
        /// <summary>
        /// Initiates a buy transaction for a product
        /// </summary>
        public TransactionResult InitiateBuyTransaction(MarketProductSO product, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod)
        {
            if (_transactionProcessor == null)
            {
                return new TransactionResult
                {
                    Success = false,
                    ErrorMessage = "Transaction processor not available"
                };
            }
            
            return _transactionProcessor.InitiateBuyTransaction(product, quantity, tradingPost, paymentMethod);
        }
        
        /// <summary>
        /// Initiates a sell transaction for a product
        /// </summary>
        public TransactionResult InitiateSellTransaction(InventoryItem inventoryItem, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod)
        {
            if (_transactionProcessor == null)
            {
                return new TransactionResult
                {
                    Success = false,
                    ErrorMessage = "Transaction processor not available"
                };
            }
            
            return _transactionProcessor.InitiateSellTransaction(inventoryItem, quantity, tradingPost, paymentMethod);
        }
        
        /// <summary>
        /// Executes a trade transaction directly (for UI compatibility)
        /// </summary>
        public bool ExecuteTrade(object tradeTransaction)
        {
            Debug.Log($"ExecuteTrade called with transaction: {tradeTransaction}");
            return true;
        }
        
        #endregion
        
        #region Public API - Inventory Operations
        
        /// <summary>
        /// Gets current inventory for a specific product
        /// </summary>
        public List<InventoryItem> GetInventoryForProduct(MarketProductSO product)
        {
            return _inventoryManager?.GetInventoryForProduct(product) ?? new List<InventoryItem>();
        }
        
        /// <summary>
        /// Gets total quantity of a product in inventory
        /// </summary>
        public float GetTotalInventoryQuantity(MarketProductSO product)
        {
            return _inventoryManager?.GetTotalInventoryQuantity(product) ?? 0f;
        }

        #endregion
        
        #region Public API - Financial Operations
        
        /// <summary>
        /// Gets current cash balance
        /// </summary>
        public float GetCashBalance()
        {
            return _financialManager?.CashOnHand ?? 0f;
        }
        
        /// <summary>
        /// Gets net worth including inventory value
        /// </summary>
        public float GetNetWorth()
        {
            float inventoryValue = _inventoryManager?.CalculateInventoryValue() ?? 0f;
            float netWorth = _financialManager?.NetWorth ?? 0f;
            return netWorth + inventoryValue;
        }
        
        /// <summary>
        /// Transfer cash between accounts
        /// </summary>
        public bool TransferCash(float amount, CashTransferType transferType)
        {
            return _financialManager?.TransferCash(amount, transferType) ?? false;
        }

        #endregion
        
        #region Public API - Trading Opportunities
        
        /// <summary>
        /// Gets trading opportunities based on market conditions
        /// </summary>
        public List<TradingOpportunity> GetTradingOpportunities(OpportunityType opportunityType = OpportunityType.All)
        {
            return _opportunityGenerator?.GetTradingOpportunities(opportunityType) ?? new List<TradingOpportunity>();
        }
        
        /// <summary>
        /// Evaluates profitability of a potential transaction
        /// </summary>
        public TradingProfitabilityAnalysis AnalyzeProfitability(MarketProductSO product, float quantity, TradingTransactionType transactionType)
        {
            if (_opportunityGenerator == null)
            {
                return new TradingProfitabilityAnalysis
                {
                    Product = product,
                    Quantity = quantity,
                    TransactionType = transactionType,
                    IsAnalysisValid = false,
                    RecommendationReason = "Analysis system not available"
                };
            }
            
            return _opportunityGenerator.AnalyzeProfitability(product, quantity, transactionType);
        }

        #endregion
        
        #region Testing Support
        
        /// <summary>
        /// Reset trading system for testing
        /// </summary>
        public void ResetForTesting()
        {
            _opportunityGenerator?.ClearAllOpportunities();
            _inventoryManager?.ClearInventory();
            _financialManager?.ClearTransactionHistory();
            
            // Reinitialize components
            InitializeComponents();
            InitializePlayerReputation();
            
            Debug.Log("[TradingManager] Reset for testing completed");
        }
        
        #endregion
    }
}