using System;
using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Data.Simulation.HVAC.Miscellaneous
{
    /// <summary>
    /// HVAC equipment types and configurations
    /// Defines specific equipment classes for different HVAC functions
    /// </summary>

    /// <summary>
    /// Active HVAC equipment instance
    /// </summary>
    [System.Serializable]
    public class ActiveHVACEquipment
    {
        public string EquipmentId;
        public string EquipmentName;
        public HVACEquipmentType EquipmentType;
        public string ZoneId;
        public Vector3 Position;
        public bool IsActive = true;
        public float Efficiency = 1.0f;
        public float PowerConsumption;
        public float Capacity;
        public DateTime LastMaintenance;
        public EquipmentStatus Status = EquipmentStatus.Running;

        /// <summary>
        /// Get equipment status description
        /// </summary>
        public string GetStatusDescription()
        {
            return $"{EquipmentName}: {Status} ({Efficiency:P0} efficiency)";
        }

        /// <summary>
        /// Check if equipment needs maintenance
        /// </summary>
        public bool NeedsMaintenance()
        {
            return (DateTime.Now - LastMaintenance).TotalDays > 30;
        }

        /// <summary>
        /// Calculate current power consumption
        /// </summary>
        public float GetCurrentPowerConsumption()
        {
            return IsActive ? PowerConsumption * Efficiency : 0f;
        }
    }

    /// <summary>
    /// Heating equipment scriptable object
    /// </summary>
    [CreateAssetMenu(fileName = "HeatingEquipment", menuName = "Project Chimera/HVAC/Heating Equipment")]
    public class HeatingEquipmentSO : EquipmentDataSO
    {
        [Header("Heating Specifications")]
        public HeatingMethod HeatingMethod;
        [Range(1000, 50000)] public float HeatingCapacity; // BTU/hr
        [Range(0.5f, 5f)] public float FuelEfficiency = 0.9f;
        public float MaxOperatingTemperature = 80f;

        [Header("Fuel Information")]
        public string FuelType;
        public float FuelConsumptionRate; // units per hour at full capacity
        public float FuelCapacity; // tank capacity

        [Header("Safety Features")]
        public bool HasOverheatProtection = true;
        public bool HasFlameSafety = true;
        public float SafetyShutdownTemperature = 85f;

        /// <summary>
        /// Calculate heating output at given capacity
        /// </summary>
        public float CalculateHeatingOutput(float capacityPercentage)
        {
            return HeatingCapacity * Mathf.Clamp01(capacityPercentage) * FuelEfficiency;
        }

        /// <summary>
        /// Calculate fuel consumption
        /// </summary>
        public float CalculateFuelConsumption(float capacityPercentage)
        {
            return FuelConsumptionRate * Mathf.Clamp01(capacityPercentage);
        }

        /// <summary>
        /// Check if temperature is within safe operating range
        /// </summary>
        public bool IsTemperatureSafe(float temperature)
        {
            return temperature <= MaxOperatingTemperature;
        }
    }

    /// <summary>
    /// Cooling equipment scriptable object
    /// </summary>
    [CreateAssetMenu(fileName = "CoolingEquipment", menuName = "Project Chimera/HVAC/Cooling Equipment")]
    public class CoolingEquipmentSO : EquipmentDataSO
    {
        [Header("Cooling Specifications")]
        public CoolingMethod CoolingMethod;
        [Range(5000, 100000)] public float CoolingCapacity; // BTU/hr
        [Range(0.5f, 4f)] public float EnergyEfficiencyRatio = 3.0f; // EER
        public float MinOperatingTemperature = 10f;

        [Header("Refrigerant Information")]
        public string RefrigerantType;
        public float RefrigerantCharge; // kg
        public float RefrigerantLeakageRate = 0.01f; // % per year

        [Header("Maintenance")]
        public int HoursBetweenFilterCleaning = 720; // 30 days
        public int HoursBetweenCoilCleaning = 2160; // 90 days

        /// <summary>
        /// Calculate cooling output
        /// </summary>
        public float CalculateCoolingOutput(float capacityPercentage)
        {
            return CoolingCapacity * Mathf.Clamp01(capacityPercentage);
        }

        /// <summary>
        /// Calculate power consumption
        /// </summary>
        public float CalculatePowerConsumption(float capacityPercentage)
        {
            return (CoolingCapacity * Mathf.Clamp01(capacityPercentage)) / EnergyEfficiencyRatio;
        }

        /// <summary>
        /// Check if temperature is within operating range
        /// </summary>
        public bool IsTemperatureInRange(float temperature)
        {
            return temperature >= MinOperatingTemperature;
        }
    }

    /// <summary>
    /// Humidification equipment scriptable object
    /// </summary>
    [CreateAssetMenu(fileName = "HumidificationEquipment", menuName = "Project Chimera/HVAC/Humidification Equipment")]
    public class HumidificationEquipmentSO : EquipmentDataSO
    {
        [Header("Humidification Specifications")]
        public HumidificationMethod HumidificationMethod;
        [Range(1, 50)] public float HumidificationCapacity; // kg/hr
        [Range(0.1f, 2f)] public float EnergyEfficiency = 0.8f;

        [Header("Water System")]
        public float TankCapacity = 20f; // liters
        public float WaterConsumptionRate; // liters per hour
        public bool HasAutoFill = true;

        [Header("Maintenance")]
        public int HoursBetweenDescaling = 720; // 30 days
        public float MineralBuildupRate = 0.1f; // % per day

        /// <summary>
        /// Calculate humidification output
        /// </summary>
        public float CalculateHumidificationOutput(float capacityPercentage)
        {
            return HumidificationCapacity * Mathf.Clamp01(capacityPercentage) * EnergyEfficiency;
        }

        /// <summary>
        /// Calculate water consumption
        /// </summary>
        public float CalculateWaterConsumption(float capacityPercentage)
        {
            return WaterConsumptionRate * Mathf.Clamp01(capacityPercentage);
        }

        /// <summary>
        /// Check if water tank needs filling
        /// </summary>
        public bool NeedsWater(float currentWaterLevel)
        {
            return currentWaterLevel < TankCapacity * 0.1f; // 10% threshold
        }
    }

    /// <summary>
    /// Dehumidification equipment scriptable object
    /// </summary>
    [CreateAssetMenu(fileName = "DehumidificationEquipment", menuName = "Project Chimera/HVAC/Dehumidification Equipment")]
    public class DehumidificationEquipmentSO : EquipmentDataSO
    {
        [Header("Dehumidification Specifications")]
        public DehumidificationMethod DehumidificationMethod;
        [Range(10, 200)] public float DehumidificationCapacity; // liters/day
        [Range(0.5f, 3f)] public float EnergyEfficiency = 1.5f; // liters/kWh

        [Header("Drainage")]
        public bool HasAutoDrain = true;
        public float DrainCapacity = 5f; // liters
        public bool HasOverflowProtection = true;

        [Header("Maintenance")]
        public int HoursBetweenFilterCleaning = 480; // 20 days
        public int HoursBetweenCoilCleaning = 1440; // 60 days

        /// <summary>
        /// Calculate dehumidification output
        /// </summary>
        public float CalculateDehumidificationOutput(float capacityPercentage)
        {
            return DehumidificationCapacity * Mathf.Clamp01(capacityPercentage);
        }

        /// <summary>
        /// Calculate power consumption
        /// </summary>
        public float CalculatePowerConsumption(float capacityPercentage)
        {
            return (DehumidificationCapacity * Mathf.Clamp01(capacityPercentage)) / EnergyEfficiency;
        }

        /// <summary>
        /// Check if drain needs emptying
        /// </summary>
        public bool NeedsDraining(float currentDrainLevel)
        {
            return currentDrainLevel >= DrainCapacity * 0.9f; // 90% threshold
        }
    }

    /// <summary>
    /// Fan equipment scriptable object
    /// </summary>
    [CreateAssetMenu(fileName = "FanEquipment", menuName = "Project Chimera/HVAC/Fan Equipment")]
    public class FanEquipmentSO : EquipmentDataSO
    {
        [Header("Fan Specifications")]
        public FanType FanType;
        [Range(100, 5000)] public float AirflowRate; // CFM
        [Range(0.1f, 1f)] public float StaticPressure = 0.5f; // inches water column
        [Range(50, 3000)] public float MaxRPM = 1500;

        [Header("Power")]
        [Range(10, 500)] public float PowerConsumption; // watts
        [Range(0.1f, 1f)] public float PowerFactor = 0.9f;

        [Header("Noise")]
        [Range(20, 80)] public float SoundPressureLevel = 45f; // dB

        /// <summary>
        /// Calculate airflow at given speed
        /// </summary>
        public float CalculateAirflow(float speedPercentage)
        {
            return AirflowRate * Mathf.Clamp01(speedPercentage);
        }

        /// <summary>
        /// Calculate power consumption at given speed
        /// </summary>
        public float CalculatePowerAtSpeed(float speedPercentage)
        {
            return PowerConsumption * Mathf.Pow(Mathf.Clamp01(speedPercentage), 1.5f);
        }

        /// <summary>
        /// Calculate noise level at given speed
        /// </summary>
        public float CalculateNoiseLevel(float speedPercentage)
        {
            return SoundPressureLevel + (10f * Mathf.Log10(Mathf.Max(0.1f, speedPercentage)));
        }
    }

    /// <summary>
    /// Ventilation equipment scriptable object
    /// </summary>
    [CreateAssetMenu(fileName = "VentilationEquipment", menuName = "Project Chimera/HVAC/Ventilation Equipment")]
    public class VentilationEquipmentSO : EquipmentDataSO
    {
        [Header("Ventilation Specifications")]
        public VentilationType VentilationType;
        [Range(50, 2000)] public float AirExchangeRate; // ACH (air changes per hour)
        [Range(100, 10000)] public float MaxAirflowRate; // CFM

        [Header("Filtration")]
        [Range(0.1f, 0.99f)] public float FilterEfficiency = 0.8f; // MERV rating equivalent
        public string FilterType;
        public int HoursBetweenFilterChange = 2160; // 90 days

        [Header("Ducting")]
        public float DuctLength; // meters
        public float DuctDiameter; // meters
        public float DuctPressureDrop; // Pa

        /// <summary>
        /// Calculate air exchange rate for given volume
        /// </summary>
        public float CalculateACH(float roomVolumeCubicMeters)
        {
            return (MaxAirflowRate * 60f) / roomVolumeCubicMeters; // Convert CFM to ACH
        }

        /// <summary>
        /// Calculate pressure drop through duct system
        /// </summary>
        public float CalculatePressureDrop(float airflowRate)
        {
            // Simplified calculation: pressure drop proportional to square of flow rate
            float flowRatio = airflowRate / MaxAirflowRate;
            return DuctPressureDrop * flowRatio * flowRatio;
        }

        /// <summary>
        /// Calculate filter efficiency degradation
        /// </summary>
        public float CalculateFilterEfficiency(float hoursInUse)
        {
            // Efficiency decreases over time due to dust loading
            float degradation = Mathf.Min(hoursInUse / (HoursBetweenFilterChange * 24f), 1f);
            return FilterEfficiency * (1f - degradation * 0.3f); // 30% max degradation
        }
    }


    /// <summary>
    /// HVAC equipment type enumeration
    /// </summary>
    public enum HVACEquipmentType
    {
        Heating,
        Cooling,
        Humidification,
        Dehumidification,
        Ventilation,
        Fan,
        AirPurification,
        ControlSystem
    }
}
