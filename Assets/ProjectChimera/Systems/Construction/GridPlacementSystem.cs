using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Grid Placement System - Handles grid-based placement and positioning
    /// Provides the grid-based construction system as described in gameplay document
    /// Manages precise control and modular design for equipment and facility placement
    /// </summary>
    public class GridPlacementSystem : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private float _gridSize = 0.5f;
        [SerializeField] private Vector3Int _gridDimensions = new Vector3Int(50, 10, 50);
        [SerializeField] private bool _showGridGizmo = true;
        [SerializeField] private Color _gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Header("Placement Settings")]
        [SerializeField] private LayerMask _placementLayerMask;
        [SerializeField] private LayerMask _obstacleLayerMask;
        [SerializeField] private float _placementClearanceRadius = 1.0f;
        [SerializeField] private bool _enableSnapToGrid = true;

        // Grid state
        private bool[,] _occupiedCells;
        private Dictionary<Vector3Int, ConstructionObject> _placedObjects = new Dictionary<Vector3Int, ConstructionObject>();
        private ConstructionObject _currentPlacementObject;

        // Placement preview
        private GameObject _placementPreview;
        private bool _isPlacingObject = false;
        private Vector3Int _currentGridPosition;
        private int _currentRotation = 0;

        private void Awake()
        {
            InitializeGrid();
        }

        private void Update()
        {
            HandlePlacementInput();
            UpdatePlacementPreview();
        }

        private void OnDrawGizmos()
        {
            if (!_showGridGizmo) return;

            Gizmos.color = _gridColor;

            // Draw grid lines
            for (int x = 0; x <= _gridDimensions.x; x++)
            {
                for (int z = 0; z <= _gridDimensions.z; z++)
                {
                    Vector3 start = new Vector3(x * _gridSize, 0, z * _gridSize);
                    Vector3 end = new Vector3(x * _gridSize, _gridDimensions.y * _gridSize, z * _gridSize);
                    Gizmos.DrawLine(start, end);
                }
            }

            for (int y = 0; y <= _gridDimensions.y; y++)
            {
                for (int x = 0; x <= _gridDimensions.x; x++)
                {
                    Vector3 start = new Vector3(x * _gridSize, y * _gridSize, 0);
                    Vector3 end = new Vector3(x * _gridSize, y * _gridSize, _gridDimensions.z * _gridSize);
                    Gizmos.DrawLine(start, end);
                }
            }
        }

        /// <summary>
        /// Initializes the grid system
        /// </summary>
        private void InitializeGrid()
        {
            // Initialize occupied cells array
            _occupiedCells = new bool[_gridDimensions.x, _gridDimensions.z];

            ChimeraLogger.Log($"[GridPlacementSystem] Initialized grid: {_gridDimensions.x}x{_gridDimensions.z} cells, size {_gridSize}");
        }

        /// <summary>
        /// Starts placing a construction object
        /// </summary>
        public void StartPlacingObject(ConstructionObject constructionObject)
        {
            if (constructionObject == null)
            {
                ChimeraLogger.LogWarning("[GridPlacementSystem] No construction object provided");
                return;
            }

            _currentPlacementObject = constructionObject;
            _isPlacingObject = true;
            _currentRotation = 0;

            CreatePlacementPreview();
            ChimeraLogger.Log($"[GridPlacementSystem] Started placing: {constructionObject.ObjectName}");
        }

        /// <summary>
        /// Cancels the current placement
        /// </summary>
        public void CancelPlacement()
        {
            _isPlacingObject = false;
            _currentPlacementObject = null;
            DestroyPlacementPreview();

            ChimeraLogger.Log("[GridPlacementSystem] Placement cancelled");
        }

        /// <summary>
        /// Confirms the current placement
        /// </summary>
        public bool ConfirmPlacement()
        {
            if (!_isPlacingObject || _currentPlacementObject == null)
                return false;

            if (!CanPlaceAtPosition(_currentGridPosition, _currentPlacementObject))
            {
                ChimeraLogger.LogWarning("[GridPlacementSystem] Cannot place object at current position");
                return false;
            }

            PlaceObject(_currentGridPosition, _currentPlacementObject, _currentRotation);
            CancelPlacement();

            return true;
        }

        /// <summary>
        /// Rotates the current placement object
        /// </summary>
        public void RotatePlacement(int steps = 1)
        {
            if (!_isPlacingObject) return;

            _currentRotation = (_currentRotation + steps * 90) % 360;
            UpdatePlacementPreview();
        }

        /// <summary>
        /// Handles input for placement positioning
        /// </summary>
        private void HandlePlacementInput()
        {
            if (!_isPlacingObject) return;

            // Handle mouse input for grid position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _placementLayerMask))
            {
                Vector3 rawPosition = hit.point;
                Vector3Int gridPos = WorldToGridPosition(rawPosition);

                // Clamp to grid bounds
                gridPos.x = Mathf.Clamp(gridPos.x, 0, _gridDimensions.x - 1);
                gridPos.z = Mathf.Clamp(gridPos.z, 0, _gridDimensions.z - 1);

                _currentGridPosition = gridPos;
            }

            // Handle rotation input
            if (Input.GetKeyDown(KeyCode.R))
            {
                RotatePlacement();
            }

            // Handle placement confirmation
            if (Input.GetMouseButtonDown(0))
            {
                ConfirmPlacement();
            }

            // Handle placement cancellation
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// Updates the placement preview
        /// </summary>
        private void UpdatePlacementPreview()
        {
            if (!_isPlacingObject || _placementPreview == null) return;

            Vector3 worldPos = GridToWorldPosition(_currentGridPosition);
            _placementPreview.transform.position = worldPos;
            _placementPreview.transform.rotation = Quaternion.Euler(0, _currentRotation, 0);

            // Update preview color based on validity
            bool canPlace = CanPlaceAtPosition(_currentGridPosition, _currentPlacementObject);
            SetPreviewColor(canPlace ? Color.green : Color.red);
        }

        /// <summary>
        /// Creates a placement preview object
        /// </summary>
        private void CreatePlacementPreview()
        {
            if (_currentPlacementObject == null || !_currentPlacementObject.Prefab) return;

            _placementPreview = Instantiate(_currentPlacementObject.Prefab);
            _placementPreview.name = $"{_currentPlacementObject.ObjectName}_Preview";

            // Make preview semi-transparent
            SetPreviewTransparency(0.5f);

            // Disable colliders on preview
            var colliders = _placementPreview.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
        }

        /// <summary>
        /// Destroys the placement preview
        /// </summary>
        private void DestroyPlacementPreview()
        {
            if (_placementPreview != null)
            {
                Destroy(_placementPreview);
                _placementPreview = null;
            }
        }

        /// <summary>
        /// Places an object at the specified grid position
        /// </summary>
        private void PlaceObject(Vector3Int gridPos, ConstructionObject constructionObject, int rotation)
        {
            Vector3 worldPos = GridToWorldPosition(gridPos);

            // Instantiate the object
            GameObject placedObject = Instantiate(constructionObject.Prefab, worldPos, Quaternion.Euler(0, rotation, 0));
            placedObject.name = $"{constructionObject.ObjectName}_{gridPos.x}_{gridPos.z}";

            // Mark grid cells as occupied
            Vector2Int size = constructionObject.GridSize;
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3Int cellPos = new Vector3Int(gridPos.x + x, 0, gridPos.z + z);
                    if (IsValidGridPosition(cellPos))
                    {
                        _occupiedCells[cellPos.x, cellPos.z] = true;
                    }
                }
            }

            // Store reference
            _placedObjects[gridPos] = constructionObject;

            ChimeraLogger.Log($"[GridPlacementSystem] Placed {constructionObject.ObjectName} at grid {gridPos}");
        }

        /// <summary>
        /// Removes an object from the specified grid position
        /// </summary>
        public bool RemoveObject(Vector3Int gridPos)
        {
            if (!_placedObjects.ContainsKey(gridPos))
                return false;

            var constructionObject = _placedObjects[gridPos];

            // Find and destroy the game object
            Vector3 worldPos = GridToWorldPosition(gridPos);
            var objectsAtPosition = Physics.OverlapSphere(worldPos, 0.1f);
            foreach (var obj in objectsAtPosition)
            {
                if (obj.name.Contains(constructionObject.ObjectName))
                {
                    Destroy(obj.gameObject);
                    break;
                }
            }

            // Clear grid cells
            Vector2Int size = constructionObject.GridSize;
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3Int cellPos = new Vector3Int(gridPos.x + x, 0, gridPos.z + z);
                    if (IsValidGridPosition(cellPos))
                    {
                        _occupiedCells[cellPos.x, cellPos.z] = false;
                    }
                }
            }

            _placedObjects.Remove(gridPos);

            ChimeraLogger.Log($"[GridPlacementSystem] Removed object at grid {gridPos}");
            return true;
        }

        /// <summary>
        /// Checks if an object can be placed at the specified position
        /// </summary>
        public bool CanPlaceAtPosition(Vector3Int gridPos, ConstructionObject constructionObject)
        {
            if (constructionObject == null) return false;

            Vector2Int size = constructionObject.GridSize;

            // Check if all required cells are free and within bounds
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3Int cellPos = new Vector3Int(gridPos.x + x, 0, gridPos.z + z);

                    if (!IsValidGridPosition(cellPos) || _occupiedCells[cellPos.x, cellPos.z])
                    {
                        return false;
                    }
                }
            }

            // Check for physical obstacles
            Vector3 worldPos = GridToWorldPosition(gridPos);
            Vector3 checkSize = new Vector3(size.x * _gridSize, 2f, size.y * _gridSize);
            bool hasObstacles = Physics.CheckBox(worldPos, checkSize / 2, Quaternion.identity, _obstacleLayerMask);

            return !hasObstacles;
        }

        /// <summary>
        /// Converts world position to grid position
        /// </summary>
        public Vector3Int WorldToGridPosition(Vector3 worldPos)
        {
            if (_enableSnapToGrid)
            {
                return new Vector3Int(
                    Mathf.RoundToInt(worldPos.x / _gridSize),
                    Mathf.RoundToInt(worldPos.y / _gridSize),
                    Mathf.RoundToInt(worldPos.z / _gridSize)
                );
            }
            else
            {
                return new Vector3Int(
                    Mathf.FloorToInt(worldPos.x / _gridSize),
                    Mathf.FloorToInt(worldPos.y / _gridSize),
                    Mathf.FloorToInt(worldPos.z / _gridSize)
                );
            }
        }

        /// <summary>
        /// Converts grid position to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector3Int gridPos)
        {
            return new Vector3(
                gridPos.x * _gridSize,
                gridPos.y * _gridSize,
                gridPos.z * _gridSize
            );
        }

        /// <summary>
        /// Checks if a grid position is valid
        /// </summary>
        private bool IsValidGridPosition(Vector3Int pos)
        {
            return pos.x >= 0 && pos.x < _gridDimensions.x &&
                   pos.z >= 0 && pos.z < _gridDimensions.z &&
                   pos.y >= 0 && pos.y < _gridDimensions.y;
        }

        /// <summary>
        /// Sets the transparency of the placement preview
        /// </summary>
        private void SetPreviewTransparency(float alpha)
        {
            if (_placementPreview == null) return;

            var renderers = _placementPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var material = renderer.material;
                var color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }

        /// <summary>
        /// Sets the color of the placement preview
        /// </summary>
        private void SetPreviewColor(Color color)
        {
            if (_placementPreview == null) return;

            var renderers = _placementPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var material = renderer.material;
                color.a = material.color.a; // Preserve transparency
                material.color = color;
            }
        }

        /// <summary>
        /// Gets all placed objects
        /// </summary>
        public Dictionary<Vector3Int, ConstructionObject> GetPlacedObjects()
        {
            return new Dictionary<Vector3Int, ConstructionObject>(_placedObjects);
        }

        /// <summary>
        /// Gets the object at a specific grid position
        /// </summary>
        public ConstructionObject GetObjectAtPosition(Vector3Int gridPos)
        {
            return _placedObjects.ContainsKey(gridPos) ? _placedObjects[gridPos] : null;
        }

        /// <summary>
        /// Checks if the grid system is currently placing an object
        /// </summary>
        public bool IsPlacingObject()
        {
            return _isPlacingObject;
        }

        /// <summary>
        /// Gets the current placement position
        /// </summary>
        public Vector3Int GetCurrentPlacementPosition()
        {
            return _currentGridPosition;
        }

        /// <summary>
        /// Gets the grid dimensions
        /// </summary>
        public Vector3Int GetGridDimensions()
        {
            return _gridDimensions;
        }

        /// <summary>
        /// Gets the grid size
        /// </summary>
        public float GetGridSize()
        {
            return _gridSize;
        }
    }
}

