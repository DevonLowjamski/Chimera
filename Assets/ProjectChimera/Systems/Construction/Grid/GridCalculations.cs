using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Construction.Grid;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Handles all coordinate conversions and grid calculations for the GridSystem.
    /// Provides methods for world-to-grid and grid-to-world transformations,
    /// grid validation, and spatial calculations.
    /// </summary>
    public class GridCalculations
    {
        private readonly GridTypes.GridBounds _bounds;
        private readonly GridTypes.GridSnapSettings _settings;

        public GridCalculations(GridTypes.GridBounds bounds, GridTypes.GridSnapSettings settings)
        {
            _bounds = bounds;
            _settings = settings;
        }

        /// <summary>
        /// Snap world position to nearest grid point
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            if (!_settings.SnapToGrid)
                return worldPosition;

            Vector3 localPos = worldPosition - _bounds.Origin;

            float snappedX = Mathf.Round(localPos.x / _settings.GridSize) * _settings.GridSize;
            float snappedZ = Mathf.Round(localPos.z / _settings.GridSize) * _settings.GridSize;

            return _bounds.Origin + new Vector3(snappedX, worldPosition.y, snappedZ);
        }

        /// <summary>
        /// Convert world position to 3D grid coordinate
        /// </summary>
        public Vector3Int WorldToGridPosition(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - _bounds.Origin;

            int gridX = Mathf.RoundToInt(localPos.x / _settings.GridSize);
            int gridY = Mathf.RoundToInt(localPos.z / _settings.GridSize);
            int gridZ = Mathf.RoundToInt(localPos.y / _bounds.HeightLevelSpacing);

            return new Vector3Int(gridX, gridY, gridZ);
        }

        /// <summary>
        /// Convert 3D grid coordinate to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector3Int gridCoordinate)
        {
            float worldX = _bounds.Origin.x + gridCoordinate.x * _settings.GridSize;
            float worldZ = _bounds.Origin.z + gridCoordinate.y * _settings.GridSize;
            float worldY = _bounds.Origin.y + gridCoordinate.z * _bounds.HeightLevelSpacing;

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
        /// Check if 3D grid coordinate is within valid bounds
        /// </summary>
        public bool IsValidGridPosition(Vector3Int gridCoordinate)
        {
            int maxX = Mathf.RoundToInt(_bounds.Dimensions.x / _settings.GridSize);
            int maxY = Mathf.RoundToInt(_bounds.Dimensions.y / _settings.GridSize);
            int maxZ = Mathf.RoundToInt(_bounds.Dimensions.z / _bounds.HeightLevelSpacing);

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
        /// Get the maximum grid coordinates for the current bounds
        /// </summary>
        public Vector3Int GetMaxGridCoordinates()
        {
            return new Vector3Int(
                Mathf.RoundToInt(_bounds.Dimensions.x / _settings.GridSize),
                Mathf.RoundToInt(_bounds.Dimensions.y / _settings.GridSize),
                Mathf.RoundToInt(_bounds.Dimensions.z / _bounds.HeightLevelSpacing)
            );
        }

        /// <summary>
        /// Get the world Y position for a specific height level
        /// </summary>
        public float GetWorldYForHeightLevel(int heightLevel)
        {
            return _bounds.Origin.y + heightLevel * _bounds.HeightLevelSpacing;
        }

        /// <summary>
        /// Check if a 3D area is within valid bounds
        /// </summary>
        public bool IsAreaWithinBounds(Vector3Int gridCoordinate, Vector3Int size)
        {
            Vector3Int maxCoord = gridCoordinate + size - Vector3Int.one;
            return IsValidGridPosition(gridCoordinate) && IsValidGridPosition(maxCoord);
        }

        /// <summary>
        /// Get all grid coordinates within a 3D area
        /// </summary>
        public List<Vector3Int> GetCoordinatesInArea(Vector3Int gridCoordinate, Vector3Int size)
        {
            var coordinates = new List<Vector3Int>();

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        coordinates.Add(gridCoordinate + new Vector3Int(x, y, z));
                    }
                }
            }

            return coordinates;
        }

        /// <summary>
        /// Calculate distance between two grid coordinates
        /// </summary>
        public float GetGridDistance(Vector3Int coordA, Vector3Int coordB)
        {
            return Vector3Int.Distance(coordA, coordB);
        }

        /// <summary>
        /// Calculate Manhattan distance between two grid coordinates
        /// </summary>
        public int GetManhattanDistance(Vector3Int coordA, Vector3Int coordB)
        {
            return Mathf.Abs(coordA.x - coordB.x) +
                   Mathf.Abs(coordA.y - coordB.y) +
                   Mathf.Abs(coordA.z - coordB.z);
        }

        /// <summary>
        /// Get neighboring coordinates (6-way for 3D grid)
        /// </summary>
        public List<Vector3Int> GetNeighbors(Vector3Int gridCoordinate)
        {
            var neighbors = new List<Vector3Int>();

            // 6-way neighbors in 3D
            Vector3Int[] directions = {
                Vector3Int.right, Vector3Int.left,
                Vector3Int.forward, Vector3Int.back,
                Vector3Int.up, Vector3Int.down
            };

            foreach (var direction in directions)
            {
                Vector3Int neighbor = gridCoordinate + direction;
                if (IsValidGridPosition(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Check if coordinates are adjacent (including diagonally in 2D)
        /// </summary>
        public bool AreAdjacent(Vector3Int coordA, Vector3Int coordB)
        {
            Vector3Int diff = coordA - coordB;
            return Mathf.Abs(diff.x) <= 1 &&
                   Mathf.Abs(diff.y) <= 1 &&
                   Mathf.Abs(diff.z) <= 1 &&
                   diff != Vector3Int.zero;
        }

        /// <summary>
        /// Clamp grid coordinate to valid bounds
        /// </summary>
        public Vector3Int ClampToBounds(Vector3Int gridCoordinate)
        {
            Vector3Int maxCoord = GetMaxGridCoordinates();

            return new Vector3Int(
                Mathf.Clamp(gridCoordinate.x, 0, maxCoord.x - 1),
                Mathf.Clamp(gridCoordinate.y, 0, maxCoord.y - 1),
                Mathf.Clamp(gridCoordinate.z, 0, maxCoord.z - 1)
            );
        }

        /// <summary>
        /// Get the center position of the entire grid
        /// </summary>
        public Vector3 GetGridCenter()
        {
            return _bounds.Origin + _bounds.Dimensions / 2f;
        }

        /// <summary>
        /// Convert a world bounds to grid coordinate bounds
        /// </summary>
        public Bounds GetGridBounds()
        {
            return new Bounds(GetGridCenter(), _bounds.Dimensions);
        }

        /// <summary>
        /// Update grid parameters (for dynamic grid updates)
        /// </summary>
        public void UpdateParameters(Vector3 origin, float gridSize, bool snapToGrid)
        {
            // Note: Since _bounds and _settings are readonly, we would need to recreate the class
            // For now, this method serves as a compatibility bridge
            ChimeraLogger.Log("GRID", "Grid parameters update requested - requires grid system recreation", null);
        }
    }
}
