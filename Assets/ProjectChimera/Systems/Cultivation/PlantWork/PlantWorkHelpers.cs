using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Cultivation.PlantWork
{
    /// <summary>
    /// Helper utilities for Plant Work operations.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>
    public static class PlantWorkHelpers
    {
        /// <summary>
        /// Checks if specific work type has been applied to plant.
        /// </summary>
        public static bool HasWorkApplied(Dictionary<string, List<PlantWorkRecord>> workHistory,
            string plantId, PlantWorkType workType)
        {
            if (!workHistory.ContainsKey(plantId))
                return false;

            return workHistory[plantId].Any(r => r.WorkType == workType);
        }

        /// <summary>
        /// Adds work record to history.
        /// </summary>
        public static void AddWorkRecord(Dictionary<string, List<PlantWorkRecord>> workHistory,
            string plantId, PlantWorkRecord record)
        {
            if (!workHistory.ContainsKey(plantId))
                workHistory[plantId] = new List<PlantWorkRecord>();

            workHistory[plantId].Add(record);
        }

        /// <summary>
        /// Gets or creates effects for plant.
        /// </summary>
        public static PlantWorkEffects GetOrCreateEffects(Dictionary<string, PlantWorkEffects> activeEffects,
            string plantId)
        {
            if (!activeEffects.ContainsKey(plantId))
            {
                activeEffects[plantId] = new PlantWorkEffects
                {
                    PlantId = plantId,
                    YieldMultiplier = 1.0f,
                    HeightMultiplier = 1.0f,
                    QualityMultiplier = 1.0f,
                    ColaCount = 1,
                    LightPenetration = 0f,
                    NutrientUptake = 1.0f,
                    CurrentStress = 0f,
                    IsScrogged = false
                };
            }

            return activeEffects[plantId];
        }
    }
}
