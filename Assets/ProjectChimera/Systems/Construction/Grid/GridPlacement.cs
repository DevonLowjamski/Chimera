using UnityEngine;
using System.Collections.Generic;
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
                ChimeraLogger.LogWarning("[GridPlacement] Cannot place null object");
                return false;
            }

            // Check if area is available
            if (!IsAreaAvailable(gridCoordinate, placeable.GridSize))
            {
                ChimeraLogger.LogWarning($"[GridPlacement] Cannot place {placeable.name} - area not available at {gridCoordinate}");
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
            ChimeraLogger.Log($"[GridPlacement] Placed {placeable.name} at grid {gridCoordinate} (world {worldPos})");

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
            ChimeraLogger.Log($"[GridPlacement] Removed {placeable.name} from grid {placeable.GridCoordinate}");

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

                ChimeraLogger.Log($"[GridPlacement] Moved {placeable.name} from {oldCoordinate} to {newGridCoordinate}");
                return true;
            }
            else
            {
                // Restore old occupation
                OccupyArea(oldCoordinate, placeable.GridSize, placeable);
                ChimeraLogger.LogWarning($"[GridPlacement] Cannot move {placeable.name} to {newGridCoordinate} - area not available");
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
    }
}
