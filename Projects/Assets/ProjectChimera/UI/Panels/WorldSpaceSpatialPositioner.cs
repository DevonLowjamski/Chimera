using UnityEngine;
using System.Collections.Generic;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Advanced spatial positioning system for world space contextual menus.
    /// Implements intelligent positioning algorithms to optimize menu placement in 3D space
    /// for cannabis cultivation facility management with collision avoidance and visibility optimization.
    /// </summary>
    public class WorldSpaceSpatialPositioner : MonoBehaviour
    {
        [Header("Positioning Configuration")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private LayerMask _obstacleLayer = 1 << 0; // Default layer
        [SerializeField] private LayerMask _facilityLayer = 1 << 8; // Facility layer
        
        [Header("Spatial Parameters")]
        [SerializeField] private float _preferredDistance = 2.0f;
        [SerializeField] private float _minDistance = 1.0f;
        [SerializeField] private float _maxDistance = 5.0f;
        [SerializeField] private float _verticalOffset = 1.5f;
        [SerializeField] private float _horizontalSpread = 1.0f;
        
        [Header("Collision Avoidance")]
        [SerializeField] private float _collisionRadius = 0.5f;
        [SerializeField] private int _positionCandidates = 8;
        [SerializeField] private float _avoidanceStrength = 2.0f;
        [SerializeField] private bool _enableSmartPositioning = true;
        
        [Header("Visibility Optimization")]
        [SerializeField] private bool _enableVisibilityOptimization = true;
        [SerializeField] private float _visibilityRayLength = 10.0f;
        [SerializeField] private int _visibilityTestPoints = 4;
        
        // Spatial tracking
        private readonly Dictionary<GameObject, Vector3> _lastValidPositions = new Dictionary<GameObject, Vector3>();
        private readonly Dictionary<GameObject, SpatialPositionData> _positionData = new Dictionary<GameObject, SpatialPositionData>();
        
        // Position evaluation
        private readonly SpatialPositionEvaluator _evaluator = new SpatialPositionEvaluator();
        
        private void Awake()
        {
            if (_targetCamera == null)
                _targetCamera = Camera.main;
            
            // Initialize evaluator
            _evaluator.Initialize(_targetCamera, _obstacleLayer, _facilityLayer, 
                                _preferredDistance, _maxDistance, _collisionRadius, _avoidanceStrength);
        }
        
        /// <summary>
        /// Calculates optimal spatial position for a menu relative to target object
        /// </summary>
        public Vector3 CalculateOptimalPosition(GameObject target, WorldSpaceMenuType menuType, Vector3 menuSize)
        {
            if (target == null || _targetCamera == null)
                return target?.transform.position ?? Vector3.zero;
            
            var targetPosition = target.transform.position;
            var cameraPosition = _targetCamera.transform.position;
            
            // Get or create position data
            if (!_positionData.TryGetValue(target, out var data))
            {
                data = new SpatialPositionData
                {
                    Target = target,
                    MenuType = menuType,
                    LastUpdateTime = Time.time
                };
                _positionData[target] = data;
            }
            
            Vector3 optimalPosition;
            
            if (_enableSmartPositioning)
            {
                optimalPosition = CalculateSmartPosition(targetPosition, cameraPosition, menuSize, data);
            }
            else
            {
                optimalPosition = CalculateBasicPosition(targetPosition, cameraPosition);
            }
            
            // Validate and refine position
            optimalPosition = _evaluator.ValidatePosition(optimalPosition, targetPosition, menuSize, _minDistance, _maxDistance);
            
            // Store as last valid position
            _lastValidPositions[target] = optimalPosition;
            data.LastPosition = optimalPosition;
            data.LastUpdateTime = Time.time;
            
            return optimalPosition;
        }
        
        /// <summary>
        /// Calculates smart position using multiple algorithms and candidate evaluation
        /// </summary>
        private Vector3 CalculateSmartPosition(Vector3 targetPos, Vector3 cameraPos, Vector3 menuSize, SpatialPositionData data)
        {
            var candidates = _evaluator.GeneratePositionCandidates(targetPos, cameraPos, menuSize,
                _preferredDistance, _minDistance, _maxDistance, _verticalOffset, _horizontalSpread, _positionCandidates);
            
            float bestScore = float.MinValue;
            Vector3 bestPosition = targetPos + Vector3.up * _verticalOffset;
            
            foreach (var candidate in candidates)
            {
                float score = _evaluator.EvaluatePositionScore(candidate, targetPos, cameraPos, menuSize, data, _enableVisibilityOptimization);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = candidate;
                }
            }
            
            return bestPosition;
        }
        
        /// <summary>
        /// Calculates basic position using simple offset from target
        /// </summary>
        private Vector3 CalculateBasicPosition(Vector3 targetPos, Vector3 cameraPos)
        {
            var toCameraDirection = (cameraPos - targetPos).normalized;
            return targetPos + toCameraDirection * _preferredDistance + Vector3.up * _verticalOffset;
        }
        
        /// <summary>
        /// Gets the spatial positioning configuration
        /// </summary>
        public SpatialPositioningConfig GetConfiguration()
        {
            return new SpatialPositioningConfig
            {
                PreferredDistance = _preferredDistance,
                MinDistance = _minDistance,
                MaxDistance = _maxDistance,
                VerticalOffset = _verticalOffset,
                HorizontalSpread = _horizontalSpread,
                CollisionRadius = _collisionRadius,
                PositionCandidates = _positionCandidates,
                EnableSmartPositioning = _enableSmartPositioning,
                EnableVisibilityOptimization = _enableVisibilityOptimization
            };
        }
        
        /// <summary>
        /// Updates the spatial positioning configuration
        /// </summary>
        public void UpdateConfiguration(SpatialPositioningConfig config)
        {
            _preferredDistance = config.PreferredDistance;
            _minDistance = config.MinDistance;
            _maxDistance = config.MaxDistance;
            _verticalOffset = config.VerticalOffset;
            _horizontalSpread = config.HorizontalSpread;
            _collisionRadius = config.CollisionRadius;
            _positionCandidates = config.PositionCandidates;
            _enableSmartPositioning = config.EnableSmartPositioning;
            _enableVisibilityOptimization = config.EnableVisibilityOptimization;
        }
        
        /// <summary>
        /// Clears position data for a target
        /// </summary>
        public void ClearPositionData(GameObject target)
        {
            _lastValidPositions.Remove(target);
            _positionData.Remove(target);
        }
        
        /// <summary>
        /// Gets spatial statistics for debugging
        /// </summary>
        public SpatialStats GetSpatialStats()
        {
            return new SpatialStats
            {
                TrackedTargets = _positionData.Count,
                CachedPositions = _evaluator.GetCacheSize(),
                LastCacheUpdate = Time.time
            };
        }
    }
    
    /// <summary>
    /// Data for tracking spatial positioning of a menu
    /// </summary>
    public class SpatialPositionData
    {
        public GameObject Target { get; set; }
        public WorldSpaceMenuType MenuType { get; set; }
        public Vector3 LastPosition { get; set; }
        public float LastUpdateTime { get; set; }
        public int PositionChangeCount { get; set; }
    }
    
    /// <summary>
    /// Configuration for spatial positioning system
    /// </summary>
    [System.Serializable]
    public class SpatialPositioningConfig
    {
        public float PreferredDistance = 2.0f;
        public float MinDistance = 1.0f;
        public float MaxDistance = 5.0f;
        public float VerticalOffset = 1.5f;
        public float HorizontalSpread = 1.0f;
        public float CollisionRadius = 0.5f;
        public int PositionCandidates = 8;
        public bool EnableSmartPositioning = true;
        public bool EnableVisibilityOptimization = true;
    }
    
    /// <summary>
    /// Statistics about spatial positioning system
    /// </summary>
    public class SpatialStats
    {
        public int TrackedTargets { get; set; }
        public int CachedPositions { get; set; }
        public float LastCacheUpdate { get; set; }
    }
}