using System;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Contract progress tracking
    /// </summary>
    [System.Serializable]
    public class ContractProgress
    {
        public string ContractId = "";
        public float OverallProgress = 0f; // 0.0 to 1.0
        public float QuantityProgress = 0f;
        public float QualityProgress = 0f;
        public float TimeProgress = 0f;
        public int RequiredQuantity = 0;
        public int ProducedQuantity = 0;
        public int CurrentQuantity = 0; // Current available quantity
        public int DeliveredQuantity = 0;
        public QualityGrade RequiredQuality = QualityGrade.Standard;
        public QualityGrade AchievedQuality = QualityGrade.Standard;
        public QualityGrade AverageQuality = QualityGrade.Standard; // Average quality across all production
        public DateTime StartDate = DateTime.Now;
        public DateTime StartTime = DateTime.Now; // Alias for StartDate
        public DateTime DueDate = DateTime.Now;
        public DateTime LastUpdated = DateTime.Now;
        public float CompletionProgress = 0f; // 0.0 to 1.0 completion percentage
        public int QualifiedPlants = 0; // Number of plants that meet quality standards
        public ContractProgressStatus Status = ContractProgressStatus.InProgress;
        public List<string> AllocatedPlantIds = new List<string>();
        public Dictionary<string, float> Milestones = new Dictionary<string, float>();
        public string Notes = "";
        public bool IsReadyForDelivery = false; // Whether contract is ready for delivery
        public ActiveContractSO Contract = null; // Contract reference (optional, use ContractId for data consistency)
    }

    /// <summary>
    /// Contract progress status
    /// </summary>
    public enum ContractProgressStatus
    {
        NotStarted = 0,
        InProgress = 1,
        OnTrack = 2,
        AtRisk = 3,
        Delayed = 4,
        Completed = 5,
        Failed = 6,
        Cancelled = 7
    }
}