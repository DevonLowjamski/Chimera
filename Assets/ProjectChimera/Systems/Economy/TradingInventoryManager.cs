using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Economy.Trading;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Manages player inventory, capacity, quality degradation, and stock tracking.
    /// Extracted from TradingManager for modular architecture.
    /// Handles inventory operations, decay processing, and stock queries.
    /// </summary>
    public class TradingInventoryManager : MonoBehaviour
    {
        [Header("Inventory Configuration")]
        [SerializeField] private bool _enableInventoryLogging = false;
        [SerializeField] private float _inventoryDecayInterval = 1f; // Process decay every hour
        [SerializeField] private bool _enableQualityDecay = true;
        [SerializeField] private bool _enableExpiration = true;
        
        // Inventory data
        private PlayerInventory _playerInventory;
        private float _lastDecayUpdate = 0f;
        
        // Events
        public System.Action<InventoryItem, float> OnInventoryChanged; // item, quantityChange
        public System.Action<InventoryItem> OnItemExpired;
        public System.Action<InventoryItem, float, float> OnQualityChanged; // item, oldQuality, newQuality
        public System.Action<float, float> OnCapacityChanged; // current, max
        
        // Properties
        public PlayerInventory PlayerInventory => _playerInventory;
        public float CurrentCapacity => _playerInventory?.CurrentCapacity ?? 0f;
        public float MaxCapacity => _playerInventory?.MaxCapacity ?? 0f;
        public float AvailableCapacity => MaxCapacity - CurrentCapacity;
        public int TotalItems => _playerInventory?.InventoryItems?.Count ?? 0;
        
        /// <summary>
        /// Initialize inventory manager with starting configuration
        /// </summary>
        public void Initialize(TradingSettings tradingSettings)
        {
            if (_playerInventory == null)
            {
                _playerInventory = new PlayerInventory
                {
                    InventoryItems = new List<InventoryItem>(),
                    MaxCapacity = tradingSettings?.StartingInventoryCapacity ?? 1000f,
                    CurrentCapacity = 0f,
                    DefaultStorageLocation = "Warehouse"
                };
            }
            
            LogDebug($"Trading inventory manager initialized - Capacity: {MaxCapacity:F1}");
        }
        
        private void Update()
        {
            if (_playerInventory == null) return;
            
            _lastDecayUpdate += Time.deltaTime;
            
            var timeManager = GameManager.Instance?.GetManager<TimeManager>();
            float gameTimeDelta = timeManager?.GetScaledDeltaTime() ?? Time.deltaTime;
            
            if (_lastDecayUpdate >= _inventoryDecayInterval * gameTimeDelta)
            {
                ProcessInventoryDecay();
                _lastDecayUpdate = 0f;
            }
        }
        
        #region Inventory Operations
        
        /// <summary>
        /// Add item to inventory with capacity checking
        /// </summary>
        public bool AddToInventory(InventoryItem item)
        {
            if (item == null || item.Product == null)
            {
                LogError("Cannot add null item or item with null product to inventory");
                return false;
            }
            
            if (CurrentCapacity + item.Quantity > MaxCapacity)
            {
                LogError($"Cannot add {item.Quantity:F1}g - exceeds capacity ({CurrentCapacity + item.Quantity:F1}/{MaxCapacity})");
                return false;
            }
            
            _playerInventory.InventoryItems.Add(item);
            _playerInventory.CurrentCapacity += item.Quantity;
            
            OnInventoryChanged?.Invoke(item, item.Quantity);
            OnCapacityChanged?.Invoke(CurrentCapacity, MaxCapacity);
            
            LogDebug($"Added {item.Quantity:F1}g of {item.Product.ProductName} to inventory");
            return true;
        }
        
        /// <summary>
        /// Remove quantity from inventory using FIFO
        /// </summary>
        public bool RemoveFromInventory(MarketProductSO product, float quantity)
        {
            if (product == null || quantity <= 0)
            {
                LogError("Invalid product or quantity for inventory removal");
                return false;
            }
            
            float remainingToRemove = quantity;
            var itemsToRemove = new List<InventoryItem>();
            
            // Use FIFO (First In, First Out) for inventory removal
            var sortedItems = _playerInventory.InventoryItems
                .Where(item => item.Product == product)
                .OrderBy(item => item.AcquisitionDate)
                .ToList();
            
            foreach (var item in sortedItems)
            {
                if (remainingToRemove <= 0) break;
                
                if (item.Quantity <= remainingToRemove)
                {
                    // Remove entire item
                    remainingToRemove -= item.Quantity;
                    _playerInventory.CurrentCapacity -= item.Quantity;
                    itemsToRemove.Add(item);
                    OnInventoryChanged?.Invoke(item, -item.Quantity);
                }
                else
                {
                    // Partially remove from item
                    float removedQuantity = remainingToRemove;
                    item.Quantity -= remainingToRemove;
                    _playerInventory.CurrentCapacity -= remainingToRemove;
                    remainingToRemove = 0;
                    OnInventoryChanged?.Invoke(item, -removedQuantity);
                }
            }
            
            // Remove empty items
            foreach (var item in itemsToRemove)
            {
                _playerInventory.InventoryItems.Remove(item);
            }
            
            OnCapacityChanged?.Invoke(CurrentCapacity, MaxCapacity);
            
            bool success = remainingToRemove <= 0.001f; // Allow for floating point precision
            if (success)
            {
                LogDebug($"Removed {quantity:F1}g of {product.ProductName} from inventory");
            }
            else
            {
                LogError($"Could not remove full quantity - {remainingToRemove:F1}g remaining");
            }
            
            return success;
        }
        
        /// <summary>
        /// Add harvested product to inventory with batch tracking
        /// </summary>
        public bool AddHarvestedProduct(MarketProductSO product, float quantity, float qualityScore, string batchId)
        {
            if (CurrentCapacity + quantity > MaxCapacity)
            {
                LogError($"Cannot add harvested product - exceeds capacity ({CurrentCapacity + quantity:F1}/{MaxCapacity})");
                return false;
            }
            
            var inventoryItem = new InventoryItem
            {
                Product = product,
                Quantity = quantity,
                QualityScore = qualityScore,
                InitialQualityScore = qualityScore,
                AcquisitionCost = 0f, // Harvested products have no acquisition cost
                AcquisitionDate = System.DateTime.Now,
                ExpirationDate = System.DateTime.Now.AddDays(product != null ? product.ShelfLife : 365f),
                StorageLocation = _playerInventory.DefaultStorageLocation,
                BatchId = batchId,
                BatchInfo = new BatchTrackingInfo
                {
                    HarvestDate = System.DateTime.Now,
                    HarvestQuality = qualityScore,
                    ProcessingMethod = "Standard",
                    TrackingMetadata = new Dictionary<string, object>
                    {
                        ["Source"] = "Harvest",
                        ["HarvestDate"] = System.DateTime.Now.ToString("yyyy-MM-dd")
                    }
                },
                Metadata = new BatchTrackingInfo()
            };
            
            return AddToInventory(inventoryItem);
        }
        
        #endregion
        
        #region Inventory Queries
        
        /// <summary>
        /// Get current inventory for a specific product
        /// </summary>
        public List<InventoryItem> GetInventoryForProduct(MarketProductSO product)
        {
            if (product == null) return new List<InventoryItem>();
            
            return _playerInventory.InventoryItems
                .Where(item => item.Product == product)
                .OrderBy(item => item.AcquisitionDate)
                .ToList();
        }
        
        /// <summary>
        /// Get total quantity of a product in inventory
        /// </summary>
        public float GetTotalInventoryQuantity(MarketProductSO product)
        {
            if (product == null) return 0f;
            
            return _playerInventory.InventoryItems
                .Where(item => item.Product == product)
                .Sum(item => item.Quantity);
        }
        
        /// <summary>
        /// Check if sufficient quantity is available
        /// </summary>
        public bool HasSufficientQuantity(MarketProductSO product, float requiredQuantity)
        {
            return GetTotalInventoryQuantity(product) >= requiredQuantity;
        }
        
        /// <summary>
        /// Get average quality for a product
        /// </summary>
        public float GetAverageQuality(MarketProductSO product)
        {
            var items = GetInventoryForProduct(product);
            if (items.Count == 0) return 0f;
            
            float weightedQuality = items.Sum(item => item.QualityScore * item.Quantity);
            float totalQuantity = items.Sum(item => item.Quantity);
            
            return totalQuantity > 0 ? weightedQuality / totalQuantity : 0f;
        }
        
        /// <summary>
        /// Calculate total inventory value using current market prices
        /// </summary>
        public float CalculateInventoryValue()
        {
            float totalValue = 0f;
            var marketManager = GameManager.Instance.GetManager<MarketManager>();
            
            if (marketManager != null)
            {
                foreach (var item in _playerInventory.InventoryItems)
                {
                    float currentPrice = marketManager.GetProductPrice(item.Product.ProductName);
                    // Apply quality modifier
                    float qualityModifier = item.QualityScore;
                    totalValue += currentPrice * item.Quantity * qualityModifier;
                }
            }
            
            return totalValue;
        }
        
        /// <summary>
        /// Get all inventory items sorted by various criteria
        /// </summary>
        public List<InventoryItem> GetAllInventoryItems(InventorySortCriteria sortBy = InventorySortCriteria.AcquisitionDate)
        {
            var items = _playerInventory.InventoryItems.ToList();
            
            switch (sortBy)
            {
                case InventorySortCriteria.AcquisitionDate:
                    return items.OrderBy(item => item.AcquisitionDate).ToList();
                case InventorySortCriteria.ExpirationDate:
                    return items.OrderBy(item => item.ExpirationDate).ToList();
                case InventorySortCriteria.Quality:
                    return items.OrderByDescending(item => item.QualityScore).ToList();
                case InventorySortCriteria.Quantity:
                    return items.OrderByDescending(item => item.Quantity).ToList();
                case InventorySortCriteria.Product:
                    return items.OrderBy(item => item.Product?.ProductName ?? "").ToList();
                default:
                    return items;
            }
        }
        
        #endregion
        
        #region Inventory Maintenance
        
        /// <summary>
        /// Process inventory decay and expiration
        /// </summary>
        private void ProcessInventoryDecay()
        {
            if (!_enableQualityDecay && !_enableExpiration) return;
            
            for (int i = _playerInventory.InventoryItems.Count - 1; i >= 0; i--)
            {
                var item = _playerInventory.InventoryItems[i];
                
                // Check for expiration
                if (_enableExpiration && System.DateTime.Now > item.ExpirationDate)
                {
                    // Remove expired items
                    _playerInventory.InventoryItems.RemoveAt(i);
                    _playerInventory.CurrentCapacity -= item.Quantity;
                    
                    OnItemExpired?.Invoke(item);
                    OnInventoryChanged?.Invoke(item, -item.Quantity);
                    LogDebug($"Expired item removed: {item.Product.ProductName} ({item.Quantity:F1}g)");
                    continue;
                }
                
                // Apply quality decay over time
                if (_enableQualityDecay && item.DegradationRate > 0)
                {
                    float daysSinceLastUpdate = (float)(System.DateTime.Now - item.LastQualityUpdate).TotalDays;
                    
                    if (daysSinceLastUpdate > 0)
                    {
                        float oldQuality = item.QualityScore;
                        float qualityLoss = item.DegradationRate * daysSinceLastUpdate;
                        item.QualityScore = Mathf.Max(0.01f, item.QualityScore - qualityLoss);
                        item.LastQualityUpdate = System.DateTime.Now;
                        
                        if (Mathf.Abs(oldQuality - item.QualityScore) > 0.01f)
                        {
                            OnQualityChanged?.Invoke(item, oldQuality, item.QualityScore);
                            item.RecordQualityDegradation(oldQuality - item.QualityScore);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Update storage conditions for all items
        /// </summary>
        public void UpdateStorageConditions(StorageEnvironment conditions)
        {
            foreach (var item in _playerInventory.InventoryItems)
            {
                item.UpdateStorageConditions(conditions);
            }
            
            LogDebug($"Updated storage conditions for {_playerInventory.InventoryItems.Count} items");
        }
        
        /// <summary>
        /// Optimize inventory storage (consolidate similar items, organize by quality, etc.)
        /// </summary>
        public void OptimizeInventory()
        {
            // Group similar items by product and batch if possible
            var groupedItems = _playerInventory.InventoryItems
                .GroupBy(item => new { item.Product, item.BatchId })
                .Where(group => group.Count() > 1)
                .ToList();
            
            foreach (var group in groupedItems)
            {
                var items = group.ToList();
                if (items.Count <= 1) continue;
                
                // Consolidate items with same product and batch
                var firstItem = items.First();
                float totalQuantity = items.Sum(item => item.Quantity);
                float weightedQuality = items.Sum(item => item.QualityScore * item.Quantity) / totalQuantity;
                
                // Remove all but first item
                for (int i = 1; i < items.Count; i++)
                {
                    _playerInventory.InventoryItems.Remove(items[i]);
                }
                
                // Update first item with consolidated data
                firstItem.Quantity = totalQuantity;
                firstItem.QualityScore = weightedQuality;
            }
            
            LogDebug("Inventory optimization completed");
        }
        
        /// <summary>
        /// Clear all inventory items
        /// </summary>
        public void ClearInventory()
        {
            int itemCount = _playerInventory.InventoryItems.Count;
            _playerInventory.InventoryItems.Clear();
            _playerInventory.CurrentCapacity = 0f;
            
            OnCapacityChanged?.Invoke(0f, MaxCapacity);
            LogDebug($"Cleared {itemCount} items from inventory");
        }
        
        /// <summary>
        /// Expand inventory capacity
        /// </summary>
        public bool ExpandCapacity(float additionalCapacity)
        {
            if (additionalCapacity <= 0) return false;
            
            float oldCapacity = _playerInventory.MaxCapacity;
            _playerInventory.MaxCapacity += additionalCapacity;
            
            OnCapacityChanged?.Invoke(CurrentCapacity, MaxCapacity);
            LogDebug($"Expanded inventory capacity: {oldCapacity:F1} -> {MaxCapacity:F1}");
            return true;
        }
        
        #endregion
        
        private void LogDebug(string message)
        {
            if (_enableInventoryLogging)
                Debug.Log($"[TradingInventoryManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[TradingInventoryManager] {message}");
        }
    }
    
    public enum InventorySortCriteria
    {
        AcquisitionDate,
        ExpirationDate,
        Quality,
        Quantity,
        Product
    }
}