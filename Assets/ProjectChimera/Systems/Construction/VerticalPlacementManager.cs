using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Manages vertical placement logic including stacking, foundation requirements, and weight validation.
    /// Handles multi-level construction with structural integrity checks and cascading removal.
    /// </summary>
    public class VerticalPlacementManager : MonoBehaviour
    {
        [Header("Foundation System")]
        [SerializeField] private bool _requireFoundationsAboveGround = true;
        [SerializeField] private float _foundationStrengthMultiplier = 1.5f;
        [SerializeField] private int _maxStackableHeight = 10;
        [SerializeField] private bool _enableCascadingRemoval = true;

        [Header("Weight Validation")]
        [SerializeField] private bool _enableWeightValidation = true;
        [SerializeField] private float _maxStackWeight = 1000f;
        [SerializeField] private float _safetyMarginPercent = 0.1f;
        [SerializeField] private bool _distributeWeightEvenly = true;

        [Header("Stacking Rules")]
        [SerializeField] private bool _enforceStackingRules = true;
        [SerializeField] private float _stackingTolerance = 0.1f;
        [SerializeField] private bool _allowPartialStacking = false;
        [SerializeField] private List<PlaceableType> _stackableTypes = new List<PlaceableType>();

        // Core references
        private GridSystem _gridSystem;
        private StructuralIntegrityValidator _structuralValidator;
        private GridPlacementValidator _placementValidator;
        private CascadingRemovalHandler _cascadingRemovalHandler;

        // Stack management
        private Dictionary<Vector3Int, StackInfo> _stackRegistry = new Dictionary<Vector3Int, StackInfo>();
        private Dictionary<Vector3Int, List<Vector3Int>> _foundationDependencies = new Dictionary<Vector3Int, List<Vector3Int>>();

        // Events
        public System.Action<Vector3Int, int> OnStackHeightChanged;
        public System.Action<Vector3Int, float> OnWeightCapacityChanged;
        public System.Action<List<Vector3Int>> OnCascadingRemoval;
        public System.Action<Vector3Int, bool> OnFoundationStatusChanged;

        private struct StackInfo
        {
            public int Height;
            public float TotalWeight;
            public float WeightCapacity;
            public List<Vector3Int> StackedObjects;
            public Vector3Int FoundationPosition;
            public bool IsStructurallySound;
        }

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            ValidateInitialStacks();
        }

        private void InitializeComponents()
        {
            _gridSystem = ServiceContainerFactory.Instance?.TryResolve<IGridSystem>() as GridSystem;
            _structuralValidator = GetComponent<StructuralIntegrityValidator>();
            _placementValidator = GetComponent<GridPlacementValidator>();
            _cascadingRemovalHandler = GetComponent<CascadingRemovalHandler>();

            if (_gridSystem == null)
                ChimeraLogger.LogError("[VerticalPlacementManager] GridSystem not found!");
        }

        #region Public API

        /// <summary>
        /// Validate if object can be placed at specified position with stacking rules
        /// </summary>
        public ValidationResult ValidateVerticalPlacement(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new ValidationResult();

            if (placeable == null || _gridSystem == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid placeable or grid system";
                return result;
            }

            // Check basic grid boundaries
            if (!_gridSystem.IsValidGridPosition(gridPosition))
            {
                result.IsValid = false;
                result.ErrorMessage = "Position outside grid boundaries";
                return result;
            }

            // Validate foundation requirements
            if (!ValidateFoundationRequirement(placeable, gridPosition, out string foundationError))
            {
                result.IsValid = false;
                result.ErrorMessage = foundationError;
                return result;
            }

            // Validate stacking rules
            if (!ValidateStackingRules(placeable, gridPosition, out string stackingError))
            {
                result.IsValid = false;
                result.ErrorMessage = stackingError;
                return result;
            }

            // Validate weight capacity
            if (!ValidateWeightCapacity(placeable, gridPosition, out string weightError))
            {
                result.IsValid = false;
                result.ErrorMessage = weightError;
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Register object placement and update stack information
        /// </summary>
        public void RegisterPlacement(GridPlaceable placeable, Vector3Int gridPosition)
        {
            if (placeable == null || !_gridSystem.IsValidGridPosition(gridPosition)) return;

            UpdateStackRegistry(gridPosition, placeable);
            UpdateFoundationDependencies(gridPosition, placeable);
            RecalculateStackMetrics(gridPosition);

            // Register with cascading removal handler
            if (_cascadingRemovalHandler != null)
                _cascadingRemovalHandler.RegisterObjectPlacement(gridPosition, placeable);

            OnStackHeightChanged?.Invoke(gridPosition, GetStackHeight(gridPosition));
        }

        /// <summary>
        /// Unregister object and handle cascading removal if necessary
        /// </summary>
        public List<Vector3Int> UnregisterPlacement(Vector3Int gridPosition)
        {
            var removedPositions = new List<Vector3Int> { gridPosition };

            if (_enableCascadingRemoval && _cascadingRemovalHandler != null)
            {
                // Use the dedicated cascading removal handler
                var analysisResult = _cascadingRemovalHandler.AnalyzeCascadingRemoval(gridPosition);
                
                if (analysisResult.TotalAffected > 1) // More than just the removed object
                {
                    _cascadingRemovalHandler.ExecuteCascadingRemoval(gridPosition, false);
                    removedPositions.AddRange(analysisResult.AffectedPositions);
                    OnCascadingRemoval?.Invoke(analysisResult.AffectedPositions);
                }
            }
            else if (_enableCascadingRemoval)
            {
                // Fallback to simple cascading removal
                var cascadePositions = CalculateCascadingRemoval(gridPosition);
                removedPositions.AddRange(cascadePositions);
                OnCascadingRemoval?.Invoke(cascadePositions);
            }

            // Clean up stack registry
            RemoveFromStackRegistry(gridPosition);
            UpdateDependentStacks(gridPosition);

            // Unregister from cascading removal handler
            if (_cascadingRemovalHandler != null)
                _cascadingRemovalHandler.UnregisterObject(gridPosition);

            return removedPositions;
        }

        /// <summary>
        /// Get maximum safe height for stacking at position
        /// </summary>
        public int GetMaxStackHeight(Vector3Int basePosition)
        {
            if (!_stackRegistry.ContainsKey(basePosition))
                return _maxStackableHeight;

            var stackInfo = _stackRegistry[basePosition];
            float remainingCapacity = stackInfo.WeightCapacity - stackInfo.TotalWeight;
            
            // Estimate based on average object weight
            float estimatedObjectWeight = GetAverageObjectWeight();
            int estimatedMaxHeight = Mathf.FloorToInt(remainingCapacity / estimatedObjectWeight);

            return Mathf.Min(estimatedMaxHeight, _maxStackableHeight - stackInfo.Height);
        }

        /// <summary>
        /// Get current stack information for position
        /// </summary>
        public StackData GetStackData(Vector3Int position)
        {
            if (!_stackRegistry.ContainsKey(position))
            {
                return new StackData
                {
                    Height = 0,
                    TotalWeight = 0f,
                    WeightCapacity = _maxStackWeight,
                    IsStructurallySound = true
                };
            }

            var stackInfo = _stackRegistry[position];
            return new StackData
            {
                Height = stackInfo.Height,
                TotalWeight = stackInfo.TotalWeight,
                WeightCapacity = stackInfo.WeightCapacity,
                IsStructurallySound = stackInfo.IsStructurallySound
            };
        }

        #endregion

        #region Foundation Validation

        private bool ValidateFoundationRequirement(GridPlaceable placeable, Vector3Int gridPosition, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Ground level doesn't need foundation
            if (gridPosition.z == 0)
                return true;

            if (!_requireFoundationsAboveGround)
                return true;

            // Check if foundation exists below
            Vector3Int foundationPos = new Vector3Int(gridPosition.x, gridPosition.y, gridPosition.z - 1);
            var foundationCell = _gridSystem.GetGridCell(foundationPos);

            if (foundationCell?.IsOccupied != true)
            {
                errorMessage = "Foundation required below this position";
                return false;
            }

            var foundationObject = foundationCell.OccupyingObject;
            if (!CanSupportWeight(foundationObject, placeable))
            {
                errorMessage = "Foundation cannot support the weight of this object";
                return false;
            }

            return true;
        }

        private bool CanSupportWeight(GridPlaceable foundation, GridPlaceable newObject)
        {
            if (!_enableWeightValidation) return true;

            float foundationCapacity = GetObjectWeightCapacity(foundation);
            float newObjectWeight = GetObjectWeight(newObject);
            
            var foundationPos = foundation.GridCoordinate;
            float currentLoad = GetCurrentStackWeight(foundationPos);
            
            return (currentLoad + newObjectWeight) <= (foundationCapacity * (1f + _safetyMarginPercent));
        }

        #endregion

        #region Stacking Validation

        private bool ValidateStackingRules(GridPlaceable placeable, Vector3Int gridPosition, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_enforceStackingRules)
                return true;

            // Check if object type is stackable
            if (!_stackableTypes.Contains(placeable.Type))
            {
                errorMessage = $"Object type {placeable.Type} cannot be stacked";
                return false;
            }

            // Check stacking alignment if not at ground level
            if (gridPosition.z > 0)
            {
                Vector3Int belowPos = new Vector3Int(gridPosition.x, gridPosition.y, gridPosition.z - 1);
                var belowCell = _gridSystem.GetGridCell(belowPos);

                if (belowCell?.IsOccupied != true)
                {
                    errorMessage = "Cannot stack without object below";
                    return false;
                }

                if (!IsAlignedForStacking(placeable, belowCell.OccupyingObject))
                {
                    errorMessage = "Objects not properly aligned for stacking";
                    return false;
                }
            }

            // Check maximum stack height
            int currentHeight = GetStackHeight(new Vector3Int(gridPosition.x, gridPosition.y, 0));
            if (currentHeight >= _maxStackableHeight)
            {
                errorMessage = $"Maximum stack height ({_maxStackableHeight}) reached";
                return false;
            }

            return true;
        }

        private bool IsAlignedForStacking(GridPlaceable upper, GridPlaceable lower)
        {
            if (_allowPartialStacking) return true;

            var upperBounds = upper.GetObjectBounds();
            var lowerBounds = lower.GetObjectBounds();

            // Check if upper object fits within lower object bounds with tolerance
            bool xAligned = upperBounds.min.x >= (lowerBounds.min.x - _stackingTolerance) &&
                           upperBounds.max.x <= (lowerBounds.max.x + _stackingTolerance);
            
            bool yAligned = upperBounds.min.y >= (lowerBounds.min.y - _stackingTolerance) &&
                           upperBounds.max.y <= (lowerBounds.max.y + _stackingTolerance);

            return xAligned && yAligned;
        }

        #endregion

        #region Weight Validation

        private bool ValidateWeightCapacity(GridPlaceable placeable, Vector3Int gridPosition, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_enableWeightValidation)
                return true;

            // Calculate total weight impact
            float objectWeight = GetObjectWeight(placeable);
            Vector3Int basePosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
            
            // Check foundation weight capacity
            if (gridPosition.z > 0)
            {
                float foundationCapacity = GetFoundationWeightCapacity(basePosition);
                float currentStackWeight = GetCurrentStackWeight(basePosition);
                
                if ((currentStackWeight + objectWeight) > foundationCapacity)
                {
                    errorMessage = $"Weight exceeds foundation capacity ({foundationCapacity:F1}kg)";
                    return false;
                }
            }

            return true;
        }

        private float GetObjectWeight(GridPlaceable placeable)
        {
            var weightComponent = placeable.GetComponent<ObjectWeight>();
            return weightComponent?.Weight ?? 10f; // Default weight
        }

        private float GetObjectWeightCapacity(GridPlaceable placeable)
        {
            var weightComponent = placeable.GetComponent<ObjectWeight>();
            return weightComponent?.WeightCapacity ?? _maxStackWeight;
        }

        private float GetFoundationWeightCapacity(Vector3Int basePosition)
        {
            var foundationCell = _gridSystem.GetGridCell(basePosition);
            if (foundationCell?.IsOccupied != true) return 0f;
            
            return GetObjectWeightCapacity(foundationCell.OccupyingObject) * _foundationStrengthMultiplier;
        }

        private float GetCurrentStackWeight(Vector3Int basePosition)
        {
            if (!_stackRegistry.ContainsKey(basePosition))
                return 0f;
            
            return _stackRegistry[basePosition].TotalWeight;
        }

        #endregion

        #region Stack Management

        private void UpdateStackRegistry(Vector3Int gridPosition, GridPlaceable placeable)
        {
            Vector3Int basePosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
            
            if (!_stackRegistry.ContainsKey(basePosition))
            {
                _stackRegistry[basePosition] = new StackInfo
                {
                    Height = 0,
                    TotalWeight = 0f,
                    WeightCapacity = _maxStackWeight,
                    StackedObjects = new List<Vector3Int>(),
                    FoundationPosition = basePosition,
                    IsStructurallySound = true
                };
            }

            var stackInfo = _stackRegistry[basePosition];
            stackInfo.StackedObjects.Add(gridPosition);
            stackInfo.Height = Mathf.Max(stackInfo.Height, gridPosition.z + 1);
            stackInfo.TotalWeight += GetObjectWeight(placeable);
            
            _stackRegistry[basePosition] = stackInfo;
        }

        private int GetStackHeight(Vector3Int basePosition)
        {
            if (!_stackRegistry.ContainsKey(basePosition))
                return 0;
            
            return _stackRegistry[basePosition].Height;
        }

        private void RecalculateStackMetrics(Vector3Int basePosition)
        {
            if (!_stackRegistry.ContainsKey(basePosition)) return;

            var stackInfo = _stackRegistry[basePosition];
            
            // Recalculate structural soundness
            stackInfo.IsStructurallySound = true;
            if (_structuralValidator != null && stackInfo.StackedObjects.Count > 0)
            {
                // Get the base object position for structural validation
                var baseObjectPosition = stackInfo.StackedObjects.FirstOrDefault();
                var gridCell = _gridSystem?.GetGridCell(baseObjectPosition);
                var baseObject = gridCell?.OccupyingObject;
                
                if (baseObject != null)
                {
                    var tempResult = new PlacementValidationResult();
                    _structuralValidator.ValidateStructuralIntegrity(baseObject, basePosition, tempResult);
                    stackInfo.IsStructurallySound = tempResult.IsValid;
                }
            }

            // Update weight capacity based on foundation
            stackInfo.WeightCapacity = GetFoundationWeightCapacity(basePosition);
            
            _stackRegistry[basePosition] = stackInfo;
            OnWeightCapacityChanged?.Invoke(basePosition, stackInfo.WeightCapacity);
        }

        #endregion

        #region Utility Methods

        private void ValidateInitialStacks()
        {
            // Validate existing stacks on startup
            foreach (var kvp in _stackRegistry.ToList())
            {
                RecalculateStackMetrics(kvp.Key);
            }
        }

        private List<Vector3Int> CalculateCascadingRemoval(Vector3Int removedPosition)
        {
            var cascadeList = new List<Vector3Int>();
            
            // Find objects that depend on this position for support
            if (_foundationDependencies.ContainsKey(removedPosition))
            {
                foreach (var dependentPos in _foundationDependencies[removedPosition])
                {
                    cascadeList.Add(dependentPos);
                    cascadeList.AddRange(CalculateCascadingRemoval(dependentPos));
                }
            }

            return cascadeList.Distinct().ToList();
        }

        private void UpdateFoundationDependencies(Vector3Int gridPosition, GridPlaceable placeable)
        {
            if (gridPosition.z == 0) return; // Ground level objects don't have dependencies

            Vector3Int foundationPos = new Vector3Int(gridPosition.x, gridPosition.y, gridPosition.z - 1);
            
            if (!_foundationDependencies.ContainsKey(foundationPos))
                _foundationDependencies[foundationPos] = new List<Vector3Int>();
            
            _foundationDependencies[foundationPos].Add(gridPosition);
        }

        private void RemoveFromStackRegistry(Vector3Int gridPosition)
        {
            Vector3Int basePosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);
            
            if (_stackRegistry.ContainsKey(basePosition))
            {
                var stackInfo = _stackRegistry[basePosition];
                stackInfo.StackedObjects.Remove(gridPosition);
                
                if (stackInfo.StackedObjects.Count == 0)
                {
                    _stackRegistry.Remove(basePosition);
                }
                else
                {
                    // Recalculate height and weight
                    stackInfo.Height = stackInfo.StackedObjects.Max(pos => pos.z) + 1;
                    _stackRegistry[basePosition] = stackInfo;
                }
            }
        }

        private void UpdateDependentStacks(Vector3Int removedPosition)
        {
            // Update stacks that may have been affected by removal
            foreach (var kvp in _stackRegistry.ToList())
            {
                if (kvp.Value.StackedObjects.Any(pos => 
                    Vector3Int.Distance(pos, removedPosition) <= 1.5f))
                {
                    RecalculateStackMetrics(kvp.Key);
                }
            }
        }

        private float GetAverageObjectWeight()
        {
            return 50f; // Placeholder - should calculate from actual objects
        }

        #endregion

        #region Data Structures

        public struct ValidationResult
        {
            public bool IsValid;
            public string ErrorMessage;
            public List<Vector3Int> AffectedPositions;
        }

        public struct StackData
        {
            public int Height;
            public float TotalWeight;
            public float WeightCapacity;
            public bool IsStructurallySound;
        }

        #endregion
    }

    /// <summary>
    /// Component for defining object weight and weight capacity
    /// </summary>
    public class ObjectWeight : MonoBehaviour
    {
        [SerializeField] private float _weight = 10f;
        [SerializeField] private float _weightCapacity = 100f;
        
        public float Weight => _weight;
        public float WeightCapacity => _weightCapacity;
    }
}