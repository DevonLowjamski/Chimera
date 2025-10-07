using UnityEngine;
using ProjectChimera.Shared;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;


namespace ProjectChimera.Data.Equipment
{
    /// <summary>
    /// Base class for all equipment used in cannabis cultivation facilities.
    /// Defines common properties like power consumption, cost, and environmental effects.
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment Data", menuName = "Project Chimera/Equipment/Equipment Data")]
    public class EquipmentDataSO : ChimeraScriptableObject
    {
        [Header("Equipment Identity")]
        [SerializeField] private string _equipmentName;
        [SerializeField] private EquipmentCategory _category = EquipmentCategory.Lighting;
        [SerializeField] private EquipmentType _equipmentType = EquipmentType.LED_Light;
        [SerializeField, TextArea(3, 5)] private string _description;
        [SerializeField] private string _manufacturer;
        [SerializeField] private string _modelNumber;
        
        [Header("Physical Properties")]
        [SerializeField] private Vector3 _dimensions = Vector3.one;
        [SerializeField] private float _weight = 1f; // kg
        [SerializeField] private GameObject _equipmentPrefab;
        [SerializeField] private Sprite _equipmentIcon;
        
        [Header("Economic Properties")]
        [SerializeField] private float _purchaseCost = 100f;
        [SerializeField] private float _installationCost = 0f;
        [SerializeField] private float _maintenanceCostPerMonth = 5f;
        [SerializeField] private float _depreciationRate = 0.1f; // per year
        [SerializeField] private int _lifespanYears = 5;
        
        [Header("Power and Utilities")]
        [SerializeField] private float _powerConsumption = 100f; // watts
        [SerializeField] private float _waterConsumption = 0f; // liters per hour
        [SerializeField] private bool _requiresExternalVenting = false;
        [SerializeField] private float _heatGeneration = 0f; // watts thermal
        [SerializeField] private float _noiseLevel = 30f; // decibels
        [SerializeField, Range(0f, 1f)] private float _efficiency = 0.85f;
        
        [Header("Operational Parameters")]
        [SerializeField] private Vector2 _operationalTemperatureRange = new Vector2(-10f, 40f);
        [SerializeField] private Vector2 _operationalHumidityRange = new Vector2(10f, 90f);
        [SerializeField] private bool _canBeAutomated = true;
        [SerializeField] private float _setupTime = 1f; // hours
        [SerializeField] private DifficultyLevel _installationDifficulty = DifficultyLevel.Intermediate;
        
        [Header("Environmental Effects")]
        [SerializeField] private List<EnvironmentalEffect> _environmentalEffects = new List<EnvironmentalEffect>();
        [SerializeField] private float _effectiveRange = 2f; // meters
        [SerializeField] private EffectDistribution _effectDistribution = EffectDistribution.Uniform;
        
        [Header("Performance Characteristics")]
        [SerializeField] private PerformanceProfile _performanceProfile;
        [SerializeField] private List<OperatingMode> _operatingModes = new List<OperatingMode>();
        [SerializeField] private int _defaultOperatingModeIndex = 0;
        
        [Header("Reliability and Maintenance")]
        [SerializeField, Range(0f, 1f)] private float _reliability = 0.95f;
        [SerializeField] private float _meanTimeBetweenFailures = 8760f; // hours (1 year)
        [SerializeField] private float _meanTimeToRepair = 2f; // hours
        [SerializeField] private List<MaintenanceTask> _maintenanceTasks = new List<MaintenanceTask>();
        
        [Header("Compatibility and Requirements")]
        [SerializeField] private List<EquipmentDataSO> _requiredEquipment = new List<EquipmentDataSO>();
        [SerializeField] private List<EquipmentDataSO> _incompatibleEquipment = new List<EquipmentDataSO>();
        [SerializeField] private List<SkillRequirement> _skillRequirements = new List<SkillRequirement>();
        
        // Public Properties
        public string EquipmentName => _equipmentName;
        public EquipmentCategory Category => _category;
        public EquipmentType EquipmentType => _equipmentType;
        public string Description => _description;
        public string Manufacturer => _manufacturer;
        public string ModelNumber => _modelNumber;
        public Vector3 Dimensions => _dimensions;
        public float Weight => _weight;
        public GameObject EquipmentPrefab => _equipmentPrefab;
        public Sprite EquipmentIcon => _equipmentIcon;
        public float PurchaseCost => _purchaseCost;
        public float InstallationCost => _installationCost;
        public float MaintenanceCostPerMonth => _maintenanceCostPerMonth;
        public float DepreciationRate => _depreciationRate;
        public int LifespanYears => _lifespanYears;
        public float PowerConsumption => _powerConsumption;
        public float WaterConsumption => _waterConsumption;
        public bool RequiresExternalVenting => _requiresExternalVenting;
        public float HeatGeneration => _heatGeneration;
        public float NoiseLevel => _noiseLevel;
        public Vector2 OperationalTemperatureRange => _operationalTemperatureRange;
        public Vector2 OperationalHumidityRange => _operationalHumidityRange;
        public bool CanBeAutomated => _canBeAutomated;
        public float SetupTime => _setupTime;
        public DifficultyLevel InstallationDifficulty => _installationDifficulty;
        public List<EnvironmentalEffect> EnvironmentalEffects => _environmentalEffects;
        public float EffectiveRange => _effectiveRange;
        public EffectDistribution EffectDistribution => _effectDistribution;
        public PerformanceProfile PerformanceProfile => _performanceProfile;
        public List<OperatingMode> OperatingModes => _operatingModes;
        public int DefaultOperatingModeIndex => _defaultOperatingModeIndex;
        public float Reliability => _reliability;
        public float MeanTimeBetweenFailures => _meanTimeBetweenFailures;
        public float MeanTimeToRepair => _meanTimeToRepair;
        public List<MaintenanceTask> MaintenanceTasks => _maintenanceTasks;
        public List<EquipmentDataSO> RequiredEquipment => _requiredEquipment;
        public List<EquipmentDataSO> IncompatibleEquipment => _incompatibleEquipment;
        public List<SkillRequirement> SkillRequirements => _skillRequirements;
        
