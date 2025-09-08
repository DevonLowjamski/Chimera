using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Handles all input processing for the 3D grid-based construction system.
    /// Supports mouse/keyboard input, height level navigation, and 3D coordinate conversion.
    /// Extracted from GridPlacementController for modular architecture.
    /// </summary>
    public class GridInputHandler : MonoBehaviour, ITickable
    {
        [Header("Input Key Bindings")]
        [SerializeField] private KeyCode _placementKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode _cancelKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode _rotateKey = KeyCode.R;
        [SerializeField] private KeyCode _deleteKey = KeyCode.Delete;
        [SerializeField] private KeyCode _dragSelectKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode _addToSelectionKey = KeyCode.LeftShift;
        
        [Header("3D Height Navigation")]
        [SerializeField] private KeyCode _heightUpKey = KeyCode.PageUp;
        [SerializeField] private KeyCode _heightDownKey = KeyCode.PageDown;
        [SerializeField] private KeyCode _heightResetKey = KeyCode.Home;
        [SerializeField] private float _mouseWheelSensitivity = 1f;
        [SerializeField] private bool _enableMouseWheelHeight = true;
        [SerializeField] private bool _requireHeightModifier = true;
        [SerializeField] private KeyCode _heightModifierKey = KeyCode.LeftControl;
        
        [Header("Schematic Operations")]
        [SerializeField] private KeyCode _saveSchematicKey = KeyCode.S;
        [SerializeField] private KeyCode _saveSchematicModifier = KeyCode.LeftControl;
        [SerializeField] private KeyCode _cancelSchematicKey = KeyCode.Escape;
        
        [Header("Raycast Settings")]
        [SerializeField] private UnityEngine.Camera _inputCamera;
        [SerializeField] private LayerMask _groundLayer = 1;
        [SerializeField] private float _raycastDistance = 1000f;
        [SerializeField] private bool _usePhysicsRaycast = true;
        
        [Header("Drag Selection")]
        [SerializeField] private float _dragThreshold = 10f;
        [SerializeField] private bool _enableDragSelection = true;
        
        [Header("Input Validation")]
        [SerializeField] private bool _enableInputValidation = true;
        [SerializeField] private bool _requireValidGridPosition = true;
        [SerializeField] private bool _blockInputDuringTransitions = true;
        
        // Core references
        private GridSystem _gridSystem;
        private int _currentHeightLevel = 0;
        
        // Input state tracking
        private bool _isDragSelecting;
        private Vector2 _dragStartPos;
        private Vector2 _dragCurrentPos;
        private Vector3Int _lastValidGridCoord = Vector3Int.zero;
        private bool _inputBlocked = false;
        private GridPlaceable _currentPlaceable;
        
        // Input events - 3D coordinate system
        public System.Action<Vector3Int> OnGridPositionClicked;
        public System.Action<Vector3Int> OnValidGridHover;
        public System.Action OnPlacementRequested;
        public System.Action OnCancelRequested;
        public System.Action OnRotationRequested;
        public System.Action OnDeletionRequested;
        public System.Action<int> OnHeightLevelChanged;
        
        // Drag selection events
        public System.Action<Vector2, Vector2> OnDragSelectionStarted;
        public System.Action<Vector2, Vector2> OnDragSelectionUpdated;
        public System.Action<Vector2, Vector2> OnDragSelectionCompleted;
        public System.Action OnDragSelectionCancelled;
        
        // Schematic input events
        public System.Action OnSaveSchematicRequested;
        public System.Action OnCancelSchematicRequested;
        
        // Mouse and coordinate events
        public System.Action<Vector3> OnMouseWorldPositionChanged;
        public System.Action<Vector3Int> OnGridCoordinateChanged;
        public System.Action<bool> OnInputValidityChanged;
        
        public bool InputBlocked 
        { 
            get => _inputBlocked; 
            set => _inputBlocked = value; 
        }
        
        public int CurrentHeightLevel => _currentHeightLevel;
        public Vector3Int LastValidGridCoordinate => _lastValidGridCoord;
        public Vector3Int CurrentGridCoordinate => GetCurrentGridCoordinate();
        public GridPlaceable CurrentPlaceable => _currentPlaceable;
        public bool IsDragSelecting => _isDragSelecting;
        public Vector2 DragStartPosition => _dragStartPos;
        public Vector2 DragCurrentPosition => _dragCurrentPos;
        
        private void Awake()
        {
            InitializeInputHandler();
        }
        
        private void Start()
        {
            FindRequiredComponents();
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }
        
        #region ITickable Implementation

        public int Priority => TickPriority.InputSystem;
        public bool Enabled => enabled && !(_inputBlocked && _blockInputDuringTransitions);

        public void Tick(float deltaTime)
        {
            if (_inputBlocked && _blockInputDuringTransitions) return;
            
            ProcessKeyboardInput();
            ProcessMouseInput();
            ProcessHeightLevelInput();
            ProcessDragSelectionInput();
        }

        #endregion
        
        #region Initialization
        
        private void InitializeInputHandler()
        {
            if (_inputCamera == null)
                _inputCamera = UnityEngine.Camera.main;
                
            _currentHeightLevel = 0;
            _isDragSelecting = false;
            _inputBlocked = false;
        }
        
        private void FindRequiredComponents()
        {
            if (_gridSystem == null)
                _gridSystem = ServiceContainerFactory.Instance?.TryResolve<IGridSystem>() as GridSystem;
                
            if (_gridSystem == null)
                ChimeraLogger.LogWarning($"[GridInputHandler] GridSystem not found - 3D coordinate conversion may not work properly");
        }
        
        #endregion
        
        #region Main Input Processing
        
        private void ProcessKeyboardInput()
        {
            // Placement input
            if (Input.GetKeyDown(_placementKey) && !_isDragSelecting)
            {
                HandlePlacementInput();
            }
            
            // Cancel input
            if (Input.GetKeyDown(_cancelKey))
            {
                HandleCancelInput();
            }
            
            // Rotation input
            if (Input.GetKeyDown(_rotateKey))
            {
                HandleRotationInput();
            }
            
            // Delete input
            if (Input.GetKeyDown(_deleteKey))
            {
                HandleDeletionInput();
            }
            
            // Save schematic input (Ctrl+S)
            if (Input.GetKey(_saveSchematicModifier) && Input.GetKeyDown(_saveSchematicKey))
            {
                HandleSaveSchematicInput();
            }
            
            // Cancel schematic placement
            if (Input.GetKeyDown(_cancelSchematicKey))
            {
                HandleCancelSchematicInput();
            }
        }
        
        private void ProcessMouseInput()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3Int gridCoord = WorldToGridCoordinate(mouseWorldPos);
            
            // Update mouse world position
            OnMouseWorldPositionChanged?.Invoke(mouseWorldPos);
            
            // Update grid coordinate if changed
            if (gridCoord != _lastValidGridCoord)
            {
                _lastValidGridCoord = gridCoord;
                OnGridCoordinateChanged?.Invoke(gridCoord);
                
                // Validate and notify
                bool isValid = ValidateGridCoordinate(gridCoord);
                OnInputValidityChanged?.Invoke(isValid);
                
                if (isValid)
                {
                    OnValidGridHover?.Invoke(gridCoord);
                }
            }
        }
        
        private void ProcessHeightLevelInput()
        {
            int heightChange = 0;
            
            // Keyboard height navigation
            if (Input.GetKeyDown(_heightUpKey))
                heightChange = 1;
            else if (Input.GetKeyDown(_heightDownKey))
                heightChange = -1;
            else if (Input.GetKeyDown(_heightResetKey))
            {
                SetHeightLevel(0);
                return;
            }
            
            // Mouse wheel height navigation (if enabled and modifier held)
            if (_enableMouseWheelHeight)
            {
                bool modifierHeld = !_requireHeightModifier || Input.GetKey(_heightModifierKey);
                if (modifierHeld)
                {
                    float wheelDelta = Input.mouseScrollDelta.y;
                    if (Mathf.Abs(wheelDelta) > 0.1f)
                    {
                        heightChange = Mathf.RoundToInt(wheelDelta * _mouseWheelSensitivity);
                    }
                }
            }
            
            // Apply height change
            if (heightChange != 0)
            {
                int newHeightLevel = _currentHeightLevel + heightChange;
                SetHeightLevel(newHeightLevel);
            }
        }
        
        private void ProcessDragSelectionInput()
        {
            if (!_enableDragSelection) return;
            
            if (Input.GetKeyDown(_dragSelectKey))
            {
                _dragStartPos = Input.mousePosition;
            }
            else if (Input.GetKey(_dragSelectKey) && !_isDragSelecting)
            {
                Vector2 currentMousePos = Input.mousePosition;
                float dragDistance = Vector2.Distance(_dragStartPos, currentMousePos);
                
                if (dragDistance > _dragThreshold)
                {
                    StartDragSelection();
                }
            }
            else if (Input.GetKeyUp(_dragSelectKey))
            {
                if (_isDragSelecting)
                {
                    CompleteDragSelection();
                }
            }
            
            // Update drag selection if active
            if (_isDragSelecting)
            {
                UpdateDragSelection();
            }
        }
        
        #endregion
        
        #region Input Event Handlers
        
        private void HandlePlacementInput()
        {
            Vector3Int gridCoord = GetCurrentGridCoordinate();
            if (ValidateGridCoordinate(gridCoord))
            {
                OnGridPositionClicked?.Invoke(gridCoord);
                OnPlacementRequested?.Invoke();
            }
        }
        
        private void HandleCancelInput()
        {
            if (_isDragSelecting)
            {
                CancelDragSelection();
            }
            else
            {
                OnCancelRequested?.Invoke();
            }
        }
        
        private void HandleRotationInput()
        {
            OnRotationRequested?.Invoke();
        }
        
        private void HandleDeletionInput()
        {
            OnDeletionRequested?.Invoke();
        }
        
        private void HandleSaveSchematicInput()
        {
            OnSaveSchematicRequested?.Invoke();
        }
        
        private void HandleCancelSchematicInput()
        {
            OnCancelSchematicRequested?.Invoke();
        }
        
        #endregion
        
        #region Drag Selection Implementation
        
        private void StartDragSelection()
        {
            _isDragSelecting = true;
            _dragCurrentPos = Input.mousePosition;
            OnDragSelectionStarted?.Invoke(_dragStartPos, _dragCurrentPos);
        }
        
        private void UpdateDragSelection()
        {
            _dragCurrentPos = Input.mousePosition;
            OnDragSelectionUpdated?.Invoke(_dragStartPos, _dragCurrentPos);
        }
        
        private void CompleteDragSelection()
        {
            if (_isDragSelecting)
            {
                OnDragSelectionCompleted?.Invoke(_dragStartPos, _dragCurrentPos);
                _isDragSelecting = false;
            }
        }
        
        private void CancelDragSelection()
        {
            if (_isDragSelecting)
            {
                OnDragSelectionCancelled?.Invoke();
                _isDragSelecting = false;
            }
        }
        
        #endregion
        
        #region 3D Coordinate Conversion and Height Management
        
        /// <summary>
        /// Set the current height level for 3D placement
        /// </summary>
        public void SetHeightLevel(int heightLevel)
        {
            int maxHeight = _gridSystem != null ? _gridSystem.MaxHeightLevels - 1 : 49;
            int clampedHeight = Mathf.Clamp(heightLevel, 0, maxHeight);
            
            if (clampedHeight != _currentHeightLevel)
            {
                _currentHeightLevel = clampedHeight;
                OnHeightLevelChanged?.Invoke(_currentHeightLevel);
                
                // Update grid coordinate with new height
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                Vector3Int gridCoord = WorldToGridCoordinate(mouseWorldPos);
                OnGridCoordinateChanged?.Invoke(gridCoord);
            }
        }
        
        /// <summary>
        /// Get mouse world position with raycast
        /// </summary>
        public Vector3 GetMouseWorldPosition()
        {
            if (_inputCamera == null) return Vector3.zero;
            
            Ray ray = _inputCamera.ScreenPointToRay(Input.mousePosition);
            
            if (_usePhysicsRaycast)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _groundLayer))
                {
                    return hit.point;
                }
            }
            
            // Fallback: project onto ground plane (Y = current height level)
            float groundHeight = _gridSystem != null ? _gridSystem.GetWorldYForHeightLevel(_currentHeightLevel) : _currentHeightLevel;
            if (Mathf.Abs(ray.direction.y) > 0.01f)
            {
                float distance = (groundHeight - ray.origin.y) / ray.direction.y;
                if (distance > 0 && distance < _raycastDistance)
                {
                    return ray.origin + ray.direction * distance;
                }
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Convert world position to 3D grid coordinate with current height level
        /// </summary>
        public Vector3Int WorldToGridCoordinate(Vector3 worldPosition)
        {
            if (_gridSystem != null)
            {
                Vector3Int baseCoord = _gridSystem.WorldToGridPosition(worldPosition);
                // Override Z coordinate with current height level
                return new Vector3Int(baseCoord.x, baseCoord.y, _currentHeightLevel);
            }
            
            // Fallback calculation if no grid system
            return new Vector3Int(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.z),
                _currentHeightLevel
            );
        }
        
        /// <summary>
        /// Get current grid coordinate at mouse position
        /// </summary>
        public Vector3Int GetCurrentGridCoordinate()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            return WorldToGridCoordinate(mouseWorldPos);
        }
        
        /// <summary>
        /// Validate if grid coordinate is within bounds and valid for placement
        /// </summary>
        public bool ValidateGridCoordinate(Vector3Int gridCoordinate)
        {
            if (!_enableInputValidation) return true;
            
            if (_gridSystem != null)
            {
                return _gridSystem.IsValidGridPosition(gridCoordinate);
            }
            
            // Basic bounds validation if no grid system
            return gridCoordinate.x >= 0 && gridCoordinate.y >= 0 && gridCoordinate.z >= 0;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Check if specific key is currently pressed
        /// </summary>
        public bool IsKeyPressed(KeyCode key)
        {
            return Input.GetKey(key);
        }
        
        /// <summary>
        /// Check if modifier keys are being held
        /// </summary>
        public bool IsShiftHeld() => Input.GetKey(_addToSelectionKey);
        public bool IsControlHeld() => Input.GetKey(_saveSchematicModifier);
        public bool IsHeightModifierHeld() => Input.GetKey(_heightModifierKey);
        
        /// <summary>
        /// Force update input processing (useful for external control)
        /// </summary>
        public void ForceInputUpdate()
        {
            ProcessMouseInput();
        }
        
        /// <summary>
        /// Reset height level to ground (0)
        /// </summary>
        public void ResetHeightLevel()
        {
            SetHeightLevel(0);
        }
        
        /// <summary>
        /// Set the current placeable object for placement mode
        /// </summary>
        public void SetPlacementTarget(GridPlaceable placeable)
        {
            _currentPlaceable = placeable;
        }
        
        /// <summary>
        /// Clear the current placement target
        /// </summary>
        public void ClearPlacementTarget()
        {
            _currentPlaceable = null;
        }
        
        /// <summary>
        /// Process input specifically for placement mode
        /// </summary>
        public void ProcessPlacementInput()
        {
            if (_currentPlaceable == null) return;
            
            ProcessMouseInput();
            ProcessKeyboardInput();
        }
        
        #endregion
        
        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
    }
}