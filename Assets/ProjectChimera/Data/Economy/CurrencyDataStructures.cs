using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Simple economy system aligned with Project Chimera vision
    /// Basic currency for buying equipment and selling harvest
    /// </summary>

    [System.Serializable]
    public enum CurrencyType
    {
        Cash,           // In-game currency for equipment/materials
        SkillPoints     // For genetics marketplace and skill progression
    }

    [System.Serializable]
    public enum TransactionType
    {
        EquipmentPurchase,
        MaterialPurchase,
        HarvestSale,
        UtilityPayment,
        GeneticsPurchase,
        SchematicPurchase
    }

    [System.Serializable]
    public class SimpleTransaction
    {
        public string TransactionId;
        public DateTime Timestamp;
        public CurrencyType CurrencyType;
        public TransactionType TransactionType;
        public float Amount;
        public string Description;

        public SimpleTransaction()
        {
            TransactionId = System.Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
        }
    }

    [System.Serializable]
    public class EconomyData
    {
        public float PlayerMoney = 10000f;
        public int SkillPoints = 0;
        public float Reputation = 0.5f; // 0.0 to 1.0, affects sale prices
        public List<SimpleTransaction> TransactionHistory = new List<SimpleTransaction>();
    }
}
