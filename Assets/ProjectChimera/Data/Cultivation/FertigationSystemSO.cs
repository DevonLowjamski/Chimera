using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// SIMPLE: Basic fertigation system aligned with Project Chimera's cultivation needs.
    /// Focuses on essential watering and nutrient management without complex automation.
    /// </summary>
    [CreateAssetMenu(fileName = "Basic Fertigation System", menuName = "Project Chimera/Cultivation/Basic Fertigation")]
    public class FertigationSystemSO : ScriptableObject
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableBasicSystem = true;
        [SerializeField] private float _wateringFrequencyHours = 12f;
        [SerializeField] private float _nutrientFrequencyHours = 48f;
        [SerializeField] private float _waterAmountPerPlant = 0.5f; // liters

        [Header("Basic Nutrients")]
        [SerializeField] private float _nitrogenLevel = 1.0f;
        [SerializeField] private float _phosphorusLevel = 0.5f;
        [SerializeField] private float _potassiumLevel = 1.0f;
        [SerializeField] private float _phLevel = 6.0f;

        // Basic tracking
        [System.NonSerialized] private float _lastWateringTime;
        [System.NonSerialized] private float _lastNutrientTime;

        /// <summary>
        /// Check if plants need watering
        /// </summary>
        public bool NeedWatering(float currentTime)
        {
            if (!_enableBasicSystem) return false;
            return (currentTime - _lastWateringTime) >= (_wateringFrequencyHours * 3600f);
        }

        /// <summary>
        /// Check if plants need nutrients
        /// </summary>
        public bool NeedNutrients(float currentTime)
        {
            if (!_enableBasicSystem) return false;
            return (currentTime - _lastNutrientTime) >= (_nutrientFrequencyHours * 3600f);
        }

        /// <summary>
        /// Water plants
        /// </summary>
        public void WaterPlants(float currentTime)
        {
            _lastWateringTime = currentTime;
            // Basic watering logic would be implemented here
        }

        /// <summary>
        /// Apply nutrients
        /// </summary>
        public void ApplyNutrients(float currentTime)
        {
            _lastNutrientTime = currentTime;
            // Basic nutrient application logic would be implemented here
        }

        /// <summary>
        /// Get basic nutrient mix
        /// </summary>
        public NutrientMix GetNutrientMix()
        {
            return new NutrientMix
            {
                Nitrogen = _nitrogenLevel,
                Phosphorus = _phosphorusLevel,
                Potassium = _potassiumLevel,
                pH = _phLevel
            };
        }

        /// <summary>
        /// Get watering schedule info
        /// </summary>
        public WateringSchedule GetWateringSchedule()
        {
            return new WateringSchedule
            {
                FrequencyHours = _wateringFrequencyHours,
                AmountPerPlant = _waterAmountPerPlant,
                LastWateringTime = DateTime.FromBinary((long)(_lastWateringTime * TimeSpan.TicksPerSecond))
            };
        }

        /// <summary>
        /// Get nutrient schedule info
        /// </summary>
        public FeedingSchedule GetNutrientSchedule()
        {
            return new FeedingSchedule
            {
                FrequencyHours = _nutrientFrequencyHours,
                LastNutrientTime = DateTime.FromBinary((long)(_lastNutrientTime * TimeSpan.TicksPerSecond)),
                CurrentMix = GetNutrientMix()
            };
        }

        /// <summary>
        /// Reset system
        /// </summary>
        public void ResetSystem()
        {
            _lastWateringTime = 0f;
            _lastNutrientTime = 0f;
        }

        /// <summary>
        /// Get system status
        /// </summary>
        public FertigationStatus GetSystemStatus(float currentTime)
        {
            return new FertigationStatus
            {
                IsEnabled = _enableBasicSystem,
                NeedsWatering = NeedWatering(currentTime),
                NeedsNutrients = NeedNutrients(currentTime),
                HoursUntilWatering = _wateringFrequencyHours - ((currentTime - _lastWateringTime) / 3600f),
                HoursUntilNutrients = _nutrientFrequencyHours - ((currentTime - _lastNutrientTime) / 3600f)
            };
        }
    }

    /// <summary>
    /// Basic nutrient mix
    /// </summary>
    [System.Serializable]
    public struct NutrientMix
    {
        public float Nitrogen;
        public float Phosphorus;
        public float Potassium;
        public float pH;
    }

    // WateringSchedule struct moved to CultivationResults.cs to avoid duplication

    // NutrientSchedule struct moved to FertigationCalculator.cs to avoid duplication

    /// <summary>
    /// Fertigation system status
    /// </summary>
    [System.Serializable]
    public struct FertigationStatus
    {
        public bool IsEnabled;
        public bool NeedsWatering;
        public bool NeedsNutrients;
        public float HoursUntilWatering;
        public float HoursUntilNutrients;
    }
}