        // Additional properties for compatibility
        public string EquipmentId => name; // Use ScriptableObject name as ID
        public float EfficiencyRating => _efficiency; // Compatibility property for equipment placement
        public int MaintenanceInterval => _maintenanceTasks.Count > 0 ? 30 : 90; // Days between maintenance
        
        /// <summary>
        /// Calculates the total cost of ownership over the equipment's lifespan.
        /// </summary>
        public float CalculateTotalCostOfOwnership()
        {
            float initialCost = _purchaseCost + _installationCost;
            float maintenanceCost = _maintenanceCostPerMonth * 12f * _lifespanYears;
            float operatingCost = CalculateAnnualOperatingCost() * _lifespanYears;
            
            return initialCost + maintenanceCost + operatingCost;
        }
        
        /// <summary>
        /// Calculates annual operating cost based on power and water consumption.
        /// </summary>
        public float CalculateAnnualOperatingCost(float electricityRate = 0.12f, float waterRate = 0.001f)
        {
            // Assuming 16 hours operation per day, 365 days per year
            float hoursPerYear = 16f * 365f;
            
            float electricityCost = (_powerConsumption / 1000f) * hoursPerYear * electricityRate;
            float waterCost = _waterConsumption * hoursPerYear * waterRate;
            
            return electricityCost + waterCost;
        }
        
        /// <summary>
        /// Gets the environmental effect for a specific factor.
        /// </summary>
        public EnvironmentalEffect GetEnvironmentalEffect(EnvironmentalFactor factor)
        {
            return _environmentalEffects.Find(e => e.Factor == factor);
        }
        
        /// <summary>
        /// Checks if this equipment is compatible with another piece of equipment.
        /// </summary>
        public bool IsCompatibleWith(EquipmentDataSO otherEquipment)
        {
            return !_incompatibleEquipment.Contains(otherEquipment);
        }
        
        /// <summary>
        /// Gets the operating mode by index with bounds checking.
        /// </summary>
        public OperatingMode GetOperatingMode(int index)
        {
            if (index < 0 || index >= _operatingModes.Count) 
                return _operatingModes.Count > 0 ? _operatingModes[0] : null;
            
            return _operatingModes[index];
        }
        
        /// <summary>
        /// Calculates equipment efficiency based on environmental conditions.
        /// </summary>
        public float CalculateEfficiency(EnvironmentalConditions environment)
        {
            float temperatureEfficiency = CalculateTemperatureEfficiency(environment.Temperature);
            float humidityEfficiency = CalculateHumidityEfficiency(environment.Humidity);
            
            return (temperatureEfficiency + humidityEfficiency) * 0.5f;
        }
        
        private float CalculateTemperatureEfficiency(float temperature)
        {
            if (temperature >= _operationalTemperatureRange.x && temperature <= _operationalTemperatureRange.y)
                return 1f;
            
            float distance = 0f;
            if (temperature < _operationalTemperatureRange.x)
                distance = _operationalTemperatureRange.x - temperature;
            else
                distance = temperature - _operationalTemperatureRange.y;
            
            return Mathf.Max(0.1f, 1f - (distance / 20f));
        }
        
        private float CalculateHumidityEfficiency(float humidity)
        {
            if (humidity >= _operationalHumidityRange.x && humidity <= _operationalHumidityRange.y)
                return 1f;
            
            float distance = 0f;
            if (humidity < _operationalHumidityRange.x)
                distance = _operationalHumidityRange.x - humidity;
            else
                distance = humidity - _operationalHumidityRange.y;
            
            return Mathf.Max(0.1f, 1f - (distance / 50f));
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_equipmentName))
            {
                SharedLogger.LogError($"EquipmentDataSO '{name}' has no equipment name assigned.");
                isValid = false;
            }
            
            if (_powerConsumption < 0f)
            {
                SharedLogger.LogError($"EquipmentDataSO '{name}' has negative power consumption: {_powerConsumption}");
                isValid = false;
            }
            
            if (_efficiency < 0f || _efficiency > 1f)
            {
                SharedLogger.LogError($"EquipmentDataSO '{name}' has invalid efficiency: {_efficiency}. Must be between 0-1.");
                isValid = false;
            }
            
