using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// BASIC: Simple grid input handler for Project Chimera's construction system.
    /// Focuses on essential grid input without complex drag selection and validation systems.
    /// </summary>
    public class GridInputHandler : MonoBehaviour
    {
        [Header("Basic Input Settings")]
        [SerializeField] private bool _enableBasicInput = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private LayerMask _gridLayer = 1;
        [SerializeField] private float _raycastDistance = 100f;

        // Basic input tracking
        private Camera _mainCamera;
        private Vector3Int _currentGridPosition = Vector3Int.zero;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for basic grid input
        /// </summary>
        public event System.Action<Vector3Int> OnGridClicked;
        public event System.Action<Vector3Int> OnGridHovered;
        public event System.Action OnPlacementRequested;
        public event System.Action OnCancelRequested;

        /// <summary>
        /// Initialize basic grid input handler
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindObjectOfType<Camera>();
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[GridInputHandler] Initialized successfully");
            }
        }

        /// <summary>
        /// Update input handling
        /// </summary>
        private void Update()
        {
            if (!_enableBasicInput || !_isInitialized) return;

            HandleMouseInput();
            HandleKeyboardInput();
        }

        /// <summary>
        /// Handle mouse input for grid interaction
        /// </summary>
        private void HandleMouseInput()
        {
            // Get mouse position in world space
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _gridLayer))
            {
                // Convert world position to grid coordinates
                Vector3Int gridPos = WorldToGrid(hit.point);

                // Check if grid position changed
                if (gridPos != _currentGridPosition)
                {
                    _currentGridPosition = gridPos;
                    OnGridHovered?.Invoke(gridPos);
                }

                // Handle mouse clicks
                if (Input.GetMouseButtonDown(0)) // Left click - place
                {
                    OnGridClicked?.Invoke(gridPos);
                    OnPlacementRequested?.Invoke();

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log($"[GridInputHandler] Placement requested at {gridPos}");
                    }
                }
                else if (Input.GetMouseButtonDown(1)) // Right click - cancel
                {
                    OnCancelRequested?.Invoke();

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("[GridInputHandler] Cancel requested");
                    }
                }
            }
        }

        /// <summary>
        /// Handle keyboard input
        /// </summary>
        private void HandleKeyboardInput()
        {
            // Basic keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancelRequested?.Invoke();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("[GridInputHandler] Cancel requested (Escape)");
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnPlacementRequested?.Invoke();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("[GridInputHandler] Placement requested (Enter)");
                }
            }
        }

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        private Vector3Int WorldToGrid(Vector3 worldPos)
        {
            // Simple grid conversion - assuming 1 unit = 1 grid cell
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x),
                Mathf.RoundToInt(worldPos.y),
                Mathf.RoundToInt(worldPos.z)
            );
        }

        /// <summary>
        /// Get current grid position
        /// </summary>
        public Vector3Int GetCurrentGridPosition()
        {
            return _currentGridPosition;
        }

        /// <summary>
        /// Set input enabled state
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _enableBasicInput = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[GridInputHandler] Input {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Check if input is enabled
        /// </summary>
        public bool IsInputEnabled()
        {
            return _enableBasicInput && _isInitialized;
        }

        /// <summary>
        /// Get input statistics
        /// </summary>
        public InputStats GetStats()
        {
            return new InputStats
            {
                CurrentGridPosition = _currentGridPosition,
                IsInputEnabled = _enableBasicInput,
                IsInitialized = _isInitialized,
                CameraFound = _mainCamera != null
            };
        }

        /// <summary>
        /// Set grid layer mask
        /// </summary>
        public void SetGridLayer(LayerMask layerMask)
        {
            _gridLayer = layerMask;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[GridInputHandler] Grid layer updated to {layerMask.value}");
            }
        }

        /// <summary>
        /// Force grid position update
        /// </summary>
        public void ForceGridPosition(Vector3Int gridPos)
        {
            _currentGridPosition = gridPos;
            OnGridHovered?.Invoke(gridPos);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[GridInputHandler] Grid position forced to {gridPos}");
            }
        }
    }

    /// <summary>
    /// Input statistics
    /// </summary>
    [System.Serializable]
    public struct InputStats
    {
        public Vector3Int CurrentGridPosition;
        public bool IsInputEnabled;
        public bool IsInitialized;
        public bool CameraFound;
    }
}
