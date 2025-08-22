using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Shared;
using ProjectChimera.Data.Genetics;
using System;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Defines a cultivation zone within a facility - a controlled environment space for growing plants.
    /// Each zone can have independent environmental control, lighting, irrigation, and plant populations.
    /// Supports professional cultivation workflows including room transitions, crop cycles, and automation.
    /// </summary>
    [CreateAssetMenu(fileName = "New Cultivation Zone", menuName = "Project Chimera/Cultivation/Cultivation Zone")]
    public class CultivationZoneSO : ChimeraConfigSO
    {
        [Header("Zone Identity")]
        [SerializeField] private string _zoneID;
        [SerializeField] private string _zoneName;
        [SerializeField] private ZoneType _zoneType = ZoneType.FloweringRoom;
        [SerializeField, TextArea(2, 4)] private string _zoneDescription;
        [SerializeField] private FacilitySO _parentFacility;
        
        [Header("Physical Specifications")]
        [SerializeField] private Vector3 _zoneDimensions = new Vector3(4f, 3f, 4f); // meters (L x H x W)
        [SerializeField, Range(1f, 1000f)] private float _floorArea = 16f; // square meters
        [SerializeField, Range(1f, 50f)] private float _ceilingHeight = 3f; // meters
        [SerializeField, Range(10f, 10000f)] private float _airVolume = 48f; // cubic meters
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(4, 4); // plant positions
        
        [Header("Environmental Capabilities")]
        [SerializeField] private EnvironmentalControlLevel _controlLevel = EnvironmentalControlLevel.Full;
        [SerializeField] private bool _hasIndependentHVAC = true;
        [SerializeField] private bool _hasIndependentLighting = true;
        [SerializeField] private bool _hasIndependentIrrigation = true;
        [SerializeField] private bool _hasIndependentCO2 = true;
        [SerializeField] private bool _hasSealedEnvironment = true;
        
        [Header("Equipment Configuration")]
        [SerializeField] private EquipmentSuite _equipmentSuite = new EquipmentSuite();
        [SerializeField] private SensorArray _sensorArray = new SensorArray();
        [SerializeField] private AutomationLevel _automationLevel = AutomationLevel.FullyAutomated;
        
        [Header("Capacity and Layout")]
        [SerializeField, Range(1, 1000)] private int _maxPlantCapacity = 16;
        [SerializeField] private PlantSpacing _plantSpacing = PlantSpacing.Standard;
        [SerializeField] private GrowingMethod _primaryGrowingMethod = GrowingMethod.Hydroponic;
        [SerializeField] private ContainerType _containerType = ContainerType.FlexiblePots;
        [SerializeField] private bool _allowsMultipleMethods = false;
        
        [Header("Cultivation Specifications")]
        [SerializeField] private PlantGrowthStage[] _supportedStages = new PlantGrowthStage[]
        {
            PlantGrowthStage.Vegetative,
            PlantGrowthStage.Flowering
        };
        [SerializeField] private SpeciesType[] _supportedSpecies = new SpeciesType[]
        {
            SpeciesType.CannabisIndica,
            SpeciesType.CannabisSativa,
            SpeciesType.CannabisHybrid
        };
        [SerializeField] private CultivationStyle _cultivationStyle = CultivationStyle.Commercial;
        
        [Header("Environmental Ranges")]
        [SerializeField] private EnvironmentalLimits _environmentalLimits = new EnvironmentalLimits();
        [SerializeField] private EnvironmentalConditions _defaultConditions;
        [SerializeField] private bool _allowsEnvironmentalOverrides = false;
        
        [Header("Safety and Compliance")]
        [SerializeField] private SafetyFeatures _safetyFeatures = new SafetyFeatures();
        [SerializeField] private ComplianceLevel _complianceLevel = ComplianceLevel.Commercial;
        [SerializeField] private bool _requiresSOPCompliance = true;
        [SerializeField] private bool _enablesDataLogging = true;
        [SerializeField] private bool _enablesRemoteMonitoring = false;
        
        [Header("Workflow Configuration")]
        [SerializeField] private WorkflowCapabilities _workflowCapabilities = new WorkflowCapabilities();
        [SerializeField] private bool _supportsMultipleCycles = true;
        [SerializeField] private bool _allowsCropRotation = true;
        [SerializeField] private float _cleaningTimeBetweenCycles = 24f; // hours
        
        [Header("Economic Parameters")]
        [SerializeField, Range(100f, 1000000f)] private float _constructionCost = 50000f;
        [SerializeField, Range(10f, 10000f)] private float _dailyOperatingCost = 150f;
        [SerializeField, Range(0.1f, 100f)] private float _energyConsumptionKWH = 25f; // per day
        [SerializeField, Range(0f, 1f)] private float _maintenanceComplexity = 0.6f;
        
        // Public Properties
        public string ZoneID => _zoneID;
        public string ZoneName => _zoneName;
        public ZoneType ZoneType => _zoneType;
        public string ZoneDescription => _zoneDescription;
        public FacilitySO ParentFacility => _parentFacility;
        
        // Physical Properties
        public Vector3 ZoneDimensions => _zoneDimensions;
        public float FloorArea => _floorArea;
        public float CeilingHeight => _ceilingHeight;
        public float AirVolume => _airVolume;
        public Vector2Int GridSize => _gridSize;
        
        // Environmental Properties
        public EnvironmentalControlLevel ControlLevel => _controlLevel;
        public bool HasIndependentHVAC => _hasIndependentHVAC;
        public bool HasIndependentLighting => _hasIndependentLighting;
        public bool HasIndependentIrrigation => _hasIndependentIrrigation;
        public bool HasIndependentCO2 => _hasIndependentCO2;
        public bool HasSealedEnvironment => _hasSealedEnvironment;
        
        // Equipment Properties
        public EquipmentSuite EquipmentSuite => _equipmentSuite;
        public SensorArray SensorArray => _sensorArray;
        public AutomationLevel AutomationLevel => _automationLevel;
        
        // Capacity Properties
        public int MaxPlantCapacity => _maxPlantCapacity;
        public PlantSpacing PlantSpacing => _plantSpacing;
        public GrowingMethod PrimaryGrowingMethod => _primaryGrowingMethod;
        public ContainerType ContainerType => _containerType;
        public bool AllowsMultipleMethods => _allowsMultipleMethods;
        
        // Cultivation Properties
        public PlantGrowthStage[] SupportedStages => _supportedStages;
        public SpeciesType[] SupportedSpecies => _supportedSpecies;
        public CultivationStyle CultivationStyle => _cultivationStyle;
        
        // Environmental Properties
        public EnvironmentalLimits EnvironmentalLimits => _environmentalLimits;
        public EnvironmentalConditions DefaultConditions => _defaultConditions;
        public bool AllowsEnvironmentalOverrides => _allowsEnvironmentalOverrides;
        
        // Safety Properties
        public SafetyFeatures SafetyFeatures => _safetyFeatures;
        public ComplianceLevel ComplianceLevel => _complianceLevel;
        public bool RequiresSOPCompliance => _requiresSOPCompliance;
        public bool EnablesDataLogging => _enablesDataLogging;
        public bool EnablesRemoteMonitoring => _enablesRemoteMonitoring;
        
        // Workflow Properties
        public WorkflowCapabilities WorkflowCapabilities => _workflowCapabilities;
        public bool SupportsMultipleCycles => _supportsMultipleCycles;
        public bool AllowsCropRotation => _allowsCropRotation;
        public float CleaningTimeBetweenCycles => _cleaningTimeBetweenCycles;
        
        // Economic Properties
        public float ConstructionCost => _constructionCost;
        public float DailyOperatingCost => _dailyOperatingCost;
        public float EnergyConsumptionKWH => _energyConsumptionKWH;
        public float MaintenanceComplexity => _maintenanceComplexity;
        
        /// <summary>
        /// Calculates the optimal plant capacity for a given growth stage and growing method.
        /// Considers plant size, spacing requirements, and equipment layout.
        /// </summary>
        public int CalculateOptimalCapacity(PlantGrowthStage stage, GrowingMethod method, PlantStrainSO strain = null)
        {
            float baseSpacing = GetSpacingForStage(stage);
            
            // Apply growing method modifier
            float methodModifier = GetGrowingMethodSpacingModifier(method);
            baseSpacing *= methodModifier;
            
            // Apply strain modifier if available
            if (strain != null)
            {
                float strainModifier = CalculateStrainSpacingModifier(strain);
                baseSpacing *= strainModifier;
            }
            
            // Calculate how many plants fit in the floor area
            float plantsPerSquareMeter = 1f / (baseSpacing * baseSpacing);
            int calculatedCapacity = Mathf.FloorToInt(_floorArea * plantsPerSquareMeter);
            
            // Respect maximum capacity limit
            return Mathf.Min(calculatedCapacity, _maxPlantCapacity);
        }
        
        /// <summary>
        /// Evaluates environmental suitability for specific cultivation requirements.
        /// Returns a score from 0-1 indicating how well the zone can meet the requirements.
        /// </summary>
        public float EvaluateEnvironmentalSuitability(EnvironmentalConditions requiredConditions)
        {
            if (!CanMeetEnvironmentalRequirements(requiredConditions))
                return 0f;
            
            float temperatureScore = CalculateParameterScore(
                requiredConditions.Temperature,
                _environmentalLimits.TemperatureRange.x,
                _environmentalLimits.TemperatureRange.y);
                
            float humidityScore = CalculateParameterScore(
                requiredConditions.Humidity,
                _environmentalLimits.HumidityRange.x,
                _environmentalLimits.HumidityRange.y);
                
            float co2Score = CalculateParameterScore(
                requiredConditions.CO2Level,
                _environmentalLimits.CO2Range.x,
                _environmentalLimits.CO2Range.y);
                
            float lightScore = CalculateParameterScore(
                requiredConditions.LightIntensity,
                _environmentalLimits.LightIntensityRange.x,
                _environmentalLimits.LightIntensityRange.y);
            
            // Weighted average based on control capabilities
            float temperatureWeight = _hasIndependentHVAC ? 1f : 0.5f;
            float humidityWeight = _hasIndependentHVAC ? 1f : 0.5f;
            float co2Weight = _hasIndependentCO2 ? 1f : 0.3f;
            float lightWeight = _hasIndependentLighting ? 1f : 0.6f;
            
            float totalWeight = temperatureWeight + humidityWeight + co2Weight + lightWeight;
            
            return (temperatureScore * temperatureWeight +
                    humidityScore * humidityWeight +
                    co2Score * co2Weight +
                    lightScore * lightWeight) / totalWeight;
        }
        
        /// <summary>
        /// Checks if the zone can support a specific cultivation workflow.
        /// </summary>
        public bool CanSupportWorkflow(CultivationWorkflow workflow)
        {
            // Check if zone supports required growth stages
            foreach (var stage in workflow.RequiredStages)
            {
                if (!System.Array.Exists(_supportedStages, s => s == stage))
                    return false;
            }
            
            // Check capacity requirements
            if (workflow.RequiredCapacity > _maxPlantCapacity)
                return false;
            
            // Check environmental requirements
            if (!CanMeetEnvironmentalRequirements(workflow.EnvironmentalRequirements))
                return false;
            
            // Check equipment requirements
            if (!HasRequiredEquipment(workflow.EquipmentRequirements))
                return false;
            
            // Check automation level
            if (workflow.RequiredAutomationLevel > _automationLevel)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Calculates energy consumption for given environmental conditions.
        /// </summary>
        public float CalculateEnergyConsumption(EnvironmentalConditions targetConditions)
        {
            float baseConsumption = _energyConsumptionKWH;
            
            // Lighting energy (typically 60-70% of total)
            float lightingEnergy = (targetConditions.LightIntensity / 400f) * baseConsumption * 0.65f;
            
            // HVAC energy (typically 20-30% of total)
            float hvacEnergy = CalculateHVACEnergy(targetConditions) * baseConsumption * 0.25f;
            
            // Other systems (pumps, fans, sensors, etc. - typically 5-15%)
            float systemsEnergy = baseConsumption * 0.1f;
            
            return lightingEnergy + hvacEnergy + systemsEnergy;
        }
        
        /// <summary>
        /// Estimates yield potential based on zone specifications and cultivation parameters.
        /// </summary>
        public float EstimateYieldPotential(PlantStrainSO strain, CultivationMethod method, float skillLevel = 1f)
        {
            if (strain == null) return 0f;
            
            // Base yield from strain
            Vector2 strainYieldRange = strain.GetModifiedYieldRange();
            float baseYield = strainYieldRange.y * skillLevel; // Optimistic yield with skill factor
            
            // Zone capacity modifier
            int plantCount = CalculateOptimalCapacity(PlantGrowthStage.Flowering, _primaryGrowingMethod, strain);
            float totalPotentialYield = baseYield * plantCount;
            
            // Environmental suitability modifier
            EnvironmentalConditions optimalConditions = GetOptimalConditionsForStrain(strain);
            float environmentalSuitability = EvaluateEnvironmentalSuitability(optimalConditions);
            totalPotentialYield *= environmentalSuitability;
            
            // Cultivation method modifier
            float methodModifier = GetCultivationMethodYieldModifier(method);
            totalPotentialYield *= methodModifier;
            
            // Equipment quality modifier
            float equipmentModifier = CalculateEquipmentQualityModifier();
            totalPotentialYield *= equipmentModifier;
            
            return totalPotentialYield;
        }
        
        /// <summary>
        /// Gets the current operational status of the zone.
        /// </summary>
        public ZoneOperationalStatus GetOperationalStatus()
        {
            var status = new ZoneOperationalStatus
            {
                ZoneID = _zoneID,
                StatusTimestamp = DateTime.Now,
                IsOperational = true
            };
            
            // Check equipment status
            status.EquipmentHealth = _equipmentSuite.CalculateOverallHealth();
            status.IsOperational &= status.EquipmentHealth > 0.7f;
            
            // Check sensor status
            status.SensorHealth = _sensorArray.CalculateOverallHealth();
            status.IsOperational &= status.SensorHealth > 0.8f;
            
            // Check environmental control capability
            status.EnvironmentalControlHealth = CalculateEnvironmentalControlHealth();
            status.IsOperational &= status.EnvironmentalControlHealth > 0.75f;
            
            // Check safety systems
            status.SafetySystemsHealth = _safetyFeatures.CalculateSystemHealth();
            status.IsOperational &= status.SafetySystemsHealth > 0.9f;
            
            // Overall health calculation
            status.OverallHealth = (status.EquipmentHealth + status.SensorHealth + 
                                  status.EnvironmentalControlHealth + status.SafetySystemsHealth) / 4f;
            
            // Determine status level
            if (status.OverallHealth > 0.9f)
                status.StatusLevel = OperationalStatusLevel.Excellent;
            else if (status.OverallHealth > 0.8f)
                status.StatusLevel = OperationalStatusLevel.Good;
            else if (status.OverallHealth > 0.7f)
                status.StatusLevel = OperationalStatusLevel.Fair;
            else if (status.OverallHealth > 0.5f)
                status.StatusLevel = OperationalStatusLevel.Poor;
            else
                status.StatusLevel = OperationalStatusLevel.Critical;
            
            return status;
        }
        
        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();
            
            if (string.IsNullOrEmpty(_zoneID))
            {
                Debug.LogWarning($"CultivationZoneSO '{name}' has no zone ID assigned.", this);
                isValid = false;
            }
            
            if (_floorArea <= 0f || _ceilingHeight <= 0f)
            {
                Debug.LogWarning($"CultivationZoneSO '{name}' has invalid physical dimensions.", this);
                isValid = false;
            }
            
            if (_maxPlantCapacity <= 0)
            {
                Debug.LogWarning($"CultivationZoneSO '{name}' has invalid plant capacity.", this);
                isValid = false;
            }
            
            if (_supportedStages == null || _supportedStages.Length == 0)
            {
                Debug.LogWarning($"CultivationZoneSO '{name}' has no supported growth stages.", this);
                isValid = false;
            }
            
            if (_airVolume != (_zoneDimensions.x * _zoneDimensions.y * _zoneDimensions.z))
            {
                Debug.LogWarning($"CultivationZoneSO '{name}' air volume doesn't match dimensions. Auto-correcting.", this);
                _airVolume = _zoneDimensions.x * _zoneDimensions.y * _zoneDimensions.z;
                isValid = false;
            }
            
            return isValid;
        }
        
        // Private helper methods
        private float GetSpacingForStage(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seed:
                case PlantGrowthStage.Germination:
                    return 0.1f; // 10cm spacing
                case PlantGrowthStage.Seedling:
                    return 0.2f; // 20cm spacing
                case PlantGrowthStage.Vegetative:
                    return 0.5f; // 50cm spacing
                case PlantGrowthStage.Flowering:
                    return 1.0f; // 100cm spacing
                default:
                    return 0.75f; // Default spacing
            }
        }
        
        private float GetGrowingMethodSpacingModifier(GrowingMethod method)
        {
            switch (method)
            {
                case GrowingMethod.Hydroponic:
                    return 0.9f; // More compact
                case GrowingMethod.Aeroponic:
                    return 0.8f; // Most compact
                case GrowingMethod.Soil:
                    return 1.1f; // More space needed
                case GrowingMethod.Coco:
                    return 1.0f; // Standard spacing
                default:
                    return 1.0f;
            }
        }
        
        private float CalculateStrainSpacingModifier(PlantStrainSO strain)
        {
            // Indica strains typically shorter and bushier
            if (strain.StrainType == StrainType.Indica)
                return 0.9f;
            
            // Sativa strains typically taller and need more space
            if (strain.StrainType == StrainType.Sativa)
                return 1.2f;
            
            // Hybrids and others
            return 1.0f;
        }
        
        private float CalculateParameterScore(float value, float min, float max)
        {
            if (value < min || value > max)
                return 0f;
            
            // Score is 1.0 if in range, could be enhanced for optimal vs acceptable ranges
            return 1f;
        }
        
        private bool CanMeetEnvironmentalRequirements(EnvironmentalConditions requirements)
        {
            return requirements.Temperature >= _environmentalLimits.TemperatureRange.x &&
                   requirements.Temperature <= _environmentalLimits.TemperatureRange.y &&
                   requirements.Humidity >= _environmentalLimits.HumidityRange.x &&
                   requirements.Humidity <= _environmentalLimits.HumidityRange.y &&
                   requirements.CO2Level >= _environmentalLimits.CO2Range.x &&
                   requirements.CO2Level <= _environmentalLimits.CO2Range.y &&
                   requirements.LightIntensity >= _environmentalLimits.LightIntensityRange.x &&
                   requirements.LightIntensity <= _environmentalLimits.LightIntensityRange.y;
        }
        
        private bool HasRequiredEquipment(EquipmentRequirement[] requirements)
        {
            // Implementation would check if zone has required equipment
            return true; // Placeholder
        }
        
        private float CalculateHVACEnergy(EnvironmentalConditions conditions)
        {
            // Implementation would calculate HVAC energy based on environmental load
            return 1f; // Placeholder
        }
        
        private EnvironmentalConditions GetOptimalConditionsForStrain(PlantStrainSO strain)
        {
            // Implementation would get optimal conditions for strain
            return _defaultConditions; // Placeholder
        }
        
        private float GetCultivationMethodYieldModifier(CultivationMethod method)
        {
            // Implementation would return yield modifier for cultivation method
            return 1f; // Placeholder
        }
        
        private float CalculateEquipmentQualityModifier()
        {
            // Implementation would calculate equipment quality impact on yield
            return 1f; // Placeholder
        }
        
        private float CalculateEnvironmentalControlHealth()
        {
            // Implementation would assess environmental control system health
            return 1f; // Placeholder
        }
    }
    
    // Supporting data structures for cultivation zones
    
    [System.Serializable]
    public class EquipmentSuite
    {
        [Header("Lighting Equipment")]
        public LightingEquipment LightingSystem = new LightingEquipment();
        
        [Header("HVAC Equipment")]
        public HVACEquipment HVACSystem = new HVACEquipment();
        
        [Header("Irrigation Equipment")]
        public IrrigationEquipment IrrigationSystem = new IrrigationEquipment();
        
        [Header("Monitoring Equipment")]
        public MonitoringEquipment MonitoringSystem = new MonitoringEquipment();
        
        [Header("Safety Equipment")]
        public SafetyEquipment SafetySystem = new SafetyEquipment();
        
        public float CalculateOverallHealth()
        {
            return (LightingSystem.Health + HVACSystem.Health + IrrigationSystem.Health + 
                   MonitoringSystem.Health + SafetySystem.Health) / 5f;
        }
    }
    
    [System.Serializable]
    public class SensorArray
    {
        [Header("Environmental Sensors")]
        public bool HasTemperatureSensors = true;
        public bool HasHumiditySensors = true;
        public bool HasCO2Sensors = true;
        public bool HasLightSensors = true;
        public bool HasAirflowSensors = false;
        
        [Header("Growing Medium Sensors")]
        public bool HaspHSensors = true;
        public bool HasECSensors = true;
        public bool HasMoistureSensors = true;
        public bool HasTemperatureSensorsGrowingMedium = true;
        
        [Header("Sensor Quality")]
        [Range(0f, 1f)] public float SensorAccuracy = 0.95f;
        [Range(0f, 1f)] public float SensorReliability = 0.98f;
        [Range(10f, 3600f)] public float UpdateFrequency = 60f; // seconds
        
        public float CalculateOverallHealth()
        {
            // Implementation would calculate sensor array health
            return SensorReliability * SensorAccuracy;
        }
    }
    
    [System.Serializable]
    public class EnvironmentalLimits
    {
        [Header("Temperature Limits")]
        public Vector2 TemperatureRange = new Vector2(10f, 40f); // Celsius
        [Range(0.1f, 10f)] public float TemperatureChangeRate = 2f; // °C per hour max
        
        [Header("Humidity Limits")]
        public Vector2 HumidityRange = new Vector2(20f, 90f); // %RH
        [Range(1f, 50f)] public float HumidityChangeRate = 10f; // % per hour max
        
        [Header("CO2 Limits")]
        public Vector2 CO2Range = new Vector2(250f, 2000f); // ppm
        [Range(10f, 500f)] public float CO2ChangeRate = 100f; // ppm per hour max
        
        [Header("Light Limits")]
        public Vector2 LightIntensityRange = new Vector2(0f, 1200f); // PPFD
        [Range(1f, 100f)] public float LightChangeRate = 50f; // PPFD per minute max
        
        [Header("Photoperiod Limits")]
        public Vector2 PhotoperiodRange = new Vector2(0f, 24f); // hours
        [Range(0.1f, 4f)] public float PhotoperiodTransitionTime = 0.5f; // hours
    }
    
    [System.Serializable]
    public class SafetyFeatures
    {
        [Header("Fire Safety")]
        public bool HasFireDetection = true;
        public bool HasFireSuppression = true;
        public bool HasEmergencyShutoff = true;
        
        [Header("Electrical Safety")]
        public bool HasGFCIProtection = true;
        public bool HasOverloadProtection = true;
        public bool HasEmergencyPower = false;
        
        [Header("Environmental Safety")]
        public bool HasTemperatureAlarms = true;
        public bool HasHumidityAlarms = true;
        public bool HasCO2Alarms = true;
        
        [Header("Physical Security")]
        public bool HasAccessControl = true;
        public bool HasSecurityCameras = false;
        public bool HasIntrusionDetection = false;
        
        public float CalculateSystemHealth()
        {
            // Implementation would calculate safety system health
            return 1f; // Placeholder
        }
    }
    
    [System.Serializable]
    public class WorkflowCapabilities
    {
        [Header("Cultivation Workflows")]
        public bool SupportsSeedStarting = true;
        public bool SupportsVegetativeGrowth = true;
        public bool SupportsFlowering = true;
        public bool SupportsDrying = false;
        public bool SupportsCuring = false;
        
        [Header("Maintenance Workflows")]
        public bool SupportsDefoliation = true;
        public bool SupportsTraining = true;
        public bool SupportsHarvesting = true;
        public bool SupportsTrimming = false;
        
        [Header("Operational Workflows")]
        public bool SupportsEnvironmentalControl = true;
        public bool SupportsNutrientManagement = true;
        public bool SupportsIPM = true;
        public bool SupportsDataCollection = true;
    }
    
    // Equipment sub-classes
    [System.Serializable]
    public class LightingEquipment
    {
        public LightType LightType = LightType.LED;
        [Range(100f, 2000f)] public float MaxPPFD = 800f;
        [Range(2700f, 6500f)] public float ColorTemperature = 4000f;
        public bool HasSpectrumControl = true;
        public bool HasDimmingControl = true;
        [Range(0f, 1f)] public float Health = 1f;
    }
    
    [System.Serializable]
    public class HVACEquipment
    {
        public bool HasHeating = true;
        public bool HasCooling = true;
        public bool HasDehumidification = true;
        public bool HasHumidification = true;
        public bool HasVentilation = true;
        [Range(0f, 1f)] public float Health = 1f;
    }
    
    [System.Serializable]
    public class IrrigationEquipment
    {
        public IrrigationType IrrigationType = IrrigationType.DripSystem;
        public bool HasNutrientInjection = true;
        public bool HaspHControl = true;
        public bool HasECControl = true;
        public bool HasAutomatedScheduling = true;
        [Range(0f, 1f)] public float Health = 1f;
    }
    
    [System.Serializable]
    public class MonitoringEquipment
    {
        public bool HasEnvironmentalMonitoring = true;
        public bool HasPlantMonitoring = false;
        public bool HasSecurityMonitoring = false;
        public bool HasRemoteAccess = false;
        public bool HasDataLogging = true;
        [Range(0f, 1f)] public float Health = 1f;
    }
    
    [System.Serializable]
    public class SafetyEquipment
    {
        public bool HasFireDetectionSystem = true;
        public bool HasLeakDetectionSystem = true;
        public bool HasEmergencyAlarms = true;
        public bool HasEmergencyShutoffs = true;
        [Range(0f, 1f)] public float Health = 1f;
    }
    
    // Supporting classes
    public class ZoneOperationalStatus
    {
        public string ZoneID;
        public DateTime StatusTimestamp;
        public bool IsOperational;
        public OperationalStatusLevel StatusLevel;
        public float OverallHealth;
        public float EquipmentHealth;
        public float SensorHealth;
        public float EnvironmentalControlHealth;
        public float SafetySystemsHealth;
    }
    
    public class CultivationWorkflow
    {
        public PlantGrowthStage[] RequiredStages;
        public int RequiredCapacity;
        public EnvironmentalConditions EnvironmentalRequirements;
        public EquipmentRequirement[] EquipmentRequirements;
        public AutomationLevel RequiredAutomationLevel;
    }
    
    public class EquipmentRequirement
    {
        public string EquipmentType;
        public bool IsRequired;
        public float MinimumCapacity;
    }
    
    public class CultivationMethod
    {
        public string MethodName;
        public GrowingMethod GrowingMethod;
        public float ComplexityLevel;
        public float ResourceRequirement;
    }
    
    public class FacilitySO : ChimeraConfigSO
    {
        // Placeholder for facility management
    }
    
    // Enums for cultivation zones
    public enum ZoneType
    {
        SeedStarting,
        Propagation,
        VegetativeRoom,
        FloweringRoom,
        DryingRoom,
        CuringRoom,
        MotherRoom,
        Quarantine,
        Processing,
        Storage
    }
    
    public enum EnvironmentalControlLevel
    {
        Basic,
        Intermediate,
        Advanced,
        Full,
        Professional
    }
    
    public enum AutomationLevel
    {
        Manual,
        BasicAutomation,
        Automated,
        FullyAutomated,
        AIControlled
    }
    
    public enum PlantSpacing
    {
        Tight,
        Standard,
        Generous,
        Commercial,
        Research
    }
    
    public enum GrowingMethod
    {
        Soil,
        Coco,
        Hydroponic,
        Aeroponic,
        Aquaponic,
        Rockwool,
        Perlite
    }
    
    public enum ContainerType
    {
        FlexiblePots,
        RigidPots,
        TraySystem,
        SlabSystem,
        NFTChannels,
        DWCBuckets,
        AeroponicTowers
    }
    
    public enum CultivationStyle
    {
        Hobbyist,
        Commercial,
        Research,
        Medical,
        Experimental
    }
    
    public enum ComplianceLevel
    {
        None,
        Basic,
        Commercial,
        Medical,
        Research
    }
    
    public enum OperationalStatusLevel
    {
        Critical,
        Poor,
        Fair,
        Good,
        Excellent
    }
    
    public enum LightType
    {
        Fluorescent,
        HPS,
        MH,
        LED,
        CMH,
        FullSpectrum
    }
    
    public enum IrrigationType
    {
        Manual,
        DripSystem,
        FloodAndDrain,
        NFT,
        DWC,
        Aeroponic,
        Misting
    }
}