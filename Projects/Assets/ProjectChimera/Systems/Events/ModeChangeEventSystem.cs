using UnityEngine;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;
using System.Collections.Generic;
using System.Collections;

namespace ProjectChimera.Systems.Events
{
    /// <summary>
    /// Comprehensive event-driven mode change system demonstrating subscriber reactions
    /// Coordinates all mode change responses across the game systems
    /// Phase 2 implementation following roadmap requirements
    /// </summary>
    public class ModeChangeEventSystem : MonoBehaviour
    {
        [Header("Event System Configuration")]
        [SerializeField] private bool _enableEventLogging = true;
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private bool _enableEventValidation = true;
        [SerializeField] private bool _debugMode = false;
        
        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;
        
        [Header("Subscriber Management")]
        [SerializeField] private int _maxSubscribers = 50;
        [SerializeField] private float _eventTimeout = 5f; // Maximum time for all subscribers to respond
        [SerializeField] private int _eventHistorySize = 100;
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool _trackResponseTimes = true;
        [SerializeField] private bool _detectSlowSubscribers = true;
        [SerializeField] private float _slowSubscriberThreshold = 0.1f; // 100ms threshold
        
        // Services
        private IGameplayModeController _modeController;
        
        // Event system state
        private bool _isInitialized = false;
        private List<IEventSubscriber> _registeredSubscribers = new List<IEventSubscriber>();
        private List<EventResponse> _eventHistory = new List<EventResponse>();
        private Dictionary<string, SubscriberStats> _subscriberStats = new Dictionary<string, SubscriberStats>();
        
        // Performance tracking
        private System.Diagnostics.Stopwatch _eventStopwatch = new System.Diagnostics.Stopwatch();
        private float _lastEventTimestamp;
        private int _totalEventsProcessed = 0;
        private int _totalSubscribersNotified = 0;
        
        [System.Serializable]
        public class EventResponse
        {
            public GameplayMode fromMode;
            public GameplayMode toMode;
            public System.DateTime timestamp;
            public float processingTime;
            public int subscribersNotified;
            public int successfulResponses;
            public int failedResponses;
            public List<string> errors;
            
            public EventResponse()
            {
                errors = new List<string>();
            }
        }
        
        [System.Serializable]
        public class SubscriberStats
        {
            public string subscriberName;
            public int eventsReceived;
            public int successfulResponses;
            public int failedResponses;
            public float totalResponseTime;
            public float averageResponseTime;
            public float slowestResponse;
            public System.DateTime lastResponseTime;
            
            public void RecordResponse(float responseTime, bool success)
            {
                eventsReceived++;
                if (success) successfulResponses++;
                else failedResponses++;
                
                totalResponseTime += responseTime;
                averageResponseTime = totalResponseTime / eventsReceived;
                
                if (responseTime > slowestResponse)
                    slowestResponse = responseTime;
                
                lastResponseTime = System.DateTime.Now;
            }
        }
        
        /// <summary>
        /// Interface for components that want to subscribe to mode change events
        /// </summary>
        public interface IEventSubscriber
        {
            string SubscriberName { get; }
            int Priority { get; } // Higher priority subscribers get notified first
            bool OnModeChangeEvent(ModeChangeEventData eventData);
        }
        
        private void Start()
        {
            InitializeEventSystem();
        }
        
        private void OnDestroy()
        {
            ShutdownEventSystem();
        }
        
        private void InitializeEventSystem()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ProjectChimera.Core.DependencyInjection.ServiceLocator.Instance.GetService<IGameplayModeController>();
                
                if (_modeController == null)
                {
                    Debug.LogError("[ModeChangeEventSystem] GameplayModeController service not found!");
                    return;
                }
                
                // Subscribe to the main mode changed event
                if (_modeChangedEvent != null)
                {
                    _modeChangedEvent.Subscribe(OnModeChanged);
                }
                else
                {
                    Debug.LogError("[ModeChangeEventSystem] ModeChangedEvent not assigned!");
                    return;
                }
                
