using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    public class UpdateStatistics
    {
        public int TotalUpdates { get; set; }
        public int SuccessfulUpdates { get; set; }
        public int FailedUpdates { get; set; }
        public double AverageUpdateTime { get; set; }
        public double MaxUpdateTime { get; set; }
        public DateTime LastUpdate { get; set; }
        public int ActivePlants { get; set; }
        public int ProcessedBatches { get; set; }

        /// <summary>
        /// Get success rate percentage
        /// </summary>
        public double GetSuccessRate()
        {
            return TotalUpdates > 0 ? (double)SuccessfulUpdates / TotalUpdates * 100.0 : 0.0;
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        public void Reset()
        {
            TotalUpdates = 0;
            SuccessfulUpdates = 0;
            FailedUpdates = 0;
            AverageUpdateTime = 0.0;
            MaxUpdateTime = 0.0;
            ActivePlants = 0;
            ProcessedBatches = 0;
            LastUpdate = DateTime.Now;
        }

        /// <summary>
        /// Record a successful update
        /// </summary>
        public void RecordUpdate(double updateTime, bool success)
        {
            TotalUpdates++;
            if (success)
                SuccessfulUpdates++;
            else
                FailedUpdates++;

            // Update running average
            AverageUpdateTime = (AverageUpdateTime * (TotalUpdates - 1) + updateTime) / TotalUpdates;

            if (updateTime > MaxUpdateTime)
                MaxUpdateTime = updateTime;

            LastUpdate = DateTime.Now;
        }
    }
}
