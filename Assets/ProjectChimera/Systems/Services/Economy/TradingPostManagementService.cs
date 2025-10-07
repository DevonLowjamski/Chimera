using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Services.Economy
{
    /// <summary>
    /// SIMPLE: Basic trading system aligned with Project Chimera's economy needs.
    /// Focuses on essential trading functionality without complex market systems.
    /// </summary>
    public class TradingPostManagementService : MonoBehaviour
    {
        [Header("Basic Trading Settings")]
        [SerializeField] private bool _enableBasicTrading = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _baseMarkup = 1.1f; // 10% markup

        // Basic trading data
        private readonly List<TradingItem> _availableItems = new List<TradingItem>();
        private float _playerCurrency = 1000f;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for trading operations
        /// </summary>
        public event System.Action<TradingItem> OnItemPurchased;
        public event System.Action<TradingItem> OnItemSold;
        public event System.Action<float> OnCurrencyChanged;

        /// <summary>
        /// Initialize basic trading system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_enableBasicTrading)
            {
                SetupBasicItems();
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get available items for purchase
        /// </summary>
        public List<TradingItem> GetAvailableItems()
        {
            return new List<TradingItem>(_availableItems);
        }

        /// <summary>
        /// Purchase an item
        /// </summary>
        public bool PurchaseItem(TradingItem item)
        {
            if (item == null || !_availableItems.Contains(item)) return false;

            float cost = item.BasePrice * _baseMarkup;
            if (_playerCurrency >= cost)
            {
                _playerCurrency -= cost;
                OnItemPurchased?.Invoke(item);
                OnCurrencyChanged?.Invoke(_playerCurrency);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("OTHER", "$1", this);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sell an item
        /// </summary>
        public bool SellItem(TradingItem item)
        {
            if (item == null) return false;

            float salePrice = item.BasePrice * 0.8f; // 80% of base price when selling
            _playerCurrency += salePrice;
            OnItemSold?.Invoke(item);
            OnCurrencyChanged?.Invoke(_playerCurrency);

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }

            return true;
        }

        /// <summary>
        /// Get player currency
        /// </summary>
        public float GetPlayerCurrency()
        {
            return _playerCurrency;
        }

        /// <summary>
        /// Add currency to player
        /// </summary>
        public void AddCurrency(float amount)
        {
            _playerCurrency += amount;
            OnCurrencyChanged?.Invoke(_playerCurrency);

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Check if player can afford item
        /// </summary>
        public bool CanAffordItem(TradingItem item)
        {
            if (item == null) return false;
            float cost = item.BasePrice * _baseMarkup;
            return _playerCurrency >= cost;
        }

        /// <summary>
        /// Get item by name
        /// </summary>
        public TradingItem GetItemByName(string itemName)
        {
            return _availableItems.Find(item => item.ItemName == itemName);
        }

        /// <summary>
        /// Get items by category
        /// </summary>
        public List<TradingItem> GetItemsByCategory(ItemCategory category)
        {
            return _availableItems.FindAll(item => item.Category == category);
        }

        /// <summary>
        /// Get trading statistics
        /// </summary>
        public TradingStatistics GetTradingStatistics()
        {
            int totalItems = _availableItems.Count;
            int equipmentItems = _availableItems.FindAll(item => item.Category == ItemCategory.Equipment).Count;
            int consumableItems = _availableItems.FindAll(item => item.Category == ItemCategory.Consumable).Count;
            int seedItems = _availableItems.FindAll(item => item.Category == ItemCategory.Seed).Count;

            return new TradingStatistics
            {
                TotalItems = totalItems,
                EquipmentItems = equipmentItems,
                ConsumableItems = consumableItems,
                SeedItems = seedItems,
                PlayerCurrency = _playerCurrency
            };
        }

        /// <summary>
        /// Restock items (basic implementation)
        /// </summary>
        public void RestockItems()
        {
            // Basic restock - could be expanded if needed
            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        #region Private Methods

        private void SetupBasicItems()
        {
            // Add basic items for cultivation
            _availableItems.AddRange(new[]
            {
                new TradingItem { ItemName = "Watering Can", Category = ItemCategory.Equipment, BasePrice = 50f, Description = "Basic watering tool" },
                new TradingItem { ItemName = "Fertilizer", Category = ItemCategory.Consumable, BasePrice = 25f, Description = "Plant nutrient boost" },
                new TradingItem { ItemName = "Seed Pack", Category = ItemCategory.Seed, BasePrice = 100f, Description = "Basic cannabis seeds" },
                new TradingItem { ItemName = "Grow Light", Category = ItemCategory.Equipment, BasePrice = 200f, Description = "LED grow light" },
                new TradingItem { ItemName = "Pot", Category = ItemCategory.Equipment, BasePrice = 15f, Description = "Plant container" },
                new TradingItem { ItemName = "Pruning Shears", Category = ItemCategory.Equipment, BasePrice = 30f, Description = "Plant maintenance tool" }
            });

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic trading item
    /// </summary>
    [System.Serializable]
    public class TradingItem
    {
        public string ItemName;
        public ItemCategory Category;
        public float BasePrice;
        public string Description;
    }

    /// <summary>
    /// Item categories
    /// </summary>
    public enum ItemCategory
    {
        Equipment,
        Consumable,
        Seed
    }

    /// <summary>
    /// Trading statistics
    /// </summary>
    [System.Serializable]
    public class TradingStatistics
    {
        public int TotalItems;
        public int EquipmentItems;
        public int ConsumableItems;
        public int SeedItems;
        public float PlayerCurrency;
    }
}
