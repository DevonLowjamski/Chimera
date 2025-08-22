using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Systems.Registry;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using TradingTransactionType = ProjectChimera.Data.Economy.TradingTransactionType;
using PendingTransaction = ProjectChimera.Data.Economy.PendingTransaction;
using CompletedTransaction = ProjectChimera.Data.Economy.CompletedTransaction;
using TransactionStatus = ProjectChimera.Data.Economy.TransactionStatus;
using TradingPost = ProjectChimera.Data.Economy.TradingPost;
using PaymentMethod = ProjectChimera.Data.Economy.PaymentMethod;
using PaymentMethodType = ProjectChimera.Data.Economy.PaymentMethodType;
using TransactionSettings = ProjectChimera.Data.Economy.TransactionSettings;
using TransactionResult = ProjectChimera.Data.Economy.TransactionResult;
using InventoryItem = ProjectChimera.Data.Economy.InventoryItem;
// using ProjectChimera.Systems.Economy; // Use ProjectChimera.Data.Economy for data types

namespace ProjectChimera.Systems.Services.Economy
{
    /// <summary>
    /// PC014-4a: Transaction Processing Service
    /// Handles buy/sell transactions, payment processing, and transaction history
    /// Decomposed from TradingManager (500 lines target)
    /// </summary>
    public class TransactionProcessingService : MonoBehaviour, ITransactionProcessingService
    {
        #region Properties
        
        public bool IsInitialized { get; private set; }
        public List<CompletedTransaction> TransactionHistory => _transactionHistory;
        
        #endregion

        #region Private Fields
        
        [Header("Transaction Configuration")]
        [SerializeField] private bool _enableTransactions = true;
        [SerializeField] private TransactionSettings _transactionSettings;
        [SerializeField] private float _transactionProcessingInterval = 0.1f; // In-game hours
        [SerializeField] private int _maxPendingTransactions = 100;
        
        [Header("Payment Methods")]
        [SerializeField] private List<PaymentMethod> _availablePaymentMethods = new List<PaymentMethod>();
        [SerializeField] private Dictionary<string, PaymentMethod> _paymentMethodLookup = new Dictionary<string, PaymentMethod>();
        
        [Header("Transaction Data")]
        [SerializeField] private Queue<PendingTransaction> _pendingTransactions = new Queue<PendingTransaction>();
        [SerializeField] private List<CompletedTransaction> _transactionHistory = new List<CompletedTransaction>();
        [SerializeField] private Dictionary<string, PendingTransaction> _activeTransactions = new Dictionary<string, PendingTransaction>();
        
        [Header("Statistics")]
        [SerializeField] private int _totalTransactionsProcessed = 0;
        [SerializeField] private float _totalTransactionValue = 0f;
        [SerializeField] private Dictionary<TradingTransactionType, int> _transactionCounts = new Dictionary<TradingTransactionType, int>();
        
        private float _timeSinceLastUpdate = 0f;
        
        #endregion

        #region Events
        
        public event Action<CompletedTransaction> OnTransactionCompleted; // transaction
        public event Action<PendingTransaction> OnTransactionStarted; // transaction
        public event Action<string, string> OnTransactionFailed; // transactionId, reason
        public event Action<string> OnTransactionCancelled; // transactionId
        
        #endregion

        #region IService Implementation
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("Initializing TransactionProcessingService...");
            
            // Initialize transaction system
            InitializeTransactionSystem();
            
            // Initialize payment methods
            InitializePaymentMethods();
            
            // Initialize transaction settings
            InitializeTransactionSettings();
            
            // Load existing data
            LoadExistingTransactionData();
            
            // Register with central ServiceRegistry
            ServiceRegistry.Instance.RegisterService<ITransactionProcessingService>(this, ServiceDomain.Economy);
            
            IsInitialized = true;
            Debug.Log("TransactionProcessingService initialized successfully");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("Shutting down TransactionProcessingService...");
            
            // Process any remaining transactions
            ProcessAllPendingTransactions();
            
            // Save transaction state
            SaveTransactionState();
            
            // Clear collections
            _pendingTransactions.Clear();
            _transactionHistory.Clear();
            _activeTransactions.Clear();
            _paymentMethodLookup.Clear();
            _transactionCounts.Clear();
            
            IsInitialized = false;
            Debug.Log("TransactionProcessingService shutdown complete");
        }
        
        #endregion

        #region Transaction Processing
        
