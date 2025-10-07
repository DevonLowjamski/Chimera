using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Foundation.Performance;
using System.Linq;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core.Foundation
{
    /// <summary>
    /// REFACTORED: Foundation Performance Coordinator - Legacy wrapper for backward compatibility
    /// Delegates to specialized FoundationPerformanceCore for all performance coordination
    /// Single Responsibility: Backward compatibility delegation
    /// </summary>
    public class FoundationPerformanceCoordinator : MonoBehaviour
    {
        [Header("Legacy Compatibility Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableLegacyMode = true;

        // Delegation target - the actual performance coordination system
        private FoundationPerformanceCore _performanceCore;

        // Properties for backward compatibility
        public bool IsEnabled => _performanceCore?.IsEnabled ?? true;
        public PerformanceCoordinatorStats GetStats() => ConvertToLegacyStats();

        // Legacy events for backward compatibility
        public System.Action<float> OnOverallPerformanceChanged;
        public System.Action<string, float> OnSystemPerformanceChanged;
        public System.Action<string> OnPerformanceOptimizationRecommended;
        public System.Action<PerformanceSnapshot> OnPerformanceSnapshot;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializePerformanceCore();
            ConnectLegacyEvents();

            if (_enableLogging)
                Logger.Log("FOUNDATION", "🔄 FoundationPerformanceCoordinator (Legacy Wrapper) initialized", this);
        }

        /// <summary>
        /// Initialize the performance core system using dependency injection
        /// </summary>
        private void InitializePerformanceCore()
        {
            // Use SafeResolve helper for clean dependency injection with fallback
            _performanceCore = DependencyResolutionHelper.SafeResolve<FoundationPerformanceCore>(this, "FOUNDATION");

            if (_performanceCore == null)
            {
                // Create and register if not found
                _performanceCore = gameObject.AddComponent<FoundationPerformanceCore>();
                DependencyResolutionHelper.SafeRegister<FoundationPerformanceCore>(_performanceCore, "FOUNDATION", this);
            }
        }

        /// <summary>
        /// Connect legacy events to the new performance core
        /// </summary>
        private void ConnectLegacyEvents()
        {
            if (_performanceCore != null)
            {
                _performanceCore.OnOverallPerformanceChanged += (score) => OnOverallPerformanceChanged?.Invoke(score);
                _performanceCore.OnSystemPerformanceChanged += (system, score) => OnSystemPerformanceChanged?.Invoke(system, score);
                _performanceCore.OnOptimizationRecommended += (system) => OnPerformanceOptimizationRecommended?.Invoke(system);
            }
        }

        /// <summary>
        /// Coordinate performance optimizations across all systems (Legacy method)
        /// Delegates to FoundationPerformanceCore
        /// </summary>
        public void CoordinatePerformanceOptimizations()
        {
            if (!_enableLegacyMode) return;

            // Delegate to the performance core - it handles all coordination
            // The core system automatically runs these operations via its Tick method
            if (_enableLogging)
                Logger.Log("FOUNDATION", "Legacy performance coordination call delegated to FoundationPerformanceCore", this);
        }

        /// <summary>
        /// Get system performance data (Legacy method - delegates to performance core)
        /// </summary>
        public SystemPerformanceData GetSystemPerformance(string systemName)
        {
            return _performanceCore?.GetSystemPerformance(systemName) ?? new SystemPerformanceData();
        }

        /// <summary>
        /// Get all system performance data (Legacy method - delegates to performance core)
        /// </summary>
        public Dictionary<string, SystemPerformanceData> GetAllSystemPerformance()
        {
            var systemPerformanceArray = _performanceCore?.GetAllSystemPerformance() ?? new SystemPerformanceData[0];
            var dictionary = new Dictionary<string, SystemPerformanceData>();
            foreach (var perfData in systemPerformanceArray)
            {
                dictionary[perfData.SystemName] = perfData;
            }
            return dictionary;
        }

        /// <summary>
        /// Get performance history (Legacy method - delegates to performance core)
        /// </summary>
        public PerformanceSnapshot[] GetPerformanceHistory()
        {
            var historySnapshots = _performanceCore?.GetPerformanceHistory() ?? new Performance.PerformanceSnapshot[0];
            // Convert new PerformanceSnapshot format to legacy format
            return ConvertToLegacySnapshots(historySnapshots);
        }

        /// <summary>
        /// Get overall performance score (Legacy method - delegates to performance core)
        /// </summary>
        public float GetOverallPerformanceScore()
        {
            return _performanceCore?.OverallPerformanceScore ?? 1.0f;
        }

        /// <summary>
        /// Get performance category (Legacy method - delegates to performance core)
        /// </summary>
        public PerformanceCategory GetPerformanceCategory()
        {
            return _performanceCore?.GetPerformanceCategory() ?? PerformanceCategory.Acceptable;
        }

        /// <summary>
        /// Get systems with poor performance (Legacy method - delegates to performance core)
        /// </summary>
        public string[] GetPoorPerformingSystems()
        {
            return _performanceCore?.GetPoorPerformingSystems() ?? new string[0];
        }

        /// <summary>
        /// Generate performance report (Legacy method - delegates to performance core)
        /// </summary>
        public PerformanceReport GeneratePerformanceReport()
        {
            var newReport = _performanceCore?.GeneratePerformanceReport();
            return ConvertToLegacyReport(newReport);
        }

        /// <summary>
        /// Set system enabled/disabled (Legacy method - delegates to performance core)
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _performanceCore?.SetEnabled(enabled);

            if (_enableLogging)
                Logger.Log("FOUNDATION", $"FoundationPerformanceCoordinator (Legacy): {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods - Legacy Compatibility Helpers

        /// <summary>
        /// Convert new performance core stats to legacy format
        /// </summary>
        private PerformanceCoordinatorStats ConvertToLegacyStats()
        {
            var overallScore = _performanceCore?.OverallPerformanceScore ?? 1.0f;
            return new PerformanceCoordinatorStats
            {
                PerformanceScore = overallScore,
                PreviousPerformanceScore = overallScore,
                IsPerformingWell = overallScore >= 0.6f,
                OptimizationsTriggered = 0,
                RecommendationsGenerated = 0
            };
        }

        /// <summary>
        /// Convert new performance snapshots to legacy format
        /// </summary>
        private PerformanceSnapshot[] ConvertToLegacySnapshots(Performance.PerformanceSnapshot[] newSnapshots)
        {
            var legacySnapshots = new List<PerformanceSnapshot>();

            foreach (var newSnapshot in newSnapshots)
            {
                var legacySnapshot = new PerformanceSnapshot
                {
                    Timestamp = newSnapshot.Timestamp,
                    OverallPerformanceScore = newSnapshot.OverallPerformanceScore,
                    TotalSystems = newSnapshot.SystemCount,
                    PoorPerformingSystems = CountPoorPerformingSystems(newSnapshot.SystemSnapshots),
                    SystemScores = ExtractSystemScores(newSnapshot.SystemSnapshots)
                };
                legacySnapshots.Add(legacySnapshot);
            }

            return legacySnapshots.ToArray();
        }

        /// <summary>
        /// Convert new performance report to legacy format
        /// </summary>
        private PerformanceReport ConvertToLegacyReport(Performance.PerformanceReport? newReport)
        {
            if (!newReport.HasValue)
            {
                return new PerformanceReport
                {
                    ReportTime = Time.time,
                    OverallPerformanceScore = 1.0f,
                    PerformanceCategory = PerformanceCategory.Acceptable,
                    TotalSystems = 0,
                    SystemDetails = new List<SystemPerformanceSummary>()
                };
            }

            var report = newReport.Value;
            return new PerformanceReport
            {
                ReportTime = report.ReportTime,
                OverallPerformanceScore = report.OverallScore,
                PerformanceCategory = report.PerformanceCategory,
                TotalSystems = report.SystemCount,
                ExcellentSystems = CountSystemsByCategory(report.SystemReports, PerformanceCategory.Excellent),
                GoodSystems = CountSystemsByCategory(report.SystemReports, PerformanceCategory.Good),
                AcceptableSystems = CountSystemsByCategory(report.SystemReports, PerformanceCategory.Acceptable),
                PoorSystems = CountSystemsByCategory(report.SystemReports, PerformanceCategory.Poor),
                SystemDetails = ConvertSystemReports(report.SystemReports)
            };
        }

        /// <summary>
        /// Helper methods for legacy format conversion
        /// </summary>
        private int CountPoorPerformingSystems(Performance.SystemPerformanceSnapshot[] snapshots)
        {
            return snapshots?.Count(s => s.Category == PerformanceCategory.Poor) ?? 0;
        }

        private Dictionary<string, float> ExtractSystemScores(Performance.SystemPerformanceSnapshot[] snapshots)
        {
            var scores = new Dictionary<string, float>();
            if (snapshots != null)
            {
                foreach (var snapshot in snapshots)
                {
                    scores[snapshot.SystemName] = snapshot.PerformanceScore;
                }
            }
            return scores;
        }

        private int CountSystemsByCategory(Performance.SystemReport[] systemReports, PerformanceCategory category)
        {
            return systemReports?.Count(r => r.Category == category) ?? 0;
        }

        private List<SystemPerformanceSummary> ConvertSystemReports(Performance.SystemReport[] systemReports)
        {
            var summaries = new List<SystemPerformanceSummary>();
            if (systemReports != null)
            {
                foreach (var report in systemReports)
                {
                    summaries.Add(new SystemPerformanceSummary
                    {
                        SystemName = report.SystemName,
                        PerformanceScore = report.PerformanceScore,
                        Category = report.Category,
                        LastUpdateTime = report.LastUpdateTime,
                        OptimizationRecommendations = report.Recommendations
                    });
                }
            }
            return summaries;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Performance category enumeration
    /// </summary>
    public enum PerformanceCategory
    {
        Excellent,
        Good,
        Acceptable,
        Poor
    }

    /// <summary>
    /// Performance trend enumeration
    /// </summary>
    public enum PerformanceTrend
    {
        Improving,
        Stable,
        Declining
    }

    /// <summary>
    /// System performance data
    /// </summary>
    [System.Serializable]
    public struct SystemPerformanceData
    {
        public string SystemName;
        public float PerformanceScore;
        public PerformanceTrend Trend;
        public float FirstMeasurementTime;
        public float LastUpdateTime;
        public int MeasurementCount;
        public int ConsecutivePoorPerformance;
        public List<string> OptimizationRecommendations;
    }

    /// <summary>
    /// Performance snapshot
    /// </summary>
    [System.Serializable]
    public struct PerformanceSnapshot
    {
        public float Timestamp;
        public float OverallPerformanceScore;
        public int TotalSystems;
        public int PoorPerformingSystems;
        public Dictionary<string, float> SystemScores;
    }

    /// <summary>
    /// Performance report
    /// </summary>
    [System.Serializable]
    public struct PerformanceReport
    {
        public float ReportTime;
        public float OverallPerformanceScore;
        public PerformanceCategory PerformanceCategory;
        public int TotalSystems;
        public int ExcellentSystems;
        public int GoodSystems;
        public int AcceptableSystems;
        public int PoorSystems;
        public List<SystemPerformanceSummary> SystemDetails;
    }

    /// <summary>
    /// System performance summary
    /// </summary>
    [System.Serializable]
    public struct SystemPerformanceSummary
    {
        public string SystemName;
        public float PerformanceScore;
        public PerformanceCategory Category;
        public float LastUpdateTime;
        public List<string> OptimizationRecommendations;
    }

    /// <summary>
    /// Performance coordinator statistics
    /// </summary>
    [System.Serializable]
    public struct PerformanceCoordinatorStats
    {
        public float PerformanceScore;
        public float PreviousPerformanceScore;
        public bool IsPerformingWell;
        public int OptimizationsTriggered;
        public int RecommendationsGenerated;
    }

    #endregion
}
