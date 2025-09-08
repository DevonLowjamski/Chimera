using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
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
}