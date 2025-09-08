using System;
using UnityEngine;
using System.Collections.Generic;
// using ProjectChimera.Data.Economy; // Removed circular reference

namespace ProjectChimera.Data.Economy.Indicators
{
    /// <summary>
    /// Economic indicators, metrics, and analytics-focused data structures extracted from EconomicDataStructures.cs
    /// Contains economic performance monitoring, risk assessment, trend analysis, and statistical reporting systems
    /// Part of Phase 1 Foundation Data Structures refactoring
    /// </summary>

    #region Economic Performance Metrics

    [System.Serializable]
    public class EconomicGamingMetrics
    {
        public int TotalTrades = 0;
        public decimal TotalVolume = 0m;
        public float WinRate = 0f;
        public decimal TotalProfitLoss = 0m;
        public decimal AverageTradeValue = 0m;
        public int SuccessfulTrades = 0;
        public int FailedTrades = 0;
        public float RiskScore = 0.5f;
        public DateTime LastTradeDate;
        public List<string> AchievedMilestones = new List<string>();

        public void UpdateMetrics(TradeExecutionResult result)
        {
            TotalTrades++;
            TotalVolume += result.TradeValue;
            LastTradeDate = DateTime.Now;

            if (result.Success)
            {
                SuccessfulTrades++;
                TotalProfitLoss += result.ProfitLoss;
            }
            else
            {
                FailedTrades++;
            }

            WinRate = TotalTrades > 0 ? (float)SuccessfulTrades / TotalTrades : 0f;
            AverageTradeValue = TotalTrades > 0 ? TotalVolume / TotalTrades : 0m;
        }
    }

    [System.Serializable]
    public class TradeExecutionResult
    {
        public bool Success = false;
        public decimal TradeValue = 0m;
        public decimal ProfitLoss = 0m;
        public DateTime ExecutionTime;
        public string ErrorMessage;
    }



    [System.Serializable]
    public class HistoricalValue
    {
        public DateTime Date;
        public float Value;
        public string Notes;
    }

    [System.Serializable]
    public class KPISet
    {
        public string SetId;
        public string SetName;
        public List<PerformanceMetrics> Metrics = new List<PerformanceMetrics>();
        public KPICategory Category = KPICategory.Financial;
        public DateTime LastCalculated;
        public float OverallScore = 0f;
        public PerformanceStatus Status = PerformanceStatus.OnTrack;
    }

    #endregion

    #region Economic Indicators and Analysis

    [System.Serializable]
    public class EconomicIndicatorAnalyzer
    {
        public bool IsActive = true;
        public List<string> TrackedIndicators = new List<string>();
        public DateTime LastAnalysis;
        public List<EconomicIndicatorTrend> CurrentTrends = new List<EconomicIndicatorTrend>();
        public AnalysisFrequency UpdateFrequency = AnalysisFrequency.Daily;

        public void PerformAnalysis()
        {
            LastAnalysis = DateTime.Now;
            // Analysis logic would be implemented here
        }
    }

    [System.Serializable]
    public class EconomicIndicatorTrend
    {
        public string IndicatorName;
        public List<float> HistoricalValues = new List<float>();
        public TrendDirection Direction = TrendDirection.Stable;
        public float TrendStrength = 0f;
        public float VolatilityIndex = 0f;
        public DateTime LastUpdate;
        public IndicatorType Type = IndicatorType.Economic;
        public float CurrentValue = 0f;
        public float PreviousValue = 0f;
        public float ChangePercent = 0f;
    }

    [System.Serializable]
    public class EconomicAnalyticsEngine
    {
        public bool IsActive = true;
        public Dictionary<string, AnalyticsReport> PlayerAnalytics = new Dictionary<string, AnalyticsReport>();
        public MarketAnalyticsData GlobalAnalytics;
        public TrendAnalysisSystem TrendAnalysis;
        public DateTime LastUpdate;
        public AnalyticsConfiguration Configuration;

        public void Initialize()
        {
            IsActive = true;
            TrendAnalysis = new TrendAnalysisSystem { IsActive = true };
            GlobalAnalytics = new MarketAnalyticsData();
            Configuration = new AnalyticsConfiguration();
            LastUpdate = DateTime.Now;
        }

        public void Initialize(bool enableAnalytics)
        {
            IsActive = enableAnalytics;
            if (IsActive)
            {
                Initialize();
            }
        }
    }

    [System.Serializable]
    public class AnalyticsConfiguration
    {
        public bool EnableRealTimeAnalysis = true;
        public bool EnablePredictiveAnalysis = false;
        public int HistoricalDataRetentionDays = 365;
        public float AnalysisThreshold = 0.05f;
        public List<string> EnabledAnalysisTypes = new List<string>();
    }

