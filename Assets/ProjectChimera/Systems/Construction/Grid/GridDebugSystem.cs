using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Construction.Grid;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Debug system for grid visualization and diagnostics.
    /// Provides gizmos, debug drawing, and grid analysis tools.
    /// </summary>
    public class GridDebugSystem
    {
        private readonly Dictionary<Vector3Int, GridTypes.GridCell> _gridCells;
        private readonly GridTypes.GridBounds _bounds;
        private readonly GridTypes.GridSnapSettings _settings;
        private readonly GridCalculations _calculations;

        // Debug settings
        private bool _showOccupiedCells = true;
        private bool _showGridBounds = true;
        private bool _showCellTypes = true;
        private bool _showFoundationRequirements = true;
        private float _debugDrawDuration = 0f; // 0 = persistent

        public GridDebugSystem(
            Dictionary<Vector3Int, GridTypes.GridCell> gridCells,
            GridTypes.GridBounds bounds,
            GridTypes.GridSnapSettings settings,
            GridCalculations calculations)
        {
            _gridCells = gridCells;
            _bounds = bounds;
            _settings = settings;
            _calculations = calculations;
        }

        /// <summary>
        /// Draw debug gizmos for the grid system
        /// </summary>
        public void DrawGizmos()
        {
            if (!_settings.ShowGrid)
                return;

            // Draw grid bounds
            if (_showGridBounds)
            {
                DrawGridBounds();
            }

            // Draw occupied cells
            if (_showOccupiedCells)
            {
                DrawOccupiedCells();
            }

            // Draw cell types
            if (_showCellTypes)
            {
                DrawCellTypes();
            }

            // Draw foundation requirements
            if (_showFoundationRequirements)
            {
                DrawFoundationRequirements();
            }
        }

        /// <summary>
        /// Draw the overall grid bounds
        /// </summary>
        private void DrawGridBounds()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = _bounds.Origin + _bounds.Dimensions / 2f;
            Gizmos.DrawWireCube(center, _bounds.Dimensions);
        }

        /// <summary>
        /// Draw occupied grid cells
        /// </summary>
        private void DrawOccupiedCells()
        {
            Gizmos.color = Color.red;
            foreach (var kvp in _gridCells)
            {
                if (kvp.Value.IsOccupied)
                {
                    Vector3 worldPos = _calculations.GridToWorldPosition(kvp.Key);
                    Vector3 size = Vector3.one * _settings.GridSize * 0.8f; // Slightly smaller for visibility
                    Gizmos.DrawWireCube(worldPos, size);
                }
            }
        }

        /// <summary>
        /// Draw different cell types with distinct colors
        /// </summary>
        private void DrawCellTypes()
        {
            foreach (var kvp in _gridCells)
            {
                Vector3 worldPos = _calculations.GridToWorldPosition(kvp.Key);
                Vector3 size = Vector3.one * _settings.GridSize * 0.6f;

                switch (kvp.Value.CellType)
                {
                    case GridTypes.GridCellType.Blocked:
                        Gizmos.color = Color.black;
                        Gizmos.DrawCube(worldPos, size);
                        break;
                    case GridTypes.GridCellType.Special:
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(worldPos, size);
                        break;
                    case GridTypes.GridCellType.Reserved:
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawWireCube(worldPos, size);
                        break;
                }
            }
        }

        /// <summary>
        /// Draw foundation requirement indicators
        /// </summary>
        private void DrawFoundationRequirements()
        {
            Gizmos.color = Color.blue;
            foreach (var kvp in _gridCells)
            {
                if (kvp.Value.RequiresFoundation)
                {
                    Vector3 worldPos = _calculations.GridToWorldPosition(kvp.Key);
                    worldPos.y -= _bounds.HeightLevelSpacing / 2f; // Show below the cell

                    Vector3 size = new Vector3(_settings.GridSize * 0.5f, _bounds.HeightLevelSpacing * 0.5f, _settings.GridSize * 0.5f);
                    Gizmos.DrawWireCube(worldPos, size);
                }
            }
        }

        /// <summary>
        /// Draw debug information for a specific grid coordinate
        /// </summary>
        public void DrawDebugInfo(Vector3Int gridCoord)
        {
            if (_gridCells.TryGetValue(gridCoord, out GridTypes.GridCell cell))
            {
                Vector3 worldPos = _calculations.GridToWorldPosition(gridCoord);

                // Draw coordinate label
                string label = $"({gridCoord.x},{gridCoord.y},{gridCoord.z})";
                if (cell.IsOccupied)
                {
                    label += $" - Occupied by {cell.OccupyingObject?.name ?? "null"}";
                }
                label += $" - {cell.CellType}";

                Debug.DrawLine(worldPos, worldPos + Vector3.up * 2f, Color.white, _debugDrawDuration);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPos + Vector3.up * 2.5f, label);
#endif
            }
        }

        /// <summary>
        /// Draw debug path between two grid coordinates
        /// </summary>
        public void DrawDebugPath(List<Vector3Int> path, Color pathColor = default)
        {
            if (path == null || path.Count < 2)
                return;

            if (pathColor == default)
                pathColor = Color.green;

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 start = _calculations.GridToWorldPosition(path[i]);
                Vector3 end = _calculations.GridToWorldPosition(path[i + 1]);
                Debug.DrawLine(start, end, pathColor, _debugDrawDuration);
            }
        }

        /// <summary>
        /// Draw debug area highlighting
        /// </summary>
        public void DrawDebugArea(Vector3Int gridCoordinate, Vector3Int size, Color areaColor = default)
        {
            if (areaColor == default)
                areaColor = Color.yellow;

            List<Vector3Int> coordinates = _calculations.GetCoordinatesInArea(gridCoordinate, size);

            foreach (Vector3Int coord in coordinates)
            {
                if (_gridCells.TryGetValue(coord, out GridTypes.GridCell cell))
                {
                    Vector3 worldPos = _calculations.GridToWorldPosition(coord);
                    Vector3 gizmoSize = Vector3.one * _settings.GridSize * 0.9f;

                    // Use different colors for different states
                    if (!cell.IsValid)
                        Gizmos.color = Color.gray;
                    else if (cell.IsOccupied)
                        Gizmos.color = Color.red;
                    else
                        Gizmos.color = areaColor;

                    Gizmos.DrawWireCube(worldPos, gizmoSize);
                }
            }
        }

        /// <summary>
        /// Log comprehensive grid statistics
        /// </summary>
        public void LogGridStatistics()
        {
            var stats = GetGridStatistics();
            ChimeraLogger.Log($"[GridDebug] Grid Statistics:");
            ChimeraLogger.Log($"[GridDebug] Total Cells: {stats.totalCells}");
            ChimeraLogger.Log($"[GridDebug] Occupied Cells: {stats.occupiedCells}");
            ChimeraLogger.Log($"[GridDebug] Free Cells: {stats.freeCells}");
            ChimeraLogger.Log($"[GridDebug] Occupancy Rate: {stats.occupancyRate:P2}");

            ChimeraLogger.Log($"[GridDebug] Cell Type Distribution:");
            foreach (var kvp in stats.cellTypeDistribution)
            {
                ChimeraLogger.Log($"[GridDebug] {kvp.Key}: {kvp.Value}");
            }

            ChimeraLogger.Log($"[GridDebug] Height Distribution:");
            foreach (var kvp in stats.heightDistribution)
            {
                if (kvp.Value > 0)
                    ChimeraLogger.Log($"[GridDebug] Level {kvp.Key}: {kvp.Value} occupied cells");
            }
        }

        /// <summary>
        /// Get comprehensive grid statistics
        /// </summary>
        private (int totalCells, int occupiedCells, int freeCells, float occupancyRate,
                Dictionary<GridTypes.GridCellType, int> cellTypeDistribution,
                Dictionary<int, int> heightDistribution) GetGridStatistics()
        {
            int totalCells = _gridCells.Count;
            int occupiedCells = 0;
            var cellTypeDistribution = new Dictionary<GridTypes.GridCellType, int>();
            var heightDistribution = new Dictionary<int, int>();

            foreach (var kvp in _gridCells)
            {
                var cell = kvp.Value;

                // Count occupied cells
                if (cell.IsOccupied)
                    occupiedCells++;

                // Count cell types
                if (!cellTypeDistribution.ContainsKey(cell.CellType))
                    cellTypeDistribution[cell.CellType] = 0;
                cellTypeDistribution[cell.CellType]++;

                // Count by height level
                int heightLevel = kvp.Key.z;
                if (!heightDistribution.ContainsKey(heightLevel))
                    heightDistribution[heightLevel] = 0;
                if (cell.IsOccupied)
                    heightDistribution[heightLevel]++;
            }

            int freeCells = totalCells - occupiedCells;
            float occupancyRate = totalCells > 0 ? (float)occupiedCells / totalCells : 0f;

            return (totalCells, occupiedCells, freeCells, occupancyRate, cellTypeDistribution, heightDistribution);
        }

        /// <summary>
        /// Validate grid integrity and log issues
        /// </summary>
        public void ValidateAndLogIntegrity()
        {
            var issues = new List<string>();

            // Check for orphaned objects
            var placedObjects = new List<GridPlaceable>();
            foreach (var cell in _gridCells.Values)
            {
                if (cell.IsOccupied && cell.OccupyingObject != null && !placedObjects.Contains(cell.OccupyingObject))
                {
                    placedObjects.Add(cell.OccupyingObject);
                }
            }

            // Check for objects not in grid
            foreach (var obj in placedObjects)
            {
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

            if (issues.Count > 0)
            {
                ChimeraLogger.LogWarning($"[GridDebug] Found {issues.Count} integrity issues:");
                foreach (var issue in issues)
                {
                    ChimeraLogger.LogWarning($"[GridDebug] {issue}");
                }
            }
            else
            {
                ChimeraLogger.Log("[GridDebug] Grid integrity validation passed");
            }
        }

        /// <summary>
        /// Draw debug sphere at world position
        /// </summary>
        public void DrawDebugSphere(Vector3 worldPosition, float radius, Color color = default)
        {
            if (color == default)
                color = Color.cyan;

            // Draw sphere
            Gizmos.color = color;
            Gizmos.DrawWireSphere(worldPosition, radius);

            // Draw grid coordinate label
            Vector3Int gridCoord = _calculations.WorldToGridPosition(worldPosition);
            string label = $"Grid: ({gridCoord.x},{gridCoord.y},{gridCoord.z})";
#if UNITY_EDITOR
            UnityEditor.Handles.Label(worldPosition + Vector3.up * (radius + 0.5f), label);
#endif
        }

        /// <summary>
        /// Set debug visualization options
        /// </summary>
        public void SetDebugOptions(bool showOccupied, bool showBounds, bool showCellTypes, bool showFoundations)
        {
            _showOccupiedCells = showOccupied;
            _showGridBounds = showBounds;
            _showCellTypes = showCellTypes;
            _showFoundationRequirements = showFoundations;
        }

        /// <summary>
        /// Toggle debug drawing duration (persistent vs. temporary)
        /// </summary>
        public void SetPersistentDebug(bool persistent)
        {
            _debugDrawDuration = persistent ? 0f : 5f; // 5 seconds for temporary, 0 for persistent
        }
    }
}
