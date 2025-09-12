using System;

namespace ProjectChimera.Data.Facilities
{
    /// <summary>
    /// Unified facility-related enumerations
    /// </summary>

    /// <summary>
    /// Facility types for Project Chimera
    /// </summary>
    public enum FacilityType
    {
        StorageBay,
        Warehouse,
        GrowRoom,
        ResearchLab,
        IndoorCultivation,
        OutdoorCultivation,
        StorageFacility
    }

    /// <summary>
    /// Room types within facilities
    /// </summary>
    public enum RoomType
    {
        Cultivation,
        Storage,
        Equipment,
        Utility,
        Vegetative,
        Flowering,
        Drying,
        Processing
    }

    /// <summary>
    /// Equipment types used in facilities
    /// </summary>
    public enum EquipmentType
    {
        Light,
        Ventilation,
        Irrigation,
        ClimateControl,
        Storage,
        Monitoring,
        Automation
    }

    // PlantGrowthStage enum moved to Data.Shared.DataStructs.cs to avoid duplication

    /// <summary>
    /// Facility operational status
    /// </summary>
    public enum FacilityStatus
    {
        Planned,
        UnderConstruction,
        Operational,
        Maintenance,
        Offline,
        Decommissioned
    }

    /// <summary>
    /// Construction phases
    /// </summary>
    public enum ConstructionPhase
    {
        Foundation,
        Framing,
        Utilities,
        Finishing,
        FinalInspection
    }
}
