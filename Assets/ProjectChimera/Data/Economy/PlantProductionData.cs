using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Plant production record for tracking
    /// </summary>
    [System.Serializable]
    public class PlantProductionRecord
    {
        public string PlantId = "";
        public string ContractId = "";
        public StrainType StrainType = StrainType.Indica;
        public int Quantity = 0;
        public QualityGrade Quality = QualityGrade.Standard;
        public DateTime HarvestDate = DateTime.Now;
        public DateTime PlantedDate = DateTime.Now;
        public DateTime AllocationDate = DateTime.Now; // When allocated to contract
        public float EstimatedYield = 0f;
        public float ActualYield = 0f;
        public ProductionStage Stage = ProductionStage.Planted;
        public string BatchId = "";
        public Vector3Int GridPosition = Vector3Int.zero;
        public Dictionary<string, float> QualityMetrics = new Dictionary<string, float>();
        public List<string> ProcessingSteps = new List<string>();
        public bool IsAllocated = false;
        public DateTime LastUpdated = DateTime.Now;
        
        public PlantProductionRecord()
        {
            PlantId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Plant production data for storage and analysis
    /// </summary>
    [System.Serializable]
    public class PlantProductionData
    {
        public string PlantId = ""; // Individual plant identifier
        public StrainType StrainType = StrainType.Indica; // Plant strain type
        public int Quantity = 0; // Quantity produced by this plant
        public QualityGrade Quality = QualityGrade.Standard; // Quality of production
        public List<PlantProductionRecord> ProductionRecords = new List<PlantProductionRecord>();
        public DateTime DataCollectionDate = DateTime.Now;
        public int TotalPlantsTracked = 0;
        public float AverageYield = 0f;
        public QualityGrade AverageQuality = QualityGrade.Standard;
        public Dictionary<StrainType, int> StrainDistribution = new Dictionary<StrainType, int>();
        public Dictionary<ProductionStage, int> StageDistribution = new Dictionary<ProductionStage, int>();
        public ProductionSummary Summary = new ProductionSummary();
    }

    /// <summary>
    /// Production stage enum
    /// </summary>
    public enum ProductionStage
    {
        Planted = 0,
        Germinating = 1,
        Vegetative = 2,
        Flowering = 3,
        ReadyForHarvest = 4,
        Harvested = 5,
        Processing = 6,
        QualityTesting = 7,
        Completed = 8,
        Failed = 9
    }

    /// <summary>
    /// Production summary for analytics
    /// </summary>
    [System.Serializable]
    public class ProductionSummary
    {
        public int TotalPlantsProduced = 0;
        public float TotalYield = 0f;
        public float AverageYieldPerPlant = 0f;
        public QualityGrade AverageQuality = QualityGrade.Standard;
        public Dictionary<StrainType, int> ProductionByStrain = new Dictionary<StrainType, int>();
        public Dictionary<QualityGrade, int> ProductionByQuality = new Dictionary<QualityGrade, int>();
        public float ProductionEfficiency = 0f; // Actual vs Estimated
        public TimeSpan AverageProductionTime = TimeSpan.Zero;
        public int SuccessfulHarvests = 0;
        public int FailedHarvests = 0;
        public DateTime ReportGeneratedDate = DateTime.Now;
        public string ReportPeriod = ""; // e.g., "Q1 2024", "January 2024"
        
        // Additional properties for compatibility
        public StrainType StrainType = StrainType.None; // Primary strain type for this summary
        public int TotalPlants = 0; // Total number of plants
        public int AllocatedPlants = 0; // Plants allocated to contracts
        public int UnallocatedPlants = 0; // Plants not allocated
        public float TotalQuantity = 0f; // Total quantity produced
        public QualityGrade BestQuality = QualityGrade.Standard; // Highest quality achieved
        public int TotalPlantsProcessed = 0; // Alias for TotalPlantsProduced
        public int TotalPlantsTracked = 0; // Total plants being tracked
        public float WorstQuality = 0f; // Lowest quality as float
        public string BestStrain = ""; // Best performing strain name
    }

    /// <summary>
    /// Production statistics for analytics
    /// </summary>
    [System.Serializable]
    public class ProductionStatistics
    {
        public int TotalPlantsProduced = 0;
        public float TotalYieldKg = 0f;
        public float AverageYieldPerPlant = 0f;
        public float ProductionEfficiency = 0f;
        public QualityGrade AverageQuality = QualityGrade.Standard;
        public Dictionary<StrainType, ProductionSummary> ProductionByStrain = new Dictionary<StrainType, ProductionSummary>();
        public Dictionary<QualityGrade, int> QualityDistribution = new Dictionary<QualityGrade, int>();
        public TimeSpan AverageGrowthCycle = TimeSpan.Zero;
        public int SuccessfulHarvests = 0;
        public int FailedHarvests = 0;
        public float WastePercentage = 0f;
        public DateTime StatsPeriodStart = DateTime.Now;
        public DateTime StatsPeriodEnd = DateTime.Now;
        public int TotalPlantsProcessed = 0; // Total plants processed
        public int TotalPlantsTracked = 0; // Total plants being tracked
        public int UnallocatedPlants = 0; // Plants not allocated to contracts
        public int AllocatedPlants = 0; // Plants allocated to contracts
        public float TotalQuantityProduced = 0f; // Total quantity produced
        public QualityGrade BestQuality = QualityGrade.Premium; // Best quality achieved
        public QualityGrade WorstQuality = QualityGrade.Poor; // Worst quality achieved
        public List<StrainType> ActiveStrainTypes = new List<StrainType>(); // Active strain types
        public int ContractsWithProduction = 0; // Number of contracts with production data
    }
}