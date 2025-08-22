using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Construction
{
    /// <summary>
    /// Unlock status for a specific schematic
    /// </summary>
    [System.Serializable]
    public class SchematicUnlockStatus
    {
        public string SchematicId;
        public string SchematicName;
        public bool IsUnlocked;
        public System.DateTime UnlockDate;
        public float SkillPointsSpent;
        public bool CanUnlock => !IsUnlocked;
    }
    
    /// <summary>
    /// Detailed unlock requirements for a schematic
    /// </summary>
    [System.Serializable]
    public class SchematicUnlockRequirements
    {
        public SchematicSO Schematic;
        public bool RequiresUnlock;
        public bool AllRequirementsMet;
        
        // Skill point requirements
        public float SkillPointCost;
        public bool HasSufficientSkillPoints;
        
        // Level requirements
        public int RequiredLevel;
        public int CurrentLevel;
        public bool MeetsLevelRequirement;
        
        // Prerequisite requirements
        public List<string> PrerequisiteSchematicIds = new List<string>();
        public List<string> UnlockedPrerequisites = new List<string>();
        public List<string> MissingPrerequisites = new List<string>();
        public bool MeetsPrerequisiteRequirement;
    }
    
    /// <summary>
    /// Display data for schematic unlock UI
    /// </summary>
    [System.Serializable]
    public class SchematicUnlockDisplayData
    {
        public SchematicSO Schematic;
        public bool IsUnlocked;
        public bool CanUnlock;
        public SchematicUnlockRequirements Requirements;
        public string UnlockHint;
        public float ProgressPercentage;
    }
}