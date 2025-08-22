using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Defines the base characteristics and parameters for a cannabis species.
    /// Contains fundamental genetic ranges and biological properties that all strains of this species inherit.
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Species", menuName = "Project Chimera/Genetics/Plant Species", order = 1)]
    public class PlantSpeciesSO : ChimeraDataSO
    {
        [Header("Species Classification")]
        [SerializeField] private SpeciesType _speciesType = SpeciesType.CannabisIndica;
        [SerializeField] private string _scientificName = "Cannabis indica";
        [SerializeField] private string _commonName = "Indica";
        [SerializeField, TextArea(2, 4)] private string _speciesDescription;

        [Header("Growth Characteristics")]
        [SerializeField] private Vector2 _heightRange = new Vector2(0.8f, 1.5f); // meters
        [SerializeField] private Vector2 _widthRange = new Vector2(0.6f, 1.2f); // meters
        [SerializeField] private Vector2 _internodeSpacing = new Vector2(0.02f, 0.08f); // meters
        [SerializeField] private Vector2 _leafSizeRange = new Vector2(0.1f, 0.3f); // meters
        [SerializeField] private int _maxBranchingLevels = 4;

        [Header("Lifecycle Timing (Days)")]
        [SerializeField] private Vector2 _germinationDays = new Vector2(2, 7);
        [SerializeField] private Vector2 _seedlingDays = new Vector2(14, 21);
        [SerializeField] private Vector2 _vegetativeDays = new Vector2(30, 90);
        [SerializeField] private Vector2 _floweringDays = new Vector2(45, 80);
        [SerializeField] private Vector2 _harvestWindowDays = new Vector2(5, 14);

        [Header("Environmental Preferences")]
        [SerializeField] private Vector2 _temperatureRange = new Vector2(18f, 28f); // Celsius
        [SerializeField] private Vector2 _humidityRange = new Vector2(40f, 70f); // %RH
        [SerializeField] private Vector2 _co2Range = new Vector2(350f, 1200f); // ppm
        [SerializeField] private Vector2 _lightIntensityRange = new Vector2(150f, 800f); // PPFD
        [SerializeField] private Vector2 _photoperiodRange = new Vector2(12f, 18f); // hours light per day

        [Header("Yield and Potency Ranges")]
        [SerializeField] private Vector2 _yieldPerPlantRange = new Vector2(20f, 150f); // grams dry weight
        [SerializeField] private Vector2 _thcPotentialRange = new Vector2(0.1f, 30f); // %THC
        [SerializeField] private Vector2 _cbdPotentialRange = new Vector2(0.05f, 25f); // %CBD
        [SerializeField] private Vector2 _terpeneContentRange = new Vector2(0.5f, 5f); // %total terpenes

        [Header("Stress Resistance")]
        [SerializeField, Range(0f, 1f)] private float _heatTolerance = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _coldTolerance = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _droughtTolerance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _humidityTolerance = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _nutrientStressTolerance = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _lightStressTolerance = 0.5f;

        [Header("Disease and Pest Resistance")]
        [SerializeField, Range(0f, 1f)] private float _powderyMildewResistance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _botrytisResistance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _spiderMiteResistance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _aphidResistance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _thripsResistance = 0.5f;

        [Header("Genetic Diversity")]
        [SerializeField, Range(0f, 1f)] private float _geneticStability = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _phenotypicVariation = 0.3f;
        [SerializeField] private int _chromosomeCount = 20; // Cannabis is diploid 2n=20

        // Public Properties
        public SpeciesType SpeciesType => _speciesType;
        public string ScientificName => _scientificName;
        public string CommonName => _commonName;
        public string SpeciesDescription => _speciesDescription;

        // Growth Characteristics
        public Vector2 HeightRange => _heightRange;
        public Vector2 WidthRange => _widthRange;
        public Vector2 InternodeSpacing => _internodeSpacing;
        public Vector2 LeafSizeRange => _leafSizeRange;
        public int MaxBranchingLevels => _maxBranchingLevels;

        // Lifecycle Timing
        public Vector2 GerminationDays => _germinationDays;
        public Vector2 SeedlingDays => _seedlingDays;
        public Vector2 VegetativeDays => _vegetativeDays;
        public Vector2 FloweringDays => _floweringDays;
        public Vector2 HarvestWindowDays => _harvestWindowDays;

        // Environmental Preferences
        public Vector2 TemperatureRange => _temperatureRange;
        public Vector2 HumidityRange => _humidityRange;
        public Vector2 Co2Range => _co2Range;
        public Vector2 LightIntensityRange => _lightIntensityRange;
        public Vector2 PhotoperiodRange => _photoperiodRange;

        // Yield and Potency
        public Vector2 YieldPerPlantRange => _yieldPerPlantRange;
        public Vector2 ThcPotentialRange => _thcPotentialRange;
        public Vector2 CbdPotentialRange => _cbdPotentialRange;
        public Vector2 TerpeneContentRange => _terpeneContentRange;

        // Stress Resistance
        public float HeatTolerance => _heatTolerance;
        public float ColdTolerance => _coldTolerance;
        public float DroughtTolerance => _droughtTolerance;
        public float HumidityTolerance => _humidityTolerance;
        public float NutrientStressTolerance => _nutrientStressTolerance;
        public float LightStressTolerance => _lightStressTolerance;

        // Disease and Pest Resistance
        public float PowderyMildewResistance => _powderyMildewResistance;
        public float BotrytisResistance => _botrytisResistance;
        public float SpiderMiteResistance => _spiderMiteResistance;
        public float AphidResistance => _aphidResistance;
        public float ThripsResistance => _thripsResistance;

        // Genetic Properties
        public float GeneticStability => _geneticStability;
        public float PhenotypicVariation => _phenotypicVariation;
        public int ChromosomeCount => _chromosomeCount;

        /// <summary>
        /// Evaluates how suitable given environmental conditions are for this species.
        /// </summary>
        /// <param name="conditions">Environmental conditions to evaluate</param>
        /// <returns>Suitability score from 0 (unsuitable) to 1 (optimal)</returns>
        public float EvaluateEnvironmentalSuitability(EnvironmentalConditions conditions)
        {
            float tempScore = CalculateRangeScore(conditions.Temperature, _temperatureRange);
            float humidityScore = CalculateRangeScore(conditions.Humidity, _humidityRange);
            float co2Score = CalculateRangeScore(conditions.CO2Level, _co2Range);
            float lightScore = CalculateRangeScore(conditions.LightIntensity, _lightIntensityRange);

            return (tempScore + humidityScore + co2Score + lightScore) * 0.25f;
        }

        private float CalculateRangeScore(float value, Vector2 range)
        {
            if (value < range.x || value > range.y)
            {
                // Outside range - calculate how far outside
                float distance = Mathf.Min(Mathf.Abs(value - range.x), Mathf.Abs(value - range.y));
                float rangeSize = range.y - range.x;
                return Mathf.Max(0f, 1f - (distance / (rangeSize * 0.5f)));
            }
            
            // Inside range - perfect score
            return 1f;
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (string.IsNullOrEmpty(_scientificName))
            {
                Debug.LogWarning($"[Chimera] PlantSpeciesSO '{DisplayName}' has no scientific name assigned.", this);
                isValid = false;
            }

            if (_heightRange.x <= 0 || _heightRange.y <= _heightRange.x)
            {
                Debug.LogWarning($"[Chimera] PlantSpeciesSO '{DisplayName}' has invalid height range.", this);
                isValid = false;
            }

            if (_germinationDays.x <= 0 || _floweringDays.y <= 0)
            {
                Debug.LogWarning($"[Chimera] PlantSpeciesSO '{DisplayName}' has invalid lifecycle timing.", this);
                isValid = false;
            }

            return isValid;
        }
    }
    /// <summary>
    /// Cannabis species types with their basic characteristics.
    /// </summary>
    public enum SpeciesType
    {
        CannabisIndica,
        CannabisSativa,
        CannabisRuderalis,
        CannabisHybrid
    }
}
