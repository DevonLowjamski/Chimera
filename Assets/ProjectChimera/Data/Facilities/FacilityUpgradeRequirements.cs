using UnityEngine;

namespace ProjectChimera.Data.Facilities
{
    /// <summary>
    /// Data structure defining requirements for facility upgrades
    /// </summary>
    public struct FacilityUpgradeRequirements
    {
        [Header("Resource Requirements")]
        public float RequiredCapital;
        public int RequiredSkillPoints;
        public float RequiredExperience;
        
        [Header("Achievement Requirements")]
        public int RequiredHarvests;
        public int RequiredPlants;
        public float RequiredYield;
        
        [Header("Facility Requirements")]
        public int MinimumTierLevel;
        public bool RequiresSpecialLicense;
        
        public static FacilityUpgradeRequirements Default => new FacilityUpgradeRequirements
        {
            RequiredCapital = 10000f,
            RequiredSkillPoints = 50,
            RequiredExperience = 100f,
            RequiredHarvests = 3,
            RequiredPlants = 5,
            RequiredYield = 50f,
            MinimumTierLevel = 1,
            RequiresSpecialLicense = false
        };
        
        public bool MeetsRequirements(float capital, int skillPoints, float experience, int harvests, int plants, float yield, int currentTier)
        {
            return capital >= RequiredCapital &&
                   skillPoints >= RequiredSkillPoints &&
                   experience >= RequiredExperience &&
                   harvests >= RequiredHarvests &&
                   plants >= RequiredPlants &&
                   yield >= RequiredYield &&
                   currentTier >= MinimumTierLevel;
        }

        /// <summary>
        /// Converts requirements to a list of string descriptions
        /// </summary>
        public System.Collections.Generic.List<string> ToStringList()
        {
            var requirements = new System.Collections.Generic.List<string>();
            
            if (RequiredCapital > 0)
                requirements.Add($"Capital: ${RequiredCapital:F0}");
            
            if (RequiredSkillPoints > 0)
                requirements.Add($"Skill Points: {RequiredSkillPoints}");
            
            if (RequiredExperience > 0)
                requirements.Add($"Experience: {RequiredExperience:F0}");
            
            if (RequiredHarvests > 0)
                requirements.Add($"Harvests: {RequiredHarvests}");
            
            if (RequiredPlants > 0)
                requirements.Add($"Plants: {RequiredPlants}");
            
            if (RequiredYield > 0)
                requirements.Add($"Yield: {RequiredYield:F0}kg");
            
            if (MinimumTierLevel > 0)
                requirements.Add($"Minimum Tier: {MinimumTierLevel}");
            
            if (RequiresSpecialLicense)
                requirements.Add("Special License Required");
            
            return requirements;
        }
    }
}