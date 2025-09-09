using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Menus
{
    /// <summary>
    /// Handles input detection and processing for contextual menus.
    /// Manages mouse clicks, keyboard shortcuts, and gesture recognition.
    /// </summary>
    public class MenuInputHandler : MonoBehaviour
    {
        private MenuCore _menuCore;
        
        [Header("Input Settings")]
        [SerializeField] private bool _enableRightClickMenu = true;
        [SerializeField] private bool _enableKeyboardShortcuts = true;
        [SerializeField] private bool _enableTouchGestures = false;
        [SerializeField] private float _rightClickHoldTime = 0.5f;
        [SerializeField] private float _longPressThreshold = 1.0f;

        // Input state tracking
        private bool _isRightMousePressed = false;
        private float _rightMousePressTime = 0f;
        private Vector3 _rightMousePressPosition = Vector3.zero;
        private bool _isLongPressing = false;

        // Touch input state
        private bool _isTouchPressed = false;
        private float _touchPressTime = 0f;
        private Vector2 _touchPressPosition = Vector2.zero;

        // Gesture detection
        private const float MaxTapDistance = 50f;
        private const float DoubleTapTimeWindow = 0.3f;
        private float _lastTapTime = 0f;
        private Vector2 _lastTapPosition = Vector2.zero;

        public void Initialize(MenuCore menuCore)
        {
            _menuCore = menuCore;
        }

        public void HandleInput(float deltaTime)
        {
            if (!_menuCore.EnableModeContextualMenus) return;

            // Handle mouse input
            HandleMouseInput(deltaTime);

            // Handle keyboard input
            if (_enableKeyboardShortcuts)
            {
                HandleKeyboardInput();
            }

            // Handle touch input
            if (_enableTouchGestures && Input.touchSupported)
            {
                HandleTouchInput(deltaTime);
            }

            // Handle menu dismissal
            HandleMenuDismissal();
        }

        private void HandleMouseInput(float deltaTime)
        {
            // Handle right-click context menu
            if (_enableRightClickMenu)
            {
                HandleRightClickInput(deltaTime);
            }

            // Handle mouse wheel for quick actions
            HandleMouseWheelInput();
        }

        private void HandleRightClickInput(float deltaTime)
        {
            // Right mouse button pressed
            if (Input.GetMouseButtonDown(1))
            {
                _isRightMousePressed = true;
                _rightMousePressTime = 0f;
                _rightMousePressPosition = Input.mousePosition;
                _isLongPressing = false;

                LogDebug("Right mouse button pressed");
            }

            // Right mouse button held
            if (_isRightMousePressed)
            {
                _rightMousePressTime += deltaTime;

                // Check for long press
                if (!_isLongPressing && _rightMousePressTime >= _longPressThreshold)
                {
                    _isLongPressing = true;
                    OnLongPress(_rightMousePressPosition);
                }

                // Check if mouse moved too far (cancel context menu)
                float mouseDelta = Vector3.Distance(Input.mousePosition, _rightMousePressPosition);
                if (mouseDelta > MaxTapDistance)
                {
                    CancelRightClickAction();
                }
            }

            // Right mouse button released
            if (Input.GetMouseButtonUp(1) && _isRightMousePressed)
            {
                if (!_isLongPressing && _rightMousePressTime < _rightClickHoldTime)
                {
                    // Quick right-click
                    OnRightClick(_rightMousePressPosition);
                }
                else if (_rightMousePressTime >= _rightClickHoldTime)
                {
                    // Hold release
                    OnRightClickHoldRelease(_rightMousePressPosition);
                }

                _isRightMousePressed = false;
                _isLongPressing = false;
            }
        }

        private void HandleMouseWheelInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.1f && _menuCore.IsMenuVisible)
            {
                // Mouse wheel can be used for menu navigation in the future
                LogDebug($"Mouse wheel scroll: {scroll}");
            }
        }

        private void HandleKeyboardInput()
        {
            // Context menu shortcut (usually right-click equivalent)
            if (Input.GetKeyDown(KeyCode.Menu) || 
                (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F10)))
            {
                Vector3 centerScreen = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                OnContextMenuShortcut(centerScreen);
            }

            // Quick action shortcuts
            HandleQuickActionShortcuts();
        }

        private void HandleQuickActionShortcuts()
        {
            if (!_menuCore.EnableQuickActions) return;

            // Common shortcuts for different modes
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    OnQuickAction("QuickInspect");
                }
                else if (Input.GetKeyDown(KeyCode.R))
                {
                    OnQuickAction("QuickRemove");
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    OnQuickAction("QuickMove");
                }
            }

            // Function key shortcuts
            if (Input.GetKeyDown(KeyCode.F1))
            {
                OnQuickAction("ShowHelp");
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                OnQuickAction("QuickRename");
            }
        }

        private void HandleTouchInput(float deltaTime)
        {
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touch);
                    break;

                case TouchPhase.Stationary:
                case TouchPhase.Moved:
                    OnTouchMoved(touch, deltaTime);
                    break;

                case TouchPhase.Ended:
                    OnTouchEnded(touch);
                    break;

                case TouchPhase.Canceled:
                    OnTouchCanceled();
                    break;
            }
        }

        private void HandleMenuDismissal()
        {
            // Hide menu when clicking elsewhere or pressing escape
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (_menuCore.IsMenuVisible)
                {
                    _menuCore.HideContextMenu();
                    LogDebug("Menu dismissed by user input");
                }
            }
        }

        #region Input Event Handlers

        private void OnRightClick(Vector3 mousePosition)
        {
            LogDebug($"Right-click detected at {mousePosition}");
            
            // Detect what object is under the cursor
            GameObject targetObject = GetObjectUnderCursor(mousePosition);
            
            // Show context menu
            _menuCore.ShowContextMenu(mousePosition, targetObject);
        }

        private void OnRightClickHoldRelease(Vector3 mousePosition)
        {
            LogDebug($"Right-click hold released at {mousePosition}");
            // Could be used for alternative menu behavior
        }

        private void OnLongPress(Vector3 position)
        {
            LogDebug($"Long press detected at {position}");
            
            // Show extended context menu for long press
            GameObject targetObject = GetObjectUnderCursor(position);
            _menuCore.ShowContextMenu(position, targetObject);
        }

        private void OnContextMenuShortcut(Vector3 position)
        {
            LogDebug($"Context menu shortcut triggered at {position}");
            
            // Show context menu at center or selected object
            GameObject selectedObject = _menuCore.SelectedObject;
            _menuCore.ShowContextMenu(position, selectedObject);
        }

        private void OnQuickAction(string actionName)
        {
            LogDebug($"Quick action triggered: {actionName}");
            
            // Execute quick action without showing menu
            // This could directly call the action provider
        }

        private void OnTouchBegan(Touch touch)
        {
            _isTouchPressed = true;
            _touchPressTime = 0f;
            _touchPressPosition = touch.position;
            
            LogDebug($"Touch began at {touch.position}");
        }

        private void OnTouchMoved(Touch touch, float deltaTime)
        {
            if (_isTouchPressed)
            {
                _touchPressTime += deltaTime;
                
                // Check for long press
                if (_touchPressTime >= _longPressThreshold)
                {
                    Vector3 worldPosition = new Vector3(touch.position.x, touch.position.y, 0);
                    OnLongPress(worldPosition);
                    _isTouchPressed = false; // Prevent multiple triggers
                }
            }
        }

        private void OnTouchEnded(Touch touch)
        {
            if (_isTouchPressed)
            {
                // Check for tap
                float touchDistance = Vector2.Distance(touch.position, _touchPressPosition);
                if (touchDistance < MaxTapDistance && _touchPressTime < _longPressThreshold)
                {
                    OnTouchTap(touch.position);
                }
            }
            
            _isTouchPressed = false;
            LogDebug($"Touch ended at {touch.position}");
        }

        private void OnTouchCanceled()
        {
            _isTouchPressed = false;
            LogDebug("Touch canceled");
        }

        private void OnTouchTap(Vector2 touchPosition)
        {
            // Check for double tap
            float timeSinceLastTap = Time.time - _lastTapTime;
            float distanceFromLastTap = Vector2.Distance(touchPosition, _lastTapPosition);
            
            if (timeSinceLastTap < DoubleTapTimeWindow && distanceFromLastTap < MaxTapDistance)
            {
                OnDoubleTap(touchPosition);
            }
            else
            {
                OnSingleTap(touchPosition);
            }
            
            _lastTapTime = Time.time;
            _lastTapPosition = touchPosition;
        }

        private void OnSingleTap(Vector2 position)
        {
            LogDebug($"Single tap at {position}");
            // Handle single tap behavior
        }

        private void OnDoubleTap(Vector2 position)
        {
            LogDebug($"Double tap at {position}");
            
            // Show context menu on double tap
            Vector3 worldPosition = new Vector3(position.x, position.y, 0);
            GameObject targetObject = GetObjectUnderCursor(worldPosition);
            _menuCore.ShowContextMenu(worldPosition, targetObject);
        }

        #endregion

        #region Utility Methods

        private GameObject GetObjectUnderCursor(Vector3 screenPosition)
        {
            // Cast a ray from camera through screen position
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return null;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                LogDebug($"Object detected under cursor: {hit.collider.gameObject.name}");
                return hit.collider.gameObject;
            }

            return null;
        }

        private void CancelRightClickAction()
        {
            _isRightMousePressed = false;
            _isLongPressing = false;
            LogDebug("Right-click action canceled due to mouse movement");
        }

        #endregion

        #region Public Interface

        public void SetRightClickEnabled(bool enabled)
        {
            _enableRightClickMenu = enabled;
            LogDebug($"Right-click menu {(enabled ? "enabled" : "disabled")}");
        }

        public void SetKeyboardShortcutsEnabled(bool enabled)
        {
            _enableKeyboardShortcuts = enabled;
            LogDebug($"Keyboard shortcuts {(enabled ? "enabled" : "disabled")}");
        }

        public void SetTouchGesturesEnabled(bool enabled)
        {
            _enableTouchGestures = enabled;
            LogDebug($"Touch gestures {(enabled ? "enabled" : "disabled")}");
        }

        public void SetRightClickHoldTime(float holdTime)
        {
            _rightClickHoldTime = Mathf.Max(0.1f, holdTime);
            LogDebug($"Right-click hold time set to {_rightClickHoldTime}s");
        }

        public void SetLongPressThreshold(float threshold)
        {
            _longPressThreshold = Mathf.Max(0.1f, threshold);
            LogDebug($"Long press threshold set to {_longPressThreshold}s");
        }

        #endregion

        private void LogDebug(string message)
        {
            if (_menuCore.DebugMode)
            {
                ChimeraLogger.Log($"[MenuInputHandler] {message}");
            }
        }
    }
}