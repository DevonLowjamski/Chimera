using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Evaluates and scores spatial positions for world space menus.
    /// Extracted from WorldSpaceSpatialPositioner.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class SpatialPositionEvaluator
    {
        private readonly Dictionary<Vector3, float> _scoreCache = new Dictionary<Vector3, float>();
        private float _cacheUpdateTime = 0f;
        private const float CacheLifetime = 1.0f;
        
        private Camera _camera;
        private LayerMask _obstacleLayer;
        private LayerMask _facilityLayer;
        private float _preferredDistance;
        private float _maxDistance;
        private float _collisionRadius;
        private float _avoidanceStrength;
        
        public void Initialize(Camera camera, LayerMask obstacleLayer, LayerMask facilityLayer, 
                              float preferredDistance, float maxDistance, float collisionRadius, float avoidanceStrength)
        {
            _camera = camera;
            _obstacleLayer = obstacleLayer;
            _facilityLayer = facilityLayer;
            _preferredDistance = preferredDistance;
            _maxDistance = maxDistance;
            _collisionRadius = collisionRadius;
            _avoidanceStrength = avoidanceStrength;
        }
        
        /// <summary>
        /// Evaluates the quality score of a position candidate
        /// </summary>
        public float EvaluatePositionScore(Vector3 position, Vector3 targetPos, Vector3 cameraPos, 
                                         Vector3 menuSize, SpatialPositionData data, bool enableVisibilityOptimization)
        {
            // Check cache first
            UpdateCache();
            if (_scoreCache.TryGetValue(position, out var cachedScore))
            {
                return cachedScore;
            }
            
            float score = 0f;
            
            // Distance preference score
            float distance = Vector3.Distance(position, cameraPos);
            float distanceScore = Mathf.Lerp(1f, 0f, Mathf.Abs(distance - _preferredDistance) / _maxDistance);
            score += distanceScore * 2f;
            
            // Visibility score
            if (enableVisibilityOptimization)
            {
                float visibilityScore = CalculateVisibilityScore(position, cameraPos);
                score += visibilityScore * 3f;
            }
            
            // Collision avoidance score
            float collisionScore = CalculateCollisionAvoidanceScore(position, menuSize);
            score += collisionScore * 2f;
            
            // Camera facing preference
            var toCamera = (cameraPos - position).normalized;
            var menuForward = (position - targetPos).normalized;
            float facingScore = Vector3.Dot(toCamera, menuForward);
            score += facingScore;
            
            // Stability bonus (prefer positions close to last valid position)
            if (data != null && data.LastPosition != Vector3.zero)
            {
                float stabilityDistance = Vector3.Distance(position, data.LastPosition);
                float stabilityScore = Mathf.Lerp(1f, 0f, stabilityDistance / 2f);
                score += stabilityScore * 0.5f;
            }
            
            // Cache the result
            _scoreCache[position] = score;
            
            return score;
        }
        
        /// <summary>
        /// Calculates visibility score based on occlusion and view frustum
        /// </summary>
        private float CalculateVisibilityScore(Vector3 position, Vector3 cameraPos)
        {
            if (_camera == null) return 0.5f;
            
            float score = 1f;
            
            // Check line of sight
            var direction = (position - cameraPos).normalized;
            var distance = Vector3.Distance(position, cameraPos);
            
            if (Physics.Raycast(cameraPos, direction, distance, _obstacleLayer))
            {
                score *= 0.1f; // Heavy penalty for occlusion
            }
            
            // Check if within camera frustum
            var screenPoint = _camera.WorldToViewportPoint(position);
            bool inFrustum = screenPoint.x >= 0 && screenPoint.x <= 1 && 
                           screenPoint.y >= 0 && screenPoint.y <= 1 && 
                           screenPoint.z > 0;
            
            if (!inFrustum)
            {
                score *= 0.3f; // Penalty for being outside frustum
            }
            
            // Prefer positions closer to screen center
            var centerDistance = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), Vector2.one * 0.5f);
            var centerScore = Mathf.Lerp(1f, 0.7f, centerDistance * 2f);
            score *= centerScore;
            
            return score;
        }
        
        /// <summary>
        /// Calculates collision avoidance score
        /// </summary>
        private float CalculateCollisionAvoidanceScore(Vector3 position, Vector3 menuSize)
        {
            // Check for overlapping colliders
            var bounds = new Bounds(position, menuSize + Vector3.one * _collisionRadius);
            var overlapping = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, _facilityLayer);
            
            if (overlapping.Length > 0)
            {
                return 0.1f; // Low score for collision
            }
            
            // Check sphere around position for nearby objects
            var nearbyObjects = Physics.OverlapSphere(position, _avoidanceStrength, _facilityLayer);
            float avoidanceScore = Mathf.Lerp(1f, 0.5f, nearbyObjects.Length / 5f);
            
            return avoidanceScore;
        }
        
        /// <summary>
        /// Generates multiple position candidates around the target
        /// </summary>
        public List<Vector3> GeneratePositionCandidates(Vector3 targetPos, Vector3 cameraPos, Vector3 menuSize,
                                                       float preferredDistance, float minDistance, float maxDistance,
                                                       float verticalOffset, float horizontalSpread, int candidateCount)
        {
            var candidates = new List<Vector3>();
            var toCameraDirection = (cameraPos - targetPos).normalized;
            
            // Primary position (towards camera)
            var primaryPos = targetPos + toCameraDirection * preferredDistance + Vector3.up * verticalOffset;
            candidates.Add(primaryPos);
            
            // Circular positions around target
            for (int i = 0; i < candidateCount; i++)
            {
                float angle = (360f / candidateCount) * i * Mathf.Deg2Rad;
                var right = Vector3.Cross(toCameraDirection, Vector3.up).normalized;
                var forward = Vector3.Cross(Vector3.up, right).normalized;
                
                var circularOffset = (right * Mathf.Sin(angle) + forward * Mathf.Cos(angle)) * horizontalSpread;
                var candidatePos = targetPos + circularOffset + Vector3.up * verticalOffset;
                
                candidates.Add(candidatePos);
            }
            
            // Distance variations
            var distances = new float[] { minDistance, preferredDistance, maxDistance };
            foreach (var distance in distances)
            {
                var distancePos = targetPos + toCameraDirection * distance + Vector3.up * verticalOffset;
                candidates.Add(distancePos);
            }
            
            return candidates;
        }
        
        /// <summary>
        /// Validates and adjusts position to ensure it's within acceptable bounds
        /// </summary>
        public Vector3 ValidatePosition(Vector3 position, Vector3 targetPos, Vector3 menuSize, 
                                      float minDistance, float maxDistance)
        {
            // Ensure minimum distance from target
            var distanceToTarget = Vector3.Distance(position, targetPos);
            if (distanceToTarget < minDistance)
            {
                var direction = (position - targetPos).normalized;
                position = targetPos + direction * minDistance;
            }
            
            // Ensure maximum distance from target
            if (distanceToTarget > maxDistance)
            {
                var direction = (position - targetPos).normalized;
                position = targetPos + direction * maxDistance;
            }
            
            // Keep above ground (assuming y=0 is ground level)
            if (position.y < 0.5f)
            {
                position.y = 0.5f;
            }
            
            return position;
        }
        
        /// <summary>
        /// Updates position cache for performance optimization
        /// </summary>
        private void UpdateCache()
        {
            if (Time.time - _cacheUpdateTime > CacheLifetime)
            {
                _scoreCache.Clear();
                _cacheUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public int GetCacheSize()
        {
            return _scoreCache.Count;
        }
        
        /// <summary>
        /// Clears the evaluation cache
        /// </summary>
        public void ClearCache()
        {
            _scoreCache.Clear();
            _cacheUpdateTime = Time.time;
        }
    }
}