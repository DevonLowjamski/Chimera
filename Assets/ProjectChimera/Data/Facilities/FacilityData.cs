using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Facilities
{
    /// <summary>
    /// SIMPLE: Basic facility data aligned with Project Chimera's facility needs.
    /// Focuses on essential facility information without complex management systems.
    /// </summary>

    /// <summary>
    /// Basic facility instance
    /// </summary>
    public class FacilityInstance
    {
        public string FacilityId;
        public string FacilityName;
        public FacilityType FacilityType;
        public Vector3 FacilitySize;
        public Vector3 FacilityPosition;
        public bool IsOperational;
        public float ConstructionProgress;
        public FacilityInfrastructure Infrastructure;
        public System.DateTime ConstructionStarted;
        public FacilityLicense License;
        public List<RoomData> Rooms = new List<RoomData>();
    }

    /// <summary>
    /// Basic room data
    /// </summary>
    [System.Serializable]
    public class RoomData
    {
        public string RoomId;
        public string RoomName;
        public RoomType RoomType;
        public Vector3 Position;
        public Vector3 Size;
        public bool IsConstructed;
        public List<EquipmentData> Equipment = new List<EquipmentData>();
        public List<PlantData> Plants = new List<PlantData>();
    }

    /// <summary>
    /// Basic equipment data
    /// </summary>
    [System.Serializable]
    public class EquipmentData
    {
        public string EquipmentId;
        public string EquipmentName;
        public EquipmentType EquipmentType;
        public Vector3 Position;
        public bool IsOperational;
        public float PowerConsumption;
    }

    /// <summary>
    /// Basic plant data
    /// </summary>
    [System.Serializable]
    public class PlantData
    {
        public string PlantId;
        public string StrainName;
        public Vector3 Position;
        public float Health;
        public float Age;
    }


    /// <summary>
    /// Facility utilities
    /// </summary>
    public static class FacilityUtilities
    {
        /// <summary>
        /// Calculate facility power consumption
        /// </summary>
        public static float CalculatePowerConsumption(FacilityInstance facility)
        {
            float totalPower = 0f;

            foreach (var room in facility.Rooms)
            {
                foreach (var equipment in room.Equipment)
                {
                    if (equipment.IsOperational)
                    {
                        totalPower += equipment.PowerConsumption;
                    }
                }
            }

            return totalPower;
        }

        /// <summary>
        /// Get facility statistics
        /// </summary>
        public static FacilityStatistics GetFacilityStatistics(FacilityInstance facility)
        {
            int totalRooms = facility.Rooms.Count;
            int constructedRooms = facility.Rooms.FindAll(r => r.IsConstructed).Count;
            int totalEquipment = 0;
            int operationalEquipment = 0;
            int totalPlants = 0;
            int healthyPlants = 0;

            foreach (var room in facility.Rooms)
            {
                totalEquipment += room.Equipment.Count;
                operationalEquipment += room.Equipment.FindAll(e => e.IsOperational).Count;
                totalPlants += room.Plants.Count;
                healthyPlants += room.Plants.FindAll(p => p.Health > 0.7f).Count;
            }

            return new FacilityStatistics
            {
                TotalRooms = totalRooms,
                ConstructedRooms = constructedRooms,
                TotalEquipment = totalEquipment,
                OperationalEquipment = operationalEquipment,
                TotalPlants = totalPlants,
                HealthyPlants = healthyPlants,
                PowerConsumption = CalculatePowerConsumption(facility),
                ConstructionProgress = facility.ConstructionProgress
            };
        }

        /// <summary>
        /// Check if facility is fully operational
        /// </summary>
        public static bool IsFullyOperational(FacilityInstance facility)
        {
            if (!facility.IsOperational) return false;
            if (facility.ConstructionProgress < 1f) return false;

            foreach (var room in facility.Rooms)
            {
                if (!room.IsConstructed) return false;

                foreach (var equipment in room.Equipment)
                {
                    if (!equipment.IsOperational) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get rooms by type
        /// </summary>
        public static List<RoomData> GetRoomsByType(FacilityInstance facility, RoomType roomType)
        {
            return facility.Rooms.FindAll(r => r.RoomType == roomType);
        }

        /// <summary>
        /// Get equipment by type
        /// </summary>
        public static List<EquipmentData> GetEquipmentByType(FacilityInstance facility, EquipmentType equipmentType)
        {
            var equipment = new List<EquipmentData>();

            foreach (var room in facility.Rooms)
            {
                equipment.AddRange(room.Equipment.FindAll(e => e.EquipmentType == equipmentType));
            }

            return equipment;
        }
    }

    /// <summary>
    /// Facility statistics
    /// </summary>
    [System.Serializable]
    public struct FacilityStatistics
    {
        public int TotalRooms;
        public int ConstructedRooms;
        public int TotalEquipment;
        public int OperationalEquipment;
        public int TotalPlants;
        public int HealthyPlants;
        public float PowerConsumption;
        public float ConstructionProgress;
    }

    // Note: Infrastructure classes (FacilityInfrastructure, ElectricalSystem, PlumbingSystem, etc.)
    // are defined in FacilityConfigSO.cs and should be used from there to avoid duplicates
}
