using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Comprehensive interaction system for World Space UI elements in Unity 6.2.
    /// Handles mouse/touch input, hover states, click detection, and gesture recognition for 3D cannabis facility management.
    /// </summary>
    public class WorldSpaceInteractionSystem : MonoBehaviour
    {
        [Header("Interaction Configuration")]
        [SerializeField] private WorldSpaceInteractionConfig _config = new WorldSpaceInteractionConfig();
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private LayerMask _interactableLayer = 1 << 5; // UI Layer
        
        [Header("Input System")]
        [SerializeField] private bool _enableTouchSupport = true;
        [SerializeField] private bool _enableGestureRecognition = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private Material _hoverMaterial;
        [SerializeField] private Material _selectedMaterial;
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private AudioClip _hoverSound;
        
        // Input tracking (fallback without Input System)
        private bool _useInputSystem = false;
        
        // Interaction tracking
        private readonly Dictionary<UIDocument, InteractionData> _interactableElements = new Dictionary<UIDocument, InteractionData>();
        private readonly List<RaycastHit> _raycastResults = new List<RaycastHit>();
        
        // Current interaction state
        private UIDocument _currentHoverTarget;
        private UIDocument _currentSelectedTarget;
        private Vector2 _lastPointerPosition;
        private bool _isPointerDown;
        
        // Gesture recognition
        private Vector2 _gestureStartPosition;
        private float _gestureStartTime;
        private readonly List<Vector2> _gesturePoints = new List<Vector2>();
        
        public WorldSpaceInteractionConfig Config => _config;
        public UIDocument CurrentHoverTarget => _currentHoverTarget;
        public UIDocument CurrentSelectedTarget => _currentSelectedTarget;
        public bool IsInteracting => _isPointerDown;
        
        // Events
        public event Action<UIDocument, Vector3> OnElementHoverEnter;
        public event Action<UIDocument> OnElementHoverExit;
        public event Action<UIDocument, Vector3> OnElementClicked;
        public event Action<UIDocument, Vector3> OnElementPressed;
        public event Action<UIDocument, Vector3> OnElementReleased;
        public event Action<UIDocument, GestureData> OnGestureDetected;
        
        private void Awake()
        {
            if (_targetCamera == null)
                _targetCamera = Camera.main;
            
            InitializeInputSystem();
        }
        
        private void OnEnable()
        {
            EnableInputActions();
        }
        
        private void OnDisable()
        {
            DisableInputActions();
        }
        
        private void Update()
        {
            ProcessInput();
            UpdateInteractionStates();
        }
        
        /// <summary>
        /// Initializes the input system and action bindings
        /// </summary>
        private void InitializeInputSystem()
        {
            // Use legacy input system for compatibility
            _useInputSystem = false;
            Debug.Log("[WorldSpaceInteractionSystem] Using legacy input system for compatibility");
        }
        
        /// <summary>
        /// Enables input action callbacks
        /// </summary>
        private void EnableInputActions()
        {
            // Input actions handled in Update() with legacy input
        }
        
        /// <summary>
        /// Disables input action callbacks
        /// </summary>
        private void DisableInputActions()
        {
            // Input actions handled in Update() with legacy input
        }
        
        /// <summary>
        /// Processes input and updates pointer position
        /// </summary>
        private void ProcessInput()
        {
            Vector2 pointerPosition = Input.mousePosition;
            
            // Handle mouse click events
            if (Input.GetMouseButtonDown(0))
            {
                OnPointerPressLegacy();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                OnPointerReleaseLegacy();
            }
            else if (Input.GetMouseButton(0))
            {
                OnPointerClickLegacy();
            }
            
            // Track gesture if enabled
            if (_enableGestureRecognition && _isPointerDown)
            {
                TrackGestureMovement(pointerPosition);
            }
            
            _lastPointerPosition = pointerPosition;
        }
        
        /// <summary>
        /// Updates interaction states for all registered elements
        /// </summary>
        private void UpdateInteractionStates()
        {
            if (_targetCamera == null) return;
            
            // Raycast from pointer position
            var ray = _targetCamera.ScreenPointToRay(_lastPointerPosition);
            _raycastResults.Clear();
            
            if (Physics.RaycastNonAlloc(ray, _raycastResults.ToArray(), _config.maxInteractionDistance, _interactableLayer) > 0)
            {
                UIDocument hitElement = null;
                float closestDistance = float.MaxValue;
                Vector3 hitPoint = Vector3.zero;
                
                // Find closest interactable element
                foreach (var hit in _raycastResults)
                {
                    var uiDocument = hit.collider.GetComponent<UIDocument>();
                    if (uiDocument != null && _interactableElements.ContainsKey(uiDocument) && hit.distance < closestDistance)
                    {
                        hitElement = uiDocument;
                        closestDistance = hit.distance;
                        hitPoint = hit.point;
                    }
                }
                
                // Update hover state
                UpdateHoverState(hitElement, hitPoint);
            }
            else
            {
                // No hit, clear hover
                UpdateHoverState(null, Vector3.zero);
            }
        }
        
        /// <summary>
        /// Updates hover state for UI elements
        /// </summary>
        private void UpdateHoverState(UIDocument newHoverTarget, Vector3 hitPoint)
        {
            // Exit previous hover
            if (_currentHoverTarget != null && _currentHoverTarget != newHoverTarget)
            {
                var exitData = _interactableElements[_currentHoverTarget];
                exitData.IsHovered = false;
                exitData.LastHoverTime = Time.time;
                
                ApplyHoverEffect(_currentHoverTarget, false);
                OnElementHoverExit?.Invoke(_currentHoverTarget);
                
                if (_config.enableHoverAudio && _hoverSound != null)
                {
                    AudioSource.PlayClipAtPoint(_hoverSound, _currentHoverTarget.transform.position, _config.audioVolume * 0.5f);
                }
            }
            
            // Enter new hover
            if (newHoverTarget != null && newHoverTarget != _currentHoverTarget)
            {
                var enterData = _interactableElements[newHoverTarget];
                enterData.IsHovered = true;
                enterData.LastHoverTime = Time.time;
                enterData.LastHoverPosition = hitPoint;
                
                ApplyHoverEffect(newHoverTarget, true);
                OnElementHoverEnter?.Invoke(newHoverTarget, hitPoint);
                
                if (_config.enableHoverAudio && _hoverSound != null)
                {
                    AudioSource.PlayClipAtPoint(_hoverSound, newHoverTarget.transform.position, _config.audioVolume);
                }
            }
            
            _currentHoverTarget = newHoverTarget;
        }
        
        /// <summary>
        /// Applies visual hover effects to UI elements
        /// </summary>
        private void ApplyHoverEffect(UIDocument element, bool isHovered)
        {
            if (!_config.enableHoverEffects) return;
            
            var rootElement = element.rootVisualElement;
            if (rootElement == null) return;
            
            if (isHovered)
            {
                // Add hover styling
                rootElement.AddToClassList("world-ui-hover");
                
                // Scale effect
                if (_config.hoverScaleMultiplier != 1f)
                {
                    var currentScale = element.transform.localScale;
                    element.transform.localScale = currentScale * _config.hoverScaleMultiplier;
                }
                
                // Opacity effect
                var canvasGroup = element.GetComponent<CanvasGroup>();
                if (canvasGroup != null && _config.hoverAlphaMultiplier != 1f)
                {
                    canvasGroup.alpha *= _config.hoverAlphaMultiplier;
                }
            }
            else
            {
                // Remove hover styling
                rootElement.RemoveFromClassList("world-ui-hover");
                
                // Reset scale
                if (_config.hoverScaleMultiplier != 1f)
                {
                    var interactionData = _interactableElements[element];
                    element.transform.localScale = interactionData.OriginalScale;
                }
                
                // Reset opacity
                var canvasGroup = element.GetComponent<CanvasGroup>();
                if (canvasGroup != null && _config.hoverAlphaMultiplier != 1f)
                {
                    var interactionData = _interactableElements[element];
                    canvasGroup.alpha = interactionData.OriginalAlpha;
                }
            }
        }
        
        /// <summary>
        /// Legacy input handlers for compatibility
        /// </summary>
        private void OnPointerClickLegacy()
        {
            // Click handling moved to OnPointerPressLegacy for immediate response
        }
        
        private void OnPointerPressLegacy()
        {
            _isPointerDown = true;
            
            if (_currentHoverTarget != null)
            {
                _currentSelectedTarget = _currentHoverTarget;
                var interactionData = _interactableElements[_currentHoverTarget];
                var hitPoint = interactionData.LastHoverPosition;
                
                // Update interaction data
                interactionData.LastClickTime = Time.time;
                interactionData.ClickCount++;
                
                // Start gesture tracking
                if (_enableGestureRecognition)
                {
                    StartGestureTracking(_lastPointerPosition);
                }
                
                // Fire events
                OnElementPressed?.Invoke(_currentHoverTarget, hitPoint);
                OnElementClicked?.Invoke(_currentHoverTarget, hitPoint);
                
                // Audio feedback
                if (_config.enableClickAudio && _clickSound != null)
                {
                    AudioSource.PlayClipAtPoint(_clickSound, _currentHoverTarget.transform.position, _config.audioVolume);
                }
            }
        }
        
        private void OnPointerReleaseLegacy()
        {
            _isPointerDown = false;
            
            if (_currentSelectedTarget != null)
            {
                var interactionData = _interactableElements[_currentSelectedTarget];
                var hitPoint = interactionData.LastHoverPosition;
                
                // End gesture tracking
                if (_enableGestureRecognition)
                {
                    EndGestureTracking();
                }
                
                OnElementReleased?.Invoke(_currentSelectedTarget, hitPoint);
                _currentSelectedTarget = null;
            }
        }
        
        // Gesture tracking methods (simplified)
        private void StartGestureTracking(Vector2 startPosition)
        {
            _gestureStartPosition = startPosition;
            _gestureStartTime = Time.time;
            _gesturePoints.Clear();
            _gesturePoints.Add(startPosition);
        }
        
        private void TrackGestureMovement(Vector2 currentPosition)
        {
            if (_gesturePoints.Count == 0) return;
            
            var lastPoint = _gesturePoints[_gesturePoints.Count - 1];
            var distance = Vector2.Distance(currentPosition, lastPoint);
            
            if (distance > _config.gestureMinMovement)
            {
                _gesturePoints.Add(currentPosition);
            }
        }
        
        private void EndGestureTracking()
        {
            if (_gesturePoints.Count < 2) return;
            
            var gestureData = new GestureData
            {
                StartPosition = _gestureStartPosition,
                EndPosition = _gesturePoints[_gesturePoints.Count - 1],
                Duration = Time.time - _gestureStartTime,
                Points = new List<Vector2>(_gesturePoints),
                IsValid = true,
                Type = GestureType.Drag
            };
            
            if (_currentSelectedTarget != null)
            {
                OnGestureDetected?.Invoke(_currentSelectedTarget, gestureData);
            }
        }
        
        /// <summary>
        /// Registers a UI element for interaction
        /// </summary>
        public bool RegisterInteractableElement(UIDocument element)
        {
            if (element == null) return false;
            
            var interactionData = new InteractionData
            {
                Element = element,
                Settings = new InteractionSettings(),
                OriginalScale = element.transform.localScale,
                OriginalAlpha = element.GetComponent<CanvasGroup>()?.alpha ?? 1f,
                RegistrationTime = Time.time
            };
            
            _interactableElements[element] = interactionData;
            return true;
        }
        
        /// <summary>
        /// Unregisters a UI element from interaction
        /// </summary>
        public bool UnregisterInteractableElement(UIDocument element)
        {
            if (element == null) return false;
            
            if (_currentHoverTarget == element)
            {
                UpdateHoverState(null, Vector3.zero);
            }
            
            if (_currentSelectedTarget == element)
            {
                _currentSelectedTarget = null;
            }
            
            return _interactableElements.Remove(element);
        }
    }
}