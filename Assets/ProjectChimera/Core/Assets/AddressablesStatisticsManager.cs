using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// Statistics and performance monitoring for Addressables operations
    /// Single Responsibility: Asset loading statistics, performance metrics, and reporting
    /// Extracted from AddressablesAssetManager for better SRP compliance
    /// </summary>
    public class AddressablesStatisticsManager
    {
        private readonly Dictionary<string, AssetLoadStatistics> _loadStatistics = new Dictionary<string, AssetLoadStatistics>();
        private readonly List<AssetLoadEvent> _loadHistory = new List<AssetLoadEvent>();
        private readonly int _maxHistoryEntries;
        private readonly bool _enableLogging;

        private AssetManagerStatistics _overallStats = new AssetManagerStatistics();

        public AddressablesStatisticsManager(int maxHistoryEntries = 100, bool enableLogging = false)
        {
            _maxHistoryEntries = maxHistoryEntries;
            _enableLogging = enableLogging;
        }

        /// <summary>
        /// Record a successful asset load
        /// </summary>
        public void RecordSuccessfulLoad(string address, float loadTime, long assetSize = 0)
        {
            // Update overall statistics
            _overallStats.TotalLoads++;
            _overallStats.TotalLoadTime += loadTime;
            _overallStats.AverageLoadTime = _overallStats.TotalLoadTime / _overallStats.TotalLoads;

            if (loadTime > _overallStats.MaxLoadTime)
                _overallStats.MaxLoadTime = loadTime;

            // Update per-asset statistics
            if (!_loadStatistics.TryGetValue(address, out var stats))
            {
                stats = new AssetLoadStatistics { Address = address };
                _loadStatistics[address] = stats;
            }

            stats.LoadCount++;
            stats.TotalLoadTime += loadTime;
            stats.AverageLoadTime = stats.TotalLoadTime / stats.LoadCount;
            stats.LastLoadTime = Time.time;

            if (loadTime > stats.MaxLoadTime)
                stats.MaxLoadTime = loadTime;

            // Add to history
            AddToHistory(new AssetLoadEvent
            {
                Address = address,
                LoadTime = loadTime,
                AssetSize = assetSize,
                Timestamp = Time.time,
                Success = true
            });

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("ASSETS", $"Loaded {address} in {loadTime:F2}ms");
            }
        }

        /// <summary>
        /// Record a failed asset load
        /// </summary>
        public void RecordFailedLoad(string address, string errorMessage)
        {
            _overallStats.FailedLoads++;

            // Update per-asset statistics
            if (!_loadStatistics.TryGetValue(address, out var stats))
            {
                stats = new AssetLoadStatistics { Address = address };
                _loadStatistics[address] = stats;
            }

            stats.FailureCount++;
            stats.LastFailureReason = errorMessage;

            // Add to history
            AddToHistory(new AssetLoadEvent
            {
                Address = address,
                ErrorMessage = errorMessage,
                Timestamp = Time.time,
                Success = false
            });

            if (_enableLogging)
            {
                ChimeraLogger.LogError("ASSETS", $"Failed to load {address}: {errorMessage}");
            }
        }

        /// <summary>
        /// Record asset release
        /// </summary>
        public void RecordAssetRelease(string address)
        {
            _overallStats.TotalReleases++;

            if (_loadStatistics.TryGetValue(address, out var stats))
            {
                stats.ReleaseCount++;
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("ASSETS", $"Released {address}");
            }
        }

        /// <summary>
        /// Get overall asset manager statistics
        /// </summary>
        public AssetManagerStatistics GetOverallStatistics()
        {
            return _overallStats;
        }

        /// <summary>
        /// Get statistics for a specific asset
        /// </summary>
        public AssetLoadStatistics GetAssetStatistics(string address)
        {
            return _loadStatistics.TryGetValue(address, out var stats) ? stats : new AssetLoadStatistics { Address = address };
        }

        /// <summary>
        /// Get all asset statistics
        /// </summary>
        public Dictionary<string, AssetLoadStatistics> GetAllAssetStatistics()
        {
            return new Dictionary<string, AssetLoadStatistics>(_loadStatistics);
        }

        /// <summary>
        /// Get recent load history
        /// </summary>
        public List<AssetLoadEvent> GetRecentHistory(int count = 10)
        {
            int startIndex = Mathf.Max(0, _loadHistory.Count - count);
            return _loadHistory.GetRange(startIndex, _loadHistory.Count - startIndex);
        }

        /// <summary>
        /// Get performance report
        /// </summary>
        public string GeneratePerformanceReport()
        {
            var report = $"Asset Manager Performance Report:\n" +
                        $"Total Loads: {_overallStats.TotalLoads}\n" +
                        $"Failed Loads: {_overallStats.FailedLoads}\n" +
                        $"Success Rate: {(_overallStats.TotalLoads > 0 ? (1f - (float)_overallStats.FailedLoads / _overallStats.TotalLoads) * 100f : 0f):F1}%\n" +
                        $"Average Load Time: {_overallStats.AverageLoadTime:F2}ms\n" +
                        $"Max Load Time: {_overallStats.MaxLoadTime:F2}ms\n" +
                        $"Total Releases: {_overallStats.TotalReleases}";

            return report;
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void ResetStatistics()
        {
            _overallStats = new AssetManagerStatistics();
            _loadStatistics.Clear();
            _loadHistory.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("ASSETS", "Statistics reset");
            }
        }

        private void AddToHistory(AssetLoadEvent loadEvent)
        {
            _loadHistory.Add(loadEvent);

            // Maintain history size limit
            while (_loadHistory.Count > _maxHistoryEntries)
            {
                _loadHistory.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Overall asset manager statistics
    /// </summary>
    [System.Serializable]
    public struct AssetManagerStatistics
    {
        public int TotalLoads;
        public int FailedLoads;
        public int TotalReleases;
        public float TotalLoadTime;
        public float AverageLoadTime;
        public float MaxLoadTime;
    }

    /// <summary>
    /// Per-asset load statistics
    /// </summary>
    [System.Serializable]
    public struct AssetLoadStatistics
    {
        public string Address;
        public int LoadCount;
        public int FailureCount;
        public int ReleaseCount;
        public float TotalLoadTime;
        public float AverageLoadTime;
        public float MaxLoadTime;
        public float LastLoadTime;
        public string LastFailureReason;
    }

    /// <summary>
    /// Individual asset load event
    /// </summary>
    [System.Serializable]
    public struct AssetLoadEvent
    {
        public string Address;
        public float LoadTime;
        public long AssetSize;
        public float Timestamp;
        public bool Success;
        public string ErrorMessage;
    }
}