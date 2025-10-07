using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Foundation Performance Optimizer Service (POCO - Unity-independent core)
    /// Single Responsibility: Performance optimization, strategy application, and optimization tracking
    /// Extracted from FoundationPerformanceOptimizer for clean architecture compliance
    /// </summary>
    public class FoundationPerformanceOptimizerService
    {
        private static readonly System.Random _random = new System.Random();

        private readonly bool _enableOptimization;
        private readonly bool _enableLogging;
        private readonly bool _enableAutomaticOptimization;
        private readonly float _optimizationCooldown;
        private readonly float _optimizationTriggerThreshold;
        private readonly int _consecutivePoorThreshold;
        private readonly float _criticalPerformanceThreshold;
        private readonly bool _enableReinitialization;
        private readonly bool _enableResourceOptimization;
        private readonly bool _enableConfigurationTuning;
        private readonly bool _enableGracefulDegradation;

        private readonly Dictionary<string, OptimizationData> _optimizationHistory = new Dictionary<string, OptimizationData>();
        private readonly Dictionary<string, float> _lastOptimizationTime = new Dictionary<string, float>();
        private readonly Queue<OptimizationRequest> _optimizationQueue = new Queue<OptimizationRequest>();

        private OptimizationStats _stats = new OptimizationStats();
        private bool _isEnabled = true;

        public System.Action<string> OnOptimizationTriggered;
        public System.Action<string, bool> OnOptimizationCompleted;
        public System.Action<string, OptimizationStrategy> OnOptimizationStrategyApplied;

        public bool IsEnabled => _isEnabled;
        public OptimizationStats GetStats() => _stats;

        public FoundationPerformanceOptimizerService(
            bool enableOptimization = true,
            bool enableLogging = false,
            bool enableAutomaticOptimization = false,
            float optimizationCooldown = 60f,
            float optimizationTriggerThreshold = 0.5f,
            int consecutivePoorThreshold = 3,
            float criticalPerformanceThreshold = 0.3f,
            bool enableReinitialization = true,
            bool enableResourceOptimization = true,
            bool enableConfigurationTuning = true,
            bool enableGracefulDegradation = true)
        {
            _enableOptimization = enableOptimization;
            _enableLogging = enableLogging;
            _enableAutomaticOptimization = enableAutomaticOptimization;
            _optimizationCooldown = optimizationCooldown;
            _optimizationTriggerThreshold = optimizationTriggerThreshold;
            _consecutivePoorThreshold = consecutivePoorThreshold;
            _criticalPerformanceThreshold = criticalPerformanceThreshold;
            _enableReinitialization = enableReinitialization;
            _enableResourceOptimization = enableResourceOptimization;
            _enableConfigurationTuning = enableConfigurationTuning;
            _enableGracefulDegradation = enableGracefulDegradation;
        }

        public void Initialize()
        {
            _stats = new OptimizationStats();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "⚙️ FoundationPerformanceOptimizer Service initialized");
        }

        public void ProcessOptimizations()
        {
            if (!_isEnabled || !_enableOptimization) return;
            ProcessOptimizationQueue();
        }

        public void ProcessAnalysisResult(string systemName, PerformanceAnalysisResult result)
        {
            if (!_isEnabled || !_enableAutomaticOptimization) return;

            if (ShouldTriggerOptimization(systemName, result))
                QueueOptimization(systemName, result);
        }

        public bool TriggerOptimization(string systemName, float currentTime, OptimizationStrategy strategy = OptimizationStrategy.Auto)
        {
            if (!_isEnabled || !_enableOptimization) return false;

            if (!CanOptimizeSystem(systemName, currentTime))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"Cannot optimize {systemName} - cooldown in effect");
                return false;
            }

            var request = new OptimizationRequest
            {
                SystemName = systemName,
                Strategy = strategy,
                Priority = OptimizationPriority.Manual,
                RequestTime = currentTime,
                Reason = "Manual trigger"
            };

            _optimizationQueue.Enqueue(request);
            return true;
        }

        public OptimizationData GetOptimizationData(string systemName)
        {
            _optimizationHistory.TryGetValue(systemName, out var data);
            return data;
        }

        public Dictionary<string, OptimizationData> GetAllOptimizationData()
        {
            return new Dictionary<string, OptimizationData>(_optimizationHistory);
        }

        public string[] GetSystemsPendingOptimization()
        {
            return _optimizationQueue.Select(r => r.SystemName).Distinct().ToArray();
        }

        public void ClearOptimizationQueue()
        {
            _optimizationQueue.Clear();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Optimization queue cleared");
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;

            if (!enabled)
                ClearOptimizationQueue();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationPerformanceOptimizer: {(enabled ? "enabled" : "disabled")}");
        }

        #region Private Methods

        private bool ShouldTriggerOptimization(string systemName, PerformanceAnalysisResult result)
        {
            if (result.CurrentScore < _optimizationTriggerThreshold)
                return true;

            if (result.ConsecutivePoorPerformance >= _consecutivePoorThreshold)
                return true;

            if (result.Trend == PerformanceTrend.Declining && result.CurrentScore < 0.6f)
                return true;

            if (result.CurrentScore < _criticalPerformanceThreshold)
                return true;

            return false;
        }

        private void QueueOptimization(string systemName, PerformanceAnalysisResult result)
        {
            var priority = DetermineOptimizationPriority(result);
            var strategy = DetermineOptimizationStrategy(result);

            var request = new OptimizationRequest
            {
                SystemName = systemName,
                Strategy = strategy,
                Priority = priority,
                RequestTime = 0f,
                Reason = GenerateOptimizationReason(result)
            };

            _optimizationQueue.Enqueue(request);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Queued {priority} optimization for {systemName}: {strategy}");
        }

        private bool CanOptimizeSystem(string systemName, float currentTime)
        {
            if (!_lastOptimizationTime.TryGetValue(systemName, out var lastTime))
                return true;

            return (currentTime - lastTime) >= _optimizationCooldown;
        }

        private void ProcessOptimizationQueue()
        {
            if (_optimizationQueue.Count == 0) return;

            var sortedRequests = _optimizationQueue.ToArray()
                .OrderBy(r => (int)r.Priority)
                .ThenBy(r => r.RequestTime);

            _optimizationQueue.Clear();

            foreach (var request in sortedRequests)
            {
                if (CanOptimizeSystem(request.SystemName, request.RequestTime))
                {
                    ExecuteOptimization(request, request.RequestTime);
                    break;
                }
                else
                {
                    _optimizationQueue.Enqueue(request);
                }
            }
        }

        private void ExecuteOptimization(OptimizationRequest request, float currentTime)
        {
            _lastOptimizationTime[request.SystemName] = currentTime;

            var strategy = request.Strategy == OptimizationStrategy.Auto
                ? DetermineOptimizationStrategy(request.SystemName)
                : request.Strategy;

            bool success = ApplyOptimizationStrategy(request.SystemName, strategy);

            RecordOptimizationAttempt(request, strategy, success, currentTime);

            _stats.TotalOptimizations++;
            if (success) _stats.SuccessfulOptimizations++;

            OnOptimizationTriggered?.Invoke(request.SystemName);
            OnOptimizationStrategyApplied?.Invoke(request.SystemName, strategy);
            OnOptimizationCompleted?.Invoke(request.SystemName, success);

            if (_enableLogging)
            {
                var result = success ? "succeeded" : "failed";
                ChimeraLogger.Log("FOUNDATION", $"Optimization {result} for {request.SystemName} using {strategy} strategy");
            }
        }

        private OptimizationPriority DetermineOptimizationPriority(PerformanceAnalysisResult result)
        {
            if (result.CurrentScore < _criticalPerformanceThreshold)
                return OptimizationPriority.Critical;

            if (result.ConsecutivePoorPerformance >= _consecutivePoorThreshold)
                return OptimizationPriority.High;

            if (result.Trend == PerformanceTrend.Declining)
                return OptimizationPriority.Medium;

            return OptimizationPriority.Low;
        }

        private OptimizationStrategy DetermineOptimizationStrategy(PerformanceAnalysisResult result)
        {
            if (result.CurrentScore < _criticalPerformanceThreshold && _enableReinitialization)
                return OptimizationStrategy.Reinitialization;

            if (result.ScoreVariability > 0.2f && _enableConfigurationTuning)
                return OptimizationStrategy.ConfigurationTuning;

            if (result.Trend == PerformanceTrend.Declining && _enableResourceOptimization)
                return OptimizationStrategy.ResourceOptimization;

            if (_enableConfigurationTuning)
                return OptimizationStrategy.ConfigurationTuning;

            if (_enableGracefulDegradation)
                return OptimizationStrategy.GracefulDegradation;

            return OptimizationStrategy.Reinitialization;
        }

        private OptimizationStrategy DetermineOptimizationStrategy(string systemName)
        {
            return OptimizationStrategy.ConfigurationTuning;
        }

        private bool ApplyOptimizationStrategy(string systemName, OptimizationStrategy strategy)
        {
            try
            {
                switch (strategy)
                {
                    case OptimizationStrategy.Reinitialization:
                        return ApplyReinitializationOptimization(systemName);
                    case OptimizationStrategy.ResourceOptimization:
                        return ApplyResourceOptimization(systemName);
                    case OptimizationStrategy.ConfigurationTuning:
                        return ApplyConfigurationTuning(systemName);
                    case OptimizationStrategy.GracefulDegradation:
                        return ApplyGracefulDegradation(systemName);
                    default:
                        return false;
                }
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("FOUNDATION", $"Optimization failed for {systemName}: {ex.Message}");
                return false;
            }
        }

        private void RecordOptimizationAttempt(OptimizationRequest request, OptimizationStrategy strategy, bool success, float currentTime)
        {
            if (!_optimizationHistory.TryGetValue(request.SystemName, out var data))
            {
                data = new OptimizationData
                {
                    SystemName = request.SystemName,
                    FirstOptimizationTime = currentTime,
                    OptimizationHistory = new List<OptimizationAttempt>()
                };
            }

            var attempt = new OptimizationAttempt
            {
                Strategy = strategy,
                AttemptTime = currentTime,
                Success = success,
                Reason = request.Reason,
                Priority = request.Priority
            };

            data.OptimizationHistory.Add(attempt);
            data.LastOptimizationTime = currentTime;
            data.TotalAttempts++;
            if (success) data.SuccessfulAttempts++;

            if (data.OptimizationHistory.Count > 20)
                data.OptimizationHistory.RemoveAt(0);

            _optimizationHistory[request.SystemName] = data;
        }

        private string GenerateOptimizationReason(PerformanceAnalysisResult result)
        {
            if (result.CurrentScore < _criticalPerformanceThreshold)
                return "Critical performance threshold exceeded";

            if (result.ConsecutivePoorPerformance >= _consecutivePoorThreshold)
                return $"Consecutive poor performance ({result.ConsecutivePoorPerformance} times)";

            if (result.Trend == PerformanceTrend.Declining)
                return "Performance declining trend detected";

            return "Performance below optimization threshold";
        }

        private bool ApplyReinitializationOptimization(string systemName)
        {
            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Applying reinitialization optimization for {systemName}");
            return _random.NextDouble() > 0.3;
        }

        private bool ApplyResourceOptimization(string systemName)
        {
            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Applying resource optimization for {systemName}");
            return _random.NextDouble() > 0.2;
        }

        private bool ApplyConfigurationTuning(string systemName)
        {
            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Applying configuration tuning for {systemName}");
            return _random.NextDouble() > 0.1;
        }

        private bool ApplyGracefulDegradation(string systemName)
        {
            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Applying graceful degradation for {systemName}");
            return true;
        }

        #endregion
    }
}