    [System.Serializable]
    public class MarketAnalyticsData
    {
        public DateTime LastUpdate;
        public Dictionary<string, float> MarketIndicators = new Dictionary<string, float>();
        public List<EconomicIndicatorTrend> DetectedTrends = new List<EconomicIndicatorTrend>();
        public float OverallMarketHealth = 0.7f;
        public MarketSentiment Sentiment = MarketSentiment.Neutral;
        public float VolatilityIndex = 0f;
        public float LiquidityIndex = 0f;
    }

    [System.Serializable]
    public class TrendAnalysisSystem
    {
        public List<TrendPattern> DetectedPatterns = new List<TrendPattern>();
        public float TrendDetectionSensitivity = 0.7f;
        public int MinimumDataPoints = 5;
        public bool EnableAutomaticDetection = true;
        public bool IsActive = true;
        public DateTime LastAnalysis;
        public TrendAnalysisSettings Settings;
    }

    [System.Serializable]
    public class TrendPattern
    {
        public string PatternId;
        public string PatternName;
        public TrendDirection Direction = TrendDirection.Stable;
        public float Strength = 0.5f;
        public float Confidence = 0.7f;
        public DateTime DetectedDate;
        public int DurationDays = 0;
        public PatternType Type = PatternType.Linear;
        public List<float> DataPoints = new List<float>();
    }

    [System.Serializable]
    public class TrendAnalysisSettings
    {
        public float MinPatternStrength = 0.3f;
        public float MinConfidenceLevel = 0.6f;
        public int LookbackPeriodDays = 30;
        public bool DetectReversals = true;
        public bool DetectBreakouts = true;
        public bool DetectSeasonality = false;
    }

    #endregion

    #region Risk Assessment and Analysis

    [System.Serializable]
    public class RiskAssessmentEngine
    {
        public bool IsActive = true;
        public float AssessmentAccuracy = 0.85f;
        public Dictionary<string, RiskProfile> RiskProfiles = new Dictionary<string, RiskProfile>();
        public List<RiskFactor> GlobalRiskFactors = new List<RiskFactor>();
        public DateTime LastAssessment;
        public RiskAssessmentSettings Settings;

        public void Initialize()
        {
            IsActive = true;
            LastAssessment = DateTime.Now;
            Settings = new RiskAssessmentSettings();
            SetupGlobalRiskFactors();
        }

        public void Initialize(bool enableRiskAssessment)
        {
            IsActive = enableRiskAssessment;
            if (IsActive)
            {
                Initialize();
            }
        }

        public RiskAssessment AssessRisk(string assetId, decimal investmentAmount)
        {
            var riskScore = CalculateRiskScore(assetId);
            return new RiskAssessment
            {
                AssetId = assetId,
                InvestmentAmount = investmentAmount,
                RiskScore = riskScore,
                RiskLevel = DetermineRiskLevel(riskScore),
                AssessmentDate = DateTime.Now
            };
        }

        private float CalculateRiskScore(string assetId)
        {
            return 0.5f; // Placeholder implementation
        }

        private RiskLevel DetermineRiskLevel(float riskScore)
        {
            if (riskScore < 0.3f) return RiskLevel.Low;
            if (riskScore < 0.6f) return RiskLevel.Medium;
            if (riskScore < 0.8f) return RiskLevel.High;
            return RiskLevel.VeryHigh;
        }

        private void SetupGlobalRiskFactors()
        {
            GlobalRiskFactors.Add(new RiskFactor
            {
                FactorName = "Market Volatility",
                Impact = 0.3f,
                Weight = 0.25f
            });
            GlobalRiskFactors.Add(new RiskFactor
            {
                FactorName = "Liquidity Risk",
                Impact = 0.2f,
                Weight = 0.2f
            });
        }
    }

    [System.Serializable]
    public class RiskAssessmentSettings
    {
        public float VolatilityWeight = 0.3f;
        public float LiquidityWeight = 0.2f;
        public float CreditWeight = 0.25f;
        public float MarketWeight = 0.25f;
        public bool EnableStressTest = true;
        public bool EnableScenarioAnalysis = false;
    }

    [System.Serializable]
    public class RiskAssessment
    {
        public string AssetId;
        public decimal InvestmentAmount;
        public float RiskScore;
        public RiskLevel RiskLevel;
        public DateTime AssessmentDate;
        public List<RiskFactor> IdentifiedRisks = new List<RiskFactor>();
        public string RecommendedAction;
        public float ConfidenceLevel = 0.8f;
    }

    [System.Serializable]
    public class RiskFactor
    {
        public string FactorName;
        public RiskFactorType Type = RiskFactorType.Market;
        public float Impact = 0f;
        public float Weight = 0f;
        public float Probability = 0.5f;
        public string Description;
        public MitigationStrategy Mitigation;
    }

