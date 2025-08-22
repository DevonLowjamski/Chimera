using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy.Market
{
    /// <summary>
    /// Simplified market data structures for basic marketplace functionality.
    /// Supports simple buying/selling of Schematics and Genetics at fixed prices.
    /// Simplified for v4.0 scope.
    /// </summary>

    #region Simple Market Data

    /// <summary>
    /// Basic product information for marketplace transactions.
    /// </summary>
    [System.Serializable]
    public class SimpleProductData
    {
        public string ProductId;
        public string ProductName;
        public ProductType ProductType;
        public float BasePrice;
        public bool IsAvailable = true;
        public string Description;
    }

    /// <summary>
    /// Basic transaction record for marketplace sales/purchases.
    /// </summary>
    [System.Serializable]
    public class SimpleTransaction
    {
        public string TransactionId;
        public string ProductId;
        public string ProductName;
        public float Quantity;
        public float UnitPrice;
        public float TotalValue;
        public TransactionType Type;
        public DateTime TransactionDate;
        public bool Success = true;
    }

    /// <summary>
    /// Basic marketplace configuration settings.
    /// </summary>
    [System.Serializable]
    public class MarketplaceConfig
    {
        public bool IsActive = true;
        public float TaxRate = 0.05f; // 5% transaction tax
        public float CommissionRate = 0.02f; // 2% marketplace commission
        public List<ProductType> AllowedProductTypes = new List<ProductType>();
    }

    #endregion

    #region Supporting Enums

    public enum ProductType
    {
        Schematic,
        Genetics,
        Equipment,
        Resource
    }

    public enum TransactionType
    {
        Purchase,
        Sale
    }

    #endregion
}