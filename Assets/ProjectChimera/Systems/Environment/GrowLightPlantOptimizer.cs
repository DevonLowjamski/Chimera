using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Handles plant monitoring, optimization algorithms, and adaptive lighting.
    /// Extracted from AdvancedGrowLightSystem for modular architecture.
    /// Monitors plant health and adjusts lighting for optimal growth.
    /// </summary>
    public class GrowLightPlantOptimizer : MonoBehaviour, ITickable
    {
        [Header("Plant Optimization Configuration")]
        [SerializeField] private bool _enableOptimizationLogging = true;
        [SerializeField] private float _optimizationUpdateInterval = 30f; // Check every 30 seconds
        [SerializeField] private float _adaptationSensitivity = 0.1f;
        [SerializeField] private int _maxMonitoredPlants = 50;

        // Dependencies
        private GrowLightController _lightController;
        private GrowLightSpectrumController _spectrumController;

        // Plant monitoring data
        private List<PlantMonitoringData> _monitoredPlants = new List<PlantMonitoringData>();
        private float _lastOptimizationUpdate = 0f;
        private Dictionary<ProjectChimera.Data.Shared.PlantGrowthStage, OptimizationProfile> _optimizationProfiles = new Dictionary<ProjectChimera.Data.Shared.PlantGrowthStage, OptimizationProfile>();

        // Optimization state
        private bool _isOptimizationActive = true;
        private bool _isAdaptiveModeEnabled = true;
        private float _currentOptimizationScore = 0f;

        // Events
        public System.Action<float> OnOptimizationScoreChanged;
        public System.Action<PlantMonitoringData> OnPlantHealthChanged;
        public System.Action<OptimizationRecommendation> OnOptimizationRecommendation;

        // Properties
        public bool IsOptimizationActive => _isOptimizationActive;
        public bool IsAdaptiveModeEnabled => _isAdaptiveModeEnabled;
        public float CurrentOptimizationScore => _currentOptimizationScore;
        public int MonitoredPlantsCount => _monitoredPlants.Count;

        /// <summary>
        /// Initialize plant optimizer with dependencies
        /// </summary>
        public void Initialize(GrowLightController lightController, GrowLightSpectrumController spectrumController)
        {
            _lightController = lightController;
            _spectrumController = spectrumController;

            InitializeOptimizationProfiles();

            LogDebug("Grow light plant optimizer initialized");
        }

        public void Tick(float deltaTime)
        {
            if (!_isOptimizationActive) return;

            _lastOptimizationUpdate += deltaTime;

            if (_lastOptimizationUpdate >= _optimizationUpdateInterval)
            {
                UpdatePlantMonitoring();
                PerformOptimization();
                _lastOptimizationUpdate = 0f;
            }
        }

        #region Optimization Profiles

        /// <summary>
        /// Initialize optimization profiles for different growth stages
        /// </summary>
        private void InitializeOptimizationProfiles()
        {
            // Seedling stage - gentle lighting
            _optimizationProfiles[ProjectChimera.Data.Shared.PlantGrowthStage.Seedling] = new OptimizationProfile
            {
                OptimalIntensity = 200f,
                MinIntensity = 100f,
                MaxIntensity = 400f,
                PreferredSpectrum = SpectrumPreset.Dawn,
                PhotoperiodHours = 16f,
                Priority = OptimizationPriority.Health
            };

            // Vegetative stage - high blue light
            _optimizationProfiles[ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative] = new OptimizationProfile
            {
                OptimalIntensity = 600f,
                MinIntensity = 400f,
                MaxIntensity = 800f,
                PreferredSpectrum = SpectrumPreset.Vegetative,
                PhotoperiodHours = 18f,
                Priority = OptimizationPriority.Growth
            };

            // Flowering stage - high red light
            _optimizationProfiles[ProjectChimera.Data.Shared.PlantGrowthStage.Flowering] = new OptimizationProfile
            {
                OptimalIntensity = 800f,
                MinIntensity = 600f,
                MaxIntensity = 1000f,
                PreferredSpectrum = SpectrumPreset.Flowering,
                PhotoperiodHours = 12f,
                Priority = OptimizationPriority.Yield
            };
        }

        #endregion

        #region Plant Monitoring

        /// <summary>
        /// Register a plant for monitoring and optimization
        /// </summary>
        public void RegisterPlant(GameObject plantObject)
        {
            if (_monitoredPlants.Count >= _maxMonitoredPlants)
            {
                LogError($"Cannot register plant - maximum monitoring limit reached ({_maxMonitoredPlants})");
                return;
            }

            // Try to get any plant component that might exist
            var plantComponent = plantObject.GetComponent<MonoBehaviour>();
            if (plantComponent == null)
            {
                LogError("Cannot register plant - no plant component found");
                return;
            }

            var monitoringData = new PlantMonitoringData
            {
                PlantId = plantObject.GetInstanceID().ToString(),
                PlantObject = plantObject,
                PlantComponent = plantComponent,
                RegistrationTime = System.DateTime.Now,
                LastHealthCheck = System.DateTime.Now,
                Distance = CalculateDistanceFromLight(plantObject),
                IsActive = true
            };

            _monitoredPlants.Add(monitoringData);
            LogDebug($"Registered plant for monitoring: {monitoringData.PlantId}");
        }

        /// <summary>
        /// Unregister a plant from monitoring
        /// </summary>
        public void UnregisterPlant(GameObject plantObject)
        {
            var plantId = plantObject.GetInstanceID().ToString();
            var monitoring = _monitoredPlants.FirstOrDefault(p => p.PlantId == plantId);

            if (monitoring != null)
            {
                _monitoredPlants.Remove(monitoring);
                LogDebug($"Unregistered plant from monitoring: {plantId}");
            }
        }

        /// <summary>
        /// Update monitoring data for all registered plants
        /// </summary>
        private void UpdatePlantMonitoring()
        {
            foreach (var monitoring in _monitoredPlants.ToList())
            {
                if (monitoring.PlantObject == null || !monitoring.IsActive)
                {
                    _monitoredPlants.Remove(monitoring);
                    continue;
                }

                UpdatePlantMonitoringData(monitoring);
            }
        }

        /// <summary>
        /// Update monitoring data for a specific plant
        /// </summary>
        private void UpdatePlantMonitoringData(PlantMonitoringData monitoring)
        {
            var plantComponent = monitoring.PlantComponent;
            if (plantComponent == null) return;

            // Update plant health metrics
            float previousHealth = monitoring.HealthScore;
            monitoring.HealthScore = CalculateHealthScore(plantComponent);
            monitoring.GrowthRate = CalculateGrowthRate(plantComponent);
            monitoring.StressLevel = CalculateStressLevel(plantComponent);
            monitoring.Distance = CalculateDistanceFromLight(monitoring.PlantObject);
            monitoring.LightIntensityReceived = CalculateReceivedIntensity(monitoring.Distance);
            monitoring.LastHealthCheck = System.DateTime.Now;

            // Check for significant health changes
            if (Mathf.Abs(monitoring.HealthScore - previousHealth) > _adaptationSensitivity)
            {
                OnPlantHealthChanged?.Invoke(monitoring);
            }
        }

        #endregion

        #region Optimization Logic

        /// <summary>
        /// Perform lighting optimization based on plant monitoring data
        /// </summary>
        private void PerformOptimization()
        {
            if (_monitoredPlants.Count == 0) return;

            var activePlants = _monitoredPlants.Where(p => p.IsActive).ToList();
            if (activePlants.Count == 0) return;

            // Calculate overall optimization metrics
            float avgHealthScore = activePlants.Average(p => p.HealthScore);
            float avgGrowthRate = activePlants.Average(p => p.GrowthRate);
            float avgStressLevel = activePlants.Average(p => p.StressLevel);

            // Update optimization score
            float previousScore = _currentOptimizationScore;
            _currentOptimizationScore = CalculateOptimizationScore(avgHealthScore, avgGrowthRate, avgStressLevel);

            if (Mathf.Abs(_currentOptimizationScore - previousScore) > 0.05f)
            {
                OnOptimizationScoreChanged?.Invoke(_currentOptimizationScore);
            }

            // Generate optimization recommendations
            if (_isAdaptiveModeEnabled)
            {
                var recommendation = GenerateOptimizationRecommendation(activePlants);
                if (recommendation != null)
                {
                    ApplyOptimizationRecommendation(recommendation);
                    OnOptimizationRecommendation?.Invoke(recommendation);
                }
            }

            LogDebug($"Optimization update - Score: {_currentOptimizationScore:F2}, Plants: {activePlants.Count}");
        }

        /// <summary>
        /// Generate optimization recommendation based on plant data
        /// </summary>
        private OptimizationRecommendation GenerateOptimizationRecommendation(List<PlantMonitoringData> plants)
        {
            // Analyze plant needs by growth stage
            var stageGroups = plants.GroupBy(p => GetPlantGrowthStage(p.PlantComponent)).ToList();
            var dominantStage = stageGroups.OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;

            if (!dominantStage.HasValue) return null;

            var profile = _optimizationProfiles[dominantStage.Value];
            var recommendation = new OptimizationRecommendation
            {
                RecommendationId = System.Guid.NewGuid().ToString(),
                Timestamp = System.DateTime.Now,
                DominantGrowthStage = dominantStage.Value,
                RecommendationType = OptimizationRecommendationType.Adaptive
            };

            // Analyze current vs optimal conditions
            float currentIntensity = _lightController.CurrentIntensity;
            var currentSpectrum = _spectrumController.CurrentSpectrum;

            // Intensity recommendations
            if (currentIntensity < profile.MinIntensity)
            {
                recommendation.RecommendedIntensity = profile.OptimalIntensity;
                recommendation.Reasoning = $"Increasing intensity for {dominantStage.Value} stage plants";
            }
            else if (currentIntensity > profile.MaxIntensity)
            {
                recommendation.RecommendedIntensity = profile.OptimalIntensity;
                recommendation.Reasoning = $"Reducing intensity to prevent stress in {dominantStage.Value} stage plants";
            }
            else
            {
                recommendation.RecommendedIntensity = currentIntensity; // No change needed
            }

            // Spectrum recommendations
            recommendation.RecommendedSpectrum = profile.PreferredSpectrum;

            // Confidence calculation
            float healthVariance = CalculateVariance(plants.Select(p => p.HealthScore));
            recommendation.Confidence = Mathf.Clamp01(1f - (healthVariance * 2f));

            return recommendation.Confidence > 0.3f ? recommendation : null; // Only return if confident enough
        }

        /// <summary>
        /// Apply optimization recommendation
        /// </summary>
        private void ApplyOptimizationRecommendation(OptimizationRecommendation recommendation)
        {
            if (recommendation.RecommendedIntensity != _lightController.CurrentIntensity)
            {
                _lightController.SetIntensity(recommendation.RecommendedIntensity);
            }

            _spectrumController.ActivatePreset(recommendation.RecommendedSpectrum);

            LogDebug($"Applied optimization: {recommendation.Reasoning}");
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// Calculate health score for a plant
        /// </summary>
        private float CalculateHealthScore(MonoBehaviour plantComponent)
        {
            // This would integrate with actual plant health metrics
            float baseHealth = 0.8f; // Placeholder

            // Factors that could affect health score:
            // - Nutrient levels
            // - Water status
            // - Disease presence
            // - Pest damage
            // - Light stress indicators

            return Mathf.Clamp01(baseHealth + Random.Range(-0.1f, 0.1f));
        }

        /// <summary>
        /// Calculate growth rate for a plant
        /// </summary>
        private float CalculateGrowthRate(MonoBehaviour plantComponent)
        {
            // This would calculate actual growth rate based on plant data
            return Random.Range(0.5f, 1.2f); // Placeholder
        }

        /// <summary>
        /// Calculate stress level for a plant
        /// </summary>
        private float CalculateStressLevel(MonoBehaviour plantComponent)
        {
            // This would analyze stress indicators
            return Random.Range(0f, 0.3f); // Placeholder - low stress
        }

        /// <summary>
        /// Get plant growth stage from component using reflection if needed
        /// </summary>
        private ProjectChimera.Data.Shared.PlantGrowthStage GetPlantGrowthStage(MonoBehaviour plantComponent)
        {
            if (plantComponent == null) return ProjectChimera.Data.Shared.PlantGrowthStage.Seed;

            // Try to get CurrentGrowthStage property using reflection
            var property = plantComponent.GetType().GetProperty("CurrentGrowthStage");
            if (property != null && property.PropertyType == typeof(ProjectChimera.Data.Shared.PlantGrowthStage))
            {
                return (ProjectChimera.Data.Shared.PlantGrowthStage)property.GetValue(plantComponent);
            }

            return ProjectChimera.Data.Shared.PlantGrowthStage.Seed; // Default
        }

        /// <summary>
        /// Calculate distance from light to plant
        /// </summary>
        private float CalculateDistanceFromLight(GameObject plantObject)
        {
            if (_lightController == null) return 2f;

            return Vector3.Distance(transform.position, plantObject.transform.position);
        }

        /// <summary>
        /// Calculate received light intensity based on distance
        /// </summary>
        private float CalculateReceivedIntensity(float distance)
        {
            float currentIntensity = _lightController.CurrentIntensity;

            // Inverse square law approximation
            float falloff = 1f / (1f + distance * distance * 0.1f);

            return currentIntensity * falloff;
        }

        /// <summary>
        /// Calculate overall optimization score
        /// </summary>
        private float CalculateOptimizationScore(float avgHealth, float avgGrowthRate, float avgStressLevel)
        {
            // Weighted scoring system
            float healthWeight = 0.4f;
            float growthWeight = 0.4f;
            float stressWeight = 0.2f;

            float score = (avgHealth * healthWeight) +
                         (Mathf.Clamp01(avgGrowthRate) * growthWeight) +
                         ((1f - avgStressLevel) * stressWeight);

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// Calculate variance in a set of values
        /// </summary>
        private float CalculateVariance(IEnumerable<float> values)
        {
            var valueList = values.ToList();
            if (valueList.Count == 0) return 0f;

            float mean = valueList.Average();
            float variance = valueList.Average(v => Mathf.Pow(v - mean, 2));

            return variance;
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Enable or disable optimization
        /// </summary>
        public void SetOptimizationActive(bool active)
        {
            _isOptimizationActive = active;
            LogDebug($"Optimization {(active ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Enable or disable adaptive mode
        /// </summary>
        public void SetAdaptiveModeEnabled(bool enabled)
        {
            _isAdaptiveModeEnabled = enabled;
            LogDebug($"Adaptive mode {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Get monitoring data for a specific plant
        /// </summary>
        public PlantMonitoringData GetPlantMonitoringData(string plantId)
        {
            return _monitoredPlants.FirstOrDefault(p => p.PlantId == plantId);
        }

        /// <summary>
        /// Get all monitoring data
        /// </summary>
        public List<PlantMonitoringData> GetAllMonitoringData()
        {
            return _monitoredPlants.ToList();
        }

        /// <summary>
        /// Clear all monitoring data
        /// </summary>
        public void ClearMonitoringData()
        {
            _monitoredPlants.Clear();
            LogDebug("Cleared all plant monitoring data");
        }

        #endregion

        private void LogDebug(string message)
        {
            if (_enableOptimizationLogging)
                ChimeraLogger.Log($"[GrowLightPlantOptimizer] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[GrowLightPlantOptimizer] {message}");
        }

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

    protected virtual void Start()
    {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
    }

        protected virtual void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    /// <summary>
    /// Plant monitoring data structure
    /// </summary>
    [System.Serializable]
    public class PlantMonitoringData
    {
        public string PlantId;
        public GameObject PlantObject;
        public MonoBehaviour PlantComponent;
        public System.DateTime RegistrationTime;
        public System.DateTime LastHealthCheck;
        public float HealthScore = 0.8f;
        public float GrowthRate = 1f;
        public float StressLevel = 0f;
        public float Distance = 2f;
        public float LightIntensityReceived = 500f;
        public bool IsActive = true;
    }

    /// <summary>
    /// Optimization profile for different growth stages
    /// </summary>
    [System.Serializable]
    public class OptimizationProfile
    {
        public float OptimalIntensity = 600f;
        public float MinIntensity = 400f;
        public float MaxIntensity = 800f;
        public SpectrumPreset PreferredSpectrum = SpectrumPreset.Balanced;
        public float PhotoperiodHours = 16f;
        public OptimizationPriority Priority = OptimizationPriority.Health;
    }

    /// <summary>
    /// Optimization recommendation
    /// </summary>
    [System.Serializable]
    public class OptimizationRecommendation
    {
        public string RecommendationId;
        public System.DateTime Timestamp;
        public ProjectChimera.Data.Shared.PlantGrowthStage DominantGrowthStage;
        public OptimizationRecommendationType RecommendationType;
        public float RecommendedIntensity;
        public SpectrumPreset RecommendedSpectrum;
        public string Reasoning;
        public float Confidence = 0.5f;
    }

    public enum OptimizationPriority
    {
        Health,
        Growth,
        Yield,
        Efficiency
    }

    public enum OptimizationRecommendationType
    {
        Adaptive,
        Manual,
        Emergency
    }
}
