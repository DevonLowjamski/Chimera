using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Construction.Grid;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Manages multi-level grid operations and foundation requirements.
    /// Handles height-based queries, structural integrity, and vertical construction rules.
    /// </summary>
    public class HeightLevelManager
    {
        private readonly Dictionary<Vector3Int, GridTypes.GridCell> _gridCells;
        private readonly GridTypes.GridBounds _bounds;
        private readonly GridCalculations _calculations;

        private int _currentVisibleHeightLevel;

        // Events
        public System.Action<int> OnVisibleHeightLevelChanged;
        public System.Action<Vector3Int, bool> OnFoundationRequirementChanged;

        public HeightLevelManager(
            Dictionary<Vector3Int, GridTypes.GridCell> gridCells,
            GridTypes.GridBounds bounds,
            GridCalculations calculations)
        {
            _gridCells = gridCells;
            _bounds = bounds;
            _calculations = calculations;
            _currentVisibleHeightLevel = 0;
        }

        /// <summary>
        /// Get the current visible height level
        /// </summary>
        public int CurrentVisibleHeightLevel => _currentVisibleHeightLevel;

        /// <summary>
        /// Set the visible height level for visualization and interaction
        /// </summary>
        public void SetVisibleHeightLevel(int heightLevel)
        {
            int clampedLevel = Mathf.Clamp(heightLevel, 0, _bounds.MaxHeightLevels - 1);
            if (clampedLevel != _currentVisibleHeightLevel)
            {
                _currentVisibleHeightLevel = clampedLevel;
                OnVisibleHeightLevelChanged?.Invoke(_currentVisibleHeightLevel);
                ChimeraLogger.Log($"[HeightLevelManager] Visible height level set to {_currentVisibleHeightLevel}");
            }
        }

        /// <summary>
        /// Get all cells at a specific height level
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
        /// Get all occupied cells at a specific height level
        /// </summary>
        public List<GridTypes.GridCell> GetOccupiedCellsAtHeightLevel(int heightLevel)
        {
            var occupiedCells = new List<GridTypes.GridCell>();
            foreach (var kvp in _gridCells)
            {
                if (kvp.Key.z == heightLevel && kvp.Value.IsOccupied)
                {
                    occupiedCells.Add(kvp.Value);
                }
            }
            return occupiedCells;
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
            return _bounds.Origin.y + heightLevel * _bounds.HeightLevelSpacing;
        }

        /// <summary>
        /// Check if foundation is required at given grid position
        /// </summary>
        public bool RequiresFoundation(Vector3Int gridPosition)
        {
            // Objects at ground level (z=0) don't need foundation
            if (gridPosition.z == 0)
                return false;

            // Check if there's support below
            Vector3Int belowPosition = new Vector3Int(gridPosition.x, gridPosition.y, gridPosition.z - 1);
            if (_gridCells.TryGetValue(belowPosition, out GridTypes.GridCell cellBelow))
            {
                return !cellBelow.IsOccupied; // Needs foundation if nothing below
            }

            return true; // Needs foundation if no cell below
        }

        /// <summary>
        /// Check if a structure has proper foundation support
        /// </summary>
        public bool HasProperFoundation(Vector3Int gridCoordinate, Vector3Int size)
        {
            // Ground level structures don't need foundation
            if (gridCoordinate.z == 0)
                return true;

            List<Vector3Int> coordinates = _calculations.GetCoordinatesInArea(gridCoordinate, size);

            foreach (Vector3Int coord in coordinates)
            {
                if (RequiresFoundation(coord))
                {
                    // Check if foundation exists below this position
                    Vector3Int foundationPos = new Vector3Int(coord.x, coord.y, coord.z - 1);
                    if (_gridCells.TryGetValue(foundationPos, out GridTypes.GridCell foundationCell))
                    {
                        if (!foundationCell.IsOccupied)
                        {
                            return false; // Missing foundation
                        }
                    }
                    else
                    {
                        return false; // No cell below to check
                    }
                }
            }

            return true; // All positions have proper foundation
        }

        /// <summary>
        /// Get all positions that need foundations for a given area
        /// </summary>
        public List<Vector3Int> GetFoundationRequirements(Vector3Int gridCoordinate, Vector3Int size)
        {
            var foundationPositions = new List<Vector3Int>();
            List<Vector3Int> coordinates = _calculations.GetCoordinatesInArea(gridCoordinate, size);

            foreach (Vector3Int coord in coordinates)
            {
                if (RequiresFoundation(coord))
                {
                    foundationPositions.Add(new Vector3Int(coord.x, coord.y, coord.z - 1));
                }
            }

            return foundationPositions;
        }

        /// <summary>
        /// Check structural integrity of a height level
        /// </summary>
        public (bool isStable, List<Vector3Int> unstablePositions) CheckStructuralIntegrity(int heightLevel)
        {
            var unstablePositions = new List<Vector3Int>();
            var cellsAtLevel = GetOccupiedCellsAtHeightLevel(heightLevel);

            foreach (var cell in cellsAtLevel)
            {
                if (!HasSupportBelow(cell.GridCoordinate))
                {
                    unstablePositions.Add(cell.GridCoordinate);
                }
            }

            return (unstablePositions.Count == 0, unstablePositions);
        }

        /// <summary>
        /// Check if a position has support below it
        /// </summary>
        private bool HasSupportBelow(Vector3Int position)
        {
            if (position.z == 0)
                return true; // Ground level always has support

            Vector3Int belowPosition = new Vector3Int(position.x, position.y, position.z - 1);
            if (_gridCells.TryGetValue(belowPosition, out GridTypes.GridCell cellBelow))
            {
                return cellBelow.IsOccupied;
            }

            return false; // No cell below
        }

        /// <summary>
        /// Get the maximum reachable height from ground level
        /// </summary>
        public int GetMaxReachableHeight(Vector3Int startPosition)
        {
            int maxHeight = startPosition.z;

            // Start from the base and work upwards
            for (int height = startPosition.z; height < _bounds.MaxHeightLevels; height++)
            {
                Vector3Int checkPos = new Vector3Int(startPosition.x, startPosition.y, height);

                if (_gridCells.TryGetValue(checkPos, out GridTypes.GridCell cell) && cell.IsOccupied)
                {
                    maxHeight = height;
                }
                else
                {
                    // Check if we can reach this height from below
                    if (!HasSupportBelow(checkPos))
                    {
                        break; // Can't reach this height
                    }
                }
            }

            return maxHeight;
        }

        /// <summary>
        /// Find the highest occupied level in the grid
        /// </summary>
        public int GetHighestOccupiedLevel()
        {
            int highestLevel = 0;

            foreach (var kvp in _gridCells)
            {
                if (kvp.Value.IsOccupied && kvp.Key.z > highestLevel)
                {
                    highestLevel = kvp.Key.z;
                }
            }

            return highestLevel;
        }

        /// <summary>
        /// Get height distribution statistics
        /// </summary>
        public Dictionary<int, int> GetHeightDistribution()
        {
            var distribution = new Dictionary<int, int>();

            for (int level = 0; level < _bounds.MaxHeightLevels; level++)
            {
                int occupiedCount = GetOccupiedCellsAtHeightLevel(level).Count;
                distribution[level] = occupiedCount;
            }

            return distribution;
        }

        /// <summary>
        /// Check if a height level is accessible (has a path from ground level)
        /// </summary>
        public bool IsHeightLevelAccessible(int heightLevel)
        {
            if (heightLevel == 0)
                return true; // Ground level is always accessible

            // Check if there's at least one occupied cell below that can support access
            for (int belowLevel = heightLevel - 1; belowLevel >= 0; belowLevel--)
            {
                if (IsHeightLevelOccupied(belowLevel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get all accessible height levels
        /// </summary>
        public List<int> GetAccessibleHeightLevels()
        {
            var accessibleLevels = new List<int>();

            for (int level = 0; level < _bounds.MaxHeightLevels; level++)
            {
                if (IsHeightLevelAccessible(level))
                {
                    accessibleLevels.Add(level);
                }
            }

            return accessibleLevels;
        }

        /// <summary>
        /// Calculate foundation cost for a given area
        /// </summary>
        public int CalculateFoundationCost(Vector3Int gridCoordinate, Vector3Int size)
        {
            List<Vector3Int> foundationPositions = GetFoundationRequirements(gridCoordinate, size);
            return foundationPositions.Count; // Assuming 1 cost per foundation block
        }

        /// <summary>
        /// Validate height-based placement rules
        /// </summary>
        public (bool isValid, string errorMessage) ValidateHeightPlacement(Vector3Int gridCoordinate, Vector3Int size)
        {
            // Check if height level is accessible
            if (!IsHeightLevelAccessible(gridCoordinate.z))
            {
                return (false, $"Height level {gridCoordinate.z} is not accessible. Build supporting structures first.");
            }

            // Check structural integrity
            if (!HasProperFoundation(gridCoordinate, size))
            {
                return (false, "Placement requires foundation support. Build foundations or supporting structures below.");
            }

            // Check if placement would create unstable structure
            var integrityCheck = CheckStructuralIntegrity(gridCoordinate.z);
            if (!integrityCheck.isStable)
            {
                return (false, "Placement would create unstable structure. Ensure proper support below.");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Get construction priority for different height levels
        /// </summary>
        public Dictionary<int, int> GetConstructionPriority()
        {
            var priorities = new Dictionary<int, int>();

            for (int level = 0; level < _bounds.MaxHeightLevels; level++)
            {
                int priority = 0;

                if (IsHeightLevelOccupied(level))
                {
                    priority += 10; // Bonus for occupied levels
                }

                if (IsHeightLevelAccessible(level))
                {
                    priority += 5; // Bonus for accessible levels
                }

                // Lower levels get higher priority for stability
                priority += (_bounds.MaxHeightLevels - level) * 2;

                priorities[level] = priority;
            }

            return priorities;
        }

        /// <summary>
        /// Update foundation requirements for all cells
        /// </summary>
        public void UpdateFoundationRequirements()
        {
            foreach (var kvp in _gridCells)
            {
                bool requiresFoundation = RequiresFoundation(kvp.Key);
                if (kvp.Value.RequiresFoundation != requiresFoundation)
                {
                    kvp.Value.RequiresFoundation = requiresFoundation;
                    OnFoundationRequirementChanged?.Invoke(kvp.Key, requiresFoundation);
                }
            }
        }
    }
}
