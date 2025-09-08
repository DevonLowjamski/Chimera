using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Contains genetic-specific data for cannabis strains including lineage, breeding, and genetic expression.
    /// Separated from PlantStrainSO to follow Single Responsibility Principle.
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Genetics Data", menuName = "Project Chimera/Genetics/Plant Genetics Data", order = 10)]
    public class PlantGeneticsData : ChimeraDataSO
    {
        [Header("Genetic Identity")]
        [SerializeField] private string _geneticId;
        [SerializeField] private bool _isFounderGenetics = false;
        [SerializeField] private bool _isCustomGenetics = false;

        [Header("Breeding Lineage")]
        [SerializeField] private PlantGeneticsData _parentGenetics1;
        [SerializeField] private PlantGeneticsData _parentGenetics2;
        [SerializeField] private int _generationNumber = 1; // F1, F2, etc.
        [SerializeField] private bool _isLandrace = false;
        [SerializeField] private bool _isStabilized = false;
        [SerializeField, Range(0f, 1f)] private float _breedingStability = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _geneticDiversity = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _rarityScore = 0.5f;

        [Header("Genetic Modifiers")]
        [SerializeField, Range(0.5f, 2f)] private float _heightModifier = 1f;
        [SerializeField, Range(0.5f, 2f)] private float _widthModifier = 1f;
        [SerializeField, Range(0.5f, 2f)] private float _yieldModifier = 1f;
        [SerializeField, Range(0.8f, 1.2f)] private float _growthRateModifier = 1f;

        [Header("Flowering Genetics")]
        [SerializeField] private PhotoperiodSensitivity _photoperiodSensitivity = PhotoperiodSensitivity.Photoperiod;
        [SerializeField, Range(0.8f, 1.2f)] private float _floweringTimeModifier = 1f;
        [SerializeField] private bool _autoflowering = false;
        [SerializeField] private int _autofloweringTriggerDays = 0;

        [Header("Chemical Genetics")]
        [SerializeField] private CannabinoidProfile _cannabinoidProfile;
        [SerializeField] private TerpeneProfile _terpeneProfile;

        [Header("Morphological Genetics")]
        [SerializeField] private LeafStructure _leafStructure;
        [SerializeField] private BudStructure _budStructure;

        // Constructor to initialize default values
        public PlantGeneticsData()
        {
            _leafStructure = new LeafStructure { broad = true };
            _budStructure = new BudStructure { dense = true };
        }
        [SerializeField] private Color _leafColor = Color.green;
        [SerializeField] private Color _budColor = Color.green;
        [SerializeField, Range(0.5f, 2f)] private float _resinProductionModifier = 1f;

        [Header("Genetic Resistances")]
        [SerializeField, Range(-0.3f, 0.3f)] private float _heatToleranceModifier = 0f;
        [SerializeField, Range(-0.3f, 0.3f)] private float _coldToleranceModifier = 0f;
        [SerializeField, Range(-0.3f, 0.3f)] private float _droughtToleranceModifier = 0f;
        [SerializeField, Range(-0.3f, 0.3f)] private float _diseaseResistanceModifier = 0f;

        [Header("Environmental Interactions")]
        [SerializeField] private GxEInteractionProfile _gxeProfile;

        // Public Properties
        public string GeneticId { get => _geneticId; set => _geneticId = value; }
        public bool IsFounderGenetics => _isFounderGenetics;
        public bool IsCustomGenetics => _isCustomGenetics;

        // Breeding Properties
        public PlantGeneticsData ParentGenetics1 => _parentGenetics1;
        public PlantGeneticsData ParentGenetics2 => _parentGenetics2;
        public int GenerationNumber => _generationNumber;
        public bool IsLandrace => _isLandrace;
        public bool IsStabilized => _isStabilized;
        public float BreedingStability => _breedingStability;
        public float GeneticDiversity => _geneticDiversity;
        public float RarityScore => _rarityScore;

        // Genetic Modifiers
        public float HeightModifier => _heightModifier;
        public float WidthModifier => _widthModifier;
        public float YieldModifier => _yieldModifier;
        public float GrowthRateModifier => _growthRateModifier;

        // Flowering Genetics
        public PhotoperiodSensitivity PhotoperiodSensitivity => _photoperiodSensitivity;
        public float FloweringTimeModifier => _floweringTimeModifier;
        public bool Autoflowering => _autoflowering;
        public int AutofloweringTriggerDays => _autofloweringTriggerDays;

        // Chemical Profiles
        public CannabinoidProfile CannabinoidProfile => _cannabinoidProfile;
        public TerpeneProfile TerpeneProfile => _terpeneProfile;

        // Morphological Genetics
        public LeafStructure LeafStructure => _leafStructure;
        public BudStructure BudStructure => _budStructure;
        public Color LeafColor => _leafColor;
        public Color BudColor => _budColor;
        public float ResinProductionModifier => _resinProductionModifier;

        // Genetic Resistances
        public float HeatToleranceModifier => _heatToleranceModifier;
        public float ColdToleranceModifier => _coldToleranceModifier;
        public float DroughtToleranceModifier => _droughtToleranceModifier;
        public float DiseaseResistanceModifier => _diseaseResistanceModifier;

        // Environmental Interactions
        public GxEInteractionProfile GxEProfile => _gxeProfile;

        // Calculated Properties
        public float THCLevel => _cannabinoidProfile?.ThcPercentage ?? 0f;
        public float CBDLevel => _cannabinoidProfile?.CbdPercentage ?? 0f;

        /// <summary>
        /// Gets the THC content percentage for this genetic profile.
        /// </summary>
        public float THCContent()
        {
            return _cannabinoidProfile?.ThcPercentage ?? 0f;
        }

        /// <summary>
        /// Gets the CBD content percentage for this genetic profile.
        /// </summary>
        public float CBDContent()
        {
            return _cannabinoidProfile?.CbdPercentage ?? 0f;
        }

        /// <summary>
        /// Calculates the modified height based on genetic modifiers.
        /// </summary>
        public Vector2 GetModifiedHeightRange(Vector2 baseRange)
        {
            return new Vector2(
                baseRange.x * _heightModifier,
                baseRange.y * _heightModifier
            );
        }

        /// <summary>
        /// Calculates the modified yield based on genetic modifiers.
        /// </summary>
        public Vector2 GetModifiedYieldRange(Vector2 baseRange)
        {
            return new Vector2(
                baseRange.x * _yieldModifier,
                baseRange.y * _yieldModifier
            );
        }

        /// <summary>
        /// Calculates the modified flowering time based on genetic modifiers.
        /// </summary>
        public Vector2 GetModifiedFloweringTime(Vector2 baseRange)
        {
            return new Vector2(
                baseRange.x * _floweringTimeModifier,
                baseRange.y * _floweringTimeModifier
            );
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (string.IsNullOrEmpty(_geneticId))
            {
                SharedLogger.LogWarning($"[Chimera] PlantGeneticsData '{DisplayName}' has no genetic ID assigned.");
                isValid = false;
            }

            if (_autoflowering && _autofloweringTriggerDays <= 0)
            {
                SharedLogger.LogWarning($"[Chimera] Autoflowering genetics '{DisplayName}' has invalid trigger days.");
                isValid = false;
            }

            return isValid;
        }
    }
}
