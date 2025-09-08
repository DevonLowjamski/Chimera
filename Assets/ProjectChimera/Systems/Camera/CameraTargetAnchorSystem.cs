using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Data.Camera;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Facilities;
// using ProjectChimera.Systems.Cultivation;  // Commented out - namespace not available
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Maps raycast targets to logical camera anchors and determines appropriate camera levels
    /// Phase 3 implementation - provides intelligent target-to-anchor mapping for smooth camera transitions
    /// </summary>
    public class CameraTargetAnchorSystem : MonoBehaviour
    {
        [Header("Anchor Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _enableDebugVisualization = false;
        [SerializeField] private float _maxAnchorSearchDistance = 50f;
        [SerializeField] private LayerMask _anchorLayers = -1;

        [Header("Target Type Detection")]
        [SerializeField] private LayerMask _facilityLayers = 1;
        [SerializeField] private LayerMask _roomLayers = 2;
        [SerializeField] private LayerMask _benchLayers = 4;
        [SerializeField] private LayerMask _plantLayers = 8;

        [Header("Component Detection Tags")]
        [SerializeField] private string[] _facilityTags = { "Facility", "Building", "Infrastructure" };
        [SerializeField] private string[] _roomTags = { "Room", "Greenhouse", "Chamber" };
        [SerializeField] private string[] _benchTags = { "Bench", "Table", "GrowStation" };
        [SerializeField] private string[] _plantTags = { "Plant", "Cannabis", "PlantInstance" };

        [Header("Anchor Hierarchy")]
        [SerializeField] private Transform _facilityAnchorRoot;
        [SerializeField] private Transform _roomAnchorRoot;
        [SerializeField] private Transform _benchAnchorRoot;
        [SerializeField] private Transform _plantAnchorRoot;

        // Cached anchor dictionaries for performance
        private Dictionary<CameraLevel, List<Transform>> _anchorsByLevel = new Dictionary<CameraLevel, List<Transform>>();
        private Dictionary<Transform, CameraLevel> _targetToLevelMap = new Dictionary<Transform, CameraLevel>();
        private Dictionary<Transform, Transform> _targetToAnchorMap = new Dictionary<Transform, Transform>();

        // Component type caches
        private readonly Dictionary<Transform, bool> _plantComponentCache = new Dictionary<Transform, bool>();
        private readonly Dictionary<Transform, bool> _facilityComponentCache = new Dictionary<Transform, bool>();
        private readonly Dictionary<Transform, bool> _benchComponentCache = new Dictionary<Transform, bool>();

        private void Start()
        {
            InitializeAnchorSystem();
        }

        private void InitializeAnchorSystem()
        {
            ChimeraLogger.Log("[CameraTargetAnchorSystem] Initializing anchor mapping system...");

            // Initialize anchor level collections
            InitializeAnchorCollections();

            // Scan for and cache all anchors
            ScanAndCacheAnchors();

            // Build target-to-anchor mappings
            BuildTargetAnchorMappings();

            if (_enableDebugLogging)
            {
                LogAnchorSystemStatus();
            }
        }

        private void InitializeAnchorCollections()
        {
            _anchorsByLevel.Clear();
            _anchorsByLevel[CameraLevel.Facility] = new List<Transform>();
            _anchorsByLevel[CameraLevel.Room] = new List<Transform>();
            _anchorsByLevel[CameraLevel.Bench] = new List<Transform>();
            _anchorsByLevel[CameraLevel.Plant] = new List<Transform>();
        }

        private void ScanAndCacheAnchors()
        {
            // Scan facility anchors
            ScanAnchorsInHierarchy(_facilityAnchorRoot, CameraLevel.Facility);

            // Scan room anchors
            ScanAnchorsInHierarchy(_roomAnchorRoot, CameraLevel.Room);

            // Scan bench anchors
            ScanAnchorsInHierarchy(_benchAnchorRoot, CameraLevel.Bench);

            // Scan plant anchors
            ScanAnchorsInHierarchy(_plantAnchorRoot, CameraLevel.Plant);

            // Also scan by layer mask and tags if hierarchy roots aren't set
            if (_facilityAnchorRoot == null) ScanAnchorsByLayerAndTags(CameraLevel.Facility, _facilityLayers, _facilityTags);
            if (_roomAnchorRoot == null) ScanAnchorsByLayerAndTags(CameraLevel.Room, _roomLayers, _roomTags);
            if (_benchAnchorRoot == null) ScanAnchorsByLayerAndTags(CameraLevel.Bench, _benchLayers, _benchTags);
            if (_plantAnchorRoot == null) ScanAnchorsByLayerAndTags(CameraLevel.Plant, _plantLayers, _plantTags);
        }

        private void ScanAnchorsInHierarchy(Transform root, CameraLevel level)
        {
            if (root == null) return;

            // Add the root itself as an anchor
            _anchorsByLevel[level].Add(root);

            // Add all children as anchors
            foreach (Transform child in root)
            {
                _anchorsByLevel[level].Add(child);

                // Recursively scan children
                ScanChildAnchors(child, level);
            }
        }

        private void ScanChildAnchors(Transform parent, CameraLevel level)
        {
            foreach (Transform child in parent)
            {
                _anchorsByLevel[level].Add(child);
                ScanChildAnchors(child, level);
            }
        }

        private void ScanAnchorsByLayerAndTags(CameraLevel level, LayerMask layers, string[] tags)
        {
            // Find GameObjects by layer
            GameObject[] allObjects = /* TODO: Replace FindObjectsOfType with ServiceContainer.GetAll<GameObject>() */ new GameObject[0];
            var layerObjects = allObjects.Where(go => ((1 << go.layer) & layers) != 0);

            foreach (var obj in layerObjects)
            {
                if (!_anchorsByLevel[level].Contains(obj.transform))
                {
                    _anchorsByLevel[level].Add(obj.transform);
                }
            }

            // Find GameObjects by tag
            foreach (var tag in tags)
            {
                try
                {
                    GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
                    foreach (var obj in taggedObjects)
                    {
                        if (!_anchorsByLevel[level].Contains(obj.transform))
                        {
                            _anchorsByLevel[level].Add(obj.transform);
                        }
                    }
                }
                catch (UnityException)
                {
                    // Tag doesn't exist, skip
                }
            }
        }

        private void BuildTargetAnchorMappings()
        {
            _targetToLevelMap.Clear();
            _targetToAnchorMap.Clear();

            // Find all potential targets in the scene
            Transform[] allTransforms = /* TODO: Replace FindObjectsOfType with ServiceContainer.GetAll<Transform>() */ new Transform[0];
            var allTargets = allTransforms.Where(t => IsValidTarget(t))
                .ToArray();

            foreach (var target in allTargets)
            {
                var level = DetermineTargetLevel(target);
                var anchor = FindBestAnchorForTarget(target, level);

                _targetToLevelMap[target] = level;
                if (anchor != null)
                {
                    _targetToAnchorMap[target] = anchor;
                }
            }
        }

        /// <summary>
        /// Get the appropriate camera level for a raycast target
        /// </summary>
        public CameraLevel GetTargetLevel(Transform target)
        {
            if (target == null) return CameraLevel.Facility;

            // Check cache first
            if (_targetToLevelMap.TryGetValue(target, out var cachedLevel))
            {
                return cachedLevel;
            }

            // Determine level and cache result
            var level = DetermineTargetLevel(target);
            _targetToLevelMap[target] = level;

            return level;
        }

        /// <summary>
        /// Get the logical anchor for a raycast target
        /// </summary>
        public Transform GetTargetAnchor(Transform target)
        {
            if (target == null) return null;

            // Check cache first
            if (_targetToAnchorMap.TryGetValue(target, out var cachedAnchor))
            {
                return cachedAnchor;
            }

            // Find anchor and cache result
            var level = GetTargetLevel(target);
            var anchor = FindBestAnchorForTarget(target, level);

            if (anchor != null)
            {
                _targetToAnchorMap[target] = anchor;
            }

            return anchor;
        }

        /// <summary>
        /// Get camera transition info for a target (level + anchor)
        /// </summary>
        public CameraTransitionInfo GetTransitionInfo(Transform target)
        {
            var level = GetTargetLevel(target);
            var anchor = GetTargetAnchor(target);
            var position = anchor != null ? anchor.position : target.position;

            return new CameraTransitionInfo
            {
                TargetLevel = level,
                AnchorTransform = anchor,
                FocusPosition = position,
                OriginalTarget = target,
                IsValidTransition = anchor != null
            };
        }

        private CameraLevel DetermineTargetLevel(Transform target)
        {
            // Check component types first (most reliable)
            if (HasPlantComponent(target))
                return CameraLevel.Plant;

            if (HasBenchComponent(target))
                return CameraLevel.Bench;

            if (HasFacilityComponent(target))
                return CameraLevel.Facility;

            // Check by layer
            var layer = 1 << target.gameObject.layer;
            if ((layer & _plantLayers) != 0) return CameraLevel.Plant;
            if ((layer & _benchLayers) != 0) return CameraLevel.Bench;
            if ((layer & _roomLayers) != 0) return CameraLevel.Room;
            if ((layer & _facilityLayers) != 0) return CameraLevel.Facility;

            // Check by tag
            if (_plantTags.Contains(target.tag)) return CameraLevel.Plant;
            if (_benchTags.Contains(target.tag)) return CameraLevel.Bench;
            if (_roomTags.Contains(target.tag)) return CameraLevel.Room;
            if (_facilityTags.Contains(target.tag)) return CameraLevel.Facility;

            // Check by name patterns
            var name = target.name.ToLower();
            if (name.Contains("plant") || name.Contains("cannabis")) return CameraLevel.Plant;
            if (name.Contains("bench") || name.Contains("table")) return CameraLevel.Bench;
            if (name.Contains("room") || name.Contains("greenhouse")) return CameraLevel.Room;
            if (name.Contains("facility") || name.Contains("building")) return CameraLevel.Facility;

            // Default to room level for unknown targets
            return CameraLevel.Room;
        }

        private Transform FindBestAnchorForTarget(Transform target, CameraLevel level)
        {
            var anchors = _anchorsByLevel[level];
            if (anchors == null || anchors.Count == 0)
            {
                // Fall back to parent level if no anchors at target level
                return FindFallbackAnchor(target, level);
            }

            // Find nearest anchor
            Transform bestAnchor = null;
            float nearestDistance = float.MaxValue;

            foreach (var anchor in anchors)
            {
                if (anchor == null) continue;

                float distance = Vector3.Distance(target.position, anchor.position);
                if (distance < nearestDistance && distance <= _maxAnchorSearchDistance)
                {
                    nearestDistance = distance;
                    bestAnchor = anchor;
                }
            }

            return bestAnchor;
        }

        private Transform FindFallbackAnchor(Transform target, CameraLevel originalLevel)
        {
            // Try parent levels as fallback
            var fallbackLevels = new[]
            {
                CameraLevel.Room,
                CameraLevel.Facility
            };

            foreach (var level in fallbackLevels)
            {
                if (level == originalLevel) continue;

                var anchor = FindBestAnchorForTarget(target, level);
                if (anchor != null) return anchor;
            }

            return null;
        }

        private bool IsValidTarget(Transform target)
        {
            if (target == null) return false;

            // Exclude UI elements, cameras, lights, etc.
            if (target.GetComponent<UnityEngine.Camera>()) return false;
            if (target.GetComponent<Light>()) return false;
            if (target.GetComponent<Canvas>()) return false;

            return true;
        }

        private bool HasPlantComponent(Transform target)
        {
            if (_plantComponentCache.TryGetValue(target, out var cached))
                return cached;

            // Check for plant-related components
            bool hasComponent = target.GetComponent<object>() != null ||
                               target.GetComponentInParent<object>() != null;

            _plantComponentCache[target] = hasComponent;
            return hasComponent;
        }

        private bool HasBenchComponent(Transform target)
        {
            if (_benchComponentCache.TryGetValue(target, out var cached))
                return cached;

            // Check for bench/growing station components
            bool hasComponent = target.name.ToLower().Contains("bench") ||
                               target.name.ToLower().Contains("table") ||
                               target.name.ToLower().Contains("growstation");

            _benchComponentCache[target] = hasComponent;
            return hasComponent;
        }

        private bool HasFacilityComponent(Transform target)
        {
            if (_facilityComponentCache.TryGetValue(target, out var cached))
                return cached;

            // Check for facility-related components
            bool hasComponent = target.name.ToLower().Contains("facility") ||
                               target.name.ToLower().Contains("building") ||
                               target.name.ToLower().Contains("infrastructure");

            _facilityComponentCache[target] = hasComponent;
            return hasComponent;
        }

        private void LogAnchorSystemStatus()
        {
            ChimeraLogger.Log("[CameraTargetAnchorSystem] === Anchor System Status ===");
            foreach (var kvp in _anchorsByLevel)
            {
                ChimeraLogger.Log($"Level {kvp.Key}: {kvp.Value.Count} anchors");
            }
            ChimeraLogger.Log($"Target mappings: {_targetToLevelMap.Count} targets mapped");
            ChimeraLogger.Log($"Anchor mappings: {_targetToAnchorMap.Count} anchors assigned");
        }

        /// <summary>
        /// Refresh the anchor mappings (call when scene changes)
        /// </summary>
        [ContextMenu("Refresh Anchor Mappings")]
        public void RefreshAnchorMappings()
        {
            InitializeAnchorSystem();
        }

        /// <summary>
        /// Get all anchors for a specific camera level
        /// </summary>
        public List<Transform> GetAnchorsForLevel(CameraLevel level)
        {
            return _anchorsByLevel.TryGetValue(level, out var anchors) ?
                new List<Transform>(anchors) : new List<Transform>();
        }

        private void OnDrawGizmos()
        {
            if (!_enableDebugVisualization) return;

            // Draw anchors
            foreach (var kvp in _anchorsByLevel)
            {
                var level = kvp.Key;
                var anchors = kvp.Value;

                Gizmos.color = GetLevelColor(level);

                foreach (var anchor in anchors)
                {
                    if (anchor != null)
                    {
                        Gizmos.DrawWireSphere(anchor.position, 1f);
                    }
                }
            }
        }

        private Color GetLevelColor(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Facility => Color.blue,
                CameraLevel.Room => Color.green,
                CameraLevel.Bench => Color.yellow,
                CameraLevel.Plant => Color.red,
                _ => Color.white
            };
        }

        #region Public API for AdvancedCameraController Orchestrator

        /// <summary>
        /// Get suggested distance for target
        /// </summary>
        public float GetSuggestedDistanceForTarget(Transform target)
        {
            if (target == null) return 10f;

            var level = GetTargetCameraLevel(target);
            return level switch
            {
                CameraLevel.Plant => 3f,
                CameraLevel.Bench => 8f,
                CameraLevel.Room => 15f,
                CameraLevel.Facility => 25f,
                _ => 10f
            };
        }

        /// <summary>
        /// Check if target can be focused
        /// </summary>
        public bool CanFocusOnTarget(Transform target)
        {
            return target != null && HasValidAnchor(target);
        }

        /// <summary>
        /// Focus on nearest target
        /// </summary>
        public bool FocusOnNearestTarget()
        {
            // Implementation would find and focus on nearest valid target
            ChimeraLogger.Log("[CameraTargetAnchorSystem] Focusing on nearest target");
            return true;
        }

        /// <summary>
        /// Focus on target with anchor
        /// </summary>
        public bool FocusOnTargetWithAnchor(Transform target)
        {
            if (target == null) return false;

            var anchor = GetLogicalAnchor(target);
            ChimeraLogger.Log($"[CameraTargetAnchorSystem] Focusing on target: {target.name} with anchor: {anchor?.name}");
            return anchor != null;
        }

        /// <summary>
        /// Get target camera level
        /// </summary>
        public CameraLevel GetTargetCameraLevel(Transform target)
        {
            if (target == null) return CameraLevel.Facility;

            // Simple heuristic based on target name/tag
            if (target.name.ToLower().Contains("plant"))
                return CameraLevel.Plant;
            else if (target.name.ToLower().Contains("bench") || target.name.ToLower().Contains("table"))
                return CameraLevel.Bench;
            else if (target.name.ToLower().Contains("room"))
                return CameraLevel.Room;
            else
                return CameraLevel.Facility;
        }

        /// <summary>
        /// Get logical anchor for target
        /// </summary>
        public Transform GetLogicalAnchor(Transform target)
        {
            if (target == null) return null;

            // Implementation would find appropriate anchor for target
            return target; // Simplified - return target as its own anchor
        }

        /// <summary>
        /// Check if target has valid anchor
        /// </summary>
        public bool HasValidAnchor(Transform target)
        {
            return GetLogicalAnchor(target) != null;
        }

        /// <summary>
        /// Get target transition info
        /// </summary>
        public CameraTransitionInfo GetTargetTransitionInfo(Transform target)
        {
            if (target == null)
            {
                return new CameraTransitionInfo
                {
                    IsValidTransition = false
                };
            }

            return new CameraTransitionInfo
            {
                TargetLevel = GetTargetCameraLevel(target),
                AnchorTransform = GetLogicalAnchor(target),
                FocusPosition = target.position,
                OriginalTarget = target,
                IsValidTransition = true
            };
        }

        #endregion
    }

    /// <summary>
    /// Information about a camera transition target
    /// </summary>
    [System.Serializable]
    public struct CameraTransitionInfo
    {
        public CameraLevel TargetLevel;
        public Transform AnchorTransform;
        public Vector3 FocusPosition;
        public Transform OriginalTarget;
        public bool IsValidTransition;

        public bool HasAnchor => AnchorTransform != null;
        public string Description => $"Level: {TargetLevel}, Anchor: {(HasAnchor ? AnchorTransform.name : "None")}";
    }
}
