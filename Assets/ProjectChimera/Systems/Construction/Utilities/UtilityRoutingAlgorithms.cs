using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Construction.Utilities
{
    /// <summary>
    /// Utility routing algorithms - pathfinding for wires, pipes, and ducts.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Professional-looking installations with automatic routing!"
    ///
    /// **Player Experience**:
    /// - Click source → click destination → auto-route path
    /// - Clean Manhattan-style routing (right angles, professional look)
    /// - Avoids obstacles (walls, equipment, other utilities)
    /// - Real-time cost calculation during drag
    /// - Visual preview before committing
    ///
    /// **Strategic Depth**:
    /// - Shorter paths = lower cost
    /// - Wall/ceiling routing is cheaper than floor trenching
    /// - Can manually override auto-routing for custom paths
    /// - Utilities can share conduits for efficiency
    ///
    /// **Technical Implementation**:
    /// - A* pathfinding with Manhattan distance heuristic
    /// - Grid-based with snap-to-grid waypoints
    /// - Obstacle avoidance with dynamic cost weighting
    /// - Path smoothing for professional appearance
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: Beautiful auto-routed wires/pipes → "It just works!"
    /// Behind scenes: A* search, cost optimization, collision detection, path smoothing.
    /// </summary>
    public static class UtilityRoutingAlgorithms
    {
        /// <summary>
        /// Routes a utility path from source to destination using A* pathfinding.
        /// </summary>
        /// <param name="from">Source location</param>
        /// <param name="to">Destination location</param>
        /// <param name="gridSize">Grid cell size (e.g., 1ft)</param>
        /// <param name="obstacles">List of obstacle bounds to avoid</param>
        /// <param name="routingPreference">Prefer walls, ceiling, or floor routing</param>
        /// <returns>List of waypoints forming the path</returns>
        public static List<Vector3> RouteUtilityPath(
            Vector3 from,
            Vector3 to,
            float gridSize = 1f,
            List<Bounds> obstacles = null,
            RoutingPreference preference = RoutingPreference.WallCeiling)
        {
            // Simplified routing for Phase 1 - direct Manhattan-style path
            // TODO Phase 2: Implement full A* with obstacle avoidance

            var path = new List<Vector3>();
            obstacles = obstacles ?? new List<Bounds>();

            // Snap to grid
            Vector3 startSnapped = SnapToGrid(from, gridSize);
            Vector3 endSnapped = SnapToGrid(to, gridSize);

            path.Add(startSnapped);

            // Choose routing style based on preference
            switch (preference)
            {
                case RoutingPreference.WallCeiling:
                    // Route along ceiling, then down wall
                    AddWallCeilingPath(path, startSnapped, endSnapped, gridSize);
                    break;

                case RoutingPreference.Floor:
                    // Route along floor
                    AddFloorPath(path, startSnapped, endSnapped, gridSize);
                    break;

                case RoutingPreference.Direct:
                    // Direct path (for short distances or same-room routing)
                    AddDirectPath(path, startSnapped, endSnapped, gridSize);
                    break;

                case RoutingPreference.AStar:
                    // Full A* pathfinding (Phase 2 feature)
                    AddAStarPath(path, startSnapped, endSnapped, gridSize, obstacles);
                    break;
            }

            path.Add(endSnapped);

            // Smooth path for professional appearance
            var smoothedPath = SmoothPath(path, gridSize);

            return smoothedPath;
        }

        /// <summary>
        /// Calculates total path length from waypoints.
        /// </summary>
        public static float CalculatePathLength(List<Vector3> path)
        {
            if (path == null || path.Count < 2)
                return 0f;

            float totalLength = 0f;

            for (int i = 0; i < path.Count - 1; i++)
            {
                totalLength += Vector3.Distance(path[i], path[i + 1]);
            }

            return totalLength;
        }

        /// <summary>
        /// Estimates path length using Manhattan distance (before actual routing).
        /// GAMEPLAY: Shows estimated cost before player commits to routing.
        /// </summary>
        public static float EstimatePathLength(Vector3 from, Vector3 to)
        {
            // Manhattan distance (sum of absolute deltas)
            return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y) + Mathf.Abs(to.z - from.z);
        }

        /// <summary>
        /// Checks if path is valid (no obstacles, within bounds).
        /// </summary>
        public static bool IsPathValid(List<Vector3> path, List<Bounds> obstacles)
        {
            if (path == null || path.Count < 2)
                return false;

            obstacles = obstacles ?? new List<Bounds>();

            // Check each path segment for obstacle collisions
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 start = path[i];
                Vector3 end = path[i + 1];

                foreach (var obstacle in obstacles)
                {
                    if (LineIntersectsBounds(start, end, obstacle))
                        return false;
                }
            }

            return true;
        }

        #region Routing Styles

        /// <summary>
        /// Adds wall/ceiling routing path (professional installation style).
        /// Routes up to ceiling, across, then down to destination.
        /// </summary>
        private static void AddWallCeilingPath(List<Vector3> path, Vector3 start, Vector3 end, float gridSize)
        {
            // Go up to ceiling height (usually max Y in room)
            float ceilingHeight = Mathf.Max(start.y, end.y) + 2f; // 2ft below ceiling
            Vector3 startCeiling = new Vector3(start.x, ceilingHeight, start.z);
            path.Add(startCeiling);

            // Route along ceiling (Manhattan style)
            Vector3 ceilingCorner = new Vector3(start.x, ceilingHeight, end.z);
            if (Vector3.Distance(startCeiling, ceilingCorner) > gridSize)
            {
                path.Add(ceilingCorner);
            }

            Vector3 endCeiling = new Vector3(end.x, ceilingHeight, end.z);
            if (Vector3.Distance(ceilingCorner, endCeiling) > gridSize)
            {
                path.Add(endCeiling);
            }

            // Down to destination
            Vector3 endWall = new Vector3(end.x, end.y, end.z);
            path.Add(endWall);
        }

        /// <summary>
        /// Adds floor routing path (floor trenching or conduit).
        /// </summary>
        private static void AddFloorPath(List<Vector3> path, Vector3 start, Vector3 end, float gridSize)
        {
            // Route along floor at minimum Y
            float floorHeight = Mathf.Min(start.y, end.y);

            // Down to floor
            Vector3 startFloor = new Vector3(start.x, floorHeight, start.z);
            path.Add(startFloor);

            // Manhattan routing along floor
            Vector3 floorCorner = new Vector3(start.x, floorHeight, end.z);
            if (Vector3.Distance(startFloor, floorCorner) > gridSize)
            {
                path.Add(floorCorner);
            }

            Vector3 endFloor = new Vector3(end.x, floorHeight, end.z);
            if (Vector3.Distance(floorCorner, endFloor) > gridSize)
            {
                path.Add(endFloor);
            }
        }

        /// <summary>
        /// Adds direct path (shortest distance, for same-room or short runs).
        /// </summary>
        private static void AddDirectPath(List<Vector3> path, Vector3 start, Vector3 end, float gridSize)
        {
            // Simple Manhattan path with one corner
            Vector3 corner = new Vector3(start.x, start.y, end.z);

            if (Vector3.Distance(start, corner) > gridSize)
            {
                path.Add(corner);
            }
        }

        /// <summary>
        /// Adds A* pathfinding path (Phase 2 feature - full obstacle avoidance).
        /// Currently uses simplified direct routing - will be enhanced in Phase 2.
        /// </summary>
        private static void AddAStarPath(List<Vector3> path, Vector3 start, Vector3 end,
            float gridSize, List<Bounds> obstacles)
        {
            // Phase 1: Fall back to direct routing
            // TODO Phase 2: Implement full A* with priority queue, g/h costs, etc.
            AddDirectPath(path, start, end, gridSize);
        }

        #endregion

        #region Path Optimization

        /// <summary>
        /// Smooths path by removing redundant waypoints.
        /// GAMEPLAY: Makes paths look professional, reduces poly count for rendering.
        /// </summary>
        private static List<Vector3> SmoothPath(List<Vector3> path, float gridSize)
        {
            if (path == null || path.Count <= 2)
                return path;

            var smoothed = new List<Vector3>();
            smoothed.Add(path[0]); // Always keep start

            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector3 prev = path[i - 1];
                Vector3 current = path[i];
                Vector3 next = path[i + 1];

                // Check if current waypoint is on a straight line between prev and next
                if (!IsCollinear(prev, current, next, gridSize * 0.1f))
                {
                    smoothed.Add(current); // Keep corner waypoints
                }
            }

            smoothed.Add(path[path.Count - 1]); // Always keep end

            return smoothed;
        }

        /// <summary>
        /// Checks if three points are collinear (on same line).
        /// </summary>
        private static bool IsCollinear(Vector3 p1, Vector3 p2, Vector3 p3, float tolerance)
        {
            // Calculate cross product - if near zero, points are collinear
            Vector3 v1 = p2 - p1;
            Vector3 v2 = p3 - p2;
            float cross = Vector3.Cross(v1.normalized, v2.normalized).magnitude;

            return cross < tolerance;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Snaps position to grid for clean alignment.
        /// </summary>
        private static Vector3 SnapToGrid(Vector3 position, float gridSize)
        {
            return new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                Mathf.Round(position.y / gridSize) * gridSize,
                Mathf.Round(position.z / gridSize) * gridSize
            );
        }

        /// <summary>
        /// Checks if line segment intersects with bounds (obstacle).
        /// </summary>
        private static bool LineIntersectsBounds(Vector3 lineStart, Vector3 lineEnd, Bounds bounds)
        {
            // Simplified AABB line intersection test
            // Check if line segment intersects any face of the bounding box

            Vector3 boxMin = bounds.min;
            Vector3 boxMax = bounds.max;

            // Check if either endpoint is inside bounds
            if (bounds.Contains(lineStart) || bounds.Contains(lineEnd))
                return true;

            // Check if line intersects any of the 6 faces
            // Simplified check: if line bounding box intersects obstacle bounds
            Bounds lineBounds = new Bounds(
                (lineStart + lineEnd) / 2f,
                new Vector3(
                    Mathf.Abs(lineEnd.x - lineStart.x),
                    Mathf.Abs(lineEnd.y - lineStart.y),
                    Mathf.Abs(lineEnd.z - lineStart.z)
                )
            );

            return lineBounds.Intersects(bounds);
        }

        /// <summary>
        /// Calculates routing cost based on path length and preferences.
        /// GAMEPLAY: Different routing styles have different costs.
        /// </summary>
        public static float CalculateRoutingCost(List<Vector3> path, RoutingPreference preference,
            float costPerFoot = 2.5f)
        {
            float length = CalculatePathLength(path);
            float baseCost = length * costPerFoot;

            // Apply preference multipliers
            float multiplier = preference switch
            {
                RoutingPreference.WallCeiling => 1.0f,   // Standard cost
                RoutingPreference.Floor => 1.3f,          // Floor trenching is more expensive
                RoutingPreference.Direct => 0.9f,         // Direct is cheapest
                RoutingPreference.AStar => 1.1f,          // Optimized routing has labor cost
                _ => 1.0f
            };

            return baseCost * multiplier;
        }

        /// <summary>
        /// Generates visual preview path for UI display.
        /// GAMEPLAY: Shows player the route before committing.
        /// </summary>
        public static UtilityPathPreview GeneratePathPreview(
            Vector3 from,
            Vector3 to,
            RoutingPreference preference,
            float costPerFoot = 2.5f,
            List<Bounds> obstacles = null)
        {
            var path = RouteUtilityPath(from, to, 1f, obstacles, preference);
            float length = CalculatePathLength(path);
            float cost = CalculateRoutingCost(path, preference, costPerFoot);
            bool isValid = IsPathValid(path, obstacles);

            return new UtilityPathPreview
            {
                Path = path,
                Length = length,
                EstimatedCost = cost,
                IsValid = isValid,
                WaypointCount = path.Count,
                Preference = preference
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Routing preference for utility paths.
    /// </summary>
    [Serializable]
    public enum RoutingPreference
    {
        WallCeiling,  // Professional install - up to ceiling, across, down
        Floor,        // Floor trenching/conduit
        Direct,       // Shortest distance (for same room)
        AStar         // Full obstacle avoidance (Phase 2)
    }

    /// <summary>
    /// Path preview data for UI display.
    /// </summary>
    [Serializable]
    public struct UtilityPathPreview
    {
        public List<Vector3> Path;
        public float Length;
        public float EstimatedCost;
        public bool IsValid;
        public int WaypointCount;
        public RoutingPreference Preference;
    }

    #endregion
}