    [System.Serializable]
    public class MitigationStrategy
    {
        public string StrategyName;
        public string Description;
        public float EffectivenessRating = 0.5f;
        public decimal ImplementationCost = 0m;
        public int TimeFrameDays = 30;
    }

    [System.Serializable]
    public class RiskSignals
    {
        public float OverallRiskLevel = 0.5f;
        public float VolatilityRisk = 0f;
        public float LiquidityRisk = 0f;
        public float MarketRisk = 0f;
        public float CreditRisk = 0f;
        public DateTime LastAssessment;
        public List<string> RiskFactors = new List<string>();
        public AlertLevel CurrentAlertLevel = AlertLevel.Green;
    }

    #endregion

    #region Reporting and Documentation

    [System.Serializable]
    public class FinancialReportingSystem
    {
        public bool IsActive = true;
        public DateTime LastReportGenerated;
        public List<string> AvailableReports = new List<string>();
        public ReportingConfiguration Configuration;
        public List<ScheduledReport> ScheduledReports = new List<ScheduledReport>();

        public FinancialReport GenerateReport(ReportType type, DateTime startDate, DateTime endDate)
        {
            LastReportGenerated = DateTime.Now;
            return new FinancialReport
            {
                ReportId = Guid.NewGuid().ToString(),
                Type = type,
                GeneratedDate = LastReportGenerated,
                PeriodStart = startDate,
                PeriodEnd = endDate
            };
        }
    }

    [System.Serializable]
    public class ReportingConfiguration
    {
        public bool AutoGenerateReports = true;
        public int RetentionDays = 365;
        public List<ReportType> EnabledReports = new List<ReportType>();
        public string DefaultFormat = "PDF";
        public bool EmailNotifications = false;
    }

    [System.Serializable]
    public class ScheduledReport
    {
        public string ScheduleId;
        public ReportType Type;
        public ReportFrequency Frequency = ReportFrequency.Monthly;
        public DateTime NextGeneration;
        public bool IsActive = true;
        public List<string> Recipients = new List<string>();
    }

    [System.Serializable]
    public class FinancialReport
    {
        public string ReportId;
        public ReportType Type;
        public DateTime GeneratedDate;
        public DateTime PeriodStart;
        public DateTime PeriodEnd;
        public Dictionary<string, object> Data = new Dictionary<string, object>();
        public List<string> KeyFindings = new List<string>();
        public string Summary;
        public ReportStatus Status = ReportStatus.Generated;
    }

    [System.Serializable]
    public class AnalyticsReport
    {
        public string PlayerId;
        public DateTime ReportDate;
        public Dictionary<string, float> PerformanceMetrics = new Dictionary<string, float>();
        public List<string> Insights = new List<string>();
        public List<string> Recommendations = new List<string>();
        public float OverallScore = 0f;
        public PerformanceGrade Grade = PerformanceGrade.B;
        public List<BenchmarkComparison> Benchmarks = new List<BenchmarkComparison>();
    }

    [System.Serializable]
    public class BenchmarkComparison
    {
        public string BenchmarkName;
        public float PlayerValue;
        public float BenchmarkValue;
        public float Difference;
        public ComparisonResult Result = ComparisonResult.AtPar;
    }

    [System.Serializable]
    public class DueDiligenceReport
    {
        public string ReportId;
        public string TargetCompany;
        public DateTime CompletionDate;
        public float OverallScore = 0f;
        public List<string> KeyFindings = new List<string>();
        public List<string> RiskFactors = new List<string>();
        public List<string> Opportunities = new List<string>();
        public DueDiligenceStatus Status = DueDiligenceStatus.InProgress;
        public decimal EstimatedValue = 0m;
        public RecommendationLevel Recommendation = RecommendationLevel.Hold;
    }

    #endregion

    #region Competitive and Threat Analysis

    [System.Serializable]
    public class ThreatAssessment
    {
        public string ThreatId;
        public ThreatSeverity Severity = ThreatSeverity.Medium;
        public List<string> RiskFactors = new List<string>();
        public float Probability = 0.5f;
        public decimal PotentialImpact = 0m;
        public ThreatCategory Category = ThreatCategory.Economic;
        public DateTime IdentifiedDate;
        public ThreatStatus Status = ThreatStatus.Active;
        public List<CountermeasureAction> Countermeasures = new List<CountermeasureAction>();
    }

    [System.Serializable]
    public class CountermeasureAction
    {
        public string ActionId;
        public string ActionName;
        public string Description;
        public decimal Cost = 0m;
        public float Effectiveness = 0.5f;
        public int ImplementationDays = 30;
        public ActionPriority Priority = ActionPriority.Medium;
    }

