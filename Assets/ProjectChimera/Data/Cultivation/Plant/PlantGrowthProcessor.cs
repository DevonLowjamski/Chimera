using UnityEngine;
using ProjectChimera.Data.Shared;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Growth Processor
    /// Single Responsibility: Plant growth calculations, stage progression, and trait expression
    /// Extracted from PlantInstanceSO for better separation of concerns
    /// </summary>
    [System.Serializable]
    public class PlantGrowthProcessor
    {
        [Header("Growth Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableTraitCalculation = true;
        [SerializeField] private bool _environmentalInfluence = true;
        [SerializeField] private float _baseGrowthMultiplier = 1f;

        // Growth parameters
        [SerializeField] private float _dailyGrowthRate = 1f;
        [SerializeField] private float _biomassAccumulation = 2f;
        [SerializeField] private float _rootDevelopmentRate = 1f;
        [SerializeField] private float _leafGrowthRate = 1.2f;
        [SerializeField] private float _stemGrowthRate = 0.8f;

        // Stage transition thresholds
        [SerializeField] private float _vegetativeStageThreshold = 0.25f;
        [SerializeField] private float _floweringStageThreshold = 0.65f;
        [SerializeField] private float _maturityStageThreshold = 0.95f;

        // Environmental factors
        [SerializeField] private EnvironmentalConditions _currentEnvironment;
        [SerializeField] private float _lightOptimalityFactor = 1f;
        [SerializeField] private float _temperatureOptimalityFactor = 1f;
        [SerializeField] private float _humidityOptimalityFactor = 1f;

        // Genetic influence
        [SerializeField] private float _calculatedMaxHeight = 150f;
        [SerializeField] private float _calculatedMaxWidth = 80f;
        [SerializeField] private float _geneticVigorModifier = 1f;
        [SerializeField] private float _lastTraitCalculationAge = 0f;

        // Growth state
        [SerializeField] private float _currentGrowthProgress = 0f;
        [SerializeField] private float _totalBiomass = 0f;
        [SerializeField] private float _rootMass = 0f;
        [SerializeField] private float _leafMass = 0f;
        [SerializeField] private float _stemMass = 0f;

        // Growth history tracking
        private System.Collections.Generic.List<GrowthMeasurement> _growthHistory = new System.Collections.Generic.List<GrowthMeasurement>();
        [SerializeField] private int _maxGrowthHistoryEntries = 30;

        // Statistics
        private PlantGrowthStats _stats = new PlantGrowthStats();

        // State tracking
        private bool _isInitialized = false;
        private DateTime _lastGrowthUpdate = DateTime.Now;

        // Events
        public event System.Action<float, float> OnGrowthProgressChanged; // old progress, new progress
        public event System.Action<float, float> OnHeightChanged; // old height, new height
        public event System.Action<float, float> OnBiomassChanged; // old biomass, new biomass
        public event System.Action<PlantGrowthStage, PlantGrowthStage> OnStageTransitionRecommended; // current stage, recommended stage
        public event System.Action<GrowthMeasurement> OnGrowthMeasurementTaken;
        public event System.Action<string> OnGrowthAnomalyDetected; // anomaly description

        public bool IsInitialized => _isInitialized;
        public PlantGrowthStats Stats => _stats;
        public float DailyGrowthRate => _dailyGrowthRate;
        public float BiomassAccumulation => _biomassAccumulation;
        public float RootDevelopmentRate => _rootDevelopmentRate;
        public float CurrentGrowthProgress => _currentGrowthProgress;
        public float TotalBiomass => _totalBiomass;
        public float CalculatedMaxHeight => _calculatedMaxHeight;
        public float CalculatedMaxWidth => _calculatedMaxWidth;
        public float GeneticVigorModifier => _geneticVigorModifier;
        public EnvironmentalConditions CurrentEnvironment => _currentEnvironment;

        public void Initialize()
        {
            if (_isInitialized) return;

            _growthHistory.Clear();
            InitializeEnvironmentalFactors();
            ResetStats();
            _lastGrowthUpdate = DateTime.Now;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Plant Growth Processor initialized - Growth Rate: {_dailyGrowthRate:F2}, Max Height: {_calculatedMaxHeight:F1}cm");
            }
        }

        /// <summary>
        /// Process daily growth
        /// </summary>
        public GrowthComputationResult ProcessDailyGrowth(float ageInDays, float healthFactor = 1f, float resourceFactor = 1f)
        {
            if (!_isInitialized) Initialize();

            var startTime = DateTime.Now;
            var oldProgress = _currentGrowthProgress;
            var oldBiomass = _totalBiomass;

            // Calculate environmental influence
            var environmentalFactor = CalculateEnvironmentalFactor();

            // Calculate genetic trait influence
            if (_enableTraitCalculation && ageInDays > _lastTraitCalculationAge + 7f) // Recalculate weekly
            {
                CalculateGeneticTraits(ageInDays);
                _lastTraitCalculationAge = ageInDays;
            }

            // Calculate total growth modifier
            var totalGrowthModifier = _baseGrowthMultiplier * healthFactor * resourceFactor * environmentalFactor * _geneticVigorModifier;

            // Process biomass accumulation
            var dailyBiomassGain = _biomassAccumulation * totalGrowthModifier;
            _totalBiomass += dailyBiomassGain;

            // Distribute biomass to plant parts
            DistributeBiomass(dailyBiomassGain);

            // Update growth progress (0-1 scale)
            var ageBasedProgress = Mathf.Clamp01(ageInDays / 90f); // Assume 90 days for full maturity
            var biomassBasedProgress = Mathf.Clamp01(_totalBiomass / 100f); // Assume 100g for full biomass
            _currentGrowthProgress = (ageBasedProgress + biomassBasedProgress) / 2f;

            // Update growth rates based on current progress
            UpdateGrowthRates();

            // Check for stage transitions
            var recommendedStage = DetermineGrowthStage(_currentGrowthProgress, ageInDays);

            // Record growth measurement
            var measurement = new GrowthMeasurement
            {
                Timestamp = DateTime.Now,
                AgeInDays = ageInDays,
                GrowthProgress = _currentGrowthProgress,
                TotalBiomass = _totalBiomass,
                Height = CalculateCurrentHeight(),
                Width = CalculateCurrentWidth(),
                HealthFactor = healthFactor,
                ResourceFactor = resourceFactor,
                EnvironmentalFactor = environmentalFactor,
                GrowthModifier = totalGrowthModifier
            };

            RecordGrowthMeasurement(measurement);

            // Detect growth anomalies
            DetectGrowthAnomalies(measurement);

            var result = new GrowthComputationResult
            {
                Success = true,
                BiomassGain = dailyBiomassGain,
                HeightGain = measurement.Height - (GetLastMeasurement()?.Height ?? measurement.Height),
                WidthGain = measurement.Width - (GetLastMeasurement()?.Width ?? measurement.Width),
                GrowthModifier = totalGrowthModifier,
                RecommendedStage = recommendedStage,
                ProcessingTime = (float)(DateTime.Now - startTime).TotalMilliseconds
            };

            _stats.GrowthProcessingCycles++;
            _stats.TotalBiomassGained += dailyBiomassGain;
            _lastGrowthUpdate = DateTime.Now;

            // Fire events
            OnGrowthProgressChanged?.Invoke(oldProgress, _currentGrowthProgress);
            OnBiomassChanged?.Invoke(oldBiomass, _totalBiomass);
            OnGrowthMeasurementTaken?.Invoke(measurement);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Daily growth processed: Progress {_currentGrowthProgress:F2}, Biomass +{dailyBiomassGain:F2}g");
            }

            return result;
        }

        /// <summary>
        /// Calculate current height based on growth progress and genetics
        /// </summary>
        public float CalculateCurrentHeight()
        {
            if (!_isInitialized) Initialize();

            var baseHeight = _calculatedMaxHeight * _currentGrowthProgress;
            var biomassInfluence = Mathf.Sqrt(_totalBiomass / 100f) * 20f; // Biomass contributes to height
            var environmentalInfluence = CalculateEnvironmentalFactor() * 10f;

            var currentHeight = baseHeight + biomassInfluence + environmentalInfluence;
            return Mathf.Clamp(currentHeight, 1f, _calculatedMaxHeight * 1.2f); // Allow some overflow
        }

        /// <summary>
        /// Calculate current width based on growth progress and genetics
        /// </summary>
        public float CalculateCurrentWidth()
        {
            if (!_isInitialized) Initialize();

            var baseWidth = _calculatedMaxWidth * _currentGrowthProgress;
            var biomassInfluence = Mathf.Sqrt(_totalBiomass / 100f) * 15f;
            var leafMassInfluence = (_leafMass / _totalBiomass) * 20f; // Leafy plants are wider

            var currentWidth = baseWidth + biomassInfluence + leafMassInfluence;
            return Mathf.Clamp(currentWidth, 1f, _calculatedMaxWidth * 1.5f);
        }

        /// <summary>
        /// Calculate leaf area based on biomass and growth stage
        /// </summary>
        public float CalculateLeafArea()
        {
            if (!_isInitialized) Initialize();

            var baseLeafArea = _leafMass * 10f; // 10 cm² per gram of leaf mass
            var stageFactor = GetLeafAreaStageFactor(_currentGrowthProgress);
            var environmentalFactor = _lightOptimalityFactor; // More light = more leaves

            var leafArea = baseLeafArea * stageFactor * environmentalFactor;
            return Mathf.Max(1f, leafArea);
        }

        /// <summary>
        /// Set environmental conditions
        /// </summary>
        public void SetEnvironmentalConditions(EnvironmentalConditions conditions)
        {
            if (!_isInitialized) Initialize();

            _currentEnvironment = conditions;
            UpdateEnvironmentalFactors();

            _stats.EnvironmentalUpdates++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Environmental conditions updated: Temp={conditions.Temperature:F1}°C, Humidity={conditions.Humidity:F1}%");
            }
        }

        /// <summary>
        /// Set genetic parameters
        /// </summary>
        public void SetGeneticParameters(float maxHeight, float maxWidth, float vigorModifier)
        {
            if (!_isInitialized) Initialize();

            _calculatedMaxHeight = Mathf.Max(10f, maxHeight);
            _calculatedMaxWidth = Mathf.Max(5f, maxWidth);
            _geneticVigorModifier = Mathf.Clamp(vigorModifier, 0.1f, 3f);

            _stats.GeneticUpdates++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Genetic parameters updated: MaxHeight={_calculatedMaxHeight:F1}cm, MaxWidth={_calculatedMaxWidth:F1}cm, Vigor={_geneticVigorModifier:F2}");
            }
        }

        /// <summary>
        /// Get growth summary
        /// </summary>
        public PlantGrowthSummary GetGrowthSummary()
        {
            return new PlantGrowthSummary
            {
                CurrentProgress = _currentGrowthProgress,
                TotalBiomass = _totalBiomass,
                CurrentHeight = CalculateCurrentHeight(),
                CurrentWidth = CalculateCurrentWidth(),
                CurrentLeafArea = CalculateLeafArea(),
                DailyGrowthRate = _dailyGrowthRate,
                BiomassAccumulation = _biomassAccumulation,
                RootDevelopmentRate = _rootDevelopmentRate,
                EnvironmentalFactor = CalculateEnvironmentalFactor(),
                GeneticVigorModifier = _geneticVigorModifier,
                RecommendedStage = DetermineGrowthStage(_currentGrowthProgress, 0f),
                LastGrowthUpdate = _lastGrowthUpdate,
                GrowthHistoryEntries = _growthHistory.Count
            };
        }

        /// <summary>
        /// Get growth history
        /// </summary>
        public System.Collections.Generic.List<GrowthMeasurement> GetGrowthHistory()
        {
            return new System.Collections.Generic.List<GrowthMeasurement>(_growthHistory);
        }

        /// <summary>
        /// Calculate environmental growth factor
        /// </summary>
        private float CalculateEnvironmentalFactor()
        {
            if (!_environmentalInfluence) return 1f;

            var temperatureFactor = _temperatureOptimalityFactor;
            var humidityFactor = _humidityOptimalityFactor;
            var lightFactor = _lightOptimalityFactor;

            // Combine factors (multiplicative for realistic interaction)
            return temperatureFactor * humidityFactor * lightFactor;
        }

        /// <summary>
        /// Update environmental factors based on current conditions
        /// </summary>
        private void UpdateEnvironmentalFactors()
        {
            // Temperature optimality (around 22-26°C is optimal)
            var tempOptimal = 24f;
            var tempTolerance = 8f;
            _temperatureOptimalityFactor = Mathf.Clamp01(1f - Mathf.Abs(_currentEnvironment.Temperature - tempOptimal) / tempTolerance);

            // Humidity optimality (around 50-70% is optimal)
            var humidityOptimal = 60f;
            var humidityTolerance = 30f;
            _humidityOptimalityFactor = Mathf.Clamp01(1f - Mathf.Abs(_currentEnvironment.Humidity - humidityOptimal) / humidityTolerance);

            // Light optimality (assumed light intensity is in the environment data)
            _lightOptimalityFactor = Mathf.Clamp01(_currentEnvironment.LightIntensity / 100f); // Normalize to 0-1
        }

        /// <summary>
        /// Initialize environmental factors with default values
        /// </summary>
        private void InitializeEnvironmentalFactors()
        {
            _lightOptimalityFactor = 0.8f;
            _temperatureOptimalityFactor = 0.9f;
            _humidityOptimalityFactor = 0.85f;
        }

        /// <summary>
        /// Calculate genetic traits based on age
        /// </summary>
        private void CalculateGeneticTraits(float ageInDays)
        {
            // Simulate genetic expression over time
            var maturityFactor = Mathf.Clamp01(ageInDays / 60f); // 60 days to express most traits

            // Vigor modifier changes as plant matures
            _geneticVigorModifier = Mathf.Lerp(0.8f, 1.2f, maturityFactor);

            // Max height can increase slightly as genetics fully express
            var baseMaxHeight = _calculatedMaxHeight;
            _calculatedMaxHeight = baseMaxHeight * (1f + (maturityFactor * 0.1f));
        }

        /// <summary>
        /// Distribute biomass to different plant parts
        /// </summary>
        private void DistributeBiomass(float dailyBiomassGain)
        {
            // Distribution varies by growth stage
            var rootRatio = GetRootMassRatio(_currentGrowthProgress);
            var leafRatio = GetLeafMassRatio(_currentGrowthProgress);
            var stemRatio = 1f - rootRatio - leafRatio;

            _rootMass += dailyBiomassGain * rootRatio;
            _leafMass += dailyBiomassGain * leafRatio;
            _stemMass += dailyBiomassGain * stemRatio;
        }

        /// <summary>
        /// Get root mass ratio based on growth progress
        /// </summary>
        private float GetRootMassRatio(float progress)
        {
            // Early growth focuses on roots, later shifts to above-ground
            return Mathf.Lerp(0.5f, 0.25f, progress);
        }

        /// <summary>
        /// Get leaf mass ratio based on growth progress
        /// </summary>
        private float GetLeafMassRatio(float progress)
        {
            // Leaf development peaks in middle stages
            return 0.4f * Mathf.Sin(progress * Mathf.PI) + 0.2f;
        }

        /// <summary>
        /// Get leaf area stage factor
        /// </summary>
        private float GetLeafAreaStageFactor(float progress)
        {
            // Leaf area increases rapidly in vegetative stage
            if (progress < 0.6f) return Mathf.Lerp(0.5f, 1.5f, progress / 0.6f);
            return Mathf.Lerp(1.5f, 1f, (progress - 0.6f) / 0.4f);
        }

        /// <summary>
        /// Update growth rates based on current progress
        /// </summary>
        private void UpdateGrowthRates()
        {
            // Growth rates change throughout plant lifecycle
            var progressFactor = _currentGrowthProgress;

            _dailyGrowthRate = Mathf.Lerp(0.5f, 2f, progressFactor) * _baseGrowthMultiplier;
            _biomassAccumulation = Mathf.Lerp(1f, 3f, progressFactor) * _baseGrowthMultiplier;
            _rootDevelopmentRate = Mathf.Lerp(2f, 0.5f, progressFactor) * _baseGrowthMultiplier; // Slower later
            _leafGrowthRate = Mathf.Lerp(0.8f, 1.5f, Mathf.Sin(progressFactor * Mathf.PI)); // Peak in middle
            _stemGrowthRate = Mathf.Lerp(0.3f, 1.2f, progressFactor) * _baseGrowthMultiplier;
        }

        /// <summary>
        /// Determine growth stage based on progress and age
        /// </summary>
        private PlantGrowthStage DetermineGrowthStage(float progress, float ageInDays)
        {
            if (progress < _vegetativeStageThreshold || ageInDays < 14f)
                return PlantGrowthStage.Seedling;
            else if (progress < _floweringStageThreshold || ageInDays < 45f)
                return PlantGrowthStage.Vegetative;
            else if (progress < _maturityStageThreshold || ageInDays < 75f)
                return PlantGrowthStage.Flowering;
            else
                return PlantGrowthStage.Mature;
        }

        /// <summary>
        /// Record growth measurement
        /// </summary>
        private void RecordGrowthMeasurement(GrowthMeasurement measurement)
        {
            _growthHistory.Add(measurement);

            // Limit history size
            while (_growthHistory.Count > _maxGrowthHistoryEntries)
            {
                _growthHistory.RemoveAt(0);
            }

            _stats.GrowthMeasurements++;
        }

        /// <summary>
        /// Detect growth anomalies
        /// </summary>
        private void DetectGrowthAnomalies(GrowthMeasurement current)
        {
            var lastMeasurement = GetLastMeasurement();
            if (lastMeasurement == null) return;

            // Check for unusual growth patterns
            var heightChange = current.Height - lastMeasurement.Value.Height;
            var biomassChange = current.TotalBiomass - lastMeasurement.Value.TotalBiomass;

            if (heightChange < -5f) // Height decreased significantly
            {
                OnGrowthAnomalyDetected?.Invoke($"Height decreased by {-heightChange:F1}cm");
            }

            if (biomassChange < -2f) // Biomass decreased significantly
            {
                OnGrowthAnomalyDetected?.Invoke($"Biomass decreased by {-biomassChange:F1}g");
            }

            if (current.GrowthModifier < 0.3f) // Very poor growth conditions
            {
                OnGrowthAnomalyDetected?.Invoke($"Poor growth conditions detected (modifier: {current.GrowthModifier:F2})");
            }
        }

        /// <summary>
        /// Get last growth measurement
        /// </summary>
        private GrowthMeasurement? GetLastMeasurement()
        {
            return _growthHistory.Count > 1 ? _growthHistory[_growthHistory.Count - 2] : null;
        }

        /// <summary>
        /// Reset growth statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new PlantGrowthStats();
        }

        /// <summary>
        /// Set growth parameters
        /// </summary>
        public void SetGrowthParameters(bool enableTraitCalculation, bool environmentalInfluence, float baseMultiplier)
        {
            _enableTraitCalculation = enableTraitCalculation;
            _environmentalInfluence = environmentalInfluence;
            _baseGrowthMultiplier = Mathf.Max(0.1f, baseMultiplier);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Growth parameters updated: Traits={enableTraitCalculation}, Environmental={environmentalInfluence}, Multiplier={baseMultiplier:F2}");
            }
        }

        /// <summary>
        /// Force growth calculation refresh
        /// </summary>
        [ContextMenu("Force Growth Refresh")]
        public void ForceGrowthRefresh()
        {
            if (_isInitialized)
            {
                UpdateGrowthRates();
                UpdateEnvironmentalFactors();
                _lastGrowthUpdate = DateTime.Now;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Plant growth manually refreshed");
                }
            }
        }
    }

    /// <summary>
    /// Plant growth statistics
    /// </summary>
    [System.Serializable]
    public struct PlantGrowthStats
    {
        public int GrowthProcessingCycles;
        public int GrowthMeasurements;
        public int EnvironmentalUpdates;
        public int GeneticUpdates;
        public float TotalBiomassGained;
    }

    /// <summary>
    /// Growth measurement for tracking
    /// </summary>
    [System.Serializable]
    public struct GrowthMeasurement
    {
        public DateTime Timestamp;
        public float AgeInDays;
        public float GrowthProgress;
        public float TotalBiomass;
        public float Height;
        public float Width;
        public float HealthFactor;
        public float ResourceFactor;
        public float EnvironmentalFactor;
        public float GrowthModifier;
    }

    /// <summary>
    /// Growth processing result
    /// </summary>
    [System.Serializable]
    public struct GrowthComputationResult
    {
        public bool Success;
        public float BiomassGain;
        public float HeightGain;
        public float WidthGain;
        public float GrowthModifier;
        public PlantGrowthStage RecommendedStage;
        public float ProcessingTime;
    }

    /// <summary>
    /// Plant growth summary
    /// </summary>
    [System.Serializable]
    public struct PlantGrowthSummary
    {
        public float CurrentProgress;
        public float TotalBiomass;
        public float CurrentHeight;
        public float CurrentWidth;
        public float CurrentLeafArea;
        public float DailyGrowthRate;
        public float BiomassAccumulation;
        public float RootDevelopmentRate;
        public float EnvironmentalFactor;
        public float GeneticVigorModifier;
        public PlantGrowthStage RecommendedStage;
        public DateTime LastGrowthUpdate;
        public int GrowthHistoryEntries;
    }
}
