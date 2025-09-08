using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Central grid system for snap-to-grid construction placement in Project Chimera.
    /// Provides grid-based spatial organization, snap-to-grid functionality, and visual grid rendering.
    /// Designed to replace the complex legacy construction system with a simple, robust grid-based approach.
    /// </summary>
    public class GridSystem : MonoBehaviour, ITickable
    {
        [Header("Grid Configuration")]
        [SerializeField] private GridSnapSettings _gridSettings = new GridSnapSettings
        {
            GridSize = 1.0f,
            SnapToGrid = true,
            ShowGrid = true,
            GridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f)
        };
        
        [Header("Grid Bounds")]
        [SerializeField] private Vector3 _gridOrigin = Vector3.zero;
        [SerializeField] private Vector3 _gridDimensions = new Vector3(100f, 100f, 50f); // 100x100x50 meter grid (width, depth, height)
        [SerializeField] private float _gridHeight = 0.01f; // Slightly above ground
        [SerializeField] private int _maxHeightLevels = 50; // Maximum vertical grid levels
        [SerializeField] private float _heightLevelSpacing = 1f; // Vertical spacing between levels
        
        [Header("Visual Settings")]
        [SerializeField] private Material _gridMaterial;
        [SerializeField] private bool _showGridLines = true;
        [SerializeField] private bool _showGridBounds = true;
        [SerializeField] private LayerMask _snapLayers = -1;
        
        [Header("Placement Settings")]
        [SerializeField] private float _placementTolerance = 0.1f;
        [SerializeField] private bool _validatePlacement = true;
        [SerializeField] private bool _preventOverlap = true;
        
        // Core grid data
        private Dictionary<Vector3Int, GridCell> _gridCells = new Dictionary<Vector3Int, GridCell>();
        private List<GridPlaceable> _placedObjects = new List<GridPlaceable>();
        private GameObject _gridVisualization;
        private LineRenderer[] _gridLines;
        
        // Grid calculation cache
        private Vector3Int _lastGridCoord = Vector3Int.zero;
        private Vector3 _lastWorldPos = Vector3.zero;
        private bool _gridNeedsUpdate = true;
        
        // 3D Grid visualization
        private Dictionary<int, GameObject> _heightLevelVisualizations = new Dictionary<int, GameObject>();
        private int _currentVisibleHeightLevel = 0;
        
        // Events
        public System.Action<Vector3, Vector3Int> OnGridPositionChanged;
        public System.Action<GridPlaceable> OnObjectPlaced;
        public System.Action<GridPlaceable> OnObjectRemoved;
        public System.Action<bool> OnGridVisibilityChanged;
        
        // Properties
        public GridSnapSettings GridSettings => _gridSettings;
        public Vector3 GridOrigin => _gridOrigin;
        public Vector3 GridDimensions => _gridDimensions;
        public float GridSize => _gridSettings.GridSize;
        public bool SnapEnabled => _gridSettings.SnapToGrid;
        public bool GridVisible => _gridSettings.ShowGrid;
        public int PlacedObjectCount => _placedObjects.Count;
        public Dictionary<Vector3Int, GridCell> GridCells => _gridCells;
        public int MaxHeightLevels => _maxHeightLevels;
        public float HeightLevelSpacing => _heightLevelSpacing;
        public int CurrentVisibleHeightLevel => _currentVisibleHeightLevel;
        
        private void Awake()
        {
            InitializeGrid();
        }
        
        private void Start()
        {
            CreateGridVisualization();
            UpdateGridVisibility();
            
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }
        
        #region ITickable Implementation
        
        public int Priority => TickPriority.ConstructionSystem;
        public bool Enabled => enabled && _gridCells.Count > 0;
        
        public void Tick(float deltaTime)
        {
            if (_gridNeedsUpdate)
            {
                UpdateGridVisualization();
                _gridNeedsUpdate = false;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeGrid()
        {
            // Initialize 3D grid cells
            int gridWidth = Mathf.RoundToInt(_gridDimensions.x / _gridSettings.GridSize);
            int gridDepth = Mathf.RoundToInt(_gridDimensions.y / _gridSettings.GridSize);
            int gridHeight = Mathf.RoundToInt(_gridDimensions.z / _heightLevelSpacing);
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridDepth; y++)
                {
                    for (int z = 0; z < gridHeight; z++)
                    {
                        var gridCoord = new Vector3Int(x, y, z);
                        var worldPos = GridToWorldPosition(gridCoord);
                        
                        _gridCells[gridCoord] = new GridCell
                        {
                            GridCoordinate = gridCoord,
                            WorldPosition = worldPos,
                            IsOccupied = false,
                            OccupyingObject = null,
                            CellType = GridCellType.Standard,
                            IsValid = true
                        };
                    }
                }
            }
            
            LogInfo($"Grid initialized: {gridWidth}x{gridDepth}x{gridHeight} cells ({_gridCells.Count} total)");
        }
        
        private void CreateGridVisualization()
        {
            if (_gridVisualization != null)
            {
                DestroyImmediate(_gridVisualization);
            }
            
            _gridVisualization = new GameObject("GridVisualization");
            _gridVisualization.transform.SetParent(transform);
            _gridVisualization.transform.localPosition = Vector3.zero;
            
            CreateGridLines();
            CreateGridPlane();
        }
        
        private void CreateGridLines()
        {
            if (!_showGridLines) return;
            
            var linesParent = new GameObject("GridLines");
            linesParent.transform.SetParent(_gridVisualization.transform);
            
            int gridWidth = Mathf.RoundToInt(_gridDimensions.x / _gridSettings.GridSize);
            int gridHeight = Mathf.RoundToInt(_gridDimensions.y / _gridSettings.GridSize);
            
            // Create vertical lines
            for (int x = 0; x <= gridWidth; x++)
            {
                var lineObj = new GameObject($"VerticalLine_{x}");
                lineObj.transform.SetParent(linesParent.transform);
                
                var lineRenderer = lineObj.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lineRenderer);
                
                Vector3 start = _gridOrigin + new Vector3(x * _gridSettings.GridSize, _gridHeight, 0);
                Vector3 end = _gridOrigin + new Vector3(x * _gridSettings.GridSize, _gridHeight, _gridDimensions.y);
                
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
            }
            
            // Create horizontal lines
            for (int y = 0; y <= gridHeight; y++)
            {
                var lineObj = new GameObject($"HorizontalLine_{y}");
                lineObj.transform.SetParent(linesParent.transform);
                
                var lineRenderer = lineObj.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lineRenderer);
                
                Vector3 start = _gridOrigin + new Vector3(0, _gridHeight, y * _gridSettings.GridSize);
                Vector3 end = _gridOrigin + new Vector3(_gridDimensions.x, _gridHeight, y * _gridSettings.GridSize);
                
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
            }
        }
        
        private void ConfigureLineRenderer(LineRenderer lineRenderer)
        {
            lineRenderer.material = _gridMaterial ? _gridMaterial : CreateDefaultGridMaterial();
            lineRenderer.startColor = _gridSettings.GridColor;
            lineRenderer.endColor = _gridSettings.GridColor;
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
        
        private Material CreateDefaultGridMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = _gridSettings.GridColor;
            material.SetFloat("_Mode", 3); // Transparent
            material.renderQueue = 3000;
            return material;
        }
        
        private void CreateGridPlane()
        {
            if (!_showGridBounds) return;
            
            var planeObj = new GameObject("GridPlane");
            planeObj.transform.SetParent(_gridVisualization.transform);
            planeObj.transform.position = _gridOrigin + new Vector3(_gridDimensions.x/2, _gridHeight, _gridDimensions.y/2);
            planeObj.transform.localScale = new Vector3(_gridDimensions.x/10f, 1f, _gridDimensions.y/10f);
            
            var meshRenderer = planeObj.AddComponent<MeshRenderer>();
            var meshFilter = planeObj.AddComponent<MeshFilter>();
            
            meshFilter.mesh = CreatePlaneMesh();
            meshRenderer.material = CreateGridPlaneMaterial();
        }
        
        private Mesh CreatePlaneMesh()
        {
            var mesh = new Mesh();
            
            Vector3[] vertices = {
                new Vector3(-5, 0, -5),
                new Vector3(5, 0, -5),
                new Vector3(5, 0, 5),
                new Vector3(-5, 0, 5)
            };
            
            int[] triangles = { 0, 2, 1, 0, 3, 2 };
            Vector2[] uv = {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private Material CreateGridPlaneMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = new Color(_gridSettings.GridColor.r, _gridSettings.GridColor.g, _gridSettings.GridColor.b, 0.1f);
            material.SetFloat("_Mode", 3); // Transparent
            material.renderQueue = 2999; // Just below grid lines
            return material;
        }
        
        #endregion
        
        #region Core Grid Functions
        
        /// <summary>
        /// Snap world position to nearest grid point
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            if (!_gridSettings.SnapToGrid)
                return worldPosition;
            
            Vector3 localPos = worldPosition - _gridOrigin;
            
            float snappedX = Mathf.Round(localPos.x / _gridSettings.GridSize) * _gridSettings.GridSize;
            float snappedZ = Mathf.Round(localPos.z / _gridSettings.GridSize) * _gridSettings.GridSize;
            
            return _gridOrigin + new Vector3(snappedX, worldPosition.y, snappedZ);
        }
        
        /// <summary>
        /// Convert world position to 3D grid coordinate
        /// </summary>
        public Vector3Int WorldToGridPosition(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - _gridOrigin;
            
            int gridX = Mathf.RoundToInt(localPos.x / _gridSettings.GridSize);
            int gridY = Mathf.RoundToInt(localPos.z / _gridSettings.GridSize);
            int gridZ = Mathf.RoundToInt(localPos.y / _heightLevelSpacing);
            
            return new Vector3Int(gridX, gridY, gridZ);
        }
        
        /// <summary>
        /// Convert 3D grid coordinate to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector3Int gridCoordinate)
        {
            float worldX = _gridOrigin.x + gridCoordinate.x * _gridSettings.GridSize;
            float worldZ = _gridOrigin.z + gridCoordinate.y * _gridSettings.GridSize;
            float worldY = _gridOrigin.y + gridCoordinate.z * _heightLevelSpacing;
            
            return new Vector3(worldX, worldY, worldZ);
        }
        
        /// <summary>
        /// Convert world position to 2D grid coordinate (backward compatibility)
        /// </summary>
        [System.Obsolete("Use WorldToGridPosition(Vector3) for 3D coordinates")]
        public Vector2Int WorldToGridPosition2D(Vector3 worldPosition)
        {
            Vector3Int coord3D = WorldToGridPosition(worldPosition);
            return new Vector2Int(coord3D.x, coord3D.y);
        }
        
        /// <summary>
        /// Convert 2D grid coordinate to world position (backward compatibility)
        /// </summary>
        [System.Obsolete("Use GridToWorldPosition(Vector3Int) for 3D coordinates")]
        public Vector3 GridToWorldPosition2D(Vector2Int gridCoordinate)
        {
            return GridToWorldPosition(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }
        
        /// <summary>
        /// Check if 3D grid coordinate is valid
        /// </summary>
        public bool IsValidGridPosition(Vector3Int gridCoordinate)
        {
            int maxX = Mathf.RoundToInt(_gridDimensions.x / _gridSettings.GridSize);
            int maxY = Mathf.RoundToInt(_gridDimensions.y / _gridSettings.GridSize);
            int maxZ = Mathf.RoundToInt(_gridDimensions.z / _heightLevelSpacing);
            
            return gridCoordinate.x >= 0 && gridCoordinate.x < maxX &&
                   gridCoordinate.y >= 0 && gridCoordinate.y < maxY &&
                   gridCoordinate.z >= 0 && gridCoordinate.z < maxZ;
        }
        
        /// <summary>
        /// Check if 2D grid coordinate is valid (backward compatibility)
        /// </summary>
        [System.Obsolete("Use IsValidGridPosition(Vector3Int) for 3D coordinates")]
        public bool IsValidGridPosition2D(Vector2Int gridCoordinate)
        {
            return IsValidGridPosition(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }
        
        /// <summary>
        /// Check if 3D grid position is available for placement
        /// </summary>
        public bool IsGridPositionAvailable(Vector3Int gridCoordinate)
        {
            if (!IsValidGridPosition(gridCoordinate))
                return false;
            
            if (_gridCells.TryGetValue(gridCoordinate, out GridCell cell))
            {
                return !cell.IsOccupied && cell.IsValid;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get grid cell at 3D coordinate
        /// </summary>
        public GridCell GetGridCell(Vector3Int gridCoordinate)
        {
            _gridCells.TryGetValue(gridCoordinate, out GridCell cell);
            return cell;
        }
        
        /// <summary>
        /// Get grid cell at 2D coordinate (backward compatibility)
        /// </summary>
        [System.Obsolete("Use GetGridCell(Vector3Int) for 3D coordinates")]
        public GridCell GetGridCell2D(Vector2Int gridCoordinate)
        {
            return GetGridCell(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }
        
        /// <summary>
        /// Check if a 3D area is available for placement
        /// </summary>
        public bool IsAreaAvailable(Vector3Int gridCoordinate, Vector3Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int checkCoord = gridCoordinate + new Vector3Int(x, y, z);
                        if (!IsGridPositionAvailable(checkCoord))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// Check if a 2D area is available for placement (backward compatibility)
        /// </summary>
        [System.Obsolete("Use IsAreaAvailable(Vector3Int, Vector3Int) for 3D coordinates")]
        public bool IsAreaAvailable2D(Vector2Int gridCoordinate, Vector2Int size)
        {
            return IsAreaAvailable(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0), new Vector3Int(size.x, size.y, 1));
        }
        
        #endregion
        
        #region Object Placement
        
        /// <summary>
        /// Place an object on the grid
        /// </summary>
        public bool PlaceObject(GridPlaceable placeable, Vector3 worldPosition)
        {
            Vector3Int gridCoord = WorldToGridPosition(worldPosition);
            return PlaceObject(placeable, gridCoord);
        }
        
        /// <summary>
        /// Place an object on the 2D grid at specific coordinate (backward compatibility)
        /// </summary>
        [System.Obsolete("Use PlaceObject(GridPlaceable, Vector3Int) for 3D coordinates")]
        public bool PlaceObject(GridPlaceable placeable, Vector2Int gridCoordinate)
        {
            return PlaceObject(placeable, new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }

        /// <summary>
        /// Place an object on the 3D grid at specific coordinate
        /// </summary>
        public bool PlaceObject(GridPlaceable placeable, Vector3Int gridCoordinate)
        {
            if (placeable == null)
            {
                LogWarning("Cannot place null object on grid");
                return false;
            }
            
            // Check if area is available
            if (!IsAreaAvailable(gridCoordinate, placeable.GridSize))
            {
                LogWarning($"Cannot place {placeable.name} - area not available at {gridCoordinate}");
                return false;
            }
            
            // Occupy grid cells
            OccupyCells(gridCoordinate, placeable.GridSize, placeable);
            
            // Set object position
            Vector3 worldPos = GridToWorldPosition(gridCoordinate);
            placeable.transform.position = worldPos;
            placeable.GridCoordinate = gridCoordinate;
            placeable.IsPlaced = true;
            
            // Track placed object
            _placedObjects.Add(placeable);
            
            OnObjectPlaced?.Invoke(placeable);
            LogInfo($"Placed {placeable.name} at grid {gridCoordinate} (world {worldPos})");
            
            return true;
        }
        
        /// <summary>
        /// Remove an object from the grid
        /// </summary>
        public bool RemoveObject(GridPlaceable placeable)
        {
            if (placeable == null || !placeable.IsPlaced)
                return false;
            
            // Free grid cells
            FreeCells(placeable.GridCoordinate, placeable.GridSize);
            
            // Update object state
            placeable.IsPlaced = false;
            
            // Remove from tracking
            _placedObjects.Remove(placeable);
            
            OnObjectRemoved?.Invoke(placeable);
            LogInfo($"Removed {placeable.name} from grid {placeable.GridCoordinate}");
            
            return true;
        }
        
        private void OccupyCells(Vector3Int gridCoordinate, Vector3Int size, GridPlaceable occupyingObject)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int cellCoord = gridCoordinate + new Vector3Int(x, y, z);
                        if (_gridCells.TryGetValue(cellCoord, out GridCell cell))
                        {
                            cell.IsOccupied = true;
                            cell.OccupyingObject = occupyingObject;
                        }
                    }
                }
            }
        }
        
        private void FreeCells(Vector3Int gridCoordinate, Vector3Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int cellCoord = gridCoordinate + new Vector3Int(x, y, z);
                        if (_gridCells.TryGetValue(cellCoord, out GridCell cell))
                        {
                            cell.IsOccupied = false;
                            cell.OccupyingObject = null;
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Visualization
        
        /// <summary>
        /// Update grid visibility
        /// </summary>
        public void SetGridVisibility(bool visible)
        {
            _gridSettings.ShowGrid = visible;
            UpdateGridVisibility();
            OnGridVisibilityChanged?.Invoke(visible);
        }
        
        private void UpdateGridVisibility()
        {
            if (_gridVisualization != null)
            {
                _gridVisualization.SetActive(_gridSettings.ShowGrid);
            }
        }
        
        private void UpdateGridVisualization()
        {
            if (_gridSettings.ShowGrid && _gridVisualization == null)
            {
                CreateGridVisualization();
            }
        }
        
        /// <summary>
        /// Update grid settings
        /// </summary>
        public void UpdateGridSettings(GridSnapSettings newSettings)
        {
            _gridSettings = newSettings;
            _gridNeedsUpdate = true;
            
            // Reinitialize if grid size changed
            if (Mathf.Abs(_gridSettings.GridSize - newSettings.GridSize) > 0.01f)
            {
                InitializeGrid();
                CreateGridVisualization();
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get all objects in a 3D area
        /// </summary>
        public List<GridPlaceable> GetObjectsInArea(Vector3Int gridCoordinate, Vector3Int size)
        {
            var objectsInArea = new List<GridPlaceable>();
            
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int checkCoord = gridCoordinate + new Vector3Int(x, y, z);
                        if (_gridCells.TryGetValue(checkCoord, out GridCell cell) && cell.IsOccupied && cell.OccupyingObject != null)
                        {
                            if (!objectsInArea.Contains(cell.OccupyingObject))
                            {
                                objectsInArea.Add(cell.OccupyingObject);
                            }
                        }
                    }
                }
            }
            
            return objectsInArea;
        }
        
        /// <summary>
        /// Get all objects in a 2D area (backward compatibility)
        /// </summary>
        [System.Obsolete("Use GetObjectsInArea(Vector3Int, Vector3Int) for 3D coordinates")]
        public List<GridPlaceable> GetObjectsInArea2D(Vector2Int gridCoordinate, Vector2Int size)
        {
            return GetObjectsInArea(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0), new Vector3Int(size.x, size.y, 1));
        }
        
        /// <summary>
        /// Get nearest valid 3D placement position
        /// </summary>
        public Vector3Int GetNearestValidPosition(Vector3Int preferredPosition, Vector3Int objectSize)
        {
            if (IsAreaAvailable(preferredPosition, objectSize))
                return preferredPosition;
            
            // Search in expanding 3D spheres
            for (int radius = 1; radius <= 10; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        for (int z = -radius; z <= radius; z++)
                        {
                            Vector3Int testPos = preferredPosition + new Vector3Int(x, y, z);
                            if (IsAreaAvailable(testPos, objectSize))
                            {
                                return testPos;
                            }
                        }
                    }
                }
            }
            
            return preferredPosition; // Return original if no valid position found
        }
        
        /// <summary>
        /// Get nearest valid 2D placement position (backward compatibility)
        /// </summary>
        [System.Obsolete("Use GetNearestValidPosition(Vector3Int, Vector3Int) for 3D coordinates")]
        public Vector2Int GetNearestValidPosition2D(Vector2Int preferredPosition, Vector2Int objectSize)
        {
            Vector3Int result3D = GetNearestValidPosition(new Vector3Int(preferredPosition.x, preferredPosition.y, 0), new Vector3Int(objectSize.x, objectSize.y, 1));
            return new Vector2Int(result3D.x, result3D.y);
        }
        
        /// <summary>
        /// Clear all objects from grid
        /// </summary>
        public void ClearGrid()
        {
            var objectsToRemove = new List<GridPlaceable>(_placedObjects);
            foreach (var obj in objectsToRemove)
            {
                RemoveObject(obj);
            }
        }
        
        #endregion
        
        #region Height Level Management
        
        /// <summary>
        /// Set the visible height level for grid visualization
        /// </summary>
        public void SetVisibleHeightLevel(int heightLevel)
        {
            _currentVisibleHeightLevel = Mathf.Clamp(heightLevel, 0, _maxHeightLevels - 1);
            UpdateGridVisualization();
        }
        
        /// <summary>
        /// Get all cells at a specific height level
        /// </summary>
        public List<GridCell> GetCellsAtHeightLevel(int heightLevel)
        {
            var cellsAtLevel = new List<GridCell>();
            foreach (var kvp in _gridCells)
            {
                if (kvp.Key.z == heightLevel)
                {
                    cellsAtLevel.Add(kvp.Value);
                }
            }
            return cellsAtLevel;
        }
        
        /// <summary>
        /// Check if a height level has any occupied cells
        /// </summary>
        public bool IsHeightLevelOccupied(int heightLevel)
        {
            foreach (var kvp in _gridCells)
            {
                if (kvp.Key.z == heightLevel && kvp.Value.IsOccupied)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Get the world Y position for a height level
        /// </summary>
        public float GetWorldYForHeightLevel(int heightLevel)
        {
            return _gridOrigin.y + heightLevel * _heightLevelSpacing;
        }
        
        /// <summary>
        /// Check if foundation is required at given height level
        /// </summary>
        public bool RequiresFoundation(Vector3Int gridPosition)
        {
            // Objects at ground level (z=0) don't need foundation
            if (gridPosition.z == 0) return false;
            
            // Check if there's support below
            Vector3Int belowPosition = new Vector3Int(gridPosition.x, gridPosition.y, gridPosition.z - 1);
            if (_gridCells.TryGetValue(belowPosition, out GridCell cellBelow))
            {
                return !cellBelow.IsOccupied; // Needs foundation if nothing below
            }
            
            return true; // Needs foundation if no cell below
        }
        
        private void LogInfo(string message)
        {
            ChimeraLogger.Log($"[GridSystem] {message}");
        }
        
        private void LogWarning(string message)
        {
            ChimeraLogger.LogWarning($"[GridSystem] {message}");
        }
        
        #endregion
        
        #region Debug and Gizmos
        
        private void OnDrawGizmos()
        {
            if (!_gridSettings.ShowGrid) return;
            
            Gizmos.color = _gridSettings.GridColor;
            
            // Draw grid bounds
            Vector3 center = _gridOrigin + new Vector3(_gridDimensions.x/2, 0, _gridDimensions.y/2);
            Gizmos.DrawWireCube(center, new Vector3(_gridDimensions.x, 0.1f, _gridDimensions.y));
            
            // Draw occupied cells
            Gizmos.color = Color.red;
            foreach (var cell in _gridCells.Values)
            {
                if (cell.IsOccupied)
                {
                    Gizmos.DrawWireCube(cell.WorldPosition + Vector3.up * 0.5f, Vector3.one * _gridSettings.GridSize * 0.9f);
                }
            }
            }
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class GridCell
    {
        public Vector3Int GridCoordinate;
        public Vector3 WorldPosition;
        public bool IsOccupied;
        public GridPlaceable OccupyingObject;
        public GridCellType CellType;
        public bool IsValid;
        public float MovementCost = 1f;
        public bool RequiresFoundation;
        public int HeightLevel => GridCoordinate.z;
    }
    
    public enum GridCellType
    {
        Standard,
        Blocked,
        Special,
        Reserved
    }
        
        /// <summary>
        /// Get all objects within a specified radius of a world position
        /// </summary>
        public List<GridPlaceable> GetObjectsNearPosition(Vector3 worldPosition, float radius)
    {
        var nearbyObjects = new List<GridPlaceable>();
        var centerGrid = WorldToGridPosition(worldPosition);
        
        // Search in a square pattern around the center position
        int searchRadius = Mathf.CeilToInt(radius / GridSize);
        
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                for (int z = -searchRadius; z <= searchRadius; z++)
                {
                    var checkPos = centerGrid + new Vector3Int(x, y, z);
                    var cell = GetGridCell(checkPos);
                    
                    if (cell != null && cell.IsOccupied && cell.OccupyingObject != null)
                    {
                        Vector3 objectWorldPos = GridToWorldPosition(checkPos);
                        if (Vector3.Distance(worldPosition, objectWorldPos) <= radius)
                        {
                            nearbyObjects.Add(cell.OccupyingObject);
                        }
                    }
                }
            }
        }
        
        return nearbyObjects;
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

    [System.Serializable]
    public struct GridSnapSettings
    {
        public float GridSize;
        public bool SnapToGrid;
        public bool ShowGrid;
        public Color GridColor;
    }
    
    #endregion
}