                // Auto-discover and register event subscribers
                DiscoverEventSubscribers();
                
                _isInitialized = true;
                
                if (_debugMode)
                {
                    Debug.Log($"[ModeChangeEventSystem] Initialized with {_registeredSubscribers.Count} subscribers");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ModeChangeEventSystem] Error during initialization: {ex.Message}");
            }
        }
        
        private void DiscoverEventSubscribers()
        {
            // Find all components implementing IEventSubscriber interface
            var allComponents = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            
            foreach (var component in allComponents)
            {
                if (component is IEventSubscriber subscriber)
                {
                    RegisterSubscriber(subscriber);
                }
            }
            
            // Create demo subscribers to showcase the system
            CreateDemoSubscribers();
            
            if (_debugMode)
            {
                Debug.Log($"[ModeChangeEventSystem] Discovered {_registeredSubscribers.Count} event subscribers");
            }
        }
        
        private void CreateDemoSubscribers()
        {
            // Create various demo subscribers to showcase different response patterns
            
            // UI System Subscriber
            var uiSubscriber = new DemoUISystemSubscriber();
            RegisterSubscriber(uiSubscriber);
            
            // Audio System Subscriber
            var audioSubscriber = new DemoAudioSystemSubscriber();
            RegisterSubscriber(audioSubscriber);
            
            // Lighting System Subscriber
            var lightingSubscriber = new DemoLightingSystemSubscriber();
            RegisterSubscriber(lightingSubscriber);
            
            // Analytics Subscriber
            var analyticsSubscriber = new DemoAnalyticsSubscriber();
            RegisterSubscriber(analyticsSubscriber);
            
            // Camera System Subscriber
            var cameraSubscriber = new DemoCameraSystemSubscriber();
            RegisterSubscriber(cameraSubscriber);
        }
        
        public void RegisterSubscriber(IEventSubscriber subscriber)
        {
            if (subscriber == null) return;
            
            if (_registeredSubscribers.Count >= _maxSubscribers)
            {
                Debug.LogWarning($"[ModeChangeEventSystem] Maximum subscribers ({_maxSubscribers}) reached!");
                return;
            }
            
            if (!_registeredSubscribers.Contains(subscriber))
            {
                _registeredSubscribers.Add(subscriber);
                
                // Sort by priority (higher priority first)
                _registeredSubscribers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                
                // Initialize subscriber stats
                _subscriberStats[subscriber.SubscriberName] = new SubscriberStats
                {
                    subscriberName = subscriber.SubscriberName
                };
                
                if (_debugMode)
                {
                    Debug.Log($"[ModeChangeEventSystem] Registered subscriber: {subscriber.SubscriberName} (Priority: {subscriber.Priority})");
                }
            }
        }
        
        public void UnregisterSubscriber(IEventSubscriber subscriber)
        {
            if (subscriber != null && _registeredSubscribers.Contains(subscriber))
            {
                _registeredSubscribers.Remove(subscriber);
                _subscriberStats.Remove(subscriber.SubscriberName);
                
                if (_debugMode)
                {
                    Debug.Log($"[ModeChangeEventSystem] Unregistered subscriber: {subscriber.SubscriberName}");
                }
            }
        }
        
        private void OnModeChanged(ModeChangeEventData eventData)
        {
            if (!_isInitialized) return;
            
            _eventStopwatch.Restart();
            
            if (_enableEventLogging && _debugMode)
            {
                Debug.Log($"[ModeChangeEventSystem] Processing mode change event: {eventData.PreviousMode} → {eventData.NewMode}");
            }
            
            // Create event response record
            var eventResponse = new EventResponse
            {
                fromMode = eventData.PreviousMode,
                toMode = eventData.NewMode,
                timestamp = System.DateTime.Now,
                subscribersNotified = _registeredSubscribers.Count
            };
            
            // Notify all subscribers
            StartCoroutine(NotifySubscribersCoroutine(eventData, eventResponse));
        }
        
        private IEnumerator NotifySubscribersCoroutine(ModeChangeEventData eventData, EventResponse eventResponse)
        {
            int successfulResponses = 0;
            int failedResponses = 0;
            
            foreach (var subscriber in _registeredSubscribers)
            {
                if (subscriber == null) continue;
                
                float subscriberStartTime = Time.realtimeSinceStartup;
                bool success = false;
                
                try
                {
                    // Notify subscriber
                    success = subscriber.OnModeChangeEvent(eventData);
                    
                    if (success) successfulResponses++;
                    else failedResponses++;
                    
                    if (_enableEventLogging && _debugMode)
                    {
                        Debug.Log($"[ModeChangeEventSystem] Notified {subscriber.SubscriberName}: {(success ? "SUCCESS" : "FAILED")}");
                    }
                }
                catch (System.Exception ex)
                {
                    failedResponses++;
                    eventResponse.errors.Add($"{subscriber.SubscriberName}: {ex.Message}");
                    
                    Debug.LogError($"[ModeChangeEventSystem] Error notifying {subscriber.SubscriberName}: {ex.Message}");
                }
                
                // Track performance
                float responseTime = Time.realtimeSinceStartup - subscriberStartTime;
                
                if (_subscriberStats.TryGetValue(subscriber.SubscriberName, out var stats))
                {
                    stats.RecordResponse(responseTime, success);
                    
                    if (_detectSlowSubscribers && responseTime > _slowSubscriberThreshold)
                    {
                        Debug.LogWarning($"[ModeChangeEventSystem] Slow subscriber detected: {subscriber.SubscriberName} took {responseTime:F3}s");
                    }
                }
                
                // Yield every few subscribers to prevent frame drops
                if (_registeredSubscribers.IndexOf(subscriber) % 5 == 0)
                {
                    yield return null;
                }
            }
            
            // Complete event processing
            _eventStopwatch.Stop();
            float totalProcessingTime = (float)_eventStopwatch.Elapsed.TotalSeconds;
            
            eventResponse.processingTime = totalProcessingTime;
            eventResponse.successfulResponses = successfulResponses;
            eventResponse.failedResponses = failedResponses;
            
            // Add to history
            RecordEventResponse(eventResponse);
            
            // Update global stats
            _totalEventsProcessed++;
            _totalSubscribersNotified += _registeredSubscribers.Count;
            _lastEventTimestamp = Time.time;
            
            if (_enableEventLogging || _debugMode)
            {
                Debug.Log($"[ModeChangeEventSystem] Event processing complete: {successfulResponses}/{_registeredSubscribers.Count} successful, {totalProcessingTime:F3}s total");
            }
            
            // Validate event completion if enabled
            if (_enableEventValidation)
            {
                ValidateEventCompletion(eventData, eventResponse);
            }
        }
        
        private void RecordEventResponse(EventResponse response)
        {
            _eventHistory.Add(response);
            
            // Maintain history size limit
            if (_eventHistory.Count > _eventHistorySize)
            {
                _eventHistory.RemoveAt(0);
            }
        }
        
        private void ValidateEventCompletion(ModeChangeEventData eventData, EventResponse response)
        {
            // Validate that the event was processed successfully
            float successRate = (float)response.successfulResponses / response.subscribersNotified;
            
            if (successRate < 0.9f) // Less than 90% success rate
            {
                Debug.LogWarning($"[ModeChangeEventSystem] Low success rate for mode change: {successRate:P} ({response.errors.Count} errors)");
            }
            
            if (response.processingTime > _eventTimeout)
            {
                Debug.LogWarning($"[ModeChangeEventSystem] Event processing timeout: {response.processingTime:F3}s > {_eventTimeout}s");
            }
            
            // Check if mode actually changed
            if (_modeController != null && _modeController.CurrentMode != eventData.NewMode)
            {
                Debug.LogError($"[ModeChangeEventSystem] Mode change validation failed! Expected: {eventData.NewMode}, Actual: {_modeController.CurrentMode}");
            }
        }
        
        private void ShutdownEventSystem()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Unsubscribe(OnModeChanged);
            }
            
            _registeredSubscribers.Clear();
            _subscriberStats.Clear();
            _eventHistory.Clear();
            
            if (_debugMode)
            {
                Debug.Log("[ModeChangeEventSystem] Event system shut down");
            }
        }
        
        #region Demo Subscribers
        
        /// <summary>
        /// Demo UI System Subscriber - High priority, quick response
        /// </summary>
        private class DemoUISystemSubscriber : IEventSubscriber
        {
            public string SubscriberName => "UI System";
            public int Priority => 100; // High priority - UI should update first
            
            public bool OnModeChangeEvent(ModeChangeEventData eventData)
            {
                // Simulate UI updates (navigation, panels, buttons)
                System.Threading.Thread.Sleep(10); // 10ms simulation
                
                Debug.Log($"[UI System] Mode changed to {eventData.NewMode} - Updated navigation and panels");
                return true;
            }
        }
        
        /// <summary>
        /// Demo Audio System Subscriber - Medium priority, moderate response time
        /// </summary>
        private class DemoAudioSystemSubscriber : IEventSubscriber
        {
            public string SubscriberName => "Audio System";
            public int Priority => 75; // Medium-high priority
            
            public bool OnModeChangeEvent(ModeChangeEventData eventData)
            {
                // Simulate audio changes (background music, sound effects)
                System.Threading.Thread.Sleep(25); // 25ms simulation
                
                string audioTheme = eventData.NewMode switch
                {
                    GameplayMode.Cultivation => "Ambient Nature",
                    GameplayMode.Construction => "Industrial",
                    GameplayMode.Genetics => "Scientific",
                    _ => "Default"
                };
                
                Debug.Log($"[Audio System] Mode changed to {eventData.NewMode} - Switched to {audioTheme} audio theme");
                return true;
            }
        }
        
        /// <summary>
        /// Demo Lighting System Subscriber - Medium priority, slower response
        /// </summary>
        private class DemoLightingSystemSubscriber : IEventSubscriber
        {
            public string SubscriberName => "Lighting System";
            public int Priority => 50; // Medium priority
            
            public bool OnModeChangeEvent(ModeChangeEventData eventData)
            {
                // Simulate lighting changes (more expensive operations)
                System.Threading.Thread.Sleep(75); // 75ms simulation
                
                string lightingMode = eventData.NewMode switch
                {
                    GameplayMode.Cultivation => "Warm Growing Lights",
                    GameplayMode.Construction => "Bright Work Lights",
                    GameplayMode.Genetics => "Cool Lab Lighting",
                    _ => "Default Lighting"
                };
                
                Debug.Log($"[Lighting System] Mode changed to {eventData.NewMode} - Applied {lightingMode}");
                return true;
            }
        }
        
        /// <summary>
        /// Demo Analytics Subscriber - Low priority, background processing
        /// </summary>
        private class DemoAnalyticsSubscriber : IEventSubscriber
        {
            public string SubscriberName => "Analytics System";
            public int Priority => 10; // Low priority - can be delayed
            
            public bool OnModeChangeEvent(ModeChangeEventData eventData)
            {
                // Simulate analytics tracking (background task)
                System.Threading.Thread.Sleep(5); // 5ms simulation
                
                Debug.Log($"[Analytics System] Mode changed to {eventData.NewMode} - Logged user behavior event");
                return true;
            }
        }
        
        /// <summary>
        /// Demo Camera System Subscriber - High priority, quick response
        /// </summary>
        private class DemoCameraSystemSubscriber : IEventSubscriber
        {
            public string SubscriberName => "Camera System";
            public int Priority => 90; // High priority - visual changes are important
            
            public bool OnModeChangeEvent(ModeChangeEventData eventData)
            {
                // Simulate camera adjustments
                System.Threading.Thread.Sleep(15); // 15ms simulation
                
                string cameraPreset = eventData.NewMode switch
                {
                    GameplayMode.Cultivation => "Plant View",
                    GameplayMode.Construction => "Blueprint View", 
                    GameplayMode.Genetics => "Analysis View",
                    _ => "Default View"
                };
                
                Debug.Log($"[Camera System] Mode changed to {eventData.NewMode} - Applied {cameraPreset} camera preset");
                return true;
            }
        }
        
        #endregion
        
        #region Public Interface & Debugging
        
        /// <summary>
        /// Get comprehensive event system statistics
        /// </summary>
        public EventSystemStats GetEventSystemStats()
        {
            return new EventSystemStats
            {
                totalEventsProcessed = _totalEventsProcessed,
                totalSubscribersNotified = _totalSubscribersNotified,
                activeSubscribers = _registeredSubscribers.Count,
                averageProcessingTime = CalculateAverageProcessingTime(),
                lastEventTime = _lastEventTimestamp,
                eventHistorySize = _eventHistory.Count
            };
        }
        
        [System.Serializable]
        public class EventSystemStats
        {
            public int totalEventsProcessed;
            public int totalSubscribersNotified;
            public int activeSubscribers;
            public float averageProcessingTime;
            public float lastEventTime;
            public int eventHistorySize;
        }
        
        private float CalculateAverageProcessingTime()
        {
            if (_eventHistory.Count == 0) return 0f;
            
            float total = 0f;
            foreach (var response in _eventHistory)
            {
                total += response.processingTime;
            }
            
            return total / _eventHistory.Count;
        }
        
        /// <summary>
        /// Get statistics for a specific subscriber
        /// </summary>
        public SubscriberStats GetSubscriberStats(string subscriberName)
        {
            return _subscriberStats.TryGetValue(subscriberName, out var stats) ? stats : null;
        }
        
        /// <summary>
        /// Get all subscriber statistics
        /// </summary>
        public Dictionary<string, SubscriberStats> GetAllSubscriberStats()
        {
            return new Dictionary<string, SubscriberStats>(_subscriberStats);
        }
        
        /// <summary>
        /// Force trigger a mode change event for testing
        /// </summary>
        public void TriggerTestEvent(GameplayMode targetMode)
        {
            if (_modeController != null)
            {
                _modeController.SetMode(targetMode, "EventSystem Test");
            }
        }
        
        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            Debug.Log($"[ModeChangeEventSystem] Debug mode {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Get current system state
        /// </summary>
        public bool IsInitialized => _isInitialized;
        public int SubscriberCount => _registeredSubscribers.Count;
        public int EventHistoryCount => _eventHistory.Count;
        
        #endregion
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Editor-only method for testing the event system
        /// </summary>
        [ContextMenu("Test Event System - Cycle Modes")]
        private void TestEventSystemCycleModes()
        {
            if (Application.isPlaying && _modeController != null)
            {
                StartCoroutine(TestEventSequence());
            }
            else
            {
                Debug.Log("[ModeChangeEventSystem] Test only works during play mode with initialized controller");
            }
        }
        
        private IEnumerator TestEventSequence()
        {
            var modes = new[] { GameplayMode.Cultivation, GameplayMode.Construction, GameplayMode.Genetics };
            
            foreach (var mode in modes)
            {
                Debug.Log($"[ModeChangeEventSystem] Testing mode change to: {mode}");
                TriggerTestEvent(mode);
                yield return new WaitForSeconds(1f);
            }
            
            Debug.Log("[ModeChangeEventSystem] Event system test sequence complete");
        }
        
        #endif
    }
}