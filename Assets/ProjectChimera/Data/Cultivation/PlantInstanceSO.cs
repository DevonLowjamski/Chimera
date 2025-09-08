using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Genetics;
using System;
using ProjectChimera.Data.Shared;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;


namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Represents an individual plant instance with its current state, health, and cultivation data.
    /// This is the core data structure for tracking individual plants throughout their lifecycle.
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Instance", menuName = "Project Chimera/Cultivation/Plant Instance")]
    public class PlantInstanceSO : ChimeraDataSO
    {
        [Header("Plant Identity")]
        [SerializeField] private string _plantID;
        [SerializeField] private string _plantName;
        [SerializeField] private PlantStrainSO _strain;
        [SerializeField] private GenotypeDataSO _genotype;

        [Header("Current State")]
        [SerializeField] private PlantGrowthStage _currentGrowthStage;
        [SerializeField] private float _ageInDays;
        [SerializeField] private float _daysInCurrentStage;
        [SerializeField] private Vector3 _worldPosition;

        [Header("Physical Characteristics")]
        [SerializeField, Range(0f, 500f)] private float _currentHeight; // cm
        [SerializeField, Range(0f, 200f)] private float _currentWidth; // cm
        [SerializeField, Range(0f, 100f)] private float _rootMassPercentage; // % of total plant mass
        [SerializeField, Range(0f, 1000f)] private float _leafArea; // cm²

        [Header("Health and Vitality")]
        [SerializeField, Range(0f, 1f)] private float _overallHealth;
        [SerializeField, Range(0f, 1f)] private float _vigor; // Growth energy
        [SerializeField, Range(0f, 1f)] private float _stressLevel;
        [SerializeField, Range(0f, 1f)] private float _immuneResponse; // Disease/pest resistance
        [SerializeField, Range(0f, 1f)] private float _maturityLevel; // Overall plant maturity for breeding

        [Header("Resource Status")]
        [SerializeField, Range(0f, 1f)] private float _waterLevel; // Current hydration
        [SerializeField, Range(0f, 1f)] private float _nutrientLevel; // Current nutrient status
        [SerializeField, Range(0f, 1f)] private float _energyReserves; // Stored energy for growth

        [Header("Growth Metrics")]
        [SerializeField] private float _dailyGrowthRate; // cm/day
        [SerializeField] private float _biomassAccumulation; // g/day
        [SerializeField] private float _rootDevelopmentRate; // Root expansion rate
        [SerializeField, Range(0f, 2f)] private float _growthProgress = 0f; // 0 = just started stage, 1 = ready for next stage
        [SerializeField] private int _daysSincePlanted = 0; // Days since plant was started

        [Header("Environmental History")]
        [SerializeField] private EnvironmentalConditions _currentEnvironment;
        [SerializeField] private float _cumulativeStressDays; // Total stress exposure
        [SerializeField] private float _optimalDays; // Days in optimal conditions

        [Header("Cultivation Events")]
        [SerializeField] private System.DateTime _plantedDate;
        [SerializeField] private System.DateTime _lastWatering;
        [SerializeField] private System.DateTime _lastFeeding;
        [SerializeField] private System.DateTime _lastTraining; // LST, topping, etc.

        [Header("Genetic Expression")]
        [SerializeField] private float _calculatedMaxHeight; // Genetically determined max height
        [SerializeField] private float _lastTraitCalculationAge; // Age when traits were last calculated

        // Non-serialized runtime references
        // TraitExpressionEngine removed to avoid circular dependency - will be handled by Systems layer
        private TraitExpressionResult _lastTraitExpression;

        // Properties for external access
        public string PlantID => _plantID;
        public string PlantName => _plantName;
        public PlantStrainSO Strain => _strain;
        public GenotypeDataSO Genotype => _genotype;
        public PlantGrowthStage CurrentGrowthStage
        {
            get => _currentGrowthStage;
            set => _currentGrowthStage = value;
        }
        public float AgeInDays => _ageInDays;
        public float DaysInCurrentStage => _daysInCurrentStage;
        public Vector3 WorldPosition => _worldPosition;
        public float CurrentHeight => _currentHeight;
        public float CurrentWidth => _currentWidth;
        public float RootMassPercentage => _rootMassPercentage;
        public float LeafArea => _leafArea;
        public float OverallHealth => _overallHealth;
        public float CurrentHealth
        {
            get => _overallHealth;
            set => _overallHealth = Mathf.Clamp01(value);
        } // Alias for compatibility
        public float CurrentGrowthProgress
        {
            get => _maturityLevel;
            set => _maturityLevel = Mathf.Clamp01(value);
        } // Growth progress based on maturity
        public float MaturityLevel => _maturityLevel;
        public float Vigor => _vigor;
        public float StressLevel => _stressLevel;
        public float ImmuneResponse => _immuneResponse;
        public float WaterLevel
        {
            get => _waterLevel;
            set => _waterLevel = Mathf.Clamp01(value);
        }
        public float NutrientLevel
        {
            get => _nutrientLevel;
            set => _nutrientLevel = Mathf.Clamp01(value);
        }
        public float EnergyReserves => _energyReserves;
        public float DailyGrowthRate => _dailyGrowthRate;
        public float BiomassAccumulation => _biomassAccumulation;
        public float GrowthProgress => _growthProgress;
        public string PlantInstanceId => _plantID;
        public string PlantId => _plantID;
        public bool RequiresTraining => _stressLevel > 0.7f || _vigor < 0.3f;
        public int DaysSincePlanted
        {
            get => _daysSincePlanted;
            set => _daysSincePlanted = value;
        }

        public float RootDevelopmentRate => _rootDevelopmentRate;
        public EnvironmentalConditions CurrentEnvironment => _currentEnvironment;
        public float CumulativeStressDays => _cumulativeStressDays;
        public float OptimalDays => _optimalDays;
        public System.DateTime PlantedDate => _plantedDate;
        public System.DateTime LastWatering => _lastWatering;
        public System.DateTime LastFeeding => _lastFeeding;
        public System.DateTime LastTraining => _lastTraining;
        public float CalculatedMaxHeight => _calculatedMaxHeight;
        public TraitExpressionResult LastTraitExpression => _lastTraitExpression;

        // Additional properties for compatibility with cultivation system
        public bool IsActive => _overallHealth > 0f && _currentGrowthStage != PlantGrowthStage.Dormant;

        /// <summary>
        /// Initializes a new plant instance with the specified strain and genotype.
        /// </summary>
        public void InitializePlant(string plantID, string plantName, PlantStrainSO strain, GenotypeDataSO genotype, Vector3 worldPosition)
        {
            _plantID = plantID;
            _plantName = plantName;
            _strain = strain;
            _genotype = genotype;
            _worldPosition = worldPosition;

            // Initialize starting values
            _currentGrowthStage = PlantGrowthStage.Seed;
            _ageInDays = 0f;
            _daysInCurrentStage = 0f;

            // Starting physical characteristics
            _currentHeight = 0.1f; // 1mm
            _currentWidth = 0.1f;
            _rootMassPercentage = 20f; // Seeds start with significant root potential
            _leafArea = 0f;

            // Starting health and vitality
            _overallHealth = 1f; // Seeds start healthy
            _vigor = 0.8f; // High starting vigor
            _stressLevel = 0f;
            _immuneResponse = 0.5f; // Moderate starting immunity

            // Starting resource status
            _waterLevel = 0.8f; // Well hydrated seed
            _nutrientLevel = 0.7f; // Seeds have stored nutrients
            _energyReserves = 1f; // Full energy reserves

            // Initialize growth metrics
            _dailyGrowthRate = 0f;
            _biomassAccumulation = 0f;
            _rootDevelopmentRate = 0f;

            // Initialize environmental data
            _currentEnvironment = EnvironmentalConditions.CreateIndoorDefault();
            _cumulativeStressDays = 0f;
            _optimalDays = 0f;

            // Set cultivation dates
            _plantedDate = System.DateTime.Now;
            _lastWatering = System.DateTime.Now;
            _lastFeeding = System.DateTime.Now.AddDays(-1); // Last fed yesterday
            _lastTraining = System.DateTime.MinValue; // Never trained

            // Initialize genetic expression system
            // TraitExpressionEngine initialization moved to Systems layer
            _calculatedMaxHeight = 0f; // Will be calculated on first growth update
            _lastTraitCalculationAge = -1f; // Force initial calculation
        }

        /// <summary>
        /// Updates the plant's daily growth and development based on environmental conditions.
        /// </summary>
        public void ProcessDailyGrowth(EnvironmentalConditions environment, float timeMultiplier = 1f)
        {
            _currentEnvironment = environment;
            _ageInDays += timeMultiplier;
            _daysInCurrentStage += timeMultiplier;

            // Calculate environmental suitability
            float environmentalSuitability = environment.CalculateOverallSuitability();
            float environmentalStress = environment.CalculateEnvironmentalStress();

            // Update stress tracking
            if (environmentalStress > 0.3f)
            {
                _cumulativeStressDays += timeMultiplier;
            }
            else if (environmentalSuitability > 0.8f)
            {
                _optimalDays += timeMultiplier;
            }

            // Calculate base growth rate from genetics and environment
            float geneticGrowthPotential = _genotype != null ? _genotype.GetGrowthPotential() : 0.5f;
            float baseGrowthRate = geneticGrowthPotential * environmentalSuitability * _vigor;

            // Apply growth stage modifiers
            float stageModifier = GetGrowthStageModifier();
            _dailyGrowthRate = baseGrowthRate * stageModifier * timeMultiplier;

            // Update genetic trait expression (Phase 0.2: Core Genetic Engine integration)
            UpdateGeneticTraitExpression(environment);

            // Update physical characteristics
            UpdatePhysicalGrowth(timeMultiplier);

            // Update health and vitality
            UpdateHealthAndVitality(environmentalStress, timeMultiplier);

            // Update resource consumption
            UpdateResourceConsumption(timeMultiplier);

            // Check for growth stage transition
            CheckStageTransition();
        }

        /// <summary>
        /// Sets the current growth stage of the plant.
        /// </summary>
        public void SetGrowthStage(PlantGrowthStage newStage)
        {
            _currentGrowthStage = newStage;
            _daysInCurrentStage = 0f; // Reset stage timer
        }

        /// <summary>
        /// Updates plant's water level and watering timestamp.
        /// </summary>
        public void Water(float waterAmount, System.DateTime wateringTime)
        {
            _waterLevel = Mathf.Clamp01(_waterLevel + waterAmount);
            _lastWatering = wateringTime;
        }

        /// <summary>
        /// Updates plant's nutrient level and feeding timestamp.
        /// </summary>
        public void Feed(float nutrientAmount, System.DateTime feedingTime)
        {
            _nutrientLevel = Mathf.Clamp01(_nutrientLevel + nutrientAmount);
            _lastFeeding = feedingTime;
        }

        /// <summary>
        /// Applies training techniques (LST, topping, etc.) to the plant.
        /// </summary>
        public void ApplyTraining(string trainingType, System.DateTime trainingTime)
        {
            _lastTraining = trainingTime;

            // Apply training effects based on type
            switch (trainingType.ToLower())
            {
                case "lst": // Low Stress Training
                    _stressLevel += 0.1f;
                    break;
                case "topping":
                    _stressLevel += 0.2f;
                    _currentHeight *= 0.85f; // Reduces height temporarily
                    break;
                case "defoliation":
                    _stressLevel += 0.15f;
                    _leafArea *= 0.7f; // Reduces leaf area
                    break;
            }

            _stressLevel = Mathf.Clamp01(_stressLevel);
        }

        /// <summary>
        /// Calculates the plant's current yield potential based on current state.
        /// </summary>
        public float CalculateYieldPotential()
        {
            if (_strain == null || _genotype == null) return 0f;

            float geneticYieldPotential = _genotype.GetYieldPotential();
            float healthModifier = _overallHealth;
            float environmentalModifier = _optimalDays / Mathf.Max(1f, _ageInDays);
            float stressModifier = 1f - (_cumulativeStressDays / Mathf.Max(1f, _ageInDays));

            return geneticYieldPotential * healthModifier * environmentalModifier * stressModifier;
        }

        /// <summary>
        /// Gets the current potency potential based on genetics and environmental factors.
        /// </summary>
        public float CalculatePotencyPotential()
        {
            if (_strain == null || _genotype == null) return 0f;

            float geneticPotency = _genotype.GetPotencyPotential();
            float stressBonus = Mathf.Clamp01(_cumulativeStressDays * 0.1f); // Light stress can increase potency
            float healthModifier = _overallHealth;

            return geneticPotency * healthModifier * (1f + stressBonus);
        }

        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;

            if (string.IsNullOrEmpty(_plantID))
            {
                SharedLogger.LogWarning($"PlantInstanceSO '{name}' has no Plant ID assigned.", this);
                isValid = false;
            }

            if (_strain == null)
            {
                SharedLogger.LogWarning($"PlantInstanceSO '{name}' has no strain assigned.", this);
                isValid = false;
            }

            if (_genotype == null)
            {
                SharedLogger.LogWarning($"PlantInstanceSO '{name}' has no genotype assigned.", this);
                isValid = false;
            }

            if (_ageInDays < 0f)
            {
                SharedLogger.LogWarning($"PlantInstanceSO '{name}' has negative age: {_ageInDays}", this);
                _ageInDays = 0f;
                isValid = false;
            }

            return isValid;
        }

        private float GetGrowthStageModifier()
        {
            return _currentGrowthStage switch
            {
                PlantGrowthStage.Seed => 0.1f,
                PlantGrowthStage.Germination => 0.3f,
                PlantGrowthStage.Seedling => 0.8f,
                PlantGrowthStage.Vegetative => 1.0f,
                PlantGrowthStage.PreFlowering => 1.2f,
                PlantGrowthStage.Flowering => 0.6f,
                PlantGrowthStage.Ripening => 0.2f,
                PlantGrowthStage.Harvest => 0f,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Calculate genetic traits using the TraitExpressionEngine.
        /// Phase 0.2: Integration with genetic expression system.
        /// </summary>
        private void UpdateGeneticTraitExpression(EnvironmentalConditions environment)
        {
            // Only recalculate if enough time has passed or if never calculated
            bool shouldRecalculate = _lastTraitCalculationAge < 0f ||
                                   (_ageInDays - _lastTraitCalculationAge) >= 1f || // Recalculate daily
                                   _calculatedMaxHeight == 0f;

            if (!shouldRecalculate || _genotype == null)
                return;

            try
            {
                // Check if we have the correct genotype type for trait calculation
                if (_genotype is GenotypeDataSO genotypeData)
                {
                    // For now, create a basic PlantGenotype from GenotypeDataSO
                    // In future iterations, this could be a direct conversion method
                    var plantGenotype = CreatePlantGenotypeFromData(genotypeData);

                    if (plantGenotype != null)
                    {
                        // Calculate trait expression using the genetic engine
                        // Trait expression calculation moved to Systems layer
                        _lastTraitExpression = new TraitExpressionResult { GenotypeID = _genotype.GenotypeID, OverallFitness = 1.0f };

                        // Update calculated max height from genetic expression
                        if (_lastTraitExpression != null)
                        {
                            _calculatedMaxHeight = _lastTraitExpression.HeightExpression * 100f; // Convert to cm
                            _lastTraitCalculationAge = _ageInDays;

                            SharedLogger.Log($"Plant {_plantName}: Calculated max height {_calculatedMaxHeight:F1}cm from genetics " +
                                     $"(Fitness: {_lastTraitExpression.OverallFitness:F2})");
                        }
                    }
                }
                else
                {
                    // Fallback for when genetic data is not in expected format
                    _calculatedMaxHeight = CalculateFallbackMaxHeight();
                    _lastTraitCalculationAge = _ageInDays;
                }
            }
            catch (System.Exception ex)
            {
                SharedLogger.LogWarning($"Plant {_plantName}: Error calculating genetic traits - {ex.Message}. Using fallback.");
                _calculatedMaxHeight = CalculateFallbackMaxHeight();
                _lastTraitCalculationAge = _ageInDays;
            }
        }

        /// <summary>
        /// Create a PlantGenotype from GenotypeDataSO for trait calculation.
        /// Temporary bridge method until unified genotype system is implemented.
        /// </summary>
        private PlantGenotype CreatePlantGenotypeFromData(GenotypeDataSO genotypeData)
        {
            try
            {
                var plantGenotype = new PlantGenotype
                {
                    GenotypeID = genotypeData.IndividualID ?? "Unknown",
                    StrainOrigin = _strain != null ?
                                   ConvertToGeneticsStrain(_strain) :
                                   CreateDefaultGeneticsStrain(),
                    Generation = 0,
                    IsFounder = true,
                    CreationDate = _plantedDate,
                    InbreedingCoefficient = 0.0f
                };

                // Convert GenePairs to Dictionary format expected by PlantGenotype
                if (genotypeData.GenePairs != null)
                {
                    foreach (var genePair in genotypeData.GenePairs)
                    {
                        if (genePair?.Gene != null)
                        {
                            var alleleCouple = new AlleleCouple(genePair.Allele1?.name ?? "unknown", genePair.Allele2?.name ?? "unknown");
                            plantGenotype.Genotype[genePair.Gene.GeneCode ?? genePair.Gene.GeneName] = alleleCouple;
                        }
                    }
                }

                return plantGenotype;
            }
            catch (System.Exception ex)
            {
                SharedLogger.LogError($"Error creating PlantGenotype from GenotypeDataSO: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculate fallback max height when genetic calculation fails.
        /// </summary>
        private float CalculateFallbackMaxHeight()
        {
            // Use strain-based estimation if available
            if (_strain != null)
            {
                var strainName = _strain.name?.ToLower();
                if (!string.IsNullOrEmpty(strainName))
                {
                    if (strainName.Contains("indica"))
                        return 120f; // 120cm average for Indica
                    else if (strainName.Contains("sativa"))
                        return 200f; // 200cm average for Sativa
                    else if (strainName.Contains("auto"))
                        return 80f; // 80cm average for Autoflower
                }
            }

            return 150f; // Default fallback height
        }

        /// <summary>
        /// Update physical growth using genetic trait expression for height limits.
        /// Phase 0.2: Refactored to use TraitExpressionEngine results.
        /// </summary>
        private void UpdatePhysicalGrowth(float timeMultiplier)
        {
            // Get genetically determined max height (fallback to strain-based if not calculated)
            float targetMaxHeight = _calculatedMaxHeight > 0f ? _calculatedMaxHeight : CalculateFallbackMaxHeight();

            // Calculate height growth with genetic constraints
            float baseHeightGrowth = _dailyGrowthRate * timeMultiplier;

            // Apply growth curve based on progress toward genetic maximum
            float heightProgress = _currentHeight / targetMaxHeight;
            float growthRateModifier = CalculateGrowthCurveModifier(heightProgress);

            // Apply the growth rate modifier to slow growth as plant approaches max height
            float adjustedHeightGrowth = baseHeightGrowth * growthRateModifier;

            // Prevent height from exceeding genetic maximum
            float newHeight = _currentHeight + adjustedHeightGrowth;
            _currentHeight = Mathf.Min(newHeight, targetMaxHeight);

            // Width follows height with genetic ratio
            float geneticWidthRatio = _genotype != null ? _genotype.GetWidthToHeightRatio() : 0.6f;
            _currentWidth = _currentHeight * geneticWidthRatio;

            // Root mass development based on genetic factors
            UpdateRootDevelopment(timeMultiplier);

            // Leaf area development with genetic and environmental influences
            UpdateLeafAreaDevelopment(adjustedHeightGrowth, timeMultiplier);

            // Update biomass accumulation based on actual growth achieved
            float actualGrowthAchieved = adjustedHeightGrowth / baseHeightGrowth; // Growth efficiency
            _biomassAccumulation = _dailyGrowthRate * actualGrowthAchieved * timeMultiplier;

            // Log genetic-based growth if in debug mode
            if (Debug.isDebugBuild && _calculatedMaxHeight > 0f)
            {
                SharedLogger.Log($"Plant {_plantName}: Height {_currentHeight:F1}cm/{targetMaxHeight:F1}cm " +
                         $"(Progress: {heightProgress * 100f:F1}%, Growth Rate: {growthRateModifier:F2}x)");
            }
        }

        /// <summary>
        /// Calculate growth rate modifier based on progress toward genetic maximum.
        /// Uses sigmoid curve to naturally slow growth as plant approaches maturity.
        /// </summary>
        private float CalculateGrowthCurveModifier(float heightProgress)
        {
            // Clamp progress to prevent issues
            heightProgress = Mathf.Clamp01(heightProgress);

            // Apply sigmoid-like growth curve (rapid early growth, slowing toward maximum)
            // Formula: 1 / (1 + exp(10 * (progress - 0.7)))
            // This gives fast growth until 70% of max height, then rapid slowdown
            float sigmoidInput = 10f * (heightProgress - 0.7f);
            float sigmoidModifier = 1f / (1f + Mathf.Exp(sigmoidInput));

            // Ensure minimum growth rate (plants always grow a little)
            return Mathf.Max(0.1f, sigmoidModifier);
        }

        /// <summary>
        /// Update root development based on genetic factors and growth stage.
        /// </summary>
        private void UpdateRootDevelopment(float timeMultiplier)
        {
            switch (_currentGrowthStage)
            {
                case PlantGrowthStage.Seed:
                case PlantGrowthStage.Germination:
                    _rootMassPercentage = Mathf.Min(80f, _rootMassPercentage + (5f * timeMultiplier)); // Rapid root development
                    break;

                case PlantGrowthStage.Seedling:
                case PlantGrowthStage.Vegetative:
                    // Root development continues but slows as above-ground mass increases
                    float targetRootPercentage = GetGeneticRootMassTarget();
                    if (_rootMassPercentage < targetRootPercentage)
                    {
                        _rootMassPercentage += 1f * timeMultiplier;
                    }
                    break;

                case PlantGrowthStage.Flowering:
                case PlantGrowthStage.Ripening:
                    // Root development mostly stops during flowering
                    break;
            }

            _rootMassPercentage = Mathf.Clamp(_rootMassPercentage, 10f, 90f);
        }

        /// <summary>
        /// Update leaf area development with genetic and environmental considerations.
        /// </summary>
        private void UpdateLeafAreaDevelopment(float heightGrowth, float timeMultiplier)
        {
            switch (_currentGrowthStage)
            {
                case PlantGrowthStage.Vegetative:
                case PlantGrowthStage.PreFlowering:
                    // Rapid leaf development during vegetative growth
                    float leafGrowthRate = heightGrowth * 2.5f; // Leaves grow faster than height

                    // Apply environmental factors to leaf development
                    if (_currentEnvironment.IsInitialized())
                    {
                        float lightModifier = Mathf.Clamp(_currentEnvironment.LightIntensity / 600f, 0.5f, 1.5f);
                        leafGrowthRate *= lightModifier;
                    }

                    _leafArea += leafGrowthRate;
                    break;

                case PlantGrowthStage.Flowering:
                    // Slower leaf development during flowering (energy goes to flowers)
                    _leafArea += heightGrowth * 0.5f * timeMultiplier;
                    break;

                case PlantGrowthStage.Ripening:
                    // Leaf area may decrease as plant focuses on seed/flower development
                    if (_leafArea > 100f) // Maintain minimum leaf area for photosynthesis
                    {
                        _leafArea -= heightGrowth * 0.2f * timeMultiplier;
                    }
                    break;
            }

            // Genetic maximum leaf area (roughly proportional to height)
            float maxLeafArea = (_calculatedMaxHeight > 0f ? _calculatedMaxHeight : 150f) * 8f; // 8 cm² per cm height
            _leafArea = Mathf.Clamp(_leafArea, 0f, maxLeafArea);
        }

        /// <summary>
        /// Get genetic target for root mass percentage.
        /// </summary>
        private float GetGeneticRootMassTarget()
        {
            // Different strains have different root characteristics
            if (_strain != null)
            {
                var strainName = _strain.name?.ToLower();
                if (!string.IsNullOrEmpty(strainName))
                {
                    if (strainName.Contains("indica"))
                        return 35f; // Indica tends to have more robust root systems
                    else if (strainName.Contains("sativa"))
                        return 25f; // Sativa focuses more on above-ground growth
                    else if (strainName.Contains("auto"))
                        return 40f; // Autoflowers need strong roots for fast growth
                }
            }

            return 30f; // Default target
        }

        private void UpdateHealthAndVitality(float environmentalStress, float timeMultiplier)
        {
            // Health is affected by stress, nutrition, and hydration
            float nutritionFactor = (_nutrientLevel + _waterLevel) * 0.5f;
            float stressFactor = 1f - environmentalStress;

            float healthChange = (nutritionFactor * stressFactor - 0.7f) * 0.1f * timeMultiplier;
            _overallHealth = Mathf.Clamp01(_overallHealth + healthChange);

            // Vigor decreases with age and stress
            float vigorDecay = (environmentalStress * 0.05f + 0.001f) * timeMultiplier;
            _vigor = Mathf.Clamp01(_vigor - vigorDecay);

            // Stress level recovers over time in good conditions
            if (environmentalStress < 0.2f)
            {
                _stressLevel = Mathf.Clamp01(_stressLevel - 0.1f * timeMultiplier);
            }
            else
            {
                _stressLevel = Mathf.Clamp01(_stressLevel + environmentalStress * 0.2f * timeMultiplier);
            }

            // Immune response develops with age and good conditions
            if (_overallHealth > 0.7f)
            {
                _immuneResponse = Mathf.Clamp01(_immuneResponse + 0.01f * timeMultiplier);
            }
        }

        private void UpdateResourceConsumption(float timeMultiplier)
        {
            // Water consumption increases with plant size and temperature
            float waterConsumption = (_currentHeight / 100f) * 0.1f * timeMultiplier;
            waterConsumption *= 1f + (_currentEnvironment.Temperature - 20f) * 0.02f; // More water needed in heat
            _waterLevel = Mathf.Clamp01(_waterLevel - waterConsumption);

            // Nutrient consumption increases with growth rate
            float nutrientConsumption = _dailyGrowthRate * 0.05f * timeMultiplier;
            _nutrientLevel = Mathf.Clamp01(_nutrientLevel - nutrientConsumption);

            // Energy reserves are consumed for growth and stress response
            float energyConsumption = (_dailyGrowthRate + _stressLevel * 0.1f) * 0.03f * timeMultiplier;
            _energyReserves = Mathf.Clamp01(_energyReserves - energyConsumption);

            // Energy reserves recover from good nutrition
            if (_nutrientLevel > 0.7f)
            {
                _energyReserves = Mathf.Clamp01(_energyReserves + 0.05f * timeMultiplier);
            }
        }

        private void CheckStageTransition()
        {
            // Define typical stage durations (can be modified by genetics and environment)
            float transitionThreshold = _currentGrowthStage switch
            {
                PlantGrowthStage.Seed => 1f, // 1 day
                PlantGrowthStage.Germination => 3f, // 3 days
                PlantGrowthStage.Seedling => 14f, // 2 weeks
                PlantGrowthStage.Vegetative => 35f, // 5 weeks (varies greatly)
                PlantGrowthStage.PreFlowering => 7f, // 1 week
                PlantGrowthStage.Flowering => 56f, // 8 weeks (strain dependent)
                PlantGrowthStage.Ripening => 7f, // 1 week
                _ => float.MaxValue
            };

            // Check if ready to transition
            if (_daysInCurrentStage >= transitionThreshold && _overallHealth > 0.3f)
            {
                TransitionToNextStage();
            }
        }

        private void TransitionToNextStage()
        {
            PlantGrowthStage nextStage = _currentGrowthStage switch
            {
                PlantGrowthStage.Seed => PlantGrowthStage.Germination,
                PlantGrowthStage.Germination => PlantGrowthStage.Seedling,
                PlantGrowthStage.Seedling => PlantGrowthStage.Vegetative,
                PlantGrowthStage.Vegetative => PlantGrowthStage.PreFlowering,
                PlantGrowthStage.PreFlowering => PlantGrowthStage.Flowering,
                PlantGrowthStage.Flowering => PlantGrowthStage.Ripening,
                PlantGrowthStage.Ripening => PlantGrowthStage.Harvest,
                _ => _currentGrowthStage
            };

            if (nextStage != _currentGrowthStage)
            {
                _currentGrowthStage = nextStage;
                _daysInCurrentStage = 0f;

                // Stage transition effects
                OnStageTransition(nextStage);
            }
        }

        private void OnStageTransition(PlantGrowthStage newStage)
        {
            switch (newStage)
            {
                case PlantGrowthStage.Flowering:
                    // Flowering transition - switch from vegetative growth to bud development
                    _leafArea *= 0.9f; // Slight reduction in leaf area
                    break;
                case PlantGrowthStage.Harvest:
                    // Ready for harvest
                    _vigor = 0.1f; // Plant energy focused on seed production
                    break;
            }
        }

        /// <summary>
        /// Gets the strain name for this plant instance.
        /// </summary>
        public string StrainName => _strain?.StrainName ?? "Unknown Strain";

        /// <summary>
        /// Harvests this plant and returns the harvest results.
        /// </summary>
        public DataHarvestResults Harvest()
        {
            var results = new DataHarvestResults
            {
                PlantID = _plantID,
                StrainName = StrainName,
                TotalYieldGrams = CalculateYieldPotential() * (_strain?.BaseYieldGrams ?? 100f),
                QualityScore = CalculateQualityScore(),
                PotencyPercentage = CalculatePotencyPotential() * 100f,
                HarvestDate = DateTime.Now
            };

            // Transition to harvested state
            _currentGrowthStage = PlantGrowthStage.Harvest;

            return results;
        }

        private float CalculateQualityScore()
        {
            float healthComponent = _overallHealth * 0.4f;
            float vigorComponent = _vigor * 0.2f;
            float stressComponent = (1f - _stressLevel) * 0.3f;
            float environmentalComponent = (_optimalDays / Mathf.Max(1f, _ageInDays)) * 0.1f;

            return Mathf.Clamp01(healthComponent + vigorComponent + stressComponent + environmentalComponent);
        }

        private ProjectChimera.Data.Genetics.PlantStrainSO ConvertToGeneticsStrain(ProjectChimera.Data.Cultivation.PlantStrainSO cultivationStrain)
        {
            var geneticsStrain = ScriptableObject.CreateInstance<ProjectChimera.Data.Genetics.PlantStrainSO>();
            geneticsStrain.strainName = cultivationStrain.strainName;
            return geneticsStrain;
        }

        private ProjectChimera.Data.Genetics.PlantStrainSO CreateDefaultGeneticsStrain()
        {
            var geneticsStrain = ScriptableObject.CreateInstance<ProjectChimera.Data.Genetics.PlantStrainSO>();
            geneticsStrain.strainName = "Unknown";
            return geneticsStrain;
        }

        public void InitializeFromStrain(PlantStrainSO strain = null)
        {
            _plantID = _plantID ?? System.Guid.NewGuid().ToString();
            _strain = strain;

            // Set plant name
            if (strain != null)
            {
                _plantName = $"{strain.strainName}_{_plantID[..8]}";
            }

            // Initialize growth state
            _ageInDays = 0f;
            _currentGrowthStage = PlantGrowthStage.Seed;
            _daysInCurrentStage = 0f;
            _maturityLevel = 0f;

            // Initialize health metrics
            _overallHealth = 1f;
            _vigor = 1f;
            _stressLevel = 0f;
            _immuneResponse = 1f;

            // Initialize resources
            _waterLevel = 1f;
            _nutrientLevel = 1f;
            _energyReserves = 1f;

            // Initialize physical characteristics
            _currentHeight = 0.1f; // Small seedling height
            _currentWidth = 0.05f; // Small seedling width
            _rootMassPercentage = 0.3f; // Seedlings have relatively high root mass
            _leafArea = 0.01f; // Very small initial leaf area

            // Initialize growth rates
            _dailyGrowthRate = 0.05f; // 5% daily growth
            _biomassAccumulation = 0f;
            _rootDevelopmentRate = 0.02f; // 2% daily root development

            // Initialize date tracking
            _plantedDate = System.DateTime.Now;

            // Initialize environmental conditions to defaults
            _currentEnvironment = new EnvironmentalConditions
            {
                Temperature = 22f,
                Humidity = 50f,
                CO2Level = 400f,
                LightIntensity = 300f,
                AirflowRate = 1f
            };

            // Initialize stress tracking
            _cumulativeStressDays = 0f;
            _optimalDays = 0f;

            Debug.Log($"[PlantInstanceSO] Initialized plant {_plantID} with strain {strain?.strainName ?? "Unknown"}");
        }


    }

    /// <summary>
    /// Results from harvesting a plant (Data layer version).
    /// </summary>
    [System.Serializable]
    public class DataHarvestResults
    {
        public string PlantID;
        public string StrainName;
        public float TotalYieldGrams;
        public float QualityScore;
        public float PotencyPercentage;
        public DateTime HarvestDate;

        // Compatibility property for PlantManager.cs
        public float TotalYield
        {
            get => TotalYieldGrams;
            set => TotalYieldGrams = value;
        }
    }
}
