using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Contract notification data
    /// </summary>
    [System.Serializable]
    public class ContractNotificationData
    {
        public string NotificationId = "";
        public string ContractId = "";
        public ContractNotificationType Type = ContractNotificationType.Progress;
        public ContractNotificationSeverity Severity = ContractNotificationSeverity.Info;
        public string Title = "";
        public string Message = "";
        public DateTime Timestamp = DateTime.Now;
        public bool IsRead = false;
        public Dictionary<string, object> Data = new Dictionary<string, object>();
        public string ActionUrl = "";
        
        public ContractNotificationData()
        {
            NotificationId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Contract notification types
    /// </summary>
    public enum ContractNotificationType
    {
        Progress = 0,
        Deadline = 1,
        Quality = 2,
        Completion = 3,
        Delivery = 4,
        Payment = 5,
        Alert = 6,
        Warning = 7,
        Error = 8
    }

    /// <summary>
    /// Contract notification severity levels
    /// </summary>
    public enum ContractNotificationSeverity
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}