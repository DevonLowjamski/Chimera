using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// SIMPLE: Basic maintenance system aligned with Project Chimera's equipment degradation vision.
    /// Focuses on essential maintenance tracking for player-managed equipment care.
    /// </summary>
    public static class MaintenanceSystem
    {
        private static readonly Dictionary<string, MaintenanceRecord> _maintenanceRecords = new Dictionary<string, MaintenanceRecord>();
        private static readonly Dictionary<string, DateTime> _lastMaintenance = new Dictionary<string, DateTime>();

        /// <summary>
        /// Perform basic maintenance on equipment
        /// </summary>
        public static void PerformMaintenance(string equipmentId, MaintenanceType type = MaintenanceType.Preventive)
        {
            var record = new MaintenanceRecord
            {
                RecordId = Guid.NewGuid().ToString("N")[..8],
                EquipmentId = equipmentId,
                MaintenanceDate = DateTime.Now,
                Type = type,
                Cost = GetMaintenanceCost(type),
                EffectivenessScore = 0.8f
            };

            _maintenanceRecords[record.RecordId] = record;
            _lastMaintenance[equipmentId] = DateTime.Now;

            // Repair the equipment
            EquipmentInstance.RepairEquipment(equipmentId, record.EffectivenessScore, 0f);

            ChimeraLogger.Log($"[MaintenanceSystem] Maintenance performed: {equipmentId} ({type}) - Cost: ${record.Cost:F2}");
        }

        /// <summary>
        /// Check if equipment needs maintenance
        /// </summary>
        public static bool NeedsMaintenance(string equipmentId)
        {
            if (!_lastMaintenance.TryGetValue(equipmentId, out var lastMaintenance))
                return true; // Never maintained

            // Simple check - needs maintenance every 30 days
            return (DateTime.Now - lastMaintenance).TotalDays > 30;
        }

        /// <summary>
        /// Get last maintenance date for equipment
        /// </summary>
        public static DateTime GetLastMaintenanceDate(string equipmentId)
        {
            return _lastMaintenance.GetValueOrDefault(equipmentId, DateTime.MinValue);
        }

        /// <summary>
        /// Get maintenance history for equipment
        /// </summary>
        public static List<MaintenanceRecord> GetMaintenanceHistory(string equipmentId)
        {
            return _maintenanceRecords.Values.Where(r => r.EquipmentId == equipmentId).ToList();
        }

        /// <summary>
        /// Get total maintenance cost for equipment
        /// </summary>
        public static float GetTotalMaintenanceCost(string equipmentId)
        {
            return _maintenanceRecords.Values.Where(r => r.EquipmentId == equipmentId).Sum(r => r.Cost);
        }

        /// <summary>
        /// Get maintenance statistics
        /// </summary>
        public static MaintenanceStatistics GetMaintenanceStatistics()
        {
            var records = _maintenanceRecords.Values.ToList();
            var preventiveCount = records.Count(r => r.Type == MaintenanceType.Preventive);
            var correctiveCount = records.Count(r => r.Type == MaintenanceType.Corrective);

            return new MaintenanceStatistics
            {
                TotalMaintenanceEvents = records.Count,
                PreventiveMaintenanceCount = preventiveCount,
                CorrectiveMaintenanceCount = correctiveCount,
                TotalMaintenanceCost = records.Sum(r => r.Cost),
                AverageEffectiveness = records.Count > 0 ? records.Average(r => r.EffectivenessScore) : 0f
            };
        }

        /// <summary>
        /// Clear maintenance records
        /// </summary>
        public static void ClearRecords()
        {
            _maintenanceRecords.Clear();
            _lastMaintenance.Clear();

            ChimeraLogger.Log("[MaintenanceSystem] Maintenance records cleared");
        }

        #region Private Methods

        private static float GetMaintenanceCost(MaintenanceType type)
        {
            switch (type)
            {
                case MaintenanceType.Preventive:
                    return 50f;
                case MaintenanceType.Corrective:
                    return 150f;
                default:
                    return 100f;
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic maintenance record
    /// </summary>
    [System.Serializable]
    public class MaintenanceRecord
    {
        public string RecordId;
        public string EquipmentId;
        public DateTime MaintenanceDate;
        public MaintenanceType Type;
        public float Cost;
        public float EffectivenessScore;
    }

    /// <summary>
    /// Maintenance type enum
    /// </summary>
    public enum MaintenanceType
    {
        Preventive,
        Corrective
    }

    /// <summary>
    /// Basic maintenance statistics
    /// </summary>
    [System.Serializable]
    public class MaintenanceStatistics
    {
        public int TotalMaintenanceEvents;
        public int PreventiveMaintenanceCount;
        public int CorrectiveMaintenanceCount;
        public float TotalMaintenanceCost;
        public float AverageEffectiveness;
    }
}
