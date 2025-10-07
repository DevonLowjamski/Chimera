using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Mouse Input Processor - Handles mouse input and gesture recognition
    /// Processes mouse movement, clicks, and drag gestures for camera control
    /// Supports the hierarchical viewpoint system as described in gameplay document
    /// </summary>
    public class MouseInputProcessor : MonoBehaviour, ITickable
    {
        [Header("Mouse Settings")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private bool _invertMouseY = false;
        [SerializeField] private float _scrollSensitivity = 1f;

        [Header("Gesture Recognition")]
        [SerializeField] private bool _enableGestureRecognition = true;
        [SerializeField] private float _gestureMinimumDistance = 50f;
        [SerializeField] private float _gestureMaximumTime = 1f;
        [SerializeField] private int _gestureMinimumPoints = 3;

        [Header("Click Settings")]
        [SerializeField] private float _doubleClickTime = 0.3f;
        [SerializeField] private LayerMask _clickFocusLayers = -1;

        // Mouse state
        private Vector2 _mousePosition;
        private Vector2 _lastMousePosition;
        private Vector2 _mouseDelta;
        private float _mouseWheelDelta;

        // Click tracking
        private float _lastClickTime;
        private int _clickCount;
        private Vector2 _lastClickPosition;

        // Gesture tracking
        private List<Vector2> _gesturePoints = new List<Vector2>();
        private float _gestureStartTime;
        private bool _isTrackingGesture;

        // Input state
        private bool _isMouseButtonDown0;
        private bool _isMouseButtonDown1;
        private bool _isMouseButtonDown2;

        // Events
        public event System.Action<Vector2> OnMouseInput;
        public event System.Action<float> OnScrollInput;
        public event System.Action<Transform> OnClickToFocus;

    public int TickPriority => 100;
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
            UpdateMouseState();
            ProcessClicks();
            ProcessGestures();
    }

    private void Awake()
    {
        UpdateOrchestrator.Instance.RegisterTickable(this);
    }

    private void OnDestroy()
    {
        UpdateOrchestrator.Instance.UnregisterTickable(this);
    }

        /// <summary>
        /// Updates the current mouse state
        /// </summary>
        private void UpdateMouseState()
        {
            _lastMousePosition = _mousePosition;
            _mousePosition = Input.mousePosition;
            _mouseDelta = _mousePosition - _lastMousePosition;
            _mouseWheelDelta = Input.mouseScrollDelta.y;

            _isMouseButtonDown0 = Input.GetMouseButton(0);
            _isMouseButtonDown1 = Input.GetMouseButton(1);
            _isMouseButtonDown2 = Input.GetMouseButton(2);

            // Trigger events
            if (_mouseDelta.sqrMagnitude > 0.01f)
            {
                OnMouseInput?.Invoke(_mouseDelta);
            }

            if (Mathf.Abs(_mouseWheelDelta) > 0.01f)
            {
                OnScrollInput?.Invoke(_mouseWheelDelta);
            }
        }

        /// <summary>
        /// Processes mouse clicks and double-clicks
        /// </summary>
        private void ProcessClicks()
        {
            if (Input.GetMouseButtonDown(0))
            {
                float currentTime = Time.time;

                if (currentTime - _lastClickTime <= _doubleClickTime &&
                    Vector2.Distance(_mousePosition, _lastClickPosition) <= 10f)
                {
                    _clickCount++;
                    if (_clickCount >= 2)
                    {
                        OnDoubleClick(_mousePosition);
                        _clickCount = 0;
                    }
                }
                else
                {
                    _clickCount = 1;
                }

                _lastClickTime = currentTime;
                _lastClickPosition = _mousePosition;
            }

            // Handle single click (with delay to allow for double-click)
            if (_clickCount == 1 && Time.time - _lastClickTime > _doubleClickTime)
            {
                OnSingleClick(_lastClickPosition);
                _clickCount = 0;
            }
        }

        /// <summary>
        /// Processes gesture recognition
        /// </summary>
        private void ProcessGestures()
        {
            if (!_enableGestureRecognition) return;

            if (Input.GetMouseButtonDown(0))
            {
                // Start tracking gesture
                _gesturePoints.Clear();
                _gesturePoints.Add(_mousePosition);
                _gestureStartTime = Time.time;
                _isTrackingGesture = true;
            }
            else if (Input.GetMouseButton(0) && _isTrackingGesture)
            {
                // Continue tracking gesture
                if (Vector2.Distance(_mousePosition, _gesturePoints[_gesturePoints.Count - 1]) > 5f)
                {
                    _gesturePoints.Add(_mousePosition);
                }
            }
            else if (Input.GetMouseButtonUp(0) && _isTrackingGesture)
            {
                // End tracking gesture
                _isTrackingGesture = false;

                if (_gesturePoints.Count >= _gestureMinimumPoints &&
                    Time.time - _gestureStartTime <= _gestureMaximumTime)
                {
                    RecognizeGesture(_gesturePoints);
                }
            }
        }

        /// <summary>
        /// Recognizes a gesture from the collected points
        /// </summary>
        private void RecognizeGesture(List<Vector2> points)
        {
            if (points.Count < 2) return;

            // Simple gesture recognition - could be expanded
            Vector2 startPoint = points[0];
            Vector2 endPoint = points[points.Count - 1];
            Vector2 direction = (endPoint - startPoint).normalized;

            float distance = Vector2.Distance(startPoint, endPoint);

            if (distance >= _gestureMinimumDistance)
            {
                // Determine gesture type based on direction
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    // Horizontal gesture
                    if (direction.x > 0)
                    {
                        OnGestureRecognized(GestureType.SwipeRight, distance);
                    }
                    else
                    {
                        OnGestureRecognized(GestureType.SwipeLeft, distance);
                    }
                }
                else
                {
                    // Vertical gesture
                    if (direction.y > 0)
                    {
                        OnGestureRecognized(GestureType.SwipeUp, distance);
                    }
                    else
                    {
                        OnGestureRecognized(GestureType.SwipeDown, distance);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current mouse delta (movement)
        /// </summary>
        public Vector2 GetMouseDelta()
        {
            return _mouseDelta * _mouseSensitivity;
        }

        /// <summary>
        /// Gets the mouse wheel delta
        /// </summary>
        public float GetMouseWheelDelta()
        {
            return _mouseWheelDelta * _scrollSensitivity;
        }

        /// <summary>
        /// Gets the current mouse position
        /// </summary>
        public Vector2 GetMousePosition()
        {
            return _mousePosition;
        }

        /// <summary>
        /// Checks if a mouse button is currently down
        /// </summary>
        public bool IsMouseButtonDown(int button)
        {
            switch (button)
            {
                case 0: return _isMouseButtonDown0;
                case 1: return _isMouseButtonDown1;
                case 2: return _isMouseButtonDown2;
                default: return false;
            }
        }

        /// <summary>
        /// Checks if a mouse button was pressed this frame
        /// </summary>
        public bool IsMouseButtonPressed(int button)
        {
            return Input.GetMouseButtonDown(button);
        }

        /// <summary>
        /// Checks if a mouse button was released this frame
        /// </summary>
        public bool IsMouseButtonReleased(int button)
        {
            return Input.GetMouseButtonUp(button);
        }

        /// <summary>
        /// Gets a ray from the camera through the mouse position
        /// </summary>
        public Ray GetMouseRay()
        {
            return UnityEngine.Camera.main.ScreenPointToRay(_mousePosition);
        }

        /// <summary>
        /// Performs a raycast from the mouse position
        /// </summary>
        public bool RaycastFromMouse(out RaycastHit hit, float maxDistance = Mathf.Infinity, LayerMask layerMask = default)
        {
            Ray ray = GetMouseRay();
            return Physics.Raycast(ray, out hit, maxDistance, layerMask == 0 ? -1 : layerMask);
        }

        /// <summary>
        /// Sets the mouse sensitivity
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            _mouseSensitivity = sensitivity;
        }

        /// <summary>
        /// Sets whether to invert the mouse Y axis
        /// </summary>
        public void SetInvertMouseY(bool invert)
        {
            _invertMouseY = invert;
        }

        /// <summary>
        /// Applies Y inversion to mouse delta if enabled
        /// </summary>
        public Vector2 ApplyMouseYInversion(Vector2 delta)
        {
            if (_invertMouseY)
            {
                delta.y = -delta.y;
            }
            return delta;
        }

        // Event handlers - would be implemented to integrate with camera system

        private void OnSingleClick(Vector2 position)
        {
            ChimeraLogger.LogInfo("MouseInputProcessor", "$1");

            // Handle click-to-focus for hierarchical viewpoint system
            if (RaycastFromMouse(out RaycastHit hit, Mathf.Infinity, _clickFocusLayers))
            {
                RaiseClickToFocus(hit.transform, hit.point);
            }
        }

        private void OnDoubleClick(Vector2 position)
        {
            ChimeraLogger.LogInfo("MouseInputProcessor", "$1");

            // Handle double-click to focus (as mentioned in gameplay document)
            if (RaycastFromMouse(out RaycastHit hit, Mathf.Infinity, _clickFocusLayers))
            {
                OnDoubleClickToFocus(hit.transform, hit.point);
            }
        }

        private void OnGestureRecognized(GestureType gesture, float distance)
        {
            ChimeraLogger.LogInfo("MouseInputProcessor", "$1");

            // Handle gestures for camera control
            switch (gesture)
            {
                case GestureType.SwipeUp:
                    OnZoomIn();
                    break;
                case GestureType.SwipeDown:
                    OnZoomOut();
                    break;
                case GestureType.SwipeLeft:
                    OnRotateLeft();
                    break;
                case GestureType.SwipeRight:
                    OnRotateRight();
                    break;
            }
        }

        /// <summary>
        /// Try to focus on target at screen position
        /// </summary>
        public bool TryFocusOnTarget(Vector2 screenPosition)
        {
            var ray = UnityEngine.Camera.main.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _clickFocusLayers))
            {
                RaiseClickToFocus(hit.transform, hit.point);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get target at screen position
        /// </summary>
        public Transform GetTargetAtPosition(Vector2 screenPosition)
        {
            var ray = UnityEngine.Camera.main.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _clickFocusLayers))
            {
                return hit.transform;
            }
            return null;
        }

        /// <summary>
        /// Set click to focus enabled
        /// </summary>
        public void SetClickToFocusEnabled(bool enabled)
        {
            // This method enables/disables click to focus functionality
            // For now, just track the state - can be expanded later
        }

        // Virtual methods for integration with camera system
        protected virtual void RaiseClickToFocus(Transform target, Vector3 point)
        {
            OnClickToFocus?.Invoke(target);
        }
        protected virtual void OnDoubleClickToFocus(Transform target, Vector3 point) { }
        protected virtual void OnZoomIn() { }
        protected virtual void OnZoomOut() { }
        protected virtual void OnRotateLeft() { }
        protected virtual void OnRotateRight() { }

        /// <summary>
        /// Checks if currently tracking a gesture
        /// </summary>
        public bool IsTrackingGesture()
        {
            return _isTrackingGesture;
        }

        /// <summary>
        /// Gets the current gesture points
        /// </summary>
        public List<Vector2> GetCurrentGesturePoints()
        {
            return new List<Vector2>(_gesturePoints);
        }
    }

    /// <summary>
    /// Gesture types for recognition
    /// </summary>
    public enum GestureType
    {
        SwipeUp,
        SwipeDown,
        SwipeLeft,
        SwipeRight,
        PinchIn,
        PinchOut
    }
}

