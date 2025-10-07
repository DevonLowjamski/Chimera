using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Cultivation.Plant;
using System;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Core configuration for the Advanced Automated Fertigation System.
    /// Contains all system settings, nutrient profiles, and configuration parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "New Fertigation System", menuName = "Project Chimera/Cultivation/Fertigation System")]
    public class FertigationConfig : ChimeraConfigSO
    {
        [Header("System Configuration")]
        [SerializeField] private FertigationMode _systemMode = FertigationMode.FullyAutomated;
        [SerializeField] private DeliveryMethod _primaryDeliveryMethod = DeliveryMethod.DripIrrigation;
        [SerializeField] private NutrientMixingStrategy _mixingStrategy = NutrientMixingStrategy.RealTimeBlending;
        [SerializeField] private bool _enableMultiZoneControl = true;
        [SerializeField] private int _maxZoneCount = 8;

        [Header("Nutrient Management")]
        [SerializeField] private NutrientLineConfiguration[] _nutrientLines = new NutrientLineConfiguration[]
        {
            new NutrientLineConfiguration { LineName = "Base A", NutrientType = NutrientType.MacronutrientA, Concentration = 100f },
            new NutrientLineConfiguration { LineName = "Base B", NutrientType = NutrientType.MacronutrientB, Concentration = 100f },
            new NutrientLineConfiguration { LineName = "Bloom", NutrientType = NutrientType.BloomEnhancer, Concentration = 50f },
            new NutrientLineConfiguration { LineName = "Cal-Mag", NutrientType = NutrientType.CalciumMagnesium, Concentration = 75f },
            new NutrientLineConfiguration { LineName = "Micronutrients", NutrientType = NutrientType.Micronutrients, Concentration = 25f }
        };

        [Header("Water Quality Control")]
        [SerializeField] private WaterQualityParameters _waterQuality = new WaterQualityParameters();
        [SerializeField] private bool _enableWaterTreatment = true;
        [SerializeField] private WaterTreatmentSystem _treatmentSystem = new WaterTreatmentSystem();
        [SerializeField] private float _waterTemperatureTarget = 20f; // Celsius
        [SerializeField, Range(0f, 15f)] private float _dissolvedOxygenTarget = 8f; // ppm

        [Header("pH and EC Control")]
        [SerializeField] private pHControlSystem _pHControl = new pHControlSystem();
        [SerializeField] private ECControlSystem _ecControl = new ECControlSystem();
        [SerializeField] private bool _enableAutomaticpHCorrection = true;
        [SerializeField] private bool _enableAutomaticECCorrection = true;
        [SerializeField] private float _correctionResponseTime = 300f; // seconds

        [Header("Irrigation Scheduling")]
        [SerializeField] private IrrigationScheduleMode _scheduleMode = IrrigationScheduleMode.EvapotranspirationBased;
        [SerializeField] private bool _enableSmartScheduling = true;
        [SerializeField] private bool _enableMoistureBasedIrrigation = true;
        [SerializeField] private float _baseIrrigationFrequency = 4f; // times per day
        [SerializeField] private AnimationCurve _irrigationCurveByStage = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);

        [Header("Growth Stage Profiles")]
        [SerializeField] private NutrientProfile[] _stageNutrientProfiles = new NutrientProfile[]
        {
            new NutrientProfile
            {
                GrowthStage = PlantGrowthStage.Seedling,
                TargetEC = 0.6f,
                TargetpH = 5.8f,
                NPKRatio = new Vector3(1f, 0.5f, 1f),
                FeedingFrequency = 2f
            },
            new NutrientProfile
            {
                GrowthStage = PlantGrowthStage.Vegetative,
                TargetEC = 1.2f,
                TargetpH = 5.9f,
                NPKRatio = new Vector3(3f, 1f, 2f),
                FeedingFrequency = 4f
            },
            new NutrientProfile
            {
                GrowthStage = PlantGrowthStage.Flowering,
                TargetEC = 1.6f,
                TargetpH = 6.0f,
                NPKRatio = new Vector3(1f, 2f, 3f),
                FeedingFrequency = 3f
            }
        };

        [Header("Advanced Features")]
        [SerializeField] private bool _enableNutrientRecycling = true;
        [SerializeField] private bool _enableRunoffAnalysis = true;
        [SerializeField] private bool _enablePrecisionDosing = true;
        [SerializeField] private bool _enableNutrientTrending = true;
        [SerializeField] private float _dosingAccuracy = 0.02f; // ±2% accuracy

        [Header("Monitoring and Safety")]
        [SerializeField] private MonitoringConfiguration _monitoring = new MonitoringConfiguration();
        [SerializeField] private SafetyProtocols _safetyProtocols = new SafetyProtocols();
        [SerializeField] private bool _enableLeakDetection = true;
        [SerializeField] private bool _enableBackupSystems = true;
        [SerializeField] private float _emergencyShutoffTime = 5f; // seconds

        [Header("Economic Optimization")]
        [SerializeField] private bool _enableCostOptimization = true;
        [SerializeField] private bool _enableWaterConservation = true;
        [SerializeField] private bool _enableNutrientOptimization = true;
        [SerializeField, Range(0f, 1f)] private float _costEfficiencyWeight = 0.3f;
        [SerializeField, Range(0f, 50f)] private float _waterWasteThreshold = 10f; // % acceptable waste

        // Public properties for accessing configuration
        public FertigationMode SystemMode => _systemMode;
        public DeliveryMethod PrimaryDeliveryMethod => _primaryDeliveryMethod;
        public NutrientMixingStrategy MixingStrategy => _mixingStrategy;
        public bool EnableMultiZoneControl => _enableMultiZoneControl;
        public int MaxZoneCount => _maxZoneCount;

        public NutrientLineConfiguration[] NutrientLines => _nutrientLines;
        public WaterQualityParameters WaterQuality => _waterQuality;
        public bool EnableWaterTreatment => _enableWaterTreatment;
        public WaterTreatmentSystem TreatmentSystem => _treatmentSystem;
        public float WaterTemperatureTarget => _waterTemperatureTarget;
        public float DissolvedOxygenTarget => _dissolvedOxygenTarget;

        public pHControlSystem PHControl => _pHControl;
        public ECControlSystem ECControl => _ecControl;
        public bool EnableAutomaticpHCorrection => _enableAutomaticpHCorrection;
        public bool EnableAutomaticECCorrection => _enableAutomaticECCorrection;
        public float CorrectionResponseTime => _correctionResponseTime;

        public IrrigationScheduleMode ScheduleMode => _scheduleMode;
        public bool EnableSmartScheduling => _enableSmartScheduling;
        public bool EnableMoistureBasedIrrigation => _enableMoistureBasedIrrigation;
        public float BaseIrrigationFrequency => _baseIrrigationFrequency;
        public AnimationCurve IrrigationCurveByStage => _irrigationCurveByStage;

        public NutrientProfile[] StageNutrientProfiles => _stageNutrientProfiles;

        public bool EnableNutrientRecycling => _enableNutrientRecycling;
        public bool EnableRunoffAnalysis => _enableRunoffAnalysis;
        public bool EnablePrecisionDosing => _enablePrecisionDosing;
        public bool EnableNutrientTrending => _enableNutrientTrending;
        public float DosingAccuracy => _dosingAccuracy;

        public MonitoringConfiguration Monitoring => _monitoring;
        public SafetyProtocols SafetyProtocols => _safetyProtocols;
        public bool EnableLeakDetection => _enableLeakDetection;
        public bool EnableBackupSystems => _enableBackupSystems;
        public float EmergencyShutoffTime => _emergencyShutoffTime;

        public bool EnableCostOptimization => _enableCostOptimization;
        public bool EnableWaterConservation => _enableWaterConservation;
        public bool EnableNutrientOptimization => _enableNutrientOptimization;
        public float CostEfficiencyWeight => _costEfficiencyWeight;
        public float WaterWasteThreshold => _waterWasteThreshold;

        /// <summary>
        /// Gets the nutrient profile for a specific growth stage.
        /// </summary>
        public NutrientProfile GetNutrientProfileForStage(PlantGrowthStage growthStage)
        {
            foreach (var profile in _stageNutrientProfiles)
            {
                if (profile.GrowthStage == growthStage)
                {
                    return profile;
                }
            }

            // Return default profile for vegetative stage if not found
            return _stageNutrientProfiles.Length > 0 ? _stageNutrientProfiles[0] : new NutrientProfile
            {
                GrowthStage = growthStage,
                TargetEC = 1.2f,
                TargetpH = 6.0f,
                NPKRatio = new Vector3(1, 1, 1),
                FeedingFrequency = 3f
            };
        }

        /// <summary>
        /// Gets the dominant growth stage from an array of plants.
        /// </summary>
        public PlantGrowthStage GetDominantGrowthStage(PlantInstanceSO[] plants)
        {
            if (plants == null || plants.Length == 0)
                return PlantGrowthStage.Vegetative;

            // Count plants in each stage
            var stageCounts = new System.Collections.Generic.Dictionary<PlantGrowthStage, int>();

            foreach (var plant in plants)
            {
                if (plant != null)
                {
                    var stage = plant.CurrentGrowthStage;
                    if (stageCounts.ContainsKey(stage))
                        stageCounts[stage]++;
                    else
                        stageCounts[stage] = 1;
                }
            }

            // Find the stage with the most plants
            PlantGrowthStage dominantStage = PlantGrowthStage.Vegetative;
            int maxCount = 0;

            foreach (var kvp in stageCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    dominantStage = kvp.Key;
                }
            }

            return dominantStage;
        }

        /// <summary>
        /// Validates the configuration data for consistency and safety.
        /// </summary>
        public override bool ValidateData()
        {
            bool isValid = true;
            var validationErrors = new System.Collections.Generic.List<string>();

            // Validate basic parameters
            if (_maxZoneCount <= 0 || _maxZoneCount > 32)
            {
                validationErrors.Add("Max zone count must be between 1 and 32");
                isValid = false;
            }

            if (_waterTemperatureTarget < 10f || _waterTemperatureTarget > 35f)
            {
                validationErrors.Add("Water temperature target should be between 10°C and 35°C");
                isValid = false;
            }

            if (_dissolvedOxygenTarget < 3f || _dissolvedOxygenTarget > 15f)
            {
                validationErrors.Add("Dissolved oxygen target should be between 3ppm and 15ppm");
                isValid = false;
            }

            // Validate nutrient lines
            if (_nutrientLines == null || _nutrientLines.Length == 0)
            {
                validationErrors.Add("At least one nutrient line must be configured");
                isValid = false;
            }
            else
            {
                for (int i = 0; i < _nutrientLines.Length; i++)
                {
                    var line = _nutrientLines[i];
                    if (string.IsNullOrEmpty(line.LineName))
                    {
                        validationErrors.Add($"Nutrient line {i} must have a valid name");
                        isValid = false;
                    }

                    if (line.Concentration <= 0f || line.Concentration > 500f)
                    {
                        validationErrors.Add($"Nutrient line {i} concentration must be between 0 and 500");
                        isValid = false;
                    }
                }
            }

            // Validate growth stage profiles
            if (_stageNutrientProfiles == null || _stageNutrientProfiles.Length == 0)
            {
                validationErrors.Add("At least one nutrient profile must be configured");
                isValid = false;
            }
            else
            {
                foreach (var profile in _stageNutrientProfiles)
                {
                    if (profile.TargetEC < 0.1f || profile.TargetEC > 4f)
                    {
                        validationErrors.Add($"Profile for {profile.GrowthStage}: EC must be between 0.1 and 4.0 mS/cm");
                        isValid = false;
                    }

                    if (profile.TargetpH < 4f || profile.TargetpH > 8f)
                    {
                        validationErrors.Add($"Profile for {profile.GrowthStage}: pH must be between 4.0 and 8.0");
                        isValid = false;
                    }
                }
            }

            // Log validation results
            if (!isValid)
            {
                ProjectChimera.Shared.SharedLogger.Log("CULTIVATION", "Fertigation validation failed", this);
            }

            return isValid;
        }
    }
}
