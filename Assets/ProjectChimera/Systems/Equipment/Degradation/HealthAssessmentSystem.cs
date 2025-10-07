using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// SIMPLE: Basic equipment health assessment aligned with Project Chimera's equipment degradation vision.
    /// Focuses on essential health tracking for player-managed equipment maintenance.
    /// </summary>
    public static class HealthAssessmentSystem
    {
        private static EquipmentHealthAssessment _currentSystemHealth;
        private static DateTime _lastAssessment = DateTime.MinValue;
        private static readonly TimeSpan _assessmentInterval = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Simple system health assessment
        /// </summary>
        public static EquipmentHealthAssessment PerformSystemHealthAssessment()
        {
            // Check if assessment is needed
            if (DateTime.Now - _lastAssessment < _assessmentInterval && _currentSystemHealth != null)
            {
                return _currentSystemHealth;
            }

            var allEquipment = EquipmentInstance.GetAllEquipment();

            _currentSystemHealth = new EquipmentHealthAssessment
            {
                LastAssessment = DateTime.Now,
                EquipmentHealthDetails = new Dictionary<string, EquipmentHealthData>()
            };

            // Calculate simple overall system health
            float totalHealth = 0f;
            int equipmentCount = allEquipment.Count;

            foreach (var equipment in allEquipment)
            {
                var healthData = EquipmentInstance.GetHealthAssessment(equipment.EquipmentId);
                if (healthData != null)
                {
                    _currentSystemHealth.EquipmentHealthDetails[equipment.EquipmentId] = healthData;
                    totalHealth += healthData.OverallHealth;
                }
            }

            _currentSystemHealth.OverallSystemHealth = equipmentCount > 0 ? totalHealth / equipmentCount : 1f;

            _lastAssessment = DateTime.Now;

            ProjectChimera.Core.Logging.ChimeraLogger.Log("EQUIPMENT", "System health assessment performed", null);

            return _currentSystemHealth;
        }

        /// <summary>
        /// Get current system health
        /// </summary>
        public static EquipmentHealthAssessment GetCurrentSystemHealth()
        {
            if (_currentSystemHealth == null || DateTime.Now - _lastAssessment > _assessmentInterval)
            {
                return PerformSystemHealthAssessment();
            }
            return _currentSystemHealth;
        }

        /// <summary>
        /// Simple equipment health assessment
        /// </summary>
        public static EquipmentHealthData AssessEquipmentHealth(string equipmentId)
        {
            var equipment = EquipmentInstance.GetEquipment(equipmentId);
            if (equipment == null) return null;

            return EquipmentInstance.GetHealthAssessment(equipmentId);
        }

        /// <summary>
        /// Check if equipment needs maintenance
        /// </summary>
        public static bool NeedsMaintenance(string equipmentId)
        {
            var healthData = AssessEquipmentHealth(equipmentId);
            return healthData != null && healthData.OverallHealth < 0.5f;
        }

        /// <summary>
        /// Get equipment health score
        /// </summary>
        public static float GetEquipmentHealthScore(string equipmentId)
        {
            var healthData = AssessEquipmentHealth(equipmentId);
            return healthData?.OverallHealth ?? 1f;
        }
    }

    /// <summary>
    /// Simple equipment health assessment data
    /// </summary>
    [Serializable]
    public class EquipmentHealthAssessment
    {
        public DateTime LastAssessment;
        public float OverallSystemHealth = 1f;
        public float CriticalEquipmentHealth = 1f;
        public Dictionary<string, EquipmentHealthData> EquipmentHealthDetails;
    }

    /// <summary>
    /// Basic equipment health data
    /// </summary>
    [Serializable]
    public class EquipmentHealthData
    {
        public string EquipmentId;
        public float OverallHealth = 1f;
        public float WearLevel = 0f;
        public EquipmentCondition Condition = EquipmentCondition.Good;
        public DateTime LastMaintenance;
    }

    /// <summary>
    /// Simple equipment condition enum
    /// </summary>
    public enum EquipmentCondition
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical
    }
}
