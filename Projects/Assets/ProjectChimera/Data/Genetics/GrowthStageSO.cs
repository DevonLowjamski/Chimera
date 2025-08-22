using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Defines the characteristics, requirements, and behaviors for a specific plant growth stage.
    /// Each stage has unique environmental needs, resource consumption, and visual characteristics.
    /// </summary>
    [CreateAssetMenu(fileName = "New Growth Stage", menuName = "Project Chimera/Genetics/Growth Stage", order = 3)]
    public class GrowthStageSO : ChimeraDataSO
    {
        [Header("Stage Identity")]
        [SerializeField] private PlantGrowthStage _stageType = PlantGrowthStage.Seedling;
        [SerializeField] private string _stageName;
        [SerializeField, TextArea(2, 4)] private string _stageDescription;
        [SerializeField] private int _stageOrder = 0; // 0-based ordering for progression

        [Header("Duration Parameters")]
        [SerializeField] private bool _hasFixedDuration = true;
        [SerializeField] private Vector2 _durationDaysRange = new Vector2(14, 21);
        [SerializeField] private bool _canBeAccelerated = true;
        [SerializeField] private bool _canBeDelayed = true;
        [SerializeField, Range(0.1f, 5f)] private float _maxAccelerationMultiplier = 1.5f;
        [SerializeField, Range(0.1f, 5f)] private float _maxDelayMultiplier = 2f;

        [Header("Environmental Requirements")]
        [SerializeField] private EnvironmentalRequirements _environmentalReqs;

        [Header("Nutrient Requirements")]
        [SerializeField] private NutrientRequirements _nutrientReqs;

        [Header("Water Requirements")]
        [SerializeField] private WaterRequirements _waterReqs;

        [Header("Growth Characteristics")]
        [SerializeField, Range(0f, 5f)] private float _dailyHeightGrowthCm = 1f;
        [SerializeField, Range(0f, 5f)] private float _dailyWidthGrowthCm = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _leafDevelopmentRate = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _rootDevelopmentRate = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _biomassAccumulationRate = 0.05f;

        [Header("Resource Consumption")]
        [SerializeField] private ResourceConsumption _resourceConsumption;

        [Header("Visual Characteristics")]
        [SerializeField] private VisualCharacteristics _visualChars;

        [Header("Stage Transitions")]
        [SerializeField] private bool _autoTransition = true;
        [SerializeField] private TransitionTrigger _transitionTrigger = TransitionTrigger.TimeElapsed;
        [SerializeField] private PlantGrowthStage _nextStage = PlantGrowthStage.Vegetative;
        [SerializeField] private bool _canSkipStage = false;
        [SerializeField] private bool _canRevertToPrevious = false;

        [Header("Stress and Health")]
        [SerializeField, Range(0f, 1f)] private float _stressResistance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _recoveryRate = 0.1f;
        [SerializeField] private float _lethalStressThreshold = 0.9f;
        [SerializeField] private bool _canDieInStage = true;

        [Header("Special Properties")]
        [SerializeField] private bool _photoperiodSensitive = false;
        [SerializeField] private bool _requiresDarkPeriod = false;
        [SerializeField] private bool _producesFlowers = false;
        [SerializeField] private bool _producesTrichomes = false;
        [SerializeField] private bool _canBeHarvested = false;

        // Public Properties
        public PlantGrowthStage StageType => _stageType;
        public PlantGrowthStage Stage => _stageType; // Alias for compatibility
        public string StageName => _stageName;
        public string StageDescription => _stageDescription;
        public int StageOrder => _stageOrder;

        // Duration
        public bool HasFixedDuration => _hasFixedDuration;
        public Vector2 DurationDaysRange => _durationDaysRange;
        public float StageDuration => (_durationDaysRange.x + _durationDaysRange.y) / 2f; // Average duration for compatibility
        public bool CanBeAccelerated => _canBeAccelerated;
        public bool CanBeDelayed => _canBeDelayed;
        public float MaxAccelerationMultiplier => _maxAccelerationMultiplier;
        public float MaxDelayMultiplier => _maxDelayMultiplier;

        // Requirements
        public EnvironmentalRequirements EnvironmentalReqs => _environmentalReqs;
        public NutrientRequirements NutrientReqs => _nutrientReqs;
        public WaterRequirements WaterReqs => _waterReqs;

        // Growth
        public float DailyHeightGrowthCm => _dailyHeightGrowthCm;
        public float DailyWidthGrowthCm => _dailyWidthGrowthCm;
        public float LeafDevelopmentRate => _leafDevelopmentRate;
        public float RootDevelopmentRate => _rootDevelopmentRate;
        public float BiomassAccumulationRate => _biomassAccumulationRate;

        // Resources
        public ResourceConsumption ResourceConsumption => _resourceConsumption;

        // Visual
        public VisualCharacteristics VisualChars => _visualChars;

        // Transitions
        public bool AutoTransition => _autoTransition;
        public TransitionTrigger TransitionTrigger => _transitionTrigger;
        public PlantGrowthStage NextStage => _nextStage;
        public bool CanSkipStage => _canSkipStage;
        public bool CanRevertToPrevious => _canRevertToPrevious;

        // Health
        public float StressResistance => _stressResistance;
        public float RecoveryRate => _recoveryRate;
        public float LethalStressThreshold => _lethalStressThreshold;
        public bool CanDieInStage => _canDieInStage;

        // Special Properties
        public bool PhotoperiodSensitive => _photoperiodSensitive;
        public bool RequiresDarkPeriod => _requiresDarkPeriod;
        public bool ProducesFlowers => _producesFlowers;
        public bool ProducesTrichomes => _producesTrichomes;
        public bool CanBeHarvested => _canBeHarvested;

        /// <summary>
        /// Calculates the optimal daily growth for this stage under given conditions.
        /// </summary>
        public float CalculateOptimalGrowthRate(EnvironmentalConditions environment, NutritionStatus nutrition, WaterStatus water)
        {
            float envSuitability = EvaluateEnvironmentalSuitability(environment);
            float nutSuitability = EvaluateNutritionalSuitability(nutrition);
            float waterSuitability = EvaluateWaterSuitability(water);

            return (envSuitability + nutSuitability + waterSuitability) / 3f;
        }

        /// <summary>
        /// Evaluates how suitable the environmental conditions are for this growth stage.
        /// </summary>
        public float EvaluateEnvironmentalSuitability(EnvironmentalConditions environment)
        {
            float tempScore = CalculateRangeScore(environment.Temperature, _environmentalReqs.TemperatureRange);
            float humidityScore = CalculateRangeScore(environment.Humidity, _environmentalReqs.HumidityRange);
            float co2Score = CalculateRangeScore(environment.CO2Level, _environmentalReqs.Co2Range);
            float lightScore = CalculateRangeScore(environment.LightIntensity, _environmentalReqs.LightIntensityRange);

            return (tempScore + humidityScore + co2Score + lightScore) * 0.25f;
        }

        /// <summary>
        /// Evaluates how suitable the nutritional conditions are for this growth stage.
        /// </summary>
        public float EvaluateNutritionalSuitability(NutritionStatus nutrition)
        {
            float nScore = CalculateRangeScore(nutrition.Nitrogen, _nutrientReqs.NitrogenRange);
            float pScore = CalculateRangeScore(nutrition.Phosphorus, _nutrientReqs.PhosphorusRange);
            float kScore = CalculateRangeScore(nutrition.Potassium, _nutrientReqs.PotassiumRange);
            float phScore = CalculateRangeScore(nutrition.pH, _nutrientReqs.PhRange);

            return (nScore + pScore + kScore + phScore) * 0.25f;
        }

        /// <summary>
        /// Evaluates how suitable the water conditions are for this growth stage.
        /// </summary>
        public float EvaluateWaterSuitability(WaterStatus water)
        {
            float moistureScore = CalculateRangeScore(water.MoistureLevel, _waterReqs.MoistureRange);
            float drainageScore = CalculateRangeScore(water.DrainageRate, _waterReqs.DrainageRange);
            float qualityScore = water.WaterQuality; // Already 0-1 normalized

            return (moistureScore + drainageScore + qualityScore) / 3f;
        }

        private float CalculateRangeScore(float value, Vector2 range)
        {
            if (value < range.x || value > range.y)
            {
                float distance = Mathf.Min(Mathf.Abs(value - range.x), Mathf.Abs(value - range.y));
                float rangeSize = range.y - range.x;
                return Mathf.Max(0f, 1f - (distance / (rangeSize * 0.5f)));
            }
            return 1f;
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (string.IsNullOrEmpty(_stageName))
            {
                Debug.LogWarning($"[Chimera] GrowthStageSO '{DisplayName}' has no stage name assigned.", this);
                isValid = false;
            }

            if (_durationDaysRange.x <= 0 || _durationDaysRange.y < _durationDaysRange.x)
            {
                Debug.LogWarning($"[Chimera] GrowthStageSO '{DisplayName}' has invalid duration range.", this);
                isValid = false;
            }

            return isValid;
        }
    }

    [System.Serializable]
    public class EnvironmentalRequirements
    {
        [Header("Temperature (°C)")]
        public Vector2 TemperatureRange = new Vector2(20f, 26f);
        public Vector2 OptimalTemperatureRange = new Vector2(22f, 24f);

        [Header("Humidity (%RH)")]
        public Vector2 HumidityRange = new Vector2(50f, 70f);
        public Vector2 OptimalHumidityRange = new Vector2(55f, 65f);

        [Header("CO2 (ppm)")]
        public Vector2 Co2Range = new Vector2(350f, 1000f);
        public Vector2 OptimalCo2Range = new Vector2(600f, 800f);

        [Header("Light (PPFD)")]
        public Vector2 LightIntensityRange = new Vector2(100f, 600f);
        public Vector2 OptimalLightRange = new Vector2(200f, 400f);

        [Header("Photoperiod")]
        public Vector2 PhotoperiodRange = new Vector2(12f, 18f);
        public float OptimalPhotoperiod = 16f;

        [Header("Air Movement")]
        [Range(0f, 2f)] public float MinAirFlow = 0.5f;
        [Range(0f, 2f)] public float MaxAirFlow = 1.5f;
        [Range(0f, 2f)] public float OptimalAirFlow = 1f;
    }

    [System.Serializable]
    public class NutrientRequirements
    {
        [Header("Macronutrients (ppm)")]
        public Vector2 NitrogenRange = new Vector2(100f, 200f);
        public Vector2 PhosphorusRange = new Vector2(30f, 60f);
        public Vector2 PotassiumRange = new Vector2(150f, 300f);

        [Header("pH and EC")]
        public Vector2 PhRange = new Vector2(5.8f, 6.5f);
        public Vector2 EcRange = new Vector2(1.0f, 1.8f);

        [Header("Micronutrients")]
        [Range(0f, 2f)] public float CalciumRequirement = 1f;
        [Range(0f, 2f)] public float MagnesiumRequirement = 1f;
        [Range(0f, 2f)] public float SulfurRequirement = 1f;
        [Range(0f, 1f)] public float IronRequirement = 0.5f;

        [Header("Feeding Schedule")]
        [Range(0.5f, 3f)] public float FeedingFrequencyDays = 2f;
        [Range(0f, 2f)] public float NutrientUptakeRate = 1f;
    }

    [System.Serializable]
    public class WaterRequirements
    {
        [Header("Moisture Levels")]
        public Vector2 MoistureRange = new Vector2(0.4f, 0.8f);
        public Vector2 OptimalMoistureRange = new Vector2(0.5f, 0.7f);

        [Header("Drainage")]
        public Vector2 DrainageRange = new Vector2(0.3f, 0.8f);
        public float OptimalDrainageRate = 0.5f;

        [Header("Watering Schedule")]
        [Range(0.5f, 7f)] public float WateringFrequencyDays = 2f;
        [Range(0.1f, 5f)] public float WaterConsumptionLitersPerDay = 1f;

        [Header("Water Quality")]
        [Range(0f, 1f)] public float MinWaterQuality = 0.7f;
        [Range(0f, 1000f)] public float MaxTds = 200f; // Total Dissolved Solids (ppm)
    }

    [System.Serializable]
    public class ResourceConsumption
    {
        [Header("Daily Consumption Rates")]
        [Range(0f, 10f)] public float WaterLitersPerDay = 1f;
        [Range(0f, 5f)] public float NutrientGramsPerDay = 2f;
        [Range(0f, 100f)] public float Co2GramsPerDay = 10f;
        [Range(0f, 50f)] public float ElectricityKwhPerDay = 5f;

        [Header("Growth Multipliers")]
        [Range(0.1f, 3f)] public float ConsumptionGrowthMultiplier = 1f;
        [Range(0.5f, 2f)] public float EfficiencyRating = 1f;
    }

    [System.Serializable]
    public class VisualCharacteristics
    {
        [Header("Plant Structure")]
        [Range(0f, 2f)] public float RelativeHeight = 1f;
        [Range(0f, 2f)] public float RelativeWidth = 1f;
        [Range(0f, 10f)] public float LeafCount = 5f;
        [Range(0f, 20f)] public float BranchCount = 0f;

        [Header("Colors")]
        public Color LeafColor = Color.green;
        public Color StemColor = new Color(0.4f, 0.6f, 0.2f);
        public Color FlowerColor = Color.white;

        [Header("Textures")]
        [Range(0f, 1f)] public float LeafGlossiness = 0.3f;
        [Range(0f, 1f)] public float TrichomeDensity = 0f;
        [Range(0f, 1f)] public float ResinProduction = 0f;

        [Header("Special Effects")]
        public bool ShowFlowers = false;
        public bool ShowTrichomes = false;
        public bool ShowRoots = false;
        public bool EmitsAroma = false;
    }



    public enum TransitionTrigger
    {
        TimeElapsed,
        PhotoperiodChange,
        SizeThreshold,
        EnvironmentalTrigger,
        ManualTrigger,
        NutrientThreshold,
        StressThreshold
    }
}