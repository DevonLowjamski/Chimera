using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Recovery action data structure
    /// </summary>
    public class RecoveryAction
    {
        public string ActionId { get; set; }
        public string ActionType { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Description { get; set; }

        public RecoveryAction()
        {
            ActionId = Guid.NewGuid().ToString();
            ScheduledTime = DateTime.Now;
            ActionType = "Unknown";
        }
    }
}
