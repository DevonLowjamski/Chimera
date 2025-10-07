using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Interfaces;
using ProjectChimera.Data.Construction;
using ProjectChimera.Systems.Construction;
// GridItem is defined in ProjectChimera.Core.Interfaces (IConstructionServices.cs)
// SchematicSO is defined in ProjectChimera.Data.Construction

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Central grid system for snap-to-grid construction placement in Project Chimera.
    /// Provides grid-based spatial organization, snap-to-grid functionality, and visual grid rendering.
    /// Designed to replace the complex legacy construction system with a simple, robust grid-based approach.
    ///
    /// REFACTORED: Previously 870-line monolithic file with 4 classes/enums
    /// Now: Modular orchestrator coordinating focused subsystems
    /// </summary>
    public class LegacyGridSystem : MonoBehaviour, ITickable
    {
        [Header("Grid Configuration")]
        [SerializeField] private Material _gridMaterial;

        // Modular subsystems
        private GridSettingsManager _settings;
        private GridCalculations _calculations;
        private GridVisualization _visualization;
        private GridPlacement _placement;

        // Grid update state
        private bool _gridNeedsUpdate = true;
        private bool _isInitialized = false;

        // Events
        public System.Action<Vector3, Vector3Int> OnGridPositionChanged;

        #region Initialization

        private void Awake()
        {
            InitializeGrid();
        }

        private void Start()
        {
            UpdateGridVisibility();

            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        /// <summary>
        /// Initialize the grid system and all subsystems
        /// </summary>
        private void InitializeGrid()
        {
            // Initialize settings first
            _settings = new GridSettingsManager();
            _settings.Initialize();

            // Create GridBounds and GridSnapSettings for the constructors
            var gridBounds = new GridTypes.GridBounds
            {
                Origin = _settings.GridOrigin,
                Dimensions = _settings.GridDimensions,
                MaxHeightLevels = 50,
                HeightLevelSpacing = 1f
            };

            var snapSettings = new GridTypes.GridSnapSettings
            {
                GridSize = _settings.GridSettings.GridSize,
                SnapToGrid = _settings.GridSettings.SnapToGrid,
                ShowGrid = _settings.GridSettings.ShowGrid,
                GridColor = _settings.GridSettings.GridColor
            };

            // Initialize subsystems with proper constructor parameters
            _calculations = new GridCalculations(gridBounds, snapSettings);
            _visualization = new GridVisualization(gridBounds, snapSettings, transform);

            // For GridPlacement, we need to create gridCells Dictionary and other required parameters
            var gridCells = new Dictionary<Vector3Int, GridTypes.GridCell>();
            var placeableObjects = new List<GridPlaceable>();
            _placement = new GridPlacement(gridCells, _calculations, placeableObjects, snapSettings.GridSize);

            // Wire up events
            WireEvents();

            _isInitialized = true;
            ChimeraLogger.Log("OTHER", "$1", null);
        }

        /// <summary>
        /// Wire up events between subsystems
        /// </summary>
        private void WireEvents()
        {
            _settings.OnSettingsChanged += OnSettingsChanged;
            _visualization.OnGridVisibilityChanged += OnGridVisibilityChanged;
            _placement.OnObjectPlaced += OnObjectPlacedHandler;
            _placement.OnObjectRemoved += OnObjectRemovedHandler;
        }

        #endregion

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.ConstructionSystem;
        public bool IsTickable => enabled && _calculations != null && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (_gridNeedsUpdate)
            {
                UpdateGrid();
                _gridNeedsUpdate = false;
            }
        }

        #endregion

        #region Grid Operations

        /// <summary>
        /// Update the grid system
        /// </summary>
        private void UpdateGrid()
        {
            // Update calculations with current settings
            _calculations.UpdateParameters(
                _settings.GridOrigin,
                _settings.GridSettings.GridSize,
                _settings.GridSettings.SnapToGrid
            );

            // Update visualization
            _visualization.UpdateGridDimensions(_settings.GridDimensions);
            _visualization.UpdateGridColor(_settings.GridSettings.GridColor);
        }

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Vector3Int WorldToGridPosition(Vector3 worldPosition)
        {
            return _calculations.WorldToGridPosition(worldPosition);
        }

        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector3Int gridPosition)
        {
            return _calculations.GridToWorldPosition(gridPosition);
        }

        /// <summary>
        /// Snap a world position to the nearest grid point
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            return _calculations.SnapToGrid(worldPosition);
        }

        #endregion

        #region Object Placement

        /// <summary>
        /// Place an object at the specified grid position
        /// </summary>
        public bool PlaceObject(GridPlaceable placeable, Vector3Int gridPosition)
        {
            return _placement.PlaceObject(placeable, gridPosition);
        }

        /// <summary>
        /// Remove an object from the grid
        /// </summary>
        public bool RemoveObject(GridPlaceable placeable)
        {
            return _placement.RemoveObject(placeable);
        }

        /// <summary>
        /// Check if a position is occupied
        /// </summary>
        public bool IsPositionOccupied(Vector3Int gridPosition)
        {
            return _placement.IsPositionOccupied(gridPosition);
        }

        /// <summary>
        /// Find the nearest valid placement position
        /// </summary>
        public Vector3Int FindNearestValidPosition(Vector3 worldPosition)
        {
            return _placement.FindNearestValidPosition(worldPosition);
        }

        /// <summary>
        /// Get objects near a position
        /// </summary>
        public List<GridPlaceable> GetObjectsNearPosition(Vector3 worldPosition, float radius)
        {
            return _placement.GetObjectsNearPosition(worldPosition, radius);
        }

        /// <summary>
        /// Clear all objects from the grid
        /// </summary>
        public void ClearAllObjects()
        {
            _placement.ClearAllObjects();
        }

        #endregion

        #region Visualization

        /// <summary>
        /// Update grid visibility
        /// </summary>
        public void UpdateGridVisibility()
        {
            _visualization.UpdateGridVisibility();
        }

        /// <summary>
        /// Set visible height level
        /// </summary>
        public void SetVisibleHeightLevel(int level)
        {
            _visualization.SetVisibleHeightLevel(level);
        }

        #endregion

        #region Settings

        /// <summary>
        /// Save current settings
        /// </summary>
        public void SaveSettings()
        {
            _settings.SaveSettings();
        }

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            _settings.ResetToDefaults();
        }

        /// <summary>
        /// Update grid snap settings
        /// </summary>
        public void UpdateGridSnapSettings(GridTypes.GridSnapSettings settings)
        {
            _settings.UpdateGridSnapSettings(settings);
        }

        #endregion

        #region Event Handlers

        private void OnSettingsChanged()
        {
            _gridNeedsUpdate = true;
            ChimeraLogger.Log("OTHER", "$1", null);
        }

        private void OnGridVisibilityChanged(bool visible)
        {
            ChimeraLogger.Log("OTHER", "$1", null);
        }

        private void OnObjectPlacedHandler(GridPlaceable placeable)
        {
            // Forward placement events
            if (placeable != null)
            {
                Vector3 worldPos = GridToWorldPosition(placeable.GridPosition);
                OnGridPositionChanged?.Invoke(worldPos, placeable.GridPosition);
            }
        }

        private void OnObjectRemovedHandler(GridPlaceable placeable)
        {
            ChimeraLogger.Log("OTHER", "$1", null);
        }

        #endregion

        #region Properties

        public GridTypes.GridSnapSettings GridSettings => _settings.GridSettings;
        public Vector3 GridOrigin => _settings.GridOrigin;
        public Vector3 GridDimensions => _settings.GridDimensions;
        public float GridCellSize => _settings.GridSettings.GridSize;
        public bool SnapEnabled => _settings.GridSettings.SnapToGrid;
        public bool GridVisible => _settings.GridSettings.ShowGrid;
        public int PlacedObjectCount => _placement.PlacedObjectCount;
        public Dictionary<Vector3Int, GridTypes.GridCell> GridCells => _placement.GridCells;
        public int MaxHeightLevels => _settings.MaxHeightLevels;
        public float HeightLevelSpacing => _settings.HeightLevelSpacing;
        public int CurrentVisibleHeightLevel => _visualization.CurrentVisibleHeightLevel;

        #endregion

        #region IGridSystem Implementation

        /// <summary>
        /// Initialize the grid system (IGridSystem interface implementation)
        /// </summary>
        public void Initialize()
        {
            if (!_isInitialized)
            {
                InitializeGrid();
            }
        }

        /// <summary>
        /// Check if a position is valid for placement
        /// </summary>
        public bool IsValidPosition(Vector3Int position)
        {
            return _calculations.IsValidGridPosition(position);
        }

        /// <summary>
        /// Check if a position is occupied
        /// </summary>
        public bool IsOccupied(Vector3Int position)
        {
            return _placement.IsPositionOccupied(position);
        }

        /// <summary>
        /// Check if schematic can be placed at position
        /// </summary>
        public bool CanPlace(ProjectChimera.Data.Construction.SchematicSO schematic, Vector3Int position)
        {
            if (schematic == null) return false;
            return _placement.IsAreaAvailable(position, schematic.Size);
        }

        /// <summary>
        /// Set position as occupied by item
        /// </summary>
        public void SetOccupied(Vector3Int position, GridItem item)
        {
            // This would need additional implementation with the GridItem system
            ChimeraLogger.Log("GRID", "SetOccupied called - requires GridItem integration", null);
        }

        /// <summary>
        /// Set position as empty
        /// </summary>
        public void SetEmpty(Vector3Int position)
        {
            // This would need additional implementation with the GridItem system
            ChimeraLogger.Log("GRID", "SetEmpty called - requires GridItem integration", null);
        }

        /// <summary>
        /// Get item at position
        /// </summary>
        public GridItem GetItemAt(Vector3Int position)
        {
            // This would need additional implementation with the GridItem system
            return null;
        }

        /// <summary>
        /// Convert grid position to world position
        /// </summary>
        public Vector3 GridToWorld(Vector3Int gridPosition)
        {
            return GridToWorldPosition(gridPosition);
        }

        /// <summary>
        /// Convert world position to grid position
        /// </summary>
        public Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            return WorldToGridPosition(worldPosition);
        }

        /// <summary>
        /// Get grid size (for compatibility)
        /// </summary>
        public Vector3Int GridSize => new Vector3Int(
            Mathf.RoundToInt(_settings.GridDimensions.x / _settings.GridSettings.GridSize),
            Mathf.RoundToInt(_settings.GridDimensions.y / _settings.GridSettings.GridSize),
            _settings.MaxHeightLevels
        );

        /// <summary>
        /// Check if the grid system is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }

            // Cleanup subsystems
            _visualization?.Cleanup();
            _placement?.ClearAllObjects();
        }

        #endregion
    }
}
