using System.Collections.Generic;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Interface for event analytics services
    /// </summary>
    public interface IEventAnalytics
    {
        int CollectorCount { get; }
        void RecordEvent(string eventName, Dictionary<string, object> eventData = null);
        void Initialize();
        void AddMetricCollector(string key, IMetricCollector collector);
        IMetricCollector GetMetricCollector(string key);
        void ProcessMetrical();
        void Shutdown();
    }
}
