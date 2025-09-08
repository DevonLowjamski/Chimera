using UnityEngine;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Advanced multi-level foundation system with weight validation and structural analysis.
    /// Manages foundation requirements, load distribution, and structural integrity for vertical construction.
    /// </summary>
    public class MultiLevelFoundationSystem : MonoBehaviour, ITickable
    {
        [Header("Foundation Configuration")]
        [SerializeField] private bool _requireFoundationsAboveGround = true;
        [SerializeField] private float _foundationLoadCapacityMultiplier = 2.0f;
        [SerializeField] private int _maxFoundationDepth = 3;
        [SerializeField] private bool _enableDeepFoundations = true;

        [Header("Weight Distribution")]
        [SerializeField] private bool _enableLoadDistribution = true;
        [SerializeField] private float _loadDistributionRadius = 2f;
        [SerializeField] private AnimationCurve _loadDistributionCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
        [SerializeField] private float _overloadTolerance = 0.15f;

        [Header("Structural Analysis")]
        [SerializeField] private bool _enableStructuralAnalysis = true;
        [SerializeField] private float _structuralAnalysisUpdateRate = 1f;
        [SerializeField] private int _maxAnalysisDepth = 5;
        [SerializeField] private bool _visualizeStructuralLoads = false;

        [Header("Foundation Types")]
        [SerializeField] private List<FoundationTypeData> _foundationTypes = new List<FoundationTypeData>();

        // Core references
        private GridSystem _gridSystem;
        private VerticalPlacementManager _verticalManager;
        private StructuralIntegrityValidator _structuralValidator;
        private CascadingRemovalHandler _cascadingRemovalHandler;

        // Foundation tracking
        private Dictionary<Vector3Int, FoundationInfo> _foundationRegistry = new Dictionary<Vector3Int, FoundationInfo>();
        private Dictionary<Vector3Int, LoadData> _loadDistribution = new Dictionary<Vector3Int, LoadData>();
        private HashSet<Vector3Int> _compromisedFoundations = new HashSet<Vector3Int>();

        // Performance management
        private float _lastAnalysisUpdate;
        private Queue<Vector3Int> _analysisQueue = new Queue<Vector3Int>();

        // Events
        public System.Action<Vector3Int, float> OnFoundationLoadChanged;
        public System.Action<Vector3Int, bool> OnFoundationIntegrityChanged;
        public System.Action<List<Vector3Int>> OnStructuralFailure;

        [System.Serializable]
        public class FoundationTypeData
        {
            public PlaceableType foundationType;
            public float baseLoadCapacity = 1000f;
            public float depthMultiplier = 1.5f;
            public bool canSupportMultipleTypes = true;
            public List<PlaceableType> supportedTypes = new List<PlaceableType>();
        }

        private struct FoundationInfo
        {
            public PlaceableType FoundationType;
            public float MaxLoadCapacity;
            public float CurrentLoad;
            public int FoundationDepth;
            public List<Vector3Int> SupportedPositions;
            public bool IsStructurallySound;
            public Vector3Int BasePosition;
        }

        private struct LoadData
        {
            public float TotalLoad;
            public float DistributedLoad;
            public List<Vector3Int> LoadSources;
            public float LoadFactor;
        }

        private void Awake()
        {
            InitializeComponents();
            InitializeDefaultFoundationTypes();
        }

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            RegisterWithVerticalManager();
        }

            public void Tick(float deltaTime)
    {
            if (Time.time - _lastAnalysisUpdate >= _structuralAnalysisUpdateRate)
            {
                PerformStructuralAnalysis();
                _lastAnalysisUpdate = Time.time;

    }
        }

        private void InitializeComponents()
        {
            _gridSystem = ServiceContainerFactory.Instance?.TryResolve<IGridSystem>() as GridSystem;
            _verticalManager = GetComponent<VerticalPlacementManager>();
            _structuralValidator = GetComponent<StructuralIntegrityValidator>();
            _cascadingRemovalHandler = GetComponent<CascadingRemovalHandler>();

            if (_gridSystem == null)
                ChimeraLogger.LogError("[MultiLevelFoundationSystem] GridSystem not found!");
        }

        private void InitializeDefaultFoundationTypes()
        {
            if (_foundationTypes.Count == 0)
            {
                _foundationTypes.Add(new FoundationTypeData
                {
                    foundationType = PlaceableType.Structure,
                    baseLoadCapacity = 1500f,
                    depthMultiplier = 2f,
                    canSupportMultipleTypes = true
                });
            }
        }

        private void RegisterWithVerticalManager()
        {
            if (_verticalManager != null)
            {
                // Register for vertical placement events
                _verticalManager.OnStackHeightChanged += OnStackChanged;
                _verticalManager.OnWeightCapacityChanged += OnWeightCapacityChanged;
            }
        }

        #region Public API

        /// <summary>
        /// Validate foundation requirements for a placement at specified position
        /// </summary>
        public FoundationValidationResult ValidateFoundationRequirement(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new FoundationValidationResult();

            if (placeable == null || !_gridSystem.IsValidGridPosition(gridPosition))
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid placeable or grid position";
                return result;
            }

            // Ground level doesn't need foundation validation
            if (gridPosition.z == 0)
            {
                result.IsValid = true;
                return result;
            }

            if (!_requireFoundationsAboveGround)
            {
                result.IsValid = true;
                return result;
            }

            // Check foundation requirements
            var foundationResult = AnalyzeFoundationRequirements(placeable, gridPosition);
            result.IsValid = foundationResult.HasAdequateFoundation;
            result.ErrorMessage = foundationResult.ErrorMessage;
            result.RequiredFoundations = foundationResult.RequiredFoundationPositions;
            result.LoadCapacity = foundationResult.AvailableLoadCapacity;

            return result;
        }

        /// <summary>
        /// Register a new foundation and calculate its load capacity
        /// </summary>
        public void RegisterFoundation(GridPlaceable foundation, Vector3Int position)
        {
            if (foundation == null || !_gridSystem.IsValidGridPosition(position)) return;

            var foundationType = GetFoundationTypeData(foundation.Type);
            if (foundationType == null) return;

            var foundationInfo = new FoundationInfo
            {
                FoundationType = foundation.Type,
                MaxLoadCapacity = CalculateFoundationCapacity(foundation, position),
                CurrentLoad = GetObjectWeight(foundation),
                FoundationDepth = CalculateFoundationDepth(position),
                SupportedPositions = new List<Vector3Int>(),
                IsStructurallySound = true,
                BasePosition = position
            };

            _foundationRegistry[position] = foundationInfo;
            UpdateLoadDistribution(position);

            OnFoundationLoadChanged?.Invoke(position, foundationInfo.CurrentLoad);
        }

        /// <summary>
        /// Unregister foundation and handle dependent structures
        /// </summary>
        public List<Vector3Int> UnregisterFoundation(Vector3Int position)
        {
            var affectedPositions = new List<Vector3Int>();

            if (!_foundationRegistry.ContainsKey(position))
                return affectedPositions;

            var foundationInfo = _foundationRegistry[position];

            // Use cascading removal handler if available
            if (_cascadingRemovalHandler != null)
            {
                var analysisResult = _cascadingRemovalHandler.AnalyzeCascadingRemoval(position);

                if (analysisResult.TotalAffected > 1)
                {
                    _cascadingRemovalHandler.ExecuteCascadingRemoval(position, false);
                    affectedPositions.AddRange(analysisResult.AffectedPositions);
                    OnStructuralFailure?.Invoke(analysisResult.AffectedPositions);
                }
            }
            else
            {
                // Fallback to original logic
                // Find all positions that depend on this foundation
                affectedPositions.AddRange(foundationInfo.SupportedPositions);

                // Analyze impact on load distribution
                var redistributionImpact = AnalyzeLoadRedistribution(position);
                affectedPositions.AddRange(redistributionImpact.AffectedFoundations);

                // Check for cascading structural failures
                var structuralFailures = AnalyzeCascadingFailures(position);
                if (structuralFailures.Count > 0)
                {
                    affectedPositions.AddRange(structuralFailures);
                    OnStructuralFailure?.Invoke(structuralFailures);
                }
            }

            _foundationRegistry.Remove(position);
            _loadDistribution.Remove(position);
            _compromisedFoundations.Remove(position);

            return affectedPositions.Distinct().ToList();
        }

        /// <summary>
        /// Get foundation capacity information for a position
        /// </summary>
        public FoundationCapacityInfo GetFoundationCapacity(Vector3Int position)
        {
            if (_foundationRegistry.ContainsKey(position))
            {
                var info = _foundationRegistry[position];
                return new FoundationCapacityInfo
                {
                    MaxCapacity = info.MaxLoadCapacity,
                    CurrentLoad = info.CurrentLoad,
                    RemainingCapacity = info.MaxLoadCapacity - info.CurrentLoad,
                    IsOverloaded = info.CurrentLoad > info.MaxLoadCapacity * (1f + _overloadTolerance),
                    StructurallySound = info.IsStructurallySound
                };
            }

            return new FoundationCapacityInfo();
        }

        /// <summary>
        /// Check if position can support additional load
        /// </summary>
        public bool CanSupportAdditionalLoad(Vector3Int foundationPosition, float additionalLoad)
        {
            if (!_foundationRegistry.ContainsKey(foundationPosition))
                return false;

            var foundationInfo = _foundationRegistry[foundationPosition];
            float totalLoad = foundationInfo.CurrentLoad + additionalLoad;
            float maxAllowedLoad = foundationInfo.MaxLoadCapacity * (1f + _overloadTolerance);

            return totalLoad <= maxAllowedLoad && foundationInfo.IsStructurallySound;
        }

        #endregion

        #region Foundation Analysis

        private FoundationAnalysisResult AnalyzeFoundationRequirements(GridPlaceable placeable, Vector3Int gridPosition)
        {
            var result = new FoundationAnalysisResult();
            var requiredFoundations = new List<Vector3Int>();

            // Check immediate foundation below
            Vector3Int foundationPos = new Vector3Int(gridPosition.x, gridPosition.y, gridPosition.z - 1);
            var foundationCell = _gridSystem.GetGridCell(foundationPos);

            if (foundationCell?.IsOccupied != true)
            {
                result.HasAdequateFoundation = false;
                result.ErrorMessage = "Direct foundation required below object";
                requiredFoundations.Add(foundationPos);
            }
            else
            {
                var foundation = foundationCell.OccupyingObject;
                var foundationCapacity = GetFoundationCapacity(foundationPos);
                float objectWeight = GetObjectWeight(placeable);

                if (!CanSupportAdditionalLoad(foundationPos, objectWeight))
                {
                    result.HasAdequateFoundation = false;
                    result.ErrorMessage = $"Foundation cannot support additional load ({objectWeight:F1}kg)";
                }
                else
                {
                    result.HasAdequateFoundation = true;
                    result.AvailableLoadCapacity = foundationCapacity.RemainingCapacity;
                }
            }

            // Check for deep foundation requirements if enabled
            if (_enableDeepFoundations && result.HasAdequateFoundation)
            {
                var deepFoundationCheck = AnalyzeDeepFoundationRequirements(gridPosition, GetObjectWeight(placeable));
                if (!deepFoundationCheck.IsAdequate)
                {
                    result.HasAdequateFoundation = false;
                    result.ErrorMessage = deepFoundationCheck.ErrorMessage;
                    requiredFoundations.AddRange(deepFoundationCheck.RequiredPositions);
                }
            }

            result.RequiredFoundationPositions = requiredFoundations;
            return result;
        }

        private DeepFoundationResult AnalyzeDeepFoundationRequirements(Vector3Int position, float load)
        {
            var result = new DeepFoundationResult { IsAdequate = true };
            var requiredPositions = new List<Vector3Int>();

            // Calculate required foundation depth based on load
            int requiredDepth = Mathf.CeilToInt(load / 500f); // 500kg per foundation level
            requiredDepth = Mathf.Min(requiredDepth, _maxFoundationDepth);

            for (int depth = 1; depth <= requiredDepth; depth++)
            {
                Vector3Int foundationPos = new Vector3Int(position.x, position.y, position.z - depth);

                if (!_gridSystem.IsValidGridPosition(foundationPos) || foundationPos.z < 0)
                    break;

                var cell = _gridSystem.GetGridCell(foundationPos);
                if (cell?.IsOccupied != true)
                {
                    result.IsAdequate = false;
                    result.ErrorMessage = $"Deep foundation required at depth {depth}";
                    requiredPositions.Add(foundationPos);
                }
            }

            result.RequiredPositions = requiredPositions;
            return result;
        }

        private float CalculateFoundationCapacity(GridPlaceable foundation, Vector3Int position)
        {
            var foundationTypeData = GetFoundationTypeData(foundation.Type);
            if (foundationTypeData == null) return 1000f; // Default capacity

            float baseCapacity = foundationTypeData.baseLoadCapacity;
            int depth = CalculateFoundationDepth(position);
            float depthMultiplier = Mathf.Pow(foundationTypeData.depthMultiplier, depth);

            return baseCapacity * depthMultiplier * _foundationLoadCapacityMultiplier;
        }

        private int CalculateFoundationDepth(Vector3Int position)
        {
            int depth = 0;
            Vector3Int checkPosition = new Vector3Int(position.x, position.y, position.z - 1);

            while (depth < _maxFoundationDepth && _gridSystem.IsValidGridPosition(checkPosition))
            {
                var cell = _gridSystem.GetGridCell(checkPosition);
                if (cell?.IsOccupied != true) break;

                var foundationTypeData = GetFoundationTypeData(cell.OccupyingObject.Type);
                if (foundationTypeData == null) break;

                depth++;
                checkPosition.z--;
            }

            return depth;
        }

        #endregion

        #region Load Distribution

        private void UpdateLoadDistribution(Vector3Int foundationPosition)
        {
            if (!_enableLoadDistribution) return;

            var foundationInfo = _foundationRegistry[foundationPosition];
            var affectedPositions = GetPositionsWithinRadius(foundationPosition, _loadDistributionRadius);

            foreach (var pos in affectedPositions)
            {
                UpdatePositionLoadDistribution(pos, foundationPosition, foundationInfo.CurrentLoad);
            }
        }

        private void UpdatePositionLoadDistribution(Vector3Int position, Vector3Int sourceFoundation, float load)
        {
            float distance = Vector3.Distance(
                _gridSystem.GridToWorldPosition(position),
                _gridSystem.GridToWorldPosition(sourceFoundation)
            );

            float distributionFactor = _loadDistributionCurve.Evaluate(distance / _loadDistributionRadius);
            float distributedLoad = load * distributionFactor;

            if (!_loadDistribution.ContainsKey(position))
            {
                _loadDistribution[position] = new LoadData
                {
                    LoadSources = new List<Vector3Int>()
                };
            }

            var loadData = _loadDistribution[position];
            if (!loadData.LoadSources.Contains(sourceFoundation))
            {
                loadData.LoadSources.Add(sourceFoundation);
            }

            loadData.DistributedLoad += distributedLoad;
            loadData.TotalLoad = CalculateTotalPositionLoad(position);
            loadData.LoadFactor = loadData.TotalLoad / GetMaxLoadCapacityForPosition(position);

            _loadDistribution[position] = loadData;
        }

        private LoadRedistributionResult AnalyzeLoadRedistribution(Vector3Int removedFoundation)
        {
            var result = new LoadRedistributionResult();
            var affectedFoundations = new List<Vector3Int>();

            if (!_foundationRegistry.ContainsKey(removedFoundation))
                return result;

            var foundationInfo = _foundationRegistry[removedFoundation];
            float redistributedLoad = foundationInfo.CurrentLoad;

            // Find nearby foundations that will absorb the load
            var nearbyFoundations = GetNearbyFoundations(removedFoundation, _loadDistributionRadius * 1.5f);

            foreach (var nearbyPos in nearbyFoundations)
            {
                float additionalLoad = redistributedLoad / nearbyFoundations.Count;

                if (!CanSupportAdditionalLoad(nearbyPos, additionalLoad))
                {
                    affectedFoundations.Add(nearbyPos);
                    result.WillCauseOverload = true;
                }
            }

            result.AffectedFoundations = affectedFoundations;
            return result;
        }

        #endregion

        #region Structural Analysis

        private void PerformStructuralAnalysis()
        {
            if (!_enableStructuralAnalysis) return;

            // Process analysis queue with limited items per frame
            int analysisCount = Mathf.Min(5, _analysisQueue.Count);

            for (int i = 0; i < analysisCount; i++)
            {
                if (_analysisQueue.Count > 0)
                {
                    Vector3Int position = _analysisQueue.Dequeue();
                    AnalyzeFoundationStructuralIntegrity(position);
                }
            }

            // Add foundations that need analysis back to queue
            foreach (var foundationPos in _foundationRegistry.Keys.Where(pos =>
                Time.time - _lastAnalysisUpdate > _structuralAnalysisUpdateRate))
            {
                _analysisQueue.Enqueue(foundationPos);
            }
        }

        private void AnalyzeFoundationStructuralIntegrity(Vector3Int foundationPosition)
        {
            if (!_foundationRegistry.ContainsKey(foundationPosition)) return;

            var foundationInfo = _foundationRegistry[foundationPosition];
            bool wasStructurallySound = foundationInfo.IsStructurallySound;

            // Check load capacity
            bool loadWithinLimits = foundationInfo.CurrentLoad <=
                foundationInfo.MaxLoadCapacity * (1f + _overloadTolerance);

            // Check structural validation (simplified check for foundation integrity)
            bool structurallyValid = true;
            if (_structuralValidator != null && _foundationRegistry.ContainsKey(foundationPosition))
            {
                // Get the grid cell to find the foundation object
                var gridCell = _gridSystem?.GetGridCell(foundationPosition);
                if (gridCell?.OccupyingObject != null)
                {
                    // Create a dummy validation result to check structural integrity
                    var tempResult = new PlacementValidationResult();
                    _structuralValidator.ValidateStructuralIntegrity(gridCell.OccupyingObject, foundationPosition, tempResult);
                    structurallyValid = tempResult.IsValid;
                }
            }

            foundationInfo.IsStructurallySound = loadWithinLimits && structurallyValid;
            _foundationRegistry[foundationPosition] = foundationInfo;

            // Update compromised foundations tracking
            if (foundationInfo.IsStructurallySound)
            {
                _compromisedFoundations.Remove(foundationPosition);
            }
            else
            {
                _compromisedFoundations.Add(foundationPosition);
            }

            // Fire event if status changed
            if (wasStructurallySound != foundationInfo.IsStructurallySound)
            {
                OnFoundationIntegrityChanged?.Invoke(foundationPosition, foundationInfo.IsStructurallySound);
            }
        }

        private List<Vector3Int> AnalyzeCascadingFailures(Vector3Int failedFoundation)
        {
            var cascadingFailures = new List<Vector3Int>();

            if (!_foundationRegistry.ContainsKey(failedFoundation)) return cascadingFailures;

            var foundationInfo = _foundationRegistry[failedFoundation];

            // Check each supported position for cascading failure
            foreach (var supportedPos in foundationInfo.SupportedPositions)
            {
                // Recursively check if this position's removal would cause more failures
                var secondaryFailures = AnalyzeCascadingFailures(supportedPos);
                cascadingFailures.AddRange(secondaryFailures);
            }

            return cascadingFailures.Distinct().ToList();
        }

        #endregion

        #region Utility Methods

        private FoundationTypeData GetFoundationTypeData(PlaceableType type)
        {
            return _foundationTypes.FirstOrDefault(ft => ft.foundationType == type);
        }

        private float GetObjectWeight(GridPlaceable placeable)
        {
            var weightComponent = placeable.GetComponent<ObjectWeight>();
            return weightComponent?.Weight ?? 50f; // Default weight
        }

        private List<Vector3Int> GetPositionsWithinRadius(Vector3Int center, float radius)
        {
            var positions = new List<Vector3Int>();
            int gridRadius = Mathf.CeilToInt(radius / _gridSystem.GridSize);

            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    for (int z = -gridRadius; z <= gridRadius; z++)
                    {
                        Vector3Int pos = center + new Vector3Int(x, y, z);

                        if (_gridSystem.IsValidGridPosition(pos))
                        {
                            float distance = Vector3.Distance(
                                _gridSystem.GridToWorldPosition(center),
                                _gridSystem.GridToWorldPosition(pos)
                            );

                            if (distance <= radius)
                                positions.Add(pos);
                        }
                    }
                }
            }

            return positions;
        }

        private List<Vector3Int> GetNearbyFoundations(Vector3Int position, float radius)
        {
            return GetPositionsWithinRadius(position, radius)
                .Where(pos => _foundationRegistry.ContainsKey(pos))
                .ToList();
        }

        private float CalculateTotalPositionLoad(Vector3Int position)
        {
            float totalLoad = 0f;

            if (_loadDistribution.ContainsKey(position))
            {
                totalLoad += _loadDistribution[position].DistributedLoad;
            }

            if (_foundationRegistry.ContainsKey(position))
            {
                totalLoad += _foundationRegistry[position].CurrentLoad;
            }

            return totalLoad;
        }

        private float GetMaxLoadCapacityForPosition(Vector3Int position)
        {
            if (_foundationRegistry.ContainsKey(position))
                return _foundationRegistry[position].MaxLoadCapacity;

            return 1000f; // Default capacity
        }

        #endregion

        #region Event Handlers

        private void OnStackChanged(Vector3Int position, int height)
        {
            // Analyze foundation impact when stack changes
            Vector3Int foundationPos = new Vector3Int(position.x, position.y, 0);
            if (_foundationRegistry.ContainsKey(foundationPos))
            {
                _analysisQueue.Enqueue(foundationPos);
            }
        }

        private void OnWeightCapacityChanged(Vector3Int position, float capacity)
        {
            // Update load distribution when weight capacity changes
            UpdateLoadDistribution(position);
        }

        #endregion

        #region Data Structures

        public struct FoundationValidationResult
        {
            public bool IsValid;
            public string ErrorMessage;
            public List<Vector3Int> RequiredFoundations;
            public float LoadCapacity;
        }

        public struct FoundationCapacityInfo
        {
            public float MaxCapacity;
            public float CurrentLoad;
            public float RemainingCapacity;
            public bool IsOverloaded;
            public bool StructurallySound;
        }

        private struct FoundationAnalysisResult
        {
            public bool HasAdequateFoundation;
            public string ErrorMessage;
            public List<Vector3Int> RequiredFoundationPositions;
            public float AvailableLoadCapacity;
        }

        private struct DeepFoundationResult
        {
            public bool IsAdequate;
            public string ErrorMessage;
            public List<Vector3Int> RequiredPositions;
        }

        private struct LoadRedistributionResult
        {
            public List<Vector3Int> AffectedFoundations;
            public bool WillCauseOverload;
        }

        #endregion

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            if (_verticalManager != null)
            {
                _verticalManager.OnStackHeightChanged -= OnStackChanged;
                _verticalManager.OnWeightCapacityChanged -= OnWeightCapacityChanged;
            }
        }

    #region ITickable Implementation

    public int Priority => TickPriority.ConstructionSystem;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public void OnRegistered()
    {
        ChimeraLogger.LogVerbose($"[{GetType().Name}] Registered with UpdateOrchestrator");
    }

    public void OnUnregistered()
    {
        ChimeraLogger.LogVerbose($"[{GetType().Name}] Unregistered from UpdateOrchestrator");
    }

    #endregion
    }
}
