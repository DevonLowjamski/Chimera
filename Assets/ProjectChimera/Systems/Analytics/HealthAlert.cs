using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Health alert data structure
    /// </summary>
    public class HealthAlert
    {
        public string AlertId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string Severity { get; set; }

        public HealthAlert()
        {
            AlertId = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
            Severity = "Info";
        }
    }
}
