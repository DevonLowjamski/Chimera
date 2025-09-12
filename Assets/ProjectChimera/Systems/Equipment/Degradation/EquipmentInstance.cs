using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// BASIC: Simple equipment instance management for Project Chimera.
    /// Focuses on essential equipment tracking without complex degradation modeling.
    /// </summary>
    public static class EquipmentInstance
    {
        // Basic equipment registry
        private static readonly Dictionary<string, BasicEquipmentData> _equipmentRegistry = new Dictionary<string, BasicEquipmentData>();

        /// <summary>
        /// Register basic equipment
        /// </summary>
        public static void RegisterEquipment(string equipmentId, EquipmentType type, string name, Vector3 location)
        {
            if (_equipmentRegistry.ContainsKey(equipmentId))
            {
                UpdateEquipment(equipmentId, location);
                return;
            }

            var equipment = new BasicEquipmentData
            {
                EquipmentId = equipmentId,
                Type = type,
                Name = name,
                Location = location,
                InstallationDate = System.DateTime.Now,
                IsOperational = true,
                Efficiency = 1f,
                Age = 0f
            };

            _equipmentRegistry[equipmentId] = equipment;

            ChimeraLogger.Log($"[EquipmentInstance] Registered equipment: {equipmentId} ({type})");
        }

        /// <summary>
        /// Update equipment location
        /// </summary>
        public static void UpdateEquipment(string equipmentId, Vector3 location)
        {
            if (_equipmentRegistry.TryGetValue(equipmentId, out var equipment))
            {
                equipment.Location = location;
                equipment.LastUpdated = System.DateTime.Now;
            }
        }

        /// <summary>
        /// Remove equipment
        /// </summary>
        public static void RemoveEquipment(string equipmentId)
        {
            if (_equipmentRegistry.Remove(equipmentId))
            {
                ChimeraLogger.Log($"[EquipmentInstance] Removed equipment: {equipmentId}");
            }
        }

        /// <summary>
        /// Get equipment by ID
        /// </summary>
        public static BasicEquipmentData GetEquipment(string equipmentId)
        {
            return _equipmentRegistry.TryGetValue(equipmentId, out var equipment) ? equipment : null;
        }

        /// <summary>
        /// Get all equipment
        /// </summary>
        public static List<BasicEquipmentData> GetAllEquipment()
        {
            return new List<BasicEquipmentData>(_equipmentRegistry.Values);
        }

        /// <summary>
        /// Update equipment age and efficiency
        /// </summary>
        public static void UpdateEquipmentAge(string equipmentId, float deltaTime)
        {
            if (_equipmentRegistry.TryGetValue(equipmentId, out var equipment))
            {
                equipment.Age += deltaTime / 86400f; // Convert to days

                // Simple efficiency degradation based on age
                var profile = EquipmentTypes.GetProfile(equipment.Type);
                if (profile != null)
                {
                    float ageRatio = equipment.Age / profile.BaseLifespan;
                    equipment.Efficiency = Mathf.Max(0.5f, 1f - ageRatio * 0.5f); // Degrade to 50% over lifespan

                    // Check if equipment should fail
                    if (equipment.Age > profile.BaseLifespan && Random.value < 0.01f) // 1% chance per update when over lifespan
                    {
                        equipment.IsOperational = false;
                        ChimeraLogger.LogWarning($"[EquipmentInstance] Equipment failed: {equipmentId}");
                    }
                }
            }
        }

        /// <summary>
        /// Perform maintenance on equipment
        /// </summary>
        public static void PerformMaintenance(string equipmentId)
        {
            if (_equipmentRegistry.TryGetValue(equipmentId, out var equipment))
            {
                equipment.Efficiency = Mathf.Min(1f, equipment.Efficiency + 0.2f); // Restore 20% efficiency
                equipment.LastMaintenance = System.DateTime.Now;

                ChimeraLogger.Log($"[EquipmentInstance] Maintenance performed on: {equipmentId}");
            }
        }

        /// <summary>
        /// Check if equipment needs maintenance
        /// </summary>
        public static bool NeedsMaintenance(string equipmentId)
        {
            var equipment = GetEquipment(equipmentId);
            if (equipment == null) return false;

            var profile = EquipmentTypes.GetProfile(equipment.Type);
            if (profile == null) return false;

            if (equipment.LastMaintenance == System.DateTime.MinValue)
            {
                return equipment.Age > profile.MaintenanceInterval;
            }

            var timeSinceMaintenance = System.DateTime.Now - equipment.LastMaintenance;
            return timeSinceMaintenance.TotalDays > profile.MaintenanceInterval;
        }

        /// <summary>
        /// Get equipment statistics
        /// </summary>
        public static EquipmentStats GetEquipmentStats()
        {
            var allEquipment = GetAllEquipment();
            int totalEquipment = allEquipment.Count;
            int operationalEquipment = allEquipment.FindAll(e => e.IsOperational).Count;
            int needsMaintenance = allEquipment.FindAll(e => NeedsMaintenance(e.EquipmentId)).Count;
            float averageEfficiency = totalEquipment > 0 ?
                allEquipment.Average(e => e.Efficiency) : 0f;

            return new EquipmentStats
            {
                TotalEquipment = totalEquipment,
                OperationalEquipment = operationalEquipment,
                EquipmentNeedingMaintenance = needsMaintenance,
                AverageEfficiency = averageEfficiency
            };
        }

        /// <summary>
        /// Clear all equipment
        /// </summary>
        public static void ClearAllEquipment()
        {
            _equipmentRegistry.Clear();
            ChimeraLogger.Log("[EquipmentInstance] Cleared all equipment");
        }
    }

    /// <summary>
    /// Basic equipment data
    /// </summary>
    [System.Serializable]
    public class BasicEquipmentData
    {
        public string EquipmentId;
        public EquipmentType Type;
        public string Name;
        public Vector3 Location;
        public System.DateTime InstallationDate;
        public System.DateTime LastMaintenance;
        public System.DateTime LastUpdated;
        public bool IsOperational;
        public float Efficiency;
        public float Age; // in years
    }

    /// <summary>
    /// Equipment statistics
    /// </summary>
    [System.Serializable]
    public struct EquipmentStats
    {
        public int TotalEquipment;
        public int OperationalEquipment;
        public int EquipmentNeedingMaintenance;
        public float AverageEfficiency;
    }
}
