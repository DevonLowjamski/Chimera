using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Core types and enumerations for SpeedTree environmental system
    /// </summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>
    /// Environmental response data for individual plants
    /// </summary>
    [System.Serializable]
    public class EnvironmentalResponseData
    {
        [Header("Plant Response")]
        public int PlantId;
        public float HealthResponse = 1f;
        public float GrowthResponse = 1f;
        public float StressResponse = 0f;
        public Color LeafColor = Color.green;

        [Header("Environmental Factors")]
        public float TemperatureInfluence = 0f;
        public float HumidityInfluence = 0f;
        public float LightInfluence = 0f;
        public float WindInfluence = 0f;

        [Header("Response Metrics")]
        public float OverallResponseStrength = 1f;
        public float AdaptationRate = 0.1f;
        public bool IsStressed = false;

        /// <summary>
        /// Update response based on environmental conditions
        /// </summary>
        public void UpdateResponse(float temperature, float humidity, float light, float wind)
        {
            TemperatureInfluence = CalculateTemperatureInfluence(temperature);
            HumidityInfluence = CalculateHumidityInfluence(humidity);
            LightInfluence = CalculateLightInfluence(light);
            WindInfluence = CalculateWindInfluence(wind);

            // Calculate overall response
            OverallResponseStrength = (TemperatureInfluence + HumidityInfluence + LightInfluence - WindInfluence) / 4f;
            OverallResponseStrength = Mathf.Clamp01(OverallResponseStrength + 0.5f); // Shift to 0-1 range

            // Update stress status
            IsStressed = OverallResponseStrength < 0.3f;

            // Update visual response
            UpdateVisualResponse();
        }

        private float CalculateTemperatureInfluence(float temperature)
        {
            // Optimal temperature range: 20-25°C
            float optimalTemp = 22.5f;
            float deviation = Mathf.Abs(temperature - optimalTemp);
            return Mathf.Clamp01(1f - (deviation / 15f)); // Max deviation 15°C
        }

        private float CalculateHumidityInfluence(float humidity)
        {
            // Optimal humidity range: 40-60%
            float optimalHumidity = 50f;
            float deviation = Mathf.Abs(humidity - optimalHumidity);
            return Mathf.Clamp01(1f - (deviation / 30f)); // Max deviation 30%
        }

        private float CalculateLightInfluence(float light)
        {
            // Optimal light range: 400-600 μmol/m²/s
            float optimalLight = 500f;
            float ratio = light / optimalLight;
            return Mathf.Clamp01(ratio > 1f ? 1f / ratio : ratio); // Penalty for too much light
        }

        private float CalculateWindInfluence(float wind)
        {
            // Wind is generally detrimental
            return Mathf.Clamp01(1f - (wind / 20f)); // Max wind 20 m/s
        }

        private void UpdateVisualResponse()
        {
            if (IsStressed)
            {
                // Stressed plants show discoloration
                LeafColor = Color.Lerp(Color.green, Color.yellow, 1f - OverallResponseStrength);
            }
            else
            {
                // Healthy plants show vibrant green
                LeafColor = Color.Lerp(Color.green, Color.green * 1.2f, OverallResponseStrength);
            }
        }

        /// <summary>
        /// Get response summary
        /// </summary>
        public string GetResponseSummary()
        {
            return $"Health: {OverallResponseStrength:P0} | Stress: {(IsStressed ? "Yes" : "No")} | Temp: {TemperatureInfluence:P0} | Humidity: {HumidityInfluence:P0}";
        }
    }

    /// <summary>
    /// Stress visualization data
    /// </summary>
    [System.Serializable]
    public class StressVisualizationData
    {
        [Header("Visualization Settings")]
        public Gradient HealthGradient = new Gradient();
        public Gradient StressGradient = new Gradient();
        public float VisualizationIntensity = 1f;
        public bool EnableColorShifting = true;
        public bool EnableLeafDropping = false;

        [Header("Current State")]
        public float CurrentStressLevel = 0f;
        public Color CurrentLeafColor = Color.green;
        public float LeafDropRate = 0f;

        /// <summary>
        /// Initialize default gradients
        /// </summary>
        public void InitializeDefaults()
        {
            // Health gradient: Yellow -> Green -> Dark Green
            HealthGradient = new Gradient();
            var healthColors = new GradientColorKey[]
            {
                new GradientColorKey(Color.yellow, 0f),
                new GradientColorKey(Color.green, 0.5f),
                new GradientColorKey(new Color(0.2f, 0.4f, 0.2f), 1f)
            };
            var healthAlphas = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            HealthGradient.SetKeys(healthColors, healthAlphas);

            // Stress gradient: Green -> Yellow -> Red
            StressGradient = new Gradient();
            var stressColors = new GradientColorKey[]
            {
                new GradientColorKey(Color.green, 0f),
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(Color.red, 1f)
            };
            var stressAlphas = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            StressGradient.SetKeys(stressColors, stressAlphas);
        }

        /// <summary>
        /// Update visualization based on stress level
        /// </summary>
        public void UpdateVisualization(float stressLevel)
        {
            CurrentStressLevel = Mathf.Clamp01(stressLevel);

            if (EnableColorShifting)
            {
                // Use stress gradient for stressed plants, health gradient for healthy
                if (stressLevel > 0.5f)
                {
                    CurrentLeafColor = StressGradient.Evaluate((stressLevel - 0.5f) * 2f);
                }
                else
                {
                    CurrentLeafColor = HealthGradient.Evaluate(stressLevel * 2f);
                }

                CurrentLeafColor *= VisualizationIntensity;
            }

            if (EnableLeafDropping)
            {
                LeafDropRate = Mathf.Clamp01(stressLevel - 0.7f); // Start dropping leaves at 70% stress
            }
        }

        /// <summary>
        /// Get visualization settings summary
        /// </summary>
        public string GetVisualizationSummary()
        {
            return $"Stress: {CurrentStressLevel:P0} | Color: {CurrentLeafColor} | Leaf Drop: {(EnableLeafDropping ? $"{LeafDropRate:P0}" : "Disabled")}";
        }
    }

    /// <summary>
    /// Wind settings for SpeedTree objects
    /// </summary>
    [System.Serializable]
    public class SpeedTreeWindSettings
    {
        [Header("Wind Properties")]
        public float BaseWindStrength = 1f;
        public Vector3 WindDirection = Vector3.right;
        public float WindFrequency = 1f;
        public float WindTurbulence = 0.1f;

        [Header("Animation Settings")]
        public float TrunkFlexibility = 1f;
        public float BranchFlexibility = 1f;
        public float LeafFlutter = 1f;
        public float GlobalWindMultiplier = 1f;

        [Header("Dynamic Properties")]
        public float CurrentWindStrength = 0f;
        public Vector3 CurrentWindDirection = Vector3.zero;
        public float WindGustStrength = 0f;

        /// <summary>
        /// Update wind settings based on environmental conditions
        /// </summary>
        public void UpdateFromEnvironment(float windSpeed, Vector3 windDir, float turbulence)
        {
            CurrentWindStrength = windSpeed * GlobalWindMultiplier;
            CurrentWindDirection = windDir.normalized;
            WindTurbulence = turbulence;

            // Add gust effects
            WindGustStrength = CurrentWindStrength * (1f + Mathf.Sin(Time.time * WindFrequency) * WindTurbulence);
        }

        /// <summary>
        /// Get wind animation parameters
        /// </summary>
        public Vector4 GetWindAnimationParams()
        {
            return new Vector4(
                WindGustStrength,
                CurrentWindDirection.x,
                CurrentWindDirection.z,
                TrunkFlexibility
            );
        }

        /// <summary>
        /// Get wind summary
        /// </summary>
        public string GetWindSummary()
        {
            return $"Strength: {CurrentWindStrength:F1} | Direction: {CurrentWindDirection} | Gust: {WindGustStrength:F1}";
        }
    }

    /// <summary>
    /// Seasonal effects data
    /// </summary>
    [System.Serializable]
    public class SeasonalEffects
    {
        [Header("Season Configuration")]
        public Season CurrentSeason = Season.Spring;
        public float SeasonalTransitionDuration = 30f; // days
        public float TransitionProgress = 0f;

        [Header("Seasonal Parameters")]
        public Color SpringLeafColor = new Color(0.4f, 0.8f, 0.4f);
        public Color SummerLeafColor = new Color(0.2f, 0.6f, 0.2f);
        public Color AutumnLeafColor = new Color(0.8f, 0.4f, 0.1f);
        public Color WinterLeafColor = new Color(0.5f, 0.3f, 0.1f);

        [Header("Growth Modifiers")]
        public float SpringGrowthModifier = 1.2f;
        public float SummerGrowthModifier = 1.0f;
        public float AutumnGrowthModifier = 0.8f;
        public float WinterGrowthModifier = 0.3f;

        [Header("Environmental Effects")]
        public float SeasonalTemperatureModifier = 0f;
        public float SeasonalHumidityModifier = 0f;
        public float SeasonalLightModifier = 1f;

        /// <summary>
        /// Update seasonal effects
        /// </summary>
        public void UpdateSeasonalEffects(float deltaTime)
        {
            // Update transition progress (simplified - would use actual time system)
            TransitionProgress += deltaTime / SeasonalTransitionDuration;
            if (TransitionProgress >= 1f)
            {
                TransitionProgress = 0f;
                AdvanceSeason();
            }
        }

        /// <summary>
        /// Advance to next season
        /// </summary>
        private void AdvanceSeason()
        {
            CurrentSeason = (Season)(((int)CurrentSeason + 1) % 4);

            // Update seasonal modifiers based on new season
            UpdateSeasonalModifiers();
        }

        /// <summary>
        /// Update seasonal modifiers
        /// </summary>
        private void UpdateSeasonalModifiers()
        {
            switch (CurrentSeason)
            {
                case Season.Spring:
                    SeasonalTemperatureModifier = -5f;
                    SeasonalHumidityModifier = 10f;
                    SeasonalLightModifier = 0.9f;
                    break;
                case Season.Summer:
                    SeasonalTemperatureModifier = 5f;
                    SeasonalHumidityModifier = -10f;
                    SeasonalLightModifier = 1.0f;
                    break;
                case Season.Autumn:
                    SeasonalTemperatureModifier = 0f;
                    SeasonalHumidityModifier = 5f;
                    SeasonalLightModifier = 0.8f;
                    break;
                case Season.Winter:
                    SeasonalTemperatureModifier = -10f;
                    SeasonalHumidityModifier = 15f;
                    SeasonalLightModifier = 0.6f;
                    break;
            }
        }

        /// <summary>
        /// Get current seasonal color
        /// </summary>
        public Color GetSeasonalColor()
        {
            switch (CurrentSeason)
            {
                case Season.Spring: return SpringLeafColor;
                case Season.Summer: return SummerLeafColor;
                case Season.Autumn: return AutumnLeafColor;
                case Season.Winter: return WinterLeafColor;
                default: return Color.green;
            }
        }

        /// <summary>
        /// Get current growth modifier
        /// </summary>
        public float GetGrowthModifier()
        {
            switch (CurrentSeason)
            {
                case Season.Spring: return SpringGrowthModifier;
                case Season.Summer: return SummerGrowthModifier;
                case Season.Autumn: return AutumnGrowthModifier;
                case Season.Winter: return WinterGrowthModifier;
                default: return 1f;
            }
        }

        /// <summary>
        /// Get seasonal effects summary
        /// </summary>
        public string GetSeasonalSummary()
        {
            return $"{CurrentSeason} | Growth: {GetGrowthModifier():F1}x | Temp: {SeasonalTemperatureModifier:+0;-#}°C | Light: {SeasonalLightModifier:P0}";
        }
    }

    /// <summary>
    /// Interface for SpeedTree Environmental Service
    /// </summary>
    public interface ISpeedTreeEnvironmentalService
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
        void UpdateEnvironment(float deltaTime);
        EnvironmentalResponseData GetEnvironmentalResponse(int plantId);
        void SetSeason(Season season);
        void UpdateWindSettings(float windSpeed, Vector3 windDirection, float turbulence);
        void EnableStressVisualization(bool enabled);
        SeasonalEffects GetSeasonalEffects();
    }

    // Minimal shared types used across environmental services
    [System.Serializable]
    public struct WindState
    {
        public float CurrentStrength;
        public Vector3 CurrentDirection;
        public float GustStrength;
        public bool Active;
        public System.DateTime LastStateUpdate;

        // Aliases for backward compatibility
        public float Strength { get => CurrentStrength; set => CurrentStrength = value; }
        public Vector3 Direction { get => CurrentDirection; set => CurrentDirection = value; }
        public bool IsActive { get => Active; set => Active = value; }
        public System.DateTime LastUpdate { get => LastStateUpdate; set => LastStateUpdate = value; }
    }

    [System.Serializable]
    public struct SeasonalState
    {
        public Season CurrentSeason;
        public float TransitionProgress;
        public bool IsTransitioning;
    }


    [System.Serializable]
    public class EnvironmentalStatistics
    {
        public int TotalPlantsMonitored;
        public int PlantsStressed;
        public bool SystemsEnabled;
    }

    [System.Serializable]
    public class StressVisualizationStatistics
    {
        public int TotalPlants;
        public int HealthyPlants;
        public int StressedPlants;
        public int TrackedPlants;
        public float AverageHealthLevel;
        public float AverageStressLevel;
        public float AverageStress;
        public float MaxStress;
        public bool VisualizationEnabled;
        public float UpdateFrequency;
    }
}