            if (_purchaseCost <= 0f)
            {
                SharedLogger.LogError($"EquipmentDataSO '{name}' has invalid purchase cost: {_purchaseCost}");
                isValid = false;
            }
            
            // Validate performance characteristics
            if (_performanceProfile.MaxOutput <= 0f)
            {
                SharedLogger.LogError($"EquipmentDataSO '{name}' has invalid max output: {_performanceProfile.MaxOutput}");
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    [System.Serializable]
    public class EnvironmentalEffect
    {
        public EnvironmentalFactor Factor;
        [Range(-100f, 100f)] public float EffectMagnitude = 10f;
        public EffectType EffectType = EffectType.Additive;
        public AnimationCurve EffectCurve;
        [TextArea(2, 3)] public string EffectDescription;
    }
    
    [System.Serializable]
    public class PerformanceProfile
    {
        [Range(0f, 100f)] public float MaxOutput = 100f;
        [Range(0f, 100f)] public float MinOutput = 0f;
        public AnimationCurve EfficiencyCurve;
        [Range(0f, 60f)] public float WarmupTime = 0f; // minutes
        [Range(0f, 60f)] public float CooldownTime = 0f; // minutes
    }
    
    [System.Serializable]
    public class OperatingMode
    {
        public string ModeName;
        [Range(0f, 2f)] public float PowerMultiplier = 1f;
        [Range(0f, 2f)] public float OutputMultiplier = 1f;
        [Range(0f, 2f)] public float EfficiencyMultiplier = 1f;
        [TextArea(2, 3)] public string ModeDescription;
    }
    
    [System.Serializable]
    public class MaintenanceTask
    {
        public string TaskName;
        public MaintenanceFrequency Frequency = MaintenanceFrequency.Monthly;
        public float TaskDuration = 1f; // hours
        public float TaskCost = 10f;
        public DifficultyLevel TaskDifficulty = DifficultyLevel.Beginner;
        [TextArea(2, 3)] public string TaskDescription;
    }
    
    [System.Serializable]
    public class SkillRequirement
    {
        public string SkillName;
        public int RequiredLevel = 1;
        public bool IsOptional = false;
        [TextArea(2, 3)] public string RequirementDescription;
    }
    
    public enum EquipmentCategory
    {
        Lighting,
        HVAC,
        Irrigation,
        Monitoring,
        Processing,
        Security,
        Automation,
        Utilities,
        Storage,
        Safety
    }
    
    public enum EquipmentType
    {
        // Generic/Unknown
        Generic,

        // Lighting
        LED_Light,
        HPS_Light,
        CMH_Light,
        Fluorescent_Light,
        GrowLight, // Generic grow light category
        Lighting, // Generic lighting category
        
        // HVAC
        Exhaust_Fan,
        Intake_Fan,
        Circulation_Fan,
        Air_Circulator, // Added missing enum value
        Air_Conditioner,
        Heater,
        Dehumidifier,
        Humidifier,
        HVAC, // Generic HVAC category
        Climate, // Generic climate control category
        
        // Irrigation
        Water_Pump,
        Drip_System,
        Sprinkler_System,
        Nutrient_Doser,
        pH_Controller,
        Watering_System, // Generic watering system
        Irrigation, // Generic irrigation category
        
        // Monitoring
        Temperature_Sensor,
        Humidity_Sensor,
        pH_Sensor,
        EC_Sensor,
        CO2_Sensor,
        Light_Sensor,
        Sensor, // Generic sensor category
        Monitoring, // Generic monitoring category
        
        // Processing
        Trimming_Machine,
        Drying_Rack,
        Curing_Container,
        Extraction_Unit,
        
        // Security
        Security_Camera,
        Access_Control,
        Alarm_System,
        Security, // Generic security category
        
        // Automation
        Timer_Controller,
        Environmental_Controller,
        Climate_Controller, // Alias for Environmental_Controller
        Irrigation_Controller,
        
        // Utilities
        Electrical_Panel,
        UPS_System,
        Generator,
        Power, // Generic power category
        
        // Storage
        Seed_Storage,
        Nutrient_Storage,
        Tool_Storage,
        Reservoir, // Water reservoir storage
        
        // Safety
        Fire_Suppression,
        Eye_Wash_Station,
        First_Aid_Kit,

        // Additional equipment types for cost database
        PumpSystem,
        ValveAssembly,
        SensorArray,
        ControlUnit,
        HeatExchanger,
        Compressor,
        FilterSystem,
        PowerDistribution
    }
    
    public enum EnvironmentalFactor
    {
        Temperature,
        Humidity,
        LightIntensity,
        CO2Level,
        AirFlow,
        pH,
        ElectricalConductivity,
        WaterLevel,
        Pressure
    }
    
    public enum EffectType
    {
        Additive,
        Multiplicative,
        Override,
        Modulation
    }
    
    public enum EffectDistribution
    {
        Uniform,
        Gradient,
        Focused,
        Irregular
    }
    
    public enum MaintenanceFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annually,
        AsNeeded
    }
    

}