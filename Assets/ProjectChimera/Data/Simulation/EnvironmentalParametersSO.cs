using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;

namespace ProjectChimera.Data.Simulation
{
    /// <summary>
    /// Defines optimal environmental parameters and ranges for plant growth.
    /// Used as templates for different cultivation environments and growth stages.
    /// </summary>
    [CreateAssetMenu(fileName = "New Environmental Parameters", menuName = "Project Chimera/Environment/Environmental Parameters")]
    public class EnvironmentalParametersSO : ChimeraDataSO
    {
        [Header("Temperature Parameters")]
        [SerializeField] private Vector2 _temperatureRange = new Vector2(20f, 26f);
        [SerializeField] private float _optimalTemperature = 23f;
        [SerializeField] private float _maxTemperatureTolerance = 35f;
        [SerializeField] private float _minTemperatureTolerance = 10f;
        [SerializeField] private AnimationCurve _temperatureResponseCurve;
        
        [Header("Humidity Parameters")]
        [SerializeField] private Vector2 _humidityRange = new Vector2(40f, 70f);
        [SerializeField] private float _optimalHumidity = 55f;
        [SerializeField] private float _maxHumidityTolerance = 90f;
        [SerializeField] private float _minHumidityTolerance = 20f;
        [SerializeField] private AnimationCurve _humidityResponseCurve;
        
        [Header("Light Parameters")]
        [SerializeField] private Vector2 _lightIntensityRange = new Vector2(300f, 800f); // PPFD µmol/m²/s
        [SerializeField] private float _optimalLightIntensity = 600f;
        [SerializeField] private Vector2 _dailyLightIntegralRange = new Vector2(25f, 50f); // DLI mol/m²/day
        [SerializeField] private float _optimalDLI = 35f;
        [SerializeField] private AnimationCurve _lightResponseCurve;
        
        [Header("CO2 Parameters")]
        [SerializeField] private Vector2 _co2Range = new Vector2(400f, 1200f); // ppm
        [SerializeField] private float _optimalCO2 = 800f;
        [SerializeField] private float _maxCO2Tolerance = 1500f;
        [SerializeField] private float _minCO2Tolerance = 300f;
        [SerializeField] private AnimationCurve _co2ResponseCurve;
        
        [Header("Air Circulation")]
        [SerializeField] private Vector2 _airVelocityRange = new Vector2(0.1f, 0.5f); // m/s
        [SerializeField] private float _optimalAirVelocity = 0.3f;
        [SerializeField] private bool _requiresAirMovement = true;
        
        [Header("VPD (Vapor Pressure Deficit)")]
        [SerializeField] private Vector2 _vpdRange = new Vector2(0.8f, 1.2f); // kPa
        [SerializeField] private float _optimalVPD = 1.0f;
        [SerializeField] private AnimationCurve _vpdResponseCurve;
        
        [Header("Growth Stage Modifications")]
        [SerializeField] private List<GrowthStageModifier> _growthStageModifiers = new List<GrowthStageModifier>();
        
        [Header("Stress Thresholds")]
        [SerializeField] private StressThresholds _stressThresholds;
        
        // Public Properties
        public Vector2 TemperatureRange => _temperatureRange;
        public float OptimalTemperature => _optimalTemperature;
        public float MaxTemperatureTolerance => _maxTemperatureTolerance;
        public float MinTemperatureTolerance => _minTemperatureTolerance;
        public AnimationCurve TemperatureResponseCurve => _temperatureResponseCurve;
        
        public Vector2 HumidityRange => _humidityRange;
        public float OptimalHumidity => _optimalHumidity;
        public float MaxHumidityTolerance => _maxHumidityTolerance;
        public float MinHumidityTolerance => _minHumidityTolerance;
        public AnimationCurve HumidityResponseCurve => _humidityResponseCurve;
        