        public TransactionResult InitiateBuyTransaction(MarketProductSO product, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod, string playerId)
        {
            var result = new TransactionResult
            {
                Success = false,
                TransactionId = Guid.NewGuid().ToString(),
                Product = product,
                Quantity = quantity,
                TradingPost = tradingPost
            };

            if (!_enableTransactions || !IsInitialized)
            {
                result.ErrorMessage = "Transaction service unavailable";
                return result;
            }

            // Validate transaction parameters
            if (!ValidateTransactionParameters(product, quantity, tradingPost, paymentMethod, result))
                return result;

            // Get current market price
            float unitPrice = GetMarketPrice(product, TradingTransactionType.Purchase, tradingPost);
            float totalCost = unitPrice * quantity;

            // Apply trading post markup and fees
            totalCost = ApplyTransactionFees(totalCost, paymentMethod, tradingPost);

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
                PlayerId = playerId,
                InitiationTime = DateTime.Now,
                EstimatedCompletionTime = CalculateTransactionTime(tradingPost, paymentMethod),
                Status = TransactionStatus.Pending
            };

            // Queue transaction
            if (!QueueTransaction(pendingTransaction))
            {
                result.ErrorMessage = "Unable to queue transaction - system at capacity";
                return result;
            }

            result.Success = true;
            result.UnitPrice = unitPrice;
            result.TotalValue = totalCost;
            result.EstimatedCompletionTime = pendingTransaction.EstimatedCompletionTime;

            OnTransactionStarted?.Invoke(pendingTransaction);
            Debug.Log($"Initiated buy transaction: {product.name} x{quantity} = ${totalCost:F2}");

