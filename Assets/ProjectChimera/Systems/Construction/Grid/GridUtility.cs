using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Construction.Grid;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Utility functions for grid operations, queries, and analysis.
    /// Provides methods for area searches, position finding, and grid statistics.
    /// </summary>
    public class GridUtility
    {
        private readonly Dictionary<Vector3Int, GridTypes.GridCell> _gridCells;
        private readonly GridCalculations _calculations;
        private readonly List<GridPlaceable> _placedObjects;

        public GridUtility(
            Dictionary<Vector3Int, GridTypes.GridCell> gridCells,
            GridCalculations calculations,
            List<GridPlaceable> placedObjects)
        {
            _gridCells = gridCells;
            _calculations = calculations;
            _placedObjects = placedObjects;
        }

        /// <summary>
        /// Get all objects within a 3D area
        /// </summary>
        public List<GridPlaceable> GetObjectsInArea(Vector3Int gridCoordinate, Vector3Int size)
        {
            var objectsInArea = new List<GridPlaceable>();
            List<Vector3Int> coordinates = _calculations.GetCoordinatesInArea(gridCoordinate, size);

            foreach (Vector3Int coord in coordinates)
            {
                if (_gridCells.TryGetValue(coord, out GridTypes.GridCell cell) &&
                    cell.IsOccupied &&
                    cell.OccupyingObject != null &&
                    !objectsInArea.Contains(cell.OccupyingObject))
                {
                    objectsInArea.Add(cell.OccupyingObject);
                }
            }

            return objectsInArea;
        }

        /// <summary>
        /// Get all objects within a 2D area (backward compatibility)
        /// </summary>
        [System.Obsolete("Use GetObjectsInArea(Vector3Int, Vector3Int) for 3D coordinates")]
        public List<GridPlaceable> GetObjectsInArea2D(Vector2Int gridCoordinate, Vector2Int size)
        {
            return GetObjectsInArea(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0),
                                   new Vector3Int(size.x, size.y, 1));
        }

        /// <summary>
        /// Get all objects within a radius of a world position
        /// </summary>
        public List<GridPlaceable> GetObjectsNearPosition(Vector3 worldPosition, float radius)
        {
            var nearbyObjects = new List<GridPlaceable>();
            Vector3Int centerGrid = _calculations.WorldToGridPosition(worldPosition);

            // Search in a 3D cube pattern around the center
            int searchRadius = Mathf.CeilToInt(radius / _calculations.GridSize);

            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    for (int z = -searchRadius; z <= searchRadius; z++)
                    {
                        Vector3Int checkPos = centerGrid + new Vector3Int(x, y, z);

                        if (_gridCells.TryGetValue(checkPos, out GridTypes.GridCell cell) &&
                            cell.IsOccupied &&
                            cell.OccupyingObject != null)
                        {
                            Vector3 objectWorldPos = _calculations.GridToWorldPosition(checkPos);
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

        /// <summary>
        /// Find the nearest valid placement position for an object
        /// </summary>
        public Vector3Int FindNearestValidPosition(Vector3Int preferredPosition, Vector3Int objectSize)
        {
            // Check preferred position first
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

            // Return original position if no valid position found
            return preferredPosition;
        }

        /// <summary>
        /// Find the nearest valid 2D placement position (backward compatibility)
        /// </summary>
        [System.Obsolete("Use FindNearestValidPosition(Vector3Int, Vector3Int) for 3D coordinates")]
        public Vector2Int FindNearestValidPosition2D(Vector2Int preferredPosition, Vector2Int objectSize)
        {
            Vector3Int result3D = FindNearestValidPosition(
                new Vector3Int(preferredPosition.x, preferredPosition.y, 0),
                new Vector3Int(objectSize.x, objectSize.y, 1));
            return new Vector2Int(result3D.x, result3D.y);
        }

        /// <summary>
        /// Check if an area is available for placement
        /// </summary>
        public bool IsAreaAvailable(Vector3Int gridCoordinate, Vector3Int size)
        {
            if (!_calculations.IsAreaWithinBounds(gridCoordinate, size))
                return false;

            List<Vector3Int> coordinates = _calculations.GetCoordinatesInArea(gridCoordinate, size);

            foreach (Vector3Int coord in coordinates)
            {
                if (_gridCells.TryGetValue(coord, out GridTypes.GridCell cell))
                {
                    if (cell.IsOccupied || !cell.IsValid || !GridTypes.GridTypeUtils.AllowsPlacement(cell.CellType))
                    {
                        return false;
                    }
                }
                else
                {
                    return false; // Cell doesn't exist
                }
            }

            return true;
        }

        /// <summary>
        /// Get cells at a specific height level
        /// </summary>
        public List<GridTypes.GridCell> GetCellsAtHeightLevel(int heightLevel)
        {
            var cellsAtLevel = new List<GridTypes.GridCell>();
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
        /// Get occupancy statistics for the grid
        /// </summary>
        public (int totalCells, int occupiedCells, int freeCells, float occupancyRate) GetOccupancyStats()
        {
            int totalCells = _gridCells.Count;
            int occupiedCells = _gridCells.Values.Count(cell => cell.IsOccupied);
            int freeCells = totalCells - occupiedCells;
            float occupancyRate = totalCells > 0 ? (float)occupiedCells / totalCells : 0f;

            return (totalCells, occupiedCells, freeCells, occupancyRate);
        }

        /// <summary>
        /// Get statistics by cell type
        /// </summary>
        public Dictionary<GridTypes.GridCellType, int> GetCellTypeStats()
        {
            var stats = new Dictionary<GridTypes.GridCellType, int>();
            foreach (var cell in _gridCells.Values)
            {
                if (!stats.ContainsKey(cell.CellType))
                {
                    stats[cell.CellType] = 0;
                }
                stats[cell.CellType]++;
            }
            return stats;
        }

        /// <summary>
        /// Find all connected occupied areas (for structure analysis)
        /// </summary>
        public List<List<Vector3Int>> FindConnectedOccupiedAreas()
        {
            var visited = new HashSet<Vector3Int>();
            var connectedAreas = new List<List<Vector3Int>>();

            foreach (var kvp in _gridCells)
            {
                if (kvp.Value.IsOccupied && !visited.Contains(kvp.Key))
                {
                    var area = new List<Vector3Int>();
                    FloodFillOccupied(kvp.Key, visited, area);
                    if (area.Count > 0)
                    {
                        connectedAreas.Add(area);
                    }
                }
            }

            return connectedAreas;
        }

        /// <summary>
        /// Flood fill to find connected occupied cells
        /// </summary>
        private void FloodFillOccupied(Vector3Int start, HashSet<Vector3Int> visited, List<Vector3Int> area)
        {
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                area.Add(current);

                // Check all 6 neighbors
                var neighbors = _calculations.GetNeighbors(current);
                foreach (Vector3Int neighbor in neighbors)
                {
                    if (_gridCells.TryGetValue(neighbor, out GridTypes.GridCell cell) &&
                        cell.IsOccupied &&
                        !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// Get the bounding box of occupied cells
        /// </summary>
        public Bounds GetOccupiedBounds()
        {
            if (_gridCells.Count == 0)
                return new Bounds();

            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;
            bool hasOccupied = false;

            foreach (var kvp in _gridCells)
            {
                if (kvp.Value.IsOccupied)
                {
                    Vector3 worldPos = _calculations.GridToWorldPosition(kvp.Key);
                    min = Vector3.Min(min, worldPos);
                    max = Vector3.Max(max, worldPos);
                    hasOccupied = true;
                }
            }

            if (!hasOccupied)
                return new Bounds();

            return new Bounds((min + max) / 2, max - min);
        }

        /// <summary>
        /// Get grid density map (useful for optimization)
        /// </summary>
        public float[,] GetDensityMap(int resolution = 10)
        {
            Vector3Int maxCoords = _calculations.GetMaxGridCoordinates();
            float[,] density = new float[resolution, resolution];

            int cellsPerBinX = Mathf.CeilToInt((float)maxCoords.x / resolution);
            int cellsPerBinY = Mathf.CeilToInt((float)maxCoords.y / resolution);

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    int occupiedCount = 0;
                    int totalCount = 0;

                    for (int cx = x * cellsPerBinX; cx < (x + 1) * cellsPerBinX && cx < maxCoords.x; cx++)
                    {
                        for (int cy = y * cellsPerBinY; cy < (y + 1) * cellsPerBinY && cy < maxCoords.y; cy++)
                        {
                            for (int cz = 0; cz < maxCoords.z; cz++)
                            {
                                Vector3Int coord = new Vector3Int(cx, cy, cz);
                                if (_gridCells.TryGetValue(coord, out GridTypes.GridCell cell))
                                {
                                    totalCount++;
                                    if (cell.IsOccupied)
                                        occupiedCount++;
                                }
                            }
                        }
                    }

                    density[x, y] = totalCount > 0 ? (float)occupiedCount / totalCount : 0f;
                }
            }

            return density;
        }

        /// <summary>
        /// Clear all objects from the grid
        /// </summary>
        public void ClearGrid()
        {
            var objectsToRemove = new List<GridPlaceable>(_placedObjects);
            foreach (var obj in objectsToRemove)
            {
                // This would need to be called through GridPlacement.RemoveObject()
                ChimeraLogger.Log($"[GridUtility] Would clear {obj.name}");
            }
        }

        /// <summary>
        /// Validate grid integrity and report issues
        /// </summary>
        public List<string> ValidateGridIntegrity()
        {
            var issues = new List<string>();

            // Check for orphaned objects
            foreach (var obj in _placedObjects)
            {
                if (obj == null)
                {
                    issues.Add("Null object found in placed objects list");
                    continue;
                }

                bool foundInGrid = false;
                for (int x = 0; x < obj.GridSize.x && !foundInGrid; x++)
                {
                    for (int y = 0; y < obj.GridSize.y && !foundInGrid; y++)
                    {
                        for (int z = 0; z < obj.GridSize.z && !foundInGrid; z++)
                        {
                            Vector3Int coord = obj.GridCoordinate + new Vector3Int(x, y, z);
                            if (_gridCells.TryGetValue(coord, out GridTypes.GridCell cell) &&
                                cell.OccupyingObject == obj)
                            {
                                foundInGrid = true;
                            }
                        }
                    }
                }

                if (!foundInGrid)
                {
                    issues.Add($"Object {obj.name} not found in expected grid cells");
                }
            }

            // Check for occupied cells without objects
            foreach (var kvp in _gridCells)
            {
                if (kvp.Value.IsOccupied && kvp.Value.OccupyingObject == null)
                {
                    issues.Add($"Cell {kvp.Key} is marked occupied but has no occupying object");
                }
            }

            return issues;
        }
    }
}