        public Vector2 LightIntensityRange => _lightIntensityRange;
        public float OptimalLightIntensity => _optimalLightIntensity;
        public Vector2 DailyLightIntegralRange => _dailyLightIntegralRange;
        public float OptimalDLI => _optimalDLI;
        public AnimationCurve LightResponseCurve => _lightResponseCurve;
        
        public Vector2 CO2Range => _co2Range;
        public float OptimalCO2 => _optimalCO2;
        public float MaxCO2Tolerance => _maxCO2Tolerance;
        public float MinCO2Tolerance => _minCO2Tolerance;
        public AnimationCurve CO2ResponseCurve => _co2ResponseCurve;
        
        public Vector2 AirVelocityRange => _airVelocityRange;
        public float OptimalAirVelocity => _optimalAirVelocity;
        public bool RequiresAirMovement => _requiresAirMovement;
        
        public Vector2 VPDRange => _vpdRange;
        public float OptimalVPD => _optimalVPD;
        public AnimationCurve VPDResponseCurve => _vpdResponseCurve;
        
        public List<GrowthStageModifier> GrowthStageModifiers => _growthStageModifiers;
        public StressThresholds StressThresholds => _stressThresholds;
        
        /// <summary>
        /// Evaluates how well the current environmental conditions match these parameters.
        /// </summary>
        /// <param name="temperature">Current temperature</param>
        /// <param name="humidity">Current humidity</param>
        /// <param name="lightIntensity">Current light intensity</param>
        /// <param name="co2Level">Current CO2 level</param>
        /// <returns>Suitability score from 0-1</returns>
        public float EvaluateEnvironmentalSuitability(float temperature, float humidity, float lightIntensity, float co2Level)
        {
            float temperatureScore = EvaluateParameterSuitability(temperature, _temperatureRange, _temperatureResponseCurve);
            float humidityScore = EvaluateParameterSuitability(humidity, _humidityRange, _humidityResponseCurve);
            float lightScore = EvaluateParameterSuitability(lightIntensity, _lightIntensityRange, _lightResponseCurve);
            float co2Score = EvaluateParameterSuitability(co2Level, _co2Range, _co2ResponseCurve);
            
            // VPD calculation and evaluation
            float vpd = CalculateVPD(temperature, humidity);
            float vpdScore = EvaluateParameterSuitability(vpd, _vpdRange, _vpdResponseCurve);
            
            // Weighted average (can be customized per parameter importance)
            return (temperatureScore * 0.25f + humidityScore * 0.2f + lightScore * 0.25f + co2Score * 0.15f + vpdScore * 0.15f);
        }
        
        /// <summary>
        /// Gets modified parameters for a specific growth stage.
        /// </summary>
        public EnvironmentalParametersSO GetModifiedParametersForStage(PlantGrowthStage stage)
        {
            var modifier = _growthStageModifiers.Find(m => m.GrowthStage == stage);
            if (modifier == null) return this;
            
            // Create a runtime copy with modifications applied
            var modifiedParams = CreateInstance<EnvironmentalParametersSO>();
            modifiedParams.CopyFrom(this);
            modifiedParams.ApplyModifier(modifier);
            
            return modifiedParams;
        }
        
        private float EvaluateParameterSuitability(float value, Vector2 optimalRange, AnimationCurve responseCurve)
        {
            if (responseCurve != null && responseCurve.length > 0)
            {
                // Use custom response curve if provided
                float normalizedValue = Mathf.InverseLerp(optimalRange.x, optimalRange.y, value);
                return responseCurve.Evaluate(normalizedValue);
            }
            
            // Default triangular response
            if (value >= optimalRange.x && value <= optimalRange.y)
            {
                return 1f;
            }
            
            float distanceFromRange = 0f;
            if (value < optimalRange.x)
            {
                distanceFromRange = optimalRange.x - value;
            }
            else
            {
                distanceFromRange = value - optimalRange.y;
            }
            
            // Linear decay outside optimal range
            float rangeSize = optimalRange.y - optimalRange.x;
            return Mathf.Max(0f, 1f - (distanceFromRange / rangeSize));
        }
        
