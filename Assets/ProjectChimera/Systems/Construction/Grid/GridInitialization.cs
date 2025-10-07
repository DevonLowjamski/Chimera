using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Construction.Grid;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Handles grid initialization, setup, and configuration.
    /// Manages the creation and initialization of grid cells and system components.
    /// </summary>
    public class GridInitialization
    {
        private readonly Dictionary<Vector3Int, GridTypes.GridCell> _gridCells;
        private readonly GridTypes.GridBounds _bounds;
        private readonly GridTypes.GridSnapSettings _settings;

        // Initialization state
        private bool _isInitialized = false;
        private System.Action _onInitializationComplete;

        public GridInitialization(
            Dictionary<Vector3Int, GridTypes.GridCell> gridCells,
            GridTypes.GridBounds bounds,
            GridTypes.GridSnapSettings settings)
        {
            _gridCells = gridCells;
            _bounds = bounds;
            _settings = settings;
        }

        /// <summary>
        /// Initialize the entire grid system
        /// </summary>
        public void Initialize(System.Action onComplete = null)
        {
            if (_isInitialized)
            {
                ChimeraLogger.Log("OTHER", "Grid initialization operation", null);
                onComplete?.Invoke();
                return;
            }

            _onInitializationComplete = onComplete;

            ChimeraLogger.Log("OTHER", "Grid initialization operation", null);

            // Clear any existing data
            _gridCells.Clear();

            // Create grid cells
            CreateGridCells();

            // Initialize special areas (if any)
            InitializeSpecialAreas();

            // Validate initialization
            ValidateInitialization();

            _isInitialized = true;
            ChimeraLogger.Log("OTHER", "Grid initialization operation", null);

            _onInitializationComplete?.Invoke();
        }

        /// <summary>
        /// Create all grid cells based on bounds and settings
        /// </summary>
        private void CreateGridCells()
        {
            Vector3Int maxCoords = GetMaxGridCoordinates();

            for (int x = 0; x < maxCoords.x; x++)
            {
                for (int y = 0; y < maxCoords.y; y++)
                {
                    for (int z = 0; z < maxCoords.z; z++)
                    {
                        Vector3Int gridCoord = new Vector3Int(x, y, z);
                        Vector3 worldPos = GridToWorldPosition(gridCoord);

                        _gridCells[gridCoord] = new GridTypes.GridCell(gridCoord, worldPos)
                        {
                            CellType = DetermineCellType(gridCoord),
                            IsValid = IsCellValid(gridCoord)
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Determine the type of cell at a given coordinate
        /// </summary>
        private GridTypes.GridCellType DetermineCellType(Vector3Int gridCoord)
        {
            // Example: Mark edges as blocked for boundary
            Vector3Int maxCoords = GetMaxGridCoordinates();

            if (gridCoord.x == 0 || gridCoord.x == maxCoords.x - 1 ||
                gridCoord.y == 0 || gridCoord.y == maxCoords.y - 1)
            {
                return GridTypes.GridCellType.Blocked; // Boundary cells
            }

            // Example: Mark some cells as special (could be based on game rules)
            if ((gridCoord.x + gridCoord.y + gridCoord.z) % 10 == 0)
            {
                return GridTypes.GridCellType.Special;
            }

            return GridTypes.GridCellType.Standard;
        }

        /// <summary>
        /// Check if a cell is valid for placement
        /// </summary>
        private bool IsCellValid(Vector3Int gridCoord)
        {
            // Basic bounds check
            Vector3Int maxCoords = GetMaxGridCoordinates();
            if (gridCoord.x < 0 || gridCoord.x >= maxCoords.x ||
                gridCoord.y < 0 || gridCoord.y >= maxCoords.y ||
                gridCoord.z < 0 || gridCoord.z >= maxCoords.z)
            {
                return false;
            }

            // Additional validation could be added here
            // (e.g., terrain height checks, obstacle detection, etc.)

            return true;
        }

        /// <summary>
        /// Initialize special areas or zones
        /// </summary>
        private void InitializeSpecialAreas()
        {
            // Example: Create a special construction zone
            Vector3Int center = GetMaxGridCoordinates() / 2;
            int zoneSize = 5;

            for (int x = center.x - zoneSize; x <= center.x + zoneSize; x++)
            {
                for (int y = center.y - zoneSize; y <= center.y + zoneSize; y++)
                {
                    for (int z = 0; z < 3; z++) // First 3 levels
                    {
                        Vector3Int coord = new Vector3Int(x, y, z);
                        if (_gridCells.TryGetValue(coord, out GridTypes.GridCell cell))
                        {
                            cell.CellType = GridTypes.GridCellType.Special;
                        }
                    }
                }
            }

            ChimeraLogger.Log("OTHER", "Grid initialization operation", null);
        }

        /// <summary>
        /// Validate that initialization was successful
        /// </summary>
        private void ValidateInitialization()
        {
            int expectedCellCount = GetMaxGridCoordinates().x * GetMaxGridCoordinates().y * GetMaxGridCoordinates().z;

            if (_gridCells.Count != expectedCellCount)
            {
                ChimeraLogger.Log("OTHER", "Grid initialization operation", null);
            }

            // Validate that all cells are properly initialized
            int invalidCells = 0;
            foreach (var kvp in _gridCells)
            {
                if (!kvp.Value.IsValid)
                    invalidCells++;
            }

            if (invalidCells > 0)
            {
                ChimeraLogger.Log("OTHER", "Grid initialization operation", null);
            }
        }

        /// <summary>
        /// Reinitialize grid with new bounds
        /// </summary>
        public void Reinitialize(GridTypes.GridBounds newBounds)
        {
            ChimeraLogger.Log("OTHER", "Grid initialization operation", null);

            // Update bounds reference (assuming it's a reference type)
            // In practice, this would need to be handled by the main GridSystem

            _isInitialized = false;
            Initialize();
        }

        /// <summary>
        /// Reset grid to initial state
        /// </summary>
        public void Reset()
        {
            ChimeraLogger.Log("OTHER", "Grid initialization operation", null);

            _gridCells.Clear();
            _isInitialized = false;

            Initialize();
        }

        /// <summary>
        /// Get the maximum grid coordinates
        /// </summary>
        private Vector3Int GetMaxGridCoordinates()
        {
            return new Vector3Int(
                Mathf.RoundToInt(_bounds.Dimensions.x / _settings.GridSize),
                Mathf.RoundToInt(_bounds.Dimensions.y / _settings.GridSize),
                Mathf.RoundToInt(_bounds.Dimensions.z / _bounds.HeightLevelSpacing)
            );
        }

        /// <summary>
        /// Convert grid coordinate to world position
        /// </summary>
        private Vector3 GridToWorldPosition(Vector3Int gridCoordinate)
        {
            float worldX = _bounds.Origin.x + gridCoordinate.x * _settings.GridSize;
            float worldZ = _bounds.Origin.z + gridCoordinate.y * _settings.GridSize;
            float worldY = _bounds.Origin.y + gridCoordinate.z * _bounds.HeightLevelSpacing;

            return new Vector3(worldX, worldY, worldZ);
        }

        /// <summary>
        /// Check if the grid is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Get initialization statistics
        /// </summary>
        public (int totalCells, int standardCells, int blockedCells, int specialCells, int invalidCells) GetInitializationStats()
        {
            int standardCells = 0, blockedCells = 0, specialCells = 0, invalidCells = 0;

            foreach (var cell in _gridCells.Values)
            {
                switch (cell.CellType)
                {
                    case GridTypes.GridCellType.Standard:
                        standardCells++;
                        break;
                    case GridTypes.GridCellType.Blocked:
                        blockedCells++;
                        break;
                    case GridTypes.GridCellType.Special:
                        specialCells++;
                        break;
                }

                if (!cell.IsValid)
                    invalidCells++;
            }

            return (_gridCells.Count, standardCells, blockedCells, specialCells, invalidCells);
        }
    }
}
