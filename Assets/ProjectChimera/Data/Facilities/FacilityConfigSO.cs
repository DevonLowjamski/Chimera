using UnityEngine;
using ProjectChimera.Shared;
using System.Collections.Generic;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Data.Facilities
{
    /// <summary>
    /// ScriptableObject for facility configuration templates and construction parameters.
    /// Defines preset facility layouts, equipment combinations, and construction requirements.
    /// </summary>
    [CreateAssetMenu(fileName = "New Facility Config", menuName = "Project Chimera/Facilities/Facility Config", order = 2)]
    public class FacilityConfigSO : ChimeraConfigSO
    {
        [Header("Facility Template")]
        [SerializeField] private string _facilityName = "Standard Indoor Facility";
        [SerializeField] private FacilityType _facilityType = FacilityType.IndoorCultivation;
        [SerializeField] private Vector3 _defaultSize = new Vector3(20f, 3f, 15f);
        [SerializeField] private string _description = "";
        
        [Header("Room Configuration")]
        [SerializeField] private List<RoomTemplate> _roomTemplates = new List<RoomTemplate>();
        [SerializeField] private int _maxRooms = 10;
        [SerializeField] private float _hallwayPercentage = 0.15f;
        [SerializeField] private bool _requiresAirlock = true;
        
        [Header("Infrastructure Requirements")]
        [SerializeField] private InfrastructureRequirements _infrastructureRequirements;
        
        [Header("Equipment Presets")]
        [SerializeField] private List<EquipmentPreset> _equipmentPresets = new List<EquipmentPreset>();
        [SerializeField] private List<FacilityEquipmentDataSO> _recommendedEquipment = new List<FacilityEquipmentDataSO>();
        
        [Header("Construction Parameters")]
        [SerializeField] private ConstructionParameters _constructionParameters;
        
        [Header("Regulatory Requirements")]
        [SerializeField] private RegulatoryRequirements _regulatoryRequirements;
        
        [Header("Economic Projections")]
        [SerializeField] private float _estimatedConstructionCost = 100000f;
        [SerializeField] private float _annualOperatingCost = 50000f;
        [SerializeField] private float _expectedROI = 0.25f;
        [SerializeField] private int _paybackPeriod = 4; // Years
        
        // Public Properties
        public string FacilityName => _facilityName;
        public FacilityType FacilityType => _facilityType;
        public Vector3 DefaultSize => _defaultSize;
        public string Description => _description;
        public List<RoomTemplate> RoomTemplates => _roomTemplates;
        public int MaxRooms => _maxRooms;
        public float HallwayPercentage => _hallwayPercentage;
        public bool RequiresAirlock => _requiresAirlock;
        public InfrastructureRequirements InfrastructureRequirements => _infrastructureRequirements;
        public List<EquipmentPreset> EquipmentPresets => _equipmentPresets;
        public List<FacilityEquipmentDataSO> RecommendedEquipment => _recommendedEquipment;
        public ConstructionParameters ConstructionParameters => _constructionParameters;
        public RegulatoryRequirements RegulatoryRequirements => _regulatoryRequirements;
        public float EstimatedConstructionCost => _estimatedConstructionCost;
        public float AnnualOperatingCost => _annualOperatingCost;
        public float ExpectedROI => _expectedROI;
        public int PaybackPeriod => _paybackPeriod;
        
        /// <summary>
        /// Creates a new facility instance based on this configuration.
        /// </summary>
        public FacilityInstance CreateFacilityInstance(string facilityName, Vector3 position)
        {
            var facility = new FacilityInstance
            {
                FacilityId = System.Guid.NewGuid().ToString(),
                FacilityName = facilityName,
                FacilityType = _facilityType,
                FacilitySize = _defaultSize,
                Infrastructure = CreateInfrastructure(),
                ConstructionProgress = 0f,
                IsOperational = false,
                ConstructionStarted = System.DateTime.Now,
                License = CreateLicense()
            };
            
            // Create rooms from templates
            foreach (var template in _roomTemplates)
            {
                for (int i = 0; i < template.Quantity; i++)
                {
                    var room = CreateRoomFromTemplate(template, i);
                    facility.Rooms.Add(room);
                }
            }
            
            return facility;
        }
        
        /// <summary>
        /// Calculates total floor area for this facility configuration.
        /// </summary>
        public float CalculateTotalFloorArea()
        {
            float roomArea = 0f;
            foreach (var template in _roomTemplates)
            {
                float templateArea = template.Size.x * template.Size.z;
                roomArea += templateArea * template.Quantity;
            }
            
            float hallwayArea = roomArea * _hallwayPercentage;
            return roomArea + hallwayArea;
        }
        
        /// <summary>
        /// Estimates construction time based on complexity and size.
        /// </summary>
        public int EstimateConstructionDays()
        {
            float area = CalculateTotalFloorArea();
            float baseTime = area / 100f; // Base days per 100 sq ft
            
            // Complexity multipliers
            float complexityMultiplier = 1f;
            if (_infrastructureRequirements.RequiresSpecialElectrical) complexityMultiplier += 0.2f;
            if (_infrastructureRequirements.RequiresSpecialHVAC) complexityMultiplier += 0.3f;
            if (_infrastructureRequirements.RequiresSpecialPlumbing) complexityMultiplier += 0.15f;
            if (_regulatoryRequirements.RequiresInspections) complexityMultiplier += 0.1f;
            
            return Mathf.RoundToInt(baseTime * complexityMultiplier);
        }
        
        /// <summary>
        /// Gets equipment requirements for a specific room type.
        /// </summary>
        public List<FacilityEquipmentDataSO> GetRequiredEquipmentForRoom(RoomType roomType)
        {
            var equipment = new List<FacilityEquipmentDataSO>();
            
            foreach (var preset in _equipmentPresets)
            {
                if (preset.ApplicableRoomTypes.Contains(roomType))
                {
                    equipment.AddRange(preset.RequiredEquipment);
                }
            }
            
            return equipment;
        }
        
        public override bool ValidateData()
        {
            if (!base.ValidateData()) return false;
            
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_facilityName))
            {
                LogError("Facility name cannot be empty");
                isValid = false;
            }
            
            if (_defaultSize.x <= 0 || _defaultSize.y <= 0 || _defaultSize.z <= 0)
            {
                LogError("Default size must be positive in all dimensions");
                isValid = false;
            }
            
            if (_roomTemplates.Count == 0)
            {
                LogError("At least one room template is required");
                isValid = false;
            }
            
            if (_estimatedConstructionCost < 0)
            {
                LogError("Estimated construction cost cannot be negative");
                isValid = false;
            }
            
            // Validate room templates
            foreach (var template in _roomTemplates)
            {
                if (template.Quantity <= 0)
                {
                    LogError($"Room template {template.RoomType} quantity must be positive");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        private FacilityInfrastructure CreateInfrastructure()
        {
            return new FacilityInfrastructure
            {
                ElectricalSystem = new ElectricalSystem
                {
                    TotalCapacity = _infrastructureRequirements.ElectricalCapacity,
                    HasGroundFaultProtection = true,
                    PowerFactor = 0.95f
                },
                PlumbingSystem = new PlumbingSystem
                {
                    WaterSupply = new WaterSupply
                    {
                        Source = "Municipal",
                        Capacity = _infrastructureRequirements.WaterCapacity
                    },
                    HasBackflowPrevention = true
                },
                VentilationSystem = new VentilationSystem
                {
                    AirExchangeRate = 6f,
                    HasHeatRecovery = _infrastructureRequirements.RequiresHeatRecovery
                },
                SecuritySystem = new SecuritySystem
                {
                    HasCameras = _regulatoryRequirements.RequiresSecurity,
                    HasAlarms = true,
                    HasAccessControl = _regulatoryRequirements.RequiresAccessControl
                },
                FireSafety = new FireSafety
                {
                    HasSprinklers = _regulatoryRequirements.RequiresFireSuppression,
                    HasAlarms = true,
                    HasExtinguishers = true
                }
            };
        }
        
        private CultivationRoom CreateRoomFromTemplate(RoomTemplate template, int index)
        {
            return new CultivationRoom
            {
                RoomId = System.Guid.NewGuid().ToString(),
                RoomName = $"{template.RoomType}_{index + 1}",
                RoomType = template.RoomType,
                Size = template.Size,
                SecurityLevel = template.SecurityLevel,
                IsClimateControlled = template.RequiresClimateControl,
                ConstructionStatus = ConstructionStatus.Planned,
                EnvironmentController = new RoomEnvironmentController
                {
                    ControllerId = System.Guid.NewGuid().ToString(),
                    AutomationEnabled = true,
                    EmergencyShutdownEnabled = true,
                    Status = ControllerStatus.Inactive
                }
            };
        }
        
        private FacilityLicense CreateLicense()
        {
            return new FacilityLicense
            {
                LicenseNumber = "PENDING",
                LicenseType = LicenseType.Cultivation,
                IssueDate = System.DateTime.Now,
                ExpirationDate = System.DateTime.Now.AddYears(1),
                IssuingAuthority = "State Regulatory Authority",
                ComplianceStatus = ComplianceStatus.Compliant
            };
        }
    }
    
    /// <summary>
    /// Template for creating rooms within facilities.
    /// </summary>
    [System.Serializable]
    public class RoomTemplate
    {
        public string TemplateId = "";
        public RoomType RoomType = RoomType.Vegetative;
        public Vector3 Size = new Vector3(5f, 3f, 5f);
        public Vector2 Dimensions = new Vector2(5f, 5f);
        public int Quantity = 1;
        public SecurityLevel SecurityLevel = SecurityLevel.Secured;
        public bool RequiresClimateControl = true;
        public bool RequiresIrrigation = true;
        public bool RequiresSpecialVentilation = false;
        public List<EquipmentCategory> RequiredEquipmentTypes = new List<EquipmentCategory>();
        public float PowerRequirement = 5000f; // Watts
        public string Description = "";
        
        public RoomTemplate()
        {
            TemplateId = System.Guid.NewGuid().ToString();
            RequiredEquipmentTypes = new List<EquipmentCategory>();
        }
    }
    
    /// <summary>
    /// Infrastructure requirements for facility construction.
    /// </summary>
    [System.Serializable]
    public class InfrastructureRequirements
    {
        [Header("Electrical")]
        public float ElectricalCapacity = 50f;           // kW
        public bool RequiresSpecialElectrical = false;
        public bool Requires3Phase = true;
        public bool RequiresBackupPower = false;
        
        [Header("Plumbing")]
        public float WaterCapacity = 1000f;              // Gallons per day
        public bool RequiresSpecialPlumbing = false;
        public bool RequiresDrainSystem = true;
        public bool RequiresWaterTreatment = false;
        
        [Header("HVAC")]
        public bool RequiresSpecialHVAC = true;
        public float HVACCapacity = 10f;                 // Tons
        public bool RequiresHeatRecovery = false;
        public bool RequiresDehumidification = true;
        
        [Header("Network")]
        public bool RequiresHighSpeedInternet = true;
        public bool RequiresSecureNetwork = true;
        public bool RequiresRedundantConnections = false;
    }
    
    /// <summary>
    /// Equipment preset for specific applications.
    /// </summary>
    [System.Serializable]
    public class EquipmentPreset
    {
        public string PresetName = "Standard Lighting";
        public string Description = "";
        public List<RoomType> ApplicableRoomTypes = new List<RoomType>();
        public List<FacilityEquipmentDataSO> RequiredEquipment = new List<FacilityEquipmentDataSO>();
        public List<FacilityEquipmentDataSO> OptionalEquipment = new List<FacilityEquipmentDataSO>();
        public float TotalCost = 0f;
        public float PowerRequirement = 0f;
        public bool IsRecommended = true;
    }
    
    /// <summary>
    /// Construction parameters and requirements.
    /// </summary>
    [System.Serializable]
    public class ConstructionParameters
    {
        [Header("Timeline")]
        public int EstimatedDays = 60;
        public int MinimumDays = 30;
        public int MaximumDays = 120;
        
        [Header("Requirements")]
        public bool RequiresProfessionalInstallation = true;
        public bool RequiresPermits = true;
        public bool RequiresInspections = true;
        public List<string> RequiredTrades = new List<string> { "Electrical", "Plumbing", "HVAC" };
        
        [Header("Materials")]
        public string PreferredConstruction = "Steel Frame";
        public string InsulationType = "Spray Foam";
        public string FlooringType = "Epoxy";
        public string WallFinish = "Antimicrobial Paint";
        
        [Header("Special Features")]
        public bool RequiresCleanRoom = false;
        public bool RequiresAirlock = true;
        public bool RequiresWashStation = true;
        public bool RequiresEmergencyExits = true;
    }
    
    /// <summary>
    /// Regulatory and compliance requirements.
    /// </summary>
    [System.Serializable]
    public class RegulatoryRequirements
    {
        [Header("Licensing")]
        public bool RequiresBusinessLicense = true;
        public bool RequiresCultivationLicense = true;
        public bool RequiresSpecialPermits = false;
        public List<string> RequiredCertifications = new List<string>();
        
        [Header("Security")]
        public bool RequiresSecurity = true;
        public bool RequiresAccessControl = true;
        public bool RequiresVideoRecording = true;
        public int VideoRetentionDays = 90;
        
        [Header("Safety")]
        public bool RequiresFireSuppression = true;
        public bool RequiresEmergencyLighting = true;
        public bool RequiresFirstAid = true;
        public bool RequiresEyewashStation = false;
        
        [Header("Environmental")]
        public bool RequiresWasteManagement = true;
        public bool RequiresOdorControl = true;
        public bool RequiresNoiseControl = false;
        
        [Header("Inspections")]
        public bool RequiresInspections = true;
        public int InspectionFrequencyDays = 180;
        public List<string> InspectionTypes = new List<string> { "Fire", "Building", "Electrical" };
    }
}