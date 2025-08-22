using UnityEngine;
using ProjectChimera.Data.Shared;
using ProjectChimera.Shared;
using System.Collections.Generic;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Configuration for breeding system operations
    /// Defines timing, success rates, and breeding constraints
    /// </summary>
    [CreateAssetMenu(fileName = "New Breeding Config", menuName = "Project Chimera/Genetics/Breeding Config")]
    public class BreedingConfig : ChimeraConfigSO
    {
        [Header("Breeding Timing")]
        [SerializeField, Range(1f, 30f)] private float _baseBreedingTime = 7f;
        [SerializeField, Range(1f, 14f)] private float _pollinationWindow = 3f;
        [SerializeField, Range(14f, 120f)] private float _seedMaturationTime = 21f;
        
        [Header("Success Rates")]
        [SerializeField, Range(0f, 1f)] private float _baseSuccessRate = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _inbreedingPenalty = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _hybridVigor = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _environmentalBonus = 0.15f;
        
        [Header("Genetic Constraints")]
        [SerializeField] private bool _requireCompatibilityCheck = true;
        [SerializeField] private bool _allowSelfPollination = false;
        [SerializeField] private bool _allowInbreeding = true;
        [SerializeField, Range(1, 10)] private int _maxBreedingAttempts = 3;
        [SerializeField, Range(0f, 1f)] private float _minimumCompatibility = 0.3f;
        
        [Header("Trait Inheritance")]
        [SerializeField, Range(0f, 1f)] private float _traitStability = 0.8f;
        [SerializeField, Range(0f, 0.1f)] private float _mutationRate = 0.02f;
        [SerializeField, Range(0f, 1f)] private float _recombinationRate = 0.5f;
        [SerializeField] private bool _enableEpistasis = true;
        [SerializeField] private bool _enableLinkage = false;
        
        [Header("Advanced Genetics")]
        [SerializeField] private bool _useQuantumGenetics = false;
        [SerializeField] private bool _enablePolyploidy = false;
        [SerializeField, Range(2, 8)] private int _maxPloidyLevel = 2;
        [SerializeField] private bool _enableChimeras = false;
        
        [Header("Breeding Bonuses")]
        [SerializeField] private List<BreedingBonus> _breedingBonuses = new List<BreedingBonus>();
        [SerializeField] private List<CompatibilityRule> _compatibilityRules = new List<CompatibilityRule>();
        
        [Header("Resource Requirements")]
        [SerializeField] private ResourceRequirement[] _breedingCosts = new ResourceRequirement[0];
        [SerializeField] private float _pedigreeCostMultiplier = 1.5f;
        
        // Public Properties
        public float BaseBreedingTime => _baseBreedingTime;
        public float PollinationWindow => _pollinationWindow;
        public float SeedMaturationTime => _seedMaturationTime;
        public float BaseSuccessRate => _baseSuccessRate;
        public float InbreedingPenalty => _inbreedingPenalty;
        public float HybridVigor => _hybridVigor;
        public float EnvironmentalBonus => _environmentalBonus;
        public bool RequireCompatibilityCheck => _requireCompatibilityCheck;
        public bool AllowSelfPollination => _allowSelfPollination;
        public bool AllowInbreeding => _allowInbreeding;
        public int MaxBreedingAttempts => _maxBreedingAttempts;
        public float MinimumCompatibility => _minimumCompatibility;
        public float TraitStability => _traitStability;
        public float MutationRate => _mutationRate;
        public float RecombinationRate => _recombinationRate;
        public bool EnableEpistasis => _enableEpistasis;
        public bool EnableLinkage => _enableLinkage;
        public bool UseQuantumGenetics => _useQuantumGenetics;
        public bool EnablePolyploidy => _enablePolyploidy;
        public int MaxPloidyLevel => _maxPloidyLevel;
        public bool EnableChimeras => _enableChimeras;
        public List<BreedingBonus> BreedingBonuses => _breedingBonuses;
        public List<CompatibilityRule> CompatibilityRules => _compatibilityRules;
        public ResourceRequirement[] BreedingCosts => _breedingCosts;
        public float PedigreeCostMultiplier => _pedigreeCostMultiplier;
        
        /// <summary>
        /// Calculate breeding success rate for specific parents
        /// </summary>
        public float CalculateSuccessRate(string strainId1, string strainId2, bool isInbred = false, float environmentalQuality = 1f)
        {
            float successRate = _baseSuccessRate;
            
            // Apply inbreeding penalty
            if (isInbred)
                successRate -= _inbreedingPenalty;
            
            // Apply hybrid vigor for crosses
            if (strainId1 != strainId2)
                successRate += _hybridVigor;
            
            // Apply environmental bonus
            successRate += _environmentalBonus * (environmentalQuality - 1f);
            
            // Apply breeding bonuses
            foreach (var bonus in _breedingBonuses)
            {
                if (bonus.AppliesToStrain(strainId1) || bonus.AppliesToStrain(strainId2))
                {
                    successRate += bonus.BonusAmount;
                }
            }
            
            return Mathf.Clamp01(successRate);
        }
        
        /// <summary>
        /// Check compatibility between two strains
        /// </summary>
        public float CheckCompatibility(string strainId1, string strainId2)
        {
            if (!_requireCompatibilityCheck)
                return 1f;
            
            // Check compatibility rules
            foreach (var rule in _compatibilityRules)
            {
                if (rule.Matches(strainId1, strainId2))
                {
                    return rule.CompatibilityScore;
                }
            }
            
            // Default compatibility based on strain similarity
            if (strainId1 == strainId2)
                return _allowSelfPollination ? 1f : 0f;
            
            // Base compatibility for different strains
            return 0.7f;
        }
        
        /// <summary>
        /// Get resource cost for breeding operation
        /// </summary>
        public ResourceRequirement[] GetBreedingCost(bool isPedigreed = false)
        {
            if (_breedingCosts == null || _breedingCosts.Length == 0)
                return new ResourceRequirement[0];
            
            var costs = new ResourceRequirement[_breedingCosts.Length];
            for (int i = 0; i < _breedingCosts.Length; i++)
            {
                costs[i] = _breedingCosts[i];
                if (isPedigreed)
                {
                    costs[i].Amount *= _pedigreeCostMultiplier;
                }
            }
            
            return costs;
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (_baseBreedingTime <= 0f)
            {
                Debug.LogError($"Breeding Config {name}: Base breeding time must be positive", this);
                isValid = false;
            }
            
            if (_baseSuccessRate <= 0f || _baseSuccessRate > 1f)
            {
                Debug.LogError($"Breeding Config {name}: Base success rate must be between 0 and 1", this);
                isValid = false;
            }
            
            if (_mutationRate < 0f || _mutationRate > 0.5f)
            {
                Debug.LogWarning($"Breeding Config {name}: Mutation rate seems unusually high", this);
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Breeding bonus configuration
    /// </summary>
    [System.Serializable]
    public class BreedingBonus
    {
        [SerializeField] private string _bonusName;
        [SerializeField] private List<string> _applicableStrains = new List<string>();
        [SerializeField] private bool _appliesToAllStrains = false;
        [SerializeField, Range(-0.5f, 0.5f)] private float _bonusAmount = 0.1f;
        [SerializeField, TextArea(2, 3)] private string _description;
        
        public string BonusName => _bonusName;
        public float BonusAmount => _bonusAmount;
        public string Description => _description;
        
        public bool AppliesToStrain(string strainId)
        {
            return _appliesToAllStrains || _applicableStrains.Contains(strainId);
        }
    }
    
    /// <summary>
    /// Compatibility rule between specific strains
    /// </summary>
    [System.Serializable]
    public class CompatibilityRule
    {
        [SerializeField] private string _strain1;
        [SerializeField] private string _strain2;
        [SerializeField] private bool _bidirectional = true;
        [SerializeField, Range(0f, 1f)] private float _compatibilityScore = 0.5f;
        [SerializeField, TextArea(2, 3)] private string _reason;
        
        public float CompatibilityScore => _compatibilityScore;
        public string Reason => _reason;
        
        public bool Matches(string strainId1, string strainId2)
        {
            if (_strain1 == strainId1 && _strain2 == strainId2)
                return true;
            
            if (_bidirectional && _strain1 == strainId2 && _strain2 == strainId1)
                return true;
            
            return false;
        }
    }
    
    /// <summary>
    /// Resource requirement for breeding operations
    /// </summary>
    [System.Serializable]
    public struct ResourceRequirement
    {
        public string ResourceType;
        public float Amount;
        public string Description;
    }
}