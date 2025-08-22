using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// ScriptableObject data structure for schematic market system in Phase 8 MVP
    /// Manages available schematics, unlock requirements, and purchase mechanics
    /// </summary>
    [CreateAssetMenu(fileName = "New Schematic Market", menuName = "Project Chimera/Economy/Schematic Market")]
    public class SchematicMarketSO : ChimeraDataSO
    {
        [Header("Market Configuration")]
        [SerializeField] private string _marketName = "Schematic Market";
        [SerializeField] private string _marketDescription = "Purchase facility schematics and equipment blueprints";
        [SerializeField] private bool _isMarketActive = true;
        [SerializeField] private float _marketRefreshIntervalHours = 24f;
        
        [Header("Schematic Categories")]
        [SerializeField] private List<SchematicCategory> _schematicCategories = new List<SchematicCategory>();
        
        [Header("Market Dynamics")]
        [SerializeField] private bool _enableDynamicPricing = true;
        [SerializeField] private float _basePriceMultiplier = 1f;
        [SerializeField] private Vector2 _priceVariationRange = new Vector2(0.8f, 1.3f);
        [SerializeField] private bool _enableLimitedStock = true;
        [SerializeField] private int _maxSchematicsPerCategory = 10;
        
        [Header("Progression Requirements")]
        [SerializeField] private bool _enableProgressionLocks = true;
        [SerializeField] private int _baseUnlockLevel = 1;
        [SerializeField] private float _skillPointInflation = 1.2f; // Price increase per tier
        
        // Public Properties
        public string MarketName => _marketName;
        public string MarketDescription => _marketDescription;
        public bool IsMarketActive => _isMarketActive;
        public float MarketRefreshIntervalHours => _marketRefreshIntervalHours;
        public List<SchematicCategory> SchematicCategories => new List<SchematicCategory>(_schematicCategories);
        public bool EnableDynamicPricing => _enableDynamicPricing;
        public float BasePriceMultiplier => _basePriceMultiplier;
        public Vector2 PriceVariationRange => _priceVariationRange;
        public bool EnableLimitedStock => _enableLimitedStock;
        public int MaxSchematicsPerCategory => _maxSchematicsPerCategory;
        public bool EnableProgressionLocks => _enableProgressionLocks;
        public int BaseUnlockLevel => _baseUnlockLevel;
        public float SkillPointInflation => _skillPointInflation;
        
        /// <summary>
        /// Get all available schematics across all categories
        /// </summary>
        public List<ConstructionSchematicSO> GetAllSchematics()
        {
            var allSchematics = new List<ConstructionSchematicSO>();
            
            foreach (var category in _schematicCategories)
            {
                allSchematics.AddRange(category.AvailableSchematics);
            }
            
            return allSchematics;
        }
        
        /// <summary>
        /// Get schematics by category type
        /// </summary>
        public List<ConstructionSchematicSO> GetSchematicsByCategory(SchematicCategoryType categoryType)
        {
            var category = _schematicCategories.Find(c => c.CategoryType == categoryType);
            return category?.AvailableSchematics ?? new List<ConstructionSchematicSO>();
        }
        
        /// <summary>
        /// Get category configuration by type
        /// </summary>
        public SchematicCategory GetCategory(SchematicCategoryType categoryType)
        {
            return _schematicCategories.Find(c => c.CategoryType == categoryType);
        }
        
        /// <summary>
        /// Calculate dynamic price for a schematic
        /// </summary>
        public float CalculateSchematicPrice(ConstructionSchematicSO schematic)
        {
            if (schematic == null) return 0f;
            
            float basePrice = schematic.SkillPointCost;
            
            // Apply market multiplier
            float marketPrice = basePrice * _basePriceMultiplier;
            
            // Apply dynamic pricing if enabled
            if (_enableDynamicPricing)
            {
                float variation = UnityEngine.Random.Range(_priceVariationRange.x, _priceVariationRange.y);
                marketPrice *= variation;
            }
            
            // Apply progression inflation
            if (_enableProgressionLocks && schematic.UnlockLevel > _baseUnlockLevel)
            {
                float inflationMultiplier = Mathf.Pow(_skillPointInflation, schematic.UnlockLevel - _baseUnlockLevel);
                marketPrice *= inflationMultiplier;
            }
            
            return Mathf.Round(marketPrice);
        }
        
        /// <summary>
        /// Check if player can afford a schematic
        /// </summary>
        public bool CanAffordSchematic(ConstructionSchematicSO schematic, float playerSkillPoints)
        {
            float price = CalculateSchematicPrice(schematic);
            return playerSkillPoints >= price;
        }
        
        /// <summary>
        /// Get recommended schematics for player level
        /// </summary>
        public List<ConstructionSchematicSO> GetRecommendedSchematics(int playerLevel, int maxRecommendations = 5)
        {
            var recommended = new List<ConstructionSchematicSO>();
            var allSchematics = GetAllSchematics();
            
            // Filter by level and availability
            var suitable = allSchematics.FindAll(s => 
                s.UnlockLevel <= playerLevel + 2 && // Allow slightly higher level
                s.UnlockLevel >= playerLevel - 1 && // Not too low level
                !s.IsUnlocked);
            
            // Sort by relevance (level proximity, category priority)
            suitable.Sort((a, b) => {
                int levelDiffA = Mathf.Abs(a.UnlockLevel - playerLevel);
                int levelDiffB = Mathf.Abs(b.UnlockLevel - playerLevel);
                return levelDiffA.CompareTo(levelDiffB);
            });
            
            // Take top recommendations
            int count = Mathf.Min(maxRecommendations, suitable.Count);
            for (int i = 0; i < count; i++)
            {
                recommended.Add(suitable[i]);
            }
            
            return recommended;
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_marketName))
            {
                Debug.LogError($"SchematicMarket {name}: Market name cannot be empty", this);
                isValid = false;
            }
            
            if (_schematicCategories.Count == 0)
            {
                Debug.LogWarning($"SchematicMarket {name}: No schematic categories defined", this);
            }
            
            if (_basePriceMultiplier <= 0f)
            {
                Debug.LogError($"SchematicMarket {name}: Base price multiplier must be positive", this);
                isValid = false;
            }
            
            if (_skillPointInflation <= 1f)
            {
                Debug.LogWarning($"SchematicMarket {name}: Skill point inflation should be > 1.0 for progression", this);
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Schematic category configuration
    /// </summary>
    [System.Serializable]
    public class SchematicCategory
    {
        [Header("Category Information")]
        [SerializeField] public SchematicCategoryType CategoryType;
        [SerializeField] public string CategoryName;
        [SerializeField] public string CategoryDescription;
        [SerializeField] public Sprite CategoryIcon;
        
        [Header("Category Settings")]
        [SerializeField] public bool IsActive = true;
        [SerializeField] public int DisplayOrder = 0;
        [SerializeField] public Color CategoryColor = Color.white;
        
        [Header("Available Schematics")]
        [SerializeField] public List<ConstructionSchematicSO> AvailableSchematics = new List<ConstructionSchematicSO>();
        
        [Header("Category Modifiers")]
        [SerializeField] public float CategoryPriceMultiplier = 1f;
        [SerializeField] public int CategoryUnlockLevel = 1;
        [SerializeField] public bool RequiresSpecialUnlock = false;
        [SerializeField] public string SpecialUnlockRequirement;
    }
    
    /// <summary>
    /// Construction schematic data
    /// </summary>
    [CreateAssetMenu(fileName = "New Construction Schematic", menuName = "Project Chimera/Economy/Construction Schematic")]
    public class ConstructionSchematicSO : ChimeraDataSO
    {
        [Header("Schematic Identity")]
        [SerializeField] private string _schematicId;
        [SerializeField] private string _schematicName;
        [SerializeField] private string _schematicDescription;
        [SerializeField] private Sprite _schematicIcon;
        [SerializeField] private SchematicCategoryType _categoryType;
        
        [Header("Construction Data")]
        [SerializeField] private SchematicFacilityType _facilityType;
        [SerializeField] private GameObject _constructionPrefab;
        [SerializeField] private Vector2Int _gridSize = Vector2Int.one;
        [SerializeField] private float _constructionCost = 1000f;
        [SerializeField] private int _constructionTime = 60; // seconds
        
        [Header("Unlock Requirements")]
        [SerializeField] private int _unlockLevel = 1;
        [SerializeField] private float _skillPointCost = 10f;
        [SerializeField] private bool _isUnlocked = false;
        [SerializeField] private List<string> _prerequisiteSchematicIds = new List<string>();
        
        [Header("Schematic Properties")]
        [SerializeField] private SchematicRarity _rarity = SchematicRarity.Common;
        [SerializeField] private List<SchematicTag> _tags = new List<SchematicTag>();
        [SerializeField] private bool _isLimitedEdition = false;
        [SerializeField] private int _stockRemaining = -1; // -1 = unlimited
        
        [Header("Performance Stats")]
        [SerializeField] private float _efficiencyRating = 1f;
        [SerializeField] private float _powerConsumption = 100f;
        [SerializeField] private float _maintenanceRequirement = 0.1f;
        [SerializeField] private int _capacityRating = 10;
        
        // Public Properties
        public string SchematicId => _schematicId;
        public string SchematicName => _schematicName;
        public string SchematicDescription => _schematicDescription;
        public Sprite SchematicIcon => _schematicIcon;
        public SchematicCategoryType CategoryType => _categoryType;
        public SchematicFacilityType FacilityType => _facilityType;
        public GameObject ConstructionPrefab => _constructionPrefab;
        public Vector2Int GridSize => _gridSize;
        public float ConstructionCost => _constructionCost;
        public int ConstructionTime => _constructionTime;
        public int UnlockLevel => _unlockLevel;
        public float SkillPointCost => _skillPointCost;
        public bool IsUnlocked { get => _isUnlocked; set => _isUnlocked = value; }
        public List<string> PrerequisiteSchematicIds => new List<string>(_prerequisiteSchematicIds);
        public SchematicRarity Rarity => _rarity;
        public List<SchematicTag> Tags => new List<SchematicTag>(_tags);
        public bool IsLimitedEdition => _isLimitedEdition;
        public int StockRemaining { get => _stockRemaining; set => _stockRemaining = value; }
        public float EfficiencyRating => _efficiencyRating;
        public float PowerConsumption => _powerConsumption;
        public float MaintenanceRequirement => _maintenanceRequirement;
        public int CapacityRating => _capacityRating;
        
        /// <summary>
        /// Check if schematic prerequisites are met
        /// </summary>
        public bool ArePrerequisitesMet(List<string> unlockedSchematicIds)
        {
            if (_prerequisiteSchematicIds.Count == 0) return true;
            
            foreach (string prerequisiteId in _prerequisiteSchematicIds)
            {
                if (!unlockedSchematicIds.Contains(prerequisiteId))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Calculate total unlock cost including prerequisites
        /// </summary>
        public float CalculateTotalUnlockCost(SchematicMarketSO market)
        {
            float totalCost = market.CalculateSchematicPrice(this);
            
            // Add prerequisite costs if not already unlocked
            // This would require access to the market data to calculate
            // For now, return just this schematic's cost
            
            return totalCost;
        }
        
        /// <summary>
        /// Get schematic difficulty score for progression
        /// </summary>
        public float GetDifficultyScore()
        {
            float difficulty = 0f;
            
            // Level-based difficulty
            difficulty += _unlockLevel * 0.2f;
            
            // Rarity-based difficulty
            difficulty += (int)_rarity * 0.1f;
            
            // Grid size complexity
            difficulty += (_gridSize.x * _gridSize.y) * 0.05f;
            
            // Cost-based difficulty
            difficulty += (_skillPointCost / 100f) * 0.3f;
            
            return Mathf.Clamp01(difficulty);
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_schematicId))
            {
                Debug.LogError($"ConstructionSchematic {name}: Schematic ID cannot be empty", this);
                isValid = false;
            }
            
            if (string.IsNullOrEmpty(_schematicName))
            {
                Debug.LogError($"ConstructionSchematic {name}: Schematic name cannot be empty", this);
                isValid = false;
            }
            
            if (_skillPointCost < 0f)
            {
                Debug.LogError($"ConstructionSchematic {name}: Skill point cost cannot be negative", this);
                isValid = false;
            }
            
            if (_unlockLevel < 1)
            {
                Debug.LogError($"ConstructionSchematic {name}: Unlock level must be at least 1", this);
                isValid = false;
            }
            
            if (_gridSize.x <= 0 || _gridSize.y <= 0)
            {
                Debug.LogError($"ConstructionSchematic {name}: Grid size must be positive", this);
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Schematic category types
    /// </summary>
    public enum SchematicCategoryType
    {
        GrowRooms,
        ProcessingFacilities,
        UtilityRooms,
        SecuritySystems,
        AutomationEquipment,
        LaboratoryEquipment,
        StorageSolutions,
        EnvironmentalControls,
        SpecializedEquipment,
        DecorationAndComfort
    }
    
    /// <summary>
    /// Schematic rarity levels
    /// </summary>
    public enum SchematicRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    /// <summary>
    /// Schematic tags for filtering and categorization
    /// </summary>
    public enum SchematicTag
    {
        Beginner,
        Advanced,
        Professional,
        Experimental,
        EnergyEfficient,
        HighCapacity,
        Automated,
        Specialized,
        Modular,
        Compact,
        Industrial,
        Residential,
        Research,
        Production,
        Safety
    }
    
    /// <summary>
    /// Facility types for schematic categorization (local copy to avoid circular dependencies)
    /// </summary>
    public enum SchematicFacilityType
    {
        Vegetative,
        Flowering,
        Nursery,
        Mother,
        Drying,
        Curing,
        Processing,
        Storage,
        Laboratory,
        Office,
        Utility,
        Security,
        Automation,
        Environmental
    }
}