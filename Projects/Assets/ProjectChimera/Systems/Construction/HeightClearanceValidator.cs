using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Specialized validator for height clearance and vertical space validation.
    /// Handles clearance calculations, access path validation, and vertical obstruction checking.
    /// </summary>
    public class HeightClearanceValidator : MonoBehaviour
    {
        [Header("Height Clearance Settings")]
        [SerializeField] private float _minimumHeightClearance = 2f; // Minimum vertical clearance
        [SerializeField] private float _accessPathClearance = 1.5f; // Clearance needed for access
        [SerializeField] private bool _enforceAccessPaths = true;
        [SerializeField] private float _workingSpaceClearance = 0.5f; // Extra space around objects
        
        [Header("Access Validation")]
        [SerializeField] private bool _requireHorizontalAccess = true;
        [SerializeField] private float _minimumAccessWidth = 1f; // Minimum access corridor width
        [SerializeField] private int _accessCheckRadius = 2; // How far to check for access paths
        [SerializeField] private LayerMask _accessObstacleLayers = -1;
        
        [Header("Clearance Calculation")]
        [SerializeField] private bool _checkDiagonalClearance = true;
        [SerializeField] private float _clearanceCheckRadius = 2f; // Radius to check around object
        [SerializeField] private bool _warnOnLimitedClearance = true;
        [SerializeField] private float _clearanceWarningThreshold = 1f; // Warn if clearance < this
        
        // Core references
        private GridSystem _gridSystem;
        
        // Height clearance events
        public System.Action<Vector3Int> OnHeightClearanceFailed;
        public System.Action<Vector3Int> OnAccessPathBlocked;
        public System.Action<Vector3Int, float> OnLimitedClearance;
        
        private void Awake()
        {
            _gridSystem = FindObjectOfType<GridSystem>();
            if (_gridSystem == null)
                Debug.LogWarning($"[HeightClearanceValidator] GridSystem not found - clearance validation may not work properly");
        }
        
        #region Main Validation API
        
        /// <summary>
        /// Validate height clearance for object placement
        /// </summary>
        public void ValidateHeightClearance(GridPlaceable placeable, Vector3Int gridPosition, PlacementValidationResult result)
        {
            if (_gridSystem == null) return;
            
            // Check vertical clearance above
            float verticalClearance = CalculateVerticalClearance(gridPosition, placeable.GridSize);
            
            if (verticalClearance < _minimumHeightClearance)
            {
                result.AddError($"Insufficient vertical clearance: {verticalClearance:F1}m (minimum: {_minimumHeightClearance}m)");
                OnHeightClearanceFailed?.Invoke(gridPosition);
                return;
            }
            else if (_warnOnLimitedClearance && verticalClearance < _clearanceWarningThreshold)
            {
                result.AddWarning($"Limited vertical clearance: {verticalClearance:F1}m");
                OnLimitedClearance?.Invoke(gridPosition, verticalClearance);
            }
            
            // Check horizontal access if required
            if (_requireHorizontalAccess)
            {
                if (!HasAdequateHorizontalAccess(gridPosition, placeable.GridSize))
                {
                    result.AddError("No adequate horizontal access path found");
                    OnAccessPathBlocked?.Invoke(gridPosition);
                }
            }
            
            // Check working space clearance
            if (!HasAdequateWorkingSpace(gridPosition, placeable.GridSize))
            {
                result.AddWarning("Limited working space around object");
            }
            
            // Check access path clearance
            if (_enforceAccessPaths)
            {
                ValidateAccessPaths(gridPosition, placeable.GridSize, result);
            }
        }
        
        /// <summary>
        /// Calculate available vertical clearance at position
        /// </summary>
        public float CalculateVerticalClearance(Vector3Int gridPosition, Vector3Int size)
        {
            if (_gridSystem == null) return 0f;
            
            float minClearance = float.MaxValue;
            
            // Check clearance for each cell of the object
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int checkPos = new Vector3Int(
                        gridPosition.x + x,
                        gridPosition.y + y,
                        gridPosition.z + size.z // Top of the object
                    );
                    
                    float clearance = CalculateClearanceAtPosition(checkPos);
                    minClearance = Mathf.Min(minClearance, clearance);
                }
            }
            
            return minClearance == float.MaxValue ? 0f : minClearance;
        }
        
        /// <summary>
        /// Check if there are adequate access paths around the object
        /// </summary>
        public bool HasAdequateAccess(Vector3Int gridPosition, Vector3Int size)
        {
            return HasAdequateHorizontalAccess(gridPosition, size) && 
                   HasAdequateVerticalAccess(gridPosition);
        }
        
        #endregion
        
        #region Clearance Calculations
        
        private float CalculateClearanceAtPosition(Vector3Int position)
        {
            // Check for obstacles above
            for (int z = position.z; z < _gridSystem.MaxHeightLevels; z++)
            {
                if (_gridSystem.GetGridCell(new Vector3Int(position.x, position.y, z))?.IsOccupied == true)
                {
                    return _gridSystem.GetWorldYForHeightLevel(z) - _gridSystem.GetWorldYForHeightLevel(position.z);
                }
            }
            
            return _gridSystem.GetWorldYForHeightLevel(_gridSystem.MaxHeightLevels - 1);
        }
        
        private bool HasAdequateWorkingSpace(Vector3Int gridPosition, Vector3Int size)
        {
            // Check border around object for working space
            for (int x = -1; x <= size.x; x++)
            {
                for (int y = -1; y <= size.y; y++)
                {
                    // Only check perimeter cells
                    if ((x == -1 || x == size.x || y == -1 || y == size.y) && 
                        !(x >= 0 && x < size.x && y >= 0 && y < size.y))
                    {
                        Vector3Int checkPos = new Vector3Int(gridPosition.x + x, gridPosition.y + y, gridPosition.z);
                        
                        if (_gridSystem.IsValidGridPosition(checkPos) && 
                            _gridSystem.GetGridCell(checkPos)?.IsOccupied != true)
                        {
                            return true; // Found available working space
                        }
                    }
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Access Path Validation
        
        private bool HasAdequateHorizontalAccess(Vector3Int gridPosition, Vector3Int size)
        {
            // Check each side of the object for access paths
            return CheckNorthAccess(gridPosition, size) ||
                   CheckSouthAccess(gridPosition, size) ||
                   CheckEastAccess(gridPosition, size) ||
                   CheckWestAccess(gridPosition, size);
        }
        
        private bool CheckNorthAccess(Vector3Int gridPosition, Vector3Int size)
        {
            return CheckDirectionalAccess(gridPosition.x, gridPosition.y + size.y, size.x, true);
        }
        
        private bool CheckSouthAccess(Vector3Int gridPosition, Vector3Int size)
        {
            return CheckDirectionalAccess(gridPosition.x, gridPosition.y - 1, size.x, true);
        }
        
        private bool CheckEastAccess(Vector3Int gridPosition, Vector3Int size)
        {
            return CheckDirectionalAccess(gridPosition.x + size.x, gridPosition.y, size.y, false);
        }
        
        private bool CheckWestAccess(Vector3Int gridPosition, Vector3Int size)
        {
            return CheckDirectionalAccess(gridPosition.x - 1, gridPosition.y, size.y, false);
        }
        
        private bool CheckDirectionalAccess(int startX, int startY, int length, bool horizontal)
        {
            int accessWidth = Mathf.RoundToInt(_minimumAccessWidth);
            
            for (int w = 0; w < accessWidth; w++)
            {
                for (int l = 0; l < length; l++)
                {
                    Vector3Int checkPos = horizontal ? 
                        new Vector3Int(startX + l, startY + w, 0) : 
                        new Vector3Int(startX + w, startY + l, 0);
                    
                    if (!_gridSystem.IsValidGridPosition(checkPos) || 
                        _gridSystem.GetGridCell(checkPos)?.IsOccupied == true)
                        return false;
                }
            }
            return true;
        }
        
        private bool HasAdequateVerticalAccess(Vector3Int gridPosition)
        {
            if (gridPosition.z == 0) return true; // Ground level accessible
            
            // Check for continuous vertical support down to ground
            for (int z = gridPosition.z - 1; z >= 0; z--)
            {
                if (_gridSystem.GetGridCell(new Vector3Int(gridPosition.x, gridPosition.y, z))?.IsOccupied != true)
                    return false; // Gap in vertical access
            }
            
            return true;
        }
        
        private void ValidateAccessPaths(Vector3Int gridPosition, Vector3Int size, PlacementValidationResult result)
        {
            bool hasValidAccess = HasPerimeterAccess(gridPosition, size);
            
            if (!hasValidAccess)
            {
                result.AddWarning("Limited access paths - maintenance may be difficult");
            }
        }
        
        private bool HasPerimeterAccess(Vector3Int gridPosition, Vector3Int size)
        {
            // Check perimeter for at least one clear access point
            for (int x = -1; x <= size.x; x++)
            {
                if (IsAccessPointClear(new Vector3Int(gridPosition.x + x, gridPosition.y - 1, gridPosition.z)) ||
                    IsAccessPointClear(new Vector3Int(gridPosition.x + x, gridPosition.y + size.y, gridPosition.z)))
                    return true;
            }
            
            for (int y = 0; y < size.y; y++)
            {
                if (IsAccessPointClear(new Vector3Int(gridPosition.x - 1, gridPosition.y + y, gridPosition.z)) ||
                    IsAccessPointClear(new Vector3Int(gridPosition.x + size.x, gridPosition.y + y, gridPosition.z)))
                    return true;
            }
            
            return false;
        }
        
        private bool IsAccessPointClear(Vector3Int accessPoint)
        {
            return _gridSystem.IsValidGridPosition(accessPoint) && 
                   _gridSystem.GetGridCell(accessPoint)?.IsOccupied != true;
        }
        
        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// Get clearance information for UI display
        /// </summary>
        public string GetClearanceInfo(Vector3Int gridPosition, Vector3Int size)
        {
            float clearance = CalculateVerticalClearance(gridPosition, size);
            bool hasAccess = HasAdequateAccess(gridPosition, size);
            return $"Clearance: {clearance:F1}m | Access: {(hasAccess ? "OK" : "Limited")}";
        }
        
        /// <summary>
        /// Check if position has adequate clearance for specific activity
        /// </summary>
        public bool HasClearanceForActivity(Vector3Int gridPosition, float requiredClearance)
        {
            return CalculateClearanceAtPosition(gridPosition) >= requiredClearance;
        }
        
        #endregion
    }
}