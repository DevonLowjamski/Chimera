using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Template for generating random contracts for Phase 8 MVP
    /// Defines ranges and parameters for procedural contract creation
    /// </summary>
    [CreateAssetMenu(fileName = "New Contract Generation Template", menuName = "Project Chimera/Economy/Contract Generation Template")]
    public class ContractGenerationTemplateSO : ChimeraDataSO
    {
        [Header("Contractor Database")]
        [SerializeField] private List<ContractorProfile> _contractorProfiles = new List<ContractorProfile>();
        
        [Header("Quantity Ranges (kg)")]
        [SerializeField] private Vector2 _smallOrderRange = new Vector2(5f, 15f);
        [SerializeField] private Vector2 _mediumOrderRange = new Vector2(16f, 40f);
        [SerializeField] private Vector2 _largeOrderRange = new Vector2(41f, 100f);
        
        [Header("Quality Requirements")]
        [SerializeField] private Vector2 _qualityRange = new Vector2(0.6f, 0.95f);
        [SerializeField] private float _highQualityThreshold = 0.85f;
        
        [Header("Time Windows (days)")]
        [SerializeField] private Vector2 _timeWindowRange = new Vector2(14, 60);
        [SerializeField] private int _rushOrderMaxDays = 21;
        [SerializeField] private int _standardOrderMaxDays = 45;
        
        [Header("Contract Values")]
        [SerializeField] private float _baseValuePerKg = 400f;
        [SerializeField] private Vector2 _valueMultiplierRange = new Vector2(0.8f, 1.4f);
        [SerializeField] private float _highQualityValueMultiplier = 1.3f;
        [SerializeField] private float _rushOrderValueMultiplier = 1.5f;
        
        [Header("Strain Preferences")]
        [SerializeField] private List<StrainPreference> _strainPreferences = new List<StrainPreference>();
        
        [Header("Generation Weights")]
        [SerializeField] private ContractTypeWeights _contractWeights;
        
        // Public Properties
        public List<ContractorProfile> ContractorProfiles => _contractorProfiles;
        public Vector2 SmallOrderRange => _smallOrderRange;
        public Vector2 MediumOrderRange => _mediumOrderRange;
        public Vector2 LargeOrderRange => _largeOrderRange;
        public Vector2 QualityRange => _qualityRange;
        public float HighQualityThreshold => _highQualityThreshold;
        public Vector2 TimeWindowRange => _timeWindowRange;
        public int RushOrderMaxDays => _rushOrderMaxDays;
        public int StandardOrderMaxDays => _standardOrderMaxDays;
        public float BaseValuePerKg => _baseValuePerKg;
        public Vector2 ValueMultiplierRange => _valueMultiplierRange;
        public float HighQualityValueMultiplier => _highQualityValueMultiplier;
        public float RushOrderValueMultiplier => _rushOrderValueMultiplier;
        public List<StrainPreference> StrainPreferences => _strainPreferences;
        public ContractTypeWeights ContractWeights => _contractWeights;
        
        /// <summary>
        /// Generate a random contractor profile
        /// </summary>
        public ContractorProfile GetRandomContractor()
        {
            if (_contractorProfiles.Count == 0)
                return GetDefaultContractor();
                
            int randomIndex = Random.Range(0, _contractorProfiles.Count);
            return _contractorProfiles[randomIndex];
        }
        
        /// <summary>
        /// Generate random contract parameters
        /// </summary>
        public ContractGenerationParameters GenerateRandomContract()
        {
            var parameters = new ContractGenerationParameters();
            
            // Select contractor
            parameters.Contractor = GetRandomContractor();
            
            // Determine contract size based on weights
            var orderSize = DetermineOrderSize();
            parameters.Quantity = GetQuantityForOrderSize(orderSize);
            
            // Generate quality requirement
            parameters.MinimumQuality = Random.Range(_qualityRange.x, _qualityRange.y);
            
            // Generate time window
            bool isRushOrder = Random.value < _contractWeights.RushOrderChance;
            if (isRushOrder)
            {
                parameters.TimeWindowDays = Random.Range(7, _rushOrderMaxDays + 1);
                parameters.IsRushOrder = true;
            }
            else
            {
                parameters.TimeWindowDays = Random.Range(_rushOrderMaxDays + 1, _standardOrderMaxDays + 1);
                parameters.IsRushOrder = false;
            }
            
            // Select strain preference
            parameters.StrainType = GetRandomStrainType();
            parameters.SpecificStrain = GetSpecificStrainForType(parameters.StrainType);
            
            // Calculate contract value
            parameters.ContractValue = CalculateContractValue(parameters);
            
            return parameters;
        }
        
        /// <summary>
        /// Calculate contract value based on parameters
        /// </summary>
        public float CalculateContractValue(ContractGenerationParameters parameters)
        {
            float baseValue = parameters.Quantity * _baseValuePerKg;
            
            // Apply random value multiplier
            float valueMultiplier = Random.Range(_valueMultiplierRange.x, _valueMultiplierRange.y);
            baseValue *= valueMultiplier;
            
            // High quality bonus
            if (parameters.MinimumQuality >= _highQualityThreshold)
            {
                baseValue *= _highQualityValueMultiplier;
            }
            
            // Rush order bonus
            if (parameters.IsRushOrder)
            {
                baseValue *= _rushOrderValueMultiplier;
            }
            
            // Contractor-specific multiplier
            baseValue *= parameters.Contractor.ValueMultiplier;
            
            return Mathf.Round(baseValue);
        }
        
        private ContractorProfile GetDefaultContractor()
        {
            return new ContractorProfile
            {
                ContractorName = "Generic Distributor",
                ReputationLevel = ReputationLevel.Standard,
                PreferredStrainTypes = new List<StrainType> { StrainType.Hybrid },
                ValueMultiplier = 1f,
                ReliabilityScore = 0.8f
            };
        }
        
        private OrderSize DetermineOrderSize()
        {
            float random = Random.value;
            
            if (random < _contractWeights.SmallOrderChance)
                return OrderSize.Small;
            else if (random < _contractWeights.SmallOrderChance + _contractWeights.MediumOrderChance)
                return OrderSize.Medium;
            else
                return OrderSize.Large;
        }
        
        private float GetQuantityForOrderSize(OrderSize orderSize)
        {
            return orderSize switch
            {
                OrderSize.Small => Random.Range(_smallOrderRange.x, _smallOrderRange.y),
                OrderSize.Medium => Random.Range(_mediumOrderRange.x, _mediumOrderRange.y),
                OrderSize.Large => Random.Range(_largeOrderRange.x, _largeOrderRange.y),
                _ => Random.Range(_smallOrderRange.x, _smallOrderRange.y)
            };
        }
        
        private StrainType GetRandomStrainType()
        {
            float random = Random.value;
            
            if (random < _contractWeights.SativaChance)
                return StrainType.Sativa;
            else if (random < _contractWeights.SativaChance + _contractWeights.IndicaChance)
                return StrainType.Indica;
            else
                return StrainType.Hybrid;
        }
        
        private string GetSpecificStrainForType(StrainType strainType)
        {
            var matchingPreferences = _strainPreferences.FindAll(sp => sp.StrainType == strainType);
            
            if (matchingPreferences.Count == 0)
                return string.Empty;
                
            var preference = matchingPreferences[Random.Range(0, matchingPreferences.Count)];
            
            if (preference.SpecificStrains.Count == 0)
                return string.Empty;
                
            return preference.SpecificStrains[Random.Range(0, preference.SpecificStrains.Count)];
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (_contractorProfiles.Count == 0)
            {
                Debug.LogWarning($"ContractGenerationTemplate {name}: No contractor profiles defined", this);
            }
            
            if (_baseValuePerKg <= 0f)
            {
                Debug.LogError($"ContractGenerationTemplate {name}: Base value per kg must be positive", this);
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Contractor profile for contract generation
    /// </summary>
    [System.Serializable]
    public class ContractorProfile
    {
        [SerializeField] public string ContractorName;
        [SerializeField] public ReputationLevel ReputationLevel = ReputationLevel.Standard;
        [SerializeField] public List<StrainType> PreferredStrainTypes = new List<StrainType>();
        [SerializeField] public float ValueMultiplier = 1f;
        [SerializeField] public float ReliabilityScore = 0.8f; // 0-1 scale
        [SerializeField, TextArea(2, 3)] public string Description;
    }
    
    /// <summary>
    /// Strain preference configuration
    /// </summary>
    [System.Serializable]
    public class StrainPreference
    {
        [SerializeField] public StrainType StrainType;
        [SerializeField] public List<string> SpecificStrains = new List<string>();
        [SerializeField] public float QualityMultiplier = 1f;
    }
    
    /// <summary>
    /// Contract generation weights for balancing
    /// </summary>
    [System.Serializable]
    public class ContractTypeWeights
    {
        [Header("Order Size Distribution")]
        [Range(0f, 1f)] public float SmallOrderChance = 0.5f;
        [Range(0f, 1f)] public float MediumOrderChance = 0.35f;
        [Range(0f, 1f)] public float LargeOrderChance = 0.15f;
        
        [Header("Strain Type Distribution")]
        [Range(0f, 1f)] public float SativaChance = 0.3f;
        [Range(0f, 1f)] public float IndicaChance = 0.3f;
        [Range(0f, 1f)] public float HybridChance = 0.4f;
        
        [Header("Special Contract Types")]
        [Range(0f, 1f)] public float RushOrderChance = 0.2f;
        [Range(0f, 1f)] public float HighQualityChance = 0.3f;
    }
    
    /// <summary>
    /// Generated contract parameters
    /// </summary>
    [System.Serializable]
    public class ContractGenerationParameters
    {
        public ContractorProfile Contractor;
        public StrainType StrainType;
        public string SpecificStrain;
        public float Quantity;
        public float MinimumQuality;
        public int TimeWindowDays;
        public float ContractValue;
        public bool IsRushOrder;
    }
    
    /// <summary>
    /// Order size categories
    /// </summary>
    public enum OrderSize
    {
        Small,
        Medium,
        Large
    }
    
    /// <summary>
    /// Contractor reputation levels
    /// </summary>
    public enum ReputationLevel
    {
        Poor,
        Standard,
        Good,
        Excellent,
        Premium
    }
}