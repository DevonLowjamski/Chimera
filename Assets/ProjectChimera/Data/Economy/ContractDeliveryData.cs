using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Contract delivery information
    /// </summary>
    [System.Serializable]
    public class ContractDelivery
    {
        public string DeliveryId = "";
        public string ContractId = "";
        public string ClientId = "";
        public DateTime DeliveryDate = DateTime.Now;
        public DateTime ScheduledDelivery = DateTime.Now;
        public List<DeliveryItem> Items = new List<DeliveryItem>();
        public DeliveryStatus Status = DeliveryStatus.Pending;
        public float TotalValue = 0f;
        public QualityGrade OverallQuality = QualityGrade.Standard;
        public string DeliveryNotes = "";
        public Vector3 DeliveryLocation = Vector3.zero;
        public string DeliveryMethod = "Standard";
        public bool RequiresSignature = false;
        public string TrackingNumber = "";
        public ActiveContractSO Contract = null; // Reference to the contract
        public float DeliveredQuantity = 0f; // Actual delivered quantity
        public QualityGrade AverageQuality = QualityGrade.Standard; // Average quality of delivered items
        public bool IsCompleted = false; // Whether delivery is completed
        public List<PlantProductionRecord> PlantRecords = new List<PlantProductionRecord>(); // Records of plants in this delivery
        public DateTime CompletionDate = DateTime.Now; // When delivery was completed
        public string FailureReason = ""; // Reason for delivery failure if applicable
        public float BaseValue = 0f; // Base value of the delivery
        public float QualityBonus = 0f; // Quality-based bonus amount
        public float QuantityBonus = 0f; // Quantity-based bonus amount
        public float TimelinessBonus = 0f; // Timeliness-based bonus amount
        
        public ContractDelivery()
        {
            DeliveryId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Individual item in a delivery
    /// </summary>
    [System.Serializable]
    public class DeliveryItem
    {
        public string ItemId = "";
        public string ProductName = "";
        public StrainType StrainType = StrainType.Indica;
        public int Quantity = 0;
        public float UnitPrice = 0f;
        public QualityGrade Quality = QualityGrade.Standard;
        public string BatchId = "";
        public DateTime HarvestDate = DateTime.Now;
        public Dictionary<string, object> ItemMetadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Delivery status enum
    /// </summary>
    public enum DeliveryStatus
    {
        Pending = 0,
        InTransit = 1,
        Delivered = 2,
        Failed = 3,
        Cancelled = 4,
        Returned = 5
    }

    /// <summary>
    /// Delivery statistics for analytics
    /// </summary>
    [System.Serializable]
    public class DeliveryStatistics
    {
        public int TotalDeliveries = 0;
        public int SuccessfulDeliveries = 0;
        public int FailedDeliveries = 0;
        public int CancelledDeliveries = 0;
        public int CompletedDeliveries = 0; // Number of completed deliveries
        public int PendingDeliveries = 0; // Number of pending deliveries
        public float SuccessRate = 0f; // 0.0 to 1.0
        public TimeSpan AverageDeliveryTime = TimeSpan.Zero;
        public float AverageProcessingTimeMinutes = 0f; // Average processing time in minutes
        public float TotalValueDelivered = 0f;
        public float TotalDeliveryValue = 0f; // Alias for TotalValueDelivered
        public float AverageDeliveryValue = 0f; // Average value per delivery
        public QualityGrade AverageDeliveryQuality = QualityGrade.Standard; // Average quality across deliveries
        public Dictionary<DeliveryStatus, int> DeliveriesByStatus = new Dictionary<DeliveryStatus, int>();
        public Dictionary<QualityGrade, int> DeliveriesByQuality = new Dictionary<QualityGrade, int>();
        public Dictionary<string, int> DeliveriesByClient = new Dictionary<string, int>();
        public List<string> TopPerformingClients = new List<string>();
        public DateTime StatsPeriodStart = DateTime.Now;
        public DateTime StatsPeriodEnd = DateTime.Now;
        public DateTime LastUpdated = DateTime.Now;
    }
}