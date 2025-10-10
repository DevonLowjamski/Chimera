using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Data.Progression
{
    /// <summary>
    /// Skill tree data - cannabis leaf-shaped progression system.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "The progression system is visualized as a cannabis leaf with five points,
    /// unique to each save file"
    ///
    /// **Five Leaf Points (Branches):**
    /// 1. **Cultivation** - Growing techniques (irrigation, IPM, training)
    /// 2. **Construction** - Building options (plumbing, rooms, equipment)
    /// 3. **Genetics** - Breeding capabilities (pheno-hunting, tissue culture)
    /// 4. **Automation** - Task outsourcing (hiring employees, automation)
    /// 5. **Research** - Advanced features (breeding techniques, analysis)
    ///
    /// **Node Scaling:**
    /// - Genetics: 7-10 nodes (most important)
    /// - Cultivation: 7-10 nodes
    /// - Construction: 5-7 nodes
    /// - Automation: 3-5 nodes
    /// - Research: 2-3 nodes (advanced, unlocked late)
    ///
    /// **Skill Points:**
    /// - Earned via: objectives, harvests, milestones
    /// - Spent to: unlock nodes, access new features
    /// - Dual use: progression + marketplace trading
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see a beautiful growing cannabis leaf, not complex node graphs.
    /// They experience unlocking "Tissue Culture" not "node ID 34 in branch 3".
    /// </summary>
    [CreateAssetMenu(fileName = "SkillTreeData", menuName = "Chimera/Progression/Skill Tree Data")]
    public class SkillTreeData : ScriptableObject
    {
        [Header("Skill Tree Configuration")]
        [SerializeField] private string _treeId = "MainProgressionLeaf";
        [SerializeField] private string _treeName = "Progression Leaf";

        [Header("Five Leaf Branches")]
        [SerializeField] private SkillBranch _cultivationBranch;
        [SerializeField] private SkillBranch _constructionBranch;
        [SerializeField] private SkillBranch _geneticsBranch;
        [SerializeField] private SkillBranch _automationBranch;
        [SerializeField] private SkillBranch _researchBranch;

        /// <summary>
        /// Gets all skill branches (5 leaf points).
        /// </summary>
        public List<SkillBranch> GetAllBranches()
        {
            return new List<SkillBranch>
            {
                _cultivationBranch,
                _constructionBranch,
                _geneticsBranch,
                _automationBranch,
                _researchBranch
            };
        }

        /// <summary>
        /// Gets a specific branch by type.
        /// </summary>
        public SkillBranch GetBranch(SkillBranchType branchType)
        {
            return branchType switch
            {
                SkillBranchType.Cultivation => _cultivationBranch,
                SkillBranchType.Construction => _constructionBranch,
                SkillBranchType.Genetics => _geneticsBranch,
                SkillBranchType.Automation => _automationBranch,
                SkillBranchType.Research => _researchBranch,
                _ => null
            };
        }

        /// <summary>
        /// Gets total node count across all branches.
        /// </summary>
        public int GetTotalNodeCount()
        {
            int total = 0;
            foreach (var branch in GetAllBranches())
            {
                if (branch != null)
                    total += branch.Nodes.Count;
            }
            return total;
        }

        /// <summary>
        /// Gets all nodes flattened.
        /// </summary>
        public List<SkillNode> GetAllNodes()
        {
            var allNodes = new List<SkillNode>();
            foreach (var branch in GetAllBranches())
            {
                if (branch != null)
                    allNodes.AddRange(branch.Nodes);
            }
            return allNodes;
        }

        public string TreeId => _treeId;
        public string TreeName => _treeName;
    }

    /// <summary>
    /// Represents one of the five leaf branches.
    /// </summary>
    [Serializable]
    public class SkillBranch
    {
        [Header("Branch Identity")]
        public string BranchId;
        public string BranchName;
        public SkillBranchType BranchType;
        public Color BranchColor = Color.green;
        public Sprite BranchIcon;

        [Header("Branch Nodes")]
        [Tooltip("3-10 nodes per branch, scaled by importance")]
        public List<SkillNode> Nodes = new List<SkillNode>();

        [Header("Visual Layout")]
        [Tooltip("Position on the cannabis leaf (0-360 degrees from center)")]
        [Range(0f, 360f)]
        public float LeafAngle = 0f;

        [Tooltip("Distance from center of leaf")]
        public float LeafRadius = 100f;

        /// <summary>
        /// Gets unlocked nodes in this branch.
        /// </summary>
        public List<SkillNode> GetUnlockedNodes()
        {
            return Nodes.FindAll(n => n.IsUnlocked);
        }

        /// <summary>
        /// Gets available nodes (can be unlocked now).
        /// </summary>
        public List<SkillNode> GetAvailableNodes()
        {
            return Nodes.FindAll(n => !n.IsUnlocked && ArePrerequisitesMet(n));
        }

        /// <summary>
        /// Checks if node prerequisites are met.
        /// </summary>
        private bool ArePrerequisitesMet(SkillNode node)
        {
            if (node.Prerequisites == null || node.Prerequisites.Count == 0)
                return true;

            foreach (var prereqId in node.Prerequisites)
            {
                var prereqNode = Nodes.Find(n => n.NodeId == prereqId);
                if (prereqNode == null || !prereqNode.IsUnlocked)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets completion percentage (unlocked / total).
        /// </summary>
        public float GetCompletionPercentage()
        {
            if (Nodes.Count == 0)
                return 0f;

            int unlocked = Nodes.Count(n => n.IsUnlocked);
            return (float)unlocked / Nodes.Count;
        }
    }

    /// <summary>
    /// Represents a single skill node.
    /// </summary>
    [Serializable]
    public class SkillNode
    {
        [Header("Node Identity")]
        public string NodeId;
        public string NodeName;
        public string Description;
        public Sprite Icon;

        [Header("Unlock Requirements")]
        [Tooltip("Skill points required to unlock")]
        public int SkillPointCost = 1;

        [Tooltip("Node IDs that must be unlocked first")]
        public List<string> Prerequisites = new List<string>();

        [Header("Effects")]
        [Tooltip("What this node unlocks")]
        public SkillNodeEffect Effect;

        [Header("State (Runtime)")]
        [NonSerialized] public bool IsUnlocked = false;

        /// <summary>
        /// Unlocks this node.
        /// </summary>
        public void Unlock()
        {
            IsUnlocked = true;
        }

        /// <summary>
        /// Locks this node (for testing/reset).
        /// </summary>
        public void Lock()
        {
            IsUnlocked = false;
        }
    }

    /// <summary>
    /// Defines what a skill node unlocks.
    /// </summary>
    [Serializable]
    public class SkillNodeEffect
    {
        public SkillNodeEffectType EffectType;

        [Header("Feature Unlock")]
        [Tooltip("Feature ID to unlock (e.g., 'TissueCulture', 'DripIrrigation')")]
        public string UnlockFeatureId;

        [Header("Stat Modifiers")]
        [Tooltip("Percentage bonus to apply (e.g., +10% yield = 0.10)")]
        public float StatModifierPercent = 0f;

        [Tooltip("Which stat to modify")]
        public string StatModifierTarget;

        [Header("Item Unlock")]
        [Tooltip("Item/equipment unlocked for purchase")]
        public string UnlockItemId;

        /// <summary>
        /// Gets effect description for UI.
        /// </summary>
        public string GetEffectDescription()
        {
            return EffectType switch
            {
                SkillNodeEffectType.UnlockFeature => $"Unlocks: {UnlockFeatureId}",
                SkillNodeEffectType.StatModifier => $"+{StatModifierPercent * 100f:F0}% {StatModifierTarget}",
                SkillNodeEffectType.UnlockItem => $"Unlocks item: {UnlockItemId}",
                SkillNodeEffectType.UnlockTechnique => $"Unlocks technique: {UnlockFeatureId}",
                _ => "No effect"
            };
        }
    }

    /// <summary>
    /// The five leaf branches matching gameplay doc.
    /// </summary>
    public enum SkillBranchType
    {
        Cultivation,    // 7-10 nodes: irrigation, IPM, training techniques
        Construction,   // 5-7 nodes: plumbing, rooms, equipment
        Genetics,       // 7-10 nodes: pheno-hunting, tissue culture, micropropagation
        Automation,     // 3-5 nodes: hiring employees, automated tasks
        Research        // 2-3 nodes: advanced breeding, analysis tools
    }

    /// <summary>
    /// Types of effects nodes can have.
    /// </summary>
    public enum SkillNodeEffectType
    {
        UnlockFeature,      // Unlocks a new game feature
        StatModifier,       // Provides a stat bonus
        UnlockItem,         // Makes an item available for purchase
        UnlockTechnique     // Unlocks a new technique/action
    }
}
