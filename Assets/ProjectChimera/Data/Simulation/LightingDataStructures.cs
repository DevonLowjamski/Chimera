using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Shared;
using EnergyDataPoint = ProjectChimera.Data.Simulation.HVAC.EnergyDataPoint;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Data.Simulation
{
    /// <summary>
    /// Comprehensive data structures for advanced lighting system management in Project Chimera.
    /// Includes spectrum control, DLI optimization, photoperiod management, and energy efficiency.
    /// </summary>

    [System.Serializable]
    public class LightingSystemSettings
    {
        [Range(10f, 80f)] public float DefaultDLI = 35f; // mol/m²/day
        [Range(5f, 100f)] public float MinDLI = 15f;
        [Range(10f, 100f)] public float MaxDLI = 65f;
        [Range(8f, 24f)] public float DefaultPhotoperiod = 18f; // hours
        [Range(1, 50)] public int MaxFixturesPerZone = 20;
        public bool EnableUVWarnings = true;
        public bool EnableSpectralOptimization = true;
        [Range(1f, 72f)] public float EnergyOptimizationInterval = 24f; // hours
        [Range(0.1f, 5f)] public float DLIUpdateFrequency = 1f; // updates per second
    }

    [System.Serializable]
    public class LightingZoneSettings
    {
        [Range(10f, 80f)] public float DefaultDLI = 35f;
        [Range(8f, 24f)] public float DefaultPhotoperiod = 18f;
        public PlantGrowthStage InitialGrowthStage = PlantGrowthStage.Vegetative;
        public SpectrumOptimizationTarget DefaultSpectrumTarget = SpectrumOptimizationTarget.Balanced_Growth;
        [Range(10f, 1000f)] public float ZoneArea = 100f; // square meters
        [Range(1f, 10f)] public float CanopyHeight = 2f; // meters
        public bool EnableAutoScheduling = true;
        public bool EnableDLIOptimization = true;
        public ZoneLightingPriority Priority = ZoneLightingPriority.Normal;
    }

    [System.Serializable]
    public class SpectrumControlSettings
    {
        public bool EnableDynamicSpectrum = true;
        public bool EnableUVSupplementation = false;
        public bool EnableFarRedControl = true;
        public bool EnableGrowthStageAdaptation = true;
        [Range(0f, 50f)] public float MaxUVIntensity = 25f; // μmol/m²/s
        [Range(0f, 200f)] public float MaxFarRedIntensity = 100f; // μmol/m²/s
        public List<SpectrumPresetSO> SpectrumPresets = new List<SpectrumPresetSO>();
    }

    [System.Serializable]
    public class DLISettings
    {
        public bool EnableAdaptiveDLI = true;
        public bool EnableWeatherCompensation = false;
        public bool EnableSeasonalAdjustment = false;
        [Range(0.1f, 5f)] public float DLIRampRate = 1f; // mol/m²/day per day
        [Range(0.05f, 0.5f)] public float DLITolerance = 0.1f;
        public List<DLIProfileSO> DLIProfiles = new List<DLIProfileSO>();
    }

    [System.Serializable]
    public class PhotoperiodSettings
    {
        public bool EnableAutomaticProgression = true;
        public bool EnableSunriseSimulation = true;
        public bool EnableSunsetSimulation = true;
        [Range(30, 300)] public int SunriseTransitionMinutes = 60;
        [Range(30, 300)] public int SunsetTransitionMinutes = 90;
        [Range(0f, 1f)] public float MinNightIntensity = 0f;
        public List<PhotoperiodPresetSO> PhotoperiodPresets = new List<PhotoperiodPresetSO>();
    }

    [System.Serializable]
    public class LightingEnergySettings
    {
        public bool EnableDimmingOptimization = true;
        public bool EnableScheduleOptimization = true;
        public bool EnableLoadBalancing = false;
        [Range(0.1f, 1f)] public float MinDimmingLevel = 0.2f;
        [Range(1f, 100f)] public float EnergyBudgetWatts = 1000f;
        [Range(0.01f, 1f)] public float EnergyEfficiencyTarget = 0.85f;
    }

    [System.Serializable]
    public class LightingZone
    {
        public string ZoneId;
        public string ZoneName;
        public LightingZoneSettings ZoneSettings;
        public LightSpectrumData CurrentSpectrum;
        public LightSpectrumData TargetSpectrum;
        public List<ActiveLightingFixture> LightingFixtures = new List<ActiveLightingFixture>();
        public float CurrentDLI;
        public float TargetDLI;
        public PlantGrowthStage GrowthStage;
        public PhotoperiodStage PhotoperiodStage;
        public SpectrumOptimizationTarget SpectrumTarget;
        public string ActiveScheduleId;
        public bool AutomaticPhotoperiod;
        public PhotoperiodProgression PhotoperiodProgression;
        public LightingZoneStatus ZoneStatus;
        public System.DateTime CreatedAt;
        public System.DateTime LastUpdated;
    }

    [System.Serializable]
    public class ActiveLightingFixture
    {
        public string FixtureId;
        public LightingFixtureSO FixtureData;
        public string ZoneId;
        public Vector3 Position;
        public Quaternion Rotation;
        public LightingFixtureStatus Status;
        [Range(0f, 1f)] public float Intensity;
        public LightSpectrumData CurrentSpectrum;
        public float OperatingHours;
        [Range(0.1f, 1.5f)] public float EfficiencyRating = 1f;
        public LightingMaintenanceStatus MaintenanceStatus;
        public System.DateTime InstallationDate;
        public System.DateTime LastMaintenanceDate;
        public List<LightingAlarm> FixtureAlarms = new List<LightingAlarm>();
        public Vector2 CoverageArea; // length x width in meters
    }

    // Note: LightSpectrumData class is already defined in LightSpectrum.cs
    // Using existing comprehensive LightSpectrumData implementation

    [System.Serializable]
    public class LightingSchedule
    {
        public string ScheduleId;
        public string ZoneId;
        public string ScheduleName;
        public ScheduleType ScheduleType;
        public List<LightingSchedulePoint> SchedulePoints = new List<LightingSchedulePoint>();
        public bool IsActive;
        public bool EnableTransitions;
        [Range(1, 180)] public int TransitionDurationMinutes = 30;
        public System.DateTime CreatedAt;
        public System.DateTime ActivatedAt;
    }

    [System.Serializable]
    public class LightingSchedulePoint
    {
        [Range(0, 24)] public int Hour;
        [Range(0, 59)] public int Minute;
        [Range(0f, 1f)] public float Intensity;
        public LightSpectrumData Spectrum;
        public ScheduleAction Action;
        public string ActionParameters;
    }

    [System.Serializable]
    public class PhotoperiodProgression
    {
        public PhotoperiodType ProgressionType;
        [Range(1, 365)] public int ProgressionDays = 14;
        [Range(8f, 24f)] public float StartPhotoperiod = 18f;
        [Range(8f, 24f)] public float EndPhotoperiod = 12f;
        [Range(0.1f, 2f)] public float DailyChange = 0.25f; // hours per day
        public bool EnableGradualTransition = true;
        public List<PhotoperiodMilestone> Milestones = new List<PhotoperiodMilestone>();
    }

    [System.Serializable]
    public class PhotoperiodMilestone
    {
        public int Day;
        public float Photoperiod;
        public LightSpectrumData RecommendedSpectrum;
        public string MilestoneDescription;
    }

    [System.Serializable]
    public class DLITracker
    {
        private List<DLIDataPoint> _dliHistory = new List<DLIDataPoint>();
        private float _currentDLI;
        private float _averageDLI;
        
        public void RecordDLI(float dli, System.DateTime timestamp)
        {
            _currentDLI = dli;
            
            _dliHistory.Add(new DLIDataPoint
            {
                DLI = dli,
                Timestamp = timestamp
            });
            
            // Keep only last 1000 data points
            if (_dliHistory.Count > 1000)
                _dliHistory.RemoveAt(0);
            
            // Update average
            _averageDLI = _dliHistory.Average(dp => dp.DLI);
        }
        
        public float GetCurrentDLI() => _currentDLI;
        public float GetAverageDLI() => _averageDLI;
        
        public float GetAverageDLI(int days)
        {
            var cutoffDate = System.DateTime.Now.AddDays(-days);
            var recentData = _dliHistory.Where(dp => dp.Timestamp >= cutoffDate).ToList();
            
            return recentData.Count > 0 ? recentData.Average(dp => dp.DLI) : 0f;
        }
        
        public List<DLIDataPoint> GetDLIHistory() => _dliHistory.ToList();
    }

    [System.Serializable]
    public class DLIDataPoint
    {
        public float DLI;
        public System.DateTime Timestamp;
    }

    [System.Serializable]
    public class LightingEnergyTracker
    {
        private List<EnergyDataPoint> _energyHistory = new List<EnergyDataPoint>();
        private float _currentConsumption;
        
        public void RecordEnergyConsumption(float consumption, System.DateTime timestamp)
        {
            _currentConsumption = consumption;
            
            _energyHistory.Add(new EnergyDataPoint
            {
                Consumption = consumption,
                Timestamp = timestamp
            });
            
            if (_energyHistory.Count > 1000)
                _energyHistory.RemoveAt(0);
        }
        
        public float GetCurrentConsumption() => _currentConsumption;
        
        public float GetAverageConsumption(int hours)
        {
            var cutoffTime = System.DateTime.Now.AddHours(-hours);
            var recentData = _energyHistory.Where(dp => dp.Timestamp >= cutoffTime).ToList();
            
            return recentData.Count > 0 ? recentData.Average(dp => dp.Consumption) : 0f;
        }
    }

    [System.Serializable]
    public class PhotoperiodController
    {
        private PhotoperiodSettings _settings;
        private Dictionary<string, PhotoperiodProgression> _activeProgressions;
        
        public PhotoperiodController(PhotoperiodSettings settings)
        {
            _settings = settings;
            _activeProgressions = new Dictionary<string, PhotoperiodProgression>();
        }
        
        public void StartProgression(string zoneId, PhotoperiodProgression progression)
        {
            _activeProgressions[zoneId] = progression;
        }
        
        public void UpdateProgression(string zoneId, LightingZone zone)
        {
            if (!_activeProgressions.TryGetValue(zoneId, out var progression))
                return;
            
            // Calculate current photoperiod based on progression
            float currentPhotoperiod = CalculateCurrentPhotoperiod(progression);
            
            // Update zone photoperiod if different
            if (Mathf.Abs(zone.ZoneSettings.DefaultPhotoperiod - currentPhotoperiod) > 0.1f)
            {
                zone.ZoneSettings.DefaultPhotoperiod = currentPhotoperiod;
                zone.LastUpdated = System.DateTime.Now;
            }
        }
        
        private float CalculateCurrentPhotoperiod(PhotoperiodProgression progression)
        {
            // Simplified calculation - would be more complex in full implementation
            return progression.StartPhotoperiod;
        }
    }

    [System.Serializable]
    public class SpectrumController
    {
        private SpectrumControlSettings _settings;
        
        public SpectrumController(SpectrumControlSettings settings)
        {
            _settings = settings;
        }
        
        public LightSpectrumData OptimizeSpectrumForGrowthStage(PlantGrowthStage growthStage)
        {
            return growthStage switch
            {
                PlantGrowthStage.Seedling => new LightSpectrumData
                {
                    Blue_420_490nm = 120f,
                    Green_490_550nm = 40f,
                    Red_630_660nm = 100f,
                    DeepRed_660_700nm = 60f,
                    UV_A_315_400nm = 5f,
                    FarRed_700_850nm = 30f
                },
                PlantGrowthStage.Vegetative => new LightSpectrumData
                {
                    Blue_420_490nm = 150f,
                    Green_490_550nm = 60f,
                    Red_630_660nm = 120f,
                    DeepRed_660_700nm = 80f,
                    UV_A_315_400nm = 10f,
                    FarRed_700_850nm = 40f
                },
                PlantGrowthStage.Flowering => new LightSpectrumData
                {
                    Blue_420_490nm = 100f,
                    Green_490_550nm = 50f,
                    Red_630_660nm = 180f,
                    DeepRed_660_700nm = 120f,
                    UV_A_315_400nm = 15f,
                    FarRed_700_850nm = 50f
                },
                _ => new LightSpectrumData()
            };
        }
    }

    [System.Serializable]
    public class LightingAlarm
    {
        public string AlarmId;
        public string ZoneId;
        public string FixtureId;
        public LightingAlarmType AlarmType;
        public LightingAlarmPriority Priority;
        public LightingAlarmStatus AlarmStatus;
        public string AlarmMessage;
        public float AlarmValue;
        public float ThresholdValue;
        public System.DateTime TriggerTime;
        public System.DateTime? AcknowledgeTime;
        public System.DateTime? ClearTime;
    }

    [System.Serializable]
    public class LightingOptimizationResult
    {
        public float EnergyReduction;
        public float DLIImprovement;
        public List<FixtureOptimization> FixtureOptimizations = new List<FixtureOptimization>();
        public List<string> RecommendedActions = new List<string>();
        public float ImplementationCost;
        public float PaybackPeriodMonths;
    }

    [System.Serializable]
    public class FixtureOptimization
    {
        public string FixtureId;
        public float CurrentIntensity;
        public float OptimalIntensity;
        public LightSpectrumData CurrentSpectrum;
        public LightSpectrumData OptimalSpectrum;
        public float EnergyReduction;
        public string OptimizationReason;
    }

    [System.Serializable]
    public class LightingZoneSnapshot
    {
        public string ZoneId;
        public string ZoneName;
        public System.DateTime Timestamp;
        public float CurrentDLI;
        public float TargetDLI;
        public LightSpectrumData CurrentSpectrum;
        public LightSpectrumData TargetSpectrum;
        public PhotoperiodStage PhotoperiodStage;
        public LightingZoneStatus ZoneStatus;
        public List<LightingFixtureSnapshot> FixtureStatus = new List<LightingFixtureSnapshot>();
        public float EnergyEfficiency;
        public float LightUniformity;
        public float SpectralQuality;
    }

    [System.Serializable]
    public class LightingFixtureSnapshot
    {
        public string FixtureId;
        public string FixtureName;
        public LightingFixtureStatus Status;
        public float Intensity;
        public LightSpectrumData CurrentSpectrum;
        public float EnergyConsumption;
        public float EfficiencyRating;
        public float PPFD;
        public LightingMaintenanceStatus MaintenanceStatus;
    }

    [System.Serializable]
    public class LightingEnergyReport
    {
        public System.DateTime ReportDate;
        public System.TimeSpan ReportingPeriod;
        public List<ZoneLightingEnergyReport> ZoneReports = new List<ZoneLightingEnergyReport>();
        public float TotalEnergyConsumption;
        public float TotalEnergyCost;
        public float AverageEfficiency;
        public float TotalDLIDelivered;
    }

    [System.Serializable]
    public class ZoneLightingEnergyReport
    {
        public string ZoneId;
        public string ZoneName;
        public float TotalEnergyConsumption;
        public Dictionary<string, float> FixtureConsumption = new Dictionary<string, float>();
        public float EnergyEfficiency;
        public float DLIEfficiency; // DLI per kWh
        public List<string> OptimizationOpportunities = new List<string>();
    }

    // Equipment ScriptableObjects
    [CreateAssetMenu(fileName = "New Lighting Fixture", menuName = "Project Chimera/Equipment/Lighting/Lighting Fixture")]
    public class LightingFixtureSO : EquipmentDataSO
    {
        [Header("Lighting Specifications")]
        [Range(100f, 3000f)] public float PPFD = 800f; // μmol/m²/s
        [Range(50f, 5000f)] public float PowerConsumption = 400f; // Watts
        [Range(1f, 3f)] public float PhotonEfficacy = 2.5f; // μmol/J
        [Range(1f, 20f)] public float CoverageArea = 4f; // square meters
        
        [Header("Spectrum Capabilities")]
        public bool SupportsSpectrumControl = true;
        public bool SupportsUV = false;
        public bool SupportsFarRed = true;
        public bool SupportsDimming = true;
        public LightSpectrumData MaxSpectrum;
        
        [Header("Control Features")]
        [Range(0.01f, 1f)] public float MinDimmingLevel = 0.1f;
        [Range(1, 100)] public int SpectrumChannels = 4;
        public bool SupportsWirelessControl = true;
        public string ControlProtocol = "WiFi";
    }

    [CreateAssetMenu(fileName = "New Spectrum Preset", menuName = "Project Chimera/Equipment/Lighting/Spectrum Preset")]
    public class SpectrumPresetSO : ScriptableObject
    {
        public string PresetName;
        public LightSpectrumData Spectrum;
        public PlantGrowthStage RecommendedGrowthStage;
        public SpectrumOptimizationTarget OptimizationTarget;
        [TextArea(2, 4)] public string PresetDescription;
    }

    [CreateAssetMenu(fileName = "New DLI Profile", menuName = "Project Chimera/Equipment/Lighting/DLI Profile")]
    public class DLIProfileSO : ScriptableObject
    {
        public string ProfileName;
        public PlantGrowthStage GrowthStage;
        [Range(10f, 80f)] public float TargetDLI = 35f;
        [Range(10f, 80f)] public float MinDLI = 25f;
        [Range(10f, 80f)] public float MaxDLI = 45f;
        public List<DLISchedulePoint> DLISchedule = new List<DLISchedulePoint>();
        [TextArea(2, 4)] public string ProfileDescription;
    }

    [System.Serializable]
    public class DLISchedulePoint
    {
        public int Day; // Day of cultivation
        public float TargetDLI;
        public string Notes;
    }

    [CreateAssetMenu(fileName = "New Photoperiod Preset", menuName = "Project Chimera/Equipment/Lighting/Photoperiod Preset")]
    public class PhotoperiodPresetSO : ScriptableObject
    {
        public string PresetName;
        public PhotoperiodType PhotoperiodType;
        [Range(8f, 24f)] public float PhotoperiodHours = 18f;
        public LightingSchedule Schedule;
        public bool EnableProgression;
        public PhotoperiodProgression Progression;
        [TextArea(2, 4)] public string PresetDescription;
    }

    // Comprehensive enum definitions
    public enum LightingZoneStatus
    {
        Active,
        Standby,
        Maintenance,
        Alarm,
        Offline
    }

    public enum LightingFixtureStatus
    {
        Off,
        Standby,
        On,
        Dimmed,
        Fault,
        Maintenance
    }

    public enum PhotoperiodStage
    {
        Day,
        Sunrise,
        Sunset,
        Night
    }

    public enum SpectrumOptimizationTarget
    {
        Energy_Efficiency,
        Maximum_Growth,
        Cannabinoid_Production,
        Terpene_Production,
        Balanced_Growth,
        Seedling_Development,
        Vegetative_Growth,
        Flowering_Induction,
        Trichome_Development
    }

    public enum ScheduleType
    {
        Daily,
        Weekly,
        Growth_Stage,
        Custom,
        Photoperiod_Progression
    }

    public enum ScheduleAction
    {
        Turn_On,
        Turn_Off,
        Set_Intensity,
        Set_Spectrum,
        Gradual_Change,
        Sunrise_Simulation,
        Sunset_Simulation
    }

    public enum PhotoperiodType
    {
        Long_Day, // 18+ hours
        Neutral_Day, // 12-18 hours
        Short_Day, // 8-12 hours
        Progressive, // Gradual change
        Custom
    }

    public enum ZoneLightingPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public enum LightingAlarmType
    {
        DLI_Too_Low,
        DLI_Too_High,
        Fixture_Fault,
        Spectrum_Deviation,
        Energy_Efficiency_Low,
        UV_Exposure_High,
        Temperature_High,
        Maintenance_Required
    }

    public enum LightingAlarmPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum LightingAlarmStatus
    {
        Active,
        Acknowledged,
        Cleared,
        Disabled
    }

    public enum LightingMaintenanceStatus
    {
        Good,
        Fair,
        Poor,
        Critical,
        MaintenanceRequired
    }

    // Additional types needed by AdvancedGrowLightSystem
    public enum GrowLightType
    {
        LED,
        HPS,
        CMH,
        Fluorescent,
        FullSpectrum,
        Quantum,
        Hybrid
    }

    [System.Serializable]
    public class LightPerformanceMetrics
    {
        public float PPFD; // μmol/m²/s
        public float PowerConsumption; // Watts
        public float Efficiency; // μmol/J
        public float Temperature; // °C
        public float Lifetime; // hours
        public float DegradationRate; // % per 1000 hours
    }
}