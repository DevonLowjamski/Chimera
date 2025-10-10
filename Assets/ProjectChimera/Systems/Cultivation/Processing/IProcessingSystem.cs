using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Cultivation.Processing
{
    /// <summary>
    /// Interface for Processing system - drying and curing operations.
    /// </summary>
    public interface IProcessingSystem
    {
        #region Events

        event Action<string> OnDryingStarted;
        event Action<string> OnDryingCompleted;
        event Action<string> OnCuringStarted;
        event Action<string> OnCuringCompleted;
        event Action<string, ProcessingQuality> OnQualityGraded;

        #endregion

        #region Drying Operations

        /// <summary>
        /// Starts drying process for harvested material.
        /// </summary>
        bool StartDrying(string batchId, float wetWeightGrams, DryingMethod method);

        /// <summary>
        /// Gets drying batch info.
        /// </summary>
        DryingBatch GetDryingBatch(string batchId);

        /// <summary>
        /// Gets all active drying batches.
        /// </summary>
        List<DryingBatch> GetActiveDryingBatches();

        #endregion

        #region Curing Operations

        /// <summary>
        /// Starts curing process for dried material.
        /// </summary>
        bool StartCuring(string batchId, float dryWeightGrams, CuringMethod method);

        /// <summary>
        /// Performs burping on jar curing batch.
        /// </summary>
        bool BurpJars(string batchId);

        /// <summary>
        /// Gets curing batch info.
        /// </summary>
        CuringBatch GetCuringBatch(string batchId);

        /// <summary>
        /// Gets all active curing batches.
        /// </summary>
        List<CuringBatch> GetActiveCuringBatches();

        #endregion

        #region Quality Grading

        /// <summary>
        /// Gets market value multiplier for quality grade.
        /// </summary>
        float GetQualityMultiplier(ProcessingQuality quality);

        #endregion

        #region Statistics

        /// <summary>
        /// Gets processing statistics for UI display.
        /// </summary>
        ProcessingStats GetStatistics();

        #endregion
    }
}
