using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Stress Visualization System for SpeedTree Plants
    /// Handles visual representation of plant health, stress, and environmental responses
    /// in the cannabis cultivation simulation.
    /// </summary>
    public class StressVisualizationSystem : MonoBehaviour
    {
        [Header("Stress Visualization Configuration")]
        [SerializeField] private bool _enableStressVisualization = true;
        [SerializeField] private Gradient _healthGradient = new Gradient();
        [SerializeField] private Gradient _stressGradient = new Gradient();
        [SerializeField] private float _stressVisualizationIntensity = 1.0f;
        [SerializeField] private float _visualizationUpdateFrequency = 0.5f;

        // Plant stress visualization data
        private Dictionary<int, PlantStressVisualization> _plantStressData = new Dictionary<int, PlantStressVisualization>();
        private List<int> _plantsNeedingVisualizationUpdate = new List<int>();

        // Shader property IDs
        private int _healthColorPropertyId;
        private int _stressColorPropertyId;
        private int _stressIntensityPropertyId;
        private int _healthIntensityPropertyId;

        // Update timing
        private float _lastVisualizationUpdate = 0f;

        #region Public Events
        public event Action<int, float> OnPlantHealthVisualizationChanged;
        public event Action<int, float> OnPlantStressVisualizationChanged;
        #endregion

        #region Initialization
        public void Initialize()
        {
            ChimeraLogger.Log("SPEEDTREE/STRESS", "Initialize", this);

            // Cache shader properties
            CacheShaderProperties();

            // Initialize default gradients if not set
            InitializeDefaultGradients();

            ChimeraLogger.Log("SPEEDTREE/STRESS", "Initialized", this);
        }

        public void Shutdown()
        {
            ChimeraLogger.Log("SPEEDTREE/STRESS", "Shutdown", this);

            _plantStressData.Clear();
            _plantsNeedingVisualizationUpdate.Clear();

            ChimeraLogger.Log("SPEEDTREE/STRESS", "Shutdown Complete", this);
        }

        private void CacheShaderProperties()
        {
            _healthColorPropertyId = Shader.PropertyToID("_HealthColor");
            _stressColorPropertyId = Shader.PropertyToID("_StressColor");
            _stressIntensityPropertyId = Shader.PropertyToID("_StressIntensity");
            _healthIntensityPropertyId = Shader.PropertyToID("_HealthIntensity");
        }

        private void InitializeDefaultGradients()
        {
            // Initialize health gradient (green = healthy, yellow = moderate, red = unhealthy)
            if (_healthGradient.colorKeys.Length == 0)
            {
                _healthGradient.colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.red, 0f),     // 0% health
                    new GradientColorKey(Color.yellow, 0.5f), // 50% health
                    new GradientColorKey(Color.green, 1f)     // 100% health
                };
            }

            // Initialize stress gradient (blue = low stress, orange = moderate, red = high stress)
            if (_stressGradient.colorKeys.Length == 0)
            {
                _stressGradient.colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.blue, 0f),     // 0% stress
                    new GradientColorKey(Color.yellow, 0.5f), // 50% stress
                    new GradientColorKey(Color.red, 1f)       // 100% stress
                };
            }
        }
        #endregion

        #region Stress Visualization Logic

        /// <summary>
        /// Updates stress visualization for a specific plant
        /// </summary>
        public void UpdatePlantStressVisualization(int plantId, float healthLevel, float stressLevel)
        {
            if (plantId <= 0 || !_enableStressVisualization) return;

            try
            {
                // Get or create visualization data
                if (!_plantStressData.TryGetValue(plantId, out var visualization))
                {
                    visualization = new PlantStressVisualization(plantId);
                    _plantStressData[plantId] = visualization;
                }

                // Update stress levels
                visualization.HealthLevel = Mathf.Clamp01(healthLevel);
                visualization.StressLevel = Mathf.Clamp01(stressLevel);
                visualization.LastUpdateTime = Time.time;

                // Mark for visual update
                if (!_plantsNeedingVisualizationUpdate.Contains(plantId))
                {
                    _plantsNeedingVisualizationUpdate.Add(plantId);
                }

                // Trigger events
                OnPlantHealthVisualizationChanged?.Invoke(plantId, healthLevel);
                OnPlantStressVisualizationChanged?.Invoke(plantId, stressLevel);

                ChimeraLogger.Log("SPEEDTREE/STRESS", "Updated plant stress", this);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/STRESS", "Update exception", this);
            }
        }

        /// <summary>
        /// Updates stress visualization for multiple plants
        /// </summary>
        public void UpdatePlantStressVisualization(Dictionary<int, PlantStressLevels> plantStressLevels)
        {
            if (plantStressLevels == null) return;

            foreach (var kvp in plantStressLevels)
            {
                UpdatePlantStressVisualization(kvp.Key, kvp.Value.HealthLevel, kvp.Value.StressLevel);
            }
        }

        /// <summary>
        /// Applies stress visualization to plant renderers
        /// </summary>
        public void ApplyStressVisualization(int plantId)
        {
            if (!_plantStressData.TryGetValue(plantId, out var visualization)) return;

            try
            {
                // Find SpeedTree renderers for this plant
                var renderers = FindPlantRenderers(plantId);

                foreach (var renderer in renderers)
                {
                    ApplyStressVisualizationToRenderer(renderer, visualization);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/STRESS", "Apply exception", this);
            }
        }

        private void ApplyStressVisualizationToRenderer(Renderer renderer, PlantStressVisualization visualization)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;

            try
            {
                var material = renderer.material;

                // Calculate visualization colors
                Color healthColor = _healthGradient.Evaluate(visualization.HealthLevel);
                Color stressColor = _stressGradient.Evaluate(visualization.StressLevel);

                // Blend colors based on stress vs health
                float stressWeight = visualization.StressLevel;
                float healthWeight = 1f - stressWeight;

                Color finalColor = (healthColor * healthWeight + stressColor * stressWeight);
                finalColor.a = 1f;

                // Apply shader properties
                if (material.HasProperty(_healthColorPropertyId))
                {
                    material.SetColor(_healthColorPropertyId, healthColor);
                }

                if (material.HasProperty(_stressColorPropertyId))
                {
                    material.SetColor(_stressColorPropertyId, stressColor);
                }

                if (material.HasProperty(_stressIntensityPropertyId))
                {
                    material.SetFloat(_stressIntensityPropertyId, visualization.StressLevel * _stressVisualizationIntensity);
                }

                if (material.HasProperty(_healthIntensityPropertyId))
                {
                    material.SetFloat(_healthIntensityPropertyId, visualization.HealthLevel * _stressVisualizationIntensity);
                }

                // Apply final blended color if shader supports it
                if (material.HasProperty("_StressBlendColor"))
                {
                    material.SetColor("_StressBlendColor", finalColor);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/STRESS", "Apply renderer exception", this);
            }
        }

        private Renderer[] FindPlantRenderers(int plantId)
        {
            // Find renderers by plant ID (this would need to be implemented based on your plant identification system)
            // For now, return empty array - this should be connected to your plant management system
            return new Renderer[0];
        }

        #endregion

        #region Advanced Visualization Features

        /// <summary>
        /// Applies environmental stress indicators
        /// </summary>
        public void ApplyEnvironmentalStressIndicators(int plantId, EnvironmentalStressIndicators indicators)
        {
            if (plantId <= 0 || indicators == null) return;

            try
            {
                // Calculate combined stress level
                float combinedStress = CalculateCombinedStress(indicators);

                // Update visualization with environmental factors
                UpdatePlantStressVisualization(plantId, 1f - combinedStress, combinedStress);

                // Apply specific environmental effects
                ApplyEnvironmentalEffects(plantId, indicators);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/STRESS", "Apply environmental effects exception", this);
            }
        }

        private float CalculateCombinedStress(EnvironmentalStressIndicators indicators)
        {
            // Weight different environmental factors
            float temperatureStress = indicators.TemperatureStress * 0.3f;
            float humidityStress = indicators.HumidityStress * 0.25f;
            float lightStress = indicators.LightStress * 0.25f;
            float nutrientStress = indicators.NutrientStress * 0.2f;

            return Mathf.Clamp01(temperatureStress + humidityStress + lightStress + nutrientStress);
        }

        private void ApplyEnvironmentalEffects(int plantId, EnvironmentalStressIndicators indicators)
        {
            // Apply specific visual effects based on environmental conditions
            var renderers = FindPlantRenderers(plantId);

            foreach (var renderer in renderers)
            {
                ApplyEnvironmentalEffectsToRenderer(renderer, indicators);
            }
        }

        private void ApplyEnvironmentalEffectsToRenderer(Renderer renderer, EnvironmentalStressIndicators indicators)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;

            try
            {
                var material = renderer.material;

                // Apply temperature effects (color tinting)
                if (indicators.TemperatureStress > 0.5f)
                {
                    if (material.HasProperty("_TemperatureTint"))
                    {
                        Color tempTint = indicators.TemperatureStress > 0.7f ? Color.red : Color.yellow;
                        material.SetColor("_TemperatureTint", tempTint);
                    }
                }

                // Apply drought effects (wilting animation)
                if (indicators.HumidityStress > 0.6f && material.HasProperty("_WiltingAmount"))
                {
                    material.SetFloat("_WiltingAmount", indicators.HumidityStress);
                }

                // Apply nutrient deficiency effects
                if (indicators.NutrientStress > 0.4f && material.HasProperty("_NutrientDeficiency"))
                {
                    material.SetFloat("_NutrientDeficiency", indicators.NutrientStress);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/STRESS", "Environmental effects renderer exception", this);
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Register a plant for stress visualization
        /// </summary>
        public void RegisterPlant(int plantId)
        {
            if (plantId <= 0) return;

            if (!_plantStressData.ContainsKey(plantId))
            {
                var visualization = new PlantStressVisualization(plantId);
                _plantStressData[plantId] = visualization;
                ChimeraLogger.Log("SPEEDTREE/STRESS", $"Registered plant {plantId} for stress visualization", this);
            }
        }

        /// <summary>
        /// Unregister a plant from stress visualization
        /// </summary>
        public void UnregisterPlant(int plantId)
        {
            if (_plantStressData.ContainsKey(plantId))
            {
                _plantStressData.Remove(plantId);
                _plantsNeedingVisualizationUpdate.Remove(plantId);
                ChimeraLogger.Log("SPEEDTREE/STRESS", $"Unregistered plant {plantId} from stress visualization", this);
            }
        }

        /// <summary>
        /// Update plant stress levels for visualization
        /// </summary>
        public void UpdatePlantStress(int plantId, float overallStress, float temperatureStress, float humidityStress, float lightStress)
        {
            if (plantId <= 0) return;

            // Ensure plant is registered
            RegisterPlant(plantId);

            // Calculate health level from stress (inverse relationship)
            float healthLevel = 1f - Mathf.Clamp01(overallStress);

            // Update stress visualization
            UpdatePlantStressVisualization(plantId, healthLevel, overallStress);

            // Apply environmental stress indicators
            var indicators = new EnvironmentalStressIndicators
            {
                TemperatureStress = temperatureStress,
                HumidityStress = humidityStress,
                LightStress = lightStress,
                NutrientStress = 0f, // Default
                PestStress = 0f,     // Default
                DiseaseStress = 0f   // Default
            };

            ApplyEnvironmentalStressIndicators(plantId, indicators);
        }

        /// <summary>
        /// Get stress visualization statistics
        /// </summary>
        public StressVisualizationStatistics GetStatistics()
        {
            int totalPlants = _plantStressData.Count;
            int healthyPlants = 0;
            int stressedPlants = 0;
            float averageHealth = 0f;
            float averageStress = 0f;

            foreach (var data in _plantStressData.Values)
            {
                averageHealth += data.HealthLevel;
                averageStress += data.StressLevel;

                if (data.HealthLevel > 0.7f)
                    healthyPlants++;
                else if (data.StressLevel > 0.5f)
                    stressedPlants++;
            }

            if (totalPlants > 0)
            {
                averageHealth /= totalPlants;
                averageStress /= totalPlants;
            }

            return new StressVisualizationStatistics
            {
                TotalPlants = totalPlants,
                HealthyPlants = healthyPlants,
                StressedPlants = stressedPlants,
                AverageHealthLevel = averageHealth,
                AverageStressLevel = averageStress,
                VisualizationEnabled = _enableStressVisualization,
                UpdateFrequency = _visualizationUpdateFrequency
            };
        }

        /// <summary>
        /// Enable or disable stress visualization
        /// </summary>
        public bool EnableStressVisualization
        {
            get => _enableStressVisualization;
            set => _enableStressVisualization = value;
        }

        /// <summary>
        /// Reset all stress visualization data
        /// </summary>
        public void ResetVisualization()
        {
            _plantStressData.Clear();
            _plantsNeedingVisualizationUpdate.Clear();
            ChimeraLogger.Log("SPEEDTREE/STRESS", "Reset stress visualization data", this);
        }

        /// <summary>
        /// Gets current stress visualization data for a plant
        /// </summary>
        public PlantStressVisualization GetPlantStressVisualization(int plantId)
        {
            if (_plantStressData.TryGetValue(plantId, out var data))
            {
                return data;
            }
            return new PlantStressVisualization(plantId);
        }

        /// <summary>
        /// Gets all plants needing visualization updates
        /// </summary>
        public IReadOnlyList<int> GetPlantsNeedingVisualizationUpdate()
        {
            return _plantsNeedingVisualizationUpdate.AsReadOnly();
        }

        /// <summary>
        /// Clears the visualization update queue
        /// </summary>
        public void ClearVisualizationUpdateQueue()
        {
            _plantsNeedingVisualizationUpdate.Clear();
        }

        /// <summary>
        /// Sets the stress visualization intensity
        /// </summary>
        public void SetVisualizationIntensity(float intensity)
        {
            _stressVisualizationIntensity = Mathf.Max(0f, intensity);
        }

        #endregion

        #region Update Loop

        public void Tick(float deltaTime)
        {
            if (!_enableStressVisualization) return;

            _lastVisualizationUpdate += deltaTime;

            if (_lastVisualizationUpdate >= _visualizationUpdateFrequency)
            {
                // Process plants needing visualization updates
                foreach (var plantId in _plantsNeedingVisualizationUpdate.ToArray())
                {
                    ApplyStressVisualization(plantId);
                }

                // Clear update queue after processing
                _plantsNeedingVisualizationUpdate.Clear();

                _lastVisualizationUpdate = 0f;
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Plant stress visualization data
    /// </summary>
    [Serializable]
}
