using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// SIMPLE: Basic fertigation validation aligned with Project Chimera's cultivation needs.
    /// Focuses on essential validation for watering and nutrient management.
    /// </summary>
    public static class FertigationValidator
    {
        /// <summary>
        /// Basic pH validation
        /// </summary>
        public const float OptimalPhMin = 5.5f;
        public const float OptimalPhMax = 6.5f;

        /// <summary>
        /// Basic EC validation (nutrient concentration)
        /// </summary>
        public const float OptimalEcMin = 1.0f;
        public const float OptimalEcMax = 2.5f;

        /// <summary>
        /// Validate pH level
        /// </summary>
        public static PhValidationResult ValidatePh(float phLevel)
        {
            var result = new PhValidationResult
            {
                CurrentPh = phLevel,
                IsValid = phLevel >= OptimalPhMin && phLevel <= OptimalPhMax
            };

            if (phLevel < OptimalPhMin)
            {
                result.Status = "Too acidic - add base";
                result.AdjustmentNeeded = OptimalPhMin - phLevel;
            }
            else if (phLevel > OptimalPhMax)
            {
                result.Status = "Too alkaline - add acid";
                result.AdjustmentNeeded = phLevel - OptimalPhMax;
            }
            else
            {
                result.Status = "Optimal pH";
                result.AdjustmentNeeded = 0f;
            }

            return result;
        }

        /// <summary>
        /// Validate EC level
        /// </summary>
        public static EcValidationResult ValidateEc(float ecLevel)
        {
            var result = new EcValidationResult
            {
                CurrentEc = ecLevel,
                IsValid = ecLevel >= OptimalEcMin && ecLevel <= OptimalEcMax
            };

            if (ecLevel < OptimalEcMin)
            {
                result.Status = "Nutrients too low - add fertilizer";
                result.AdjustmentNeeded = OptimalEcMin - ecLevel;
            }
            else if (ecLevel > OptimalEcMax)
            {
                result.Status = "Nutrients too high - add water";
                result.AdjustmentNeeded = ecLevel - OptimalEcMax;
            }
            else
            {
                result.Status = "Optimal nutrient level";
                result.AdjustmentNeeded = 0f;
            }

            return result;
        }

        /// <summary>
        /// Validate watering schedule
        /// </summary>
        public static WateringValidationResult ValidateWatering(float hoursSinceLastWatering, float wateringFrequencyHours)
        {
            var result = new WateringValidationResult
            {
                HoursSinceLastWatering = hoursSinceLastWatering,
                WateringFrequencyHours = wateringFrequencyHours,
                NeedsWatering = hoursSinceLastWatering >= wateringFrequencyHours
            };

            if (result.NeedsWatering)
            {
                result.Status = "Plants need watering";
                result.HoursOverdue = hoursSinceLastWatering - wateringFrequencyHours;
            }
            else
            {
                result.Status = "Watering schedule OK";
                result.HoursUntilNext = wateringFrequencyHours - hoursSinceLastWatering;
            }

            return result;
        }

        /// <summary>
        /// Validate nutrient schedule
        /// </summary>
        public static NutrientValidationResult ValidateNutrients(float hoursSinceLastNutrients, float nutrientFrequencyHours)
        {
            var result = new NutrientValidationResult
            {
                HoursSinceLastNutrients = hoursSinceLastNutrients,
                NutrientFrequencyHours = nutrientFrequencyHours,
                NeedsNutrients = hoursSinceLastNutrients >= nutrientFrequencyHours
            };

            if (result.NeedsNutrients)
            {
                result.Status = "Plants need nutrients";
                result.HoursOverdue = hoursSinceLastNutrients - nutrientFrequencyHours;
            }
            else
            {
                result.Status = "Nutrient schedule OK";
                result.HoursUntilNext = nutrientFrequencyHours - hoursSinceLastNutrients;
            }

            return result;
        }

        /// <summary>
        /// Get overall system validation
        /// </summary>
        public static SystemValidationResult ValidateSystem(float phLevel, float ecLevel, float hoursSinceWatering, float hoursSinceNutrients, float wateringFrequency, float nutrientFrequency)
        {
            var phResult = ValidatePh(phLevel);
            var ecResult = ValidateEc(ecLevel);
            var waterResult = ValidateWatering(hoursSinceWatering, wateringFrequency);
            var nutrientResult = ValidateNutrients(hoursSinceNutrients, nutrientFrequency);

            var result = new SystemValidationResult
            {
                PhValidation = phResult,
                EcValidation = ecResult,
                WateringValidation = waterResult,
                NutrientValidation = nutrientResult,
                IsSystemHealthy = phResult.IsValid && ecResult.IsValid,
                NeedsAttention = waterResult.NeedsWatering || nutrientResult.NeedsNutrients
            };

            // Count issues
            int issueCount = 0;
            if (!phResult.IsValid) issueCount++;
            if (!ecResult.IsValid) issueCount++;
            if (waterResult.NeedsWatering) issueCount++;
            if (nutrientResult.NeedsNutrients) issueCount++;

            result.IssueCount = issueCount;
            result.SystemStatus = issueCount == 0 ? "All systems optimal" :
                                issueCount == 1 ? "Minor issue detected" :
                                $"{issueCount} issues need attention";

            return result;
        }

        /// <summary>
        /// Get recommended actions
        /// </summary>
        public static List<string> GetRecommendedActions(SystemValidationResult validation)
        {
            var actions = new List<string>();

            if (validation.PhValidation.AdjustmentNeeded != 0)
            {
                actions.Add(validation.PhValidation.Status);
            }

            if (validation.EcValidation.AdjustmentNeeded != 0)
            {
                actions.Add(validation.EcValidation.Status);
            }

            if (validation.WateringValidation.NeedsWatering)
            {
                actions.Add($"Water plants - {validation.WateringValidation.HoursOverdue:F1} hours overdue");
            }

            if (validation.NutrientValidation.NeedsNutrients)
            {
                actions.Add($"Apply nutrients - {validation.NutrientValidation.HoursOverdue:F1} hours overdue");
            }

            return actions;
        }
    }

    /// <summary>
    /// pH validation result
    /// </summary>
    [System.Serializable]
    public struct PhValidationResult
    {
        public float CurrentPh;
        public bool IsValid;
        public string Status;
        public float AdjustmentNeeded;
    }

    /// <summary>
    /// EC validation result
    /// </summary>
    [System.Serializable]
    public struct EcValidationResult
    {
        public float CurrentEc;
        public bool IsValid;
        public string Status;
        public float AdjustmentNeeded;
    }

    /// <summary>
    /// Watering validation result
    /// </summary>
    [System.Serializable]
    public struct WateringValidationResult
    {
        public float HoursSinceLastWatering;
        public float WateringFrequencyHours;
        public bool NeedsWatering;
        public string Status;
        public float HoursOverdue;
        public float HoursUntilNext;
    }

    /// <summary>
    /// Nutrient validation result
    /// </summary>
    [System.Serializable]
    public struct NutrientValidationResult
    {
        public float HoursSinceLastNutrients;
        public float NutrientFrequencyHours;
        public bool NeedsNutrients;
        public string Status;
        public float HoursOverdue;
        public float HoursUntilNext;
    }

    /// <summary>
    /// System validation result
    /// </summary>
    [System.Serializable]
    public struct SystemValidationResult
    {
        public PhValidationResult PhValidation;
        public EcValidationResult EcValidation;
        public WateringValidationResult WateringValidation;
        public NutrientValidationResult NutrientValidation;
        public bool IsSystemHealthy;
        public bool NeedsAttention;
        public int IssueCount;
        public string SystemStatus;
    }
}
