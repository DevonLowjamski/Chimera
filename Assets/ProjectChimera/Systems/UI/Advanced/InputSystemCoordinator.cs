using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// SIMPLE: Basic input coordinator aligned with Project Chimera's input needs.
    /// Focuses on essential input handling without complex coordination systems.
    /// </summary>
    public class InputSystemCoordinator : MonoBehaviour
    {
        [Header("Basic Input Settings")]
        [SerializeField] private bool _enableBasicInput = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _inputSensitivity = 1f;

        // Basic input tracking
        private Vector2 _moveInput = Vector2.zero;
        private bool _interactPressed = false;
        private bool _menuPressed = false;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for basic input
        /// </summary>
        public event System.Action<Vector2> OnMoveInput;
        public event System.Action OnInteractPressed;
        public event System.Action OnMenuPressed;

        /// <summary>
        /// Initialize basic input system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[InputSystemCoordinator] Initialized successfully");
            }
        }

        /// <summary>
        /// Update basic input handling
        /// </summary>
        public void UpdateInput()
        {
            if (!_enableBasicInput) return;

            // Basic keyboard input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            _moveInput = new Vector2(horizontal, vertical) * _inputSensitivity;

            if (_moveInput != Vector2.zero)
            {
                OnMoveInput?.Invoke(_moveInput);
            }

            // Basic button inputs
            if (Input.GetButtonDown("Interact"))
            {
                _interactPressed = true;
                OnInteractPressed?.Invoke();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("[InputSystemCoordinator] Interact pressed");
                }
            }
            else
            {
                _interactPressed = false;
            }

            if (Input.GetButtonDown("Menu"))
            {
                _menuPressed = true;
                OnMenuPressed?.Invoke();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("[InputSystemCoordinator] Menu pressed");
                }
            }
            else
            {
                _menuPressed = false;
            }
        }

        /// <summary>
        /// Get current move input
        /// </summary>
        public Vector2 GetMoveInput()
        {
            return _moveInput;
        }

        /// <summary>
        /// Check if interact is pressed
        /// </summary>
        public bool IsInteractPressed()
        {
            return _interactPressed;
        }

        /// <summary>
        /// Check if menu is pressed
        /// </summary>
        public bool IsMenuPressed()
        {
            return _menuPressed;
        }

        /// <summary>
        /// Set input sensitivity
        /// </summary>
        public void SetInputSensitivity(float sensitivity)
        {
            _inputSensitivity = Mathf.Max(0.1f, sensitivity);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[InputSystemCoordinator] Input sensitivity set to {_inputSensitivity:F2}");
            }
        }

        /// <summary>
        /// Enable or disable basic input
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _enableBasicInput = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[InputSystemCoordinator] Basic input {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Get input statistics
        /// </summary>
        public InputStatistics GetInputStatistics()
        {
            return new InputStatistics
            {
                IsInitialized = _isInitialized,
                CurrentMoveInput = _moveInput,
                InteractPressed = _interactPressed,
                MenuPressed = _menuPressed,
                InputSensitivity = _inputSensitivity,
                EnableInput = _enableBasicInput
            };
        }

        /// <summary>
        /// Reset input state
        /// </summary>
        public void ResetInputState()
        {
            _moveInput = Vector2.zero;
            _interactPressed = false;
            _menuPressed = false;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[InputSystemCoordinator] Input state reset");
            }
        }
    }

    /// <summary>
    /// Basic input statistics
    /// </summary>
    [System.Serializable]
    public class InputStatistics
    {
        public bool IsInitialized;
        public Vector2 CurrentMoveInput;
        public bool InteractPressed;
        public bool MenuPressed;
        public float InputSensitivity;
        public bool EnableInput;
    }
}
