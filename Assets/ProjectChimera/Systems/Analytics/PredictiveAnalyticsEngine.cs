using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Specialized engine for predictive analytics and machine learning insights.
    /// Handles prediction generation, model training, and trend forecasting.
    /// </summary>
    public class PredictiveAnalyticsEngine
    {
        private readonly AnalyticsCore _analyticsCore;
        private readonly PredictiveEngine _predictiveEngine;

        // Prediction tracking
        private List<PredictionResult> _predictionHistory = new List<PredictionResult>();
        private Dictionary<string, PredictionModel> _predictionModels = new Dictionary<string, PredictionModel>();
        private Queue<PredictionRequest> _predictionQueue = new Queue<PredictionRequest>();

        // Machine learning settings
        private bool _enableMLInsights;
        private bool _enableReinforcementLearning;
        private int _mlModelUpdateInterval;
        private float _predictionConfidenceThreshold;

        // Events
        public event Action<PredictionResult> OnPredictionMade;
        public event Action<TrendForecast> OnTrendForecastGenerated;
        public event Action<MLInsight> OnMLInsightGenerated;

        public PredictiveAnalyticsEngine(AnalyticsCore analyticsCore)
        {
            _analyticsCore = analyticsCore;
            _enableMLInsights = false; // Default off, requires ML packages
            _enableReinforcementLearning = false;
            _mlModelUpdateInterval = 3600; // 1 hour
            _predictionConfidenceThreshold = 0.7f;

            _predictiveEngine = new PredictiveEngine(_enableMLInsights);

            // Configure predictive engine
            _predictiveEngine.EnableReinforcementLearning(_enableReinforcementLearning);

            InitializePredictionModels();

            ChimeraLogger.Log("[PredictiveAnalyticsEngine] Predictive analytics engine initialized");
        }

        private void InitializePredictionModels()
        {
            // Player churn prediction model
            RegisterPredictionModel(new PredictionModel
            {
                ModelId = "player_churn",
                ModelType = "classification",
                Description = "Predicts player churn probability",
                InputFeatures = new List<string> { "session_frequency", "engagement_score", "last_activity_days", "feature_adoption" },
                OutputClass = "churn_probability",
                AccuracyScore = 0.85f,
                TrainingDataSize = 1000,
                LastTrained = DateTime.UtcNow.AddDays(-7),
                IsActive = true
            });

            // Performance degradation prediction
            RegisterPredictionModel(new PredictionModel
            {
                ModelId = "performance_degradation",
                ModelType = "regression",
                Description = "Predicts system performance degradation",
                InputFeatures = new List<string> { "memory_usage_trend", "cpu_usage_trend", "fps_trend", "error_rate_trend" },
                OutputClass = "performance_score",
                AccuracyScore = 0.78f,
                TrainingDataSize = 500,
                LastTrained = DateTime.UtcNow.AddDays(-3),
                IsActive = true
            });

            // Economy balance prediction
            RegisterPredictionModel(new PredictionModel
            {
                ModelId = "economy_balance",
                ModelType = "regression",
                Description = "Predicts economy balance and inflation trends",
                InputFeatures = new List<string> { "resource_flow", "trade_volume", "player_wealth_distribution", "market_activity" },
                OutputClass = "balance_score",
                AccuracyScore = 0.72f,
                TrainingDataSize = 750,
                LastTrained = DateTime.UtcNow.AddDays(-5),
                IsActive = true
            });
        }

        /// <summary>
        /// Make predictions based on current data
        /// </summary>
        public async Task<PredictionResult> MakePrediction(string modelId, Dictionary<string, object> currentState)
        {
            if (!_analyticsCore.EnablePredictiveAnalytics)
                return PredictionResult.CreateFailed("Predictive analytics disabled");

            if (!_predictionModels.TryGetValue(modelId, out var model))
            {
                return PredictionResult.CreateFailed($"Model {modelId} not found");
            }

            try
            {
                var prediction = _predictiveEngine.Predict(currentState, model.ModelType);

                if (prediction.Confidence >= _predictionConfidenceThreshold)
                {
                    _predictionHistory.Add(prediction);
                    _analyticsCore.UpdateMetrics(m =>
                    {
                        m.PredictionsMade++;
                        m.SuccessfulPredictions++;
                    });

                    OnPredictionMade?.Invoke(prediction);

                    ChimeraLogger.Log($"[PredictiveAnalyticsEngine] High-confidence prediction made for {modelId}: {prediction.PredictedValue}");
                }

                return prediction;
            }
            catch (Exception ex)
            {
                _analyticsCore.UpdateMetrics(m => m.PredictionsMade++);
                ChimeraLogger.LogError($"[PredictiveAnalyticsEngine] Prediction failed for {modelId}: {ex.Message}");
                return PredictionResult.CreateFailed(ex.Message);
            }
        }

        /// <summary>
        /// Generate trend forecast based on historical data
        /// </summary>
        public TrendForecast GenerateTrendForecast(string dataSource, TimeSpan forecastWindow, List<DataPoint> historicalData)
        {
            try
            {
                if (historicalData.Count < 10)
                {
                    return null; // Insufficient data for forecasting
                }

                var forecast = new TrendForecast
                {
                    DataSource = dataSource,
                    ForecastWindow = forecastWindow,
                    GeneratedAt = DateTime.UtcNow,
                    HistoricalDataPoints = historicalData,
                    TrendDirection = AnalyzeTrendDirection(historicalData),
                    ForecastedValues = GenerateForecastedValues(historicalData, forecastWindow),
                    ConfidenceInterval = new Dictionary<string, float> { ["default"] = CalculateConfidenceInterval(historicalData) },
                    Seasonality = DetectSeasonality(historicalData) ? 1.0f : 0.0f,
                    Volatility = CalculateVolatility(historicalData)
                };

                OnTrendForecastGenerated?.Invoke(forecast);

                return forecast;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PredictiveAnalyticsEngine] Trend forecast failed for {dataSource}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate ML-based insights
        /// </summary>
        public async Task<MLInsight> GenerateMLInsight(string modelId, Dictionary<string, object> featureData)
        {
            if (!_enableMLInsights)
            {
                return MLInsight.CreateDisabled("ML insights are disabled");
            }

            try
            {
                // Simulate ML insight generation
                await Task.Delay(100); // Simulate processing time

                var insight = new MLInsight
                {
                    InsightId = Guid.NewGuid().ToString(),
                    ModelId = modelId,
                    GeneratedAt = DateTime.UtcNow,
                    InsightType = DetermineInsightType(modelId),
                    Confidence = UnityEngine.Random.Range(0.6f, 0.95f),
                    KeyFindings = GenerateKeyFindings(modelId, featureData),
                    FeatureImportance = CalculateFeatureImportance(featureData),
                    Recommendations = GenerateMLRecommendations(modelId),
                    PredictedOutcome = GeneratePredictedOutcome(modelId, featureData)?.ToString() ?? "Unknown"
                };

                OnMLInsightGenerated?.Invoke(insight);

                return insight;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PredictiveAnalyticsEngine] ML insight generation failed for {modelId}: {ex.Message}");
                return MLInsight.CreateFailed(ex.Message);
            }
        }

        /// <summary>
        /// Queue prediction request
        /// </summary>
        public void QueuePrediction(string modelId, Dictionary<string, object> inputData, PredictionPriority priority = PredictionPriority.Normal)
        {
            var request = new PredictionRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                ModelId = modelId,
                InputData = inputData,
                Priority = priority,
                RequestedAt = DateTime.UtcNow
            };

            _predictionQueue.Enqueue(request);
        }

        /// <summary>
        /// Process queued prediction requests
        /// </summary>
        public async Task ProcessPredictionQueue()
        {
            var processedRequests = 0;
            const int maxRequestsPerFrame = 2; // Lower limit for async operations

            while (_predictionQueue.Count > 0 && processedRequests < maxRequestsPerFrame)
            {
                var request = _predictionQueue.Dequeue();

                try
                {
                    var result = await MakePrediction(request.ModelId, request.InputData);
                    request.Result = result;
                    request.IsCompleted = true;
                    request.CompletedAt = DateTime.UtcNow;

                    processedRequests++;
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[PredictiveAnalyticsEngine] Failed to process prediction request: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Train or retrain prediction model
        /// </summary>
        public async Task<bool> TrainModel(string modelId, List<TrainingDataPoint> trainingData)
        {
            if (!_predictionModels.TryGetValue(modelId, out var model))
            {
                ChimeraLogger.LogError($"[PredictiveAnalyticsEngine] Cannot train model {modelId}: model not found");
                return false;
            }

            try
            {
                // Simulate model training
                await Task.Delay(500);

                model.LastTrained = DateTime.UtcNow;
                model.TrainingDataSize = trainingData.Count;
                model.AccuracyScore = UnityEngine.Random.Range(0.7f, 0.95f);

                ChimeraLogger.Log($"[PredictiveAnalyticsEngine] Model {modelId} retrained with {trainingData.Count} data points");
                return true;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PredictiveAnalyticsEngine] Model training failed for {modelId}: {ex.Message}");
                return false;
            }
        }

        private void RegisterPredictionModel(PredictionModel model)
        {
            _predictionModels[model.ModelId] = model;
            ChimeraLogger.Log($"[PredictiveAnalyticsEngine] Registered prediction model: {model.ModelId}");
        }

        private string AnalyzeTrendDirection(List<DataPoint> data)
        {
            if (data.Count < 2) return "unknown";

            var firstHalf = data.Take(data.Count / 2).Average(d => d.Value);
            var secondHalf = data.Skip(data.Count / 2).Average(d => d.Value);

            if (secondHalf > firstHalf * 1.1) return "increasing";
            if (secondHalf < firstHalf * 0.9) return "decreasing";
            return "stable";
        }

        private List<float> GenerateForecastedValues(List<DataPoint> historicalData, TimeSpan forecastWindow)
        {
            var forecastPoints = (int)(forecastWindow.TotalHours / 24); // Daily predictions
            var lastValue = historicalData.Last().Value;
            var trend = CalculateTrend(historicalData);

            var forecasted = new List<float>();

            for (int i = 1; i <= forecastPoints; i++)
            {
                var forecastedValue = lastValue + (trend * i) + UnityEngine.Random.Range(-0.1f, 0.1f);
                forecasted.Add(forecastedValue);
            }

            return forecasted;
        }

        private float CalculateTrend(List<DataPoint> data)
        {
            if (data.Count < 2) return 0f;

            // Simple linear trend calculation
            var values = data.Select(d => d.Value).ToList();
            var n = values.Count;
            var sumX = n * (n + 1) / 2; // Sum of indices
            var sumY = values.Sum();
            var sumXY = values.Select((y, x) => (x + 1) * y).Sum();
            var sumX2 = n * (n + 1) * (2 * n + 1) / 6; // Sum of squared indices

            return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        }

        private float CalculateConfidenceInterval(List<DataPoint> data)
        {
            if (data.Count < 3) return 0.5f;

            var values = data.Select(d => d.Value).ToArray();
            var mean = values.Average();
            var variance = values.Sum(v => Mathf.Pow(v - mean, 2)) / (values.Length - 1);
            var standardDeviation = Mathf.Sqrt(variance);

            // 95% confidence interval approximation
            return 1.96f * standardDeviation / Mathf.Sqrt(values.Length);
        }

        private bool DetectSeasonality(List<DataPoint> data)
        {
            // Simple seasonality detection based on periodic patterns
            if (data.Count < 14) return false; // Need at least 2 weeks of data

            // Check for weekly patterns (simplified)
            var weeklyPattern = data.Take(7).Select(d => d.Value).ToArray();
            var nextWeekPattern = data.Skip(7).Take(7).Select(d => d.Value).ToArray();

            if (weeklyPattern.Length == nextWeekPattern.Length)
            {
                var correlation = CalculateCorrelation(weeklyPattern, nextWeekPattern);
                return correlation > 0.7f; // Strong correlation indicates seasonality
            }

            return false;
        }

        private float CalculateCorrelation(float[] x, float[] y)
        {
            if (x.Length != y.Length) return 0f;

            var meanX = x.Average();
            var meanY = y.Average();

            var numerator = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
            var denominatorX = Mathf.Sqrt(x.Sum(xi => Mathf.Pow(xi - meanX, 2)));
            var denominatorY = Mathf.Sqrt(y.Sum(yi => Mathf.Pow(yi - meanY, 2)));

            return denominatorX * denominatorY != 0 ? numerator / (denominatorX * denominatorY) : 0f;
        }

        private float CalculateVolatility(List<DataPoint> data)
        {
            if (data.Count < 2) return 0f;

            var values = data.Select(d => d.Value).ToArray();
            var mean = values.Average();
            var variance = values.Sum(v => Mathf.Pow(v - mean, 2)) / values.Length;

            return Mathf.Sqrt(variance);
        }

        private MLInsightType DetermineInsightType(string modelId)
        {
            return modelId switch
            {
                "player_churn" => MLInsightType.PlayerBehavior,
                "performance_degradation" => MLInsightType.SystemPerformance,
                "economy_balance" => MLInsightType.GameBalance,
                _ => MLInsightType.General
            };
        }

        private List<string> GenerateKeyFindings(string modelId, Dictionary<string, object> featureData)
        {
            return modelId switch
            {
                "player_churn" => new List<string>
                {
                    "Low engagement score indicates higher churn risk",
                    "Recent activity patterns show concerning trends",
                    "Feature adoption rate below average"
                },
                "performance_degradation" => new List<string>
                {
                    "Memory usage trending upward over time",
                    "Frame rate stability shows degradation pattern",
                    "CPU usage spikes correlate with specific operations"
                },
                _ => new List<string> { "Analysis completed successfully" }
            };
        }

        private Dictionary<string, float> CalculateFeatureImportance(Dictionary<string, object> featureData)
        {
            var importance = new Dictionary<string, float>();

            foreach (var feature in featureData.Keys)
            {
                // Mock feature importance (would be calculated by ML model)
                importance[feature] = UnityEngine.Random.Range(0.1f, 1.0f);
            }

            return importance;
        }

        private List<string> GenerateMLRecommendations(string modelId)
        {
            return modelId switch
            {
                "player_churn" => new List<string>
                {
                    "Implement targeted retention campaigns",
                    "Provide personalized content recommendations",
                    "Offer progression assistance to at-risk players"
                },
                "performance_degradation" => new List<string>
                {
                    "Schedule proactive memory cleanup",
                    "Optimize rendering for lower-end devices",
                    "Implement adaptive quality settings"
                },
                _ => new List<string> { "Continue monitoring key metrics" }
            };
        }

        private object GeneratePredictedOutcome(string modelId, Dictionary<string, object> featureData)
        {
            return modelId switch
            {
                "player_churn" => new { churn_probability = UnityEngine.Random.Range(0.1f, 0.9f), risk_level = "medium" },
                "performance_degradation" => new { performance_score = UnityEngine.Random.Range(0.3f, 0.9f), trend = "declining" },
                "economy_balance" => new { balance_score = UnityEngine.Random.Range(0.5f, 0.95f), stability = "stable" },
                _ => new { prediction = "no_specific_outcome" }
            };
        }

        // Event handlers
        public void OnDataEventCollected(DataEvent dataEvent)
        {
            // Use events for model training and prediction updates
            if (_enableMLInsights && dataEvent.EventType == "player_action")
            {
                var features = ExtractFeaturesFromEvent(dataEvent);
                QueuePrediction("player_churn", features, PredictionPriority.Low);
            }
        }

        private Dictionary<string, object> ExtractFeaturesFromEvent(DataEvent dataEvent)
        {
            return new Dictionary<string, object>
            {
                ["event_type"] = dataEvent.EventType,
                ["timestamp"] = dataEvent.Timestamp,
                ["user_id"] = dataEvent.UserId,
                ["session_id"] = dataEvent.SessionId
            };
        }

        public void CleanupOldData(int retentionDays)
        {
            var cutoffTime = DateTime.UtcNow.AddDays(-retentionDays);
            _predictionHistory.RemoveAll(p => p.GeneratedAt < cutoffTime);
        }

        public int GetPredictionHistorySize() => _predictionHistory.Count;
        public int GetQueueSize() => _predictionQueue.Count;
        public PredictionModel GetPredictionModel(string modelId) => _predictionModels.TryGetValue(modelId, out var model) ? model : null;
        public void SetMLInsightsEnabled(bool enabled) => _enableMLInsights = enabled;
        public void SetReinforcementLearningEnabled(bool enabled) => _enableReinforcementLearning = enabled;
    }

    public class PredictionRequest
    {
        public string RequestId { get; set; }
        public string ModelId { get; set; }
        public Dictionary<string, object> InputData { get; set; }
        public PredictionPriority Priority { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }
        public PredictionResult Result { get; set; }
    }

    public enum PredictionPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}
