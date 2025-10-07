using UnityEngine;
using System;
using ProjectChimera.Core.Memory;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// REFACTORED: Input Event Processor
    /// Single Responsibility: Event queuing, buffering, and distribution
    /// Extracted from OptimizedInputManager for better separation of concerns
    /// </summary>
    public class InputEventProcessor : MonoBehaviour
    {
        [Header("Event Processing Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxInputEventsPerFrame = 50;
        [SerializeField] private bool _useInputBuffering = true;
        [SerializeField] private int _inputBufferSize = 100;

        // Event processing
        private readonly MemoryOptimizedQueue<InputEvent> _inputEventQueue = new MemoryOptimizedQueue<InputEvent>();
        private int _inputEventsThisFrame;

        // State tracking
        private bool _isInitialized = false;
        private InputEventProcessorStats _stats = new InputEventProcessorStats();

        // Events
        public event Action<Vector2> OnOptimizedMouseMove;
        public event Action<Vector2> OnMouseClick;
        public event Action<Vector2> OnMouseDrag;
        public event Action<float> OnScrollWheel;
        public event Action<KeyCode> OnKeyPressed;
        public event Action<KeyCode> OnKeyReleased;
        public event Action<InputEvent> OnInputEventProcessed;

        public bool IsInitialized => _isInitialized;
        public InputEventProcessorStats Stats => _stats;
        public int QueuedEventCount => _inputEventQueue.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            _inputEventQueue.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", "Input Event Processor initialized", this);
            }
        }

        /// <summary>
        /// Queue input event for processing
        /// </summary>
        public void QueueInputEvent(InputEvent inputEvent)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("INPUT", "Cannot queue event - processor not initialized", this);
                }
                return;
            }

            if (_useInputBuffering)
            {
                if (_inputEventQueue.Count < _inputBufferSize)
                {
                    _inputEventQueue.Enqueue(inputEvent);
                    _stats.EventsQueued++;
                }
                else
                {
                    _stats.EventsDropped++;
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogWarning("INPUT", "Input buffer full, dropping event", this);
                    }
                }
            }
            else
            {
                ProcessInputEvent(inputEvent);
            }
        }

        /// <summary>
        /// Process input event buffer
        /// </summary>
        public void ProcessInputBuffer()
        {
            if (!_isInitialized) return;

            _inputEventsThisFrame = 0;

            while (_inputEventQueue.Count > 0 && _inputEventsThisFrame < _maxInputEventsPerFrame)
            {
                if (_inputEventQueue.TryDequeue(out var inputEvent))
                {
                    ProcessInputEvent(inputEvent);
                    _inputEventsThisFrame++;
                }
            }

            _stats.ProcessingCycles++;
        }

        /// <summary>
        /// Process individual input event
        /// </summary>
        public void ProcessInputEvent(InputEvent inputEvent)
        {
            if (!_isInitialized) return;

            var processingStartTime = Time.realtimeSinceStartup;

            try
            {
                // Fire specific event handlers
                switch (inputEvent.Type)
                {
                    case InputEventType.MouseMove:
                        OnOptimizedMouseMove?.Invoke(inputEvent.MousePosition);
                        break;
                    case InputEventType.MouseClick:
                        OnMouseClick?.Invoke(inputEvent.MousePosition);
                        break;
                    case InputEventType.MouseDrag:
                        OnMouseDrag?.Invoke(inputEvent.MousePosition);
                        break;
                    case InputEventType.ScrollWheel:
                        OnScrollWheel?.Invoke(inputEvent.ScrollDelta);
                        break;
                    case InputEventType.KeyPress:
                        OnKeyPressed?.Invoke(inputEvent.KeyCode);
                        break;
                    case InputEventType.KeyRelease:
                        OnKeyReleased?.Invoke(inputEvent.KeyCode);
                        break;
                }

                // Fire general event handler
                OnInputEventProcessed?.Invoke(inputEvent);

                _stats.EventsProcessed++;

                // Track processing time
                var processingTime = Time.realtimeSinceStartup - processingStartTime;
                _stats.TotalProcessingTime += processingTime;
                _stats.AverageProcessingTime = _stats.TotalProcessingTime / _stats.EventsProcessed;

                if (processingTime > _stats.MaxProcessingTime)
                    _stats.MaxProcessingTime = processingTime;
            }
            catch (System.Exception ex)
            {
                _stats.ProcessingErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("INPUT", $"Error processing input event {inputEvent.Type}: {ex.Message}", this);
                }
            }
        }

        /// <summary>
        /// Clear all queued events
        /// </summary>
        public void ClearEventQueue()
        {
            var clearedCount = _inputEventQueue.Count;
            _inputEventQueue.Clear();

            if (_enableLogging && clearedCount > 0)
            {
                ChimeraLogger.Log("INPUT", $"Cleared {clearedCount} queued events", this);
            }
        }

        /// <summary>
        /// Set event processing parameters
        /// </summary>
        public void SetProcessingParameters(int maxEventsPerFrame, int bufferSize, bool useBuffering)
        {
            _maxInputEventsPerFrame = Mathf.Max(1, maxEventsPerFrame);
            _inputBufferSize = Mathf.Max(1, bufferSize);
            _useInputBuffering = useBuffering;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Processing parameters updated: MaxEvents={_maxInputEventsPerFrame}, BufferSize={_inputBufferSize}, UseBuffering={_useInputBuffering}", this);
            }
        }

        /// <summary>
        /// Get processing status
        /// </summary>
        public InputProcessingStatus GetProcessingStatus()
        {
            return new InputProcessingStatus
            {
                QueuedEvents = _inputEventQueue.Count,
                EventsThisFrame = _inputEventsThisFrame,
                MaxEventsPerFrame = _maxInputEventsPerFrame,
                BufferUtilization = _inputBufferSize > 0 ? (float)_inputEventQueue.Count / _inputBufferSize : 0f,
                IsBufferingEnabled = _useInputBuffering
            };
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new InputEventProcessorStats
            {
                EventsQueued = 0,
                EventsProcessed = 0,
                EventsDropped = 0,
                ProcessingErrors = 0,
                ProcessingCycles = 0,
                TotalProcessingTime = 0f,
                AverageProcessingTime = 0f,
                MaxProcessingTime = 0f
            };
        }

        private void OnDestroy()
        {
            _inputEventQueue?.Dispose();
        }
    }

    /// <summary>
    /// Input event processor statistics
    /// </summary>
    [System.Serializable]
    public struct InputEventProcessorStats
    {
        public int EventsQueued;
        public int EventsProcessed;
        public int EventsDropped;
        public int ProcessingErrors;
        public int ProcessingCycles;
        public float TotalProcessingTime;
        public float AverageProcessingTime;
        public float MaxProcessingTime;
    }

    /// <summary>
    /// Input processing status
    /// </summary>
    [System.Serializable]
    public struct InputProcessingStatus
    {
        public int QueuedEvents;
        public int EventsThisFrame;
        public int MaxEventsPerFrame;
        public float BufferUtilization;
        public bool IsBufferingEnabled;
    }
}