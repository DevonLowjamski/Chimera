using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Simple Economy Manager - Aligned with Project Chimera's vision
    /// Provides basic currency and resource management as described in gameplay document
    /// Focuses on equipment purchasing and schematics trading without complex financial systems
    /// </summary>
    public interface ICurrencyManager
    {
        bool CanAfford(int cost);
        bool SpendCash(int amount);
        bool SpendCurrency(int currencyType, int amount, string reason = "");
        void AddCurrency(int currencyType, int amount, string description = "");
        void AddCurrency(int amount, string description = "");
        int GetCurrentCurrency();
    }

    public class SimpleEconomyManager : MonoBehaviour, ICurrencyManager
    {
        [Header("Starting Resources")]
        [SerializeField] private int _startingCurrency = 10000;
        [SerializeField] private int _startingSkillPoints = 0;

        [Header("Resource Limits")]
        [SerializeField] private int _maxCurrency = 999999;
        [SerializeField] private int _maxSkillPoints = 99999;

        // Current resources
        private int _currentCurrency;
        private int _currentSkillPoints;
        private Dictionary<string, int> _resourceStocks = new Dictionary<string, int>();

        // Transaction history (simple version)
        private List<TransactionRecord> _recentTransactions = new List<TransactionRecord>();

        private void Awake()
        {
            InitializeEconomy();
        }

        /// <summary>
        /// Initializes the economy system
        /// </summary>
        private void InitializeEconomy()
        {
            _currentCurrency = _startingCurrency;
            _currentSkillPoints = _startingSkillPoints;

            // Initialize basic resource stocks
            _resourceStocks["electricity"] = 100; // Starting power capacity
            _resourceStocks["water"] = 100;      // Starting water capacity
            _resourceStocks["nutrients"] = 50;    // Starting nutrient stock

            ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
        }

        /// <summary>
        /// Attempts to purchase an item
        /// </summary>
        public bool PurchaseItem(string itemName, int cost, string description = "")
        {
            if (_currentCurrency >= cost)
            {
                _currentCurrency -= cost;
                RecordTransaction($"Purchased {itemName}", -cost, description);

                ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
                return true;
            }
            else
            {
                ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
                return false;
            }
        }

        /// <summary>
        /// Attempts to purchase equipment
        /// </summary>
        public bool PurchaseEquipment(string equipmentName, int cost)
        {
            return PurchaseItem(equipmentName, cost, "Equipment purchase");
        }

        /// <summary>
        /// Attempts to purchase materials
        /// </summary>
        public bool PurchaseMaterials(string materialName, int quantity, int costPerUnit)
        {
            int totalCost = quantity * costPerUnit;
            return PurchaseItem($"{quantity}x {materialName}", totalCost, "Material purchase");
        }

        /// <summary>
        /// Attempts to purchase utilities (electricity, water, etc.)
        /// </summary>
        public bool PurchaseUtilities(string utilityType, int amount, int costPerUnit)
        {
            int totalCost = amount * costPerUnit;
            if (PurchaseItem($"{amount} {utilityType}", totalCost, "Utility purchase"))
            {
                // Add to resource stock
                if (_resourceStocks.ContainsKey(utilityType))
                {
                    _resourceStocks[utilityType] += amount;
                }
                else
                {
                    _resourceStocks[utilityType] = amount;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sells harvested cannabis
        /// </summary>
        public void SellHarvest(float grams, float pricePerGram, float qualityMultiplier = 1f)
        {
            int revenue = Mathf.RoundToInt(grams * pricePerGram * qualityMultiplier);
            AddCurrency(revenue, $"Sold {grams:F1}g harvest at ${pricePerGram:F2}/g");
        }

        /// <summary>
        /// Adds currency to player balance
        /// </summary>
        public void AddCurrency(int amount, string description = "")
        {
            _currentCurrency = Mathf.Min(_currentCurrency + amount, _maxCurrency);
            RecordTransaction($"Received {amount} currency", amount, description);

            ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
        }

        // ICurrencyManager explicit method for typed currency (0=cash for now)
        public void AddCurrency(int currencyType, int amount, string description = "")
        {
            AddCurrency(amount, description);
        }

        /// <summary>
        /// Adds skill points to player balance
        /// </summary>
        public void AddSkillPoints(int amount, string description = "")
        {
            _currentSkillPoints = Mathf.Min(_currentSkillPoints + amount, _maxSkillPoints);
            RecordTransaction($"Earned {amount} skill points", amount, description, true);

            ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
        }

        /// <summary>
        /// Consumes resources (electricity, water, nutrients)
        /// </summary>
        public bool ConsumeResource(string resourceType, int amount)
        {
            if (_resourceStocks.ContainsKey(resourceType) && _resourceStocks[resourceType] >= amount)
            {
                _resourceStocks[resourceType] -= amount;
                ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
                return true;
            }
            else
            {
                ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
                return false;
            }
        }

        /// <summary>
        /// Purchases a schematic using skill points
        /// </summary>
        public bool PurchaseSchematic(string schematicName, int skillPointCost, int materialCost)
        {
            if (_currentSkillPoints >= skillPointCost && _currentCurrency >= materialCost)
            {
                _currentSkillPoints -= skillPointCost;
                _currentCurrency -= materialCost;

                RecordTransaction($"Purchased schematic: {schematicName}", -materialCost, "Schematic purchase");
                RecordTransaction($"Spent {skillPointCost} skill points", -skillPointCost, "Schematic purchase", true);

                ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
                return true;
            }
            else
            {
                ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
                return false;
            }
        }

        /// <summary>
        /// Sells a schematic for skill points
        /// </summary>
        public void SellSchematic(string schematicName, int skillPointReward)
        {
            AddSkillPoints(skillPointReward, $"Sold schematic: {schematicName}");
        }

        /// <summary>
        /// Gets current currency balance
        /// </summary>
        public int GetCurrentCurrency()
        {
            return _currentCurrency;
        }

        /// <summary>
        /// Gets current skill points balance
        /// </summary>
        public int GetCurrentSkillPoints()
        {
            return _currentSkillPoints;
        }

        /// <summary>
        /// Gets current stock of a resource
        /// </summary>
        public int GetResourceStock(string resourceType)
        {
            return _resourceStocks.ContainsKey(resourceType) ? _resourceStocks[resourceType] : 0;
        }

        /// <summary>
        /// Gets resource status summary
        /// </summary>
        public Dictionary<string, int> GetResourceStatus()
        {
            return new Dictionary<string, int>(_resourceStocks);
        }

        /// <summary>
        /// Checks if player can afford an item
        /// </summary>
        public bool CanAfford(int cost)
        {
            return _currentCurrency >= cost;
        }

        public bool SpendCash(int amount)
        {
            if (_currentCurrency >= amount)
            {
                _currentCurrency -= amount;
                RecordTransaction($"Spent {amount} currency", -amount, "Cash spend");
                return true;
            }
            return false;
        }

        public bool SpendCurrency(int currencyType, int amount, string reason = "")
        {
            return SpendCash(amount);
        }

        /// <summary>
        /// Checks if player can afford a schematic
        /// </summary>
        public bool CanAffordSchematic(int skillPointCost, int materialCost)
        {
            return _currentSkillPoints >= skillPointCost && _currentCurrency >= materialCost;
        }

        /// <summary>
        /// Gets recent transaction history
        /// </summary>
        public List<TransactionRecord> GetRecentTransactions(int maxRecords = 10)
        {
            int count = Mathf.Min(maxRecords, _recentTransactions.Count);
            return _recentTransactions.GetRange(_recentTransactions.Count - count, count);
        }

        /// <summary>
        /// Records a transaction
        /// </summary>
        private void RecordTransaction(string description, int amount, string details = "", bool isSkillPoints = false)
        {
            var transaction = new TransactionRecord
            {
                Timestamp = DateTime.Now,
                Description = description,
                Amount = amount,
                Details = details,
                IsSkillPoints = isSkillPoints
            };

            _recentTransactions.Add(transaction);

            // Keep only recent transactions (last 50)
            if (_recentTransactions.Count > 50)
            {
                _recentTransactions.RemoveAt(0);
            }
        }

        /// <summary>
        /// Gets economy status summary
        /// </summary>
        public EconomyStatus GetEconomyStatus()
        {
            return new EconomyStatus
            {
                CurrentCurrency = _currentCurrency,
                CurrentSkillPoints = _currentSkillPoints,
                ResourceStocks = new Dictionary<string, int>(_resourceStocks),
                RecentTransactions = GetRecentTransactions(5)
            };
        }

        /// <summary>
        /// Resets economy to starting values (for new game)
        /// </summary>
        public void ResetEconomy()
        {
            InitializeEconomy();
            _recentTransactions.Clear();
            ChimeraLogger.LogInfo("ECONOMY", "Economy system initialized", this);
        }
    }

    // Data structures

    [Serializable]
    public class TransactionRecord
    {
        public DateTime Timestamp;
        public string Description;
        public int Amount;
        public string Details;
        public bool IsSkillPoints;
    }

    [Serializable]
    public class EconomyStatus
    {
        public int CurrentCurrency;
        public int CurrentSkillPoints;
        public Dictionary<string, int> ResourceStocks;
        public List<TransactionRecord> RecentTransactions;
    }
}
