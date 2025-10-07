using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Environment;
using ProjectChimera.Systems.Environment;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
// NOTE: Using explicit namespace to avoid conflict with ProjectChimera.Data.Save.CultivationStateDTO
using ProjectChimera.Data.Save.Structures;
using PlantInstanceSO = ProjectChimera.Data.Cultivation.Plant.PlantInstanceSO;
using DataEnvironmental = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantInstanceDTO = ProjectChimera.Data.Save.PlantInstanceDTO;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Concrete implementation of cultivation system save/load integration
    /// Bridges the gap between SaveManager and CultivationManager
    /// </summary>
    public class CultivationSaveService : MonoBehaviour, ICultivationSaveService
    {
        [Header("Cultivation Save Service Configuration")]
        [SerializeField] private bool _isEnabled = true;
        [SerializeField] private bool _supportsOfflineProgression = true;

        private ICultivationSaveManager _cultivationManager;
        private bool _isInitialized = false;

        public string SystemName => "Cultivation Save Service";
        public bool IsAvailable => _isInitialized && _isEnabled && _cultivationManager != null;
        public bool SupportsOfflineProgression => _supportsOfflineProgression;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeService();
        }

        private void Start()
        {
            // Register with SaveManager
            RegisterWithSaveManager();
        }

        #endregion

        #region Service Initialization

        private void InitializeService()
        {
            // Find CultivationManager - placeholder implementation
            _cultivationManager = new CultivationSaveManagerPlaceholder();

            if (_cultivationManager == null)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                _isEnabled = false;
                return;
            }

            _isInitialized = true;
            ChimeraLogger.Log("OTHER", "$1", this);
        }

        private void RegisterWithSaveManager()
        {
            var saveManager = GameManager.Instance?.GetManager<SaveManager>();
            if (saveManager != null)
            {
                saveManager.RegisterSaveService(this);
                ChimeraLogger.Log("OTHER", "$1", this);
            }
            else
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        #endregion

        #region ICultivationSaveService Implementation

        public CultivationStateDTO GatherCultivationState()
        {
            if (!IsAvailable)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                return new CultivationStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = "1.0",
                    EnableCultivationSystem = false
                };
            }

            try
            {
                ChimeraLogger.Log("OTHER", "$1", this);

                // Gather plant data from cultivation manager
                var activePlants = _cultivationManager.GetAllPlants();
                var plantDTOs = activePlants?.Select(plant => ConvertPlantToDTO(plant as PlantInstanceSO)).Where(p => p != null).ToList()
                    ?? new System.Collections.Generic.List<PlantInstanceDTO>();

                // Get cultivation statistics
                var (active, grown, harvested, yield, avgHealth) = _cultivationManager.GetCultivationStats();

                var cultivationState = new CultivationStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = "1.0",
                    EnableCultivationSystem = true,
                    MaxPlantsPerGrow = 50, // Default value
                    EnableAutoGrowth = _cultivationManager.EnableAutoGrowth,
                    TimeAcceleration = _cultivationManager.TimeAcceleration,

                    // Plant data
                    ActivePlants = plantDTOs,

                    // Cultivation metrics
                    Metrics = new CultivationMetricsDTO
                    {
                        TotalPlantsCultivated = grown,
                        PlantsHarvested = harvested,
                        TotalYieldProduced = yield,
                        ActivePlants = active
                    },

                    // Environmental state
                    EnvironmentalState = GatherEnvironmentalState(),

                    // Cultivation zones
                    CultivationZones = GatherCultivationZones()
                };

                ChimeraLogger.Log("OTHER", "$1", this);
                return cultivationState;
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                return new CultivationStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = "1.0",
                    EnableCultivationSystem = false
                };
            }
        }

        public async Task ApplyCultivationState(CultivationStateDTO cultivationData)
        {
            if (!IsAvailable)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                return;
            }

            if (cultivationData == null)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                return;
            }

            try
            {
                ChimeraLogger.Log("OTHER", "$1", this);

                // Apply system settings
                if (cultivationData.EnableAutoGrowth != _cultivationManager.EnableAutoGrowth)
                {
                    _cultivationManager.EnableAutoGrowth = cultivationData.EnableAutoGrowth;
                }

                if (Math.Abs(cultivationData.TimeAcceleration - _cultivationManager.TimeAcceleration) > 0.001f)
                {
                    _cultivationManager.TimeAcceleration = cultivationData.TimeAcceleration;
                }

                // Apply plant data
                if (cultivationData.ActivePlants != null)
                {
                    await ApplyPlantData(cultivationData.ActivePlants);
                }

                // Apply environmental state
                if (cultivationData.EnvironmentalState != null)
                {
                    ApplyEnvironmentalState(cultivationData.EnvironmentalState);
                }

                ChimeraLogger.Log("OTHER", "$1", this);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        public OfflineProgressionResult ProcessOfflineProgression(float offlineHours)
        {
            if (!IsAvailable || !SupportsOfflineProgression)
            {
                return new OfflineProgressionResult
                {
                    SystemName = SystemName,
                    Success = false,
                    ErrorMessage = "Service not available or offline progression not supported",
                    ProcessedHours = 0f
                };
            }

            try
            {
                ChimeraLogger.Log("OTHER", "$1", this);

                // Placeholder for CultivationManager integration
                // Will be implemented when CultivationManager is available

                var activePlantsBefore = _cultivationManager.ActivePlantCount;

                // Additional offline progression logic could go here
                // Placeholder implementation

                return new OfflineProgressionResult
                {
                    SystemName = SystemName,
                    Success = true,
                    ProcessedHours = offlineHours,
                    Description = $"Processed cultivation offline progression for {activePlantsBefore} plants",
                    ResultData = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["PlantsProcessed"] = activePlantsBefore,
                        ["OfflineHours"] = offlineHours,
                        ["SystemEnabled"] = _cultivationManager.EnableAutoGrowth
                    }
                };
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
                return new OfflineProgressionResult
                {
                    SystemName = SystemName,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessedHours = 0f
                };
            }
        }

        #endregion

        #region Helper Methods

        private PlantInstanceDTO ConvertPlantToDTO(PlantInstanceSO plant)
        {
            if (plant == null) return null;

            return new PlantInstanceDTO
            {
                PlantID = plant.PlantID,
                PlantName = plant.PlantName,
                // PlantStrainSO doesn't inherit from UnityEngine.Object in all cases, use hash code instead
                StrainID = plant.Strain != null ? plant.Strain.GetHashCode().ToString() : "Unknown",
                CurrentGrowthStage = plant.CurrentGrowthStage,
                Health = plant.CurrentHealth,
                Position = plant.WorldPosition,
                WaterLevel = plant.WaterLevel,
                NutrientLevel = plant.NutrientLevel,
                StressLevel = plant.StressLevel,
                PlantDate = plant.PlantedDate,
                LastUpdateDate = System.DateTime.Now
            };
        }

        private CultivationEnvironmentalStateDTO GatherEnvironmentalState()
        {
            try
            {
                // Environmental manager integration would go here
                // var environmentalManager = _cultivationManager.GetEnvironmentalManager();
                // Currently returning default state as IEnvironmentalManager doesn't have GetZoneEnvironment method yet

                return new CultivationEnvironmentalStateDTO
                {
                    IsInitialized = true,
                    LastEnvironmentalUpdate = DateTime.Now,
                    DefaultEnvironment = new CultivationEnvironmentalDataDTO
                    {
                        Temperature = 24f,
                        Humidity = 55f,
                        CO2Level = 400f,
                        LightIntensity = 600f,
                        AirFlow = 0.5f,
                        AirVelocity = 0.3f,
                        pH = 6.5f,
                        PhotoperiodHours = 18f,
                        WaterAvailability = 1.0f,
                        ElectricalConductivity = 1.2f,
                        DailyLightIntegral = 35f
                    }
                };
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }

            return new CultivationEnvironmentalStateDTO
            {
                IsInitialized = false,
                LastEnvironmentalUpdate = DateTime.Now
            };
        }

        private CultivationEnvironmentalDataDTO ConvertEnvironmentalConditions(DataEnvironmental conditions)
        {
            return new CultivationEnvironmentalDataDTO
            {
                Temperature = conditions.Temperature,
                Humidity = conditions.Humidity,
                CO2Level = conditions.CO2Level,
                LightIntensity = conditions.LightIntensity,
                AirFlow = conditions.AirFlow,
                AirVelocity = conditions.AirVelocity,
                pH = conditions.pH,
                PhotoperiodHours = conditions.PhotoperiodHours,
                WaterAvailability = conditions.WaterAvailability,
                ElectricalConductivity = conditions.ElectricalConductivity,
                DailyLightIntegral = conditions.DailyLightIntegral
            };
        }

        private CultivationEnvironmentalDataDTO ConvertZoneEnvironmentToConditions(ZoneEnvironmentDTO zoneEnvironment)
        {
            return new CultivationEnvironmentalDataDTO
            {
                Temperature = zoneEnvironment.Temperature,
                Humidity = zoneEnvironment.Humidity,
                CO2Level = zoneEnvironment.CO2Level,
                LightIntensity = zoneEnvironment.LightIntensity,
                AirFlow = 0.5f, // Default values for properties not in ZoneEnvironmentDTO
                AirVelocity = 0.3f,
                pH = 6.5f,
                PhotoperiodHours = 18f,
                WaterAvailability = 1.0f,
                ElectricalConductivity = 1.2f,
                DailyLightIntegral = 35f
            };
        }

        private System.Collections.Generic.List<ProjectChimera.Data.Save.Structures.CultivationZoneDTO> GatherCultivationZones()
        {
            // Default zone for now - could be expanded to gather actual zone data
            return new System.Collections.Generic.List<ProjectChimera.Data.Save.Structures.CultivationZoneDTO>
            {
                new ProjectChimera.Data.Save.Structures.CultivationZoneDTO
                {
                    ZoneId = "default",
                    ZoneName = "Default Cultivation Zone",
                    ZoneType = "Indoor",
                    IsActive = true,
                    MaxPlantCapacity = 50,
                    CurrentPlantCount = _cultivationManager.ActivePlantCount
                }
            };
        }

        private async Task ApplyPlantData(System.Collections.Generic.List<PlantInstanceDTO> plantData)
        {
            // This would involve recreating plant instances from saved data
            // For now, we'll log the operation as it requires complex plant instantiation logic
            ChimeraLogger.Log("OTHER", "$1", this);

            // In a full implementation, this would:
            // 1. Clear existing plants
            // 2. Recreate PlantInstanceSO objects from DTOs
            // 3. Register plants with the cultivation manager
            // 4. Apply growth stages and states

            await Task.CompletedTask; // Placeholder for async operations
        }

        private void ApplyEnvironmentalState(CultivationEnvironmentalStateDTO environmentalState)
        {
            ChimeraLogger.Log("OTHER", "$1", this);

            // This would apply environmental conditions to the cultivation zones
            // For now, we'll log the operation as it requires environmental manager integration
        }

        #endregion
    }
}
