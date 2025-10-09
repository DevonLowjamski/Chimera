// REFACTORED: Environmental Stress Calculator
// Extracted from EnvironmentalResponseSystem for better separation of concerns

using UnityEngine;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Calculates environmental stress factors for plants
    /// </summary>
    public static class EnvironmentalStressCalculator
    {
        /// <summary>
        /// Calculate temperature-induced stress
        /// </summary>
        public static float CalculateTemperatureStress(float currentTemp, float baselineTemp)
        {
            float deviation = Mathf.Abs(currentTemp - baselineTemp);
            return Mathf.Clamp01(deviation / 10f); // 10°C deviation = max stress
        }

        /// <summary>
        /// Calculate humidity-induced stress
        /// </summary>
        public static float CalculateHumidityStress(float currentHumidity, float baselineHumidity)
        {
            float deviation = Mathf.Abs(currentHumidity - baselineHumidity);
            return Mathf.Clamp01(deviation / 30f); // 30% deviation = max stress
        }

        /// <summary>
        /// Calculate light-induced stress
        /// </summary>
        public static float CalculateLight Stress(float currentLight, float baselineLight)
        {
            float deviation = Mathf.Abs(currentLight - baselineLight);
            return Mathf.Clamp01(deviation / 500f); // 500 PPFD deviation = max stress
        }

        /// <summary>
        /// Calculate CO2-induced stress
        /// </summary>
        public static float CalculateCO2Stress(float currentCO2, float baselineCO2)
        {
            float deviation = Mathf.Abs(currentCO2 - baselineCO2);
            return Mathf.Clamp01(deviation / 400f); // 400 ppm deviation = max stress
        }

        /// <summary>
        /// Calculate overall stress from individual stress factors
        /// </summary>
        public static float CalculateOverallStress(float temperatureStress, float humidityStress, 
                                                   float lightStress, float co2Stress)
        {
            // Weighted average of stress factors
            float weightedStress = (temperatureStress * 0.3f) + 
                                  (humidityStress * 0.25f) + 
                                  (lightStress * 0.3f) + 
                                  (co2Stress * 0.15f);
            return Mathf.Clamp01(weightedStress);
        }

        /// <summary>
        /// Calculate temperature adaptation rate
        /// </summary>
        public static float CalculateTemperatureAdaptation(float currentTemp, float adaptedTemp, float adaptationRate)
        {
            float difference = currentTemp - adaptedTemp;
            return Mathf.Lerp(adaptedTemp, currentTemp, adaptationRate);
        }

        /// <summary>
        /// Calculate humidity adaptation rate
        /// </summary>
        public static float CalculateHumidityAdaptation(float currentHumidity, float adaptedHumidity, float adaptationRate)
        {
            float difference = currentHumidity - adaptedHumidity;
            return Mathf.Lerp(adaptedHumidity, currentHumidity, adaptationRate);
        }

        /// <summary>
        /// Calculate light adaptation rate
        /// </summary>
        public static float CalculateLightAdaptation(float currentLight, float adaptedLight, float adaptationRate)
        {
            return Mathf.Lerp(adaptedLight, currentLight, adaptationRate);
        }

        /// <summary>
        /// Calculate CO2 adaptation rate
        /// </summary>
        public static float CalculateCO2Adaptation(float currentCO2, float adaptedCO2, float adaptationRate)
        {
            return Mathf.Lerp(adaptedCO2, currentCO2, adaptationRate);
        }

        /// <summary>
        /// Calculate overall adaptation progress
        /// </summary>
        public static float CalculateAdaptationProgress(float temperatureAdapted, float humidityAdapted,
                                                       float lightAdapted, float co2Adapted)
        {
            // Average of all adaptation values
            return (temperatureAdapted + humidityAdapted + lightAdapted + co2Adapted) / 4f;
        }
    }
}

