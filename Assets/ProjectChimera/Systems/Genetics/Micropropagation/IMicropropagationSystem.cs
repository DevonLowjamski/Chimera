using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Data.Cultivation.Plant;

namespace ProjectChimera.Systems.Genetics.Micropropagation
{
    /// <summary>
    /// Interface for Micropropagation system.
    /// Allows for dependency injection and testing.
    /// </summary>
    public interface IMicropropagationSystem
    {
        // Events
        event Action<string> OnBatchCreated;
        event Action<string, MicropropagationStage> OnBatchStageChanged;
        event Action<string, int> OnBatchCompleted;
        event Action<string, float> OnBatchProgressChanged;

        // Batch lifecycle
        Task<MicropropagationBatch> CreateBatchAsync(string cultureSampleId, int quantity);
        List<PlantInstance> HarvestBatch(string batchId);

        // Query methods
        MicropropagationBatch GetBatch(string batchId);
        List<MicropropagationBatch> GetActiveBatches();
        List<MicropropagationBatch> GetCompletedBatches();
        MicropropagationStats GetStatistics();
        MicropropagationEstimate EstimateBatch(string cultureSampleId, int quantity);
    }
}
