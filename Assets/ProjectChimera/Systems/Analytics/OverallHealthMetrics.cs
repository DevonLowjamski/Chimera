using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Overall health metrics data structure
    /// </summary>
    public class OverallHealthMetrics
    {
        public float OverallScore { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }

        public OverallHealthMetrics()
        {
            OverallScore = 100f;
            Timestamp = DateTime.Now;
            Status = "Healthy";
        }
    }
}
