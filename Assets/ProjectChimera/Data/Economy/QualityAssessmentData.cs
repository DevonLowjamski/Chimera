using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Economy
{
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