    [System.Serializable]
    public class GeopoliticalImpactAssessment
    {
        public string AssessmentId;
        public List<string> AffectedRegions = new List<string>();
        public float ImpactSeverity = 0.5f;
        public List<string> RiskFactors = new List<string>();
        public DateTime AssessmentDate;
        public GeopoliticalEventType EventType = GeopoliticalEventType.Regulatory;
        public float ConfidenceLevel = 0.7f;
        public TimeframeDuration TimeFrame = TimeframeDuration.ShortTerm;
    }

    [System.Serializable]
    public class WarfareStatistics
    {
        public string PlayerId;
        public int CampaignsLaunched = 0;
        public int CampaignsWon = 0;
        public int CampaignsLost = 0;
        public decimal TotalDamageDealt = 0m;
        public decimal TotalDamageReceived = 0m;
        public float WarfareRating = 0f;
        public DateTime LastUpdate;
        public WarfareSpecialty Specialty = WarfareSpecialty.Balanced;
    }

    [System.Serializable]
    public class WarfareMetrics
    {
        public int TotalCampaigns = 0;
        public int ActiveCampaigns = 0;
        public float AverageSuccessRate = 0f;
        public decimal TotalDamageDealt = 0m;
        public DateTime LastCalculated;
        public float EfficiencyRating = 0f;
        public float ResourceUtilization = 0f;
    }

    #endregion

    #region Supporting Enums

    public enum MetricType
    {
        Ratio,
        Percentage,
        Currency,
        Count,
        Index,
        Score,
        Rate,
        Volume
    }

    public enum TrendDirection
    {
        Rising,
        Falling,
        Stable,
        Volatile,
        Bullish,
        Bearish,
        Sideways
    }

    public enum KPICategory
    {
        Financial,
        Operational,
        Strategic,
        Risk,
        Performance,
        Growth,
        Efficiency,
        Quality
    }

    public enum PerformanceStatus
    {
        Exceeding,
        OnTrack,
        AtRisk,
        Below,
        Critical,
        Improving,
        Declining
    }

    public enum AnalysisFrequency
    {
        RealTime,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annually
    }

    public enum IndicatorType
    {
        Economic,
        Financial,
        Market,
        Technical,
        Fundamental,
        Sentiment,
        Volume,
        Volatility
    }

    public enum MarketSentiment
    {
        VeryBearish,
        Bearish,
        Neutral,
        Bullish,
        VeryBullish
    }

    public enum PatternType
    {
        Linear,
        Exponential,
        Cyclical,
        Seasonal,
        Random,
        Trending,
        Reversal,
        Breakout
    }

    public enum RiskLevel
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh,
        Extreme
    }

    public enum RiskFactorType
    {
        Market,
        Credit,
        Operational,
        Liquidity,
        Regulatory,
        Geopolitical,
        Technology,
        Environmental
    }

    public enum AlertLevel
    {
        Green,
        Yellow,
        Orange,
        Red,
        Critical
    }

    public enum ReportType
    {
        Performance,
        Risk,
        Financial,
        Analytics,
        Compliance,
        Executive,
        Operational,
        Strategic
    }

    public enum ReportFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annually,
        OnDemand
    }

    public enum ReportStatus
    {
        Pending,
        Generated,
        Delivered,
        Archived,
        Failed
    }

    public enum PerformanceGrade
    {
        A,
        B,
        C,
        D,
        F
    }

    public enum ComparisonResult
    {
        Above,
        AtPar,
        Below,
        Significantly_Above,
        Significantly_Below
    }

    public enum DueDiligenceStatus
    {
        NotStarted,
        InProgress,
        UnderReview,
        Completed,
        Approved,
        Rejected
    }

    public enum RecommendationLevel
    {
        StrongBuy,
        Buy,
        Hold,
        Sell,
        StrongSell
    }

    public enum ThreatSeverity
    {
        Low,
        Medium,
        High,
        Critical,
        Extreme
    }

    public enum ThreatCategory
    {
        Economic,
        Competitive,
        Regulatory,
        Operational,
        Strategic,
        Reputational
    }

    public enum ThreatStatus
    {
        Identified,
        Active,
        Monitoring,
        Mitigated,
        Resolved,
        Escalated
    }

    public enum ActionPriority
    {
        Low,
        Medium,
        High,
        Urgent,
        Critical
    }

    public enum GeopoliticalEventType
    {
        Regulatory,
        Trade,
        Political,
        Economic,
        Security,
        Environmental
    }

    public enum TimeframeDuration
    {
        Immediate,
        ShortTerm,
        MediumTerm,
        LongTerm,
        Indefinite
    }

    public enum WarfareSpecialty
    {
        Aggressive,
        Defensive,
        Strategic,
        Tactical,
        Balanced,
        Economic,
        Intelligence
    }

    #endregion
}
