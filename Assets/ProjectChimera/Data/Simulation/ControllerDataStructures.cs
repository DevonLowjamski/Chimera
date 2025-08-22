using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Environment
{
    [System.Serializable]
    public class PostProcessingController
    {
        public bool isActive;
        public float intensity;
        public Dictionary<string, object> effects;
        
        // Depth of Field settings
        public bool depthOfFieldEnabled = false;
        public float depthOfFieldFocusDistance = 1f;
        
        // Vignette settings
        public float vignetteIntensity = 0f;
        public bool vignetteEnabled = false;
        
        // Film Grain settings
        public bool filmGrainEnabled = false;
        public float filmGrainIntensity = 0f;
        
        // Color Grading settings
        public bool colorGradingEnabled = false;
        public float colorGradingIntensity = 1f;
        
        /// <summary>
        /// Enable or disable depth of field effect
        /// </summary>
        public void SetDepthOfFieldEnabled(bool enabled)
        {
            depthOfFieldEnabled = enabled;
        }
        
        /// <summary>
        /// Set vignette intensity
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = Mathf.Clamp01(intensity);
            vignetteEnabled = intensity > 0f;
        }
        
        /// <summary>
        /// Enable or disable film grain effect
        /// </summary>
        public void SetFilmGrainEnabled(bool enabled)
        {
            filmGrainEnabled = enabled;
        }
        
        /// <summary>
        /// Enable or disable color grading
        /// </summary>
        public void SetColorGradingEnabled(bool enabled)
        {
            colorGradingEnabled = enabled;
        }
        
        /// <summary>
        /// Reset all post-processing effects to default values
        /// </summary>
        public void ResetToDefaults()
        {
            isActive = false;
            intensity = 1f;
            depthOfFieldEnabled = false;
            depthOfFieldFocusDistance = 1f;
            vignetteIntensity = 0f;
            vignetteEnabled = false;
            filmGrainEnabled = false;
            filmGrainIntensity = 0f;
            colorGradingEnabled = false;
            colorGradingIntensity = 1f;
            
            if (effects != null)
            {
                effects.Clear();
            }
        }
    }

    [System.Serializable]
    public class EnvironmentalSensor
    {
        public string sensorId;
        public string sensorType;
        public Vector3 position;
        public bool isActive;
        public float lastReading;
        public DateTime lastUpdate;

        public void Initialize(string id, string type, Vector3 pos)
        {
            sensorId = id;
            sensorType = type;
            position = pos;
            isActive = true;
            lastUpdate = DateTime.Now;
        }
    }

    [System.Serializable]
    public class LightSpectrum
    {
        public float redPercentage;
        public float bluePercentage;
        public float greenPercentage;
        public float farRedPercentage;
        public float uvPercentage;
        public float irPercentage;
        
        // Compatibility properties for LightingController
        public float Red 
        { 
            get => redPercentage; 
            set => redPercentage = value; 
        }
        
        public float Blue 
        { 
            get => bluePercentage; 
            set => bluePercentage = value; 
        }
        
        public float Green 
        { 
            get => greenPercentage; 
            set => greenPercentage = value; 
        }
        
        public float FarRed 
        { 
            get => farRedPercentage; 
            set => farRedPercentage = value; 
        }
        
        public float UVA 
        { 
            get => uvPercentage; 
            set => uvPercentage = value; 
        }
        
        public float UVB 
        { 
            get => irPercentage; 
            set => irPercentage = value; 
        }
        
        public float WhiteBalance { get; set; } = 0.08f;
        
        // Percentage properties for compatibility with EnvironmentalOrchestrator
        public float RedPercent 
        { 
            get => redPercentage; 
            set => redPercentage = value; 
        }
        
        public float BluePercent 
        { 
            get => bluePercentage; 
            set => bluePercentage = value; 
        }
        
        public float GreenPercent 
        { 
            get => greenPercentage; 
            set => greenPercentage = value; 
        }
        
        public float FarRedPercent 
        { 
            get => farRedPercentage; 
            set => farRedPercentage = value; 
        }
        
        public float UVPercent 
        { 
            get => uvPercentage; 
            set => uvPercentage = value; 
        }
    }

    [System.Serializable]
    public class HVACController
    {
        public bool isActive;
        public float targetTemperature;
        public float targetHumidity;
        public float currentTemperature;
        public float currentHumidity;
        public float fanSpeed;
        public bool heatingEnabled;
        public bool coolingEnabled;

        public void SetTargetTemperature(float temperature)
        {
            targetTemperature = temperature;
        }

        public void SetTargetHumidity(float humidity)
        {
            targetHumidity = humidity;
        }
    }

    [System.Serializable]
    public class LightingController
    {
        public bool isActive;
        public float intensity;
        public float targetIntensity;
        public LightSpectrum spectrum;
        public TimeSpan dailySchedule;
        public bool isDimming;
        public string scheduleName;

        public void SetTargetIntensity(float newIntensity)
        {
            targetIntensity = newIntensity;
        }

        public void SetLightSchedule(string name)
        {
            scheduleName = name;
        }
    }

    [System.Serializable]
    public class VentilationController
    {
        public bool isActive;
        public float fanSpeed;
        public float targetAirFlow;
        public float airFlowRate;
        public bool exhaustFanActive;
        public bool intakeFanActive;

        public void SetTargetAirFlow(float airFlow)
        {
            targetAirFlow = airFlow;
        }
    }

    [System.Serializable]
    public class IrrigationController
    {
        public bool isActive;
        public float waterFlowRate;
        public TimeSpan scheduleInterval;
        public float phLevel;
        public float nutrientConcentration;
    }
}