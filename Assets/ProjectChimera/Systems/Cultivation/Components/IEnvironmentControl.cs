using ProjectChimera.Data.Shared;
using System;
using System.Collections.Generic;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Interface for environmental control and zone management
    /// </summary>
    public interface IEnvironmentControl
    {
        Dictionary<string, EnvironmentalConditions> ZoneEnvironments { get; }
        
        void SetZoneEnvironment(string zoneId, EnvironmentalConditions environment);
        EnvironmentalConditions GetZoneEnvironment(string zoneId);
        
        void ProcessEnvironmentalChanges(float deltaTime);
        void ProcessOfflineEnvironmentalChanges(float offlineHours);
        
        // Automation system management
        bool IsAutoWateringEnabled();
        bool IsAutoFeedingEnabled();
        void ProcessAutomationSystemWear(float offlineHours);
        
        // Equipment management
        void ProcessOfflineEquipmentMaintenance(float offlineHours);
        int SimulateEquipmentDegradation(float offlineHours);
        int CheckEquipmentMaintenanceAlerts(float offlineHours);
        
        // Environmental calculations
        float CalculateEnvironmentalStress(EnvironmentalConditions conditions);
        bool IsEnvironmentOptimal(string zoneId);
        EnvironmentalConditions GetOptimalEnvironmentForStage(PlantGrowthStage stage);
        
        // Events
        Action<string, EnvironmentalConditions> OnZoneEnvironmentChanged { get; set; }
        Action<string> OnEquipmentMaintenanceRequired { get; set; }
        Action<float> OnAutomationEfficiencyChanged { get; set; }
        
        void Initialize();
        void Shutdown();
    }
}
