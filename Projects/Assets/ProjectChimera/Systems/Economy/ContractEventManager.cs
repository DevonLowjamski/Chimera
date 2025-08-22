using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Events;
using ProjectChimera.Data.Economy;
// using ProjectChimera.UI.Core; // Removed to avoid circular dependency
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Economy
{
    /// <summary>
    /// Central manager for contract event publishing and notification integration
    /// Handles contract state change notifications and event coordination for Phase 8 MVP
    /// </summary>
    public class ContractEventManager : ChimeraManager
    {
        [Header("Contract Event Configuration")]
        [SerializeField] private bool _enableEventLogging = true;
        [SerializeField] private bool _enableNotificationIntegration = true;
        [SerializeField] private bool _enableEventHistory = true;
        [SerializeField] private int _maxEventHistorySize = 1000;
        
        [Header("Event Channels")]
        [SerializeField] private ContractEventSO _globalContractEvent;
        [SerializeField] private ContractAcceptedEventSO _contractAcceptedEvent;
        [SerializeField] private ContractCompletedEventSO _contractCompletedEvent;
        [SerializeField] private ContractFailedEventSO _contractFailedEvent;
        [SerializeField] private ContractDeadlineWarningEventSO _deadlineWarningEvent;
        [SerializeField] private ContractProgressEventSO _progressEvent;
        [SerializeField] private ContractQualityEventSO _qualityEvent;
        [SerializeField] private ContractPaymentEventSO _paymentEvent;
        
        [Header("Notification Settings")]
        [SerializeField] private bool _showProgressNotifications = true;
        [SerializeField] private bool _showDeadlineNotifications = true;
        [SerializeField] private bool _showCompletionNotifications = true;
        [SerializeField] private bool _showQualityNotifications = true;
        [SerializeField] private bool _showPaymentNotifications = true;
        [SerializeField] private float _notificationDisplayDuration = 5f;
        
        [Header("Event Filtering")]
        [SerializeField] private List<ContractEventType> _suppressedEventTypes = new List<ContractEventType>();
        [SerializeField] private bool _enableEventThrottling = true;
        [SerializeField] private float _eventThrottleInterval = 1f;
        
        // Service dependencies
        private object _notificationManager; // Using object to avoid circular dependency
        private CurrencyManager _currencyManager;
        
        // Event management
        private List<ContractEventData> _eventHistory = new List<ContractEventData>();
        private Dictionary<string, DateTime> _lastEventTimes = new Dictionary<string, DateTime>();
        private Dictionary<ContractEventType, int> _eventCounts = new Dictionary<ContractEventType, int>();
        
        // Event statistics
        private int _totalEventsPublished = 0;
        private int _totalNotificationsSent = 0;
        private DateTime _lastEventTime;
        
        public override ManagerPriority Priority => ManagerPriority.Normal;
        
        // Public Properties
        public bool EventLoggingEnabled { get => _enableEventLogging; set => _enableEventLogging = value; }
        public bool NotificationIntegrationEnabled { get => _enableNotificationIntegration; set => _enableNotificationIntegration = value; }
        public int TotalEventsPublished => _totalEventsPublished;
        public int TotalNotificationsSent => _totalNotificationsSent;
        public List<ContractEventData> EventHistory => new List<ContractEventData>(_eventHistory);
        public DateTime LastEventTime => _lastEventTime;
        
        // Events
        public System.Action<ContractEventData> OnAnyContractEvent;
        public System.Action<ContractEventData> OnContractAccepted;
        public System.Action<ContractEventData> OnContractCompleted;
        public System.Action<ContractEventData> OnContractFailed;
        public System.Action<ContractEventData> OnDeadlineWarning;
        public System.Action<ContractEventData> OnProgressUpdate;
        public System.Action<ContractEventData> OnQualityAssessment;
        public System.Action<ContractEventData> OnPaymentProcessed;
        
        protected override void OnManagerInitialize()
        {
            InitializeServices();
            InitializeEventChannels();
            InitializeEventHistory();
            
            LogInfo("ContractEventManager initialized with comprehensive event system");
        }
        
        protected override void OnManagerShutdown()
        {
            CleanupEventChannels();
            SaveEventHistory();
            
            LogInfo($"ContractEventManager shutdown - {_totalEventsPublished} events published, {_totalNotificationsSent} notifications sent");
        }
        
        #region Public API - Event Publishing
        
        /// <summary>
        /// Publish a contract event with full event data
        /// </summary>
        public void PublishContractEvent(ContractEventData eventData)
        {
            if (eventData == null || ShouldSuppressEvent(eventData.EventType))
                return;
            
            if (_enableEventThrottling && IsEventThrottled(eventData))
                return;
            
            // Update statistics
            _totalEventsPublished++;
            _lastEventTime = DateTime.Now;
            UpdateEventCounts(eventData.EventType);
            
            // Add to history
            if (_enableEventHistory)
            {
                AddToEventHistory(eventData);
            }
            
            // Log event
            if (_enableEventLogging)
            {
                LogContractEvent(eventData);
            }
            
            // Publish to appropriate event channels
            PublishToEventChannels(eventData);
            
            // Send notifications
            if (_enableNotificationIntegration)
            {
                SendEventNotification(eventData);
            }
            
            // Invoke local events
            InvokeLocalEvents(eventData);
        }
        
        /// <summary>
        /// Publish a simple contract event with minimal data
        /// </summary>
        public void PublishSimpleEvent(string contractId, ContractEventType eventType, string description = "")
        {
            var eventData = new ContractEventData(contractId, eventType, description);
            PublishContractEvent(eventData);
        }
        
        /// <summary>
        /// Publish contract accepted event
        /// </summary>
        public void PublishContractAccepted(string contractId, string contractTitle, string clientName, float baseReward)
        {
            var eventData = new ContractEventData(contractId, ContractEventType.ContractAccepted, "Contract accepted by player")
            {
                ContractTitle = contractTitle,
                ClientName = clientName,
                BaseReward = baseReward,
                IsAccepted = true,
                IsActive = true,
                NewStatus = "Accepted"
            };
            
            PublishContractEvent(eventData);
        }
        
        /// <summary>
        /// Publish contract completion event
        /// </summary>
        public void PublishContractCompleted(string contractId, string contractTitle, float finalPayout, float qualityScore)
        {
            var eventData = new ContractEventData(contractId, ContractEventType.ContractCompleted, "Contract successfully completed")
            {
                ContractTitle = contractTitle,
                FinalPayout = finalPayout,
                QualityScore = qualityScore,
                IsCompleted = true,
                CompletionProgress = 1f,
                CompletionTime = DateTime.Now,
                NewStatus = "Completed"
            };
            
            PublishContractEvent(eventData);
        }
        
        /// <summary>
        /// Publish contract progress update
        /// </summary>
        public void PublishProgressUpdate(string contractId, string contractTitle, float progress, string milestone = "")
        {
            var eventData = new ContractEventData(contractId, ContractEventType.ProgressUpdated, $"Progress updated: {progress:P0}")
            {
                ContractTitle = contractTitle,
                CompletionProgress = progress,
                AdditionalInfo = milestone,
                NewStatus = "In Progress"
            };
            
            PublishContractEvent(eventData);
        }
        
        /// <summary>
        /// Publish deadline warning event
        /// </summary>
        public void PublishDeadlineWarning(string contractId, string contractTitle, DateTime dueTime, float hoursRemaining)
        {
            var eventData = new ContractEventData(contractId, ContractEventType.DeadlineWarning, $"Deadline approaching: {hoursRemaining:F1} hours remaining")
            {
                ContractTitle = contractTitle,
                DueTime = dueTime,
                TimeRemaining = hoursRemaining,
                IsOverdue = hoursRemaining <= 0,
                NewStatus = hoursRemaining <= 0 ? "Overdue" : "Deadline Warning"
            };
            
            PublishContractEvent(eventData);
        }
        
        /// <summary>
        /// Publish quality assessment event
        /// </summary>
        public void PublishQualityAssessment(string contractId, string contractTitle, float qualityScore, bool meetsStandards)
        {
            var eventData = new ContractEventData(contractId, ContractEventType.QualityAssessed, $"Quality assessed: {qualityScore:P0}")
            {
                ContractTitle = contractTitle,
                QualityScore = qualityScore,
                QualityMeetsStandards = meetsStandards,
                NewStatus = meetsStandards ? "Quality Approved" : "Quality Review Required"
            };
            
            PublishContractEvent(eventData);
        }
        
        /// <summary>
        /// Publish payment processed event
        /// </summary>
        public void PublishPaymentProcessed(string contractId, string contractTitle, float payout, float bonus = 0f)
        {
            var eventData = new ContractEventData(contractId, ContractEventType.PaymentProcessed, $"Payment processed: ${payout:F2}")
            {
                ContractTitle = contractTitle,
                FinalPayout = payout,
                BonusReward = bonus,
                NewStatus = "Payment Completed"
            };
            
            PublishContractEvent(eventData);
        }
        
        #endregion
        
        #region Event Management
        
        /// <summary>
        /// Get event statistics by type
        /// </summary>
        public Dictionary<ContractEventType, int> GetEventStatistics()
        {
            return new Dictionary<ContractEventType, int>(_eventCounts);
        }
        
        /// <summary>
        /// Get recent events of a specific type
        /// </summary>
        public List<ContractEventData> GetRecentEvents(ContractEventType eventType, int count = 10)
        {
            return _eventHistory
                .Where(e => e.EventType == eventType)
                .OrderByDescending(e => e.EventTimestamp)
                .Take(count)
                .ToList();
        }
        
        /// <summary>
        /// Get events for a specific contract
        /// </summary>
        public List<ContractEventData> GetContractEvents(string contractId)
        {
            return _eventHistory
                .Where(e => e.ContractId == contractId)
                .OrderBy(e => e.EventTimestamp)
                .ToList();
        }
        
        /// <summary>
        /// Clear event history
        /// </summary>
        public void ClearEventHistory()
        {
            _eventHistory.Clear();
            _eventCounts.Clear();
            _lastEventTimes.Clear();
            LogInfo("Event history cleared");
        }
        
        /// <summary>
        /// Add or remove event type from suppression list
        /// </summary>
        public void SetEventSuppression(ContractEventType eventType, bool suppress)
        {
            if (suppress && !_suppressedEventTypes.Contains(eventType))
            {
                _suppressedEventTypes.Add(eventType);
            }
            else if (!suppress && _suppressedEventTypes.Contains(eventType))
            {
                _suppressedEventTypes.Remove(eventType);
            }
        }
        
        #endregion
        
        #region Private Implementation
        
        private void InitializeServices()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                // Try to get NotificationManager without circular dependency
                // We'll disable notification integration for now to avoid compilation issues
                _notificationManager = null;
                _currencyManager = gameManager.GetManager<CurrencyManager>();
            }
            
            if (_notificationManager == null)
                LogWarning("NotificationManager not found - notification integration disabled");
            if (_currencyManager == null)
                LogWarning("CurrencyManager not found - currency events may not work properly");
        }
        
        private void InitializeEventChannels()
        {
            // Subscribe to event channels if they exist
            if (_globalContractEvent != null)
            {
                _globalContractEvent.Subscribe(OnGlobalContractEvent);
            }
            
            if (_contractAcceptedEvent != null)
            {
                _contractAcceptedEvent.Subscribe(OnContractAcceptedEvent);
            }
            
            if (_contractCompletedEvent != null)
            {
                _contractCompletedEvent.Subscribe(OnContractCompletedEvent);
            }
            
            // Add other event channel subscriptions as needed
        }
        
        private void CleanupEventChannels()
        {
            // Unsubscribe from event channels
            if (_globalContractEvent != null)
            {
                _globalContractEvent.Unsubscribe(OnGlobalContractEvent);
            }
            
            if (_contractAcceptedEvent != null)
            {
                _contractAcceptedEvent.Unsubscribe(OnContractAcceptedEvent);
            }
            
            if (_contractCompletedEvent != null)
            {
                _contractCompletedEvent.Unsubscribe(OnContractCompletedEvent);
            }
        }
        
        private void InitializeEventHistory()
        {
            _eventHistory.Clear();
            _eventCounts.Clear();
            _lastEventTimes.Clear();
            
            // Initialize event counts for all event types
            foreach (ContractEventType eventType in Enum.GetValues(typeof(ContractEventType)))
            {
                _eventCounts[eventType] = 0;
            }
        }
        
        private void SaveEventHistory()
        {
            // Could implement saving event history to persistent storage
            LogInfo($"Event history contains {_eventHistory.Count} events");
        }
        
        private bool ShouldSuppressEvent(ContractEventType eventType)
        {
            return _suppressedEventTypes.Contains(eventType);
        }
        
        private bool IsEventThrottled(ContractEventData eventData)
        {
            if (!_enableEventThrottling)
                return false;
            
            string throttleKey = $"{eventData.ContractId}_{eventData.EventType}";
            
            if (_lastEventTimes.TryGetValue(throttleKey, out DateTime lastTime))
            {
                var timeSinceLastEvent = DateTime.Now - lastTime;
                if (timeSinceLastEvent.TotalSeconds < _eventThrottleInterval)
                {
                    return true; // Throttled
                }
            }
            
            _lastEventTimes[throttleKey] = DateTime.Now;
            return false;
        }
        
        private void UpdateEventCounts(ContractEventType eventType)
        {
            if (_eventCounts.ContainsKey(eventType))
            {
                _eventCounts[eventType]++;
            }
            else
            {
                _eventCounts[eventType] = 1;
            }
        }
        
        private void AddToEventHistory(ContractEventData eventData)
        {
            _eventHistory.Add(eventData);
            
            // Trim history if it exceeds max size
            if (_eventHistory.Count > _maxEventHistorySize)
            {
                var eventsToRemove = _eventHistory.Count - _maxEventHistorySize;
                _eventHistory.RemoveRange(0, eventsToRemove);
            }
        }
        
        private void LogContractEvent(ContractEventData eventData)
        {
            string logMessage = $"[ContractEvent] {eventData.EventType}: {eventData.ContractTitle} ({eventData.ContractId})";
            if (!string.IsNullOrEmpty(eventData.EventDescription))
            {
                logMessage += $" - {eventData.EventDescription}";
            }
            
            LogInfo(logMessage);
        }
        
        private void PublishToEventChannels(ContractEventData eventData)
        {
            // Publish to global contract event channel
            if (_globalContractEvent != null)
            {
                _globalContractEvent.Invoke(eventData);
            }
            
            // Publish to specific event channels based on event type
            switch (eventData.EventType)
            {
                case ContractEventType.ContractAccepted:
                    _contractAcceptedEvent?.Invoke(eventData);
                    break;
                case ContractEventType.ContractCompleted:
                    _contractCompletedEvent?.Invoke(eventData);
                    break;
                case ContractEventType.ContractFailed:
                    _contractFailedEvent?.Invoke(eventData);
                    break;
                case ContractEventType.DeadlineWarning:
                    _deadlineWarningEvent?.Invoke(eventData);
                    break;
                case ContractEventType.ProgressUpdated:
                    _progressEvent?.Invoke(eventData);
                    break;
                case ContractEventType.QualityAssessed:
                    _qualityEvent?.Invoke(eventData);
                    break;
                case ContractEventType.PaymentProcessed:
                    _paymentEvent?.Invoke(eventData);
                    break;
            }
        }
        
        private void SendEventNotification(ContractEventData eventData)
        {
            if (_notificationManager == null)
                return;
            
            string notificationTitle = GetNotificationTitle(eventData.EventType);
            string notificationMessage = GetNotificationMessage(eventData);
            
            // Check if this type of notification should be shown
            if (ShouldShowNotification(eventData.EventType))
            {
                // Use reflection to call ShowNotification to avoid circular dependency
                if (_notificationManager != null)
                {
                    var showNotificationMethod = _notificationManager.GetType().GetMethod("ShowNotification");
                    if (showNotificationMethod != null)
                    {
                        showNotificationMethod.Invoke(_notificationManager, new object[] { notificationTitle, notificationMessage, _notificationDisplayDuration });
                        _totalNotificationsSent++;
                    }
                }
            }
        }
        
        private bool ShouldShowNotification(ContractEventType eventType)
        {
            return eventType switch
            {
                ContractEventType.ProgressUpdated => _showProgressNotifications,
                ContractEventType.DeadlineWarning => _showDeadlineNotifications,
                ContractEventType.ContractCompleted => _showCompletionNotifications,
                ContractEventType.QualityAssessed => _showQualityNotifications,
                ContractEventType.PaymentProcessed => _showPaymentNotifications,
                _ => true // Show other notifications by default
            };
        }
        
        private string GetNotificationTitle(ContractEventType eventType)
        {
            return eventType switch
            {
                ContractEventType.ContractAccepted => "Contract Accepted",
                ContractEventType.ContractCompleted => "Contract Completed",
                ContractEventType.ContractFailed => "Contract Failed",
                ContractEventType.DeadlineWarning => "Deadline Warning",
                ContractEventType.ProgressUpdated => "Progress Update",
                ContractEventType.QualityAssessed => "Quality Assessment",
                ContractEventType.PaymentProcessed => "Payment Processed",
                _ => "Contract Update"
            };
        }
        
        private string GetNotificationMessage(ContractEventData eventData)
        {
            string baseMessage = $"{eventData.ContractTitle}";
            
            if (!string.IsNullOrEmpty(eventData.EventDescription))
            {
                baseMessage += $": {eventData.EventDescription}";
            }
            
            // Add specific information based on event type
            switch (eventData.EventType)
            {
                case ContractEventType.PaymentProcessed:
                    baseMessage += $" (${eventData.FinalPayout:F2})";
                    break;
                case ContractEventType.QualityAssessed:
                    baseMessage += $" (Quality: {eventData.QualityScore:P0})";
                    break;
                case ContractEventType.ProgressUpdated:
                    baseMessage += $" ({eventData.CompletionProgress:P0} complete)";
                    break;
            }
            
            return baseMessage;
        }
        
        private void InvokeLocalEvents(ContractEventData eventData)
        {
            // Invoke general contract event
            OnAnyContractEvent?.Invoke(eventData);
            
            // Invoke specific events based on type
            switch (eventData.EventType)
            {
                case ContractEventType.ContractAccepted:
                    OnContractAccepted?.Invoke(eventData);
                    break;
                case ContractEventType.ContractCompleted:
                    OnContractCompleted?.Invoke(eventData);
                    break;
                case ContractEventType.ContractFailed:
                    OnContractFailed?.Invoke(eventData);
                    break;
                case ContractEventType.DeadlineWarning:
                    OnDeadlineWarning?.Invoke(eventData);
                    break;
                case ContractEventType.ProgressUpdated:
                    OnProgressUpdate?.Invoke(eventData);
                    break;
                case ContractEventType.QualityAssessed:
                    OnQualityAssessment?.Invoke(eventData);
                    break;
                case ContractEventType.PaymentProcessed:
                    OnPaymentProcessed?.Invoke(eventData);
                    break;
            }
        }
        
        // Event channel handlers
        private void OnGlobalContractEvent(ContractEventData eventData)
        {
            // Handle global contract events if needed
        }
        
        private void OnContractAcceptedEvent(ContractEventData eventData)
        {
            // Handle contract accepted events if needed
        }
        
        private void OnContractCompletedEvent(ContractEventData eventData)
        {
            // Update currency when contracts are completed
            if (_currencyManager != null && eventData.FinalPayout > 0)
            {
                _currencyManager.AddCurrency(CurrencyType.Cash, eventData.FinalPayout, "Contract completion payout");
            }
        }
        
        #endregion
    }
}