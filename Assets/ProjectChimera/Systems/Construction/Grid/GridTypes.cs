using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Core data structures and types for the GridSystem.
    /// Contains all grid-related data classes, enums, and settings structures.
    /// </summary>
    public static class GridTypes
    {
        /// <summary>
        /// Represents a single cell in the 3D grid system.
        /// Contains spatial information, occupancy state, and cell properties.
        /// </summary>
        [System.Serializable]
        public class GridCell
        {
            /// <summary>The 3D grid coordinate of this cell</summary>
            public Vector3Int GridCoordinate;

            /// <summary>The world position of this cell's center</summary>
            public Vector3 WorldPosition;

            /// <summary>Whether this cell is occupied by an object</summary>
            public bool IsOccupied;

            /// <summary>The object currently occupying this cell</summary>
            public GridPlaceable OccupyingObject;

            /// <summary>The type/classification of this grid cell</summary>
            public GridCellType CellType;

            /// <summary>Whether this cell is valid for placement</summary>
            public bool IsValid;

            /// <summary>Movement cost multiplier for pathfinding</summary>
            public float MovementCost = 1f;

            /// <summary>Whether this cell requires a foundation for stability</summary>
            public bool RequiresFoundation;

            /// <summary>The height level (Z-coordinate) of this cell</summary>
            public int HeightLevel => GridCoordinate.z;

            /// <summary>
            /// Create a new grid cell with default values
            /// </summary>
            public GridCell()
            {
                CellType = GridCellType.Standard;
                IsValid = true;
                MovementCost = 1f;
            }

            /// <summary>
            /// Create a new grid cell at specified coordinates
            /// </summary>
            public GridCell(Vector3Int coordinate, Vector3 worldPos)
            {
                GridCoordinate = coordinate;
                WorldPosition = worldPos;
                CellType = GridCellType.Standard;
                IsValid = true;
                MovementCost = 1f;
            }
        }

        /// <summary>
        /// Defines different types of grid cells for specialized behavior
        /// </summary>
        public enum GridCellType
        {
            /// <summary>Standard grid cell with no special properties</summary>
            Standard,

            /// <summary>Cell that cannot be occupied (obstacles, boundaries)</summary>
            Blocked,

            /// <summary>Cell with special properties or bonuses</summary>
            Special,

            /// <summary>Cell reserved for future use or system objects</summary>
            Reserved
        }

        /// <summary>
        /// Configuration settings for grid snap behavior and visualization
        /// </summary>
        [System.Serializable]
        public struct GridSnapSettings
        {
            /// <summary>Size of each grid cell in world units</summary>
            public float GridSize;

            /// <summary>Whether objects should snap to grid positions</summary>
            public bool SnapToGrid;

            /// <summary>Whether the grid should be visually displayed</summary>
            public bool ShowGrid;

            /// <summary>Color used for grid lines and visualization</summary>
            public Color GridColor;

            /// <summary>
            /// Create default grid settings
            /// </summary>
            public static GridSnapSettings Default()
            {
                return new GridSnapSettings
                {
                    GridSize = 1.0f,
                    SnapToGrid = true,
                    ShowGrid = true,
                    GridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f)
                };
            }
        }

        /// <summary>
        /// Configuration for 3D grid bounds and dimensions
        /// </summary>
        [System.Serializable]
        public struct GridBounds
        {
            /// <summary>Origin point of the grid in world space</summary>
            public Vector3 Origin;

            /// <summary>Dimensions of the grid (width, depth, height)</summary>
            public Vector3 Dimensions;

            /// <summary>Maximum number of vertical levels</summary>
            public int MaxHeightLevels;

            /// <summary>Spacing between vertical levels</summary>
            public float HeightLevelSpacing;

            /// <summary>
            /// Create default grid bounds
            /// </summary>
            public static GridBounds Default()
            {
                return new GridBounds
                {
                    Origin = Vector3.zero,
                    Dimensions = new Vector3(100f, 100f, 50f),
                    MaxHeightLevels = 50,
                    HeightLevelSpacing = 1f
                };
            }
        }

        /// <summary>
        /// Utility methods for grid type operations
        /// </summary>
        public static class GridTypeUtils
        {
            /// <summary>
            /// Check if a cell type allows placement
            /// </summary>
            public static bool AllowsPlacement(GridCellType cellType)
            {
                return cellType == GridCellType.Standard || cellType == GridCellType.Special;
            }

            /// <summary>
            /// Check if a cell type blocks movement/pathfinding
            /// </summary>
            public static bool BlocksMovement(GridCellType cellType)
            {
                return cellType == GridCellType.Blocked;
            }

            /// <summary>
            /// Get the movement cost multiplier for a cell type
            /// </summary>
            public static float GetMovementCost(GridCellType cellType)
            {
                return cellType switch
                {
                    GridCellType.Blocked => float.MaxValue,
                    GridCellType.Special => 0.5f, // Faster movement on special cells
                    _ => 1f
                };
            }
        }
    }
}
