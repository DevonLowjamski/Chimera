using System;
using UnityEngine;

namespace ProjectChimera.Data.Simulation.HVAC.Miscellaneous
{
    /// <summary>
    /// Core HVAC control types and strategies
    /// </summary>
    public enum HVACControlType
    {
        Temperature,
        Humidity,
        Airflow,
        Pressure,
        CO2,
        VPD,
        Combined
    }

    /// <summary>
    /// Control strategies for HVAC systems
    /// </summary>
    public enum ControlStrategy
    {
        PID,
        OnOff,
        Proportional,
        BangBang,
        FuzzyLogic,
        Adaptive,
        ScheduleBased
    }

    /// <summary>
    /// Equipment operational status
    /// </summary>
    public enum EquipmentStatus
    {
        Offline,
        Starting,
        Running,
        Stopping,
        Maintenance,
        Fault,
        Standby
    }

    /// <summary>
    /// Heating method types
    /// </summary>
    public enum HeatingMethod
    {
        ElectricResistance,
        GasFired,
        HeatPump,
        Radiant,
        ForcedAir,
        Hydronic,
        Infrared
    }

    /// <summary>
    /// Cooling method types
    /// </summary>
    public enum CoolingMethod
    {
        Compressor,
        Evaporative,
        Absorption,
        Desiccant,
        ChilledWater,
        AirCooled,
        WaterCooled
    }

    /// <summary>
    /// Humidification method types
    /// </summary>
    public enum HumidificationMethod
    {
        Steam,
        Ultrasonic,
        Evaporative,
        Impeller,
        WettedMedia,
        Electrode
    }

    /// <summary>
    /// Dehumidification method types
    /// </summary>
    public enum DehumidificationMethod
    {
        Refrigerative,
        Desiccant,
        Ventilative,
        HeatPipe,
        Membrane
    }

    /// <summary>
    /// Fan type classifications
    /// </summary>
    public enum FanType
    {
        Axial,
        Centrifugal,
        MixedFlow,
        Inline,
        RoofMounted,
        WallMounted,
        CeilingMounted
    }

    /// <summary>
    /// Ventilation system types
    /// </summary>
    public enum VentilationType
    {
        Natural,
        Mechanical,
        Hybrid,
        DemandControlled,
        EnergyRecovery,
        Displacement,
        Underfloor
    }

    /// <summary>
    /// HVAC alarm types for system monitoring
    /// </summary>
    public enum HVACAlarmType
    {
        PowerFailure,
        SensorFailure,
        TemperatureOutOfRange,
        HumidityOutOfRange,
        EquipmentFailure,
        CommunicationError,
        MaintenanceRequired,
        Performance,
        Safety,
        FilterClogged,
        RefrigerantLeak,
        Overload,
        CalibrationRequired
    }

    /// <summary>
    /// Priority levels for HVAC alarms
    /// </summary>
    public enum HVACAlarmPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Types of maintenance operations
    /// </summary>
    public enum MaintenanceType
    {
        Inspection,
        Cleaning,
        Calibration,
        Replacement,
        Lubrication,
        Testing,
        Repair,
        Upgrade
    }

    /// <summary>
    /// Priority levels for maintenance tasks
    /// </summary>
    public enum MaintenancePriority
    {
        Routine,
        Scheduled,
        Urgent,
        Emergency
    }

    /// <summary>
    /// Timeframes for predictions and forecasts
    /// </summary>
    public enum PredictionTimeframe
    {
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Seasonal
    }
}
