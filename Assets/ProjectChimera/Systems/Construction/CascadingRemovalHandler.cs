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
    /// Handles cascading removal logic when foundations or supporting structures are destroyed.
    /// Manages dependency tracking, removal ordering, and structural stability validation.
    /// </summary>
    public class CascadingRemovalHandler : MonoBehaviour, ITickable
    {
        [Header("Cascading Removal Settings")]
        [SerializeField] private bool _enableCascadingRemoval = true;
        [SerializeField] private float _removalDelay = 0.1f;
        [SerializeField] private bool _enableRemovalAnimation = true;
        [SerializeField] private bool _showRemovalPreview = true;

        [Header("Dependency Analysis")]
        [SerializeField] private int _maxAnalysisDepth = 10;
        [SerializeField] private bool _considerStructuralIntegrity = true;
        [SerializeField] private bool _allowPartialCollapse = false;
        [SerializeField] private float _structuralAnalysisRadius = 3f;

        [Header("Safety Settings")]
        [SerializeField] private bool _confirmCascadingRemoval = true;
        [SerializeField] private int _maxCascadeCount = 50;
        [SerializeField] private bool _preventMassRemoval = true;

        // Core references
        private GridSystem _gridSystem;
        private VerticalPlacementManager _verticalManager;
        private MultiLevelFoundationSystem _foundationSystem;

        // Dependency tracking
        private Dictionary<Vector3Int, DependencyInfo> _dependencyRegistry = new Dictionary<Vector3Int, DependencyInfo>();
        private Dictionary<Vector3Int, List<Vector3Int>> _supportDependencies = new Dictionary<Vector3Int, List<Vector3Int>>();
        private Dictionary<Vector3Int, List<Vector3Int>> _reverseDependencies = new Dictionary<Vector3Int, List<Vector3Int>>();

        // Removal management
        private Queue<RemovalTask> _removalQueue = new Queue<RemovalTask>();
        private HashSet<Vector3Int> _pendingRemoval = new HashSet<Vector3Int>();
        private bool _isProcessingRemoval = false;

        // Events
        public System.Action<List<Vector3Int>> OnCascadingRemovalStarted;
        public System.Action<Vector3Int> OnObjectRemoved;
        public System.Action<List<Vector3Int>> OnCascadingRemovalCompleted;
        public System.Action<List<Vector3Int>> OnRemovalPrevented;

        private struct DependencyInfo
        {
            public List<Vector3Int> SupportedBy;
            public List<Vector3Int> Supports;
            public DependencyType Type;
            public float StructuralImportance;
            public bool IsRequired;
        }

        private struct RemovalTask
        {
            public Vector3Int Position;
            public float RemovalTime;
            public RemovalReason Reason;
            public Vector3Int CausingPosition;
        }

        private enum DependencyType
        {
            Foundation,
            Structural,
            Adjacent,
            Stacked
        }

        private enum RemovalReason
        {
            Direct,
            FoundationLost,
            StructuralFailure,
            WeightOverload,
            AccessLost
        }

        private void Awake()
        {
            InitializeComponents();
        }

            public void Tick(float deltaTime)
    {
            ProcessRemovalQueue();

    }

        private void InitializeComponents()
        {
            _gridSystem = ServiceContainerFactory.Instance?.TryResolve<IGridSystem>() as GridSystem;
            _verticalManager = GetComponent<VerticalPlacementManager>();
            _foundationSystem = GetComponent<MultiLevelFoundationSystem>();

            if (_gridSystem == null)
                ChimeraLogger.LogError("[CascadingRemovalHandler] GridSystem not found!");
        }

        #region Public API

        /// <summary>
        /// Analyze what would be removed if the specified position is removed
        /// </summary>
        public CascadeAnalysisResult AnalyzeCascadingRemoval(Vector3Int position)
        {
            var result = new CascadeAnalysisResult();
            var affectedPositions = new HashSet<Vector3Int>();

            if (!_enableCascadingRemoval)
            {
                result.AffectedPositions = new List<Vector3Int>();
                result.TotalAffected = 0;
                result.IsSignificant = false;
                return result;
            }

            // Build dependency tree for analysis
            UpdateDependencyRegistry();

            // Find all objects that would be affected
            AnalyzeCascadeImpact(position, affectedPositions, 0, RemovalReason.Direct);

            result.AffectedPositions = affectedPositions.ToList();
            result.TotalAffected = affectedPositions.Count;
            result.IsSignificant = affectedPositions.Count > 5;
            result.RequiresConfirmation = _confirmCascadingRemoval && result.IsSignificant;
            result.PreventionReasons = ValidateRemovalSafety(affectedPositions.ToList());

            return result;
        }

        /// <summary>
        /// Execute cascading removal starting from specified position
        /// </summary>
        public void ExecuteCascadingRemoval(Vector3Int originPosition, bool skipConfirmation = false)
        {
            if (!_enableCascadingRemoval) return;

            var analysisResult = AnalyzeCascadingRemoval(originPosition);

            // Check safety constraints
            if (_preventMassRemoval && analysisResult.TotalAffected > _maxCascadeCount)
            {
                OnRemovalPrevented?.Invoke(analysisResult.AffectedPositions);
                return;
            }

            if (analysisResult.RequiresConfirmation && !skipConfirmation)
            {
                // In a real implementation, this would show confirmation UI
                ChimeraLogger.LogWarning($"[CascadingRemovalHandler] Cascading removal affects {analysisResult.TotalAffected} objects. Proceeding...");
            }

            // Start the removal process
            InitiateRemovalSequence(analysisResult.AffectedPositions, originPosition);
        }

        /// <summary>
        /// Register object placement and update dependencies
        /// </summary>
        public void RegisterObjectPlacement(Vector3Int position, GridPlaceable placeable)
        {
            UpdateObjectDependencies(position, placeable);
        }

        /// <summary>
        /// Unregister object and cleanup dependencies
        /// </summary>
        public void UnregisterObject(Vector3Int position)
        {
            CleanupObjectDependencies(position);
        }

        /// <summary>
        /// Check if removing an object would cause structural instability
        /// </summary>
        public bool WouldCauseStructuralFailure(Vector3Int position)
        {
            if (!_considerStructuralIntegrity) return false;

            var analysisResult = AnalyzeCascadingRemoval(position);
            return analysisResult.IsSignificant &&
                   analysisResult.PreventionReasons.Contains("Critical structural support");
        }

        #endregion

        #region Dependency Management

        private void UpdateDependencyRegistry()
        {
            // Clear existing registry
            _dependencyRegistry.Clear();
            _supportDependencies.Clear();
            _reverseDependencies.Clear();

            // Rebuild dependencies for all occupied positions
            foreach (var kvp in _gridSystem.GridCells)
            {
                var cell = kvp.Value;
                if (cell.IsOccupied && cell.OccupyingObject != null)
                {
                    UpdateObjectDependencies(cell.GridCoordinate, cell.OccupyingObject);
                }
            }
        }

        private void UpdateObjectDependencies(Vector3Int position, GridPlaceable placeable)
        {
            if (placeable == null) return;

            var dependencies = AnalyzeObjectDependencies(position, placeable);
            _dependencyRegistry[position] = dependencies;

            // Update support relationships
            foreach (var supportPos in dependencies.SupportedBy)
            {
                if (!_supportDependencies.ContainsKey(supportPos))
                    _supportDependencies[supportPos] = new List<Vector3Int>();

                if (!_supportDependencies[supportPos].Contains(position))
                    _supportDependencies[supportPos].Add(position);

                // Update reverse dependencies
                if (!_reverseDependencies.ContainsKey(position))
                    _reverseDependencies[position] = new List<Vector3Int>();

                if (!_reverseDependencies[position].Contains(supportPos))
                    _reverseDependencies[position].Add(supportPos);
            }
        }

        private DependencyInfo AnalyzeObjectDependencies(Vector3Int position, GridPlaceable placeable)
        {
            var info = new DependencyInfo
            {
                SupportedBy = new List<Vector3Int>(),
                Supports = new List<Vector3Int>(),
                Type = DependencyType.Structural,
                StructuralImportance = CalculateStructuralImportance(position, placeable),
                IsRequired = false
            };

            // Check foundation dependency (directly below)
            Vector3Int belowPos = new Vector3Int(position.x, position.y, position.z - 1);
            if (_gridSystem.IsValidGridPosition(belowPos))
            {
                var belowCell = _gridSystem.GetGridCell(belowPos);
                if (belowCell?.IsOccupied == true)
                {
                    info.SupportedBy.Add(belowPos);
                    info.Type = DependencyType.Foundation;
                }
            }

            // Check structural dependencies (adjacent positions)
            var adjacentPositions = GetAdjacentPositions(position);
            foreach (var adjPos in adjacentPositions)
            {
                var adjCell = _gridSystem.GetGridCell(adjPos);
                if (adjCell?.IsOccupied == true && IsStructurallyConnected(position, adjPos, placeable, adjCell.OccupyingObject))
                {
                    info.SupportedBy.Add(adjPos);
                }
            }

            // Check stacked dependencies (directly above)
            Vector3Int abovePos = new Vector3Int(position.x, position.y, position.z + 1);
            if (_gridSystem.IsValidGridPosition(abovePos))
            {
                var aboveCell = _gridSystem.GetGridCell(abovePos);
                if (aboveCell?.IsOccupied == true)
                {
                    info.Supports.Add(abovePos);
                }
            }

            return info;
        }

        private float CalculateStructuralImportance(Vector3Int position, GridPlaceable placeable)
        {
            float importance = 1f;

            // Foundation objects are more important
            if (position.z == 0)
                importance += 2f;

            // Objects supporting others are more important
            Vector3Int abovePos = new Vector3Int(position.x, position.y, position.z + 1);
            var aboveCell = _gridSystem.GetGridCell(abovePos);
            if (aboveCell?.IsOccupied == true)
                importance += 1.5f;

            // Large objects are more important
            var bounds = placeable.GetObjectBounds();
            float volume = bounds.size.x * bounds.size.y * bounds.size.z;
            importance += Mathf.Log10(volume + 1f);

            return importance;
        }

        private bool IsStructurallyConnected(Vector3Int pos1, Vector3Int pos2, GridPlaceable obj1, GridPlaceable obj2)
        {
            // For now, consider objects structurally connected if they're adjacent and have compatible types
            return Vector3Int.Distance(pos1, pos2) <= 1.5f &&
                   (obj1.Type == PlaceableType.Structure || obj1.Type == PlaceableType.Equipment) &&
                   (obj2.Type == PlaceableType.Structure || obj2.Type == PlaceableType.Equipment);
        }

        #endregion

        #region Cascade Analysis

        private void AnalyzeCascadeImpact(Vector3Int position, HashSet<Vector3Int> affectedPositions, int depth, RemovalReason reason)
        {
            if (depth > _maxAnalysisDepth || affectedPositions.Contains(position))
                return;

            affectedPositions.Add(position);

            // Check direct dependencies (objects this supports)
            if (_supportDependencies.ContainsKey(position))
            {
                foreach (var dependentPos in _supportDependencies[position])
                {
                    if (WouldLoseSupport(dependentPos, position))
                    {
                        var cascadeReason = DetermineCascadeReason(position, dependentPos);
                        AnalyzeCascadeImpact(dependentPos, affectedPositions, depth + 1, cascadeReason);
                    }
                }
            }

            // Check structural integrity impact
            if (_considerStructuralIntegrity)
            {
                var structurallyAffected = GetStructurallyAffectedPositions(position);
                foreach (var affectedPos in structurallyAffected)
                {
                    AnalyzeCascadeImpact(affectedPos, affectedPositions, depth + 1, RemovalReason.StructuralFailure);
                }
            }
        }

        private bool WouldLoseSupport(Vector3Int position, Vector3Int removedSupport)
        {
            if (!_dependencyRegistry.ContainsKey(position))
                return false;

            var dependencies = _dependencyRegistry[position];

            // If the removed position was the only support, the object loses support
            if (dependencies.SupportedBy.Contains(removedSupport))
            {
                var remainingSupports = dependencies.SupportedBy.Where(pos => pos != removedSupport).ToList();

                // Check if remaining supports are adequate
                return !HasAdequateRemainingSupport(position, remainingSupports);
            }

            return false;
        }

        private bool HasAdequateRemainingSupport(Vector3Int position, List<Vector3Int> remainingSupports)
        {
            // Ground level objects don't need support
            if (position.z == 0) return true;

            // Must have at least one foundation support for objects above ground
            bool hasFoundationSupport = remainingSupports.Any(pos => pos.z == position.z - 1);

            if (!hasFoundationSupport && position.z > 0)
                return false;

            // For structural objects, check if remaining supports can handle load
            if (_foundationSystem != null)
            {
                foreach (var supportPos in remainingSupports)
                {
                    var capacity = _foundationSystem.GetFoundationCapacity(supportPos);
                    if (capacity.IsOverloaded)
                        return false;
                }
            }

            return true;
        }

        private RemovalReason DetermineCascadeReason(Vector3Int removedPos, Vector3Int affectedPos)
        {
            // Direct vertical support
            if (removedPos.x == affectedPos.x && removedPos.y == affectedPos.y && removedPos.z == affectedPos.z - 1)
                return RemovalReason.FoundationLost;

            // Adjacent structural support
            if (Vector3Int.Distance(removedPos, affectedPos) <= 1.5f)
                return RemovalReason.StructuralFailure;

            return RemovalReason.AccessLost;
        }

        private List<Vector3Int> GetStructurallyAffectedPositions(Vector3Int position)
        {
            var affected = new List<Vector3Int>();

            // Check positions within structural analysis radius
            var nearbyPositions = GetPositionsWithinRadius(position, _structuralAnalysisRadius);

            foreach (var pos in nearbyPositions)
            {
                if (IsStructurallyDependent(pos, position))
                    affected.Add(pos);
            }

            return affected;
        }

        private bool IsStructurallyDependent(Vector3Int position, Vector3Int supportPosition)
        {
            if (!_dependencyRegistry.ContainsKey(position))
                return false;

            var dependencies = _dependencyRegistry[position];
            return dependencies.SupportedBy.Contains(supportPosition) && dependencies.IsRequired;
        }

        #endregion

        #region Removal Execution

        private void InitiateRemovalSequence(List<Vector3Int> positionsToRemove, Vector3Int originPosition)
        {
            if (positionsToRemove.Count == 0) return;

            OnCascadingRemovalStarted?.Invoke(positionsToRemove);

            // Sort removal order (bottom-up to prevent floating objects)
            var sortedPositions = positionsToRemove.OrderByDescending(pos => pos.z).ToList();

            float currentTime = Time.time;
            for (int i = 0; i < sortedPositions.Count; i++)
            {
                var removalTask = new RemovalTask
                {
                    Position = sortedPositions[i],
                    RemovalTime = currentTime + (i * _removalDelay),
                    Reason = sortedPositions[i] == originPosition ? RemovalReason.Direct : RemovalReason.FoundationLost,
                    CausingPosition = originPosition
                };

                _removalQueue.Enqueue(removalTask);
                _pendingRemoval.Add(sortedPositions[i]);
            }

            _isProcessingRemoval = true;
        }

        private void ProcessRemovalQueue()
        {
            if (!_isProcessingRemoval || _removalQueue.Count == 0) return;

            float currentTime = Time.time;
            var completedRemovals = new List<Vector3Int>();

            while (_removalQueue.Count > 0)
            {
                var task = _removalQueue.Peek();

                if (currentTime >= task.RemovalTime)
                {
                    _removalQueue.Dequeue();
                    ExecuteRemoval(task);
                    completedRemovals.Add(task.Position);
                    _pendingRemoval.Remove(task.Position);
                }
                else
                {
                    break; // Wait for next removal time
                }
            }

            if (_removalQueue.Count == 0 && _isProcessingRemoval)
            {
                _isProcessingRemoval = false;
                OnCascadingRemovalCompleted?.Invoke(completedRemovals);
            }
        }

        private void ExecuteRemoval(RemovalTask task)
        {
            var cell = _gridSystem.GetGridCell(task.Position);
            if (cell?.IsOccupied == true && cell.OccupyingObject != null)
            {
                // Notify other systems about the removal
                if (_verticalManager != null)
                    _verticalManager.UnregisterPlacement(task.Position);

                if (_foundationSystem != null)
                    _foundationSystem.UnregisterFoundation(task.Position);

                // Remove from grid - pass the GridPlaceable object, not position
                _gridSystem.RemoveObject(cell.OccupyingObject);

                // Clean up dependencies
                CleanupObjectDependencies(task.Position);

                OnObjectRemoved?.Invoke(task.Position);
            }
        }

        #endregion

        #region Utility Methods

        private List<Vector3Int> GetAdjacentPositions(Vector3Int position)
        {
            return new List<Vector3Int>
            {
                position + Vector3Int.right,
                position + Vector3Int.left,
                position + Vector3Int.forward,
                position + Vector3Int.back
            }.Where(pos => _gridSystem.IsValidGridPosition(pos)).ToList();
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

        private void CleanupObjectDependencies(Vector3Int position)
        {
            _dependencyRegistry.Remove(position);
            _supportDependencies.Remove(position);
            _reverseDependencies.Remove(position);

            // Remove references from other objects
            foreach (var kvp in _supportDependencies.ToList())
            {
                kvp.Value.Remove(position);
                if (kvp.Value.Count == 0)
                    _supportDependencies.Remove(kvp.Key);
            }

            foreach (var kvp in _reverseDependencies.ToList())
            {
                kvp.Value.Remove(position);
                if (kvp.Value.Count == 0)
                    _reverseDependencies.Remove(kvp.Key);
            }
        }

        private List<string> ValidateRemovalSafety(List<Vector3Int> positionsToRemove)
        {
            var reasons = new List<string>();

            // Check for critical infrastructure
            foreach (var pos in positionsToRemove)
            {
                var cell = _gridSystem.GetGridCell(pos);
                if (cell?.OccupyingObject?.Type == PlaceableType.Structure)
                {
                    reasons.Add("Critical infrastructure would be removed");
                }

                // Check structural importance
                if (_dependencyRegistry.ContainsKey(pos))
                {
                    var dependencies = _dependencyRegistry[pos];
                    if (dependencies.StructuralImportance > 5f)
                    {
                        reasons.Add("Critical structural support");
                    }
                }
            }

            return reasons.Distinct().ToList();
        }

        #endregion

        #region Data Structures

        public struct CascadeAnalysisResult
        {
            public List<Vector3Int> AffectedPositions;
            public int TotalAffected;
            public bool IsSignificant;
            public bool RequiresConfirmation;
            public List<string> PreventionReasons;
        }

        #endregion

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            _removalQueue.Clear();
            _pendingRemoval.Clear();
            _dependencyRegistry.Clear();
            _supportDependencies.Clear();
            _reverseDependencies.Clear();
        }

        #region ITickable Implementation

        int ITickable.Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }

        // NOTE: ITickable Tick method implementation - original Tick method exists above

        #endregion
    }
}
