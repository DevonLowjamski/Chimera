using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Economy.Components
{
    /// <summary>
    /// Handles contract performance analytics, metrics tracking, and quality profiling
    /// for Project Chimera's game economy system.
    /// </summary>
    public class ContractTrackingAnalyticsService : MonoBehaviour
    {
        [Header("Analytics Configuration")]
        [SerializeField] private bool _enablePerformanceTracking = true;
        [SerializeField] private bool _enableQualityProfiling = true;
        [SerializeField] private bool _enableProgressNotifications = true;
        [SerializeField] private float _progressNotificationThreshold = 0.25f; // Notify every 25%
        [SerializeField] private bool _enableDeadlineWarnings = true;
        [SerializeField] private int _deadlineWarningDays = 7;

        // Analytics state
        private ContractTrackingMetrics _metrics = new ContractTrackingMetrics();
        private Dictionary<string, QualityAssessmentProfile> _qualityProfiles = new Dictionary<string, QualityAssessmentProfile>();
        private Dictionary<string, ContractPerformanceMetrics> _contractPerformance = new Dictionary<string, ContractPerformanceMetrics>();
        
        // Notification tracking
        private Dictionary<string, float> _lastProgressNotification = new Dictionary<string, float>();
        private Dictionary<string, bool> _deadlineWarningsSent = new Dictionary<string, bool>();

        // Events
        public System.Action<ActiveContractSO, float> OnContractProgressUpdated;
        public System.Action<ActiveContractSO, int> OnDeadlineWarning;
        public System.Action<string, ContractPerformanceMetrics> OnPerformanceMetricsUpdated;

        // Properties
        public ContractTrackingMetrics Metrics => _metrics;
        public bool PerformanceTrackingEnabled => _enablePerformanceTracking;
        public bool QualityProfilingEnabled => _enableQualityProfiling;

        public void Initialize()
        {
            InitializeMetrics();
            LogInfo("Contract analytics service initialized for game metrics tracking");
        }

        #region Metrics Tracking

        /// <summary>
        /// Update contract progress metrics and trigger notifications
        /// </summary>
        public void UpdateContractProgress(ContractProgress progress, List<PlantProductionRecord> production)
        {
            if (progress == null || production == null) return;

            var contractId = progress.ContractId;
            var contract = progress.Contract;

            // Calculate current progress
            float totalQuantity = production.Where(p => p.IsAllocated).Sum(p => p.Quantity);
            float progressPercentage = contract.RequiredQuantity > 0 ? totalQuantity / contract.RequiredQuantity : 0f;
            float averageQuality = production.Count > 0 ? production.Average(p => p.Quality.ToFloat()) : 0f;

            // Update progress data
            progress.CurrentQuantity = (int)totalQuantity;
            progress.AverageQuality = QualityGradeExtensions.FromFloat(averageQuality);
            progress.CompletionProgress = Mathf.Clamp01(progressPercentage);

            // Update performance metrics
            if (_enablePerformanceTracking)
            {
                UpdatePerformanceMetrics(contractId, progress, production);
            }

            // Check for progress notifications
            if (_enableProgressNotifications)
            {
                CheckProgressNotifications(contract, progressPercentage);
            }

            // Update quality profiles
            if (_enableQualityProfiling)
            {
                UpdateQualityProfiles(production);
            }

            // Trigger progress update event
            OnContractProgressUpdated?.Invoke(contract, progressPercentage);

            LogInfo($"Contract progress updated: {contract.ContractTitle} - {progressPercentage:P1} complete");
        }

        /// <summary>
        /// Track plant production metrics for analytics
        /// </summary>
        public void TrackPlantProduction(PlantProductionRecord plant)
        {
            if (plant == null) return;

            _metrics.TotalPlantsProcessed++;
            _metrics.TotalQuantityProduced += plant.Quantity;

            // Update quality metrics
            float avgQualityFloat = (_metrics.AverageQuality.ToFloat() * (_metrics.TotalPlantsProcessed - 1) + plant.Quality.ToFloat()) / _metrics.TotalPlantsProcessed;
            _metrics.AverageQuality = QualityGradeExtensions.FromFloat(avgQualityFloat);

            // Update strain-specific metrics
            UpdateStrainMetrics(plant.StrainType, plant.Quality, plant.Quantity);

            LogInfo($"Plant production tracked: {plant.PlantId}, Quality: {plant.Quality:P1}, Quantity: {plant.Quantity}g");
        }

        /// <summary>
        /// Update contract completion metrics
        /// </summary>
        public void TrackContractCompletion(ContractDelivery delivery, bool successful)
        {
            if (delivery == null) return;

            if (successful)
            {
                _metrics.TotalContractsCompleted++;
                _metrics.TotalContractValue += delivery.Contract.TotalValue;
                float avgDeliveryQualityFloat = (_metrics.AverageDeliveryQuality.ToFloat() * (_metrics.TotalContractsCompleted - 1) + delivery.AverageQuality.ToFloat()) / _metrics.TotalContractsCompleted;
                _metrics.AverageDeliveryQuality = QualityGradeExtensions.FromFloat(avgDeliveryQualityFloat);
            }
            else
            {
                _metrics.TotalContractsFailed++;
            }

            _metrics.TotalDeliveriesProcessed++;

            // Calculate success rate
            int totalContracts = _metrics.TotalContractsCompleted + _metrics.TotalContractsFailed;
            _metrics.ContractSuccessRate = totalContracts > 0 ? (float)_metrics.TotalContractsCompleted / totalContracts : 0f;

            LogInfo($"Contract completion tracked: {delivery.ContractId}, Success: {successful}");
        }

        /// <summary>
        /// Check and send deadline warnings for contracts
        /// </summary>
        public void CheckContractDeadlines(Dictionary<string, ContractProgress> activeContracts)
        {
            if (!_enableDeadlineWarnings) return;

            foreach (var kvp in activeContracts)
            {
                var contractId = kvp.Key;
                var progress = kvp.Value;
                var contract = progress.Contract;

                // Skip if warning already sent
                if (_deadlineWarningsSent.ContainsKey(contractId) && _deadlineWarningsSent[contractId])
                    continue;

                // Calculate days remaining
                var timeRemaining = contract.Deadline - DateTime.Now;
                int daysRemaining = (int)timeRemaining.TotalDays;

                // Send warning if within threshold
                if (daysRemaining <= _deadlineWarningDays && daysRemaining > 0)
                {
                    OnDeadlineWarning?.Invoke(contract, daysRemaining);
                    _deadlineWarningsSent[contractId] = true;
                    
                    LogInfo($"Deadline warning sent for contract {contract.ContractTitle}: {daysRemaining} days remaining");
                }
            }
        }

        #endregion

        #region Quality Analytics

        /// <summary>
        /// Update quality profiles for strain types based on production data
        /// </summary>
        public void UpdateQualityProfiles(List<PlantProductionRecord> production)
        {
            if (!_enableQualityProfiling || production == null) return;

            var strainGroups = production.GroupBy(p => p.StrainType);

            foreach (var group in strainGroups)
            {
                var strainType = group.Key;
                var plants = group.ToList();
                var key = strainType.ToString();

                if (!_qualityProfiles.ContainsKey(key))
                {
                    _qualityProfiles[key] = new QualityAssessmentProfile
                    {
                        StrainType = strainType,
                        SampleCount = 0,
                        AverageQuality = 0f,
                        QualityHistory = new List<QualityDataPoint>()
                    };
                }

                var profile = _qualityProfiles[key];

                foreach (var plant in plants)
                {
                    profile.SampleCount++;
                    float avgQualityFloat = (profile.AverageQuality.ToFloat() * (profile.SampleCount - 1) + plant.Quality.ToFloat()) / profile.SampleCount;
                    profile.AverageQuality = QualityGradeExtensions.FromFloat(avgQualityFloat);
                    
                    profile.QualityHistory.Add(new QualityDataPoint
                    {
                        Quality = plant.Quality,
                        Timestamp = plant.HarvestDate,
                        PlantId = plant.PlantId
                    });

                    // Maintain reasonable history size
                    if (profile.QualityHistory.Count > 100)
                    {
                        profile.QualityHistory.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Get performance metrics for a specific contract
        /// </summary>
        public ContractPerformanceMetrics GetContractPerformanceMetrics(string contractId)
        {
            return _contractPerformance.TryGetValue(contractId, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// Generate analytics report for all contracts
        /// </summary>
        public ContractAnalyticsReport GenerateAnalyticsReport()
        {
            var report = new ContractAnalyticsReport
            {
                GeneratedDate = DateTime.Now,
                OverallMetrics = _metrics,
                QualityProfiles = new Dictionary<string, QualityAssessmentProfile>(_qualityProfiles),
                ContractPerformance = new Dictionary<string, ContractPerformanceMetrics>(_contractPerformance)
            };

            // Calculate additional analytics
            report.TopPerformingStrains = GetTopPerformingStrains();
            report.QualityTrends = CalculateQualityTrends();
            report.PerformanceRecommendations = GeneratePerformanceRecommendations();

            LogInfo("Analytics report generated");
            return report;
        }

        #endregion

        #region Performance Analysis

        /// <summary>
        /// Analyze contract performance and identify trends
        /// </summary>
        public ContractPerformanceTrends AnalyzePerformanceTrends()
        {
            var trends = new ContractPerformanceTrends
            {
                AnalysisDate = DateTime.Now,
                TotalContractsAnalyzed = _contractPerformance.Count
            };

            if (_contractPerformance.Count == 0)
            {
                return trends;
            }

            var metrics = _contractPerformance.Values.ToList();

            // Calculate performance trends
            trends.AverageCompletionTime = (float)metrics.Average(m => m.AverageCompletionTime.TotalHours);
            trends.AverageQualityScore = QualityGradeExtensions.FromFloat(metrics.Average(m => m.AverageQuality.ToFloat()));
            trends.OnTimeDeliveryRate = metrics.Average(m => m.OnTimeDeliveryRate);

            // Identify best and worst performing contracts
            trends.BestPerformingContract = metrics.OrderByDescending(m => m.OverallScore).FirstOrDefault();
            trends.WorstPerformingContract = metrics.OrderBy(m => m.OverallScore).FirstOrDefault();

            return trends;
        }

        /// <summary>
        /// Get quality assessment profile for a strain type
        /// </summary>
        public QualityAssessmentProfile GetQualityProfile(StrainType strainType)
        {
            string key = strainType.ToString();
            return _qualityProfiles.TryGetValue(key, out var profile) ? profile : null;
        }

        /// <summary>
        /// Get all quality profiles
        /// </summary>
        public List<QualityAssessmentProfile> GetAllQualityProfiles()
        {
            return new List<QualityAssessmentProfile>(_qualityProfiles.Values);
        }

        #endregion

        #region Private Methods

        private void InitializeMetrics()
        {
            _metrics = new ContractTrackingMetrics
            {
                TotalContractsCompleted = 0,
                TotalContractsFailed = 0,
                TotalDeliveriesProcessed = 0,
                TotalPlantsProcessed = 0,
                TotalQuantityProduced = 0f,
                TotalContractValue = 0f,
                AverageQuality = 0f,
                AverageDeliveryQuality = 0f,
                ContractSuccessRate = 0f
            };
        }

        private void UpdatePerformanceMetrics(string contractId, ContractProgress progress, List<PlantProductionRecord> production)
        {
            if (!_contractPerformance.ContainsKey(contractId))
            {
                _contractPerformance[contractId] = new ContractPerformanceMetrics
                {
                    ContractId = contractId,
                    StartTime = progress.StartTime,
                    TotalPlantsUsed = 0,
                    AverageQuality = QualityGrade.BelowStandard,
                    AverageCompletionTime = TimeSpan.Zero,
                    OnTimeDeliveryRate = 1f,
                    OverallScore = 0f
                };
            }

            var metrics = _contractPerformance[contractId];
            metrics.TotalPlantsUsed = production.Count;
            metrics.AverageQuality = production.Count > 0 ? QualityGradeExtensions.FromFloat(production.Average(p => p.Quality.ToFloat())) : QualityGrade.BelowStandard;
            
            // Calculate completion time
            if (progress.IsReadyForDelivery)
            {
                metrics.AverageCompletionTime = DateTime.Now - progress.StartTime;
            }

            // Calculate overall performance score
            metrics.OverallScore = CalculateOverallPerformanceScore(metrics);

            OnPerformanceMetricsUpdated?.Invoke(contractId, metrics);
        }

        private void CheckProgressNotifications(ActiveContractSO contract, float progressPercentage)
        {
            var contractId = contract.ContractId;
            var lastNotified = _lastProgressNotification.TryGetValue(contractId, out var last) ? last : 0f;

            // Check if we've crossed a notification threshold
            float threshold = _progressNotificationThreshold;
            float nextThreshold = Mathf.Ceil(lastNotified / threshold) * threshold;

            if (progressPercentage >= nextThreshold && progressPercentage > lastNotified)
            {
                _lastProgressNotification[contractId] = progressPercentage;
                LogInfo($"Progress milestone reached for {contract.ContractTitle}: {progressPercentage:P0}");
            }
        }

        private void UpdateStrainMetrics(StrainType strainType, float quality, float quantity)
        {
            // Update strain-specific analytics
            // This could be expanded to track strain performance over time
        }

        private float CalculateOverallPerformanceScore(ContractPerformanceMetrics metrics)
        {
            // Weighted performance score calculation
            float qualityWeight = 0.4f;
            float timeWeight = 0.3f;
            float deliveryWeight = 0.3f;

            float qualityScore = metrics.AverageQuality.ToFloat();
            float timeScore = metrics.AverageCompletionTime.TotalDays > 0 ? Mathf.Clamp01(30f / (float)metrics.AverageCompletionTime.TotalDays) : 1f;
            float deliveryScore = metrics.OnTimeDeliveryRate;

            return (qualityScore * qualityWeight) + (timeScore * timeWeight) + (deliveryScore * deliveryWeight);
        }

        private TrendDirection CalculateTrendDirection(List<float> values)
        {
            if (values.Count < 2) return TrendDirection.Stable;

            float firstHalf = values.Take(values.Count / 2).Average();
            float secondHalf = values.Skip(values.Count / 2).Average();

            float change = secondHalf - firstHalf;
            
            if (change > 0.05f) return TrendDirection.Improving;
            if (change < -0.05f) return TrendDirection.Declining;
            return TrendDirection.Stable;
        }

        private float CalculateAverageChange(List<float> values)
        {
            if (values.Count < 2) return 0f;

            float totalChange = 0f;
            for (int i = 1; i < values.Count; i++)
            {
                totalChange += values[i] - values[i - 1];
            }

            return totalChange / (values.Count - 1);
        }

        private float CalculateTrendConfidence(List<float> values)
        {
            if (values.Count < 3) return 0.5f;

            // Simple confidence calculation based on consistency of trend
            var changes = new List<float>();
            for (int i = 1; i < values.Count; i++)
            {
                changes.Add(values[i] - values[i - 1]);
            }

            var positiveChanges = changes.Count(c => c > 0);
            var negativeChanges = changes.Count(c => c < 0);
            var noChanges = changes.Count(c => Mathf.Approximately(c, 0));

            float maxDirection = Mathf.Max(positiveChanges, negativeChanges, noChanges);
            return maxDirection / changes.Count;
        }

        #endregion

        #region Analytics Helper Methods

        /// <summary>
        /// Get top performing strains based on quality and quantity metrics
        /// </summary>
        private List<string> GetTopPerformingStrains()
        {
            // Return top 5 performing strains based on quality profiles
            return _qualityProfiles
                .OrderByDescending(kvp => kvp.Value.AverageQuality.ToFloat())
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Calculate quality trends over time
        /// </summary>
        private List<QualityTrendData> CalculateQualityTrends()
        {
            var trends = new List<QualityTrendData>();
            
            foreach (var profile in _qualityProfiles.Values)
            {
                if (profile.QualityHistory.Count > 1)
                {
                    var trend = new QualityTrendData
                    {
                        TrendId = System.Guid.NewGuid().ToString(),
                        QualityHistory = profile.QualityHistory,
                        CurrentQuality = profile.AverageQuality,
                        Direction = CalculateTrendDirection(profile.QualityHistory)
                    };
                    trends.Add(trend);
                }
            }
            
            return trends;
        }

        /// <summary>
        /// Generate performance recommendations based on analytics
        /// </summary>
        private List<string> GeneratePerformanceRecommendations()
        {
            var recommendations = new List<string>();
            
            // Analyze performance metrics and generate recommendations
            if (_metrics.ContractSuccessRate < 0.8f)
            {
                recommendations.Add("Consider improving quality control processes to increase contract success rate");
            }
            
            if (_metrics.AverageDeliveryQuality.ToFloat() < 0.7f)
            {
                recommendations.Add("Focus on strain selection and cultivation methods to improve delivery quality");
            }
            
            return recommendations;
        }

        /// <summary>
        /// Calculate trend direction for quality data points
        /// </summary>
        private TrendDirection CalculateTrendDirection(List<QualityDataPoint> qualityHistory)
        {
            if (qualityHistory.Count < 2) return TrendDirection.Stable;
            
            var recentValues = qualityHistory.TakeLast(5).Select(q => q.Quality.ToFloat()).ToList();
            if (recentValues.Count < 2) return TrendDirection.Stable;
            
            float trend = recentValues.Last() - recentValues.First();
            
            if (trend > 0.1f) return TrendDirection.Improving;
            if (trend < -0.1f) return TrendDirection.Declining;
            return TrendDirection.Stable;
        }

        /// <summary>
        /// Update strain-specific metrics
        /// </summary>
        private void UpdateStrainMetrics(StrainType strainType, QualityGrade quality, int quantity)
        {
            // Implementation for strain-specific metrics tracking
            // This method updates internal metrics for different strain types
            LogInfo($"Updated strain metrics for {strainType}: Quality={quality.ToFloat():F2}, Quantity={quantity}");
        }

        #endregion

        private void LogInfo(string message)
        {
            ChimeraLogger.Log($"[ContractAnalyticsService] {message}");
        }
    }
}