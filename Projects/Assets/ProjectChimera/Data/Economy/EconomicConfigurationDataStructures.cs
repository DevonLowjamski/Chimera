using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy.Configuration
{
    /// <summary>
    /// Simplified economic configuration data structures for basic marketplace functionality.
    /// Contains only essential configurations for simple buying/selling operations.
    /// Simplified for v4.0 scope.
    /// </summary>

    #region Basic Economic Configuration

    /// <summary>
    /// Simple marketplace configuration and settings.
    /// </summary>
    [System.Serializable]
    public class SimpleMarketplaceConfig
    {
        public bool IsActive = true;
        public string MarketplaceName = "Project Chimera Marketplace";
        public float DefaultTaxRate = 0.05f; // 5% tax
        public float DefaultCommissionRate = 0.02f; // 2% commission
        public int MaxTransactionsPerDay = 100;
        public DateTime LastConfigUpdate;
        
        public void Initialize()
        {
            LastConfigUpdate = DateTime.Now;
        }
    }

    /// <summary>
    /// Basic price management for fixed marketplace prices.
    /// </summary>
    [System.Serializable]
    public class SimplePriceManager
    {
        public bool IsActive = true;
        public Dictionary<string, float> FixedPrices = new Dictionary<string, float>();
        public float PriceAdjustmentMultiplier = 1.0f; // Admin can adjust all prices by this multiplier
        public DateTime LastPriceUpdate;
        
        public void Initialize()
        {
            SetupDefaultPrices();
            LastPriceUpdate = DateTime.Now;
        }
        
        private void SetupDefaultPrices()
        {
            // Default prices for Schematics and Genetics
            FixedPrices["basic_schematic"] = 100f;
            FixedPrices["advanced_schematic"] = 250f;
            FixedPrices["basic_genetics"] = 150f;
            FixedPrices["premium_genetics"] = 300f;
        }
        
        public float GetPrice(string productId)
        {
            if (FixedPrices.ContainsKey(productId))
            {
                return FixedPrices[productId] * PriceAdjustmentMultiplier;
            }
            return 0f;
        }
        
        public void SetPrice(string productId, float price)
        {
            FixedPrices[productId] = price;
            LastPriceUpdate = DateTime.Now;
        }
    }

    /// <summary>
    /// Simple transaction logging for marketplace operations.
    /// </summary>
    [System.Serializable]
    public class SimpleTransactionLogger
    {
        public bool IsActive = true;
        public List<SimpleTransactionRecord> TransactionHistory = new List<SimpleTransactionRecord>();
        public int MaxHistorySize = 1000;
        public DateTime LastLogCleanup;
        
        public void Initialize()
        {
            LastLogCleanup = DateTime.Now;
        }
        
        public void LogTransaction(SimpleTransactionRecord transaction)
        {
            TransactionHistory.Add(transaction);
            
            // Clean up old records if we exceed max size
            if (TransactionHistory.Count > MaxHistorySize)
            {
                TransactionHistory.RemoveAt(0);
            }
        }
        
        public List<SimpleTransactionRecord> GetRecentTransactions(int days = 7)
        {
            DateTime cutoff = DateTime.Now.AddDays(-days);
            return TransactionHistory.FindAll(t => t.TransactionDate >= cutoff);
        }
    }

    #endregion

    #region Supporting Data Structures

    /// <summary>
    /// Basic transaction record for logging marketplace activity.
    /// </summary>
    [System.Serializable]
    public class SimpleTransactionRecord
    {
        public string TransactionId;
        public string ProductId;
        public string BuyerId;
        public string SellerId;
        public float Quantity;
        public float UnitPrice;
        public float TotalValue;
        public float TaxAmount;
        public float CommissionAmount;
        public TransactionStatus Status;
        public DateTime TransactionDate;
        public string Notes;
    }

    /// <summary>
    /// Basic user profile for marketplace participants.
    /// </summary>
    [System.Serializable]
    public class SimpleUserProfile
    {
        public string UserId;
        public string UserName;
        public float CashBalance;
        public int TransactionCount;
        public float TotalSpent;
        public float TotalEarned;
        public DateTime LastActivity;
        public bool IsActive = true;
    }

    #endregion

    #region Supporting Enums

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    #endregion
}