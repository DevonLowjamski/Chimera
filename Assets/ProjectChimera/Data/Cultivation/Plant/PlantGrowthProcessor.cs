using UnityEngine;
using ProjectChimera.Data.Shared;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Growth Processor Coordinator
    /// Single Responsibility: Coordinate plant growth using composition pattern
    /// Reduced from 531 lines by extracting environmental and biomass logic to helper classes
    /// </summary>
    [System.Serializable]
    public class PlantGrowthProcessor
    {
        [Header("Growth Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableTraitCalculation = true;
        [SerializeField] private float _baseGrowthMultiplier = 1f;

        // Helper components (Composition pattern for SRP)
        [SerializeField] private GrowthEnvironmentalCalculator _environmentalCalculator;
        [SerializeField] private GrowthBiomassDistributor _biomassDistributor;

        // Growth parameters
        [SerializeField] private float _dailyGrowthRate = 1f;
        [SerializeField] private float _biomassAccumulation = 2f;

        // Stage transition thresholds
        [SerializeField] private float _vegetativeStageThreshold = 0.25f;
        [SerializeField] private float _floweringStageThreshold = 0.65f;
        [SerializeField] private float _maturityStageThreshold = 0.95f;

        // Genetic influence
        [SerializeField] private float _calculatedMaxHeight = 150f;
        [SerializeField] private float _calculatedMaxWidth = 80f;
        [SerializeField] private float _geneticVigorModifier = 1f;
        [SerializeField] private float _lastTraitCalculationAge = 0f;

        // Growth state
        [SerializeField] private float _currentGrowthProgress = 0f;

        // Growth history tracking
        private System.Collections.Generic.List<GrowthMeasurement> _growthHistory = new System.Collections.Generic.List<GrowthMeasurement>();
        [SerializeField] private int _maxGrowthHistoryEntries = 30;

        // Statistics
        private PlantGrowthStats _stats = new PlantGrowthStats();

        // State tracking
        private bool _isInitialized = false;
        private DateTime _lastGrowthUpdate = DateTime.Now;

        // Events
        public event System.Action<float, float> OnGrowthProgressChanged;
        public event System.Action<float, float> OnHeightChanged;
        public event System.Action<float, float> OnBiomassChanged;
        public event System.Action<PlantGrowthStage, PlantGrowthStage> OnStageTransitionRecommended;
        public event System.Action<GrowthMeasurement> OnGrowthMeasurementTaken;
        public event System.Action<string> OnGrowthAnomalyDetected;

        // Public properties
        public bool IsInitialized => _isInitialized;
        public PlantGrowthStats Stats => _stats;
        public float DailyGrowthRate => _dailyGrowthRate;
        public float BiomassAccumulation => _biomassAccumulation;
        public float RootDevelopmentRate => _biomassDistributor?.RootDevelopmentRate ?? 0f;
        public float CurrentGrowthProgress => _currentGrowthProgress;
        public float TotalBiomass => _biomassDistributor?.TotalBiomass ?? 0f;
        public float CalculatedMaxHeight => _calculatedMaxHeight;
        public float CalculatedMaxWidth => _calculatedMaxWidth;
        public float GeneticVigorModifier => _geneticVigorModifier;
        public EnvironmentalConditions CurrentEnvironment => _environmentalCalculator?.CurrentEnvironment;

        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize helper components
            if (_environmentalCalculator == null)
                _environmentalCalculator = new GrowthEnvironmentalCalculator();
            _environmentalCalculator.Initialize();

            if (_biomassDistributor == null)
                _biomassDistributor = new GrowthBiomassDistributor();
            _biomassDistributor.Initialize();

            _growthHistory.Clear();
            ResetStats();
            _lastGrowthUpdate = DateTime.Now;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Growth Processor initialized - Rate: {_dailyGrowthRate:F2}, MaxHeight: {_calculatedMaxHeight:F1}cm", null);
            }
        }

        public GrowthComputationResult ProcessDailyGrowth(float ageInDays, float healthFactor = 1f, float resourceFactor = 1f)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            var result = new GrowthComputationResult
            {
                Success = false,
                ErrorMessage = string.Empty
            };

            try
            {
                // Calculate environmental factor
                float environmentalFactor = _environmentalCalculator.CalculateEnvironmentalFactor();

                // Update genetic traits if enabled
                if (_enableTraitCalculation && ageInDays - _lastTraitCalculationAge > 7f)
                {
                    CalculateGeneticTraits(ageInDays);
                    _lastTraitCalculationAge = ageInDays;
                }

                // Calculate total growth modifier
                float totalModifier = environmentalFactor * healthFactor * resourceFactor * _geneticVigorModifier;
                result.GrowthModifier = totalModifier;

                // Store previous values for events
                float previousProgress = _currentGrowthProgress;
                float previousHeight = CalculateCurrentHeight();
                float previousBiomass = TotalBiomass;

                // Calculate growth increment
                float dailyGrowth = _dailyGrowthRate * totalModifier * 0.01f; // 0.01 progress per day at base rate
                _currentGrowthProgress = Mathf.Clamp01(_currentGrowthProgress + dailyGrowth);

                // Calculate biomass gain
                float dailyBiomassGain = _biomassAccumulation * totalModifier;
                _biomassDistributor.DistributeBiomass(dailyBiomassGain, _currentGrowthProgress);

                // Update growth rates periodically
                if (ageInDays % 7f < 1f) // Update weekly
                {
                    UpdateGrowthRates();
                    _biomassDistributor.UpdateGrowthRates();
                }

                // Calculate current dimensions
                float currentHeight = CalculateCurrentHeight();
                float currentWidth = CalculateCurrentWidth();

                // Determine current and recommended stages
                var currentStage = DetermineGrowthStage(_currentGrowthProgress, ageInDays);
                var recommendedStage = DetermineGrowthStage(_currentGrowthProgress, ageInDays);

                // Create growth measurement
                var measurement = new GrowthMeasurement
                {
                    Timestamp = DateTime.Now,
                    Age = ageInDays,
                    Progress = _currentGrowthProgress,
                    Height = currentHeight,
                    Width = currentWidth,
                    TotalBiomass = TotalBiomass,
                    GrowthModifier = totalModifier,
                    Stage = currentStage
                };

                RecordGrowthMeasurement(measurement);
                DetectGrowthAnomalies(measurement);

                // Fire events for changes
                if (Mathf.Abs(_currentGrowthProgress - previousProgress) > 0.001f)
                    OnGrowthProgressChanged?.Invoke(previousProgress, _currentGrowthProgress);

                if (Mathf.Abs(currentHeight - previousHeight) > 0.1f)
                    OnHeightChanged?.Invoke(previousHeight, currentHeight);

                if (Mathf.Abs(TotalBiomass - previousBiomass) > 0.1f)
                    OnBiomassChanged?.Invoke(previousBiomass, TotalBiomass);

                OnGrowthMeasurementTaken?.Invoke(measurement);
                _stats.TotalGrowthCycles++;

                // Update last growth time
                _lastGrowthUpdate = DateTime.Now;

                result.Success = true;
                result.Height = currentHeight;
                result.Width = currentWidth;
                result.BiomassGain = dailyBiomassGain;
                result.RecommendedStage = recommendedStage;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Growth processed - Progress: {_currentGrowthProgress:P0}, Height: {currentHeight:F1}cm, Modifier: {totalModifier:F2}", null);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Growth processing failed: {ex.Message}";
                ChimeraLogger.LogError("PLANT", result.ErrorMessage, null);
            }

            return result;
        }

        public float CalculateCurrentHeight()
        {
            // Height grows with progress (sigmoid curve for realistic growth)
            float progress = _currentGrowthProgress;
            float sigmoidProgress = 1f / (1f + Mathf.Exp(-10f * (progress - 0.5f)));
            return _calculatedMaxHeight * sigmoidProgress;
        }

        public float CalculateCurrentWidth()
        {
            // Width grows proportionally to height
            float progress = _currentGrowthProgress;
            float widthProgress = Mathf.Pow(progress, 0.8f); // Slightly slower than height
            return _calculatedMaxWidth * widthProgress;
        }

        public float CalculateLeafArea()
        {
            return _biomassDistributor.CalculateLeafArea() * _biomassDistributor.GetLeafAreaStageFactor(_currentGrowthProgress);
        }

        public void SetEnvironmentalConditions(EnvironmentalConditions conditions)
        {
            _environmentalCalculator.SetEnvironmentalConditions(conditions);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Environmental conditions updated - Light: {conditions.LightIntensity}, Temp: {conditions.Temperature}°C, Humidity: {conditions.Humidity}%", null);
            }
        }

        public void SetGeneticParameters(float maxHeight, float maxWidth, float vigorModifier)
        {
            _calculatedMaxHeight = Mathf.Max(10f, maxHeight);
            _calculatedMaxWidth = Mathf.Max(5f, maxWidth);
            _geneticVigorModifier = Mathf.Clamp(vigorModifier, 0.1f, 5f);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Genetic parameters updated - MaxHeight: {maxHeight:F0}cm, MaxWidth: {maxWidth:F0}cm, Vigor: {vigorModifier:F2}", null);
            }
        }

        public PlantGrowthSummary GetGrowthSummary()
        {
            return new PlantGrowthSummary
            {
                CurrentProgress = _currentGrowthProgress,
                CurrentHeight = CalculateCurrentHeight(),
                CurrentWidth = CalculateCurrentWidth(),
                TotalBiomass = TotalBiomass,
                LeafArea = CalculateLeafArea(),
                RootMass = _biomassDistributor.RootMass,
                LeafMass = _biomassDistributor.LeafMass,
                StemMass = _biomassDistributor.StemMass,
                DailyGrowthRate = _dailyGrowthRate,
                GeneticVigorModifier = _geneticVigorModifier,
                EnvironmentalFactor = _environmentalCalculator.CalculateEnvironmentalFactor(),
                LastUpdate = _lastGrowthUpdate,
                TotalMeasurements = _growthHistory.Count,
                Stats = _stats
            };
        }

        public System.Collections.Generic.List<GrowthMeasurement> GetGrowthHistory()
        {
            return new System.Collections.Generic.List<GrowthMeasurement>(_growthHistory);
        }

        private void CalculateGeneticTraits(float ageInDays)
        {
            float maturityFactor = Mathf.Clamp01(ageInDays / 60f);
            _geneticVigorModifier = Mathf.Lerp(0.8f, 1.2f, maturityFactor);

            float baseMaxHeight = _calculatedMaxHeight;
            _calculatedMaxHeight = baseMaxHeight * (1f + (maturityFactor * 0.1f));
        }

        private void UpdateGrowthRates()
        {
            float progressFactor = _currentGrowthProgress;
            _dailyGrowthRate = Mathf.Lerp(0.5f, 2f, progressFactor) * _baseGrowthMultiplier;
            _biomassAccumulation = Mathf.Lerp(1f, 3f, progressFactor) * _baseGrowthMultiplier;
        }

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

        private void RecordGrowthMeasurement(GrowthMeasurement measurement)
        {
            _growthHistory.Add(measurement);

            while (_growthHistory.Count > _maxGrowthHistoryEntries)
            {
                _growthHistory.RemoveAt(0);
            }

            _stats.GrowthMeasurements++;
        }

        private void DetectGrowthAnomalies(GrowthMeasurement current)
        {
            var lastMeasurement = GetLastMeasurement();
            if (lastMeasurement == null) return;

            var heightChange = current.Height - lastMeasurement.Value.Height;
            var biomassChange = current.TotalBiomass - lastMeasurement.Value.TotalBiomass;

            if (heightChange < -5f)
                OnGrowthAnomalyDetected?.Invoke($"Height decreased by {-heightChange:F1}cm");

            if (biomassChange < -2f)
                OnGrowthAnomalyDetected?.Invoke($"Biomass decreased by {-biomassChange:F1}g");

            if (current.GrowthModifier < 0.3f)
                OnGrowthAnomalyDetected?.Invoke($"Poor growth conditions detected (modifier: {current.GrowthModifier:F2})");
        }

        private GrowthMeasurement? GetLastMeasurement()
        {
            return _growthHistory.Count > 1 ? _growthHistory[_growthHistory.Count - 2] : null;
        }

        private void ResetStats()
        {
            _stats = new PlantGrowthStats();
        }

        public void SetGrowthParameters(bool enableTraitCalculation, bool environmentalInfluence, float baseMultiplier)
        {
            _enableTraitCalculation = enableTraitCalculation;
            _environmentalCalculator.SetEnvironmentalInfluence(environmentalInfluence);
            _baseGrowthMultiplier = Mathf.Max(0.1f, baseMultiplier);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Growth parameters updated: Traits={enableTraitCalculation}, Environmental={environmentalInfluence}, Multiplier={baseMultiplier:F2}", null);
            }
        }

        [ContextMenu("Force Growth Refresh")]
        public void ForceGrowthRefresh()
        {
            if (_isInitialized)
            {
                UpdateGrowthRates();
                _lastGrowthUpdate = DateTime.Now;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Plant growth manually refreshed", null);
                }
            }
        }
    }
}
