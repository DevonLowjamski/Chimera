using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// BASIC: Simple input action processor for Project Chimera's UI system.
    /// Focuses on essential input processing without complex action systems and search modes.
    /// </summary>
    public class InputActionProcessor : MonoBehaviour, ITickable
    {
        [Header("Basic Input Settings")]
        [SerializeField] private bool _enableBasicProcessing = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _inputCooldown = 0.2f;

        // Basic input tracking
        private float _lastInputTime = 0f;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for input actions
        /// </summary>
        public event System.Action OnMenuToggle;
        public event System.Action OnSelect;
        public event System.Action OnCancel;
        public event System.Action OnNavigateUp;
        public event System.Action OnNavigateDown;
        public event System.Action OnNavigateLeft;
        public event System.Action OnNavigateRight;

        /// <summary>
        /// Initialize basic input processor
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "InputActionProcessor initialized", this);
            }
        }

        /// <summary>
        /// Process menu toggle input
        /// </summary>
        public void ProcessMenuToggle()
        {
            if (!CanProcessInput()) return;

            OnMenuToggle?.Invoke();
            _lastInputTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Menu toggle", this);
            }
        }

        /// <summary>
        /// Process select input
        /// </summary>
        public void ProcessSelect()
        {
            if (!CanProcessInput()) return;

            OnSelect?.Invoke();
            _lastInputTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Select", this);
            }
        }

        /// <summary>
        /// Process cancel input
        /// </summary>
        public void ProcessCancel()
        {
            if (!CanProcessInput()) return;

            OnCancel?.Invoke();
            _lastInputTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Cancel", this);
            }
        }

        /// <summary>
        /// Process navigation inputs
        /// </summary>
        public void ProcessNavigateUp()
        {
            if (!CanProcessInput()) return;

            OnNavigateUp?.Invoke();
            _lastInputTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Navigate Up", this);
            }
        }

        public void ProcessNavigateDown()
        {
            if (!CanProcessInput()) return;

            OnNavigateDown?.Invoke();
            _lastInputTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Navigate Down", this);
            }
        }

        public void ProcessNavigateLeft()
        {
            if (!CanProcessInput()) return;

            OnNavigateLeft?.Invoke();
            _lastInputTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Navigate Left", this);
            }
        }

        public void ProcessNavigateRight()
        {
            if (!CanProcessInput()) return;

            OnNavigateRight?.Invoke();
            _lastInputTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Navigate Right", this);
            }
        }

        /// <summary>
        /// Process keyboard input automatically
        /// </summary>
    [SerializeField] private float _tickInterval = 0.1f; // Configurable update frequency
    private float _lastTickTime;

    public int TickPriority => 50; // Lower priority for complex updates
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
        _lastTickTime += deltaTime;
        if (_lastTickTime >= _tickInterval)
        {
            _lastTickTime = 0f;
                if (!_enableBasicProcessing || !_isInitialized) return;

                // Menu toggle
                if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
                    ProcessMenuToggle();

                // Select
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    ProcessSelect();

                // Cancel
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetMouseButtonDown(1))
                    ProcessCancel();

                // Navigation
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                    ProcessNavigateUp();

                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                    ProcessNavigateDown();

                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                    ProcessNavigateLeft();

                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                    ProcessNavigateRight();
        }
    }

    private void Awake()
    {
        UpdateOrchestrator.Instance?.RegisterTickable(this);
    }

    private void OnDestroy()
    {
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
    }

        /// <summary>
        /// Set input processing enabled state
        /// </summary>
        public void SetInputProcessingEnabled(bool enabled)
        {
            _enableBasicProcessing = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Input processing toggled", this);
            }
        }

        /// <summary>
        /// Check if input can be processed (cooldown check)
        /// </summary>
        public bool CanProcessInput()
        {
            return _enableBasicProcessing && _isInitialized && (Time.time - _lastInputTime) >= _inputCooldown;
        }

        /// <summary>
        /// Get input processor statistics
        /// </summary>
        public InputProcessorStats GetStats()
        {
            return new InputProcessorStats
            {
                IsEnabled = _enableBasicProcessing,
                IsInitialized = _isInitialized,
                CanProcessInput = CanProcessInput(),
                InputCooldown = _inputCooldown,
                TimeSinceLastInput = Time.time - _lastInputTime
            };
        }

        /// <summary>
        /// Force reset input cooldown
        /// </summary>
        public void ResetCooldown()
        {
            _lastInputTime = 0f;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI/INPUT", "Input cooldown reset", this);
            }
        }
    }

    /// <summary>
    /// Input processor statistics
    /// </summary>
    [System.Serializable]
    public struct InputProcessorStats
    {
        public bool IsEnabled;
        public bool IsInitialized;
        public bool CanProcessInput;
        public float InputCooldown;
        public float TimeSinceLastInput;
    }
}
