using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Shared;
using ProjectChimera.UI.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// ScriptableObject-based event channel for UI-to-Manager communication.
    /// Provides typed event channels for decoupled communication between UI and game systems.
    /// </summary>
    [CreateAssetMenu(fileName = "New UI Event Channel", menuName = "Project Chimera/UI/Event Channel")]
    public class UIEventChannelSO : ChimeraDataSO
    {
        [Header("Event Channel Configuration")]
        [SerializeField] private string _channelName;
        [SerializeField] private string _description;
        [SerializeField] private bool _logEvents = false;
        
        [Header("Event History")]
        [SerializeField] private int _maxHistorySize = 50;
        [SerializeField] private bool _maintainHistory = true;
        
        // Event history for debugging
        private Queue<UIEventData> _eventHistory = new Queue<UIEventData>();
        
        // Properties
        public string ChannelName => _channelName;
        public string Description => _description;
        public bool LogEvents => _logEvents;
        public IReadOnlyCollection<UIEventData> EventHistory => _eventHistory;
        
        // Generic event for flexible communication
        public event Action<UIEventData> OnUIEvent;
        
        // Typed events for common UI actions
        public event Action<string> OnPanelNavigationRequest;
        public event Action<string, object> OnDataUpdateRequest;
        public event Action<string> OnActionTriggered;
        public event Action<NotificationRequest> OnNotificationRequest;
        public event Action<ModalRequest> OnModalRequest;
        
        /// <summary>
        /// Raise a generic UI event
        /// </summary>
        public void RaiseEvent(UIEventData eventData)
        {
            if (_logEvents)
            {
                Debug.Log($"[{_channelName}] UI Event: {eventData.EventType} - {eventData.Message}");
            }
            
            // Add to history
            if (_maintainHistory)
            {
                _eventHistory.Enqueue(eventData);
                while (_eventHistory.Count > _maxHistorySize)
                {
                    _eventHistory.Dequeue();
                }
            }
            
            OnUIEvent?.Invoke(eventData);
        }
        
        /// <summary>
        /// Request navigation to a specific panel
        /// </summary>
        public void RequestPanelNavigation(string panelId)
        {
            var eventData = new UIEventData
            {
                EventType = UIEventType.Navigation,
                Source = "UI",
                Target = "GameUIManager",
                Message = $"Navigate to {panelId}",
                Data = panelId,
                Timestamp = DateTime.Now
            };
            
            RaiseEvent(eventData);
            OnPanelNavigationRequest?.Invoke(panelId);
        }
        
        /// <summary>
        /// Request data update from a manager
        /// </summary>
        public void RequestDataUpdate(string managerId, object requestData = null)
        {
            var eventData = new UIEventData
            {
                EventType = UIEventType.DataRequest,
                Source = "UI",
                Target = managerId,
                Message = $"Data update requested from {managerId}",
                Data = requestData,
                Timestamp = DateTime.Now
            };
            
            RaiseEvent(eventData);
            OnDataUpdateRequest?.Invoke(managerId, requestData);
        }
        
        /// <summary>
        /// Trigger an action in a manager
        /// </summary>
        public void TriggerAction(string actionId, object actionData = null)
        {
            var eventData = new UIEventData
            {
                EventType = UIEventType.Action,
                Source = "UI",
                Target = "Manager",
                Message = $"Action triggered: {actionId}",
                Data = actionData,
                Timestamp = DateTime.Now
            };
            
            RaiseEvent(eventData);
            OnActionTriggered?.Invoke(actionId);
        }
        
        /// <summary>
        /// Request to show a notification
        /// </summary>
        public void RequestNotification(NotificationRequest request)
        {
            var eventData = new UIEventData
            {
                EventType = UIEventType.Notification,
                Source = "UI",
                Target = "GameUIManager",
                Message = $"Notification: {request.Title}",
                Data = request,
                Timestamp = DateTime.Now
            };
            
            RaiseEvent(eventData);
            OnNotificationRequest?.Invoke(request);
        }
        
        /// <summary>
        /// Request to show a modal dialog
        /// </summary>
        public void RequestModal(ModalRequest request)
        {
            var eventData = new UIEventData
            {
                EventType = UIEventType.Modal,
                Source = "UI",
                Target = "GameUIManager",
                Message = $"Modal: {request.Title}",
                Data = request,
                Timestamp = DateTime.Now
            };
            
            RaiseEvent(eventData);
            OnModalRequest?.Invoke(request);
        }
        
        /// <summary>
        /// Clear event history
        /// </summary>
        public void ClearHistory()
        {
            _eventHistory.Clear();
            Debug.Log($"[{_channelName}] Event history cleared");
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (string.IsNullOrEmpty(_channelName))
            {
                _channelName = name;
            }
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_channelName))
            {
                LogError("Channel name cannot be empty");
                isValid = false;
            }
            
            if (_maxHistorySize < 0)
            {
                LogError("Max history size cannot be negative");
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Data structure for UI events
    /// </summary>
    [System.Serializable]
    public class UIEventData
    {
        public UIEventType EventType;
        public string Source;
        public string Target;
        public string Message;
        public object Data;
        public DateTime Timestamp;
        public int Priority = 0;
        
        public override string ToString()
        {
            return $"{EventType}: {Source} -> {Target} | {Message} [{Timestamp:HH:mm:ss}]";
        }
    }
    
    /// <summary>
    /// Types of UI events
    /// </summary>
    public enum UIEventType
    {
        Navigation,
        DataRequest,
        DataUpdate,
        Action,
        Notification,
        Modal,
        StateChange,
        Configuration,
        Error,
        Debug
    }
    
    /// <summary>
    /// Notification request data
    /// </summary>
    [System.Serializable]
    public class NotificationRequest
    {
        public string Title;
        public string Message;
        public NotificationType Type = NotificationType.Info;
        public float Duration = 5f;
        public bool ShowIcon = true;
        public object Data;
    }
    
    /// <summary>
    /// Modal request data
    /// </summary>
    [System.Serializable]
    public class ModalRequest
    {
        public string Title;
        public string Message;
        public ModalType Type = ModalType.Confirmation;
        public Action OnConfirm;
        public Action OnCancel;
        public object Data;
    }
    
    /// <summary>
    /// Types of modals
    /// </summary>
    public enum ModalType
    {
        Information,
        Confirmation,
        Warning,
        Error,
        Custom
    }
}