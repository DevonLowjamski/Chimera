using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation
{
    // Local type definitions to replace deleted types
    public enum TreeGrowthLevel
    {
        Seed, Seedling, Vegetative, Mature, Flowering, FullyFlowered
    }

    /// <summary>
    /// Data structures for tree visualization and skill tree rendering
    /// </summary>
    
    /// <summary>
    /// State of tree visualization for skill trees
    /// </summary>
    [System.Serializable]
    public class TreeVisualizationState
    {
        public string StateId;
        public string StateName;
        public TreeRenderMode RenderMode = TreeRenderMode.ThreeD;
        public float ZoomLevel = 1f;
        public Vector3 CameraPosition = Vector3.zero;
        public Vector3 CameraRotation = Vector3.zero;
        public bool ShowConnections = true;
        public bool ShowLabels = true;
        public bool ShowEffects = true;
        public float OverallTreeHealth = 1.0f;
        public Dictionary<string, bool> VisibleBranches = new Dictionary<string, bool>();
        public PlantGrowthStage TreeGrowthState = PlantGrowthStage.Seedling;
        
        // Additional properties for skill tree visualization compatibility
        public TreeGrowthLevel TreeGrowthStage = TreeGrowthLevel.Seed;
        public float TreeVibrancy = 0.1f;
        public Dictionary<SkillBranch, float> BranchVibrancy = new Dictionary<SkillBranch, float>();
        public Dictionary<SkillBranch, TreeGrowthLevel> BranchGrowthLevels = new Dictionary<SkillBranch, TreeGrowthLevel>();
        public Dictionary<string, bool> NodeUnlockStates = new Dictionary<string, bool>();
        public float LastUpdateTime = 0f;
        public bool IsAnimating = false;
        
        // Additional calculated properties
        public float CanopyDevelopment = 0f;
        public float RootSystemStrength = 0f;
        public float FloweringProgress = 0f;
        
        public TreeVisualizationState()
        {
            StateId = System.Guid.NewGuid().ToString();
            InitializeDefaults();
        }
        
        /// <summary>
        /// Initialize default values
        /// </summary>
        private void InitializeDefaults()
        {
            foreach (SkillBranch branch in System.Enum.GetValues(typeof(SkillBranch)))
            {
                BranchVibrancy[branch] = 0.1f;
                BranchGrowthLevels[branch] = TreeGrowthLevel.Seed;
            }
            
            // Calculate derived properties
            CanopyDevelopment = OverallTreeHealth * 0.8f + TreeVibrancy * 0.2f;
            RootSystemStrength = OverallTreeHealth;
            FloweringProgress = TreeGrowthStage >= TreeGrowthLevel.Flowering ? 1.0f : 0.0f;
        }
    }
    
    /// <summary>
    /// Rendering mode for skill trees
    /// </summary>
    public enum TreeRenderMode
    {
        TwoD,
        ThreeD,
        Hybrid,
        Simplified
    }
    
    /// <summary>
    /// Extension methods for TreeVisualizationState
    /// </summary>
    public static class TreeVisualizationStateExtensions
    {
        /// <summary>
        /// Extension method for CanopyDevelopment
        /// </summary>
        public static float CanopyDevelopment(this TreeVisualizationState state)
        {
            return state.OverallTreeHealth * 0.8f + state.TreeVibrancy * 0.2f;
        }
        
        /// <summary>
        /// Extension method for RootSystemStrength
        /// </summary>
        public static float RootSystemStrength(this TreeVisualizationState state)
        {
            return state.OverallTreeHealth;
        }
        
        /// <summary>
        /// Extension method for FloweringProgress
        /// </summary>
        public static float FloweringProgress(this TreeVisualizationState state)
        {
            return state.TreeGrowthStage >= TreeGrowthLevel.Flowering ? 1.0f : 0.0f;
        }
    }
}