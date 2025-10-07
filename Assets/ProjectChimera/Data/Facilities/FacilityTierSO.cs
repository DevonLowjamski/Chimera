
using UnityEngine;

namespace ProjectChimera.Data.Facilities
{
    /// <summary>
    /// ScriptableObject defining a facility tier in the upgrade ladder
    /// </summary>
    [CreateAssetMenu(fileName = "New Facility Tier", menuName = "Project Chimera/Facilities/Facility Tier")]
    public class FacilityTierSO : ScriptableObject
    {
        [Header("Tier Configuration")]
        [SerializeField] private string _tierName;
        [SerializeField] private int _tierLevel = 1;
        [SerializeField] private string _sceneName;
        [SerializeField] private Sprite _facilityIcon;
        
        [Header("Requirements")]
        [SerializeField] private float _requiredCapital = 10000f;
        [SerializeField] private int _requiredPlants = 5;
        [SerializeField] private float _requiredExperience = 100f;
        [SerializeField] private int _requiredHarvests = 3;
        
        [Header("Benefits")]
        [SerializeField] private int _maxPlantCapacity = 20;
        [SerializeField] private float _efficiencyMultiplier = 1.0f;
        [SerializeField] private float _unlockCost = 25000f;
        
        // Properties
        public string TierName => _tierName;
        public int TierLevel => _tierLevel;
        public string SceneName => _sceneName;
        public Sprite FacilityIcon => _facilityIcon;
        public float RequiredCapital => _requiredCapital;
        public int RequiredPlants => _requiredPlants;
        public float RequiredExperience => _requiredExperience;
        public int RequiredHarvests => _requiredHarvests;
        public int MaxPlantCapacity => _maxPlantCapacity;
        public float EfficiencyMultiplier => _efficiencyMultiplier;
        public float UnlockCost => _unlockCost;
        
        public bool MeetsUpgradeRequirements(FacilityProgressionData progressionData)
        {
            return progressionData.Capital >= _requiredCapital &&
                   progressionData.TotalPlants >= _requiredPlants &&
                   progressionData.Experience >= _requiredExperience &&
                   progressionData.TotalHarvests >= _requiredHarvests;
        }

        public FacilityUpgradeRequirements GetUpgradeRequirements(FacilityProgressionData progressionData)
        {
            return new FacilityUpgradeRequirements
            {
                RequiredCapital = _requiredCapital,
                RequiredPlants = _requiredPlants,
                RequiredExperience = _requiredExperience,
                RequiredHarvests = _requiredHarvests,
                RequiredSkillPoints = 0, // Default value
                RequiredYield = 0f, // Default value
                MinimumTierLevel = _tierLevel,
                RequiresSpecialLicense = false
            };
        }
    }
    
    /// <summary>
    /// Data structure for tracking facility progression
    /// </summary>
    [System.Serializable]
    public struct FacilityProgressionData
    {
        public float Capital;
        public int TotalPlants;
        public float Experience;
        public int TotalHarvests;
        public int UnlockedTiers;
        public int TotalUpgrades;
    }
    
    /// <summary>
    /// Data structure for owned facility instances
    /// </summary>
    [System.Serializable]
    public struct OwnedFacility
    {
        public string FacilityId;
        public FacilityTierSO Tier;
        public string FacilityName;
        public string SceneName;
        public System.DateTime PurchaseDate;
        public bool IsActive;
        
        // Financial tracking
        public float TotalInvestment;
        public float CurrentValue;
        
        // Facility management
        public float MaintenanceLevel;
        public System.DateTime LastMaintenance;
        
        // Performance metrics
        public int TotalPlantsGrown;
        public float TotalRevenue;
        public float AverageYield;
        
        // Operational status
        public bool IsOperational;
        public string Notes;
    }
}