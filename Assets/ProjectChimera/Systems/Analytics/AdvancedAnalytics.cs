namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// DEPRECATED: Advanced Analytics has been broken down into focused components.
    /// This file now serves as a reference point for the decomposed analytics system structure.
    /// 
    /// New Component Structure:
    /// - AnalyticsCore.cs: Core analytics infrastructure and component coordination
    /// - BehaviorAnalysisEngine.cs: Player behavior patterns and engagement metrics
    /// - AnomalyDetectionEngine.cs: System anomaly detection and health monitoring
    /// - PredictiveAnalyticsEngine.cs: Predictive analytics and machine learning insights
    /// - ReportingEngine.cs: Report generation and alert management
    /// </summary>
    
    // The AdvancedAnalytics functionality has been moved to focused component files.
    // This file is kept for reference and to prevent breaking changes during migration.
    // 
    // To use the new component structure, inherit from AnalyticsCore:
    // 
    // public class AdvancedAnalytics : AnalyticsCore
    // {
    //     // Your custom advanced analytics implementation
    // }
    // 
    // The following classes are now available in their focused components:
    // 
    // From AnalyticsCore.cs:
    // - AnalyticsCore (base class with core functionality)
    // - Analytics model registration and management
    // - Component orchestration and coordination
    // 
    // From BehaviorAnalysisEngine.cs:
    // - BehaviorAnalysisEngine (player behavior pattern analysis)
    // - Engagement scoring and player journey analysis
    // - Behavior pattern detection and tracking
    // 
    // From AnomalyDetectionEngine.cs:
    // - AnomalyDetectionEngine (system anomaly detection)
    // - Performance monitoring and health analysis
    // - Alert generation and system health profiling
    // 
    // From PredictiveAnalyticsEngine.cs:
    // - PredictiveAnalyticsEngine (predictive analytics and ML)
    // - Trend forecasting and prediction generation
    // - Machine learning insights and model training
    // 
    // From ReportingEngine.cs:
    // - ReportingEngine (comprehensive reporting and alerts)
    // - Automated report generation and export
    // - Alert management and notification systems
    
    /// <summary>
    /// Concrete implementation of AdvancedAnalytics using the new component structure.
    /// Inherits all functionality from AnalyticsCore and specialized engines.
    /// </summary>
    public class AdvancedAnalytics : AnalyticsCore
    {
        // This class inherits all functionality from AnalyticsCore
        // Individual analytics engines are automatically initialized in the base class
        // Add any custom AdvancedAnalytics-specific functionality here if needed
        
        /// <summary>
        /// Legacy support methods for backward compatibility
        /// </summary>
        public new void CollectEvent(string eventType, string action, object data)
        {
            _behaviorEngine?.OnDataEventCollected(new DataEvent 
            { 
                EventId = System.Guid.NewGuid().ToString(),
                EventType = eventType, 
                Data = data, 
                Timestamp = System.DateTime.UtcNow 
            });
        }
        
        public new void CollectEvent(string category, string eventType, object eventData, System.Collections.Generic.Dictionary<string, object> metadata = null)
        {
            _behaviorEngine?.OnDataEventCollected(new DataEvent 
            { 
                EventId = System.Guid.NewGuid().ToString(),
                EventType = eventType, 
                Data = eventData,
                Metadata = metadata,
                Timestamp = System.DateTime.UtcNow 
            });
        }
    }
}
