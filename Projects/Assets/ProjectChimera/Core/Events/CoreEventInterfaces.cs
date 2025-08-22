using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Core.Events
{
    /// <summary>
    /// Core event interfaces and data structures for Project Chimera's event system.
    /// Contains only the essential types needed by both Data and Systems assemblies
    /// to break circular dependencies.
    /// </summary>
    
    #region Core Event Types
    
    /// <summary>
    /// Core interface for live event definitions that can be defined in Data assembly
    /// and implemented in Systems assembly.
    /// </summary>
    public interface ILiveEventDefinition
    {
        string EventId { get; }
        string EventName { get; }
        string Description { get; }
        DateTime StartTime { get; }
        DateTime EndTime { get; }
        
        // Extended properties for full event system support
        object EventType { get; } // Generic object to avoid enum conflicts between assemblies
        EventScope Scope { get; }
        TimeSpan Duration { get; }
        List<string> ParticipationRequirements { get; }
        bool HasEducationalContent { get; }
        bool RequiresScientificAccuracy { get; }
        bool IsScientificallyValidated { get; }
    }
    
    /// <summary>
    /// Types of messages that can be sent through the live event system.
    /// </summary>
    public enum LiveEventMessageType
    {
        SystemNotification,
        PlayerAction,
        GameStateChange,
        CompetitionUpdate,
        CommunityGoalProgress,
        SeasonalTransition,
        EducationalContent,
        NarrativeEvent,
        EnvironmentalChange,
        EconomicUpdate,
        Achievement,
        Alert,
        Debug,
        EventStarted,
        EventEnded,
        EventUpdated,
        GlobalSynchronization,
        CompetitionPhaseChange,
        EducationalProgress,
        CrisisResponse
    }
    
    /// <summary>
    /// Priority levels for event processing and delivery.
    /// </summary>
    public enum EventPriority
    {
        Immediate = -1,
        Critical = 0,
        High = 1,
        Medium = 2,
        Normal = 2, // Alias for Medium
        Low = 3,
        Background = 4
    }
    
    /// <summary>
    /// Scope of event distribution and impact.
    /// </summary>
    public enum EventScope
    {
        All,
        Global,
        Regional,
        Local,
        Community,
        Personal,
        System
    }
    
    /// <summary>
    /// Global event state for synchronized events.
    /// </summary>
    [Serializable]
    public class GlobalEventState
    {
        [SerializeField] private string _eventId;
        [SerializeField] private float _globalProgress;
        [SerializeField] private int _totalParticipants;
        [SerializeField] private DateTime _lastUpdate;
        
        public string EventId 
        { 
            get => _eventId; 
            set => _eventId = value; 
        }
        public float GlobalProgress 
        { 
            get => _globalProgress; 
            set => _globalProgress = value; 
        }
        public int TotalParticipants 
        { 
            get => _totalParticipants; 
            set => _totalParticipants = value; 
        }
        public DateTime LastUpdate 
        { 
            get => _lastUpdate; 
            set => _lastUpdate = value; 
        }
        
        // Non-serialized data storage for regional progress
        public Dictionary<string, float> RegionalProgress { get; private set; } = new Dictionary<string, float>();
    }
    
    #endregion
    
    #region Core Message Types
    
    /// <summary>
    /// Core live event message structure for cross-system communication.
    /// </summary>
    [Serializable]
    public class LiveEventMessage
    {
        [SerializeField] private string _messageId;
        [SerializeField] private LiveEventMessageType _messageType;
        [SerializeField] private EventPriority _priority = EventPriority.Medium;
        [SerializeField] private EventScope _scope = EventScope.Global;
        [SerializeField] private DateTime _timestamp;
        [SerializeField] private string _sourceId;
        [SerializeField] private string _title;
        [SerializeField] private string _description;
        [SerializeField] private List<string> _tags = new List<string>();
        [SerializeField] private bool _requiresAcknowledgment = false;
        [SerializeField] private DateTime _expirationTime;
        
        // Non-serialized data storage
        public Dictionary<string, object> Data { get; private set; } = new Dictionary<string, object>();
        
        // Properties
        public string MessageId => _messageId;
        public LiveEventMessageType MessageType 
        { 
            get => _messageType; 
            set => _messageType = value; 
        }
        public LiveEventMessageType EventType => _messageType;  // Compatibility property for EventType
        public EventPriority Priority 
        { 
            get => _priority; 
            set => _priority = value; 
        }
        public EventScope Scope 
        { 
            get => _scope; 
            set => _scope = value; 
        }
        public DateTime Timestamp 
        { 
            get => _timestamp; 
            set => _timestamp = value; 
        }
        public string SourceId 
        { 
            get => _sourceId; 
            set => _sourceId = value; 
        }
        public string Title 
        { 
            get => _title; 
            set => _title = value; 
        }
        public string Description 
        { 
            get => _description; 
            set => _description = value; 
        }
        public List<string> Tags => _tags;
        public bool RequiresAcknowledgment 
        { 
            get => _requiresAcknowledgment; 
            set => _requiresAcknowledgment = value; 
        }
        public DateTime ExpirationTime 
        { 
            get => _expirationTime; 
            set => _expirationTime = value; 
        }
        
        public LiveEventMessage()
        {
            _messageId = Guid.NewGuid().ToString();
            _timestamp = DateTime.Now;
            _expirationTime = DateTime.Now.AddHours(24); // Default 24 hour expiration
        }
        
        public LiveEventMessage(LiveEventMessageType messageType, string title, string description = null) : this()
        {
            _messageType = messageType;
            _title = title;
            _description = description ?? string.Empty;
        }
        
        public void SetData<T>(string key, T value)
        {
            Data[key] = value;
        }
        
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        public bool IsExpired()
        {
            return DateTime.Now > _expirationTime;
        }
        
        public void AddTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && !_tags.Contains(tag))
            {
                _tags.Add(tag);
            }
        }
        
        public bool HasTag(string tag)
        {
            return !string.IsNullOrEmpty(tag) && _tags.Contains(tag);
        }
    }
    
    #endregion
} 