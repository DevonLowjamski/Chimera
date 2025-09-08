using UnityEngine;
using ProjectChimera.Data.Economy;
using System;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Events
{
    /// <summary>
    /// Event data structure for contract-related events
    /// Contains all relevant information about contract state changes
    /// </summary>
    [System.Serializable]
    public class ContractEventData
    {
        [Header("Contract Identity")]
        public string ContractId;
        public string ContractTitle;
        public string ClientName;
        
        [Header("Event Details")]
        public ContractEventType EventType;
        public DateTime EventTimestamp;
        public string EventDescription;
        
        [Header("Contract State")]
        public string PreviousStatus;
        public string NewStatus;
        public float CompletionProgress;
        public bool IsAccepted;
        public bool IsActive;
        public bool IsCompleted;
        public bool IsFailed;
        
        [Header("Financial Information")]
        public float BaseReward;
        public float BonusReward;
        public float PenaltyAmount;
        public float FinalPayout;
        
        [Header("Quality Information")]
        public float QualityScore;
        public bool QualityMeetsStandards;
        
        [Header("Timing Information")]
        public DateTime DueTime;
        public DateTime CompletionTime;
        public float TimeRemaining;
        public bool IsOverdue;
        
        [Header("Additional Data")]
        public string AdditionalInfo;
        public object CustomData;
        
        public ContractEventData()
        {
            EventTimestamp = DateTime.Now;
        }
        
        public ContractEventData(string contractId, ContractEventType eventType, string description = "")
        {
            ContractId = contractId;
            EventType = eventType;
            EventDescription = description;
            EventTimestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Contract event types for categorizing different contract state changes
    /// </summary>
    public enum ContractEventType
    {
        // Contract Lifecycle Events
        ContractGenerated,
        ContractOffered,
        ContractAccepted,
        ContractStarted,
        ContractCompleted,
        ContractFailed,
        ContractCancelled,
        ContractExpired,
        
        // Progress Events
        ProgressUpdated,
        MilestoneReached,
        DeliveryMade,
        PartialDelivery,
        
        // Quality Events
        QualityAssessed,
        QualityApproved,
        QualityRejected,
        QualityIssueDetected,
        
        // Financial Events
        PaymentProcessed,
        BonusEarned,
        PenaltyApplied,
        PayoutCalculated,
        
        // Deadline Events
        DeadlineWarning,
        DeadlineApproaching,
        DeadlineMissed,
        ExtensionGranted,
        
        // Client Events
        ClientFeedback,
        ClientRatingReceived,
        RelationshipChanged,
        
        // System Events
        ContractGeneration,
        MarketDemandChanged,
        SeasonalModifierApplied,
        
        // Error Events
        ValidationError,
        SystemError,
        DataCorruption
    }
    
    /// <summary>
    /// Typed event channel for contract state changes
    /// </summary>
    [CreateAssetMenu(fileName = "ContractEvent", menuName = "Project Chimera/Events/Contract Event")]
    public class ContractEventSO : TypedGameEventSO<ContractEventData>
    {
        [Header("Contract Event Configuration")]
        [SerializeField] protected bool _logEvents = true;
        [SerializeField] protected ContractEventType _specificEventType = ContractEventType.ProgressUpdated;
        [SerializeField] protected bool _filterByEventType = false;
        
        public override void Invoke(ContractEventData data)
        {
            // Filter by event type if enabled
            if (_filterByEventType && data.EventType != _specificEventType)
                return;
            
            // Log event if enabled
            if (_logEvents)
            {
                SharedLogger.Log($"[ContractEvent] {data.EventType}: {data.ContractTitle} ({data.ContractId}) - {data.EventDescription}");
            }
            
            base.Invoke(data);
        }
        
        /// <summary>
        /// Convenience method to invoke with minimal data
        /// </summary>
        public void InvokeSimple(string contractId, ContractEventType eventType, string description = "")
        {
            var eventData = new ContractEventData(contractId, eventType, description);
            Invoke(eventData);
        }
    }
    
    /// <summary>
    /// Specific event for contract acceptance
    /// </summary>
    [CreateAssetMenu(fileName = "ContractAcceptedEvent", menuName = "Project Chimera/Events/Contract Accepted")]
    public class ContractAcceptedEventSO : ContractEventSO
    {
        private void OnEnable()
        {
            _specificEventType = ContractEventType.ContractAccepted;
            _filterByEventType = true;
        }
    }
    
    /// <summary>
    /// Specific event for contract completion
    /// </summary>
    [CreateAssetMenu(fileName = "ContractCompletedEvent", menuName = "Project Chimera/Events/Contract Completed")]
    public class ContractCompletedEventSO : ContractEventSO
    {
        private void OnEnable()
        {
            _specificEventType = ContractEventType.ContractCompleted;
            _filterByEventType = true;
        }
    }
    
    /// <summary>
    /// Specific event for contract failures
    /// </summary>
    [CreateAssetMenu(fileName = "ContractFailedEvent", menuName = "Project Chimera/Events/Contract Failed")]
    public class ContractFailedEventSO : ContractEventSO
    {
        private void OnEnable()
        {
            _specificEventType = ContractEventType.ContractFailed;
            _filterByEventType = true;
        }
    }
    
    /// <summary>
    /// Specific event for deadline warnings
    /// </summary>
    [CreateAssetMenu(fileName = "ContractDeadlineWarningEvent", menuName = "Project Chimera/Events/Contract Deadline Warning")]
    public class ContractDeadlineWarningEventSO : ContractEventSO
    {
        private void OnEnable()
        {
            _specificEventType = ContractEventType.DeadlineWarning;
            _filterByEventType = true;
        }
    }
    
    /// <summary>
    /// Specific event for progress updates
    /// </summary>
    [CreateAssetMenu(fileName = "ContractProgressEvent", menuName = "Project Chimera/Events/Contract Progress")]
    public class ContractProgressEventSO : ContractEventSO
    {
        private void OnEnable()
        {
            _specificEventType = ContractEventType.ProgressUpdated;
            _filterByEventType = true;
        }
    }
    
    /// <summary>
    /// Specific event for quality assessments
    /// </summary>
    [CreateAssetMenu(fileName = "ContractQualityEvent", menuName = "Project Chimera/Events/Contract Quality")]
    public class ContractQualityEventSO : ContractEventSO
    {
        private void OnEnable()
        {
            _specificEventType = ContractEventType.QualityAssessed;
            _filterByEventType = true;
        }
    }
    
    /// <summary>
    /// Specific event for payment processing
    /// </summary>
    [CreateAssetMenu(fileName = "ContractPaymentEvent", menuName = "Project Chimera/Events/Contract Payment")]
    public class ContractPaymentEventSO : ContractEventSO
    {
        private void OnEnable()
        {
            _specificEventType = ContractEventType.PaymentProcessed;
            _filterByEventType = true;
        }
    }
}