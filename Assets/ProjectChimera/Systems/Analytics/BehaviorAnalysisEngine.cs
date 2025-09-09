using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Specialized engine for analyzing player behavior patterns and engagement metrics.
    /// Handles behavior pattern detection, engagement scoring, and player journey analysis.
    /// </summary>
    public class BehaviorAnalysisEngine
    {
        private readonly AnalyticsCore _analyticsCore;
        private readonly BehaviorAnalyzer _behaviorAnalyzer;
        
        // Behavior tracking
        private List<AnalyticsEvent> _behaviorEventHistory = new List<AnalyticsEvent>();
        private Dictionary<string, BehaviorPattern> _identifiedPatterns = new Dictionary<string, BehaviorPattern>();
        private Queue<AnalysisRequest> _behaviorAnalysisQueue = new Queue<AnalysisRequest>();

        // Events
        public event Action<BehaviorPattern> OnBehaviorPatternIdentified;
        public event Action<string, float> OnEngagementScoreUpdated;
        public event Action<PlayerJourneyInsight> OnPlayerJourneyAnalyzed;

        public BehaviorAnalysisEngine(AnalyticsCore analyticsCore)
        {
            _analyticsCore = analyticsCore;
            _behaviorAnalyzer = new BehaviorAnalyzer(_analyticsCore.DataWindowSize);
            
            // Configure behavior analyzer
            _behaviorAnalyzer.EnablePatternDetection(_analyticsCore.EnableBehaviorAnalysis);
            
            ChimeraLogger.Log("[BehaviorAnalysisEngine] Behavior analysis engine initialized");
        }

        /// <summary>
        /// Analyze player behavior patterns
        /// </summary>
        public BehaviorPattern AnalyzeBehaviorPattern(string playerId, TimeSpan analysisWindow)
        {
            if (!_analyticsCore.EnableBehaviorAnalysis)
                return null;

            try
            {
                var playerEvents = _behaviorEventHistory
                    .Where(e => e.PlayerId == playerId &&
                               DateTime.UtcNow - e.Timestamp <= analysisWindow)
                    .ToList();

                var pattern = _behaviorAnalyzer.AnalyzePattern(playerId, playerEvents);

                if (pattern != null)
                {
                    _identifiedPatterns[playerId] = pattern;
                    _analyticsCore.UpdateMetrics(m => m.BehaviorPatternsIdentified++);
                    OnBehaviorPatternIdentified?.Invoke(pattern);
                    
                    ChimeraLogger.Log($"[BehaviorAnalysisEngine] Identified behavior pattern for player {playerId}: {pattern.PatternType}");
                }

                return pattern;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[BehaviorAnalysisEngine] Behavior analysis failed for player {playerId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculate engagement score for a player
        /// </summary>
        public float CalculateEngagementScore(string playerId, TimeSpan timeWindow)
        {
            try
            {
                var playerEvents = GetPlayerEventsInWindow(playerId, timeWindow);
                
                if (playerEvents.Count == 0)
                    return 0f;

                // Calculate engagement metrics
                var sessionCount = CountUniqueSessions(playerEvents);
                var averageSessionLength = CalculateAverageSessionLength(playerEvents);
                var featureUsageScore = CalculateFeatureUsageScore(playerEvents);
                var retentionScore = CalculateRetentionScore(playerEvents, timeWindow);
                
                // Weighted engagement score
                var engagementScore = (sessionCount * 0.2f) + 
                                     (averageSessionLength * 0.3f) + 
                                     (featureUsageScore * 0.3f) + 
                                     (retentionScore * 0.2f);

                engagementScore = Mathf.Clamp01(engagementScore / 100f); // Normalize to 0-1
                
                OnEngagementScoreUpdated?.Invoke(playerId, engagementScore);
                
                return engagementScore;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[BehaviorAnalysisEngine] Engagement calculation failed for player {playerId}: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Analyze player journey and progression patterns
        /// </summary>
        public PlayerJourneyInsight AnalyzePlayerJourney(string playerId)
        {
            try
            {
                var playerEvents = _behaviorEventHistory
                    .Where(e => e.PlayerId == playerId)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                if (playerEvents.Count == 0)
                    return null;

                var insight = new PlayerJourneyInsight
                {
                    PlayerId = playerId,
                    AnalyzedAt = DateTime.UtcNow,
                    FirstSessionDate = playerEvents.First().Timestamp,
                    LastSessionDate = playerEvents.Last().Timestamp,
                    TotalSessions = CountUniqueSessions(playerEvents),
                    TotalPlayTime = CalculateTotalPlayTime(playerEvents),
                    MostUsedFeatures = IdentifyMostUsedFeatures(playerEvents),
                    ProgressionMilestones = IdentifyProgressionMilestones(playerEvents),
                    EngagementTrend = AnalyzeEngagementTrend(playerEvents),
                    ChurnRisk = CalculateChurnRisk(playerEvents)
                };

                OnPlayerJourneyAnalyzed?.Invoke(insight);
                
                return insight;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[BehaviorAnalysisEngine] Player journey analysis failed for player {playerId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Queue behavior analysis request
        /// </summary>
        public void QueueBehaviorAnalysis(string playerId, BehaviorAnalysisType analysisType, Dictionary<string, object> parameters = null)
        {
            var request = new AnalysisRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                ModelId = "player_behavior",
                InputData = new Dictionary<string, object>
                {
                    ["player_id"] = playerId,
                    ["analysis_type"] = analysisType.ToString(),
                    ["parameters"] = parameters ?? new Dictionary<string, object>()
                },
                Priority = AnalyticsPriority.Normal,
                RequestedAt = DateTime.UtcNow
            };

            _behaviorAnalysisQueue.Enqueue(request);
        }

        /// <summary>
        /// Process queued behavior analysis requests
        /// </summary>
        public void ProcessBehaviorAnalysisQueue()
        {
            var processedRequests = 0;
            const int maxRequestsPerFrame = 3;

            while (_behaviorAnalysisQueue.Count > 0 && processedRequests < maxRequestsPerFrame)
            {
                var request = _behaviorAnalysisQueue.Dequeue();

                try
                {
                    ProcessBehaviorAnalysisRequest(request);
                    processedRequests++;
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[BehaviorAnalysisEngine] Failed to process analysis request: {ex.Message}");
                }
            }
        }

        private void ProcessBehaviorAnalysisRequest(AnalysisRequest request)
        {
            if (!request.InputData.TryGetValue("player_id", out var playerIdObj) ||
                !request.InputData.TryGetValue("analysis_type", out var analysisTypeObj))
            {
                return;
            }

            var playerId = playerIdObj.ToString();
            var analysisType = Enum.Parse<BehaviorAnalysisType>(analysisTypeObj.ToString());

            switch (analysisType)
            {
                case BehaviorAnalysisType.PatternDetection:
                    AnalyzeBehaviorPattern(playerId, TimeSpan.FromHours(24));
                    break;
                    
                case BehaviorAnalysisType.EngagementScoring:
                    CalculateEngagementScore(playerId, TimeSpan.FromDays(7));
                    break;
                    
                case BehaviorAnalysisType.PlayerJourney:
                    AnalyzePlayerJourney(playerId);
                    break;
            }
        }

        // Data event handling
        public void OnDataEventCollected(DataEvent dataEvent)
        {
            // Convert to behavior-specific analytics event
            var behaviorEvent = new AnalyticsEvent
            {
                EventId = dataEvent.EventId,
                EventType = dataEvent.EventType,
                PlayerId = dataEvent.UserId,
                SessionId = dataEvent.SessionId,
                Timestamp = dataEvent.Timestamp,
                Category = ExtractBehaviorCategory(dataEvent),
                Data = dataEvent.Data as Dictionary<string, object> ?? new Dictionary<string, object>(),
                Metadata = dataEvent.Metadata as Dictionary<string, object> ?? new Dictionary<string, object>()
            };

            _behaviorEventHistory.Add(behaviorEvent);

            // Maintain behavior history size
            if (_behaviorEventHistory.Count > _analyticsCore.DataWindowSize * 10)
            {
                _behaviorEventHistory.RemoveRange(0, _analyticsCore.DataWindowSize);
            }

            // Perform real-time behavior analysis
            if (_analyticsCore.EnableAnalytics)
            {
                PerformRealTimeBehaviorAnalysis(behaviorEvent);
            }
        }

        private string ExtractBehaviorCategory(DataEvent dataEvent)
        {
            // Categorize events for behavior analysis
            return dataEvent.EventType switch
            {
                "user_action" => "interaction",
                "session_start" => "session",
                "session_end" => "session",
                "feature_usage" => "engagement",
                "error" => "friction",
                _ => "general"
            };
        }

        private void PerformRealTimeBehaviorAnalysis(AnalyticsEvent behaviorEvent)
        {
            // Check for immediate behavior patterns
            if (behaviorEvent.EventType == "session_start")
            {
                // Update engagement metrics in real-time
                var recentEngagement = CalculateEngagementScore(behaviorEvent.PlayerId, TimeSpan.FromHours(1));
                OnEngagementScoreUpdated?.Invoke(behaviorEvent.PlayerId, recentEngagement);
            }
        }

        // Helper methods
        private List<AnalyticsEvent> GetPlayerEventsInWindow(string playerId, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow - timeWindow;
            return _behaviorEventHistory
                .Where(e => e.PlayerId == playerId && e.Timestamp >= cutoffTime)
                .ToList();
        }

        private int CountUniqueSessions(List<AnalyticsEvent> events)
        {
            return events.Select(e => e.SessionId).Distinct().Count();
        }

        private float CalculateAverageSessionLength(List<AnalyticsEvent> events)
        {
            var sessionEvents = events.GroupBy(e => e.SessionId);
            var sessionLengths = new List<float>();

            foreach (var session in sessionEvents)
            {
                var sessionEvents_list = session.OrderBy(e => e.Timestamp).ToList();
                if (sessionEvents_list.Count > 1)
                {
                    var duration = (sessionEvents_list.Last().Timestamp - sessionEvents_list.First().Timestamp).TotalMinutes;
                    sessionLengths.Add((float)duration);
                }
            }

            return sessionLengths.Count > 0 ? sessionLengths.Average() : 0f;
        }

        private float CalculateFeatureUsageScore(List<AnalyticsEvent> events)
        {
            var featureUsageEvents = events.Where(e => e.Category == "engagement").ToList();
            var uniqueFeatures = featureUsageEvents.Select(e => e.EventType).Distinct().Count();
            
            // Score based on feature diversity (0-100)
            return Mathf.Min(uniqueFeatures * 10f, 100f);
        }

        private float CalculateRetentionScore(List<AnalyticsEvent> events, TimeSpan timeWindow)
        {
            var totalDays = timeWindow.TotalDays;
            var activeDays = events.Select(e => e.Timestamp.Date).Distinct().Count();
            
            return totalDays > 0 ? (float)(activeDays / totalDays) * 100f : 0f;
        }

        private TimeSpan CalculateTotalPlayTime(List<AnalyticsEvent> events)
        {
            // Simplified calculation based on session events
            var sessionGroups = events.GroupBy(e => e.SessionId);
            var totalMinutes = 0.0;

            foreach (var session in sessionGroups)
            {
                var sessionEvents_list = session.OrderBy(e => e.Timestamp).ToList();
                if (sessionEvents_list.Count > 1)
                {
                    totalMinutes += (sessionEvents_list.Last().Timestamp - sessionEvents_list.First().Timestamp).TotalMinutes;
                }
            }

            return TimeSpan.FromMinutes(totalMinutes);
        }

        private List<string> IdentifyMostUsedFeatures(List<AnalyticsEvent> events)
        {
            return events
                .Where(e => e.Category == "engagement")
                .GroupBy(e => e.EventType)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();
        }

        private List<string> IdentifyProgressionMilestones(List<AnalyticsEvent> events)
        {
            return events
                .Where(e => e.EventType.Contains("milestone") || e.EventType.Contains("achievement"))
                .Select(e => e.EventType)
                .Distinct()
                .ToList();
        }

        private string AnalyzeEngagementTrend(List<AnalyticsEvent> events)
        {
            if (events.Count < 2) return "insufficient_data";

            // Simple trend analysis based on event frequency over time
            var halfPoint = events.Count / 2;
            var firstHalf = events.Take(halfPoint).Count();
            var secondHalf = events.Skip(halfPoint).Count();

            if (secondHalf > firstHalf * 1.2f) return "increasing";
            if (secondHalf < firstHalf * 0.8f) return "decreasing";
            return "stable";
        }

        private float CalculateChurnRisk(List<AnalyticsEvent> events)
        {
            if (events.Count == 0) return 1.0f;

            var lastActivity = events.Max(e => e.Timestamp);
            var daysSinceLastActivity = (DateTime.UtcNow - lastActivity).TotalDays;

            // Higher churn risk with more days of inactivity
            return Mathf.Clamp01((float)daysSinceLastActivity / 30f);
        }

        public void CleanupOldData(int retentionDays)
        {
            var cutoffTime = DateTime.UtcNow.AddDays(-retentionDays);
            _behaviorEventHistory.RemoveAll(e => e.Timestamp < cutoffTime);
            
            var oldPatterns = _identifiedPatterns
                .Where(kvp => DateTime.UtcNow - kvp.Value.IdentifiedAt > TimeSpan.FromDays(retentionDays))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldPatterns)
            {
                _identifiedPatterns.Remove(key);
            }
        }

        public int GetEventHistorySize() => _behaviorEventHistory.Count;
        public int GetQueueSize() => _behaviorAnalysisQueue.Count;
        public BehaviorPattern GetIdentifiedPattern(string playerId) => _identifiedPatterns.TryGetValue(playerId, out var pattern) ? pattern : null;
    }

    public enum BehaviorAnalysisType
    {
        PatternDetection,
        EngagementScoring,
        PlayerJourney
    }
}