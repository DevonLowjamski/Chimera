using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Component Synchronizer
    /// Single Responsibility: Handle bidirectional sync between components and serialized data
    /// Extracted from PlantDataSynchronizer (834 lines → 4 files <500 lines each)
    /// </summary>
    public class PlantComponentSynchronizer
    {
        // Component references
        private PlantIdentityManager _identityManager;
        private PlantStateCoordinator _stateCoordinator;
        private PlantResourceHandler _resourceHandler;
        private PlantGrowthProcessor _growthProcessor;
        private PlantHarvestOperator _harvestOperator;

        public void SetComponents(
            PlantIdentityManager identity,
            PlantStateCoordinator state,
            PlantResourceHandler resources,
            PlantGrowthProcessor growth,
            PlantHarvestOperator harvest)
        {
            _identityManager = identity;
            _stateCoordinator = state;
            _resourceHandler = resources;
            _growthProcessor = growth;
            _harvestOperator = harvest;
        }

        #region Sync FROM Components

        /// <summary>
        /// Sync all data from components to serialized data
        /// </summary>
        public void SyncAllFromComponents(ref SerializedPlantData data, ref PlantDataSyncStats stats)
        {
            SyncIdentityFromComponent(ref data, ref stats);
            SyncStateFromComponent(ref data, ref stats);
            SyncResourcesFromComponent(ref data, ref stats);
            SyncGrowthFromComponent(ref data, ref stats);
            SyncHarvestFromComponent(ref data, ref stats);
        }

        /// <summary>
        /// Sync identity data from component
        /// </summary>
        public void SyncIdentityFromComponent(ref SerializedPlantData data, ref PlantDataSyncStats stats)
        {
            if (_identityManager == null) return;

            var identity = _identityManager.GetIdentitySummary();

            data.PlantID = identity.PlantID;
            data.PlantName = identity.PlantName;
            data.StrainName = identity.StrainName;
            data.GenotypeName = identity.GenotypeName;
            data.CreationDate = identity.CreationDate;
            data.ParentPlantID = identity.ParentPlantID;
            data.GenerationNumber = identity.GenerationNumber;

            stats.IdentitySyncs++;
        }

        /// <summary>
        /// Sync state data from component
        /// </summary>
        public void SyncStateFromComponent(ref SerializedPlantData data, ref PlantDataSyncStats stats)
        {
            if (_stateCoordinator == null) return;

            var state = _stateCoordinator.GetStateSummary();

            data.CurrentGrowthStage = state.CurrentStage;
            data.AgeInDays = state.AgeInDays;
            data.DaysInCurrentStage = state.DaysInCurrentStage;
            data.OverallHealth = state.OverallHealth;
            data.Vigor = state.Vigor;
            data.StressLevel = state.StressLevel;
            data.MaturityLevel = state.MaturityLevel;
            data.CurrentHeight = state.CurrentHeight;
            data.CurrentWidth = state.CurrentWidth;
            data.LeafArea = state.LeafArea;
            data.WorldPosition = Vector3.zero; // Would need to be passed in

            stats.StateSyncs++;
        }

        /// <summary>
        /// Sync resource data from component
        /// </summary>
        public void SyncResourcesFromComponent(ref SerializedPlantData data, ref PlantDataSyncStats stats)
        {
            if (_resourceHandler == null) return;

            var resources = _resourceHandler.GetResourceSummary();

            data.WaterLevel = resources.WaterLevel;
            data.NutrientLevel = resources.NutrientLevel;
            data.EnergyReserves = resources.EnergyReserves;
            data.LastWatering = resources.LastWatering;
            data.LastFeeding = resources.LastFeeding;
            data.LastTraining = resources.LastTraining;

            stats.ResourceSyncs++;
        }

        /// <summary>
        /// Sync growth data from component
        /// </summary>
        public void SyncGrowthFromComponent(ref SerializedPlantData data, ref PlantDataSyncStats stats)
        {
            if (_growthProcessor == null) return;

            var growth = _growthProcessor.GetGrowthSummary();

            data.GrowthProgress = growth.CurrentProgress;
            data.DailyGrowthRate = growth.DailyGrowthRate;
            data.BiomassAccumulation = growth.BiomassAccumulation;
            data.RootDevelopmentRate = growth.RootDevelopmentRate;
            data.CalculatedMaxHeight = growth.CurrentHeight; // Simplified
            data.GeneticVigorModifier = growth.GeneticVigorModifier;

            stats.GrowthSyncs++;
        }

        /// <summary>
        /// Sync harvest data from component
        /// </summary>
        public void SyncHarvestFromComponent(ref SerializedPlantData data, ref PlantDataSyncStats stats)
        {
            if (_harvestOperator == null) return;

            var readiness = _harvestOperator.CheckHarvestReadiness();

            data.HarvestReadiness = readiness.ReadinessScore;
            data.EstimatedYield = readiness.EstimatedYield;
            data.EstimatedPotency = readiness.EstimatedPotency;
            data.OptimalHarvestDate = readiness.OptimalHarvestDate;
            data.IsHarvested = _harvestOperator.IsHarvested;

            stats.HarvestSyncs++;
        }

        #endregion

        #region Sync TO Components

        /// <summary>
        /// Sync all data from serialized data to components
        /// </summary>
        public void SyncAllToComponents(SerializedPlantData data)
        {
            SyncIdentityToComponent(data);
            SyncStateToComponent(data);
            SyncResourcesToComponent(data);
            SyncGrowthToComponent(data);
            SyncHarvestToComponent(data);
        }

        /// <summary>
        /// Sync identity data to component
        /// </summary>
        public void SyncIdentityToComponent(SerializedPlantData data)
        {
            if (_identityManager == null) return;

            _identityManager.SetIdentity(
                data.PlantID,
                data.PlantName,
                null, // PlantStrainSO - would need reference
                null  // GenotypeDataSO - would need reference
            );

            _identityManager.SetParentInfo(data.ParentPlantID, data.GenerationNumber);
        }

        /// <summary>
        /// Sync state data to component
        /// </summary>
        public void SyncStateToComponent(SerializedPlantData data)
        {
            if (_stateCoordinator == null) return;

            _stateCoordinator.SetGrowthStage(data.CurrentGrowthStage);
            _stateCoordinator.UpdateAge(data.AgeInDays);
            _stateCoordinator.UpdateHealth(data.OverallHealth);
            _stateCoordinator.UpdateStressLevel(data.StressLevel);
            _stateCoordinator.UpdatePosition(data.WorldPosition);
            _stateCoordinator.UpdatePhysicalCharacteristics(
                data.CurrentHeight,
                data.CurrentWidth,
                30f, // Default root mass
                data.LeafArea
            );
            _stateCoordinator.UpdateVitality(data.Vigor, 0.8f, data.MaturityLevel);
        }

        /// <summary>
        /// Sync resource data to component
        /// </summary>
        public void SyncResourcesToComponent(SerializedPlantData data)
        {
            if (_resourceHandler == null) return;

            // Set resource levels directly (simplified)
            float waterDelta = data.WaterLevel - _resourceHandler.WaterLevel;
            if (waterDelta > 0)
            {
                _resourceHandler.Water(waterDelta);
            }

            var nutrients = new Dictionary<string, float>
            {
                ["NPK"] = data.NutrientLevel - _resourceHandler.NutrientLevel
            };
            if (nutrients["NPK"] > 0)
            {
                _resourceHandler.Feed(nutrients);
            }

            _resourceHandler.SetEnergyLevel(data.EnergyReserves);
        }

        /// <summary>
        /// Sync growth data to component
        /// </summary>
        public void SyncGrowthToComponent(SerializedPlantData data)
        {
            if (_growthProcessor == null) return;

            _growthProcessor.SetGeneticParameters(
                data.CalculatedMaxHeight,
                data.CurrentWidth * 2f, // Estimate max width
                data.GeneticVigorModifier
            );
        }

        /// <summary>
        /// Sync harvest data to component
        /// </summary>
        public void SyncHarvestToComponent(SerializedPlantData data)
        {
            if (_harvestOperator == null) return;

            // Update harvest operator with current readiness (read-only operation)
            // Most harvest data is calculated, not set
        }

        #endregion

        #region Component Summaries

        /// <summary>
        /// Generate complete plant summary from all components
        /// </summary>
        public CompletePlantSummary GetCompleteSummary(SerializedPlantData data, System.DateTime lastSyncTime, int syncVersion, bool isDirty, bool isDataValid)
        {
            var summary = new CompletePlantSummary
            {
                IsValid = isDataValid,
                SyncInfo = new SyncInfo
                {
                    LastSyncTime = lastSyncTime,
                    SyncVersion = syncVersion,
                    IsDirty = isDirty,
                    IsDataValid = isDataValid
                }
            };

            if (_identityManager != null)
            {
                summary.Identity = _identityManager.GetIdentitySummary();
            }

            if (_stateCoordinator != null)
            {
                summary.State = _stateCoordinator.GetStateSummary();
            }

            if (_resourceHandler != null)
            {
                summary.Resources = _resourceHandler.GetResourceSummary();
            }

            if (_growthProcessor != null)
            {
                summary.Growth = _growthProcessor.GetGrowthSummary();
            }

            if (_harvestOperator != null)
            {
                summary.Harvest = _harvestOperator.GetHarvestRecommendations();
            }

            return summary;
        }

        #endregion
    }
}
