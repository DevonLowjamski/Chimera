using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree.Performance
{
    /// <summary>
    /// Manages culling for SpeedTree rendering optimization
    /// Handles frustum culling, distance culling, and occlusion culling
    /// </summary>
    public class CullingManager : ISpeedTreeCullingManager
    {
        // Culling configuration
        private SpeedTreeCullingStrategy _cullingStrategy;
        private float _cullingDistance = 100f;
        private float _frustumPadding = 5f;
        private bool _enableOcclusionCulling = false;

        // Culling data
        private Dictionary<GameObject, SpeedTreeRendererData> _rendererData = new Dictionary<GameObject, SpeedTreeRendererData>();
        private List<GameObject> _visibleRenderers = new List<GameObject>();
        private List<GameObject> _culledRenderers = new List<GameObject>();
        private HashSet<GameObject> _forceVisible = new HashSet<GameObject>();
        private HashSet<GameObject> _forceCulled = new HashSet<GameObject>();

        // Camera and frustum data
        private UnityEngine.Camera _mainCamera;
        private Plane[] _frustumPlanes = new Plane[6];
        private Vector3 _cameraPosition;
        private float _cameraFarClipPlane;

        // Performance tracking
        private int _totalCullingUpdates = 0;
        private float _lastCullingUpdate = 0f;
        private float _cullingUpdateInterval = 0.05f; // Update culling 20 times per second

        // Occlusion culling
        private Dictionary<GameObject, bool> _occlusionStates = new Dictionary<GameObject, bool>();

        /// <summary>
        /// Initialize the culling manager
        /// </summary>
        public void Initialize(SpeedTreeCullingStrategy strategy, float cullingDistance)
        {
            _cullingStrategy = strategy;
            _cullingDistance = cullingDistance;

            // Find main camera
            _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera != null)
            {
                _cameraFarClipPlane = _mainCamera.farClipPlane;
            }

            ChimeraLogger.Log("SPEEDTREE/CULL", "Initialized", null);
        }

        /// <summary>
        /// Update culling for all SpeedTree objects
        /// </summary>
        public void UpdateCulling(GameObject[] speedTrees, Vector3 cameraPosition, Plane[] frustumPlanes)
        {
            if (Time.time - _lastCullingUpdate < _cullingUpdateInterval)
            {
                return; // Throttle culling updates
            }

            _lastCullingUpdate = Time.time;
            _cameraPosition = cameraPosition;
            _frustumPlanes = frustumPlanes ?? (_mainCamera != null ? GeometryUtility.CalculateFrustumPlanes(_mainCamera) : new Plane[6]);

            // Clear previous results
            _visibleRenderers.Clear();
            _culledRenderers.Clear();

            // Update culling for all trees
            foreach (GameObject speedTree in speedTrees)
            {
                if (speedTree == null) continue;

                bool isVisible = EvaluateCulling(speedTree);
                UpdateRendererVisibility(speedTree, isVisible);
            }

            _totalCullingUpdates++;
        }

        /// <summary>
        /// Evaluate if a SpeedTree should be culled
        /// </summary>
        private bool EvaluateCulling(GameObject speedTree)
        {
            // Check forced visibility/culling first
            if (_forceVisible.Contains(speedTree))
            {
                return true;
            }

            if (_forceCulled.Contains(speedTree))
            {
                return false;
            }

            // Get or create renderer data
            if (!_rendererData.TryGetValue(speedTree, out SpeedTreeRendererData data))
            {
                data = new SpeedTreeRendererData(speedTree);
                _rendererData[speedTree] = data;
            }

            // Distance culling
            float distanceToCamera = Vector3.Distance(speedTree.transform.position, _cameraPosition);
            if (distanceToCamera > _cullingDistance)
            {
                return false;
            }

            // Frustum culling
            if (_cullingStrategy == SpeedTreeCullingStrategy.FrustumBased ||
                _cullingStrategy == SpeedTreeCullingStrategy.Hybrid)
            {
                if (!IsInFrustum(speedTree, data))
                {
                    return false;
                }
            }

            // Occlusion culling
            if (_enableOcclusionCulling && (_cullingStrategy == SpeedTreeCullingStrategy.OcclusionBased ||
                                           _cullingStrategy == SpeedTreeCullingStrategy.Hybrid))
            {
                if (IsOccluded(speedTree))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if object is in camera frustum
        /// </summary>
        private bool IsInFrustum(GameObject speedTree, SpeedTreeRendererData data)
        {
            if (_frustumPlanes == null || _frustumPlanes.Length != 6) return true;

            // Use bounds for more accurate frustum culling
            Bounds bounds = data.Bounds;
            bounds.center = speedTree.transform.position + data.Bounds.center;

            // Add padding to frustum planes
            Plane[] paddedPlanes = new Plane[6];
            for (int i = 0; i < 6; i++)
            {
                paddedPlanes[i] = _frustumPlanes[i];
                // Move plane slightly to add padding
                Vector3 normal = paddedPlanes[i].normal;
                float distance = paddedPlanes[i].distance - _frustumPadding;
                paddedPlanes[i] = new Plane(normal, distance);
            }

            return GeometryUtility.TestPlanesAABB(paddedPlanes, bounds);
        }

        /// <summary>
        /// Check if object is occluded
        /// </summary>
        private bool IsOccluded(GameObject speedTree)
        {
            // Simple occlusion check using raycast
            Vector3 direction = speedTree.transform.position - _cameraPosition;
            float distance = direction.magnitude;

            if (Physics.Raycast(_cameraPosition, direction.normalized, out RaycastHit hit, distance - 0.1f))
            {
                // Check if hit object is not the speedTree itself
                if (hit.collider.gameObject != speedTree)
                {
                    _occlusionStates[speedTree] = true;
                    return true;
                }
            }

            _occlusionStates[speedTree] = false;
            return false;
        }

        /// <summary>
        /// Update renderer visibility
        /// </summary>
        private void UpdateRendererVisibility(GameObject speedTree, bool isVisible)
        {
            Renderer renderer = speedTree.GetComponent<Renderer>();
            if (renderer == null) return;

            // Update renderer enabled state
            bool wasVisible = renderer.enabled;
            renderer.enabled = isVisible;

            // Track visibility changes
            if (isVisible)
            {
                _visibleRenderers.Add(speedTree);
                if (!wasVisible)
                {
                    ChimeraLogger.Log("SPEEDTREE/CULL", "Became visible", null);
                }
            }
            else
            {
                _culledRenderers.Add(speedTree);
                if (wasVisible)
                {
                    ChimeraLogger.Log("SPEEDTREE/CULL", "Became hidden", null);
                }
            }

            // Update renderer data
            if (_rendererData.TryGetValue(speedTree, out SpeedTreeRendererData data))
            {
                data.IsVisible = isVisible;
                data.IsCulled = !isVisible;
                data.LastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Set culling distance
        /// </summary>
        public void SetCullingDistance(float distance)
        {
            _cullingDistance = Mathf.Max(1f, distance);
            ChimeraLogger.Log("SPEEDTREE/CULL", "Culling distance set", null);
        }

        /// <summary>
        /// Force a SpeedTree to be visible
        /// </summary>
        public void ForceVisible(GameObject speedTree)
        {
            if (speedTree != null)
            {
                _forceVisible.Add(speedTree);
                _forceCulled.Remove(speedTree);
                UpdateRendererVisibility(speedTree, true);
                ChimeraLogger.Log("SPEEDTREE/CULL", "Forced visible", null);
            }
        }

        /// <summary>
        /// Force a SpeedTree to be culled
        /// </summary>
        public void ForceCull(GameObject speedTree)
        {
            if (speedTree != null)
            {
                _forceCulled.Add(speedTree);
                _forceVisible.Remove(speedTree);
                UpdateRendererVisibility(speedTree, false);
                ChimeraLogger.Log("SPEEDTREE/CULL", "Forced culled", null);
            }
        }

        /// <summary>
        /// Force a GameObject to be visible, ignoring normal culling
        /// </summary>
        public void ForceShow(GameObject speedTree)
        {
            if (speedTree != null)
            {
                _forceVisible.Add(speedTree);
                _forceCulled.Remove(speedTree);
                UpdateRendererVisibility(speedTree, true);
                ChimeraLogger.Log("SPEEDTREE/CULL", "Force show", null);
            }
        }

        /// <summary>
        /// Get count of visible renderers
        /// </summary>
        public int GetVisibleCount()
        {
            return _visibleRenderers.Count;
        }

        /// <summary>
        /// Get count of culled renderers
        /// </summary>
        public int GetCulledCount()
        {
            return _culledRenderers.Count;
        }

        /// <summary>
        /// Clear all culling data and forces
        /// </summary>
        public void ClearCulling()
        {
            _visibleRenderers.Clear();
            _culledRenderers.Clear();
            _forceVisible.Clear();
            _forceCulled.Clear();
            _occlusionStates.Clear();

            // Reset all renderers to visible
            foreach (var kvp in _rendererData)
            {
                GameObject speedTree = kvp.Key;
                Renderer renderer = speedTree.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            ChimeraLogger.Log("SPEEDTREE/CULL", "Cleared culling", null);
        }

        /// <summary>
        /// Add a SpeedTree to culling tracking
        /// </summary>
        public void AddSpeedTree(GameObject speedTree)
        {
            if (!_rendererData.ContainsKey(speedTree))
            {
                _rendererData[speedTree] = new SpeedTreeRendererData(speedTree);
                ChimeraLogger.Log("SPEEDTREE/CULL", "Added SpeedTree", null);
            }
        }

        /// <summary>
        /// Remove a SpeedTree from culling tracking
        /// </summary>
        public void RemoveSpeedTree(GameObject speedTree)
        {
            if (_rendererData.Remove(speedTree))
            {
                _visibleRenderers.Remove(speedTree);
                _culledRenderers.Remove(speedTree);
                _forceVisible.Remove(speedTree);
                _forceCulled.Remove(speedTree);
                _occlusionStates.Remove(speedTree);

                ChimeraLogger.Log("SPEEDTREE/CULL", "Removed SpeedTree", null);
            }
        }

        /// <summary>
        /// Get culling statistics
        /// </summary>
        public string GetCullingStatistics()
        {
            int total = _visibleRenderers.Count + _culledRenderers.Count;
            float visiblePercentage = total > 0 ? (_visibleRenderers.Count / (float)total) * 100f : 0f;

            return $"Culling Statistics - Total: {total}, " +
                   $"Visible: {_visibleRenderers.Count} ({visiblePercentage:F1}%), " +
                   $"Culled: {_culledRenderers.Count}, " +
                   $"Forced Visible: {_forceVisible.Count}, " +
                   $"Forced Culled: {_forceCulled.Count}";
        }

        /// <summary>
        /// Optimize culling settings based on performance
        /// </summary>
        public void OptimizeCulling()
        {
            // Adjust culling distance based on performance
            float averageFrameTime = Time.time / Time.frameCount; // Rough estimate

            if (averageFrameTime > 33f) // Less than 30 FPS
            {
                // Reduce culling distance to improve performance
                _cullingDistance = Mathf.Max(50f, _cullingDistance * 0.8f);
                ChimeraLogger.Log("SPEEDTREE/CULL", "Optimized culling", null);
            }
            else if (averageFrameTime < 16f) // More than 60 FPS
            {
                // Increase culling distance for better quality
                _cullingDistance = Mathf.Min(200f, _cullingDistance * 1.2f);
                ChimeraLogger.Log("SPEEDTREE/CULLING", "Operation completed");
            }
        }

        /// <summary>
        /// Set culling strategy
        /// </summary>
        public void SetCullingStrategy(SpeedTreeCullingStrategy strategy)
        {
            _cullingStrategy = strategy;
            ChimeraLogger.Log("SPEEDTREE/CULL", "Culling strategy set", null);
        }

        /// <summary>
        /// Enable/disable occlusion culling
        /// </summary>
        public void SetOcclusionCulling(bool enabled)
        {
            _enableOcclusionCulling = enabled;
            if (!enabled)
            {
                _occlusionStates.Clear();
            }
            ChimeraLogger.Log("SPEEDTREE/CULL", "Occlusion toggled", null);
        }

        // Public properties
        public SpeedTreeCullingStrategy CullingStrategy => _cullingStrategy;
        public float CullingDistance => _cullingDistance;
        public float FrustumPadding
        {
            get => _frustumPadding;
            set => _frustumPadding = Mathf.Max(0f, value);
        }

        public bool EnableOcclusionCulling
        {
            get => _enableOcclusionCulling;
            set => SetOcclusionCulling(value);
        }

        public int TotalCullingUpdates => _totalCullingUpdates;
        public Dictionary<GameObject, SpeedTreeRendererData> RendererData => _rendererData;
        public List<GameObject> VisibleRenderers => _visibleRenderers;
        public List<GameObject> CulledRenderers => _culledRenderers;
    }
}
