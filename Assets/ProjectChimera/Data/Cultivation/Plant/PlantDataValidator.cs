using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Data Validator
    /// Single Responsibility: Validate plant data integrity
    /// Extracted from PlantDataSynchronizer (834 lines → 4 files <500 lines each)
    /// </summary>
    public class PlantDataValidator
    {
        private readonly bool _enableLogging;

        public PlantDataValidator(bool enableLogging = false)
        {
            _enableLogging = enableLogging;
        }

        /// <summary>
        /// Validate serialized plant data
        /// </summary>
        public (bool isValid, List<string> errors) ValidateData(SerializedPlantData data)
        {
            var errors = new List<string>();

            ValidateIdentityData(data, errors);
            ValidateRanges(data, errors);
            ValidateLogicalConsistency(data, errors);

            bool isValid = errors.Count == 0;

            if (!isValid && _enableLogging)
            {
                ChimeraLogger.LogWarning("PLANT", $"Data validation failed: {errors.Count} errors", null);
            }

            return (isValid, errors);
        }

        #region Validation Methods

        /// <summary>
        /// Validate identity data
        /// </summary>
        private void ValidateIdentityData(SerializedPlantData data, List<string> errors)
        {
            if (string.IsNullOrEmpty(data.PlantID))
            {
                errors.Add("Plant ID is required");
            }

            if (string.IsNullOrEmpty(data.PlantName))
            {
                errors.Add("Plant name is required");
            }

            if (data.GenerationNumber < 0)
            {
                errors.Add($"Invalid generation number: {data.GenerationNumber}");
            }
        }

        /// <summary>
        /// Validate value ranges
        /// </summary>
        private void ValidateRanges(SerializedPlantData data, List<string> errors)
        {
            // Health metrics (0-1 range)
            ValidateRange("OverallHealth", data.OverallHealth, 0f, 1f, errors);
            ValidateRange("Vigor", data.Vigor, 0f, 1f, errors);
            ValidateRange("StressLevel", data.StressLevel, 0f, 1f, errors);
            ValidateRange("MaturityLevel", data.MaturityLevel, 0f, 1f, errors);

            // Resource levels (0-1 range)
            ValidateRange("WaterLevel", data.WaterLevel, 0f, 1f, errors);
            ValidateRange("NutrientLevel", data.NutrientLevel, 0f, 1f, errors);
            ValidateRange("EnergyReserves", data.EnergyReserves, 0f, 1f, errors);

            // Growth metrics (0-1 range)
            ValidateRange("GrowthProgress", data.GrowthProgress, 0f, 1f, errors);
            ValidateRange("HarvestReadiness", data.HarvestReadiness, 0f, 1f, errors);

            // Physical measurements (positive values)
            if (data.CurrentHeight < 0f)
            {
                errors.Add($"Negative height: {data.CurrentHeight}");
            }

            if (data.CurrentWidth < 0f)
            {
                errors.Add($"Negative width: {data.CurrentWidth}");
            }

            if (data.LeafArea < 0f)
            {
                errors.Add($"Negative leaf area: {data.LeafArea}");
            }

            // Estimated values (positive)
            if (data.EstimatedYield < 0f)
            {
                errors.Add($"Negative estimated yield: {data.EstimatedYield}");
            }

            if (data.EstimatedPotency < 0f || data.EstimatedPotency > 1f)
            {
                errors.Add($"Potency out of range: {data.EstimatedPotency}");
            }
        }

        /// <summary>
        /// Validate logical consistency
        /// </summary>
        private void ValidateLogicalConsistency(SerializedPlantData data, List<string> errors)
        {
            // Age must be positive
            if (data.AgeInDays < 0f)
            {
                errors.Add($"Negative age: {data.AgeInDays}");
            }

            // Days in current stage must be <= total age
            if (data.DaysInCurrentStage > data.AgeInDays)
            {
                errors.Add($"Days in current stage ({data.DaysInCurrentStage}) exceeds total age ({data.AgeInDays})");
            }

            // Days in current stage must be positive
            if (data.DaysInCurrentStage < 0f)
            {
                errors.Add($"Negative days in current stage: {data.DaysInCurrentStage}");
            }

            // Creation date cannot be in the future
            if (data.CreationDate > System.DateTime.Now)
            {
                errors.Add($"Creation date is in the future: {data.CreationDate}");
            }

            // Harvest readiness consistency
            if (data.IsHarvested && data.HarvestReadiness < 0.5f)
            {
                errors.Add($"Plant marked as harvested but readiness was only {data.HarvestReadiness:F2}");
            }

            // Growth stage consistency
            if (data.CurrentGrowthStage == ProjectChimera.Data.Shared.PlantGrowthStage.Seedling && data.AgeInDays > 30f)
            {
                errors.Add($"Plant age ({data.AgeInDays} days) inconsistent with Seedling stage");
            }

            // Maturity vs growth stage
            if (data.MaturityLevel >= 1f && data.CurrentGrowthStage != ProjectChimera.Data.Shared.PlantGrowthStage.Flowering &&
                data.CurrentGrowthStage != ProjectChimera.Data.Shared.PlantGrowthStage.Ripening)
            {
                errors.Add($"Maturity at 100% but not in flowering/ripening stage: {data.CurrentGrowthStage}");
            }

            // Daily growth rate sanity check
            if (data.DailyGrowthRate < 0f)
            {
                errors.Add($"Negative daily growth rate: {data.DailyGrowthRate}");
            }

            if (data.DailyGrowthRate > 1f)
            {
                errors.Add($"Unrealistic daily growth rate: {data.DailyGrowthRate} (exceeds 100%)");
            }

            // Biomass accumulation sanity
            if (data.BiomassAccumulation < 0f)
            {
                errors.Add($"Negative biomass accumulation: {data.BiomassAccumulation}");
            }
        }

        /// <summary>
        /// Validate a value is within range
        /// </summary>
        private void ValidateRange(string fieldName, float value, float min, float max, List<string> errors)
        {
            if (value < min || value > max)
            {
                errors.Add($"{fieldName} out of range ({min}-{max}): {value}");
            }
        }

        #endregion

        #region Quick Validation Checks

        /// <summary>
        /// Quick check if basic identity data is valid
        /// </summary>
        public bool HasValidIdentity(SerializedPlantData data)
        {
            return !string.IsNullOrEmpty(data.PlantID) && !string.IsNullOrEmpty(data.PlantName);
        }

        /// <summary>
        /// Quick check if health metrics are in valid range
        /// </summary>
        public bool HasValidHealthMetrics(SerializedPlantData data)
        {
            return data.OverallHealth >= 0f && data.OverallHealth <= 1f &&
                   data.Vigor >= 0f && data.Vigor <= 1f &&
                   data.StressLevel >= 0f && data.StressLevel <= 1f;
        }

        /// <summary>
        /// Quick check if resource levels are in valid range
        /// </summary>
        public bool HasValidResourceLevels(SerializedPlantData data)
        {
            return data.WaterLevel >= 0f && data.WaterLevel <= 1f &&
                   data.NutrientLevel >= 0f && data.NutrientLevel <= 1f &&
                   data.EnergyReserves >= 0f && data.EnergyReserves <= 1f;
        }

        #endregion
    }
}

