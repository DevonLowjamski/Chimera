using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Core 3D validation system for grid-based construction placement.
    /// Handles collision detection, foundation requirements, and basic placement validation.
    /// Delegates complex validation to specialized validator components.
    /// </summary>
    public class GridPlacementValidator : MonoBehaviour
    {
        [Header("3D Validation Settings")]
        [SerializeField] private bool _enableSpaceValidation = true;
        [SerializeField] private bool _enableFoundationValidation = true;
        [SerializeField] private bool _enableCollisionValidation = true;
        [SerializeField] private bool _enableHeightValidation = true;
        [SerializeField] private bool _requireFoundationForElevatedObjects = true;
        [SerializeField] private LayerMask _collisionLayers = -1;
        [SerializeField] private float _collisionTolerance = 0.1f;
        [SerializeField] private bool _usePhysicsOverlapBox = true;
        [SerializeField] private float _maximumPlacementHeight = 50f;
        [SerializeField] private bool _enforceAccessibleHeight = true;
        [SerializeField] private float _accessibleHeightLimit = 20f;
        [SerializeField] private bool _enableValidationCaching = true;
        [SerializeField] private float _cacheExpireTime = 1f;
        
        // Core references
        private GridSystem _gridSystem;
        private StructuralIntegrityValidator _structuralValidator;
        private HeightClearanceValidator _heightClearanceValidator;
        
        // Validation cache for performance
        private Dictionary<ValidatorCacheKey, ValidatorCacheEntry> _validationCache = new Dictionary<ValidatorCacheKey, ValidatorCacheEntry>();
        
        // 3D validation events
        public System.Action<Vector3Int, PlacementValidationResult> OnValidationCompleted;
        public System.Action<Vector3Int, PlacementValidationResult> OnValidationFailed;
        public System.Action<Vector3Int> OnFoundationValidationFailed;
        public System.Action<Vector3Int> OnCollisionDetected;
        
        private void Awake()
        {
            _validationCache.Clear();
        }
        
        private void Start()
        {
            _gridSystem = FindObjectOfType<GridSystem>();
            _structuralValidator = GetComponent<StructuralIntegrityValidator>();
            _heightClearanceValidator = GetComponent<HeightClearanceValidator>();
        }
        
        private void Update() => CleanExpiredCache();
        
        /// <summary>
        /// Comprehensive 3D placement validation for single object
        /// </summary>
        public PlacementValidationResult ValidateObjectPlacement(GridPlaceable placeable, Vector3Int gridPosition)
        {
            if (placeable == null) { var r = new PlacementValidationResult(); r.AddError("Placeable object is null"); return r; }
            if (_gridSystem == null) { var r = new PlacementValidationResult(); r.AddError("Grid system not available"); return r; }
            
            var result = new PlacementValidationResult
            {
                ValidatedPosition = gridPosition,
                ValidatedObject = placeable,
                ValidationTime = Time.time
            };
            
            // Check cache first
            if (_enableValidationCaching && TryGetCachedResult(placeable, gridPosition, out var cached))
                return cached;
            
            // Core validations
            ValidateGridBounds(gridPosition, placeable.GridSize, result);
            if (_enableSpaceValidation && result.IsValid) ValidateSpaceAvailability(gridPosition, placeable.GridSize, result);
            if (_enableFoundationValidation && result.IsValid) ValidateFoundationRequirements(placeable, gridPosition, result);
            if (_enableCollisionValidation && result.IsValid) ValidateCollisions(placeable, gridPosition, result);
            if (_enableHeightValidation && result.IsValid) ValidateHeightLimits(placeable, gridPosition, result);
            
            // Specialized validators
            if (result.IsValid && _structuralValidator != null)
                _structuralValidator.ValidateStructuralIntegrity(placeable, gridPosition, result);
            if (result.IsValid && _heightClearanceValidator != null)
                _heightClearanceValidator.ValidateHeightClearance(placeable, gridPosition, result);
            
            // Cache and notify
            if (_enableValidationCaching && result.IsValid) CacheValidationResult(placeable, gridPosition, result);
            
            // Store last result
            LastValidationResult = result;
            
            if (result.IsValid) OnValidationCompleted?.Invoke(gridPosition, result);
            else OnValidationFailed?.Invoke(gridPosition, result);
            
            return result;
        }
        
        /// <summary>
        /// Quick validation check for UI feedback
        /// </summary>
        public bool QuickValidatePosition(Vector3Int gridPosition, Vector3Int size)
        {
            return _gridSystem != null && _gridSystem.IsValidGridPosition(gridPosition) && _gridSystem.IsAreaAvailable(gridPosition, size);
        }
        
        /// <summary>
        /// Simple boolean validation check for placement
        /// </summary>
        public bool ValidatePosition(Vector3Int gridPosition, GridPlaceable placeable)
        {
            if (placeable == null || _gridSystem == null) return false;
            return QuickValidatePosition(gridPosition, placeable.GridSize);
        }
        
        /// <summary>
        /// Validate 3D area availability with Vector3Int coordinates
        /// </summary>
        public bool ValidateAreaAvailability(Vector3Int gridPosition, Vector3Int size)
        {
            return _gridSystem?.IsAreaAvailable(gridPosition, size) ?? false;
        }
        
        /// <summary>
        /// Property to access the last validation result
        /// </summary>
        public PlacementValidationResult LastValidationResult { get; private set; }
        
        /// <summary>
        /// Get summary of validation results
        /// </summary>
        public string GetValidationSummary()
        {
            if (LastValidationResult == null) return "No validation performed";
            return LastValidationResult.IsValid ? "Validation passed" : $"Validation failed: {LastValidationResult.GetErrorSummary()}";
        }
        
        
        private void ValidateGridBounds(Vector3Int gridPosition, Vector3Int size, PlacementValidationResult result)
        {
            if (_gridSystem == null)
            {
                result.AddError("Grid system not available");
                return;
            }
            
            // Check if all cells are within bounds
            for (int x = 0; x < size.x && result.IsValid; x++)
            {
                for (int y = 0; y < size.y && result.IsValid; y++)
                {
                    for (int z = 0; z < size.z && result.IsValid; z++)
                    {
                        if (!_gridSystem.IsValidGridPosition(gridPosition + new Vector3Int(x, y, z)))
                        {
                            result.AddError($"Position out of grid bounds");
                        }
                    }
                }
            }
        }
        
        private void ValidateSpaceAvailability(Vector3Int gridPosition, Vector3Int size, PlacementValidationResult result)
        {
            if (_gridSystem == null) return;
            
            var occupied = new List<Vector3Int>();
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int pos = gridPosition + new Vector3Int(x, y, z);
                        if (_gridSystem.GetGridCell(pos)?.IsOccupied == true)
                            occupied.Add(pos);
                    }
                }
            }
            
            if (occupied.Count > 0)
            {
                result.AddError($"Space occupied: {occupied.Count} cells in use");
                result.ConflictingPositions = occupied;
            }
        }
        
        private void ValidateFoundationRequirements(GridPlaceable placeable, Vector3Int gridPosition, PlacementValidationResult result)
        {
            if (!_requireFoundationForElevatedObjects || gridPosition.z == 0) return;
            
            if (placeable.RequiresFoundation || _gridSystem.RequiresFoundation(gridPosition))
            {
                var missing = new List<Vector3Int>();
                for (int x = 0; x < placeable.GridSize.x; x++)
                {
                    for (int y = 0; y < placeable.GridSize.y; y++)
                    {
                        Vector3Int pos = new Vector3Int(gridPosition.x + x, gridPosition.y + y, gridPosition.z - 1);
                        if (!HasAdequateFoundation(pos))
                            missing.Add(pos);
                    }
                }
                
                if (missing.Count > 0)
                {
                    result.AddError($"Missing foundation support at {missing.Count} positions");
                    result.RequiredFoundations = missing;
                    OnFoundationValidationFailed?.Invoke(gridPosition);
                }
            }
        }
        
        private void ValidateCollisions(GridPlaceable placeable, Vector3Int gridPosition, PlacementValidationResult result)
        {
            if (!_usePhysicsOverlapBox) return;
            
            Vector3 worldPos = _gridSystem.GridToWorldPosition(gridPosition);
            Vector3 size = new Vector3(
                placeable.GridSize.x * _gridSystem.GridSize,
                placeable.GridSize.z * _gridSystem.HeightLevelSpacing,
                placeable.GridSize.y * _gridSystem.GridSize
            );
            
            var overlapping = Physics.OverlapBox(
                worldPos + size * 0.5f,
                size * 0.5f - Vector3.one * _collisionTolerance,
                Quaternion.identity, _collisionLayers, QueryTriggerInteraction.Ignore
            );
            
            var conflicts = overlapping.Where(c => c.gameObject != placeable.gameObject).ToList();
            if (conflicts.Count > 0)
            {
                result.AddError($"Collision detected with {conflicts.Count} objects");
                OnCollisionDetected?.Invoke(gridPosition);
            }
        }
        
        private void ValidateHeightLimits(GridPlaceable placeable, Vector3Int gridPosition, PlacementValidationResult result)
        {
            float height = _gridSystem.GetWorldYForHeightLevel(gridPosition.z);
            if (height > _maximumPlacementHeight)
                result.AddError($"Height {height}m exceeds maximum {_maximumPlacementHeight}m");
            else if (_enforceAccessibleHeight && height > _accessibleHeightLimit)
                result.AddWarning($"Height {height}m may be difficult to access");
        }
        
        private bool HasAdequateFoundation(Vector3Int pos)
        {
            var cell = _gridSystem?.GetGridCell(pos);
            return cell?.IsOccupied == true && cell.OccupyingObject != null && 
                   (cell.OccupyingObject.Type == PlaceableType.Structure || cell.OccupyingObject.Type == PlaceableType.Equipment);
        }
        
        
        private void CleanExpiredCache()
        {
            if (!_enableValidationCaching) return;
            
            var expired = _validationCache.Keys
                .Where(key => Time.time - _validationCache[key].Timestamp > _cacheExpireTime)
                .ToList();
            
            foreach (var key in expired)
                _validationCache.Remove(key);
        }
        
        private bool TryGetCachedResult(GridPlaceable placeable, Vector3Int position, out PlacementValidationResult result)
        {
            var key = new ValidatorCacheKey(placeable.GetInstanceID(), position);
            if (_validationCache.TryGetValue(key, out ValidatorCacheEntry entry) && 
                Time.time - entry.Timestamp < _cacheExpireTime)
            {
                result = entry.Result;
                return true;
            }
            
            result = null;
            return false;
        }
        
        private void CacheValidationResult(GridPlaceable placeable, Vector3Int position, PlacementValidationResult result)
        {
            var key = new ValidatorCacheKey(placeable.GetInstanceID(), position);
            _validationCache[key] = new ValidatorCacheEntry { Result = result, Timestamp = Time.time };
        }
        
        #region Internal Cache Types
        
        internal struct ValidatorCacheKey
        {
            public int ObjectInstanceId;
            public Vector3Int Position;
            public ValidatorCacheKey(int instanceId, Vector3Int position) { ObjectInstanceId = instanceId; Position = position; }
        }
        
        internal class ValidatorCacheEntry
        {
            public PlacementValidationResult Result;
            public float Timestamp;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Shared data structure for validation results across all validator types
    /// </summary>
    [System.Serializable]
    public class PlacementValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        public List<Vector3Int> ConflictingPositions = new List<Vector3Int>();
        public List<Vector3Int> RequiredFoundations = new List<Vector3Int>();
        public Vector3Int ValidatedPosition;
        public GridPlaceable ValidatedObject;
        public float ValidationTime;
        
        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
        public string GetErrorSummary() => string.Join("; ", Errors);
        public string GetWarningSummary() => string.Join("; ", Warnings);
    }
}