            return result;
        }

        public TransactionResult InitiateSellTransaction(InventoryItem inventoryItem, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod, string playerId)
        {
            var result = new TransactionResult
            {
                Success = false,
                TransactionId = Guid.NewGuid().ToString(),
                Product = inventoryItem.Product,
                Quantity = quantity,
                TradingPost = tradingPost
            };

            if (!_enableTransactions || !IsInitialized)
            {
                result.ErrorMessage = "Transaction service unavailable";
                return result;
            }

            // Validate sell transaction
            if (inventoryItem.Quantity < quantity)
            {
                result.ErrorMessage = "Insufficient inventory quantity";
                return result;
            }

            // Get current market price for selling
            float unitPrice = GetMarketPrice(inventoryItem.Product, TradingTransactionType.Sale, tradingPost);
            float totalRevenue = unitPrice * quantity;

            // Apply trading post discount and fees
            totalRevenue = ApplyTransactionFees(totalRevenue, paymentMethod, tradingPost);

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
                PlayerId = playerId,
                InitiationTime = DateTime.Now,
                EstimatedCompletionTime = CalculateTransactionTime(tradingPost, paymentMethod),
                Status = TransactionStatus.Pending
            };

            // Queue transaction
            if (!QueueTransaction(pendingTransaction))
            {
                result.ErrorMessage = "Unable to queue transaction - system at capacity";
                return result;
            }

            result.Success = true;
            result.UnitPrice = unitPrice;
            result.TotalValue = totalRevenue;
            result.EstimatedCompletionTime = pendingTransaction.EstimatedCompletionTime;

            OnTransactionStarted?.Invoke(pendingTransaction);
            Debug.Log($"Initiated sell transaction: {inventoryItem.Product.name} x{quantity} = ${totalRevenue:F2}");

            return result;
        }

        public bool CancelTransaction(string transactionId)
        {
            if (_activeTransactions.ContainsKey(transactionId))
            {
                var transaction = _activeTransactions[transactionId];
                transaction.Status = TransactionStatus.Cancelled;
                _activeTransactions.Remove(transactionId);
                
                OnTransactionCancelled?.Invoke(transactionId);
                Debug.Log($"Cancelled transaction: {transactionId}");
                return true;
            }

            Debug.LogWarning($"Transaction not found for cancellation: {transactionId}");
            return false;
        }

        public TransactionStatus GetTransactionStatus(string transactionId)
        {
            if (_activeTransactions.ContainsKey(transactionId))
            {
                return _activeTransactions[transactionId].Status;
            }

            // Check completed transactions
            var completed = _transactionHistory.FirstOrDefault(t => t.TransactionId == transactionId);
            if (completed != null)
            {
                return TransactionStatus.Completed;
            }

            return TransactionStatus.Failed;
        }

        public List<PendingTransaction> GetPendingTransactions(string playerId = null)
        {
            var transactions = _pendingTransactions.ToList();
            
            if (!string.IsNullOrEmpty(playerId))
            {
                transactions = transactions.Where(t => t.PlayerId == playerId).ToList();
            }

            return transactions;
        }
        
        #endregion

        #region Payment Processing
        
        public bool ProcessPayment(PendingTransaction transaction)
        {
            if (transaction.PaymentMethod == null)
            {
                Debug.LogError("No payment method specified for transaction");
                return false;
            }

            try
            {
                // Validate payment method
                if (!ValidatePaymentMethod(transaction.PaymentMethod, transaction.TotalValue))
                {
                    return false;
                }

                // Process payment based on method type
                bool paymentSuccess = transaction.PaymentMethod.Type switch
                {
                    PaymentMethodType.Cash => ProcessCashPayment(transaction),
                    PaymentMethodType.Credit => ProcessCreditPayment(transaction),
                    PaymentMethodType.Cryptocurrency => ProcessCryptoPayment(transaction),
                    PaymentMethodType.Barter => ProcessBarterPayment(transaction),
                    PaymentMethodType.Contract => ProcessContractPayment(transaction),
                    _ => false
                };

                if (paymentSuccess)
                {
                    transaction.Status = TransactionStatus.Processing;
                    Debug.Log($"Payment processed successfully for transaction {transaction.TransactionId}");
                }

                return paymentSuccess;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Payment processing failed: {ex.Message}");
                return false;
            }
        }

        public List<PaymentMethod> GetAvailablePaymentMethods(string playerId)
        {
            // Filter payment methods based on player status, reputation, etc.
            return _availablePaymentMethods.Where(pm => pm.IsAvailable).ToList();
        }

        public bool ValidatePaymentMethod(PaymentMethod paymentMethod, float transactionAmount)
        {
            if (!paymentMethod.IsAvailable)
                return false;

            if (transactionAmount > paymentMethod.MaxTransactionAmount)
                return false;

            return true;
        }
        
        #endregion

        #region Transaction History
        
        public List<CompletedTransaction> GetTransactionHistory(string playerId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = _transactionHistory.AsQueryable();

            if (!string.IsNullOrEmpty(playerId))
            {
                transactions = transactions.Where(t => t.PlayerId == playerId);
            }

            if (startDate.HasValue)
            {
                transactions = transactions.Where(t => t.CompletionTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                transactions = transactions.Where(t => t.CompletionTime <= endDate.Value);
            }

            return transactions.ToList();
        }

        public TradingPerformanceMetrics GetPerformanceMetrics(string playerId)
        {
            var playerTransactions = GetTransactionHistory(playerId);
            
            var metrics = new TradingPerformanceMetrics
            {
                PlayerId = playerId,
                LastUpdate = DateTime.Now
            };

            if (playerTransactions.Count == 0)
                return metrics;

            metrics.SuccessfulTrades = playerTransactions.Count(t => t.ProfitLoss >= 0);
            metrics.FailedTrades = playerTransactions.Count(t => t.ProfitLoss < 0);
            metrics.SuccessRate = (float)metrics.SuccessfulTrades / playerTransactions.Count;
            
            metrics.TotalProfit = playerTransactions.Where(t => t.ProfitLoss > 0).Sum(t => t.ProfitLoss);
            metrics.TotalLoss = Math.Abs(playerTransactions.Where(t => t.ProfitLoss < 0).Sum(t => t.ProfitLoss));
            metrics.NetProfit = metrics.TotalProfit - metrics.TotalLoss;
            
            metrics.AverageTradeSize = playerTransactions.Average(t => t.TotalValue);
            metrics.LargestProfit = playerTransactions.Max(t => t.ProfitLoss);
            metrics.LargestLoss = playerTransactions.Min(t => t.ProfitLoss);

            return metrics;
        }
        
        #endregion

        #region Private Helper Methods
        
        private void InitializeTransactionSystem()
        {
            if (_pendingTransactions == null)
                _pendingTransactions = new Queue<PendingTransaction>();
            
            if (_transactionHistory == null)
                _transactionHistory = new List<CompletedTransaction>();
            
            if (_activeTransactions == null)
                _activeTransactions = new Dictionary<string, PendingTransaction>();
            
            if (_transactionCounts == null)
                _transactionCounts = new Dictionary<TradingTransactionType, int>();

            Debug.Log("Transaction system initialized");
        }

        private void InitializePaymentMethods()
        {
            if (_availablePaymentMethods.Count == 0)
            {
                CreateDefaultPaymentMethods();
            }

            // Build lookup dictionary
            _paymentMethodLookup.Clear();
            foreach (var method in _availablePaymentMethods)
            {
                _paymentMethodLookup[method.PaymentId] = method;
            }

            Debug.Log($"Initialized {_availablePaymentMethods.Count} payment methods");
        }

        private void CreateDefaultPaymentMethods()
        {
            _availablePaymentMethods.Add(new PaymentMethod
            {
                PaymentId = "cash",
                Name = "Cash",
                Type = PaymentMethodType.Cash,
                TransactionFeePercentage = 0f,
                ProcessingTime = 0f,
                MaxTransactionAmount = 10000f,
                IsAvailable = true
            });

            _availablePaymentMethods.Add(new PaymentMethod
            {
                PaymentId = "credit",
                Name = "Credit Card",
                Type = PaymentMethodType.Credit,
                TransactionFeePercentage = 0.03f,
                ProcessingTime = 0.1f,
                MaxTransactionAmount = 50000f,
                IsAvailable = true
            });

            _availablePaymentMethods.Add(new PaymentMethod
            {
                PaymentId = "crypto",
                Name = "Cryptocurrency",
                Type = PaymentMethodType.Cryptocurrency,
                TransactionFeePercentage = 0.01f,
                ProcessingTime = 0.5f,
                MaxTransactionAmount = 100000f,
                IsAvailable = true
            });
        }

        private void InitializeTransactionSettings()
        {
            if (_transactionSettings == null)
            {
                _transactionSettings = new TransactionSettings();
            }
        }

        private void LoadExistingTransactionData()
        {
            // TODO: Load from persistent storage
            Debug.Log("Loading existing transaction data...");
        }

        private void SaveTransactionState()
        {
            // TODO: Save to persistent storage
            Debug.Log("Saving transaction state...");
        }

        private bool ValidateTransactionParameters(MarketProductSO product, float quantity, TradingPost tradingPost, PaymentMethod paymentMethod, TransactionResult result)
        {
            if (product == null)
            {
                result.ErrorMessage = "Invalid product";
                return false;
            }

            if (quantity <= 0)
            {
                result.ErrorMessage = "Invalid quantity";
                return false;
            }

            if (tradingPost == null)
            {
                result.ErrorMessage = "Invalid trading post";
                return false;
            }

            if (paymentMethod == null)
            {
                result.ErrorMessage = "Invalid payment method";
                return false;
            }

            return true;
        }

        private float GetMarketPrice(MarketProductSO product, TradingTransactionType transactionType, TradingPost tradingPost)
        {
            // Get price from market manager
            var marketManager = ProjectChimera.Core.GameManager.Instance?.GetManager<ProjectChimera.Systems.Economy.MarketManager>();
            if (marketManager == null)
            {
                Debug.LogWarning("Market manager not available, using default price");
                return 100f; // Default price
            }

            bool isWholesale = transactionType == TradingTransactionType.Purchase;
            return marketManager.GetCurrentPrice(product, isWholesale);
        }

        private float ApplyTransactionFees(float baseAmount, PaymentMethod paymentMethod, TradingPost tradingPost)
        {
            float amount = baseAmount;

            // Apply payment method fees
            if (paymentMethod != null && _transactionSettings.EnableTransactionFees)
            {
                float fee = amount * paymentMethod.TransactionFeePercentage;
                amount -= fee;
            }

            return amount;
        }

        private DateTime CalculateTransactionTime(TradingPost tradingPost, PaymentMethod paymentMethod)
        {
            float processingTime = paymentMethod?.ProcessingTime ?? _transactionSettings.DefaultProcessingTime;
            return DateTime.Now.AddHours(processingTime);
        }

        private bool QueueTransaction(PendingTransaction transaction)
        {
            if (_pendingTransactions.Count >= _maxPendingTransactions)
            {
                Debug.LogWarning("Transaction queue at capacity");
                return false;
            }

            _pendingTransactions.Enqueue(transaction);
            _activeTransactions[transaction.TransactionId] = transaction;
            return true;
        }

        private void ProcessAllPendingTransactions()
        {
            while (_pendingTransactions.Count > 0)
            {
                var transaction = _pendingTransactions.Dequeue();
                CompleteTransaction(transaction);
            }
        }

        private void CompleteTransaction(PendingTransaction transaction)
        {
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
                PlayerId = transaction.PlayerId,
                CompletionTime = DateTime.Now,
                ProfitLoss = CalculateProfitLoss(transaction),
                Notes = $"Transaction completed successfully"
            };

            _transactionHistory.Add(completedTransaction);
            _totalTransactionsProcessed++;
            _totalTransactionValue += transaction.TotalValue;

            if (!_transactionCounts.ContainsKey(transaction.TransactionType))
            {
                _transactionCounts[transaction.TransactionType] = 0;
            }
            _transactionCounts[transaction.TransactionType]++;

            _activeTransactions.Remove(transaction.TransactionId);

            OnTransactionCompleted?.Invoke(completedTransaction);
            Debug.Log($"Completed transaction: {transaction.TransactionId}");
        }

        private float CalculateProfitLoss(PendingTransaction transaction)
        {
            // Simple profit/loss calculation - would be more complex in real implementation
            return transaction.TransactionType == TradingTransactionType.Sale ? 
                transaction.TotalValue * 0.1f : // 10% profit on sales
                -transaction.TotalValue * 0.02f; // 2% cost on purchases
        }

        private bool ProcessCashPayment(PendingTransaction transaction)
        {
            // Cash payment logic
            Debug.Log($"Processing cash payment for ${transaction.TotalValue:F2}");
            return true;
        }

        private bool ProcessCreditPayment(PendingTransaction transaction)
        {
            // Credit payment logic
            Debug.Log($"Processing credit payment for ${transaction.TotalValue:F2}");
            return true;
        }

        private bool ProcessCryptoPayment(PendingTransaction transaction)
        {
            // Cryptocurrency payment logic
            Debug.Log($"Processing crypto payment for ${transaction.TotalValue:F2}");
            return true;
        }

        private bool ProcessBarterPayment(PendingTransaction transaction)
        {
            // Barter payment logic
            Debug.Log($"Processing barter payment for ${transaction.TotalValue:F2}");
            return true;
        }

        private bool ProcessContractPayment(PendingTransaction transaction)
        {
            // Contract payment logic
            Debug.Log($"Processing contract payment for ${transaction.TotalValue:F2}");
            return true;
        }
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void Update()
        {
            if (!IsInitialized || !_enableTransactions) return;

            _timeSinceLastUpdate += UnityEngine.Time.deltaTime;

            // Find TimeManager using reflection to avoid generic constraint issues
            var timeManager = FindTimeManager();
            float gameTimeDelta = GetScaledDeltaTimeFromManager(timeManager) ?? UnityEngine.Time.deltaTime;

            if (_timeSinceLastUpdate >= _transactionProcessingInterval * gameTimeDelta)
            {
                ProcessPendingTransactions();
                _timeSinceLastUpdate = 0f;
            }
        }

        private void ProcessPendingTransactions()
        {
            int processedCount = 0;
            var transactionsToProcess = new List<PendingTransaction>();

            // Get transactions ready for processing
            foreach (var transaction in _pendingTransactions)
            {
                if (DateTime.Now >= transaction.EstimatedCompletionTime)
                {
                    transactionsToProcess.Add(transaction);
                }
            }

            // Process ready transactions
            foreach (var transaction in transactionsToProcess)
            {
                if (ProcessPayment(transaction))
                {
                    CompleteTransaction(transaction);
                    processedCount++;
                }
                else
                {
                    transaction.Status = TransactionStatus.Failed;
                    OnTransactionFailed?.Invoke(transaction.TransactionId, "Payment processing failed");
                }
            }

            if (processedCount > 0)
            {
                Debug.Log($"Processed {processedCount} transactions");
            }
        }
        
        /// <summary>
        /// Helper method to find TimeManager using reflection to avoid generic constraints
        /// </summary>
        private MonoBehaviour FindTimeManager()
        {
            var allManagers = FindObjectsOfType<MonoBehaviour>();
            foreach (var manager in allManagers)
            {
                if (manager.GetType().Name == "TimeManager")
                {
                    return manager;
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method to get scaled delta time from TimeManager using reflection
        /// </summary>
        private float? GetScaledDeltaTimeFromManager(MonoBehaviour timeManager)
        {
            if (timeManager == null) return null;
            
            var method = timeManager.GetType().GetMethod("GetScaledDeltaTime");
            if (method != null)
            {
                var result = method.Invoke(timeManager, null);
                if (result is float deltaTime)
                {
                    return deltaTime;
                }
            }
            return null;
        }
        
        #endregion
    }
}