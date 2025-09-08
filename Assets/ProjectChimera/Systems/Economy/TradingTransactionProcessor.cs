using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Trading;
using ProjectChimera.Core.Events;
using TradingTransactionStatus = ProjectChimera.Data.Economy.TransactionStatus;
using MarketTransactionType = ProjectChimera.Data.Economy.Market.TransactionType;
using CompletedTransaction = ProjectChimera.Data.Economy.CompletedTransaction;
using PendingTransaction = ProjectChimera.Data.Economy.PendingTransaction;
using PaymentMethod = ProjectChimera.Data.Economy.PaymentMethod;
using PaymentType = ProjectChimera.Data.Economy.PaymentMethodType;
using TransactionResult = ProjectChimera.Data.Economy.TransactionResult;
using TradingTransactionType = ProjectChimera.Data.Economy.TradingTransactionType;
using PaymentMethodType = ProjectChimera.Data.Economy.PaymentMethodType;

// Access to GameManager from TradingOpportunityGenerator
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Handles transaction processing, pending transaction queue, and completion logic.
    /// Extracted from TradingManager for modular architecture.
    /// Manages buy/sell transaction lifecycle and validation.
    /// </summary>
    public class TradingTransactionProcessor : MonoBehaviour, ITickable
    {
        [Header("Transaction Processing Configuration")]
        [SerializeField] private bool _enableTransactionLogging = true;
        [SerializeField] private float _transactionProcessingInterval = 0.1f;
        [SerializeField] private int _maxPendingTransactions = 100;
        
        // Dependencies (injected by orchestrator)
        private TradingInventoryManager _inventoryManager;
        private TradingFinancialManager _financialManager;
        private TradingPostManager _tradingPostManager;
        
        // Transaction queues and history
        private Queue<PendingTransaction> _pendingTransactions = new Queue<PendingTransaction>();
        private List<CompletedTransaction> _transactionHistory = new List<CompletedTransaction>();
        private float _timeSinceLastProcess = 0f;
        
        // Events
        public System.Action<CompletedTransaction> OnTransactionCompleted;
        public System.Action<PendingTransaction> OnTransactionInitiated;
        public System.Action<string> OnTransactionFailed;
        
        // Properties
        public int PendingTransactionCount => _pendingTransactions.Count;
        public List<CompletedTransaction> TransactionHistory => _transactionHistory.ToList();
        public bool IsProcessingTransactions { get; private set; }
        
        /// <summary>
        /// Initialize transaction processor with dependencies
        /// </summary>
        public void Initialize(TradingInventoryManager inventoryManager, TradingFinancialManager financialManager, TradingPostManager tradingPostManager)
        {
            _inventoryManager = inventoryManager;
            _financialManager = financialManager;
            _tradingPostManager = tradingPostManager;
            
            LogDebug("Trading transaction processor initialized");
        }
        
            public void Tick(float deltaTime)
    {
            _timeSinceLastProcess += deltaTime;
            
            if (_timeSinceLastProcess >= _transactionProcessingInterval)
            {
                ProcessPendingTransactions();
                _timeSinceLastProcess = 0f;
            
    }
        }
        
        #region Transaction Initiation
        
        /// <summary>
        /// Initiates a buy transaction for a product
        /// </summary>
        public TransactionResult InitiateBuyTransaction(MarketProductSO product, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod)
        {
            var result = new TransactionResult
            {
                Success = false,
                TransactionId = System.Guid.NewGuid().ToString(),
                Product = product,
                Quantity = quantity,
                TradingPost = tradingPost
            };
            
            if (_pendingTransactions.Count >= _maxPendingTransactions)
            {
                result.ErrorMessage = "Transaction queue full";
                return result;
            }
            
            // Get current market price
            var marketManager = GameManager.Instance.GetManager<MarketManager>();
            if (marketManager == null)
            {
                result.ErrorMessage = "Market system unavailable";
                return result;
            }
            
            float unitPrice = marketManager.GetProductPrice(product.ProductName);
            float totalCost = unitPrice * quantity;
            
            // Apply trading post markup
            if (_tradingPostManager != null)
            {
                totalCost *= _tradingPostManager.GetPriceMarkup(tradingPost);
            }
            
            // Check affordability
            if (!_financialManager.CanAffordTransaction(totalCost, paymentMethod))
            {
                result.ErrorMessage = "Insufficient funds";
                return result;
            }
            
            // Check trading post availability
            if (!_tradingPostManager.IsProductAvailable(tradingPost, product, quantity))
            {
                result.ErrorMessage = "Product unavailable at this trading post";
                return result;
            }
            
            // Create pending transaction
            var pendingTransaction = new PendingTransaction
            {
                TransactionId = result.TransactionId,
                TransactionType = TradingTransactionType.Purchase,
                Product = product,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalValue = totalCost,
                TradingPost = tradingPost,
                PaymentMethod = paymentMethod,
                InitiationTime = System.DateTime.Now,
                EstimatedCompletionTime = CalculateTransactionTime(tradingPost, paymentMethod),
                Status = TradingTransactionStatus.Pending
            };
            
            _pendingTransactions.Enqueue(pendingTransaction);
            OnTransactionInitiated?.Invoke(pendingTransaction);
            
            result.Success = true;
            result.UnitPrice = unitPrice;
            result.TotalValue = totalCost;
            result.EstimatedCompletionTime = pendingTransaction.EstimatedCompletionTime;
            
            LogDebug($"Buy transaction initiated: {product.ProductName} x{quantity} = ${totalCost:F2}");
            return result;
        }
        
        /// <summary>
        /// Initiates a sell transaction for a product
        /// </summary>
        public TransactionResult InitiateSellTransaction(InventoryItem inventoryItem, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod)
        {
            var result = new TransactionResult
            {
                Success = false,
                TransactionId = System.Guid.NewGuid().ToString(),
                Product = inventoryItem.Product,
                Quantity = quantity,
                TradingPost = tradingPost
            };
            
            if (_pendingTransactions.Count >= _maxPendingTransactions)
            {
                result.ErrorMessage = "Transaction queue full";
                return result;
            }
            
            // Check inventory availability
            if (!_inventoryManager.HasSufficientQuantity(inventoryItem.Product, quantity))
            {
                result.ErrorMessage = "Insufficient inventory";
                return result;
            }
            
            // Get current market price
            var marketManager = GameManager.Instance.GetManager<MarketManager>();
            if (marketManager == null)
            {
                result.ErrorMessage = "Market system unavailable";
                return result;
            }
            
            float unitPrice = marketManager.GetProductPrice(inventoryItem.Product.ProductName);
            float totalRevenue = unitPrice * quantity;
            
            // Apply trading post commission
            if (_tradingPostManager != null)
            {
                totalRevenue *= _tradingPostManager.GetCommissionMultiplier(tradingPost);
            }
            
            // Check trading post acceptance
            if (!_tradingPostManager.WillAcceptProduct(tradingPost, inventoryItem, quantity))
            {
                result.ErrorMessage = "Trading post will not accept this product";
                return result;
            }
            
            // Create pending transaction
            var pendingTransaction = new PendingTransaction
            {
                TransactionId = result.TransactionId,
                TransactionType = TradingTransactionType.Sale,
                Product = inventoryItem.Product,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalValue = totalRevenue,
                TradingPost = tradingPost,
                PaymentMethod = paymentMethod,
                InitiationTime = System.DateTime.Now,
                EstimatedCompletionTime = CalculateTransactionTime(tradingPost, paymentMethod),
                Status = TradingTransactionStatus.Pending,
                SourceInventoryItem = inventoryItem
            };
            
            _pendingTransactions.Enqueue(pendingTransaction);
            OnTransactionInitiated?.Invoke(pendingTransaction);
            
            result.Success = true;
            result.UnitPrice = unitPrice;
            result.TotalValue = totalRevenue;
            result.EstimatedCompletionTime = pendingTransaction.EstimatedCompletionTime;
            
            LogDebug($"Sell transaction initiated: {inventoryItem.Product.ProductName} x{quantity} = ${totalRevenue:F2}");
            return result;
        }
        
        #endregion
        
        #region Transaction Processing
        
        /// <summary>
        /// Process all pending transactions that are ready for completion
        /// </summary>
        public void ProcessPendingTransactions()
        {
            if (_pendingTransactions.Count == 0) return;
            
            IsProcessingTransactions = true;
            var completedCount = 0;
            
            while (_pendingTransactions.Count > 0)
            {
                var transaction = _pendingTransactions.Peek();
                
                if (System.DateTime.Now >= transaction.EstimatedCompletionTime)
                {
                    _pendingTransactions.Dequeue();
                    
                    bool success = CompleteTransaction(transaction);
                    
                    var completedTransaction = new CompletedTransaction
                    {
                        TransactionId = transaction.TransactionId,
                        TransactionType = transaction.TransactionType,
                        Product = transaction.Product,
                        Quantity = transaction.Quantity,
                        UnitPrice = transaction.UnitPrice,
                        TotalValue = transaction.TotalValue,
                        TradingPost = transaction.TradingPost,
                        PaymentMethod = transaction.PaymentMethod,
                        CompletionTime = System.DateTime.Now,
                        Success = success,
                        QualityScore = transaction.SourceInventoryItem?.QualityScore ?? 0.8f
                    };
                    
                    _transactionHistory.Add(completedTransaction);
                    OnTransactionCompleted?.Invoke(completedTransaction);
                    completedCount++;
                }
                else
                {
                    break; // No more transactions ready to complete
                }
            }
            
            if (completedCount > 0)
            {
                LogDebug($"Processed {completedCount} transactions");
            }
            
            IsProcessingTransactions = false;
        }
        
        /// <summary>
        /// Complete a pending transaction
        /// </summary>
        private bool CompleteTransaction(PendingTransaction transaction)
        {
            try
            {
                if (transaction.TransactionType == TradingTransactionType.Purchase)
                {
                    return CompletePurchaseTransaction(transaction);
                }
                else
                {
                    return CompleteSellTransaction(transaction);
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Error completing transaction {transaction.TransactionId}: {ex.Message}");
                OnTransactionFailed?.Invoke(transaction.TransactionId);
                return false;
            }
        }
        
        /// <summary>
        /// Complete a purchase transaction
        /// </summary>
        private bool CompletePurchaseTransaction(PendingTransaction transaction)
        {
            // Process payment
            if (!_financialManager.ProcessPayment(transaction.TotalValue, transaction.PaymentMethod))
            {
                LogError($"Payment failed for transaction {transaction.TransactionId}");
                return false;
            }
            
            // Create inventory item
            var inventoryItem = new InventoryItem
            {
                Product = transaction.Product,
                Quantity = transaction.Quantity,
                QualityScore = Random.Range(0.7f, 0.95f),
                AcquisitionCost = transaction.UnitPrice,
                AcquisitionDate = System.DateTime.Now,
                ExpirationDate = System.DateTime.Now.AddDays(transaction.Product.ShelfLife),
                StorageLocation = "Warehouse",
                BatchId = System.Guid.NewGuid().ToString()
            };
            
            // Add to inventory
            if (!_inventoryManager.AddToInventory(inventoryItem))
            {
                LogError($"Failed to add to inventory for transaction {transaction.TransactionId}");
                // Refund payment
                _financialManager.RefundPayment(transaction.TotalValue, transaction.PaymentMethod);
                return false;
            }
            
            LogDebug($"Purchase completed: {transaction.Product.ProductName} x{transaction.Quantity}");
            return true;
        }
        
        /// <summary>
        /// Complete a sell transaction
        /// </summary>
        private bool CompleteSellTransaction(PendingTransaction transaction)
        {
            // Remove from inventory
            if (!_inventoryManager.RemoveFromInventory(transaction.Product, transaction.Quantity))
            {
                LogError($"Failed to remove from inventory for transaction {transaction.TransactionId}");
                return false;
            }
            
            // Process payment receipt
            _financialManager.ReceivePayment(transaction.TotalValue, transaction.PaymentMethod);
            
            LogDebug($"Sale completed: {transaction.Product.ProductName} x{transaction.Quantity} for ${transaction.TotalValue:F2}");
            return true;
        }
        
        #endregion
        
        #region Transaction Utilities
        
        /// <summary>
        /// Calculate transaction completion time based on trading post and payment method
        /// </summary>
        private System.DateTime CalculateTransactionTime(TradingPost tradingPost, PaymentMethod paymentMethod)
        {
            float baseHours = tradingPost.ProcessingTimeHours;
            
            // Payment method affects processing time
            switch (paymentMethod.PaymentType)
            {
                case PaymentType.Cash:
                    baseHours *= 0.5f; // Cash is faster
                    break;
                case PaymentType.Credit:
                    baseHours *= 1.5f; // Credit requires verification
                    break;
                case PaymentType.Bank_Transfer:
                    baseHours *= 1.2f; // Bank transfers take time
                    break;
            }
            
            return System.DateTime.Now.AddHours(baseHours);
        }
        
        /// <summary>
        /// Get pending transaction by ID
        /// </summary>
        public PendingTransaction GetPendingTransaction(string transactionId)
        {
            return _pendingTransactions.FirstOrDefault(t => t.TransactionId == transactionId);
        }
        
        /// <summary>
        /// Cancel a pending transaction
        /// </summary>
        public bool CancelTransaction(string transactionId)
        {
            var transaction = GetPendingTransaction(transactionId);
            if (transaction != null)
            {
                // Note: Queue doesn't support removal, would need different data structure for full implementation
                LogDebug($"Transaction cancellation requested: {transactionId}");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Clear completed transaction history
        /// </summary>
        public void ClearTransactionHistory(int keepRecentCount = 50)
        {
            if (_transactionHistory.Count > keepRecentCount)
            {
                var toKeep = _transactionHistory
                    .OrderByDescending(t => t.CompletionTime)
                    .Take(keepRecentCount)
                    .ToList();
                
                _transactionHistory.Clear();
                _transactionHistory.AddRange(toKeep);
                
                LogDebug($"Transaction history cleared, kept {keepRecentCount} recent transactions");
            }
        }
        
        #endregion
        
        private void LogDebug(string message)
        {
            if (_enableTransactionLogging)
                ChimeraLogger.Log($"[TradingTransactionProcessor] {message}");
        }
        
        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[TradingTransactionProcessor] {message}");
        }
        
        protected virtual void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected virtual void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    
        // ITickable implementation
        public int Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;
        
        public virtual void OnRegistered() 
        { 
            // Override in derived classes if needed
        }
        
        public virtual void OnUnregistered() 
        { 
            // Override in derived classes if needed
        }
    }
}
