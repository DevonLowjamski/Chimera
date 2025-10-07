using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Save.Storage
{
    /// <summary>
    /// Represents a save transaction for atomic operations
    /// </summary>
    public class SaveTransaction
    {
        public string TransactionId { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<ProjectChimera.Data.Save.FileOperation> Operations { get; set; } = new List<ProjectChimera.Data.Save.FileOperation>();
        public bool IsComplete { get; set; } = false;
        public bool IsRolledBack { get; set; } = false;
    }

    /// <summary>
    /// Transaction status enum
    /// </summary>
    public enum TransactionStatus
    {
        Pending,
        Active,
        Committing,
        Committed,
        Failed,
        RollingBack,
        RolledBack
    }

    /// <summary>
    /// Represents an operation within a transaction
    /// Alias for FileOperation to maintain compatibility
    /// </summary>
    public class TransactionOperation : ProjectChimera.Data.Save.FileOperation
    {
        // Inherits all properties from FileOperation
    }
}