        private float CalculateVPD(float temperature, float humidity)
        {
            // Saturation vapor pressure (kPa) using simplified formula
            float svp = 0.6108f * Mathf.Exp((17.27f * temperature) / (temperature + 237.3f));
            
            // Actual vapor pressure
            float avp = svp * (humidity / 100f);
            
            // VPD = SVP - AVP
            return svp - avp;
        }
        
        private void CopyFrom(EnvironmentalParametersSO source)
        {
            _temperatureRange = source._temperatureRange;
            _optimalTemperature = source._optimalTemperature;
            _humidityRange = source._humidityRange;
            _optimalHumidity = source._optimalHumidity;
            _lightIntensityRange = source._lightIntensityRange;
            _optimalLightIntensity = source._optimalLightIntensity;
            _co2Range = source._co2Range;
            _optimalCO2 = source._optimalCO2;
            _vpdRange = source._vpdRange;
            _optimalVPD = source._optimalVPD;
        }
        
        private void ApplyModifier(GrowthStageModifier modifier)
        {
            _temperatureRange = new Vector2(
                _temperatureRange.x + modifier.TemperatureOffset,
                _temperatureRange.y + modifier.TemperatureOffset
            );
            _humidityRange = new Vector2(
                _humidityRange.x + modifier.HumidityOffset,
                _humidityRange.y + modifier.HumidityOffset
            );
            _lightIntensityRange = new Vector2(
                _lightIntensityRange.x * modifier.LightMultiplier,
                _lightIntensityRange.y * modifier.LightMultiplier
            );
            _co2Range = new Vector2(
                _co2Range.x * modifier.CO2Multiplier,
                _co2Range.y * modifier.CO2Multiplier
            );
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (_temperatureRange.x >= _temperatureRange.y)
            {
                SharedLogger.LogError($"Environmental Parameters {name}: Invalid temperature range");
                isValid = false;
            }
                
            if (_humidityRange.x >= _humidityRange.y)
            {
                SharedLogger.LogError($"Environmental Parameters {name}: Invalid humidity range");
                isValid = false;
            }
                
            if (_lightIntensityRange.x >= _lightIntensityRange.y)
            {
                SharedLogger.LogError($"Environmental Parameters {name}: Invalid light intensity range");
                isValid = false;
            }
                
            if (_co2Range.x >= _co2Range.y)
            {
                SharedLogger.LogError($"Environmental Parameters {name}: Invalid CO2 range");
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    [System.Serializable]
    public class GrowthStageModifier
    {
        public PlantGrowthStage GrowthStage;
        [Range(-10f, 10f)] public float TemperatureOffset = 0f;
        [Range(-20f, 20f)] public float HumidityOffset = 0f;
        [Range(0.5f, 2f)] public float LightMultiplier = 1f;
        [Range(0.5f, 2f)] public float CO2Multiplier = 1f;
        [TextArea(2, 3)] public string ModifierDescription;
    }
    
    [System.Serializable]
    public class StressThresholds
    {
        [Header("Temperature Stress")]
        public float HeatStressThreshold = 30f;
        public float ColdStressThreshold = 15f;
        public float CriticalHeatThreshold = 35f;
        public float CriticalColdThreshold = 5f;
        
        [Header("Humidity Stress")]
        public float HighHumidityStress = 80f;
        public float LowHumidityStress = 30f;
        public float CriticalHighHumidity = 95f;
        public float CriticalLowHumidity = 15f;
        
        [Header("Light Stress")]
        public float LightBurnThreshold = 1000f;
        public float LightDeficiencyThreshold = 200f;
        
        [Header("CO2 Stress")]
        public float CO2ToxicityThreshold = 1500f;
        public float CO2DeficiencyThreshold = 300f;
    }
}