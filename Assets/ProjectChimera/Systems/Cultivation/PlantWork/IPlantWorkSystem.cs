using System;
using System.Collections.Generic;
using ProjectChimera.Data.Cultivation.Plant;

namespace ProjectChimera.Systems.Cultivation.PlantWork
{
    /// <summary>
    /// Interface for Plant Work system.
    /// Allows for dependency injection and testing.
    /// </summary>
    public interface IPlantWorkSystem
    {
        // Events
        event Action<string, PlantWorkType> OnWorkPerformed;
        event Action<string, string> OnWorkFailed;

        // Pruning operations
        bool ApplyTopping(string plantId, PlantInstance plant);
        bool ApplyFIMming(string plantId, PlantInstance plant);
        bool ApplyLollipopping(string plantId, PlantInstance plant);
        bool ApplyDefoliation(string plantId, PlantInstance plant);

        // Training operations
        bool ApplyLST(string plantId, PlantInstance plant);
        bool ApplyHST(string plantId, PlantInstance plant);
        bool ApplyScrOG(string plantId, PlantInstance plant);
        bool ApplySupercropping(string plantId, PlantInstance plant);

        // Query methods
        List<PlantWorkRecord> GetWorkHistory(string plantId);
        PlantWorkEffects GetEffects(string plantId);
        PlantWorkStats GetStatistics();
    }
}
