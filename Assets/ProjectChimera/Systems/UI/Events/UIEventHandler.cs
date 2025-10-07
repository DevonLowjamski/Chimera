using UnityEngine;
using ProjectChimera.Core.SimpleDI;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Systems.Camera;
using ProjectChimera.Core.Input;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.UI.Events
{
    /// <summary>
    /// REFACTORED: UI Event Handler - Focused UI event processing and input management with ITickable
    /// Single Responsibility: Managing UI events, input processing, and interaction handling
    /// Extracted from OptimizedUIManager for better SRP compliance
    /// Uses ITickable for centralized update management
    /// </summary>
    public class UIEventHandler : MonoBehaviour, ITickable
    {
        private readonly bool _enableLogging;
        private readonly bool _enableInputOptimization;

        // Event management
        private readonly Dictionary<string, System.Action> _registeredEvents = new Dictionary<string, System.Action>();
        private readonly Dictionary<GameObject, UIInteractionData> _interactionCache = new Dictionary<GameObject, UIInteractionData>();
        private readonly Queue<UIEvent> _eventQueue = new Queue<UIEvent>();

        // Input optimization
        private readonly HashSet<Selectable> _cachedSelectables = new HashSet<Selectable>();
        private readonly Dictionary<KeyCode, System.Action> _keyboardShortcuts = new Dictionary<KeyCode, System.Action>();
        private UnityEngine.Camera _uiCamera;
        private GraphicRaycaster _raycaster;

        // Performance tracking
        private int _eventsProcessedThisFrame;
        private float _lastEventProcessTime;
        private readonly int _maxEventsPerFrame = 10;

        // Events
        public event System.Action<UIEvent> OnUIEventProcessed;
        public event System.Action<string, float> OnEventPerformanceWarning;

        public UIEventHandler(bool enableLogging = false, bool enableInputOptimization = true)
        {
            _enableLogging = enableLogging;
            _enableInputOptimization = enableInputOptimization;
        }

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeEventSystem();
        }

        private void Start()
        {
            CacheUIComponents();
        }

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.UIManager; // UI event processing
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            ProcessEventQueue();
            ProcessInputOptimizations();
        }

        private void OnEnable()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion

        private void OnDestroy()
        {
            CleanupEvents();
        }

        #endregion

        #region Initialization

        private void InitializeEventSystem()
        {
                       // Try main camera first, then ServiceContainer
            _uiCamera = UnityEngine.Camera.main;
            if (_uiCamera == null)
            {
                var cameraService = ServiceContainerFactory.Instance?.TryResolve<ICameraProvider>();
                _uiCamera = cameraService?.main;
            }
            _raycaster = ServiceContainerFactory.Instance?.TryResolve<GraphicRaycaster>();

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_EVENTS", "UIEventHandler initialized", this);
        }

        private void CacheUIComponents()
        {
            if (_enableInputOptimization)
            {
                // Selectables should self-register via ServiceContainer
                // This is now a placeholder - selectables register themselves during Awake()
                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("UI_EVENTS",
                        "UI selectables should self-register - manual caching disabled", this);
                }
            }
        }

        #endregion

        #region Event Processing

        /// <summary>
        /// Process queued UI events
        /// </summary>
        private void ProcessEventQueue()
        {
            _eventsProcessedThisFrame = 0;
            var startTime = Time.realtimeSinceStartup;

            while (_eventQueue.Count > 0 && _eventsProcessedThisFrame < _maxEventsPerFrame)
            {
                var uiEvent = _eventQueue.Dequeue();
                ProcessUIEvent(uiEvent);
                _eventsProcessedThisFrame++;
            }

            var processingTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            if (processingTime > 2.0f) // 2ms threshold
            {
                OnEventPerformanceWarning?.Invoke("Event processing", processingTime);
            }

            _lastEventProcessTime = processingTime;
        }

        /// <summary>
        /// Process a single UI event
        /// </summary>
        private void ProcessUIEvent(UIEvent uiEvent)
        {
            try
            {
                switch (uiEvent.Type)
                {
                    case UIEventType.Click:
                        ProcessClickEvent(uiEvent);
                        break;
                    case UIEventType.Hover:
                        ProcessHoverEvent(uiEvent);
                        break;
                    case UIEventType.Focus:
                        ProcessFocusEvent(uiEvent);
                        break;
                    case UIEventType.KeyPress:
                        ProcessKeyPressEvent(uiEvent);
                        break;
                    case UIEventType.Custom:
                        ProcessCustomEvent(uiEvent);
                        break;
                }

                OnUIEventProcessed?.Invoke(uiEvent);
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("UI_EVENTS", $"Error processing UI event {uiEvent.Type}: {ex.Message}", this);
            }
        }

        /// <summary>
        /// Queue a UI event for processing
        /// </summary>
        public void QueueEvent(UIEvent uiEvent)
        {
            _eventQueue.Enqueue(uiEvent);
        }

        /// <summary>
        /// Queue a UI event with automatic timestamp
        /// </summary>
        public void QueueEvent(UIEventType type, GameObject source, object data = null)
        {
            var uiEvent = new UIEvent
            {
                Type = type,
                Source = source,
                Data = data,
                Timestamp = Time.realtimeSinceStartup
            };
            QueueEvent(uiEvent);
        }

        #endregion

        #region Event Type Handlers

        private void ProcessClickEvent(UIEvent uiEvent)
        {
            if (uiEvent.Source == null) return;

            // Cache interaction data for performance
            if (!_interactionCache.TryGetValue(uiEvent.Source, out var interactionData))
            {
                interactionData = new UIInteractionData
                {
                    LastInteractionTime = Time.realtimeSinceStartup,
                    InteractionCount = 0
                };
                _interactionCache[uiEvent.Source] = interactionData;
            }

            interactionData.LastInteractionTime = Time.realtimeSinceStartup;
            interactionData.InteractionCount++;

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_EVENTS", $"Click processed on {uiEvent.Source.name}", this);
        }

        private void ProcessHoverEvent(UIEvent uiEvent)
        {
            if (uiEvent.Source == null) return;

            // Update hover state efficiently
            var graphic = uiEvent.Source.GetComponent<Graphic>();
            if (graphic != null)
            {
                // Optimize hover state changes
                var isHovering = (bool)(uiEvent.Data ?? false);
                if (isHovering)
                {
                    graphic.color = Color.Lerp(graphic.color, Color.white, 0.1f);
                }
                else
                {
                    graphic.color = Color.Lerp(graphic.color, Color.gray, 0.1f);
                }
            }
        }

        private void ProcessFocusEvent(UIEvent uiEvent)
        {
            if (uiEvent.Source == null) return;

            var selectable = uiEvent.Source.GetComponent<Selectable>();
            if (selectable != null)
            {
                var isFocused = (bool)(uiEvent.Data ?? false);
                if (isFocused)
                {
                    selectable.Select();
                }
            }
        }

        private void ProcessKeyPressEvent(UIEvent uiEvent)
        {
            if (uiEvent.Data is KeyCode keyCode)
            {
                if (_keyboardShortcuts.TryGetValue(keyCode, out var action))
                {
                    action?.Invoke();
                }
            }
        }

        private void ProcessCustomEvent(UIEvent uiEvent)
        {
            if (uiEvent.Data is string eventName)
            {
                if (_registeredEvents.TryGetValue(eventName, out var action))
                {
                    action?.Invoke();
                }
            }
        }

        #endregion

        #region Input Optimization

        private void ProcessInputOptimizations()
        {
            if (!_enableInputOptimization) return;

            // Optimize input processing based on current context
            OptimizeMouseInteractions();
            ProcessKeyboardShortcuts();
        }

        private void OptimizeMouseInteractions()
        {
            // Only process mouse interactions when mouse is moving or clicking
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0) || HasMouseMoved())
            {
                var mousePosition = Input.mousePosition;
                var raycastResults = new List<RaycastResult>();

                // Perform optimized raycasting
                if (_raycaster != null)
                {
                    var eventData = new PointerEventData(EventSystem.current)
                    {
                        position = mousePosition
                    };

                    _raycaster.Raycast(eventData, raycastResults);

                    // Process only the top-most UI element
                    if (raycastResults.Count > 0)
                    {
                        var topResult = raycastResults[0];
                        ProcessMouseInteraction(topResult.gameObject, mousePosition);
                    }
                }
            }
        }

        private bool HasMouseMoved()
        {
            // Simple mouse movement detection (could be optimized further)
            var currentMousePos = Input.mousePosition;
            var mouseDelta = Vector3.Distance(currentMousePos, _lastMousePosition);
            _lastMousePosition = currentMousePos;
            return mouseDelta > 1.0f; // 1 pixel threshold
        }

        private Vector3 _lastMousePosition;

        private void ProcessMouseInteraction(GameObject target, Vector3 mousePosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                QueueEvent(UIEventType.Click, target, mousePosition);
            }
            else
            {
                QueueEvent(UIEventType.Hover, target, true);
            }
        }

        private void ProcessKeyboardShortcuts()
        {
            // Process registered keyboard shortcuts efficiently
            foreach (var shortcut in _keyboardShortcuts)
            {
                if (Input.GetKeyDown(shortcut.Key))
                {
                    QueueEvent(UIEventType.KeyPress, null, shortcut.Key);
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Register a keyboard shortcut
        /// </summary>
        public void RegisterKeyboardShortcut(KeyCode keyCode, System.Action action)
        {
            _keyboardShortcuts[keyCode] = action;

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_EVENTS", $"Registered keyboard shortcut: {keyCode}", this);
        }

        /// <summary>
        /// Unregister a keyboard shortcut
        /// </summary>
        public void UnregisterKeyboardShortcut(KeyCode keyCode)
        {
            _keyboardShortcuts.Remove(keyCode);
        }

        /// <summary>
        /// Register a custom event handler
        /// </summary>
        public void RegisterEventHandler(string eventName, System.Action action)
        {
            _registeredEvents[eventName] = action;

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_EVENTS", $"Registered event handler: {eventName}", this);
        }

        /// <summary>
        /// Unregister a custom event handler
        /// </summary>
        public void UnregisterEventHandler(string eventName)
        {
            _registeredEvents.Remove(eventName);
        }

        /// <summary>
        /// Get event processing statistics
        /// </summary>
        public EventProcessingStats GetEventStats()
        {
            return new EventProcessingStats
            {
                QueuedEvents = _eventQueue.Count,
                ProcessedEventsThisFrame = _eventsProcessedThisFrame,
                LastProcessingTime = _lastEventProcessTime,
                RegisteredShortcuts = _keyboardShortcuts.Count,
                RegisteredEvents = _registeredEvents.Count,
                CachedInteractions = _interactionCache.Count
            };
        }

        /// <summary>
        /// Clear event queue
        /// </summary>
        public void ClearEventQueue()
        {
            _eventQueue.Clear();

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_EVENTS", "Event queue cleared", this);
        }

        /// <summary>
        /// Refresh cached UI components
        /// </summary>
        public void RefreshUICache()
        {
            _cachedSelectables.Clear();
            _interactionCache.Clear();
            CacheUIComponents();

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_EVENTS", "UI component cache refreshed", this);
        }

        #endregion

        #region Cleanup

        private void CleanupEvents()
        {
            _eventQueue.Clear();
            _registeredEvents.Clear();
            _keyboardShortcuts.Clear();
            _interactionCache.Clear();
            _cachedSelectables.Clear();

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_EVENTS", "UIEventHandler cleanup completed", this);
        }

        #endregion
    }

    /// <summary>
    /// UI event types
    /// </summary>
    public enum UIEventType
    {
        Click,
        Hover,
        Focus,
        KeyPress,
        Custom
    }

    /// <summary>
    /// UI event data structure
    /// </summary>
    [System.Serializable]
    public struct UIEvent
    {
        public UIEventType Type;
        public GameObject Source;
        public object Data;
        public float Timestamp;
    }

    /// <summary>
    /// UI interaction tracking data
    /// </summary>
    [System.Serializable]
    public class UIInteractionData
    {
        public float LastInteractionTime;
        public int InteractionCount;
    }

    /// <summary>
    /// Event processing statistics
    /// </summary>
    [System.Serializable]
    public struct EventProcessingStats
    {
        public int QueuedEvents;
        public int ProcessedEventsThisFrame;
        public float LastProcessingTime;
        public int RegisteredShortcuts;
        public int RegisteredEvents;
        public int CachedInteractions;
    }
}