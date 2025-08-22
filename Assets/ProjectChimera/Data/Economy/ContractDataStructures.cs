using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Quality grade enum for product evaluation
    /// </summary>
    public enum QualityGrade
    {
        Poor = -1,
        BelowStandard = 0,
        Acceptable = 1,
        Standard = 2,
        Good = 3,
        Excellent = 4,
        Premium = 5
    }

    /// <summary>
    /// Extension class for QualityGrade operations
    /// </summary>
    public static class QualityGradeExtensions
    {
        /// <summary>
        /// Convert QualityGrade to float value (-0.2 to 1.0)
        /// </summary>
        public static float ToFloat(this QualityGrade grade)
        {
            switch (grade)
            {
                case QualityGrade.Poor:         return -0.2f;
                case QualityGrade.BelowStandard: return 0.1f;
                case QualityGrade.Acceptable:   return 0.3f;
                case QualityGrade.Standard:     return 0.5f;
                case QualityGrade.Good:         return 0.7f;
                case QualityGrade.Excellent:    return 0.85f;
                case QualityGrade.Premium:      return 0.95f;
                default:                        return 0.5f;
            }
        }

        /// <summary>
        /// Convert QualityGrade to int value
        /// </summary>
        public static int ToInt(this QualityGrade grade)
        {
            return (int)grade;
        }

        /// <summary>
        /// Convert float value to QualityGrade
        /// </summary>
        public static QualityGrade FromFloat(float value)
        {
            if (value < 0.0f) return QualityGrade.Poor;
            if (value <= 0.2f) return QualityGrade.BelowStandard;
            if (value <= 0.4f) return QualityGrade.Acceptable;
            if (value <= 0.6f) return QualityGrade.Standard;
            if (value <= 0.8f) return QualityGrade.Good;
            if (value <= 0.9f) return QualityGrade.Excellent;
            return QualityGrade.Premium;
        }

        /// <summary>
        /// Convert int value to QualityGrade
        /// </summary>
        public static QualityGrade FromInt(int value)
        {
            return (QualityGrade)Mathf.Clamp(value, -1, 5);
        }

        /// <summary>
        /// Compare QualityGrade with float
        /// </summary>
        public static bool IsGreaterThan(this QualityGrade grade, float value)
        {
            return grade.ToFloat() > value;
        }

        /// <summary>
        /// Compare QualityGrade with float
        /// </summary>
        public static bool IsLessThan(this QualityGrade grade, float value)
        {
            return grade.ToFloat() < value;
        }

        /// <summary>
        /// Safe comparison with float using tolerance
        /// </summary>
        public static bool Equals(this QualityGrade grade, float value, float tolerance = 0.01f)
        {
            return Mathf.Abs(grade.ToFloat() - value) < tolerance;
        }

        /// <summary>
        /// Convert float to int with rounding
        /// </summary>
        public static int FloatToInt(float value)
        {
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// Safe float to int conversion for quantity calculations
        /// </summary>
        public static int ToQuantity(float value)
        {
            return Mathf.Max(0, Mathf.RoundToInt(value));
        }

        /// <summary>
        /// Convert QualityGrade to long for database operations
        /// </summary>
        public static long ToLong(this QualityGrade grade)
        {
            return (long)(int)grade;
        }

        /// <summary>
        /// Convert long to QualityGrade
        /// </summary>
        public static QualityGrade FromLong(long value)
        {
            return (QualityGrade)Mathf.Clamp((int)value, 0, 5);
        }

        /// <summary>
        /// Comparison methods - use these instead of < > operators since enums already have natural comparison
        /// </summary>
        public static bool IsLowerThan(this QualityGrade left, QualityGrade right)
        {
            return (int)left < (int)right;
        }

        public static bool IsHigherThan(this QualityGrade left, QualityGrade right)
        {
            return (int)left > (int)right;
        }

        public static bool IsLowerOrEqual(this QualityGrade left, QualityGrade right)
        {
            return (int)left <= (int)right;
        }

        public static bool IsHigherOrEqual(this QualityGrade left, QualityGrade right)
        {
            return (int)left >= (int)right;
        }

        /// <summary>
        /// Alias for IsHigherOrEqual for consistency
        /// </summary>
        public static bool IsGreaterThanOrEqual(this QualityGrade left, QualityGrade right)
        {
            return (int)left >= (int)right;
        }

        /// <summary>
        /// Helper methods for common quality operations
        /// </summary>
        public static QualityGrade Min(QualityGrade a, QualityGrade b)
        {
            return (int)a <= (int)b ? a : b;
        }

        public static QualityGrade Max(QualityGrade a, QualityGrade b)
        {
            return (int)a >= (int)b ? a : b;
        }

        /// <summary>
        /// Calculate average quality grade from a collection
        /// </summary>
        public static QualityGrade Average(IEnumerable<QualityGrade> grades)
        {
            if (!grades.Any()) return QualityGrade.Standard;
            
            float average = grades.Select(g => g.ToFloat()).Average();
            return FromFloat(average);
        }

        /// <summary>
        /// Additional conversion helper methods
        /// </summary>
        public static int ConvertToInt(this QualityGrade grade)
        {
            return (int)grade;
        }

        public static float ConvertToFloat(this QualityGrade grade)
        {
            return grade.ToFloat();
        }

        public static long ConvertToLong(this QualityGrade grade)
        {
            return grade.ToLong();
        }

        /// <summary>
        /// Safe conversion for nullable DateTime
        /// </summary>
        public static DateTime? ToNullableDateTime(DateTime? dateTime)
        {
            return dateTime;
        }

        public static DateTime GetValueOrNow(DateTime? dateTime)
        {
            return dateTime ?? DateTime.Now;
        }

        /// <summary>
        /// Extension method to check if DateTime has a meaningful value
        /// </summary>
        /// <param name="dateTime">DateTime to check</param>
        /// <returns>True if datetime is not MinValue or default</returns>
        public static bool HasValue(this DateTime dateTime)
        {
            return dateTime != DateTime.MinValue && dateTime != default(DateTime);
        }

        /// <summary>
        /// Convert List of QualityGrade to List of float
        /// </summary>
        public static List<float> ToFloatList(this List<QualityGrade> grades)
        {
            var result = new List<float>();
            foreach (var grade in grades)
            {
                result.Add(grade.ToFloat());
            }
            return result;
        }

        /// <summary>
        /// Convert List of float to List of QualityGrade
        /// </summary>
        public static List<QualityGrade> ToQualityGradeList(this List<float> values)
        {
            var result = new List<QualityGrade>();
            foreach (var value in values)
            {
                result.Add(FromFloat(value));
            }
            return result;
        }

        /// <summary>
        /// Calculate standard deviation of quality grades
        /// </summary>
        public static float CalculateStandardDeviation(this List<QualityGrade> grades)
        {
            if (grades == null || grades.Count <= 1) return 0f;
            
            var floatValues = grades.ToFloatList();
            float average = floatValues.Sum() / floatValues.Count;
            float sumOfSquares = floatValues.Sum(x => (x - average) * (x - average));
            return UnityEngine.Mathf.Sqrt(sumOfSquares / (floatValues.Count - 1));
        }

        /// <summary>
        /// Convert List of QualityDataPoint to List of QualityGrade
        /// </summary>
        public static List<QualityGrade> ToQualityGradeList(this List<QualityDataPoint> dataPoints)
        {
            var result = new List<QualityGrade>();
            if (dataPoints != null)
            {
                foreach (var point in dataPoints)
                {
                    result.Add(point.Quality);
                }
            }
            return result;
        }

        /// <summary>
        /// Convert List of QualityDataPoint to List of float values
        /// </summary>
        public static List<float> ToFloatValueList(this List<QualityDataPoint> dataPoints)
        {
            var result = new List<float>();
            if (dataPoints != null)
            {
                foreach (var point in dataPoints)
                {
                    result.Add(point.QualityValue);
                }
            }
            return result;
        }

        /// <summary>
        /// Safe DateTime conversion from nullable
        /// </summary>
        public static DateTime ToDateTime(this DateTime? nullableDateTime)
        {
            return nullableDateTime ?? DateTime.Now;
        }

        /// <summary>
        /// Safe DateTime conversion with default value
        /// </summary>
        public static DateTime ToDateTimeOrDefault(this DateTime? nullableDateTime, DateTime defaultValue)
        {
            return nullableDateTime ?? defaultValue;
        }

        /// <summary>
        /// Safe List<string> count conversion to int
        /// </summary>
        public static int ToCount(this List<string> list)
        {
            return list?.Count ?? 0;
        }

        /// <summary>
        /// Safe List<T> count conversion to int
        /// </summary>
        public static int ToCount<T>(this List<T> list)
        {
            return list?.Count ?? 0;
        }

        /// <summary>
        /// Explicit conversion from QualityGrade to float (for method parameters)
        /// </summary>
        public static float AsFloat(this QualityGrade grade)
        {
            return grade.ToFloat();
        }

        /// <summary>
        /// Explicit conversion from float to QualityGrade (for assignments)
        /// </summary>
        public static QualityGrade AsQualityGrade(this float value)
        {
            return FromFloat(value);
        }

        /// <summary>
        /// Safe float to int conversion with rounding
        /// </summary>
        public static int ToIntSafe(this float value)
        {
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// Safe float to int conversion with floor
        /// </summary>
        public static int ToIntFloor(this float value)
        {
            return Mathf.FloorToInt(value);
        }

        /// <summary>
        /// Safe float to int conversion with ceiling  
        /// </summary>
        public static int ToIntCeil(this float value)
        {
            return Mathf.CeilToInt(value);
        }

        /// <summary>
        /// Less than comparison for QualityGrade vs float
        /// </summary>
        public static bool IsLessThanFloat(this QualityGrade grade, float value)
        {
            return grade.ToFloat() < value;
        }

        /// <summary>
        /// Greater than comparison for QualityGrade vs float
        /// </summary>
        public static bool IsGreaterThanFloat(this QualityGrade grade, float value)
        {
            return grade.ToFloat() > value;
        }

        /// <summary>
        /// Less than or equal comparison for QualityGrade vs float
        /// </summary>
        public static bool IsLessThanOrEqualFloat(this QualityGrade grade, float value)
        {
            return grade.ToFloat() <= value;
        }

        /// <summary>
        /// Greater than or equal comparison for QualityGrade vs float
        /// </summary>
        public static bool IsGreaterThanOrEqualFloat(this QualityGrade grade, float value)
        {
            return grade.ToFloat() >= value;
        }

        /// <summary>
        /// Equality comparison for QualityGrade vs float with tolerance
        /// </summary>
        public static bool IsEqualToFloat(this QualityGrade grade, float value, float tolerance = 0.01f)
        {
            return Mathf.Abs(grade.ToFloat() - value) <= tolerance;
        }

        /// <summary>
        /// Force explicit conversion from QualityGrade to float for problematic contexts
        /// </summary>
        public static float ForceFloat(this QualityGrade grade)
        {
            return (float)grade.ToFloat();
        }

        /// <summary>
        /// Force explicit conversion from float to QualityGrade for problematic contexts
        /// </summary>
        public static QualityGrade ForceQualityGrade(this float value)
        {
            return (QualityGrade)FromFloat(value);
        }

        /// <summary>
        /// Safe explicit conversion from float to int with validation
        /// </summary>
        public static int ToIntExplicit(this float value)
        {
            return (int)Mathf.Round(value);
        }

        /// <summary>
        /// Create QualityGrade from float with explicit cast
        /// </summary>
        public static QualityGrade CastToQualityGrade(float value)
        {
            return FromFloat(value);
        }

        /// <summary>
        /// Create float from QualityGrade with explicit cast
        /// </summary>
        public static float CastToFloat(QualityGrade grade)
        {
            return grade.ToFloat();
        }



        /// <summary>
        /// Helper methods for common conversion scenarios
        /// </summary>
        public static int ConvertFloatToInt(float value)
        {
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// Find the best (highest) quality in a list
        /// </summary>
        public static QualityGrade BestQuality(this List<QualityGrade> qualities)
        {
            return qualities.Count > 0 ? qualities.Max() : QualityGrade.Poor;
        }

        /// <summary>
        /// Find the best quality from production data
        /// </summary>
        public static QualityGrade BestQuality(this List<PlantProductionRecord> production)
        {
            return production.Count > 0 ? production.Max(p => p.Quality) : QualityGrade.Poor;
        }

        /// <summary>
        /// Safe conversion with explicit float to int casting
        /// </summary>
        public static int SafeCastToInt(float value)
        {
            return (int)Mathf.Round(value);
        }

        /// <summary>
        /// Safe conversion with explicit QualityGrade to float casting
        /// </summary>
        public static float SafeCastToFloat(QualityGrade grade)
        {
            return (float)grade.ToFloat();
        }

        /// <summary>
        /// Force explicit int conversion for List.Count operations
        /// </summary>
        public static int ForceInt(this int value)
        {
            return value;
        }

        public static QualityGrade ConvertFloatToQuality(float value)
        {
            return FromFloat(value);
        }

        public static float ConvertQualityToFloat(QualityGrade grade)
        {
            return grade.ToFloat();
        }

        public static long ConvertQualityToLong(QualityGrade grade)
        {
            return grade.ToLong();
        }
    }

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

    /// <summary>
    /// Contract notification data
    /// </summary>
    [System.Serializable]
    public class ContractNotificationData
    {
        public string NotificationId = "";
        public string ContractId = "";
        public ContractNotificationType Type = ContractNotificationType.Progress;
        public ContractNotificationSeverity Severity = ContractNotificationSeverity.Info;
        public string Title = "";
        public string Message = "";
        public DateTime Timestamp = DateTime.Now;
        public bool IsRead = false;
        public Dictionary<string, object> Data = new Dictionary<string, object>();
        public string ActionUrl = "";
        
        public ContractNotificationData()
        {
            NotificationId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Contract notification types
    /// </summary>
    public enum ContractNotificationType
    {
        Progress = 0,
        Deadline = 1,
        Quality = 2,
        Completion = 3,
        Delivery = 4,
        Payment = 5,
        Alert = 6,
        Warning = 7,
        Error = 8
    }

    /// <summary>
    /// Contract notification severity levels
    /// </summary>
    public enum ContractNotificationSeverity
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    /// <summary>
    /// Contract tracking metrics for performance analysis
    /// </summary>
    [System.Serializable]
    public class ContractTrackingMetrics
    {
        public string ContractId = "";
        public float CompletionRate = 0f;
        public float QualityScore = 0f;
        public TimeSpan AverageCompletionTime = TimeSpan.Zero;
        public int TotalContracts = 0;
        public int CompletedContracts = 0;
        public int FailedContracts = 0;
        public int TotalContractsCompleted = 0; // Alias for CompletedContracts
        public float RevenueGenerated = 0f;
        public float TotalContractValue = 0f; // Alias for RevenueGenerated
        public int TotalPlantsProcessed = 0; // Total number of plants processed
        public float TotalQuantityProduced = 0f; // Total quantity produced in kg
        public QualityGrade AverageQuality = QualityGrade.Standard; // Average quality across all contracts
        public QualityGrade AverageDeliveryQuality = QualityGrade.Standard; // Average quality of deliveries
        public int TotalContractsFailed = 0; // Total number of failed contracts
        public int TotalDeliveriesProcessed = 0; // Total number of deliveries processed
        public float ContractSuccessRate = 0f; // Success rate as percentage (0.0 to 1.0)
        public Dictionary<string, float> PerformanceIndicators = new Dictionary<string, float>();
        public DateTime LastUpdated = DateTime.Now;
    }

    /// <summary>
    /// Contract completion validation result
    /// </summary>
    [System.Serializable]
    public class ContractCompletionValidation
    {
        public string ContractId = "";
        public bool IsValid = false;
        public bool QuantityMet = false;
        public bool QualityMet = false;
        public bool DeadlineMet = false;
        public List<string> ValidationErrors = new List<string>();
        public List<string> ValidationWarnings = new List<string>();
        public QualityGrade AchievedQuality = QualityGrade.Standard;
        public float CompletionPercentage = 0f;
        public DateTime ValidationDate = DateTime.Now;
        public string Reason = ""; // Reason for validation result
    }

    /// <summary>
    /// Contract analytics report
    /// </summary>
    [System.Serializable]
    public class ContractAnalyticsReport
    {
        public string ReportId = "";
        public DateTime ReportDate = DateTime.Now;
        public DateTime GeneratedDate = DateTime.Now; // Alias for ReportDate
        public string ReportPeriod = "";
        public List<ContractTrackingMetrics> ContractMetrics = new List<ContractTrackingMetrics>();
        public ProductionStatistics ProductionStats = new ProductionStatistics();
        public ContractPerformanceMetrics PerformanceMetrics = new ContractPerformanceMetrics();
        public ContractTrackingMetrics OverallMetrics = new ContractTrackingMetrics(); // Overall metrics summary
        public List<ContractPerformanceTrends> Trends = new List<ContractPerformanceTrends>();
        public Dictionary<string, QualityAssessmentProfile> QualityProfiles = new Dictionary<string, QualityAssessmentProfile>();
        public Dictionary<string, ContractPerformanceMetrics> ContractPerformance = new Dictionary<string, ContractPerformanceMetrics>();
        public List<string> TopPerformingStrains = new List<string>();
        public List<QualityTrendData> QualityTrends = new List<QualityTrendData>();
        public List<string> PerformanceRecommendations = new List<string>();
        public Dictionary<string, object> CustomAnalytics = new Dictionary<string, object>();
        
        public ContractAnalyticsReport()
        {
            ReportId = Guid.NewGuid().ToString();
        }
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

    /// <summary>
    /// Contract performance metrics
    /// </summary>
    [System.Serializable]
    public class ContractPerformanceMetrics
    {
        public string ContractId = ""; // Contract identifier
        public DateTime StartTime = DateTime.Now; // When metrics tracking started
        public int TotalPlantsUsed = 0; // Total plants used for this contract
        public float OverallPerformanceScore = 0f;
        public float OnTimeDeliveryRate = 0f;
        public float QualityComplianceRate = 0f;
        public float CustomerSatisfactionScore = 0f;
        public float ProfitMargin = 0f;
        public float ContractFulfillmentRate = 0f;
        public TimeSpan AverageDeliveryTime = TimeSpan.Zero;
        public TimeSpan AverageCompletionTime = TimeSpan.Zero; // Average time to complete contracts
        public Dictionary<string, float> ClientPerformanceScores = new Dictionary<string, float>();
        public Dictionary<string, int> ContractTypePerformance = new Dictionary<string, int>();
        public List<string> TopPerformingContracts = new List<string>();
        public List<string> UnderperformingContracts = new List<string>();
        public QualityGrade AverageQuality = QualityGrade.Standard; // Average quality across all contracts
        public DateTime MetricsDate = DateTime.Now;
        public float OverallScore { get => OverallPerformanceScore; set => OverallPerformanceScore = value; } // Alias for overall performance score
    }

    /// <summary>
    /// Contract performance trends over time
    /// </summary>
    [System.Serializable]
    public class ContractPerformanceTrends
    {
        public string TrendType = ""; // e.g., "Quality", "Delivery", "Revenue"
        public List<float> TrendData = new List<float>();
        public List<DateTime> TrendDates = new List<DateTime>();
        public float TrendDirection = 0f; // Positive = improving, Negative = declining
        public float TrendStrength = 0f; // 0.0 to 1.0
        public string TrendDescription = "";
        public DateTime AnalysisDate = DateTime.Now;
        public int TotalContractsAnalyzed = 0;
        public float AverageCompletionTime = 0f; // Average time to complete contracts
        public QualityGrade AverageQualityScore = QualityGrade.Standard; // Average quality across trends
        public Dictionary<string, object> TrendMetadata = new Dictionary<string, object>();
        public ContractPerformanceMetrics BestPerformingContract = new ContractPerformanceMetrics(); // Best performing contract
        public ContractPerformanceMetrics WorstPerformingContract = new ContractPerformanceMetrics(); // Worst performing contract
        public float OnTimeDeliveryRate = 0f; // On-time delivery percentage
    }

    /// <summary>
    /// Advanced contract validator for complex validation logic
    /// </summary>
    [System.Serializable]
    public class AdvancedContractValidator
    {
        public string ValidatorId = "";
        public string ValidatorName = "";
        public List<string> ValidationRules = new List<string>();
        public Dictionary<string, object> ValidationParameters = new Dictionary<string, object>();
        public bool IsEnabled = true;
        public int Priority = 0;
        public string ValidatorType = ""; // e.g., "Quality", "Quantity", "Timing"
    }

    /// <summary>
    /// Quality assessment result
    /// </summary>
    [System.Serializable]
    public class QualityAssessmentResult
    {
        public string AssessmentId = "";
        public string ContractId = "";
        public string PlantId = "";
        public QualityGrade OverallGrade = QualityGrade.Standard;
        public QualityGrade QualityGrade = QualityGrade.Standard; // Alias for OverallGrade
        public bool IsValid = false; // Whether the assessment is valid
        public string ErrorMessage = ""; // Error message if assessment failed
        public string QualityAssessment = ""; // Text description of quality assessment
        public bool MeetsQualityStandards = false; // Whether quality meets standards
        public bool MeetsRequirements = false; // Whether assessment meets all requirements
        public Dictionary<string, float> QualityMetrics = new Dictionary<string, float>();
        public List<string> QualityNotes = new List<string>();
        public bool PassesMinimumStandards = false;
        public QualityConsistency QualityConsistency = new QualityConsistency(); // Quality consistency data
        public QualityGrade OverallQuality = QualityGrade.Standard; // Alias for OverallGrade
        public DateTime AssessmentDate = DateTime.Now;
        public string AssessorId = "";
        public QualityAssessmentProfile Profile = new QualityAssessmentProfile();
        
        public QualityAssessmentResult()
        {
            AssessmentId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Quality validation result
    /// </summary>
    [System.Serializable]
    public class QualityValidationResult
    {
        public bool IsValid = false;
        public QualityGrade ValidationGrade = QualityGrade.Standard;
        public List<string> ValidationErrors = new List<string>();
        public List<string> ValidationWarnings = new List<string>();
        public Dictionary<string, bool> ValidationChecks = new Dictionary<string, bool>();
        public float ValidationScore = 0f;
        public DateTime ValidationTimestamp = DateTime.Now;
        public string ValidatorInfo = "";
        
        // Alias properties for compatibility
        public string ContractId = "";
        public DateTime ValidationDate { get => ValidationTimestamp; set => ValidationTimestamp = value; }
        public QualityGrade OverallQuality { get => ValidationGrade; set => ValidationGrade = value; }
        public QualityGrade QualityGrade { get => ValidationGrade; set => ValidationGrade = value; }
        public bool MeetsRequirements { get => IsValid; set => IsValid = value; }
        public List<string> Issues { get => ValidationErrors; set => ValidationErrors = value; }
    }

    /// <summary>
    /// Quality assessment profile
    /// </summary>
    [System.Serializable]
    public class QualityAssessmentProfile
    {
        public string ProfileId = "";
        public string ProfileName = "";
        public Dictionary<string, float> QualityThresholds = new Dictionary<string, float>();
        public List<string> RequiredMetrics = new List<string>();
        public QualityGrade MinimumGrade = QualityGrade.Acceptable;
        public bool RequiresManualReview = false;
        public List<string> AutomatedChecks = new List<string>();
        public StrainType StrainType = StrainType.None; // Target strain type for this profile
        public int SampleCount = 0; // Number of samples analyzed
        public QualityGrade AverageQuality = QualityGrade.Standard; // Average quality from samples
        public List<QualityDataPoint> QualityHistory = new List<QualityDataPoint>(); // Historical quality data
    }

    /// <summary>
    /// Quality consistency tracking
    /// </summary>
    [System.Serializable]
    public class QualityConsistency
    {
        public string ConsistencyId = "";
        public float ConsistencyScore = 0f; // 0.0 to 1.0
        public float QualityVariance = 0f;
        public float Variance = 0f; // Alias for QualityVariance
        public float StandardDeviation = 0f; // Standard deviation of quality scores
        public List<QualityGrade> RecentGrades = new List<QualityGrade>();
        public Dictionary<string, float> MetricConsistency = new Dictionary<string, float>();
        public bool IsConsistent = false;
        public string ConsistencyTrend = ""; // "Improving", "Stable", "Declining"
        public DateTime LastUpdated = DateTime.Now;
    }

    /// <summary>
    /// Contract specific validation check
    /// </summary>
    [System.Serializable]
    public class ContractSpecificCheck
    {
        public string CheckId = "";
        public string CheckName = "";
        public string CheckType = ""; // e.g., "Quality", "Quantity", "Timing", "Custom"
        public bool IsRequired = true;
        public Dictionary<string, object> CheckParameters = new Dictionary<string, object>();
        public bool CheckPassed = false;
        public bool Passed = false; // Alias for CheckPassed
        public string CheckResult = "";
        public DateTime CheckDate = DateTime.Now;
        public string CheckDetails = "";
        public string Details { get => CheckDetails; set => CheckDetails = value; } // Alias for CheckDetails
    }

    /// <summary>
    /// Advanced contract validation for complex business logic
    /// </summary>
    [System.Serializable]
    public class AdvancedContractValidation
    {
        public string ValidationId = "";
        public string ContractId = "";
        public bool IsValid = false;
        public List<string> ValidationErrors = new List<string>();
        public List<string> ValidationWarnings = new List<string>();
        public Dictionary<string, bool> ValidationChecks = new Dictionary<string, bool>();
        public float ValidationScore = 0f;
        public DateTime ValidationTimestamp = DateTime.Now;
        public DateTime ValidatorDate = DateTime.Now; // When validation was performed
        public DateTime ValidationDate = DateTime.Now; // Alias for ValidationTimestamp
        public string ValidationType = ""; // e.g., "Comprehensive", "Quick", "Custom"
        public string FailureReason = ""; // Primary reason for validation failure
        public List<AdvancedContractValidator> ValidatorsUsed = new List<AdvancedContractValidator>();
        public List<ContractSpecificCheck> ContractSpecificChecks = new List<ContractSpecificCheck>(); // Contract-specific validation checks
        public BatchValidationResult BatchValidation = new BatchValidationResult(); // Batch validation result
        public string QualityAssessment = ""; // Quality assessment text
        public bool MeetsQualityStandards = false; // Whether validation meets quality standards
        public Dictionary<string, object> ValidationMetadata = new Dictionary<string, object>();
        
        public AdvancedContractValidation()
        {
            ValidationId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Batch validation result for multiple contracts
    /// </summary>
    [System.Serializable]
    public class BatchValidationResult
    {
        public string BatchId = "";
        public string ContractId = ""; // Contract ID for this batch
        public int BatchSize = 0; // Number of items in batch
        public List<string> Issues = new List<string>(); // Validation issues
        public List<AdvancedContractValidation> ValidationResults = new List<AdvancedContractValidation>();
        public int TotalValidated = 0;
        public int PassedValidation = 0;
        public int FailedValidation = 0;
        public float OverallSuccessRate = 0f;
        public bool IsValid = false; // Whether the batch validation passed overall
        public DateTime BatchProcessedDate = DateTime.Now;
        public TimeSpan ProcessingTime = TimeSpan.Zero;
        public List<string> BatchErrors = new List<string>();
        public Dictionary<string, int> ValidationSummary = new Dictionary<string, int>();
        
        public BatchValidationResult()
        {
            BatchId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Batch validation request for processing multiple contracts
    /// </summary>
    [System.Serializable]
    public class BatchValidationRequest
    {
        public string RequestId = "";
        public List<string> ContractIds = new List<string>();
        public string ValidationType = "Standard";
        public bool HighPriority = false;
        public Dictionary<string, object> ValidationParameters = new Dictionary<string, object>();
        public DateTime RequestedDate = DateTime.Now;
        public string RequestedBy = "";
        public List<string> ValidatorTypes = new List<string>();
        
        public BatchValidationRequest()
        {
            RequestId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Quality trend data for analytics
    /// </summary>
    [System.Serializable]
    public class QualityTrendData
    {
        public string TrendId = "";
        public List<QualityDataPoint> QualityHistory = new List<QualityDataPoint>();
        public List<DateTime> QualityDates = new List<DateTime>();
        public QualityGrade CurrentQuality = QualityGrade.Standard;
        public QualityGrade PreviousQuality = QualityGrade.Standard;
        public TrendDirection Direction = TrendDirection.Stable;
        public float TrendStrength = 0f; // 0.0 to 1.0
        public float QualityVariance = 0f;
        public bool IsImproving = false;
        public DateTime LastUpdated = DateTime.Now;
        public Dictionary<string, float> QualityMetrics = new Dictionary<string, float>();
    }

    /// <summary>
    /// Trend direction enumeration
    /// </summary>
    public enum TrendDirection
    {
        Improving = 1,
        Stable = 0,
        Declining = -1,
        Unknown = 99
    }

    /// <summary>
    /// Quality metrics data structure
    /// </summary>
    [System.Serializable]
    public class QualityMetrics
    {
        public Dictionary<string, float> Metrics = new Dictionary<string, float>();
        public float OverallScore = 0f;
        public QualityGrade AverageQuality = QualityGrade.Standard;
        public QualityGrade MinQuality = QualityGrade.BelowStandard;
        public QualityGrade MaxQuality = QualityGrade.Premium;
        public float StandardDeviation = 0f;
        public DateTime LastUpdated = DateTime.Now;
        
        public QualityMetrics()
        {
            Metrics = new Dictionary<string, float>();
        }
        
        public void SetMetric(string key, float value)
        {
            Metrics[key] = value;
            LastUpdated = DateTime.Now;
        }
        
        public float GetMetric(string key)
        {
            return Metrics.TryGetValue(key, out float value) ? value : 0f;
        }
        
        public void UpdateQualityStats(List<QualityGrade> grades)
        {
            if (grades != null && grades.Count > 0)
            {
                var floatValues = grades.ToFloatList();
                OverallScore = floatValues.Average();
                AverageQuality = QualityGradeExtensions.FromFloat(OverallScore);
                MinQuality = grades.Min();
                MaxQuality = grades.Max();
                StandardDeviation = grades.CalculateStandardDeviation();
                LastUpdated = DateTime.Now;
            }
        }
    }

    /// <summary>
    /// Quality data point for time-series analysis
    /// </summary>
    [System.Serializable]
    public class QualityDataPoint
    {
        public DateTime Timestamp = DateTime.Now;
        public QualityGrade Quality = QualityGrade.Standard;
        public float QualityValue = 0.5f; // Float representation of quality
        public string SampleId = "";
        public string ContractId = "";
        public string PlantId = ""; // Plant identifier for the sample
        public StrainType StrainType = StrainType.None;
        public Dictionary<string, float> Metrics = new Dictionary<string, float>();
        
        public QualityDataPoint()
        {
            SampleId = Guid.NewGuid().ToString();
        }
        
        public QualityDataPoint(QualityGrade quality, DateTime timestamp)
        {
            SampleId = Guid.NewGuid().ToString();
            Quality = quality;
            QualityValue = quality.ToFloat();
            Timestamp = timestamp;
        }
    }
}
