using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;


namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Defines a specific phenotypic expression of a cannabis strain.
    /// Represents individual plant characteristics that may vary within the same strain.
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Phenotype", menuName = "Project Chimera/Genetics/Plant Phenotype", order = 3)]
    public class PlantPhenotypeSO : ChimeraDataSO
    {
        [Header("Phenotype Identity")]
        [SerializeField] private GeneticPlantStrainSO _parentStrain;
        [SerializeField] private string _phenotypeId;
        [SerializeField] private string _phenotypeName;
        [SerializeField, TextArea(2, 4)] private string _phenotypeDescription;
        [SerializeField] private bool _isRarePhenotype = false;
        [SerializeField] private bool _isStablePhenotype = true;

        [Header("Physical Expression Modifiers")]
        [SerializeField, Range(0.7f, 1.3f)] private float _heightVariation = 1f;
        [SerializeField, Range(0.7f, 1.3f)] private float _widthVariation = 1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _nodeSpacingVariation = 1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _branchingVariation = 1f;

        [Header("Cannabinoid Expression")]
        [SerializeField, Range(0.8f, 1.2f)] private float _thcVariation = 1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _cbdVariation = 1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _terpeneVariation = 1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _resinProductionVariation = 1f;

        [Header("Flowering Characteristics")]
        [SerializeField, Range(0.9f, 1.1f)] private float _floweringTimeVariation = 1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _yieldVariation = 1f;
        [SerializeField, Range(0.9f, 1.1f)] private float _budDensityVariation = 1f;

        [Header("Visual Characteristics")]
        [SerializeField] private Color _leafColorVariation = Color.green;
        [SerializeField] private Color _budColorVariation = Color.green;
        [SerializeField, Range(0.8f, 1.2f)] private float _leafSizeVariation = 1f;
        [SerializeField] private LeafPattern _leafPattern = LeafPattern.Standard;

        [Header("Environmental Response")]
        [SerializeField, Range(0.9f, 1.1f)] private float _environmentalSensitivityVariation = 1f;
        [SerializeField, Range(0.9f, 1.1f)] private float _stressToleranceVariation = 1f;
        [SerializeField, Range(0.9f, 1.1f)] private float _diseaseResistanceVariation = 1f;

        [Header("Unique Traits")]
        [SerializeField] private List<PhenotypeTrait> _uniqueTraits = new List<PhenotypeTrait>();
        [SerializeField] private bool _hasUniqueTerpeneProfile = false;
        [SerializeField] private bool _hasUniqueColorExpression = false;
        [SerializeField] private bool _hasUniqueMorphology = false;

        // Public Properties
        public GeneticPlantStrainSO ParentStrain => _parentStrain;
        public string PhenotypeId { get => _phenotypeId; set => _phenotypeId = value; }
        public string PhenotypeName { get => _phenotypeName; set => _phenotypeName = value; }
        public string PhenotypeDescription { get => _phenotypeDescription; set => _phenotypeDescription = value; }
        public bool IsRarePhenotype => _isRarePhenotype;
        public bool IsStablePhenotype => _isStablePhenotype;

        // Physical Expression
        public float HeightVariation => _heightVariation;
        public float WidthVariation => _widthVariation;
        public float NodeSpacingVariation => _nodeSpacingVariation;
        public float BranchingVariation => _branchingVariation;

        // Cannabinoid Expression
        public float THCVariation => _thcVariation;
        public float CBDVariation => _cbdVariation;
        public float TerpeneVariation => _terpeneVariation;
        public float ResinProductionVariation => _resinProductionVariation;

        // Flowering
        public float FloweringTimeVariation => _floweringTimeVariation;
        public float YieldVariation => _yieldVariation;
        public float BudDensityVariation => _budDensityVariation;

        // Visual
        public Color LeafColorVariation => _leafColorVariation;
        public Color BudColorVariation => _budColorVariation;
        public float LeafSizeVariation => _leafSizeVariation;
        public LeafPattern LeafPattern => _leafPattern;

        // Environmental Response
        public float EnvironmentalSensitivityVariation => _environmentalSensitivityVariation;
        public float StressToleranceVariation => _stressToleranceVariation;
        public float DiseaseResistanceVariation => _diseaseResistanceVariation;

        // Unique Traits
        public List<PhenotypeTrait> UniqueTraits => _uniqueTraits;
        public bool HasUniqueTerpeneProfile => _hasUniqueTerpeneProfile;
        public bool HasUniqueColorExpression => _hasUniqueColorExpression;
        public bool HasUniqueMorphology => _hasUniqueMorphology;

        /// <summary>
        /// Calculates the modified THC content for this phenotype
        /// </summary>
        public float GetModifiedTHCContent()
        {
            if (_parentStrain == null) return 0f;
            return _parentStrain.THCContent * _thcVariation;
        }

        /// <summary>
        /// Calculates the modified CBD content for this phenotype
        /// </summary>
        public float GetModifiedCBDContent()
        {
            if (_parentStrain == null) return 0f;
            return _parentStrain.CBDContent * _cbdVariation;
        }

        /// <summary>
        /// Calculates the modified yield for this phenotype
        /// </summary>
        public float GetModifiedYield()
        {
            if (_parentStrain == null) return 0f;
            return _parentStrain.BaseYield * _yieldVariation;
        }

        /// <summary>
        /// Calculates the modified flowering time for this phenotype
        /// </summary>
        public float GetModifiedFloweringTime()
        {
            if (_parentStrain == null) return 0f;
            return _parentStrain.BaseFloweringTime * _floweringTimeVariation;
        }

        /// <summary>
        /// Gets the overall rarity score for this phenotype
        /// </summary>
        public float GetRarityScore()
        {
            float rarityScore = _isRarePhenotype ? 0.8f : 0.2f;
            rarityScore += _uniqueTraits.Count * 0.1f;
            rarityScore += (_hasUniqueTerpeneProfile ? 0.2f : 0f);
            rarityScore += (_hasUniqueColorExpression ? 0.15f : 0f);
            rarityScore += (_hasUniqueMorphology ? 0.15f : 0f);

            return Mathf.Clamp01(rarityScore);
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_parentStrain == null)
            {
                SharedLogger.LogError($"PlantPhenotypeSO {name}: Parent strain is required.");
                isValid = false;
            }

            if (string.IsNullOrEmpty(_phenotypeId))
            {
                SharedLogger.LogError($"PlantPhenotypeSO {name}: Phenotype ID is required.");
                isValid = false;
            }

            if (string.IsNullOrEmpty(_phenotypeName))
            {
                SharedLogger.LogError($"PlantPhenotypeSO {name}: Phenotype name is required.");
                isValid = false;
            }

            return isValid;
        }
    }

    [System.Serializable]
    public class PhenotypeTrait
    {
        [SerializeField] private string _traitName;
        [SerializeField] private PhenotypeTraitCategory _traitType;
        [SerializeField, TextArea(2, 3)] private string _traitDescription;
        [SerializeField, Range(0f, 1f)] private float _expressionIntensity = 0.5f;
        [SerializeField] private bool _isVisibleTrait = true;
        [SerializeField] private bool _affectsYield = false;
        [SerializeField] private bool _affectsPotency = false;

        public string TraitName { get => _traitName; set => _traitName = value; }
        public PhenotypeTraitCategory TraitType { get => _traitType; set => _traitType = value; }
        public string TraitDescription { get => _traitDescription; set => _traitDescription = value; }
        public float ExpressionIntensity => _expressionIntensity;
        public bool IsVisibleTrait => _isVisibleTrait;
        public bool AffectsYield => _affectsYield;
        public bool AffectsPotency => _affectsPotency;
    }

    public enum LeafPattern
    {
        Standard,
        Webbed,
        Serrated,
        Smooth,
        Variegated,
        Twisted,
        Curled
    }

    public enum PhenotypeTraitCategory
    {
        Morphological,
        Biochemical,
        Physiological,
        Behavioral,
        Developmental,
        Resistance,
        Quality
    }
}
