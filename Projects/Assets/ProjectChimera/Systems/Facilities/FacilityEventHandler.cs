using UnityEngine;
using ProjectChimera.Data.Events;
using ProjectChimera.Data.Facilities;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// Handles facility event coordination and notification system.
    /// Extracted from FacilityManager for modular architecture.
    /// Manages all facility-related events and their propagation.
    /// </summary>
    public class FacilityEventHandler : MonoBehaviour
    {
        [Header("Event Configuration")]
        [SerializeField] private bool _enableEventLogging = true;
        [SerializeField] private bool _enableEventHistory = false;
        [SerializeField] private int _maxEventHistory = 50;
        
        [Header("Event Channels")]
        [SerializeField] private SimpleGameEventSO _onFacilityUpgraded;
        [SerializeField] private SimpleGameEventSO _onFacilityUnlocked;
        [SerializeField] private SimpleGameEventSO _onFacilitySwitch;
        [SerializeField] private SimpleGameEventSO _onSceneTransitionStarted;
        [SerializeField] private SimpleGameEventSO _onSceneTransitionCompleted;
        [SerializeField] private SimpleGameEventSO _onFacilityPurchased;
        [SerializeField] private SimpleGameEventSO _onFacilitySold;
        [SerializeField] private SimpleGameEventSO _onFacilityUpgradeAvailable;
        [SerializeField] private SimpleGameEventSO _onFacilityRequirementsNotMet;
        [SerializeField] private SimpleGameEventSO _onFacilityValueUpdated;
        
        // Event history
        private System.Collections.Generic.Queue<FacilityEvent> _eventHistory = 
            new System.Collections.Generic.Queue<FacilityEvent>();
        
        // Public events for internal system coordination
        public System.Action<FacilityTierSO> OnFacilityUpgraded;
        public System.Action<FacilityTierSO> OnFacilityUnlocked;
        public System.Action<string, string> OnFacilitySwitch;
        public System.Action<OwnedFacility> OnFacilityPurchased;
        public System.Action<OwnedFacility, float> OnFacilitySold;
        public System.Action<FacilityTierSO> OnFacilityUpgradeAvailable;
        public System.Action<string> OnFacilityRequirementsNotMet;
        public System.Action OnFacilityValueUpdated;
        public System.Action<string> OnSceneTransitionStarted;
        public System.Action<string> OnSceneTransitionCompleted;
        
        // Properties
        public int EventHistoryCount => _eventHistory.Count;
        public bool EventLoggingEnabled => _enableEventLogging;
        
        /// <summary>
        /// Initialize the event handler
        /// </summary>
        public void Initialize()
        {
            LogEvent("Facility event handler initialized");
        }
        
        #region Facility Lifecycle Events
        
        /// <summary>
        /// Trigger facility upgraded event
        /// </summary>
        public void TriggerFacilityUpgraded(FacilityTierSO tier)
        {
            if (tier == null) return;
            
            LogEvent($"Facility upgraded to {tier.TierName}");
            RecordEvent(FacilityEventType.Upgraded, $"Upgraded to {tier.TierName}");
            
            _onFacilityUpgraded?.Invoke();
            OnFacilityUpgraded?.Invoke(tier);
        }
        
        /// <summary>
        /// Trigger facility unlocked event
        /// </summary>
        public void TriggerFacilityUnlocked(FacilityTierSO tier)
        {
            if (tier == null) return;
            
            LogEvent($"Facility tier unlocked: {tier.TierName}");
            RecordEvent(FacilityEventType.Unlocked, $"Unlocked {tier.TierName}");
            
            _onFacilityUnlocked?.Invoke();
            OnFacilityUnlocked?.Invoke(tier);
        }
        
        /// <summary>
        /// Trigger facility switch event
        /// </summary>
        public void TriggerFacilitySwitch(string fromFacilityId, string toFacilityId)
        {
            LogEvent($"Facility switch: {fromFacilityId} -> {toFacilityId}");
            RecordEvent(FacilityEventType.Switched, $"Switched facilities");
            
            _onFacilitySwitch?.Invoke();
            OnFacilitySwitch?.Invoke(fromFacilityId, toFacilityId);
        }
        
        /// <summary>
        /// Trigger facility purchased event
        /// </summary>
        public void TriggerFacilityPurchased(OwnedFacility facility)
        {
            if (facility.FacilityId == null) return;
            
            LogEvent($"Facility purchased: {facility.FacilityName}");
            RecordEvent(FacilityEventType.Purchased, $"Purchased {facility.FacilityName}");
            
            _onFacilityPurchased?.Invoke();
            OnFacilityPurchased?.Invoke(facility);
        }
        
        /// <summary>
        /// Trigger facility sold event
        /// </summary>
        public void TriggerFacilitySold(OwnedFacility facility, float salePrice)
        {
            if (facility.FacilityId == null) return;
            
            LogEvent($"Facility sold: {facility.FacilityName} for ${salePrice:F0}");
            RecordEvent(FacilityEventType.Sold, $"Sold {facility.FacilityName} for ${salePrice:F0}");
            
            _onFacilitySold?.Invoke();
            OnFacilitySold?.Invoke(facility, salePrice);
        }
        
        #endregion
        
        #region Status and Notification Events
        
        /// <summary>
        /// Trigger facility upgrade available event
        /// </summary>
        public void TriggerFacilityUpgradeAvailable(FacilityTierSO tier)
        {
            if (tier == null) return;
            
            LogEvent($"Facility upgrade available: {tier.TierName}");
            RecordEvent(FacilityEventType.UpgradeAvailable, $"Upgrade to {tier.TierName} available");
            
            _onFacilityUpgradeAvailable?.Invoke();
            OnFacilityUpgradeAvailable?.Invoke(tier);
        }
        
        /// <summary>
        /// Trigger facility requirements not met event
        /// </summary>
        public void TriggerFacilityRequirementsNotMet(string reason)
        {
            LogEvent($"Facility requirements not met: {reason}");
            RecordEvent(FacilityEventType.RequirementsNotMet, reason);
            
            _onFacilityRequirementsNotMet?.Invoke();
            OnFacilityRequirementsNotMet?.Invoke(reason);
        }
        
        /// <summary>
        /// Trigger facility value updated event
        /// </summary>
        public void TriggerFacilityValueUpdated()
        {
            LogEvent("Facility portfolio values updated");
            RecordEvent(FacilityEventType.ValueUpdated, "Portfolio values updated");
            
            _onFacilityValueUpdated?.Invoke();
            OnFacilityValueUpdated?.Invoke();
        }
        
        #endregion
        
        #region Scene Transition Events
        
        /// <summary>
        /// Trigger scene transition started event
        /// </summary>
        public void TriggerSceneTransitionStarted(string sceneName)
        {
            LogEvent($"Scene transition started: {sceneName}");
            RecordEvent(FacilityEventType.SceneTransition, $"Loading {sceneName}");
            
            _onSceneTransitionStarted?.Invoke();
            OnSceneTransitionStarted?.Invoke(sceneName);
        }
        
        /// <summary>
        /// Trigger scene transition completed event
        /// </summary>
        public void TriggerSceneTransitionCompleted(string sceneName)
        {
            LogEvent($"Scene transition completed: {sceneName}");
            RecordEvent(FacilityEventType.SceneTransition, $"Loaded {sceneName}");
            
            _onSceneTransitionCompleted?.Invoke();
            OnSceneTransitionCompleted?.Invoke(sceneName);
        }
        
        #endregion
        
        #region Event Subscription Management
        
        /// <summary>
        /// Subscribe to facility events for external systems
        /// </summary>
        public void SubscribeToFacilityEvents(
            System.Action onFacilityUpgraded = null,
            System.Action onFacilityUnlocked = null,
            System.Action onFacilitySwitch = null,
            System.Action onFacilityPurchased = null,
            System.Action onFacilitySold = null,
            System.Action onFacilityUpgradeAvailable = null,
            System.Action onFacilityRequirementsNotMet = null,
            System.Action onFacilityValueUpdated = null)
        {
            if (onFacilityUpgraded != null) _onFacilityUpgraded?.Subscribe(onFacilityUpgraded);
            if (onFacilityUnlocked != null) _onFacilityUnlocked?.Subscribe(onFacilityUnlocked);
            if (onFacilitySwitch != null) _onFacilitySwitch?.Subscribe(onFacilitySwitch);
            if (onFacilityPurchased != null) _onFacilityPurchased?.Subscribe(onFacilityPurchased);
            if (onFacilitySold != null) _onFacilitySold?.Subscribe(onFacilitySold);
            if (onFacilityUpgradeAvailable != null) _onFacilityUpgradeAvailable?.Subscribe(onFacilityUpgradeAvailable);
            if (onFacilityRequirementsNotMet != null) _onFacilityRequirementsNotMet?.Subscribe(onFacilityRequirementsNotMet);
            if (onFacilityValueUpdated != null) _onFacilityValueUpdated?.Subscribe(onFacilityValueUpdated);
            
            LogEvent("External event subscriptions registered");
        }
        
        /// <summary>
        /// Unsubscribe from facility events
        /// </summary>
        public void UnsubscribeFromFacilityEvents(
            System.Action onFacilityUpgraded = null,
            System.Action onFacilityUnlocked = null,
            System.Action onFacilitySwitch = null,
            System.Action onFacilityPurchased = null,
            System.Action onFacilitySold = null,
            System.Action onFacilityUpgradeAvailable = null,
            System.Action onFacilityRequirementsNotMet = null,
            System.Action onFacilityValueUpdated = null)
        {
            if (onFacilityUpgraded != null) _onFacilityUpgraded?.Unsubscribe(onFacilityUpgraded);
            if (onFacilityUnlocked != null) _onFacilityUnlocked?.Unsubscribe(onFacilityUnlocked);
            if (onFacilitySwitch != null) _onFacilitySwitch?.Unsubscribe(onFacilitySwitch);
            if (onFacilityPurchased != null) _onFacilityPurchased?.Unsubscribe(onFacilityPurchased);
            if (onFacilitySold != null) _onFacilitySold?.Unsubscribe(onFacilitySold);
            if (onFacilityUpgradeAvailable != null) _onFacilityUpgradeAvailable?.Unsubscribe(onFacilityUpgradeAvailable);
            if (onFacilityRequirementsNotMet != null) _onFacilityRequirementsNotMet?.Unsubscribe(onFacilityRequirementsNotMet);
            if (onFacilityValueUpdated != null) _onFacilityValueUpdated?.Unsubscribe(onFacilityValueUpdated);
            
            LogEvent("External event subscriptions removed");
        }
        
        #endregion
        
        #region Event History and Notifications
        
        /// <summary>
        /// Record event in history
        /// </summary>
        private void RecordEvent(FacilityEventType eventType, string description)
        {
            if (!_enableEventHistory) return;
            
            var facilityEvent = new FacilityEvent
            {
                EventType = eventType,
                Description = description,
                Timestamp = System.DateTime.Now
            };
            
            _eventHistory.Enqueue(facilityEvent);
            
            // Maintain history size limit
            while (_eventHistory.Count > _maxEventHistory)
            {
                _eventHistory.Dequeue();
            }
        }
        
        /// <summary>
        /// Get recent event history
        /// </summary>
        public System.Collections.Generic.List<FacilityEvent> GetRecentEvents(int count = 10)
        {
            var events = new System.Collections.Generic.List<FacilityEvent>();
            var eventArray = _eventHistory.ToArray();
            
            int startIndex = Mathf.Max(0, eventArray.Length - count);
            for (int i = startIndex; i < eventArray.Length; i++)
            {
                events.Add(eventArray[i]);
            }
            
            return events;
        }
        
        /// <summary>
        /// Get formatted notification message for UI display
        /// </summary>
        public string GetFacilityNotificationMessage(FacilityEventType eventType, FacilityTierSO tier = null, float amount = 0f)
        {
            switch (eventType)
            {
                case FacilityEventType.Upgraded:
                    return $"Facility upgraded to {tier?.TierName ?? "Unknown"}!";
                case FacilityEventType.Unlocked:
                    return $"{tier?.TierName ?? "New facility"} unlocked and available for upgrade!";
                case FacilityEventType.Purchased:
                    return $"New facility purchased: {tier?.TierName ?? "Unknown"}";
                case FacilityEventType.Sold:
                    return $"Facility sold for ${amount:F0}";
                case FacilityEventType.UpgradeAvailable:
                    return "New facility upgrades are available!";
                case FacilityEventType.RequirementsNotMet:
                    return "Upgrade requirements not met. Keep growing to unlock better facilities!";
                case FacilityEventType.ValueUpdated:
                    return "Facility portfolio values have been updated.";
                case FacilityEventType.Switched:
                    return "Successfully switched to new facility.";
                case FacilityEventType.SceneTransition:
                    return "Loading facility environment...";
                default:
                    return "Facility event occurred.";
            }
        }
        
        /// <summary>
        /// Clear event history
        /// </summary>
        public void ClearEventHistory()
        {
            _eventHistory.Clear();
            LogEvent("Event history cleared");
        }
        
        #endregion
        
        private void LogEvent(string message)
        {
            if (_enableEventLogging)
                Debug.Log($"[FacilityEventHandler] {message}");
        }
    }
    
    /// <summary>
    /// Facility event data structure
    /// </summary>
    [System.Serializable]
    public class FacilityEvent
    {
        public FacilityEventType EventType;
        public string Description;
        public System.DateTime Timestamp;
        
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {EventType}: {Description}";
        }
    }
}