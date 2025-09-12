using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Keyboard Input Processor - Manages keyboard input and shortcuts
    /// Handles keyboard controls for camera movement and viewpoint switching
    /// Supports the hierarchical viewpoint system as described in gameplay document
    /// </summary>
    public class KeyboardInputProcessor : MonoBehaviour
    {
        [Header("Movement Keys")]
        [SerializeField] private KeyCode _forwardKey = KeyCode.W;
        [SerializeField] private KeyCode _backwardKey = KeyCode.S;
        [SerializeField] private KeyCode _leftKey = KeyCode.A;
        [SerializeField] private KeyCode _rightKey = KeyCode.D;
        [SerializeField] private KeyCode _upKey = KeyCode.E;
        [SerializeField] private KeyCode _downKey = KeyCode.Q;

        [Header("Rotation Keys")]
        [SerializeField] private KeyCode _rotateLeftKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode _rotateRightKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode _rotateUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode _rotateDownKey = KeyCode.DownArrow;

        [Header("Zoom Keys")]
        [SerializeField] private KeyCode _zoomInKey = KeyCode.Equals;
        [SerializeField] private KeyCode _zoomOutKey = KeyCode.Minus;
        [SerializeField] private KeyCode _zoomResetKey = KeyCode.Alpha0;

        [Header("Viewpoint Keys")]
        [SerializeField] private KeyCode _facilityViewKey = KeyCode.F1;
        [SerializeField] private KeyCode _roomViewKey = KeyCode.F2;
        [SerializeField] private KeyCode _tableViewKey = KeyCode.F3;
        [SerializeField] private KeyCode _plantViewKey = KeyCode.F4;
        [SerializeField] private KeyCode _overviewKey = KeyCode.Tab;

        [Header("Modifier Keys")]
        [SerializeField] private KeyCode _fastModifierKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode _slowModifierKey = KeyCode.LeftControl;

        [Header("Movement Settings")]
        [SerializeField] private float _baseMovementSpeed = 5f;
        [SerializeField] private float _fastMultiplier = 2f;
        [SerializeField] private float _slowMultiplier = 0.5f;
        [SerializeField] private float _rotationSpeed = 2f;
        [SerializeField] private float _zoomSpeed = 10f;

        // Input state
        private Vector3 _movementInput;
        private Vector2 _rotationInput;
        private float _zoomInput;

        // Modifier state
        private bool _isFastModifierPressed;
        private bool _isSlowModifierPressed;

        // Custom shortcuts
        private Dictionary<KeyCode, System.Action> _keyboardShortcuts = new Dictionary<KeyCode, System.Action>();

        private void Update()
        {
            UpdateMovementInput();
            UpdateRotationInput();
            UpdateZoomInput();
            UpdateModifierKeys();
            ProcessKeyboardShortcuts();
        }

        /// <summary>
        /// Updates movement input from keyboard
        /// </summary>
        private void UpdateMovementInput()
        {
            _movementInput = Vector3.zero;

            if (Input.GetKey(_forwardKey)) _movementInput.z += 1;
            if (Input.GetKey(_backwardKey)) _movementInput.z -= 1;
            if (Input.GetKey(_leftKey)) _movementInput.x -= 1;
            if (Input.GetKey(_rightKey)) _movementInput.x += 1;
            if (Input.GetKey(_upKey)) _movementInput.y += 1;
            if (Input.GetKey(_downKey)) _movementInput.y -= 1;

            // Normalize diagonal movement
            if (_movementInput.sqrMagnitude > 1f)
            {
                _movementInput.Normalize();
            }
        }

        /// <summary>
        /// Updates rotation input from keyboard
        /// </summary>
        private void UpdateRotationInput()
        {
            _rotationInput = Vector2.zero;

            if (Input.GetKey(_rotateLeftKey)) _rotationInput.x -= 1;
            if (Input.GetKey(_rotateRightKey)) _rotationInput.x += 1;
            if (Input.GetKey(_rotateUpKey)) _rotationInput.y += 1;
            if (Input.GetKey(_rotateDownKey)) _rotationInput.y -= 1;

            // Normalize diagonal rotation
            if (_rotationInput.sqrMagnitude > 1f)
            {
                _rotationInput.Normalize();
            }
        }

        /// <summary>
        /// Updates zoom input from keyboard
        /// </summary>
        private void UpdateZoomInput()
        {
            _zoomInput = 0f;

            if (Input.GetKey(_zoomInKey)) _zoomInput += 1;
            if (Input.GetKey(_zoomOutKey)) _zoomInput -= 1;

            if (Input.GetKeyDown(_zoomResetKey))
            {
                OnZoomReset();
            }
        }

        /// <summary>
        /// Updates modifier key state
        /// </summary>
        private void UpdateModifierKeys()
        {
            _isFastModifierPressed = Input.GetKey(_fastModifierKey);
            _isSlowModifierPressed = Input.GetKey(_slowModifierKey);
        }

        /// <summary>
        /// Processes keyboard shortcuts and viewpoint switching
        /// </summary>
        private void ProcessKeyboardShortcuts()
        {
            // Viewpoint switching (hierarchical viewpoint system)
            if (Input.GetKeyDown(_facilityViewKey))
            {
                OnSwitchToFacilityView();
            }
            else if (Input.GetKeyDown(_roomViewKey))
            {
                OnSwitchToRoomView();
            }
            else if (Input.GetKeyDown(_tableViewKey))
            {
                OnSwitchToTableView();
            }
            else if (Input.GetKeyDown(_plantViewKey))
            {
                OnSwitchToPlantView();
            }
            else if (Input.GetKeyDown(_overviewKey))
            {
                OnSwitchToOverview();
            }

            // Process custom shortcuts
            foreach (var shortcut in _keyboardShortcuts)
            {
                if (Input.GetKeyDown(shortcut.Key))
                {
                    shortcut.Value?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gets the current movement input vector
        /// </summary>
        public Vector3 GetMovementInput()
        {
            return _movementInput;
        }

        /// <summary>
        /// Gets the current rotation input vector
        /// </summary>
        public Vector2 GetRotationInput()
        {
            return _rotationInput;
        }

        /// <summary>
        /// Gets the current zoom input value
        /// </summary>
        public float GetZoomInput()
        {
            return _zoomInput;
        }

        /// <summary>
        /// Gets the current movement speed with modifiers applied
        /// </summary>
        public float GetCurrentMovementSpeed()
        {
            float speed = _baseMovementSpeed;

            if (_isFastModifierPressed)
            {
                speed *= _fastMultiplier;
            }
            else if (_isSlowModifierPressed)
            {
                speed *= _slowMultiplier;
            }

            return speed;
        }

        /// <summary>
        /// Gets the current rotation speed
        /// </summary>
        public float GetRotationSpeed()
        {
            return _rotationSpeed;
        }

        /// <summary>
        /// Gets the current zoom speed
        /// </summary>
        public float GetZoomSpeed()
        {
            return _zoomSpeed;
        }

        /// <summary>
        /// Checks if the fast modifier is pressed
        /// </summary>
        public bool IsFastModifierPressed()
        {
            return _isFastModifierPressed;
        }

        /// <summary>
        /// Checks if the slow modifier is pressed
        /// </summary>
        public bool IsSlowModifierPressed()
        {
            return _isSlowModifierPressed;
        }

        /// <summary>
        /// Registers a keyboard shortcut
        /// </summary>
        public void RegisterShortcut(KeyCode key, System.Action action)
        {
            if (_keyboardShortcuts.ContainsKey(key))
            {
                _keyboardShortcuts[key] = action;
            }
            else
            {
                _keyboardShortcuts.Add(key, action);
            }

            ChimeraLogger.Log($"[KeyboardInputProcessor] Registered shortcut: {key}");
        }

        /// <summary>
        /// Unregisters a keyboard shortcut
        /// </summary>
        public void UnregisterShortcut(KeyCode key)
        {
            if (_keyboardShortcuts.Remove(key))
            {
                ChimeraLogger.Log($"[KeyboardInputProcessor] Unregistered shortcut: {key}");
            }
        }

        /// <summary>
        /// Gets all registered keyboard shortcuts
        /// </summary>
        public Dictionary<KeyCode, string> GetKeyboardShortcuts()
        {
            var shortcuts = new Dictionary<KeyCode, string>();

            // Built-in shortcuts
            shortcuts[_facilityViewKey] = "Switch to Facility View";
            shortcuts[_roomViewKey] = "Switch to Room View";
            shortcuts[_tableViewKey] = "Switch to Table View";
            shortcuts[_plantViewKey] = "Switch to Plant View";
            shortcuts[_overviewKey] = "Switch to Overview";
            shortcuts[_zoomResetKey] = "Reset Zoom";

            // Custom shortcuts (would need to be enhanced to include descriptions)
            foreach (var shortcut in _keyboardShortcuts.Keys)
            {
                if (!shortcuts.ContainsKey(shortcut))
                {
                    shortcuts[shortcut] = "Custom Shortcut";
                }
            }

            return shortcuts;
        }

        /// <summary>
        /// Sets the base movement speed
        /// </summary>
        public void SetBaseMovementSpeed(float speed)
        {
            _baseMovementSpeed = speed;
        }

        /// <summary>
        /// Sets the rotation speed
        /// </summary>
        public void SetRotationSpeed(float speed)
        {
            _rotationSpeed = speed;
        }

        /// <summary>
        /// Sets the zoom speed
        /// </summary>
        public void SetZoomSpeed(float speed)
        {
            _zoomSpeed = speed;
        }

        /// <summary>
        /// Sets the movement key bindings
        /// </summary>
        public void SetMovementKeys(KeyCode forward, KeyCode backward, KeyCode left, KeyCode right, KeyCode up, KeyCode down)
        {
            _forwardKey = forward;
            _backwardKey = backward;
            _leftKey = left;
            _rightKey = right;
            _upKey = up;
            _downKey = down;
        }

        /// <summary>
        /// Sets the rotation key bindings
        /// </summary>
        public void SetRotationKeys(KeyCode left, KeyCode right, KeyCode up, KeyCode down)
        {
            _rotateLeftKey = left;
            _rotateRightKey = right;
            _rotateUpKey = up;
            _rotateDownKey = down;
        }

        /// <summary>
        /// Sets the zoom key bindings
        /// </summary>
        public void SetZoomKeys(KeyCode zoomIn, KeyCode zoomOut, KeyCode reset)
        {
            _zoomInKey = zoomIn;
            _zoomOutKey = zoomOut;
            _zoomResetKey = reset;
        }

        // Virtual methods for integration with camera system (hierarchical viewpoint system)

        protected virtual void OnSwitchToFacilityView()
        {
            ChimeraLogger.Log("[KeyboardInputProcessor] Switching to Facility View");
            // Would integrate with camera system to switch to facility overview
        }

        protected virtual void OnSwitchToRoomView()
        {
            ChimeraLogger.Log("[KeyboardInputProcessor] Switching to Room View");
            // Would integrate with camera system to focus on current room
        }

        protected virtual void OnSwitchToTableView()
        {
            ChimeraLogger.Log("[KeyboardInputProcessor] Switching to Table View");
            // Would integrate with camera system to focus on grow tables/racks
        }

        protected virtual void OnSwitchToPlantView()
        {
            ChimeraLogger.Log("[KeyboardInputProcessor] Switching to Plant View");
            // Would integrate with camera system to close-up on plants
        }

        protected virtual void OnSwitchToOverview()
        {
            ChimeraLogger.Log("[KeyboardInputProcessor] Switching to Overview");
            // Would integrate with camera system to main overview (as mentioned in gameplay document)
        }

        protected virtual void OnZoomReset()
        {
            ChimeraLogger.Log("[KeyboardInputProcessor] Zoom reset");
            // Would integrate with camera system to reset zoom level
        }
    }
}

