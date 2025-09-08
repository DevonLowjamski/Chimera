using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Market;
using ProjectChimera.Data.Economy.Configuration;
using ProjectChimera.Core.Events;
// Explicit aliases to resolve TransactionStatus and TransactionType ambiguity
using ConfigTransactionStatus = ProjectChimera.Data.Economy.Configuration.TransactionStatus;
using MarketTransactionType = ProjectChimera.Data.Economy.Market.TransactionType;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Simplified market manager for basic marketplace functionality.
    /// Handles simple buying/selling of Schematics and Genetics at fixed prices.
    /// Simplified for v4.0 scope.
    /// </summary>
    public class MarketManager : DIChimeraManager, ITickable
    {
        [Header("Marketplace Configuration")]
        [SerializeField] private SimpleMarketplaceConfig _marketplaceConfig;
        [SerializeField] private SimplePriceManager _priceManager;
        [SerializeField] private SimpleTransactionLogger _transactionLogger;
        
        [Header("Available Products")]
        [SerializeField] private List<SimpleProductData> _availableProducts = new List<SimpleProductData>();
        
        [Header("Events")]
        [SerializeField] private SimpleGameEventSO _transactionCompletedEvent;
        
        // Runtime Data
        private Dictionary<string, SimpleProductData> _productCatalog;
        private List<SimpleUserProfile> _userProfiles;
        
        public SimpleMarketplaceConfig MarketplaceConfig => _marketplaceConfig;
        public SimplePriceManager PriceManager => _priceManager;
        
        // Events
        public System.Action<SimpleTransaction> OnTransactionCompleted;
        public System.Action<string, float> OnPriceUpdated; // productId, newPrice
        public System.Action<string> OnProductAdded; // productId
        
        protected override void OnManagerInitialize()
        {
            _productCatalog = new Dictionary<string, SimpleProductData>();
            _userProfiles = new List<SimpleUserProfile>();
            
            InitializeMarketplaceConfig();
            InitializePriceManager();
            InitializeTransactionLogger();
            InitializeProductCatalog();
            
            ChimeraLogger.Log("SimplifiedMarketManager initialized successfully");
        }
        
        protected override void OnManagerShutdown()
        {
            // Cleanup resources
        }
        
        #region ITickable Implementation
        
        public int Priority => TickPriority.EconomyManager;
        public bool Enabled => IsInitialized;
        
        public void Tick(float deltaTime)
        {
            if (!Enabled) return;
            
            // No complex market dynamics - marketplace is stable with fixed prices
            // Only basic maintenance operations if needed
        }
        
        public void OnRegistered()
        {
            ChimeraLogger.LogVerbose("[MarketManager] Registered with UpdateOrchestrator");
        }
        
        public void OnUnregistered()
        {
            ChimeraLogger.LogVerbose("[MarketManager] Unregistered from UpdateOrchestrator");
        }
        
        #endregion
        
        #region Initialization Methods
        
        private void InitializeMarketplaceConfig()
        {
            if (_marketplaceConfig == null)
            {
                _marketplaceConfig = new SimpleMarketplaceConfig();
            }
            _marketplaceConfig.Initialize();
        }
        
        private void InitializePriceManager()
        {
            if (_priceManager == null)
            {
                _priceManager = new SimplePriceManager();
            }
            _priceManager.Initialize();
        }
        
        private void InitializeTransactionLogger()
        {
            if (_transactionLogger == null)
            {
                _transactionLogger = new SimpleTransactionLogger();
            }
            _transactionLogger.Initialize();
        }
        
        private void InitializeProductCatalog()
        {
            foreach (var product in _availableProducts)
            {
                _productCatalog[product.ProductId] = product;
            }
        }
        
        #endregion
        
        #region Core Marketplace Methods
        
        /// <summary>
        /// Gets current fixed price for a product.
        /// </summary>
        public float GetProductPrice(string productId)
        {
            return _priceManager.GetPrice(productId);
        }
        
        /// <summary>
        /// Gets current price for a MarketProductSO with wholesale/retail option.
        /// Backward compatibility method.
        /// </summary>
        public float GetCurrentPrice(MarketProductSO product, bool isWholesale)
        {
            if (product == null) return 0f;
            return GetProductPrice(product.ProductName);
        }
        
        /// <summary>
        /// Gets product information by ID.
        /// </summary>
        public SimpleProductData GetProduct(string productId)
        {
            return _productCatalog.ContainsKey(productId) ? _productCatalog[productId] : null;
        }
        
        /// <summary>
        /// Gets all available products.
        /// </summary>
        public List<SimpleProductData> GetAllProducts()
        {
            return _availableProducts.Where(p => p.IsAvailable).ToList();
        }
        
        /// <summary>
        /// Processes a simple purchase transaction.
        /// </summary>
        public SimpleTransaction ProcessPurchase(string productId, float quantity, string buyerId)
        {
            if (!_productCatalog.ContainsKey(productId))
            {
                ChimeraLogger.LogWarning($"Product {productId} not found in catalog");
                return null;
            }
            
            var product = _productCatalog[productId];
            if (!product.IsAvailable)
            {
                ChimeraLogger.LogWarning($"Product {productId} is not available");
                return null;
            }
            
            float unitPrice = _priceManager.GetPrice(productId);
            float totalValue = unitPrice * quantity;
            float taxAmount = totalValue * _marketplaceConfig.DefaultTaxRate;
            float commissionAmount = totalValue * _marketplaceConfig.DefaultCommissionRate;
            
            var transaction = new SimpleTransaction
            {
                TransactionId = System.Guid.NewGuid().ToString(),
                ProductId = productId,
                ProductName = product.ProductName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalValue = totalValue,
                Type = MarketTransactionType.Purchase,
                TransactionDate = System.DateTime.Now,
                Success = true
            };
            
            // Log the transaction
            var transactionRecord = new SimpleTransactionRecord
            {
                TransactionId = transaction.TransactionId,
                ProductId = productId,
                BuyerId = buyerId,
                SellerId = "marketplace", // Simple marketplace is the seller
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalValue = totalValue,
                TaxAmount = taxAmount,
                CommissionAmount = commissionAmount,
                Status = ConfigTransactionStatus.Completed,
                TransactionDate = System.DateTime.Now,
                Notes = "Marketplace purchase"
            };
            
            _transactionLogger.LogTransaction(transactionRecord);
            
            // Invoke events
            OnTransactionCompleted?.Invoke(transaction);
            _transactionCompletedEvent?.Raise();
            
            return transaction;
        }
        
        /// <summary>
        /// Processes a simple sale transaction.
        /// </summary>
        public SimpleTransaction ProcessSale(string productId, float quantity, string sellerId)
        {
            if (!_productCatalog.ContainsKey(productId))
            {
                ChimeraLogger.LogWarning($"Product {productId} not found in catalog");
                return null;
            }
            
            var product = _productCatalog[productId];
            
            float unitPrice = _priceManager.GetPrice(productId);
            float totalValue = unitPrice * quantity;
            float taxAmount = totalValue * _marketplaceConfig.DefaultTaxRate;
            float commissionAmount = totalValue * _marketplaceConfig.DefaultCommissionRate;
            
            var transaction = new SimpleTransaction
            {
                TransactionId = System.Guid.NewGuid().ToString(),
                ProductId = productId,
                ProductName = product.ProductName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalValue = totalValue,
                Type = MarketTransactionType.Sale,
                TransactionDate = System.DateTime.Now,
                Success = true
            };
            
            // Log the transaction
            var transactionRecord = new SimpleTransactionRecord
            {
                TransactionId = transaction.TransactionId,
                ProductId = productId,
                BuyerId = "marketplace", // Simple marketplace is the buyer
                SellerId = sellerId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalValue = totalValue,
                TaxAmount = taxAmount,
                CommissionAmount = commissionAmount,
                Status = ConfigTransactionStatus.Completed,
                TransactionDate = System.DateTime.Now,
                Notes = "Marketplace sale"
            };
            
            _transactionLogger.LogTransaction(transactionRecord);
            
            // Invoke events
            OnTransactionCompleted?.Invoke(transaction);
            _transactionCompletedEvent?.Raise();
            
            return transaction;
        }
        
        #endregion
        
        #region Administrative Methods
        
        /// <summary>
        /// Adds a new product to the marketplace.
        /// </summary>
        public void AddProduct(SimpleProductData product)
        {
            if (!_productCatalog.ContainsKey(product.ProductId))
            {
                _productCatalog[product.ProductId] = product;
                _availableProducts.Add(product);
                
                // Set default price if not already set
                if (_priceManager.GetPrice(product.ProductId) == 0f)
                {
                    _priceManager.SetPrice(product.ProductId, product.BasePrice);
                }
                
                OnProductAdded?.Invoke(product.ProductId);
                ChimeraLogger.Log($"Added product {product.ProductName} to marketplace");
            }
        }
        
        /// <summary>
        /// Updates the price of a product (admin function).
        /// </summary>
        public void UpdateProductPrice(string productId, float newPrice)
        {
            if (_productCatalog.ContainsKey(productId))
            {
                _priceManager.SetPrice(productId, newPrice);
                OnPriceUpdated?.Invoke(productId, newPrice);
                ChimeraLogger.Log($"Updated price for {productId} to {newPrice}");
            }
        }
        
        /// <summary>
        /// Gets transaction history for reporting purposes.
        /// </summary>
        public List<SimpleTransactionRecord> GetTransactionHistory(int days = 30)
        {
            return _transactionLogger.GetRecentTransactions(days);
        }
        
        /// <summary>
        /// Gets basic marketplace statistics.
        /// </summary>
        public MarketplaceStats GetMarketplaceStats()
        {
            var recentTransactions = _transactionLogger.GetRecentTransactions(7);
            
            return new MarketplaceStats
            {
                TotalTransactions = recentTransactions.Count,
                TotalRevenue = recentTransactions.Sum(t => t.TotalValue),
                AverageTransactionValue = recentTransactions.Any() ? recentTransactions.Average(t => t.TotalValue) : 0f,
                ActiveProducts = _availableProducts.Count(p => p.IsAvailable),
                LastTransactionDate = recentTransactions.Any() ? recentTransactions.Max(t => t.TransactionDate) : System.DateTime.MinValue
            };
        }
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    /// <summary>
    /// Basic marketplace statistics for reporting.
    /// </summary>
    [System.Serializable]
    public class MarketplaceStats
    {
        public int TotalTransactions;
        public float TotalRevenue;
        public float AverageTransactionValue;
        public int ActiveProducts;
        public System.DateTime LastTransactionDate;
    }
    
    #endregion
}