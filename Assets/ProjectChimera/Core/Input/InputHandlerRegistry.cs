using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// REFACTORED: Input Handler Registry
    /// Single Responsibility: Managing registered input handlers and notifications
    /// Extracted from OptimizedInputManager for better separation of concerns
    /// </summary>
    public class InputHandlerRegistry : MonoBehaviour
    {
        [Header("Registry Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxHandlers = 100;
        [SerializeField] private bool _enableHandlerValidation = true;
        [SerializeField] private bool _notifyHandlersAsync = false;

        // Handler management
        private readonly List<IInputHandler> _inputHandlers = new List<IInputHandler>();
        private readonly Dictionary<System.Type, List<IInputHandler>> _handlersByType = new Dictionary<System.Type, List<IInputHandler>>();
        private readonly HashSet<IInputHandler> _activeHandlers = new HashSet<IInputHandler>();

        // Statistics
        private InputHandlerRegistryStats _stats = new InputHandlerRegistryStats();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event System.Action<IInputHandler> OnHandlerRegistered;
        public event System.Action<IInputHandler> OnHandlerUnregistered;
        public event System.Action<int> OnHandlerCountChanged;
        public event System.Action<IInputHandler, InputEvent> OnHandlerNotified;

        public bool IsInitialized => _isInitialized;
        public InputHandlerRegistryStats Stats => _stats;
        public int RegisteredHandlerCount => _inputHandlers.Count;
        public int ActiveHandlerCount => _activeHandlers.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            _inputHandlers.Clear();
            _handlersByType.Clear();
            _activeHandlers.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", "Input Handler Registry initialized", this);
            }
        }

        /// <summary>
        /// Register input handler for optimized events
        /// </summary>
        public bool RegisterInputHandler(IInputHandler handler)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("INPUT", "Cannot register handler - registry not initialized", this);
                }
                return false;
            }

            if (handler == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("INPUT", "Cannot register null handler", this);
                }
                return false;
            }

            if (_inputHandlers.Contains(handler))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("INPUT", $"Handler already registered: {handler.GetType().Name}", this);
                }
                return false;
            }

            if (_inputHandlers.Count >= _maxHandlers)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("INPUT", $"Cannot register handler - maximum limit reached ({_maxHandlers})", this);
                }
                _stats.RegistrationsFailed++;
                return false;
            }

            // Validate handler if enabled
            if (_enableHandlerValidation && !ValidateHandler(handler))
            {
                _stats.RegistrationsFailed++;
                return false;
            }

            // Add to main list
            _inputHandlers.Add(handler);
            _activeHandlers.Add(handler);

            // Add to type-specific dictionary
            var handlerType = handler.GetType();
            if (!_handlersByType.TryGetValue(handlerType, out var handlerList))
            {
                handlerList = new List<IInputHandler>();
                _handlersByType[handlerType] = handlerList;
            }
            handlerList.Add(handler);

            _stats.HandlersRegistered++;
            _stats.CurrentHandlers++;

            OnHandlerRegistered?.Invoke(handler);
            OnHandlerCountChanged?.Invoke(_inputHandlers.Count);

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Registered input handler: {handler.GetType().Name} (Total: {_inputHandlers.Count})", this);
            }

            return true;
        }

        /// <summary>
        /// Unregister input handler
        /// </summary>
        public bool UnregisterInputHandler(IInputHandler handler)
        {
            if (!_isInitialized || handler == null)
                return false;

            bool removed = _inputHandlers.Remove(handler);
            if (removed)
            {
                _activeHandlers.Remove(handler);

                // Remove from type-specific dictionary
                var handlerType = handler.GetType();
                if (_handlersByType.TryGetValue(handlerType, out var handlerList))
                {
                    handlerList.Remove(handler);
                    if (handlerList.Count == 0)
                    {
                        _handlersByType.Remove(handlerType);
                    }
                }

                _stats.HandlersUnregistered++;
                _stats.CurrentHandlers--;

                OnHandlerUnregistered?.Invoke(handler);
                OnHandlerCountChanged?.Invoke(_inputHandlers.Count);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("INPUT", $"Unregistered input handler: {handler.GetType().Name} (Total: {_inputHandlers.Count})", this);
                }
            }

            return removed;
        }

        /// <summary>
        /// Notify all registered handlers of input event
        /// </summary>
        public void NotifyHandlers(InputEvent inputEvent)
        {
            if (!_isInitialized || _activeHandlers.Count == 0)
                return;

            var notificationStartTime = Time.realtimeSinceStartup;
            int successfulNotifications = 0;
            int failedNotifications = 0;

            // Create a snapshot to avoid collection modification issues
            var handlersSnapshot = _activeHandlers.ToArray();

            foreach (var handler in handlersSnapshot)
            {
                try
                {
                    if (_activeHandlers.Contains(handler)) // Verify handler is still active
                    {
                        handler.HandleInputEvent(inputEvent);
                        OnHandlerNotified?.Invoke(handler, inputEvent);
                        successfulNotifications++;
                    }
                }
                catch (System.Exception ex)
                {
                    failedNotifications++;
                    _stats.NotificationErrors++;

                    if (_enableLogging)
                    {
                        ChimeraLogger.LogError("INPUT", $"Error notifying handler {handler.GetType().Name}: {ex.Message}", this);
                    }
                }
            }

            // Update statistics
            var notificationTime = Time.realtimeSinceStartup - notificationStartTime;
            _stats.NotificationsSent += successfulNotifications;
            _stats.TotalNotificationTime += notificationTime;
            _stats.AverageNotificationTime = _stats.NotificationsSent > 0 ? _stats.TotalNotificationTime / _stats.NotificationsSent : 0f;

            if (notificationTime > _stats.MaxNotificationTime)
                _stats.MaxNotificationTime = notificationTime;
        }

        /// <summary>
        /// Get handlers of specific type
        /// </summary>
        public List<T> GetHandlersOfType<T>() where T : class, IInputHandler
        {
            var result = new List<T>();
            var targetType = typeof(T);

            if (_handlersByType.TryGetValue(targetType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    if (handler is T typedHandler)
                    {
                        result.Add(typedHandler);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check if handler is registered
        /// </summary>
        public bool IsHandlerRegistered(IInputHandler handler)
        {
            return handler != null && _activeHandlers.Contains(handler);
        }

        /// <summary>
        /// Get all registered handler types
        /// </summary>
        public System.Type[] GetRegisteredHandlerTypes()
        {
            return _handlersByType.Keys.ToArray();
        }

        /// <summary>
        /// Set handler as active/inactive
        /// </summary>
        public bool SetHandlerActive(IInputHandler handler, bool active)
        {
            if (!_isInitialized || handler == null || !_inputHandlers.Contains(handler))
                return false;

            bool wasActive = _activeHandlers.Contains(handler);

            if (active && !wasActive)
            {
                _activeHandlers.Add(handler);
                return true;
            }
            else if (!active && wasActive)
            {
                _activeHandlers.Remove(handler);
                return true;
            }

            return false; // No change needed
        }

        /// <summary>
        /// Clear all registered handlers
        /// </summary>
        public void ClearAllHandlers()
        {
            var clearedCount = _inputHandlers.Count;

            _inputHandlers.Clear();
            _handlersByType.Clear();
            _activeHandlers.Clear();

            _stats.CurrentHandlers = 0;

            OnHandlerCountChanged?.Invoke(0);

            if (_enableLogging && clearedCount > 0)
            {
                ChimeraLogger.Log("INPUT", $"Cleared all handlers ({clearedCount} handlers removed)", this);
            }
        }

        /// <summary>
        /// Get registry status
        /// </summary>
        public InputHandlerRegistryStatus GetRegistryStatus()
        {
            return new InputHandlerRegistryStatus
            {
                RegisteredHandlers = _inputHandlers.Count,
                ActiveHandlers = _activeHandlers.Count,
                HandlerTypes = _handlersByType.Keys.Count,
                MaxHandlers = _maxHandlers,
                ValidationEnabled = _enableHandlerValidation,
                RegistryUtilization = _maxHandlers > 0 ? (float)_inputHandlers.Count / _maxHandlers : 0f
            };
        }

        /// <summary>
        /// Set registry parameters
        /// </summary>
        public void SetRegistryParameters(int maxHandlers, bool enableValidation, bool asyncNotification)
        {
            _maxHandlers = Mathf.Max(1, maxHandlers);
            _enableHandlerValidation = enableValidation;
            _notifyHandlersAsync = asyncNotification;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Registry parameters updated: MaxHandlers={_maxHandlers}, Validation={_enableHandlerValidation}, Async={_notifyHandlersAsync}", this);
            }
        }

        /// <summary>
        /// Validate input handler
        /// </summary>
        private bool ValidateHandler(IInputHandler handler)
        {
            try
            {
                // Basic validation - handler implements IInputHandler interface
                // Compiler guarantees HandleInputEvent exists, no reflection needed
                if (handler == null)
                {
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogWarning("INPUT", "Null handler provided for validation", this);
                    }
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("INPUT", $"Handler validation failed: {ex.Message}", this);
                }
                return false;
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new InputHandlerRegistryStats
            {
                HandlersRegistered = 0,
                HandlersUnregistered = 0,
                RegistrationsFailed = 0,
                CurrentHandlers = 0,
                NotificationsSent = 0,
                NotificationErrors = 0,
                TotalNotificationTime = 0f,
                AverageNotificationTime = 0f,
                MaxNotificationTime = 0f
            };
        }
    }

    /// <summary>
    /// Input handler registry statistics
    /// </summary>
    [System.Serializable]
    public struct InputHandlerRegistryStats
    {
        public int HandlersRegistered;
        public int HandlersUnregistered;
        public int RegistrationsFailed;
        public int CurrentHandlers;
        public int NotificationsSent;
        public int NotificationErrors;
        public float TotalNotificationTime;
        public float AverageNotificationTime;
        public float MaxNotificationTime;
    }

    /// <summary>
    /// Input handler registry status
    /// </summary>
    [System.Serializable]
    public struct InputHandlerRegistryStatus
    {
        public int RegisteredHandlers;
        public int ActiveHandlers;
        public int HandlerTypes;
        public int MaxHandlers;
        public bool ValidationEnabled;
        public float RegistryUtilization;
    }
}