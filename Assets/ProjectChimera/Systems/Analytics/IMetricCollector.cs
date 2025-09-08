using System.Collections.Generic;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Interface for metric collection components
    /// </summary>
    public interface IMetricCollector
    {
        float CollectMetric();
    }
}
