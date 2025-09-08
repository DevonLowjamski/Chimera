using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Camera;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Handles all input processing for the camera system including mouse, keyboard,
    /// gesture recognition, click-to-focus, and input validation. Extracted from
    /// AdvancedCameraController to improve maintainability and separation of concerns.
    /// </summary>
    public class CameraInputHandler : MonoBehaviour, ITickable
    {
        [Header("Input Settings")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _panSpeed = 3f;
        [SerializeField] private bool _invertMouseY = false;

        [Header("Click-to-Focus Settings")]
        [SerializeField] private bool _enableClickToFocus = true;
        [SerializeField] private bool _enableDoubleClickToFocus = true;
        [SerializeField] private float _doubleClickTime = 0.3f;
        [SerializeField] private bool _rightClickToClearFocus = true;
        [SerializeField] private bool _enableFocusHoverPreview = true;
        [SerializeField] private float _maxFocusDistance = 100f;
        [SerializeField] private LayerMask _clickFocusLayers = -1;

        [Header("Input Validation")]
        [SerializeField] private bool _enableInputFiltering = true;
        [SerializeField] private float _minimumMouseMovement = 0.1f;
        [SerializeField] private float _maximumMouseSpeed = 1000f;
        [SerializeField] private bool _validateInputBounds = true;

        [Header("Gesture Recognition")]
        [SerializeField] private bool _enableGestureRecognition = true;
        [SerializeField] private float _gestureMinimumDistance = 50f;
        [SerializeField] private float _gestureMaximumTime = 1f;
        [SerializeField] private int _gestureMinimumPoints = 3;

        // Core references
        private UnityEngine.Camera _mainCamera;
        private AdvancedCameraController _cameraController;

        // Input state
        private Vector2 _mouseInput;
        private float _scrollInput;
        private bool _isDragging = false;
        private Vector3 _lastMousePosition;
        private Vector2 _mouseStartPosition;

        // Click-to-focus state
        private float _lastClickTime = 0f;
        private Transform _lastClickTarget;
        private bool _awaitingDoubleClick = false;
        private Transform _hoverTarget;
        private Coroutine _hoverPreviewCoroutine;
        private Coroutine _doubleClickClearCoroutine;

        // Gesture recognition state
        private bool _isRecordingGesture = false;
        private Vector3[] _gesturePoints = new Vector3[32];
        private int _gesturePointCount = 0;
        private float _gestureStartTime = 0f;

        // Input validation state
        private Vector2 _previousMouseInput;
        private float _inputSpeedThreshold = 100f;

        // Events
        public System.Action<Vector2> OnMouseInput;
        public System.Action<float> OnScrollInput;
        public System.Action<Vector3> OnKeyboardMovement;
        public System.Action<Transform> OnFocusTargetClicked;
        public System.Action<Transform> OnFocusTargetDoubleClicked;
        public System.Action OnFocusClearRequested;
        public System.Action<Transform> OnTargetHover;
        public System.Action OnTargetHoverEnd;
        public System.Action<GestureType> OnGestureRecognized;

        // Input validation events
        public System.Action<string> OnInputValidationFailed;
        public System.Action<InputFilterResult> OnInputFiltered;

        public enum GestureType
        {
            None,
            SwipeLeft,
            SwipeRight,
            SwipeUp,
            SwipeDown,
            Circle,
            Zoom,
            Pan
        }

        public struct InputFilterResult
        {
            public bool IsValid;
            public Vector2 FilteredMouseInput;
            public string FilterReason;
        }

        // Properties
        public Vector2 MouseInput => _mouseInput;
        public float ScrollInput => _scrollInput;
        public bool IsDragging => _isDragging;
        public bool IsAwaitingDoubleClick => _awaitingDoubleClick;
        public Transform HoverTarget => _hoverTarget;
        public bool EnableClickToFocus { get => _enableClickToFocus; set => _enableClickToFocus = value; }
        public bool EnableGestureRecognition { get => _enableGestureRecognition; set => _enableGestureRecognition = value; }

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            ValidateConfiguration();
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        #region ITickable Implementation

        public int Priority => TickPriority.InputSystem;
        public bool Enabled => enabled && _mainCamera != null && _cameraController != null;

        public void Tick(float deltaTime)
        {
            ProcessInput();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            _mainCamera = ServiceContainerFactory.Instance?.TryResolve<UnityEngine.Camera>() ?? UnityEngine.Camera.main ?? ServiceContainerFactory.Instance?.TryResolve<UnityEngine.Camera>();
            _cameraController = GetComponent<AdvancedCameraController>();

            if (_mainCamera == null)
            {
                ChimeraLogger.LogError("[CameraInputHandler] No camera found in scene!");
                enabled = false;
            }

            if (_cameraController == null)
            {
                ChimeraLogger.LogError("[CameraInputHandler] AdvancedCameraController component required!");
                enabled = false;
            }
        }

        private void ValidateConfiguration()
        {
            // Validate input settings
            if (_mouseSensitivity <= 0f)
            {
                ChimeraLogger.LogWarning("[CameraInputHandler] Mouse sensitivity should be greater than 0");
                _mouseSensitivity = 1f;
            }

            if (_maxFocusDistance <= 0f)
            {
                ChimeraLogger.LogWarning("[CameraInputHandler] Max focus distance should be greater than 0");
                _maxFocusDistance = 100f;
            }

            if (_doubleClickTime <= 0f)
            {
                ChimeraLogger.LogWarning("[CameraInputHandler] Double click time should be greater than 0");
                _doubleClickTime = 0.3f;
            }
        }

        #endregion

        #region Input Processing

        /// <summary>
        /// Main input processing method called every frame
        /// </summary>
        private void ProcessInput()
        {
            // Capture raw input
            CaptureRawInput();

            // Apply input filtering and validation
            if (_enableInputFiltering)
            {
                var filterResult = FilterAndValidateInput();
                if (!filterResult.IsValid)
                {
                    OnInputValidationFailed?.Invoke(filterResult.FilterReason);
                    return;
                }
                OnInputFiltered?.Invoke(filterResult);
            }

            // Process different input types
            ProcessMouseInput();
            ProcessKeyboardInput();
            ProcessClickInput();

            // Handle hover preview
            if (_enableFocusHoverPreview && !_isDragging)
            {
                ProcessHoverPreview();
            }

            // Process gesture recognition
            if (_enableGestureRecognition)
            {
                ProcessGestureRecognition();
            }
        }

        private void CaptureRawInput()
        {
            _mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            if (_invertMouseY)
                _mouseInput.y = -_mouseInput.y;

            _scrollInput = Input.GetAxis("Mouse ScrollWheel");

            // Update dragging state
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
                _mouseStartPosition = Input.mousePosition;

                // Start gesture recording if enabled
                if (_enableGestureRecognition)
                {
                    StartGestureRecording();
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;

                // End gesture recording if enabled
                if (_enableGestureRecognition)
                {
                    EndGestureRecording();
                }
            }
        }

        private InputFilterResult FilterAndValidateInput()
        {
            var result = new InputFilterResult { IsValid = true, FilteredMouseInput = _mouseInput };

            // Check for excessive mouse speed (potential input spike)
            float inputSpeed = _mouseInput.magnitude;
            if (inputSpeed > _inputSpeedThreshold)
            {
                result.IsValid = false;
                result.FilterReason = $"Input speed too high: {inputSpeed:F2}";
                return result;
            }

            // Check minimum movement threshold
            if (_mouseInput.magnitude < _minimumMouseMovement && _mouseInput.magnitude > 0f)
            {
                result.FilteredMouseInput = Vector2.zero;
                result.FilterReason = "Input below minimum threshold";
            }

            // Validate against maximum speed
            if (_mouseInput.magnitude > _maximumMouseSpeed)
            {
                result.FilteredMouseInput = _mouseInput.normalized * _maximumMouseSpeed;
                result.FilterReason = "Input clamped to maximum speed";
            }

            // Store for next frame comparison
            _previousMouseInput = _mouseInput;

            return result;
        }

        #endregion

        #region Mouse Input Processing

        private void ProcessMouseInput()
        {
            // Emit raw mouse input for camera movement
            if (_mouseInput.magnitude > 0.01f)
            {
                OnMouseInput?.Invoke(_mouseInput);
            }

            // Emit scroll input for zoom
            if (Mathf.Abs(_scrollInput) > 0.01f)
            {
                OnScrollInput?.Invoke(_scrollInput);
            }

            // Update last mouse position for next frame
            if (_isDragging)
            {
                _lastMousePosition = Input.mousePosition;
            }
        }

        #endregion

        #region Keyboard Input Processing

        private void ProcessKeyboardInput()
        {
            Vector3 keyboardMovement = Vector3.zero;

            // WASD movement
            if (Input.GetKey(KeyCode.W)) keyboardMovement += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) keyboardMovement += Vector3.back;
            if (Input.GetKey(KeyCode.A)) keyboardMovement += Vector3.left;
            if (Input.GetKey(KeyCode.D)) keyboardMovement += Vector3.right;

            // Vertical movement
            if (Input.GetKey(KeyCode.Q)) keyboardMovement += Vector3.down;
            if (Input.GetKey(KeyCode.E)) keyboardMovement += Vector3.up;

            // Speed modifiers
            float speedMultiplier = 1f;
            if (Input.GetKey(KeyCode.LeftShift)) speedMultiplier = 2f;
            if (Input.GetKey(KeyCode.LeftControl)) speedMultiplier = 0.5f;

            keyboardMovement = keyboardMovement.normalized * _panSpeed * speedMultiplier * Time.deltaTime;

            if (keyboardMovement.magnitude > 0.01f)
            {
                OnKeyboardMovement?.Invoke(keyboardMovement);
            }

            // Handle keyboard shortcuts
            ProcessKeyboardShortcuts();
        }

        private void ProcessKeyboardShortcuts()
        {
            // Focus shortcuts
            if (Input.GetKeyDown(KeyCode.F))
            {
                // F key - focus on selected object or clear focus
                if (_cameraController?.FocusTarget != null)
                    OnFocusClearRequested?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Escape - clear focus and cancel operations
                OnFocusClearRequested?.Invoke();
                CancelDoubleClickState();
            }

            // Camera level shortcuts (if supported by controller)
            if (Input.GetKeyDown(KeyCode.Alpha1)) EmitCameraLevelShortcut(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) EmitCameraLevelShortcut(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) EmitCameraLevelShortcut(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) EmitCameraLevelShortcut(4);
        }

        private void EmitCameraLevelShortcut(int levelIndex)
        {
            // This would be handled by the camera controller
            ChimeraLogger.Log($"[CameraInputHandler] Camera level shortcut: {levelIndex}");
        }

        #endregion

        #region Click Input Processing

        private void ProcessClickInput()
        {
            bool leftMouseDown = Input.GetMouseButtonDown(0);
            bool rightMouseDown = Input.GetMouseButtonDown(1);

            // Handle click-to-focus interactions
            if (_enableClickToFocus && leftMouseDown)
            {
                ProcessClickToFocus();
            }

            // Handle right-click to clear focus
            if (rightMouseDown && _rightClickToClearFocus)
            {
                OnFocusClearRequested?.Invoke();
            }
        }

        private void ProcessClickToFocus()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxFocusDistance, _clickFocusLayers))
            {
                Transform hitTarget = hit.transform;

                // Validate that target can be focused
                if (!CanFocusOnTarget(hitTarget)) return;

                float currentTime = Time.time;
                bool isDoubleClick = _enableDoubleClickToFocus &&
                                   _awaitingDoubleClick &&
                                   hitTarget == _lastClickTarget &&
                                   (currentTime - _lastClickTime) <= _doubleClickTime;

                if (isDoubleClick)
                {
                    // Double-click detected
                    OnFocusTargetDoubleClicked?.Invoke(hitTarget);
                    CancelDoubleClickState();
                }
                else
                {
                    // Single click
                    OnFocusTargetClicked?.Invoke(hitTarget);

                    // Setup double-click detection
                    if (_enableDoubleClickToFocus)
                    {
                        SetupDoubleClickDetection(hitTarget, currentTime);
                    }
                }
            }
            else
            {
                // Clicked on empty space - clear focus
                OnFocusClearRequested?.Invoke();
            }
        }

        private bool CanFocusOnTarget(Transform target)
        {
            if (target == null) return false;

            // Check if target has a focusable component or is on focusable layer
            var focusable = target.GetComponent<ICameraFocusable>();
            if (focusable != null)
                return focusable.CanBeFocused();

            // Check layer mask
            return ((_clickFocusLayers.value & (1 << target.gameObject.layer)) != 0);
        }

        private void SetupDoubleClickDetection(Transform target, float clickTime)
        {
            _lastClickTime = clickTime;
            _lastClickTarget = target;
            _awaitingDoubleClick = true;

            // Clear double-click state after timeout
            if (_doubleClickClearCoroutine != null)
                StopCoroutine(_doubleClickClearCoroutine);
            _doubleClickClearCoroutine = StartCoroutine(ClearDoubleClickStateCoroutine(_doubleClickTime));
        }

        private void CancelDoubleClickState()
        {
            _awaitingDoubleClick = false;
            _lastClickTarget = null;
            if (_doubleClickClearCoroutine != null)
            {
                StopCoroutine(_doubleClickClearCoroutine);
                _doubleClickClearCoroutine = null;
            }
        }

        private IEnumerator ClearDoubleClickStateCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _awaitingDoubleClick = false;
            _doubleClickClearCoroutine = null;
        }

        #endregion

        #region Hover Preview Processing

        private void ProcessHoverPreview()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxFocusDistance, _clickFocusLayers))
            {
                Transform hitTarget = hit.transform;

                if (hitTarget != _hoverTarget && CanFocusOnTarget(hitTarget))
                {
                    // New hover target
                    EndHoverPreview();
                    StartHoverPreview(hitTarget);
                }
            }
            else
            {
                // No hover target
                EndHoverPreview();
            }
        }

        private void StartHoverPreview(Transform target)
        {
            _hoverTarget = target;
            OnTargetHover?.Invoke(target);

            if (_hoverPreviewCoroutine != null)
                StopCoroutine(_hoverPreviewCoroutine);
            _hoverPreviewCoroutine = StartCoroutine(HoverPreviewCoroutine());
        }

        private void EndHoverPreview()
        {
            if (_hoverTarget != null)
            {
                OnTargetHoverEnd?.Invoke();
                _hoverTarget = null;

                if (_hoverPreviewCoroutine != null)
                {
                    StopCoroutine(_hoverPreviewCoroutine);
                    _hoverPreviewCoroutine = null;
                }
            }
        }

        private IEnumerator HoverPreviewCoroutine()
        {
            // Brief preview of focus target
            yield return new WaitForSeconds(0.5f);
            // Additional hover logic can be added here
            _hoverPreviewCoroutine = null;
        }

        #endregion

        #region Gesture Recognition

        private void ProcessGestureRecognition()
        {
            if (_isRecordingGesture && _isDragging)
            {
                // Record gesture point
                if (_gesturePointCount < _gesturePoints.Length)
                {
                    _gesturePoints[_gesturePointCount] = Input.mousePosition;
                    _gesturePointCount++;
                }
            }
        }

        private void StartGestureRecording()
        {
            _isRecordingGesture = true;
            _gesturePointCount = 0;
            _gestureStartTime = Time.time;
            _gesturePoints[0] = Input.mousePosition;
            _gesturePointCount = 1;
        }

        private void EndGestureRecording()
        {
            if (!_isRecordingGesture) return;

            _isRecordingGesture = false;
            float gestureTime = Time.time - _gestureStartTime;

            // Analyze gesture if it meets minimum requirements
            if (_gesturePointCount >= _gestureMinimumPoints && gestureTime <= _gestureMaximumTime)
            {
                var gestureType = AnalyzeGesture();
                if (gestureType != GestureType.None)
                {
                    OnGestureRecognized?.Invoke(gestureType);
                }
            }
        }

        private GestureType AnalyzeGesture()
        {
            if (_gesturePointCount < 2) return GestureType.None;

            Vector3 startPoint = _gesturePoints[0];
            Vector3 endPoint = _gesturePoints[_gesturePointCount - 1];
            Vector3 gestureVector = endPoint - startPoint;
            float gestureDistance = gestureVector.magnitude;

            // Check minimum distance
            if (gestureDistance < _gestureMinimumDistance)
                return GestureType.None;

            // Simple directional gesture recognition
            Vector3 normalizedGesture = gestureVector.normalized;
            float horizontalComponent = Mathf.Abs(normalizedGesture.x);
            float verticalComponent = Mathf.Abs(normalizedGesture.y);

            if (horizontalComponent > verticalComponent)
            {
                // Horizontal gesture
                return normalizedGesture.x > 0 ? GestureType.SwipeRight : GestureType.SwipeLeft;
            }
            else
            {
                // Vertical gesture
                return normalizedGesture.y > 0 ? GestureType.SwipeUp : GestureType.SwipeDown;
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Enable or disable input processing
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            this.enabled = enabled;
            
            if (!enabled)
            {
                // Clear current input state
                _mouseInput = Vector2.zero;
                _scrollInput = 0f;
                _isDragging = false;
                EndHoverPreview();
                CancelDoubleClickState();
            }
        }

        /// <summary>
        /// Force clear all input states
        /// </summary>
        public void ClearInputState()
        {
            _mouseInput = Vector2.zero;
            _scrollInput = 0f;
            _isDragging = false;
            _awaitingDoubleClick = false;
            _lastClickTarget = null;
            EndHoverPreview();
            CancelDoubleClickState();
        }

        /// <summary>
        /// Update input sensitivity settings
        /// </summary>
        public void UpdateSensitivity(float mouseSensitivity, float zoomSpeed, float panSpeed)
        {
            _mouseSensitivity = Mathf.Max(0.1f, mouseSensitivity);
            _zoomSpeed = Mathf.Max(0.1f, zoomSpeed);
            _panSpeed = Mathf.Max(0.1f, panSpeed);
        }

        #endregion
        
        #region Public API for AdvancedCameraController Orchestrator
        
        // Events for orchestrator
        public System.Action<Transform> OnFocusRequested;
        
        /// <summary>
        /// Get camera ray for screen position
        /// </summary>
        public Ray GetCameraRay(Vector2 screenPosition)
        {
            if (_mainCamera != null)
            {
                return _mainCamera.ScreenPointToRay(screenPosition);
            }
            return new Ray();
        }
        
        /// <summary>
        /// Focus on target at screen position
        /// </summary>
        public bool FocusOnTargetAtScreenPosition(Vector2 screenPosition)
        {
            var target = GetTargetAtScreenPosition(screenPosition);
            if (target != null)
            {
                OnFocusRequested?.Invoke(target);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get target at screen position
        /// </summary>
        public Transform GetTargetAtScreenPosition(Vector2 screenPosition)
        {
            Ray ray = GetCameraRay(screenPosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, _maxFocusDistance, _clickFocusLayers))
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
            _enableClickToFocus = enabled;
        }
        
        /// <summary>
        /// Get keyboard shortcuts
        /// </summary>
        public Dictionary<string, string> GetKeyboardShortcuts()
        {
            return new Dictionary<string, string>
            {
                {"Focus Clear", "Right Click"},
                {"Double Click Focus", "Double Click"},
                {"Pan", "Middle Mouse + Drag"},
                {"Rotate", "Alt + Left Mouse"},
                {"Zoom", "Mouse Wheel"}
            };
        }
        
        #endregion

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
        
        private void OnDisable()
        {
            // Cleanup when disabled
            EndHoverPreview();
            CancelDoubleClickState();
            
            if (_isRecordingGesture)
                EndGestureRecording();
        }
    }

    /// <summary>
    /// Interface for objects that can be focused by the camera
    /// </summary>
    public interface ICameraFocusable
    {
        bool CanBeFocused();
        Vector3 GetFocusPoint();
        float GetOptimalFocusDistance();
    }
}