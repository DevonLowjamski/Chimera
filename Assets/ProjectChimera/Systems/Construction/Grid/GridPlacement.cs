using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Construction.Grid;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Handles object placement and removal operations on the grid.
    /// Manages grid cell occupation, object positioning, and placement validation.
    /// </summary>
    public class GridPlacement
    {
        private readonly Dictionary<Vector3Int, GridTypes.GridCell> _gridCells;
        private readonly GridCalculations _calculations;
        private readonly List<GridPlaceable> _placedObjects;
        private readonly float _placementTolerance;

        // Events
        public System.Action<Vector3Int, Vector3Int> OnAreaOccupied;
        public System.Action<Vector3Int, Vector3Int> OnAreaFreed;
        public System.Action<GridPlaceable> OnObjectPlaced;
        public System.Action<GridPlaceable> OnObjectRemoved;

        public GridPlacement(
            Dictionary<Vector3Int, GridTypes.GridCell> gridCells,
            GridCalculations calculations,
            List<GridPlaceable> placedObjects,
            float placementTolerance = 0.1f)
        {
            _gridCells = gridCells;
            _calculations = calculations;
            _placedObjects = placedObjects;
            _placementTolerance = placementTolerance;
        }

        /// <summary>
        /// Place an object at a specific world position
        /// </summary>
        public bool PlaceObject(GridPlaceable placeable, Vector3 worldPosition)
        {
            Vector3Int gridCoord = _calculations.WorldToGridPosition(worldPosition);
            return PlaceObjectAtGridCoordinate(placeable, gridCoord);
        }

        /// <summary>
        /// Place an object at a specific grid coordinate
        /// </summary>
        public bool PlaceObjectAtGridCoordinate(GridPlaceable placeable, Vector3Int gridCoordinate)
        {
            if (placeable == null)
            {
                ChimeraLogger.Log("OTHER", "PlaceObject: Placeable object is null", null);
                return false;
            }

            // Check if area is available
            if (!IsAreaAvailable(gridCoordinate, placeable.GridSize))
            {
                ChimeraLogger.Log("OTHER", "PlaceObject: Area not available for placement", null);
                return false;
            }

            // Occupy grid cells
            OccupyArea(gridCoordinate, placeable.GridSize, placeable);

            // Set object position and state
            Vector3 worldPos = _calculations.GridToWorldPosition(gridCoordinate);
            placeable.transform.position = worldPos;
            placeable.GridCoordinate = gridCoordinate;
            placeable.IsPlaced = true;

            // Track placed object
            _placedObjects.Add(placeable);

            OnObjectPlaced?.Invoke(placeable);
            ChimeraLogger.Log("OTHER", "PlaceObject: Successfully placed object", null);

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
            FreeArea(placeable.GridCoordinate, placeable.GridSize);

            // Update object state
            placeable.IsPlaced = false;

            // Remove from tracking
            _placedObjects.Remove(placeable);

            OnObjectRemoved?.Invoke(placeable);
            ChimeraLogger.Log("OTHER", "RemoveObject: Successfully removed object", null);

            return true;
        }

        /// <summary>
        /// Check if a grid area is available for placement
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
        /// Occupy a grid area with an object
        /// </summary>
        private void OccupyArea(Vector3Int gridCoordinate, Vector3Int size, GridPlaceable occupyingObject)
        {
            List<Vector3Int> coordinates = _calculations.GetCoordinatesInArea(gridCoordinate, size);

            foreach (Vector3Int coord in coordinates)
            {
                if (_gridCells.TryGetValue(coord, out GridTypes.GridCell cell))
                {
                    cell.IsOccupied = true;
                    cell.OccupyingObject = occupyingObject;
                }
            }

            OnAreaOccupied?.Invoke(gridCoordinate, size);
        }

        /// <summary>
        /// Free a grid area from occupation
        /// </summary>
        private void FreeArea(Vector3Int gridCoordinate, Vector3Int size)
        {
            List<Vector3Int> coordinates = _calculations.GetCoordinatesInArea(gridCoordinate, size);

            foreach (Vector3Int coord in coordinates)
            {
                if (_gridCells.TryGetValue(coord, out GridTypes.GridCell cell))
                {
                    cell.IsOccupied = false;
                    cell.OccupyingObject = null;
                }
            }

            OnAreaFreed?.Invoke(gridCoordinate, size);
        }

        /// <summary>
        /// Move an object to a new grid position
        /// </summary>
        public bool MoveObject(GridPlaceable placeable, Vector3Int newGridCoordinate)
        {
            if (placeable == null || !placeable.IsPlaced)
                return false;

            // Check if new area is available (excluding current position)
            Vector3Int oldCoordinate = placeable.GridCoordinate;
            FreeArea(oldCoordinate, placeable.GridSize); // Temporarily free old area

            bool canPlace = IsAreaAvailable(newGridCoordinate, placeable.GridSize);

            if (canPlace)
            {
                // Move to new position
                OccupyArea(newGridCoordinate, placeable.GridSize, placeable);
                Vector3 worldPos = _calculations.GridToWorldPosition(newGridCoordinate);
                placeable.transform.position = worldPos;
                placeable.GridCoordinate = newGridCoordinate;

                ChimeraLogger.Log("OTHER", "MoveObject: Successfully moved object", null);
                return true;
            }
            else
            {
                // Restore old occupation
                OccupyArea(oldCoordinate, placeable.GridSize, placeable);
                ChimeraLogger.Log("OTHER", "MoveObject: Move failed, restored old position", null);
                return false;
            }
        }

        /// <summary>
        /// Validate placement at a specific position with tolerance
        /// </summary>
        public bool ValidatePlacement(GridPlaceable placeable, Vector3 worldPosition, out Vector3Int validCoordinate)
        {
            validCoordinate = _calculations.WorldToGridPosition(worldPosition);

            if (IsAreaAvailable(validCoordinate, placeable.GridSize))
            {
                return true;
            }

            // Try to find nearby valid position within tolerance
            Vector3 snappedPos = _calculations.SnapToGrid(worldPosition);
            float toleranceSquared = _placementTolerance * _placementTolerance;

            if (Vector3.SqrMagnitude(worldPosition - snappedPos) <= toleranceSquared)
            {
                validCoordinate = _calculations.WorldToGridPosition(snappedPos);
                if (IsAreaAvailable(validCoordinate, placeable.GridSize))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get all objects occupying a specific area
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
        /// Clear all objects from the grid
        /// </summary>
        public void ClearAllObjects()
        {
            var objectsToRemove = new List<GridPlaceable>(_placedObjects);
            foreach (var obj in objectsToRemove)
            {
                RemoveObject(obj);
            }
        }

        /// <summary>
        /// Get placement statistics
        /// </summary>
        public (int totalCells, int occupiedCells, float occupancyRate) GetPlacementStats()
        {
            int totalCells = _gridCells.Count;
            int occupiedCells = _gridCells.Values.Count(cell => cell.IsOccupied);
            float occupancyRate = totalCells > 0 ? (float)occupiedCells / totalCells : 0f;

            return (totalCells, occupiedCells, occupancyRate);
        }

        /// <summary>
        /// Place an object at a grid position (overload for Vector3Int)
        /// </summary>
        public bool PlaceObject(GridPlaceable placeable, Vector3Int gridPosition)
        {
            return PlaceObjectAtGridCoordinate(placeable, gridPosition);
        }

        /// <summary>
        /// Check if a position is occupied
        /// </summary>
        public bool IsPositionOccupied(Vector3Int gridPosition)
        {
            if (_gridCells.TryGetValue(gridPosition, out GridTypes.GridCell cell))
            {
                return cell.IsOccupied;
            }
            return false;
        }

        /// <summary>
        /// Find the nearest valid placement position
        /// </summary>
        public Vector3Int FindNearestValidPosition(Vector3 worldPosition)
        {
            Vector3Int startCoord = _calculations.WorldToGridPosition(worldPosition);

            // Check if the starting position is valid
            if (IsAreaAvailable(startCoord, Vector3Int.one))
            {
                return startCoord;
            }

            // Search in expanding radius
            for (int radius = 1; radius <= 10; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        for (int z = -radius; z <= radius; z++)
                        {
                            Vector3Int testCoord = startCoord + new Vector3Int(x, y, z);
                            if (_calculations.IsValidGridPosition(testCoord) &&
                                IsAreaAvailable(testCoord, Vector3Int.one))
                            {
                                return testCoord;
                            }
                        }
                    }
                }
            }

            return startCoord; // Return original if no valid position found
        }

        /// <summary>
        /// Get objects near a position
        /// </summary>
        public List<GridPlaceable> GetObjectsNearPosition(Vector3 worldPosition, float radius)
        {
            var nearObjects = new List<GridPlaceable>();
            Vector3Int centerCoord = _calculations.WorldToGridPosition(worldPosition);
            int gridRadius = Mathf.CeilToInt(radius);

            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    for (int z = -gridRadius; z <= gridRadius; z++)
                    {
                        Vector3Int testCoord = centerCoord + new Vector3Int(x, y, z);

                        if (_gridCells.TryGetValue(testCoord, out GridTypes.GridCell cell) &&
                            cell.IsOccupied &&
                            cell.OccupyingObject != null)
                        {
                            Vector3 objWorldPos = _calculations.GridToWorldPosition(testCoord);
                            if (Vector3.Distance(worldPosition, objWorldPos) <= radius &&
                                !nearObjects.Contains(cell.OccupyingObject))
                            {
                                nearObjects.Add(cell.OccupyingObject);
                            }
                        }
                    }
                }
            }

            return nearObjects;
        }

        /// <summary>
        /// Get count of placed objects
        /// </summary>
        public int PlacedObjectCount => _placedObjects.Count;

        /// <summary>
        /// Get grid cells dictionary
        /// </summary>
        public Dictionary<Vector3Int, GridTypes.GridCell> GridCells => _gridCells;
    }
}
