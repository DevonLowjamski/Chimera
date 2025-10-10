using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Data.Progression;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Skill tree manager - handles progression leaf unlocking and skill points.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "Skill Points are earned through gameplay, used for progression and trading"
    ///
    /// **Earning Skill Points:**
    /// - Complete objectives (first harvest, breed F5 strain, etc.)
    /// - Achieve milestones (100 plants harvested, $100K revenue, etc.)
    /// - Excel in cultivation (high THC, high yield per sqft, etc.)
    /// - Breed exceptional genetics (discover rare phenotype, etc.)
    ///
    /// **Spending Skill Points:**
    /// - Unlock nodes in skill tree (1-3 points each)
    /// - Purchase genetics in marketplace (player-set prices)
    /// - Purchase schematics in marketplace (player-set prices)
    ///
    /// **Save File Specific:**
    /// Each save has its own skill tree progress.
    /// Achievements are system-wide, but skill trees reset per save.
    ///
    /// PROGRESSION TRACKING:
    /// "The leaf visually expands as players progress" - UI shows growth!
    /// </summary>
    public class SkillTreeManager : MonoBehaviour
    {
        [Header("Skill Tree Configuration")]
        [SerializeField] private SkillTreeData _skillTreeData;
        [SerializeField] private int _startingSkillPoints = 3;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;

        // Current save file's skill tree state
        private Dictionary<string, bool> _unlockedNodes = new Dictionary<string, bool>();
        private int _availableSkillPoints;
        private int _totalSkillPointsEarned;
        private int _totalSkillPointsSpent;

        // Events
        public event Action<SkillNode> OnNodeUnlocked;
        public event Action<int> OnSkillPointsChanged;
        public event Action<SkillBranch> OnBranchCompleted;

        // Service flag
        private bool _isInitialized = false;

        private void Start()
        {
            InitializeSkillTree();
        }

        private void InitializeSkillTree()
        {
            if (_isInitialized)
                return;

            if (_skillTreeData == null)
            {
                ChimeraLogger.LogWarning("PROGRESSION",
                    "SkillTreeManager: No SkillTreeData assigned", this);
                return;
            }

            // Initialize with starting points (new save file)
            _availableSkillPoints = _startingSkillPoints;
            _totalSkillPointsEarned = _startingSkillPoints;
            _totalSkillPointsSpent = 0;

            // Initialize unlock state for all nodes
            foreach (var node in _skillTreeData.GetAllNodes())
            {
                _unlockedNodes[node.NodeId] = false;
            }

            _isInitialized = true;

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("PROGRESSION",
                    $"Skill tree initialized: {_skillTreeData.TreeName} " +
                    $"({_skillTreeData.GetTotalNodeCount()} nodes, {_availableSkillPoints} starting points)", this);
            }
        }

        #region Skill Point Management

        /// <summary>
        /// Awards skill points to player.
        ///
        /// GAMEPLAY:
        /// - Player completes objective: "First Harvest" → +2 Skill Points
        /// - Player achieves milestone: "100 Plants Harvested" → +5 Skill Points
        /// - Player breeds exceptional genetics: "Discovered 30% THC phenotype" → +3 Skill Points
        /// </summary>
        public void AwardSkillPoints(int amount, string reason = "")
        {
            if (amount <= 0)
                return;

            _availableSkillPoints += amount;
            _totalSkillPointsEarned += amount;

            OnSkillPointsChanged?.Invoke(_availableSkillPoints);

            ChimeraLogger.Log("PROGRESSION",
                $"+{amount} Skill Points awarded ({reason}). Total: {_availableSkillPoints}", this);
        }

        /// <summary>
        /// Spends skill points (for unlocking or trading).
        /// Returns true if successful.
        /// </summary>
        public bool SpendSkillPoints(int amount, string reason = "")
        {
            if (amount <= 0)
                return false;

            if (_availableSkillPoints < amount)
            {
                if (_enableDebugLogging)
                {
                    ChimeraLogger.LogWarning("PROGRESSION",
                        $"Cannot spend {amount} skill points: only {_availableSkillPoints} available", this);
                }
                return false;
            }

            _availableSkillPoints -= amount;
            _totalSkillPointsSpent += amount;

            OnSkillPointsChanged?.Invoke(_availableSkillPoints);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("PROGRESSION",
                    $"-{amount} Skill Points spent ({reason}). Remaining: {_availableSkillPoints}", this);
            }

            return true;
        }

        // Gets available skill points.
        public int AvailableSkillPoints => _availableSkillPoints;

        // Gets total skill points earned (lifetime).
        public int TotalSkillPointsEarned => _totalSkillPointsEarned;

        // Gets total skill points spent.
        public int TotalSkillPointsSpent => _totalSkillPointsSpent;

        #endregion

        #region Node Unlocking

        /// <summary>
        /// Unlocks a skill node.
        ///
        /// GAMEPLAY:
        /// - Player clicks node in skill tree UI
        /// - Checks: enough skill points? prerequisites met?
        /// - Spends points → unlocks node → applies effect
        /// - UI shows node illuminated, leaf grows
        /// </summary>
        public bool UnlockNode(string nodeId)
        {
            var node = FindNodeById(nodeId);
            if (node == null)
            {
                ChimeraLogger.LogWarning("PROGRESSION",
                    $"Cannot unlock node: {nodeId} not found", this);
                return false;
            }

            // Check if already unlocked
            if (IsNodeUnlocked(nodeId))
            {
                if (_enableDebugLogging)
                {
                    ChimeraLogger.LogWarning("PROGRESSION",
                        $"Node {nodeId} already unlocked", this);
                }
                return false;
            }

            // Check prerequisites
            if (!ArePrerequisitesMet(node))
            {
                if (_enableDebugLogging)
                {
                    ChimeraLogger.LogWarning("PROGRESSION",
                        $"Cannot unlock {nodeId}: prerequisites not met", this);
                }
                return false;
            }

            // Check skill point cost
            if (!SpendSkillPoints(node.SkillPointCost, $"Unlock {node.NodeName}"))
            {
                return false;
            }

            // Unlock the node
            _unlockedNodes[nodeId] = true;
            node.Unlock();

            // Apply node effect
            ApplyNodeEffect(node);

            OnNodeUnlocked?.Invoke(node);

            ChimeraLogger.Log("PROGRESSION",
                $"✅ Node unlocked: {node.NodeName} ({node.Effect.GetEffectDescription()})", this);

            // Check if branch completed
            CheckBranchCompletion(node);

            return true;
        }

        // Checks if node is unlocked.
        public bool IsNodeUnlocked(string nodeId)
        {
            return _unlockedNodes.TryGetValue(nodeId, out bool unlocked) && unlocked;
        }

        // Checks if node can be unlocked (prerequisites met, not already unlocked).
        public bool CanUnlockNode(string nodeId)
        {
            var node = FindNodeById(nodeId);
            if (node == null)
                return false;

            if (IsNodeUnlocked(nodeId))
                return false;

            if (!ArePrerequisitesMet(node))
                return false;

            if (_availableSkillPoints < node.SkillPointCost)
                return false;

            return true;
        }

        // Checks if node prerequisites are met.
        private bool ArePrerequisitesMet(SkillNode node)
        {
            if (node.Prerequisites == null || node.Prerequisites.Count == 0)
                return true;

            foreach (var prereqId in node.Prerequisites)
            {
                if (!IsNodeUnlocked(prereqId))
                    return false;
            }

            return true;
        }

        #endregion

        #region Node Effects

        /// <summary>
        /// Applies node effect when unlocked.
        ///
        /// GAMEPLAY:
        /// - UnlockFeature: "Tissue Culture" tab becomes visible in genetics menu
        /// - StatModifier: "+10% yield" applies to all plants
        /// - UnlockItem: "Drip irrigation" appears in construction menu
        /// - UnlockTechnique: "Topping" becomes available in cultivation actions
        /// </summary>
        private void ApplyNodeEffect(SkillNode node)
        {
            switch (node.Effect.EffectType)
            {
                case SkillNodeEffectType.UnlockFeature:
                    UnlockFeature(node.Effect.UnlockFeatureId);
                    break;

                case SkillNodeEffectType.StatModifier:
                    ApplyStatModifier(node.Effect.StatModifierTarget, node.Effect.StatModifierPercent);
                    break;

                case SkillNodeEffectType.UnlockItem:
                    UnlockItem(node.Effect.UnlockItemId);
                    break;

                case SkillNodeEffectType.UnlockTechnique:
                    UnlockTechnique(node.Effect.UnlockFeatureId);
                    break;
            }
        }

        private void UnlockFeature(string featureId)
        {
            // TODO: Integrate with feature unlock system
            // For now, just log
            ChimeraLogger.Log("PROGRESSION",
                $"Feature unlocked: {featureId}", this);
        }

        private void ApplyStatModifier(string stat, float percentBonus)
        {
            // TODO: Integrate with stat modifier system
            // For now, just log
            ChimeraLogger.Log("PROGRESSION",
                $"Stat modifier applied: +{percentBonus * 100f:F0}% {stat}", this);
        }

        private void UnlockItem(string itemId)
        {
            // TODO: Integrate with item unlock system
            // For now, just log
            ChimeraLogger.Log("PROGRESSION",
                $"Item unlocked: {itemId}", this);
        }

        private void UnlockTechnique(string techniqueId)
        {
            // TODO: Integrate with technique unlock system
            // For now, just log
            ChimeraLogger.Log("PROGRESSION",
                $"Technique unlocked: {techniqueId}", this);
        }

        #endregion

        #region Branch Completion

        // Checks if a branch is completed after unlocking a node.
        private void CheckBranchCompletion(SkillNode unlockedNode)
        {
            var branch = FindBranchContainingNode(unlockedNode);
            if (branch == null)
                return;

            // Check if all nodes in branch are unlocked
            bool allUnlocked = branch.Nodes.All(n => IsNodeUnlocked(n.NodeId));

            if (allUnlocked)
            {
                OnBranchCompleted?.Invoke(branch);

                ChimeraLogger.Log("PROGRESSION",
                    $"🌿 Branch completed: {branch.BranchName}!", this);
            }
        }

        // Gets completion percentage for entire skill tree.
        public float GetOverallCompletion()
        {
            int totalNodes = _skillTreeData.GetTotalNodeCount();
            if (totalNodes == 0)
                return 0f;

            int unlockedCount = _unlockedNodes.Values.Count(unlocked => unlocked);
            return (float)unlockedCount / totalNodes;
        }

        // Gets completion percentage for a specific branch.
        public float GetBranchCompletion(SkillBranchType branchType)
        {
            var branch = _skillTreeData.GetBranch(branchType);
            if (branch == null || branch.Nodes.Count == 0)
                return 0f;

            int unlocked = branch.Nodes.Count(n => IsNodeUnlocked(n.NodeId));
            return (float)unlocked / branch.Nodes.Count;
        }

        #endregion

        #region Query Methods

        // Finds node by ID.
        private SkillNode FindNodeById(string nodeId)
        {
            return _skillTreeData.GetAllNodes().Find(n => n.NodeId == nodeId);
        }

        // Finds which branch contains a node.
        private SkillBranch FindBranchContainingNode(SkillNode node)
        {
            foreach (var branch in _skillTreeData.GetAllBranches())
            {
                if (branch.Nodes.Contains(node))
                    return branch;
            }
            return null;
        }

        // Gets all unlocked nodes.
        public List<SkillNode> GetUnlockedNodes()
        {
            return _skillTreeData.GetAllNodes().FindAll(n => IsNodeUnlocked(n.NodeId));
        }

        // Gets all available nodes (can be unlocked right now).
        public List<SkillNode> GetAvailableNodes()
        {
            return _skillTreeData.GetAllNodes().FindAll(n => CanUnlockNode(n.NodeId));
        }

        // Gets skill tree data (for UI).
        public SkillTreeData GetSkillTreeData()
        {
            return _skillTreeData;
        }

        #endregion

        #region Save/Load Support

        // Gets save data for persistence.
        public SkillTreeSaveData GetSaveData()
        {
            return new SkillTreeSaveData
            {
                UnlockedNodeIds = _unlockedNodes.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList(),
                AvailableSkillPoints = _availableSkillPoints,
                TotalSkillPointsEarned = _totalSkillPointsEarned,
                TotalSkillPointsSpent = _totalSkillPointsSpent
            };
        }

        // Loads save data.
        public void LoadSaveData(SkillTreeSaveData saveData)
        {
            if (saveData.UnlockedNodeIds == null)
                return;

            // Clear current state
            foreach (var key in _unlockedNodes.Keys.ToList())
            {
                _unlockedNodes[key] = false;
            }

            // Load unlocked nodes
            foreach (var nodeId in saveData.UnlockedNodeIds)
            {
                if (_unlockedNodes.ContainsKey(nodeId))
                {
                    _unlockedNodes[nodeId] = true;

                    // Apply node effect
                    var node = FindNodeById(nodeId);
                    if (node != null)
                    {
                        node.Unlock();
                        ApplyNodeEffect(node);
                    }
                }
            }

            // Load skill points
            _availableSkillPoints = saveData.AvailableSkillPoints;
            _totalSkillPointsEarned = saveData.TotalSkillPointsEarned;
            _totalSkillPointsSpent = saveData.TotalSkillPointsSpent;

            OnSkillPointsChanged?.Invoke(_availableSkillPoints);

            ChimeraLogger.Log("PROGRESSION",
                $"Skill tree loaded: {saveData.UnlockedNodeIds.Count} nodes unlocked, " +
                $"{_availableSkillPoints} points available", this);
        }

        #endregion
    }

    /// <summary>
    /// Save data for skill tree (per save file).
    /// </summary>
    [Serializable]
    public struct SkillTreeSaveData
    {
        public List<string> UnlockedNodeIds;
        public int AvailableSkillPoints;
        public int TotalSkillPointsEarned;
        public int TotalSkillPointsSpent;
    }
}
