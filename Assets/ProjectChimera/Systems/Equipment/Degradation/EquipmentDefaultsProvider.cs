// REFACTORED: Equipment Defaults Provider
// Extracted from EquipmentDegradationManager for better separation of concerns

using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Provides default reliability and lifespan values for equipment types
    /// </summary>
    public static class EquipmentDefaultsProvider
    {
        /// <summary>
        /// Get default Mean Time Between Failures (in hours) for equipment type
        /// </summary>
        public static float GetDefaultMTBF(EquipmentType type)
        {
            return type switch
            {
                // Lighting (1 year)
                EquipmentType.LED_Light => 8760f,
                EquipmentType.HPS_Light => 8760f,
                EquipmentType.GrowLight => 8760f,

                // Fans (6 months)
                EquipmentType.Exhaust_Fan => 4380f,
                EquipmentType.Intake_Fan => 4380f,
                EquipmentType.Air_Circulator => 4380f,

                // Watering Systems (3 months)
                EquipmentType.Watering_System => 2190f,
                EquipmentType.Drip_System => 2190f,

                // Controllers (4 months)
                EquipmentType.Climate_Controller => 2920f,
                EquipmentType.Environmental_Controller => 2920f,

                // Reservoirs (1 year)
                EquipmentType.Reservoir => 8760f,

                // Default (6 months)
                _ => 4380f
            };
        }

        /// <summary>
        /// Get default lifespan (in years) for equipment type
        /// </summary>
        public static float GetDefaultLifespan(EquipmentType type)
        {
            return type switch
            {
                // Lighting (3 years)
                EquipmentType.LED_Light => 3f,
                EquipmentType.HPS_Light => 3f,
                EquipmentType.GrowLight => 3f,

                // Fans (8 years)
                EquipmentType.Exhaust_Fan => 8f,
                EquipmentType.Intake_Fan => 8f,
                EquipmentType.Air_Circulator => 8f,

                // Watering Systems (5 years)
                EquipmentType.Watering_System => 5f,
                EquipmentType.Drip_System => 5f,

                // Controllers (10 years)
                EquipmentType.Climate_Controller => 10f,
                EquipmentType.Environmental_Controller => 10f,

                // Reservoirs (15 years)
                EquipmentType.Reservoir => 15f,

                // Default (7 years)
                _ => 7f
            };
        }

        /// <summary>
        /// Get default failure rate (probability per hour) for equipment type
        /// </summary>
        public static float GetDefaultFailureRate(EquipmentType type)
        {
            return type switch
            {
                // Lighting (Low failure rate)
                EquipmentType.LED_Light => 0.001f,
                EquipmentType.HPS_Light => 0.001f,
                EquipmentType.GrowLight => 0.001f,

                // Fans (Higher failure rate for mechanical components)
                EquipmentType.Exhaust_Fan => 0.005f,
                EquipmentType.Intake_Fan => 0.005f,
                EquipmentType.Air_Circulator => 0.005f,

                // Watering Systems (Medium failure rate)
                EquipmentType.Watering_System => 0.003f,
                EquipmentType.Drip_System => 0.003f,

                // Controllers (Higher failure rate for complex electronics)
                EquipmentType.Climate_Controller => 0.004f,
                EquipmentType.Environmental_Controller => 0.004f,

                // Reservoirs (Very low failure rate)
                EquipmentType.Reservoir => 0.0005f,

                // Default
                _ => 0.002f
            };
        }

        /// <summary>
        /// Create a complete default reliability profile for equipment type
        /// </summary>
        public static EquipmentReliabilityProfile CreateDefaultProfile(EquipmentType type, float baseDegradationRate)
        {
            return new EquipmentReliabilityProfile
            {
                Type = type,
                MeanTimeBetweenFailures = GetDefaultMTBF(type),
                AverageLifespan = GetDefaultLifespan(type),
                FailureRate = GetDefaultFailureRate(type),
                WearProgressionRate = baseDegradationRate,
                CriticalWearThreshold = 0.8f
            };
        }
    }
}

