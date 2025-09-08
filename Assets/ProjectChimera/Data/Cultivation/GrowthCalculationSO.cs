using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// ScriptableObject that encapsulates complex plant growth calculations.
    /// Provides configurable growth models for different cultivation approaches and environmental conditions.
    /// </summary>
    [CreateAssetMenu(fileName = "New Growth Calculation", menuName = "Project Chimera/Cultivation/Growth Calculation")]
    public class GrowthCalculationSO : ChimeraConfigSO
    {
        [Header("Base Growth Parameters")]
        [SerializeField, Range(0.1f, 10f)] private float _baseGrowthMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float _environmentalImpactFactor = 1.2f;
        [SerializeField, Range(0f, 2f)] private float _geneticVarianceFactor = 0.8f;
        
        [Header("Environmental Response Curves")]
        [SerializeField] private AnimationCurve _temperatureResponseCurve = AnimationCurve.EaseInOut(15f, 0.2f, 28f, 1f);
        [SerializeField] private AnimationCurve _humidityResponseCurve = AnimationCurve.EaseInOut(30f, 0.3f, 70f, 1f);
        [SerializeField] private AnimationCurve _lightResponseCurve = AnimationCurve.EaseInOut(100f, 0.1f, 800f, 1f);
        [SerializeField] private AnimationCurve _co2ResponseCurve = AnimationCurve.EaseInOut(300f, 0.5f, 1200f, 1f);
        [SerializeField] private AnimationCurve _pHResponseCurve = AnimationCurve.EaseInOut(5.5f, 0.3f, 7f, 1f);
        [SerializeField] private AnimationCurve _nutrientResponseCurve = AnimationCurve.EaseInOut(0.2f, 0.4f, 0.9f, 1f);
        [SerializeField] private AnimationCurve _waterResponseCurve = AnimationCurve.EaseInOut(0.3f, 0.2f, 0.8f, 1f);
        
        [Header("Growth Stage Modifiers")]
        [SerializeField, Range(0f, 2f)] private float _seedGrowthModifier = 0.1f;
        [SerializeField, Range(0f, 2f)] private float _germinationGrowthModifier = 0.3f;
        [SerializeField, Range(0f, 2f)] private float _seedlingGrowthModifier = 0.8f;
        [SerializeField, Range(0f, 2f)] private float _vegetativeGrowthModifier = 1.0f;
        [SerializeField, Range(0f, 2f)] private float _preFloweringGrowthModifier = 1.2f;
        [SerializeField, Range(0f, 2f)] private float _floweringGrowthModifier = 0.6f;
        [SerializeField, Range(0f, 2f)] private float _ripeningGrowthModifier = 0.2f;
        
        [Header("Stress Impact")]
        [SerializeField] private AnimationCurve _stressImpactCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f);
        [SerializeField, Range(0f, 5f)] private float _stressRecoveryRate = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _maxStressTolerance = 0.8f;
        
        [Header("Health Impact")]
        [SerializeField] private AnimationCurve _healthImpactCurve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);
        [SerializeField, Range(0f, 2f)] private float _vigorImpactFactor = 1.3f;
        [SerializeField, Range(0f, 1f)] private float _immuneSystemBonus = 0.15f;
        
        [Header("Resource Consumption")]
        [SerializeField] private AnimationCurve _waterConsumptionCurve = AnimationCurve.Linear(0f, 0.05f, 100f, 0.3f);
        [SerializeField] private AnimationCurve _nutrientConsumptionCurve = AnimationCurve.Linear(0f, 0.02f, 100f, 0.2f);
        [SerializeField, Range(0f, 2f)] private float _energyConsumptionRate = 1.0f;
        
        [Header("Advanced Calculations")]
        [SerializeField] private bool _enablePhotoperiodResponse = true;
        [SerializeField] private bool _enableDailyLightIntegral = true;
        [SerializeField] private bool _enableVaporPressureDeficit = true;
        [SerializeField] private bool _enableCO2Compensation = true;
        
        /// <summary>
        /// Calculates the overall growth rate for a plant based on all environmental and genetic factors.
        /// </summary>
        public float CalculateGrowthRate(PlantInstanceSO plant, EnvironmentalConditions environment)
        {
            if (plant == null) return 0f;
            
            // Base growth rate from genetics
            float geneticGrowthRate = GetGeneticGrowthRate(plant);
            
            // Environmental impact
            float environmentalMultiplier = CalculateEnvironmentalMultiplier(environment);
            
            // Growth stage modifier
            float stageModifier = GetGrowthStageModifier(plant.CurrentGrowthStage);
            
            // Health and stress impact
            float healthModifier = CalculateHealthModifier(plant);
            float stressModifier = CalculateStressModifier(plant.StressLevel);
            
            // Resource availability impact
            float resourceModifier = CalculateResourceModifier(plant);
            
            // Advanced environmental calculations
            float advancedModifier = CalculateAdvancedModifiers(environment, plant);
            
            // Final growth rate calculation
            float finalGrowthRate = geneticGrowthRate * 
                                  environmentalMultiplier * 
                                  stageModifier * 
                                  healthModifier * 
                                  stressModifier * 
                                  resourceModifier * 
                                  advancedModifier * 
                                  _baseGrowthMultiplier;
            
            return Mathf.Max(0f, finalGrowthRate);
        }
        
        /// <summary>
        /// Calculates daily water consumption for a plant.
        /// </summary>
        public float CalculateWaterConsumption(PlantInstanceSO plant, EnvironmentalConditions environment)
        {
            if (plant == null) return 0f;
            
            float baseConsumption = _waterConsumptionCurve.Evaluate(plant.CurrentHeight);
            
            // Temperature factor (higher temp = more transpiration)
            float tempFactor = 1f + (environment.Temperature - 20f) * 0.03f;
            
            // Humidity factor (lower humidity = more transpiration)
            float humidityFactor = 1f + (60f - environment.Humidity) * 0.01f;
            
            // Light factor (more light = more photosynthesis = more water use)
            float lightFactor = 1f + (environment.LightIntensity / 800f) * 0.2f;
            
            // Leaf area factor
            float leafAreaFactor = 1f + (plant.LeafArea / 100f) * 0.1f;
            
            // Growth stage factor
            float stageMultiplier = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Seed => 0.1f,
                PlantGrowthStage.Germination => 0.2f,
                PlantGrowthStage.Seedling => 0.5f,
                PlantGrowthStage.Vegetative => 1.0f,
                PlantGrowthStage.PreFlowering => 1.2f,
                PlantGrowthStage.Flowering => 1.4f,
                PlantGrowthStage.Ripening => 0.8f,
                _ => 0.3f
            };
            
            return baseConsumption * tempFactor * humidityFactor * lightFactor * leafAreaFactor * stageMultiplier;
        }
        
        /// <summary>
        /// Calculates daily nutrient consumption for a plant.
        /// </summary>
        public float CalculateNutrientConsumption(PlantInstanceSO plant, EnvironmentalConditions environment)
        {
            if (plant == null) return 0f;
            
            float baseConsumption = _nutrientConsumptionCurve.Evaluate(plant.CurrentHeight);
            
            // Growth rate factor (faster growth = more nutrients needed)
            float growthFactor = 1f + plant.DailyGrowthRate * 0.5f;
            
            // Health factor (healthier plants use nutrients more efficiently)
            float healthFactor = Mathf.Lerp(1.2f, 0.8f, plant.OverallHealth);
            
            // Stress factor (stressed plants may consume more or less efficiently)
            float stressFactor = Mathf.Lerp(1f, 1.3f, plant.StressLevel);
            
            // Growth stage factor
            float stageMultiplier = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Seed => 0.05f,
                PlantGrowthStage.Germination => 0.1f,
                PlantGrowthStage.Seedling => 0.6f,
                PlantGrowthStage.Vegetative => 1.0f,
                PlantGrowthStage.PreFlowering => 1.1f,
                PlantGrowthStage.Flowering => 1.3f,
                PlantGrowthStage.Ripening => 0.4f,
                _ => 0.2f
            };
            
            return baseConsumption * growthFactor * healthFactor * stressFactor * stageMultiplier;
        }
        
        /// <summary>
        /// Calculates energy consumption rate for growth and maintenance.
        /// </summary>
        public float CalculateEnergyConsumption(PlantInstanceSO plant, EnvironmentalConditions environment)
        {
            if (plant == null) return 0f;
            
            // Base energy consumption for maintenance
            float maintenanceEnergy = plant.CurrentHeight * 0.001f;
            
            // Growth energy consumption
            float growthEnergy = plant.DailyGrowthRate * 0.02f;
            
            // Stress response energy
            float stressEnergy = plant.StressLevel * 0.05f;
            
            // Immune response energy
            float immuneEnergy = plant.ImmuneResponse * 0.01f;
            
            // Temperature stress energy
            float tempStress = Mathf.Abs(environment.Temperature - 24f) * 0.003f;
            
            return (maintenanceEnergy + growthEnergy + stressEnergy + immuneEnergy + tempStress) * _energyConsumptionRate;
        }
        
        /// <summary>
        /// Calculates health change rate based on environmental and internal factors.
        /// </summary>
        public float CalculateHealthChange(PlantInstanceSO plant, EnvironmentalConditions environment)
        {
            if (plant == null) return 0f;
            
            // Environmental health impact
            float environmentalHealth = CalculateEnvironmentalHealthImpact(environment);
            
            // Resource availability impact
            float resourceHealth = (plant.WaterLevel + plant.NutrientLevel + plant.EnergyReserves) / 3f;
            
            // Stress impact on health
            float stressImpact = -plant.StressLevel * 0.1f;
            
            // Immune system contribution
            float immuneContribution = plant.ImmuneResponse * _immuneSystemBonus;
            
            // Age factor (very old plants may decline)
            float ageFactor = plant.AgeInDays > 150f ? -0.005f : 0f;
            
            // Recovery bonus for optimal conditions
            float recoveryBonus = (environmentalHealth > 0.8f && resourceHealth > 0.8f) ? 0.05f : 0f;
            
            return environmentalHealth * 0.3f + 
                   resourceHealth * 0.4f + 
                   stressImpact + 
                   immuneContribution + 
                   ageFactor + 
                   recoveryBonus;
        }
        
        /// <summary>
        /// Calculates yield potential based on current plant state and growth history.
        /// </summary>
        public float CalculateYieldPotential(PlantInstanceSO plant)
        {
            if (plant == null || plant.Genotype == null) return 0f;
            
            float geneticYield = plant.Genotype.GetYieldPotential();
            
            // Health impact on yield
            float healthModifier = _healthImpactCurve.Evaluate(plant.OverallHealth);
            
            // Stress history impact
            float stressHistoryModifier = 1f - (plant.CumulativeStressDays / Mathf.Max(1f, plant.AgeInDays)) * 0.5f;
            
            // Optimal days bonus
            float optimalBonus = (plant.OptimalDays / Mathf.Max(1f, plant.AgeInDays)) * 0.3f;
            
            // Plant size factor
            float sizeModifier = Mathf.Sqrt(plant.CurrentHeight / 100f); // Height in meters
            
            // Vigor contribution
            float vigorModifier = Mathf.Lerp(0.7f, 1.2f, plant.Vigor);
            
            return geneticYield * healthModifier * stressHistoryModifier * (1f + optimalBonus) * sizeModifier * vigorModifier;
        }
        
        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();
            
            if (_baseGrowthMultiplier <= 0f)
            {
                SharedLogger.LogWarning($"GrowthCalculationSO '{name}' has invalid base growth multiplier: {_baseGrowthMultiplier}", this);
                _baseGrowthMultiplier = 1f;
                isValid = false;
            }
            
            // Validate curves have proper key ranges
            ValidateCurve(_temperatureResponseCurve, "Temperature Response", 0f, 50f);
            ValidateCurve(_humidityResponseCurve, "Humidity Response", 0f, 100f);
            ValidateCurve(_lightResponseCurve, "Light Response", 0f, 1200f);
            ValidateCurve(_co2ResponseCurve, "CO2 Response", 200f, 2000f);
            ValidateCurve(_pHResponseCurve, "pH Response", 4f, 9f);
            ValidateCurve(_nutrientResponseCurve, "Nutrient Response", 0f, 1f);
            ValidateCurve(_waterResponseCurve, "Water Response", 0f, 1f);
            
            return isValid;
        }
        
        private float GetGeneticGrowthRate(PlantInstanceSO plant)
        {
            if (plant.Genotype == null) return 0.5f;
            
            float baseGeneticRate = plant.Genotype.GetGrowthPotential();
            float variance = Random.Range(-_geneticVarianceFactor, _geneticVarianceFactor) * 0.1f;
            
            return Mathf.Clamp01(baseGeneticRate + variance);
        }
        
        private float CalculateEnvironmentalMultiplier(EnvironmentalConditions environment)
        {
            float tempResponse = _temperatureResponseCurve.Evaluate(environment.Temperature);
            float humidityResponse = _humidityResponseCurve.Evaluate(environment.Humidity);
            float lightResponse = _lightResponseCurve.Evaluate(environment.LightIntensity);
            float co2Response = _co2ResponseCurve.Evaluate(environment.CO2Level);
            float pHResponse = _pHResponseCurve.Evaluate(environment.pH);
            
            // Weighted average of environmental factors
            float environmentalScore = (tempResponse * 0.25f + 
                                      humidityResponse * 0.15f + 
                                      lightResponse * 0.30f + 
                                      co2Response * 0.15f + 
                                      pHResponse * 0.15f);
            
            return Mathf.Lerp(1f, environmentalScore, _environmentalImpactFactor);
        }
        
        private float GetGrowthStageModifier(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Seed => _seedGrowthModifier,
                PlantGrowthStage.Germination => _germinationGrowthModifier,
                PlantGrowthStage.Seedling => _seedlingGrowthModifier,
                PlantGrowthStage.Vegetative => _vegetativeGrowthModifier,
                PlantGrowthStage.PreFlowering => _preFloweringGrowthModifier,
                PlantGrowthStage.Flowering => _floweringGrowthModifier,
                PlantGrowthStage.Ripening => _ripeningGrowthModifier,
                PlantGrowthStage.Harvest => 0f,
                _ => 0.5f
            };
        }
        
        private float CalculateHealthModifier(PlantInstanceSO plant)
        {
            float healthResponse = _healthImpactCurve.Evaluate(plant.OverallHealth);
            float vigorBonus = plant.Vigor * _vigorImpactFactor;
            float immuneBonus = plant.ImmuneResponse * _immuneSystemBonus;
            
            return healthResponse * (1f + vigorBonus * 0.3f + immuneBonus);
        }
        
        private float CalculateStressModifier(float stressLevel)
        {
            float stressResponse = _stressImpactCurve.Evaluate(stressLevel);
            
            // Some stress can actually be beneficial (hormesis effect)
            if (stressLevel > 0f && stressLevel < 0.3f)
            {
                stressResponse = Mathf.Max(stressResponse, 1.05f); // Slight growth boost
            }
            
            return stressResponse;
        }
        
        private float CalculateResourceModifier(PlantInstanceSO plant)
        {
            float waterResponse = _waterResponseCurve.Evaluate(plant.WaterLevel);
            float nutrientResponse = _nutrientResponseCurve.Evaluate(plant.NutrientLevel);
            float energyResponse = Mathf.Clamp01(plant.EnergyReserves);
            
            // Resources have diminishing returns but severe penalties for deficiency
            return (waterResponse * 0.4f + nutrientResponse * 0.4f + energyResponse * 0.2f);
        }
        
        private float CalculateAdvancedModifiers(EnvironmentalConditions environment, PlantInstanceSO plant)
        {
            float modifier = 1f;
            
            if (_enablePhotoperiodResponse)
            {
                // Photoperiod affects flowering plants differently
                if (plant.CurrentGrowthStage == PlantGrowthStage.Flowering || 
                    plant.CurrentGrowthStage == PlantGrowthStage.PreFlowering)
                {
                    // Cannabis is typically short-day plant for flowering
                    float photoperiodModifier = environment.PhotoperiodHours < 14f ? 1.1f : 0.9f;
                    modifier *= photoperiodModifier;
                }
            }
            
            if (_enableDailyLightIntegral)
            {
                // DLI = PPFD × photoperiod × 0.0036
                float dli = environment.LightIntensity * environment.PhotoperiodHours * 0.0036f;
                float dliModifier = Mathf.Clamp(dli / 40f, 0.5f, 1.2f); // Optimal DLI around 40 mol/m²/day
                modifier *= dliModifier;
            }
            
            if (_enableVaporPressureDeficit)
            {
                // Simplified VPD calculation
                float vpd = CalculateVPD(environment.Temperature, environment.Humidity);
                float vpdModifier = vpd > 0.8f && vpd < 1.5f ? 1.1f : 0.9f; // Optimal VPD range
                modifier *= vpdModifier;
            }
            
            if (_enableCO2Compensation)
            {
                // CO2 compensation for high light conditions
                if (environment.LightIntensity > 600f && environment.CO2Level > 800f)
                {
                    modifier *= 1.15f; // Bonus for high CO2 + high light
                }
            }
            
            return modifier;
        }
        
        private float CalculateEnvironmentalHealthImpact(EnvironmentalConditions environment)
        {
            // Calculate how environmental conditions affect health
            float suitability = environment.CalculateOverallSuitability();
            
            // Convert suitability to health impact (-0.1 to +0.1 per day)
            return (suitability - 0.5f) * 0.2f;
        }
        
        private float CalculateVPD(float temperature, float humidity)
        {
            // Simplified VPD calculation
            float saturationPressure = 0.6108f * Mathf.Exp((17.27f * temperature) / (temperature + 237.3f));
            float actualPressure = saturationPressure * (humidity / 100f);
            return saturationPressure - actualPressure;
        }
        
        private void ValidateCurve(AnimationCurve curve, string curveName, float minValue, float maxValue)
        {
            if (curve == null || curve.keys.Length == 0)
            {
                SharedLogger.LogWarning($"GrowthCalculationSO '{name}' has invalid {curveName} curve.", this);
                return;
            }
            
            var firstKey = curve.keys[0];
            var lastKey = curve.keys[curve.keys.Length - 1];
            
            if (firstKey.time < minValue || lastKey.time > maxValue)
            {
                SharedLogger.LogWarning($"GrowthCalculationSO '{name}' {curveName} curve range [{firstKey.time:F1}, {lastKey.time:F1}] outside expected range [{minValue:F1}, {maxValue:F1}].", this);
            }
        }
    }
}