using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Care Tool Library - Library of tools available for plant care and cultivation
    /// Defines tool types, effectiveness, and unlock requirements
    /// Part of Enhanced Scientific Gaming System v2.0
    /// </summary>
    [CreateAssetMenu(fileName = "New Care Tool Library", menuName = "Project Chimera/Cultivation/Care Tool Library")]
    public class CareToolLibrarySO : ChimeraDataSO
    {
        [Header("Tool Categories")]
        public List<CareToolCategory> ToolCategories = new List<CareToolCategory>();
        
        [Header("Individual Tools")]
        public List<CareToolDefinition> AvailableTools = new List<CareToolDefinition>();
        
        [Header("Unlock Progression")]
        public List<ToolUnlockRequirement> UnlockRequirements = new List<ToolUnlockRequirement>();
        
        public CareToolDefinition GetTool(string toolId)
        {
            return AvailableTools.Find(t => t.ToolId == toolId);
        }
        
        public List<CareToolDefinition> GetToolsByCategory(ToolCategory category)
        {
            return AvailableTools.FindAll(t => t.Category == category);
        }
        
        public bool IsToolUnlocked(string toolId, int playerLevel, List<string> unlockedAchievements)
        {
            var requirement = UnlockRequirements.Find(r => r.ToolId == toolId);
            if (requirement == null) return true;
            
            return playerLevel >= requirement.RequiredLevel && 
                   unlockedAchievements.Contains(requirement.RequiredAchievement);
        }
    }
    
    [System.Serializable]
    public class CareToolCategory
    {
        public string CategoryName;
        public ToolCategory Category;
        public string Description;
        public Sprite CategoryIcon;
    }
    
    [System.Serializable]
    public class CareToolDefinition
    {
        public string ToolId;
        public string ToolName;
        public ToolCategory Category;
        [Range(0.1f, 5.0f)] public float EffectivenessMultiplier = 1.0f;
        [Range(0.1f, 2.0f)] public float QualityBonus = 1.0f;
        public bool IsAutomationTool = false;
        public List<string> SupportedActions = new List<string>();
        public string Description;
        public Sprite ToolIcon;
        public GameObject ToolPrefab;
    }
    
    [System.Serializable]
    public class ToolUnlockRequirement
    {
        public string ToolId;
        [Range(1, 100)] public int RequiredLevel = 1;
        public string RequiredAchievement;
        public List<string> PrerequisiteTools = new List<string>();
    }
    
    public enum ToolCategory
    {
        BasicCare,
        AdvancedCare,
        Monitoring,
        Automation,
        Specialized,
        Precision,
        Maintenance,
        Innovation
    }
}