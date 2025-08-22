using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{


    /// <summary>
    /// Defines a specific cannabis strain with its unique genetic profile, characteristics, and breeding history.
    /// Inherits base parameters from PlantSpeciesSO and adds strain-specific modifications and traits.
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Strain", menuName = "Project Chimera/Genetics/Plant Strain", order = 2)]
    public class PlantStrainSO : ChimeraDataSO
    {
        [Header("Strain Identity")]
        [SerializeField] private PlantSpeciesSO _baseSpecies;
        [SerializeField] private string _strainId;
        [SerializeField] private string _strainName;
        [SerializeField] private string _breederName;
        [SerializeField] private string _originRegion;
        [SerializeField, TextArea(3, 6)] private string _strainDescription;
        [SerializeField] private StrainType _strainType = StrainType.Hybrid;
        [SerializeField] private bool _isFounderStrain = false;
        [SerializeField] private bool _isCustomStrain = false;

        [Header("Breeding Lineage")]
        [SerializeField] private PlantStrainSO _parentStrain1;
        [SerializeField] private PlantStrainSO _parentStrain2;
        [SerializeField] private int _generationNumber = 1; // F1, F2, etc.
        [SerializeField] private bool _isLandrace = false;
        [SerializeField] private bool _isStabilized = false;
        [SerializeField, Range(0f, 1f)] private float _breedingStability = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _geneticDiversity = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _rarityScore = 0.5f;

        [Header("Genetic Modifiers (Applied to Base Species)")]
        [SerializeField, Range(0.5f, 2f)] private float _heightModifier = 1f;
        [SerializeField, Range(0.5f, 2f)] private float _widthModifier = 1f;
        [SerializeField, Range(0.5f, 2f)] private float _yieldModifier = 1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _growthRateModifier = 1f;

        [Header("Flowering Characteristics")]
        [SerializeField] private PhotoperiodSensitivity _photoperiodSensitivity = PhotoperiodSensitivity.Photoperiod;
        [SerializeField, Range(0.8f, 1.2f)] private float _floweringTimeModifier = 1f;
        [SerializeField] private bool _autoflowering = false;
        [SerializeField] private int _autofloweringTriggerDays = 0;

        [Header("Cannabinoid Profile")]
        [SerializeField] private CannabinoidProfile _cannabinoidProfile;

        [Header("Terpene Profile")]
        [SerializeField] private TerpeneProfile _terpeneProfile;

        [Header("Morphological Traits")]
        [SerializeField] private LeafStructure _leafStructure = LeafStructure.Broad;
        [SerializeField] private BudStructure _budStructure = BudStructure.Dense;
        [SerializeField] private Color _leafColor = Color.green;
        [SerializeField] private Color _budColor = Color.green;
        [SerializeField, Range(0.5f, 2f)] private float _resinProductionModifier = 1f;

        [Header("Strain-Specific Resistances")]
        [SerializeField, Range(-0.3f, 0.3f)] private float _heatToleranceModifier = 0f;
        [SerializeField, Range(-0.3f, 0.3f)] private float _coldToleranceModifier = 0f;
        [SerializeField, Range(-0.3f, 0.3f)] private float _droughtToleranceModifier = 0f;
        [SerializeField, Range(-0.3f, 0.3f)] private float _diseaseResistanceModifier = 0f;

        [Header("Cultivation Difficulty")]
        [SerializeField] private DifficultyLevel _cultivationDifficulty = DifficultyLevel.Intermediate;
        [SerializeField, Range(0f, 1f)] private float _beginerFriendliness = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _environmentalSensitivity = 0.5f;

        [Header("Effects and Medical Properties")]
        [SerializeField] private EffectsProfile _effectsProfile;
        [SerializeField] private List<MedicalApplication> _medicalApplications = new List<MedicalApplication>();

        [Header("Environmental Interactions")]
        [SerializeField] private GxEInteractionProfile _gxeProfile;

        [Header("Commercial Properties")]
        [SerializeField, Range(0f, 100f)] private float _marketValue = 10f; // per gram
        [SerializeField, Range(0f, 1f)] private float _marketDemand = 0.5f;
        [SerializeField] private bool _seedsAvailable = true;
        [SerializeField] private bool _clonesAvailable = false;

        [Header("Cultivation System Properties")]
        
        [SerializeField] private AnimationCurve _growthCurve;
        [SerializeField, Range(0.5f, 2f)] private float _baseHealthModifier = 1f;
        [SerializeField, Range(0.01f, 0.5f)] private float _healthRecoveryRate = 0.1f;
        [SerializeField, Range(20f, 200f)] private float _baseYieldGrams = 100f;
        [SerializeField, Range(0.5f, 2f)] private float _baseQualityModifier = 1f;
        [SerializeField, Range(0.5f, 2f)] private float _basePotencyModifier = 1f;
        [SerializeField, Range(40f, 120f)] private int _baseFloweringTime = 60;
        [SerializeField, Range(0.3f, 5f)] private float _baseHeight = 1.5f;

        // Public Properties
        public PlantSpeciesSO BaseSpecies => _baseSpecies;
        public string StrainId { get => _strainId; set => _strainId = value; }
        public string StrainName { get => _strainName; set => _strainName = value; }
        public string BreederName => _breederName;
        public string OriginRegion => _originRegion;
        public string StrainDescription { get => _strainDescription; set => _strainDescription = value; }
        public StrainType StrainType { get => _strainType; set => _strainType = value; }
        public bool IsFounderStrain => _isFounderStrain;
        public bool IsCustomStrain => _isCustomStrain;

        // Breeding Properties
        public PlantStrainSO ParentStrain1 => _parentStrain1;
        public PlantStrainSO ParentStrain2 => _parentStrain2;
        public int GenerationNumber => _generationNumber;
        public bool IsLandrace => _isLandrace;
        public bool IsStabilized => _isStabilized;
        public float BreedingStability => _breedingStability;
        public float GeneticDiversity => _geneticDiversity;
        public float RarityScore => _rarityScore;
        public PlantStrainSO GeneticProfile => this; // Self-reference for compatibility

        // Genetic Modifiers
        public float HeightModifier => _heightModifier;
        public float WidthModifier => _widthModifier;
        public float YieldModifier => _yieldModifier;
        public float GrowthRateModifier => _growthRateModifier;

        // Flowering
        public PhotoperiodSensitivity PhotoperiodSensitivity => _photoperiodSensitivity;
        public float FloweringTimeModifier => _floweringTimeModifier;
        public bool Autoflowering => _autoflowering;
        public int AutofloweringTriggerDays => _autofloweringTriggerDays;

        // Profiles
        public CannabinoidProfile CannabinoidProfile => _cannabinoidProfile;
        public TerpeneProfile TerpeneProfile => _terpeneProfile;
        public EffectsProfile EffectsProfile => _effectsProfile;

        // Morphology
        public LeafStructure LeafStructure => _leafStructure;
        public BudStructure BudStructure => _budStructure;
        public Color LeafColor => _leafColor;
        public Color BudColor => _budColor;
        public float ResinProductionModifier => _resinProductionModifier;

        // Resistances
        public float HeatToleranceModifier => _heatToleranceModifier;
        public float ColdToleranceModifier => _coldToleranceModifier;
        public float DroughtToleranceModifier => _droughtToleranceModifier;
        public float DiseaseResistanceModifier => _diseaseResistanceModifier;

        // Cultivation
        public DifficultyLevel CultivationDifficulty => _cultivationDifficulty;
        public float BeginnerFriendliness => _beginerFriendliness;
        public float EnvironmentalSensitivity => _environmentalSensitivity;

        // Commercial
        public float MarketValue => _marketValue;
        public float MarketDemand => _marketDemand;
        public bool SeedsAvailable => _seedsAvailable;
        public bool ClonesAvailable => _clonesAvailable;

        // Environmental Interactions
        public GxEInteractionProfile GxEProfile => _gxeProfile;

        // Cultivation System
        
        public AnimationCurve GrowthCurve => _growthCurve;
        public float BaseHealthModifier => _baseHealthModifier;
        public float HealthRecoveryRate => _healthRecoveryRate;
        public float BaseYieldGrams => _baseYieldGrams;
        public float BaseQualityModifier => _baseQualityModifier;
        public float BasePotencyModifier => _basePotencyModifier;
        public int BaseFloweringTime => _baseFloweringTime;
        public float BaseHeight => _baseHeight;

        // UI Compatibility Properties
        public StrainRarity StrainRarity => StrainRarity.Common; // Default rarity - could be calculated based on breeding complexity
        public string Type => _strainType.ToString();
        public float THCLevel => _cannabinoidProfile?.ThcPercentage ?? 0f;
        public float CBDLevel => _cannabinoidProfile?.CbdPercentage ?? 0f;
        public float YieldPotential => _baseYieldGrams;

        /// <summary>
        /// Gets the THC content percentage for this strain.
        /// </summary>
        public float THCContent()
        {
            return _cannabinoidProfile?.ThcPercentage ?? 0f;
        }

        /// <summary>
        /// Gets the CBD content percentage for this strain.
        /// </summary>
        public float CBDContent()
        {
            return _cannabinoidProfile?.CbdPercentage ?? 0f;
        }

        /// <summary>
        /// Gets the base yield in grams for this strain.
        /// </summary>
        public float BaseYield()
        {
            return _baseYieldGrams;
        }

        /// <summary>
        /// Calculates the modified height range for this strain.
        /// </summary>
        public Vector2 GetModifiedHeightRange()
        {
            if (_baseSpecies == null) return Vector2.zero;
            
            Vector2 baseRange = _baseSpecies.HeightRange;
            return new Vector2(
                baseRange.x * _heightModifier,
                baseRange.y * _heightModifier
            );
        }

        /// <summary>
        /// Calculates the modified yield range for this strain.
        /// </summary>
        public Vector2 GetModifiedYieldRange()
        {
            if (_baseSpecies == null) return Vector2.zero;
            
            Vector2 baseRange = _baseSpecies.YieldPerPlantRange;
            return new Vector2(
                baseRange.x * _yieldModifier,
                baseRange.y * _yieldModifier
            );
        }

        /// <summary>
        /// Calculates the modified flowering time for this strain.
        /// </summary>
        public Vector2 GetModifiedFloweringTime()
        {
            if (_baseSpecies == null) return Vector2.zero;
            
            Vector2 baseRange = _baseSpecies.FloweringDays;
            return new Vector2(
                baseRange.x * _floweringTimeModifier,
                baseRange.y * _floweringTimeModifier
            );
        }



        private float CalculateToleranceAdjustment(float value, Vector2 range, float toleranceModifier)
        {
            if (toleranceModifier == 0f) return 0f;
            
            if (value < range.x || value > range.y)
            {
                // Outside optimal range - tolerance modifier can help
                return toleranceModifier * 0.2f; // Max 6% improvement
            }
            
            return 0f;
        }
        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_baseSpecies == null)
            {
                Debug.LogWarning($"[Chimera] PlantStrainSO '{DisplayName}' has no base species assigned.", this);
                isValid = false;
            }

            if (string.IsNullOrEmpty(_strainName))
            {
                Debug.LogWarning($"[Chimera] PlantStrainSO '{DisplayName}' has no strain name assigned.", this);
                isValid = false;
            }

            if (_autoflowering && _autofloweringTriggerDays <= 0)
            {
                Debug.LogWarning($"[Chimera] Autoflowering strain '{DisplayName}' has invalid trigger days.", this);
                isValid = false;
            }

            return isValid;
        }
    }

    [System.Serializable]
    public class CannabinoidProfile
    {
        [Range(0f, 35f)] public float ThcPercentage = 15f;
        [Range(0f, 25f)] public float CbdPercentage = 1f;
        [Range(0f, 5f)] public float CbgPercentage = 0.5f;
        [Range(0f, 3f)] public float CbnPercentage = 0.1f;
        [Range(0f, 2f)] public float CbcPercentage = 0.1f;
        [Range(0f, 1f)] public float ThcvPercentage = 0.1f;
        
        /// <summary>
        /// Gets the THC content percentage
        /// </summary>
        public float THC() => ThcPercentage;
        
        /// <summary>
        /// Gets the CBD content percentage
        /// </summary>
        public float CBD() => CbdPercentage;
        
        /// <summary>
        /// Gets the CBG content percentage
        /// </summary>
        public float CBG() => CbgPercentage;
        
        /// <summary>
        /// Gets the CBN content percentage
        /// </summary>
        public float CBN() => CbnPercentage;
    }

    [System.Serializable]
    public class TerpeneProfile
    {
        [Range(0f, 3f)] public float Myrcene = 0.5f;
        [Range(0f, 2f)] public float Limonene = 0.3f;
        [Range(0f, 2f)] public float Pinene = 0.2f;
        [Range(0f, 1.5f)] public float Linalool = 0.1f;
        [Range(0f, 1.5f)] public float Caryophyllene = 0.2f;
        [Range(0f, 1f)] public float Humulene = 0.1f;
        [Range(0f, 1f)] public float Terpinolene = 0.1f;
    }

    [System.Serializable]
    public class EffectsProfile
    {
        [Range(0f, 1f)] public float Euphoria = 0.5f;
        [Range(0f, 1f)] public float Relaxation = 0.5f;
        [Range(0f, 1f)] public float Creativity = 0.3f;
        [Range(0f, 1f)] public float Focus = 0.3f;
        [Range(0f, 1f)] public float Energy = 0.4f;
        [Range(0f, 1f)] public float Sedation = 0.3f;
        [Range(0f, 1f)] public float AppetiteStimulation = 0.4f;
        [Range(0f, 1f)] public float PainRelief = 0.3f;
    }



    public enum PhotoperiodSensitivity
    {
        Photoperiod,
        Autoflower,
        SemiAutoflower
    }

    public enum LeafStructure
    {
        Narrow,
        Medium,
        Broad,
        Serrated,
        Smooth
    }

    public enum BudStructure
    {
        Loose,
        Medium,
        Dense,
        Airy,
        Compact
    }

    public enum MedicalApplication
    {
        PainRelief,
        AnxietyReduction,
        DepressionSupport,
        InsomniaTreatment,
        AppetiteStimulation,
        NauseaReduction,
        InflammationReduction,
        MuscleRelaxation,
        SeizureControl,
        GlaucomaTreatment
    }

    public enum StrainRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Custom
    }


    /// <summary>
    /// Represents a pair of alleles for a specific gene locus.
    /// </summary>
    [System.Serializable]
    public class AlleleCouple
    {
        public AlleleSO Allele1;
        public AlleleSO Allele2;
        
        public AlleleCouple(AlleleSO allele1, AlleleSO allele2)
        {
            Allele1 = allele1;
            Allele2 = allele2;
        }
    }

    /// <summary>
    /// Represents a genetic mutation that occurred during breeding.
    /// </summary>
    [System.Serializable]
    public class GeneticMutation
    {
        public string MutationID;
        public string GeneLocusAffected;
        public MutationType MutationType;
        public string OriginalAlleleID;
        public string MutatedAlleleID;
        public float PhenotypicEffect;
        public string Description;
        public System.DateTime OccurrenceDate;
        public bool IsBeneficial;
        public bool IsHarmful;
        public bool IsNeutral;
    }
    
    /// <summary>
    /// Data structure for plant strain information used in UI
    /// </summary>
    [System.Serializable]
    public struct PlantStrainData
    {
        public string StrainName;
        public string BreederName;
        public string OriginRegion;
        public string Description;
        public StrainType StrainType;
        public StrainRarity Rarity;
        public float ThcPercentage;
        public float CBDPercentage;
        public float FloweringTime;
        public float YieldPotential;
        public float PotencyRating;
        public bool IsStable;
        public bool IsAutoflower;
        public string[] ParentStrains;
        public string StrainID;
        
        // Additional properties for testing compatibility
        public string GeneticLineage;
        public string BreedingGeneration;
        public System.Collections.Generic.List<string> Traits;
        
        // Compatibility properties for UI and testing
        public string Id => StrainID;
        public string Name => StrainName;
        public string Type => StrainType.ToString();
        public float THCLevel => ThcPercentage;
        public float CBDLevel => CBDPercentage;
        public string StrainId 
        { 
            get => StrainID; 
            set => StrainID = value; 
        }
        public float ThcContent 
        { 
            get => ThcPercentage; 
            set => ThcPercentage = value; 
        }
        public float CbdContent 
        { 
            get => CBDPercentage; 
            set => CBDPercentage = value; 
        }
        public float Yield 
        { 
            get => YieldPotential; 
            set => YieldPotential = value; 
        }
        
        public static PlantStrainData FromSO(PlantStrainSO strainSO)
        {
            if (strainSO == null)
            {
                return new PlantStrainData
                {
                    StrainName = "Unknown",
                    BreederName = "Unknown",
                    OriginRegion = "Unknown",
                    Description = "",
                    StrainType = StrainType.Hybrid,
                    Rarity = StrainRarity.Common,
                    ThcPercentage = 0f,
                    CBDPercentage = 0f,
                    FloweringTime = 0f,
                    YieldPotential = 0f,
                    PotencyRating = 0f,
                    IsStable = false,
                    IsAutoflower = false,
                    ParentStrains = new string[0],
                    StrainID = "",
                    GeneticLineage = "Unknown",
                    BreedingGeneration = "F1",
                    Traits = new System.Collections.Generic.List<string>()
                };
            }
            
            return new PlantStrainData
            {
                StrainName = strainSO.StrainName,
                BreederName = strainSO.BreederName,
                OriginRegion = strainSO.OriginRegion,
                Description = strainSO.StrainDescription,
                StrainType = strainSO.StrainType,
                Rarity = StrainRarity.Common,
                ThcPercentage = strainSO.THCLevel, // Use actual THC level
                CBDPercentage = strainSO.CBDLevel, // Use actual CBD level
                FloweringTime = strainSO.BaseFloweringTime, // Use actual flowering time
                YieldPotential = strainSO.YieldPotential, // Use actual yield potential
                PotencyRating = 0.7f,  // Placeholder - would need actual calculation
                IsStable = strainSO.IsStabilized, // Use actual stability
                IsAutoflower = strainSO.Autoflowering, // Use actual autoflower status
                ParentStrains = new string[0], // Placeholder - would need actual parent tracking
                StrainID = strainSO.StrainId,
                GeneticLineage = $"{strainSO.ParentStrain1?.StrainName ?? "Unknown"} x {strainSO.ParentStrain2?.StrainName ?? "Unknown"}",
                BreedingGeneration = $"F{strainSO.GenerationNumber}",
                Traits = new System.Collections.Generic.List<string> { strainSO.StrainType.ToString(), strainSO.BudStructure.ToString() }
            };
        }
    }

}
