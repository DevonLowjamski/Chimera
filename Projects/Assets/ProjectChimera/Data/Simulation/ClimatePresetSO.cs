using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation
{
    /// <summary>
    /// Simple season enumeration for climate variations
    /// </summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
    /// <summary>
    /// Defines a complete climate preset with environmental parameters, seasonal variations, and diurnal cycles.
    /// Used for different geographical regions, cultivation methods, and facility types.
    /// </summary>
    [CreateAssetMenu(fileName = "New Climate Preset", menuName = "Project Chimera/Simulation/Climate Preset")]
    public class ClimatePresetSO : ChimeraDataSO
    {
        [Header("Climate Identity")]
        [SerializeField] private string _climateName;
        [SerializeField] private ClimateType _climateType = ClimateType.Temperate;
        [SerializeField] private CultivationEnvironment _cultivationEnvironment = CultivationEnvironment.Indoor;
        [SerializeField, TextArea(3, 5)] private string _climateDescription;
        
        [Header("Base Environmental Parameters")]
        [SerializeField] private EnvironmentalParametersSO _baseParameters;
        
        [Header("Seasonal Variations")]
        [SerializeField] private bool _hasSeasonalVariations = true;
        [SerializeField] private List<SeasonalProfile> _seasonalProfiles = new List<SeasonalProfile>();
        
        [Header("Diurnal Cycles")]
        [SerializeField] private bool _hasDiurnalCycles = true;
        [SerializeField] private DiurnalCycle _temperatureCycle;
        [SerializeField] private DiurnalCycle _humidityCycle;
        [SerializeField] private LightCycle _lightCycle;
        
        [Header("Weather Patterns")]
        [SerializeField] private bool _hasWeatherVariability = false;
        [SerializeField] private List<WeatherPattern> _weatherPatterns = new List<WeatherPattern>();
        [SerializeField, Range(0f, 1f)] private float _weatherStability = 0.8f;
        
        [Header("Regional Characteristics")]
        [SerializeField] private float _altitude = 0f; // meters above sea level
        [SerializeField] private float _latitude = 40f; // degrees
        [SerializeField] private float _averageAirPressure = 101.3f; // kPa
        [SerializeField] private Vector2 _annualRainfallRange = new Vector2(500f, 1000f); // mm
        
        [Header("Cultivation Suitability")]
        [SerializeField] private List<CultivationSuitability> _suitabilityRatings = new List<CultivationSuitability>();
        [SerializeField, Range(1, 5)] private int _difficultyRating = 3;
        [SerializeField, Range(0f, 1f)] private float _stabilityRating = 0.7f;
        
        // Public Properties
        public string ClimateName => _climateName;
        public ClimateType ClimateType => _climateType;
        public CultivationEnvironment CultivationEnvironment => _cultivationEnvironment;
        public string ClimateDescription => _climateDescription;
        public EnvironmentalParametersSO BaseParameters => _baseParameters;
        public bool HasSeasonalVariations => _hasSeasonalVariations;
        public List<SeasonalProfile> SeasonalProfiles => _seasonalProfiles;
        public bool HasDiurnalCycles => _hasDiurnalCycles;
        public DiurnalCycle TemperatureCycle => _temperatureCycle;
        public DiurnalCycle HumidityCycle => _humidityCycle;
        public LightCycle LightCycle => _lightCycle;
        public bool HasWeatherVariability => _hasWeatherVariability;
        public List<WeatherPattern> WeatherPatterns => _weatherPatterns;
        public float WeatherStability => _weatherStability;
        public float Altitude => _altitude;
        public float Latitude => _latitude;
        public float AverageAirPressure => _averageAirPressure;
        public Vector2 AnnualRainfallRange => _annualRainfallRange;
        public List<CultivationSuitability> SuitabilityRatings => _suitabilityRatings;
        public int DifficultyRating => _difficultyRating;
        public float StabilityRating => _stabilityRating;
        
        /// <summary>
        /// Gets the current environmental conditions based on time of day and season.
        /// </summary>
        /// <param name="dayOfYear">Day of the year (1-365)</param>
        /// <param name="timeOfDay">Time of day (0-24 hours)</param>
        /// <returns>Current environmental conditions</returns>
        public EnvironmentalConditions GetCurrentConditions(int dayOfYear, float timeOfDay)
        {
            var conditions = new EnvironmentalConditions();
            
            // Start with base parameters
            if (_baseParameters == null) return conditions;
            
            float temperature = _baseParameters.OptimalTemperature;
            float humidity = _baseParameters.OptimalHumidity;
            float lightIntensity = _baseParameters.OptimalLightIntensity;
            float co2Level = _baseParameters.OptimalCO2;
            
            // Apply seasonal variations
            if (_hasSeasonalVariations)
            {
                var seasonalModifier = GetSeasonalModifier(dayOfYear);
                temperature += seasonalModifier.TemperatureOffset;
                humidity += seasonalModifier.HumidityOffset;
                lightIntensity *= seasonalModifier.LightMultiplier;
            }
            
            // Apply diurnal cycles
            if (_hasDiurnalCycles)
            {
                temperature += _temperatureCycle.GetValueAtTime(timeOfDay);
                humidity += _humidityCycle.GetValueAtTime(timeOfDay);
                lightIntensity = _lightCycle.GetLightIntensityAtTime(timeOfDay, dayOfYear);
            }
            
            // Apply weather variability
            if (_hasWeatherVariability)
            {
                var weatherModifier = GetWeatherModifier();
                temperature *= weatherModifier.TemperatureMultiplier;
                humidity *= weatherModifier.HumidityMultiplier;
                lightIntensity *= weatherModifier.LightMultiplier;
            }
            
            conditions.Temperature = temperature;
            conditions.Humidity = humidity;
            conditions.LightIntensity = lightIntensity;
            conditions.CO2Level = co2Level;
            
            return conditions;
        }
        
        /// <summary>
        /// Evaluates cultivation suitability for a specific strain type.
        /// </summary>
        public float EvaluateCultivationSuitability(StrainType strainType)
        {
            var suitability = _suitabilityRatings.Find(s => s.StrainType == strainType);
            return suitability?.SuitabilityScore ?? 0.5f;
        }
        
        private SeasonalModifier GetSeasonalModifier(int dayOfYear)
        {
            // Determine current season based on day of year (Northern Hemisphere)
            Season currentSeason = Season.Spring;
            if (dayOfYear >= 80 && dayOfYear < 172) currentSeason = Season.Spring;
            else if (dayOfYear >= 172 && dayOfYear < 264) currentSeason = Season.Summer;
            else if (dayOfYear >= 264 && dayOfYear < 355) currentSeason = Season.Autumn;
            else currentSeason = Season.Winter;
            
            var seasonalProfile = _seasonalProfiles.Find(sp => sp.Season == currentSeason);
            return seasonalProfile?.Modifier ?? new SeasonalModifier();
        }
        
        private WeatherModifier GetWeatherModifier()
        {
            if (_weatherPatterns.Count == 0) return new WeatherModifier();
            
            // Simple random weather selection weighted by frequency
            float randomValue = UnityEngine.Random.value;
            float cumulativeFrequency = 0f;
            
            foreach (var pattern in _weatherPatterns)
            {
                cumulativeFrequency += pattern.Frequency;
                if (randomValue <= cumulativeFrequency)
                {
                    return pattern.Modifier;
                }
            }
            
            return _weatherPatterns[0].Modifier;
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_climateName))
            {
                Debug.LogError($"Climate Preset {name}: Climate name cannot be empty", this);
                isValid = false;
            }
                
            if (_baseParameters == null)
            {
                Debug.LogError($"Climate Preset {name}: Base parameters must be assigned", this);
                isValid = false;
            }
                
            if (_hasSeasonalVariations && _seasonalProfiles.Count == 0)
            {
                Debug.LogWarning($"Climate Preset {name}: Seasonal variations enabled but no profiles defined", this);
                isValid = false;
            }
                
            if (_hasWeatherVariability && _weatherPatterns.Count == 0)
            {
                Debug.LogWarning($"Climate Preset {name}: Weather variability enabled but no patterns defined", this);
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    [System.Serializable]
    public class SeasonalProfile
    {
        public Season Season;
        public SeasonalModifier Modifier;
        [TextArea(2, 3)] public string SeasonDescription;
    }
    
    [System.Serializable]
    public class SeasonalModifier
    {
        [Range(-15f, 15f)] public float TemperatureOffset = 0f;
        [Range(-30f, 30f)] public float HumidityOffset = 0f;
        [Range(0.3f, 2f)] public float LightMultiplier = 1f;
        [Range(0.8f, 1.2f)] public float CO2Multiplier = 1f;
    }
    
    [System.Serializable]
    public class DiurnalCycle
    {
        public AnimationCurve CycleCurve; // 24-hour cycle (0-24 on X-axis)
        [Range(0f, 20f)] public float Amplitude = 5f; // Maximum variation from baseline
        public string CycleDescription;
        
        public float GetValueAtTime(float timeOfDay)
        {
            if (CycleCurve == null) return 0f;
            
            float normalizedTime = timeOfDay / 24f;
            return CycleCurve.Evaluate(normalizedTime) * Amplitude;
        }
    }
    
    [System.Serializable]
    public class LightCycle
    {
        public AnimationCurve DaylightCurve; // 24-hour daylight cycle
        [Range(6f, 18f)] public float DaylightHours = 12f;
        [Range(0f, 1200f)] public float MaxIntensity = 800f;
        public bool HasSeasonalVariation = true;
        
        public float GetLightIntensityAtTime(float timeOfDay, int dayOfYear)
        {
            if (DaylightCurve == null) return 0f;
            
            // Adjust daylight hours based on season if enabled
            float adjustedDaylightHours = DaylightHours;
            if (HasSeasonalVariation)
            {
                // Simple seasonal adjustment (more sophisticated could use latitude)
                float seasonalAdjustment = Mathf.Sin((dayOfYear - 80f) / 365f * 2f * Mathf.PI) * 2f;
                adjustedDaylightHours += seasonalAdjustment;
            }
            
            float sunrise = 12f - adjustedDaylightHours / 2f;
            float sunset = 12f + adjustedDaylightHours / 2f;
            
            if (timeOfDay < sunrise || timeOfDay > sunset) return 0f;
            
            float normalizedTime = (timeOfDay - sunrise) / adjustedDaylightHours;
            return DaylightCurve.Evaluate(normalizedTime) * MaxIntensity;
        }
    }
    
    [System.Serializable]
    public class WeatherPattern
    {
        public string PatternName;
        [Range(0f, 1f)] public float Frequency = 0.1f;
        public WeatherModifier Modifier;
        [TextArea(2, 3)] public string PatternDescription;
    }
    
    [System.Serializable]
    public class WeatherModifier
    {
        [Range(0.5f, 1.5f)] public float TemperatureMultiplier = 1f;
        [Range(0.5f, 2f)] public float HumidityMultiplier = 1f;
        [Range(0.1f, 1.5f)] public float LightMultiplier = 1f;
        [Range(0.9f, 1.1f)] public float CO2Multiplier = 1f;
        [Range(1, 7)] public int DurationDays = 1;
    }
    
    [System.Serializable]
    public class CultivationSuitability
    {
        public StrainType StrainType;
        [Range(0f, 1f)] public float SuitabilityScore = 0.5f;
        [TextArea(2, 3)] public string SuitabilityNotes;
    }
    
    public enum ClimateType
    {
        Tropical,
        Subtropical,
        Temperate,
        Continental,
        Arctic,
        Arid,
        Mediterranean,
        Oceanic,
        Controlled // For indoor/greenhouse environments
    }
    
    public enum CultivationEnvironment
    {
        Indoor,
        Greenhouse,
        Outdoor,
        Polytunnel,
        ColdFrame,
        Hydroponic,
        Aeroponic
    }
}