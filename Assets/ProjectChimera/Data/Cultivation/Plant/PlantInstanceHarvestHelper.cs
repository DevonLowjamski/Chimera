// REFACTORED: Plant Instance Harvest Helper
// Extracted harvest operations from PlantInstanceSO for better SRP

using UnityEngine;
using System;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Helper class for plant harvest operations
    /// Extracted from PlantInstanceSO to maintain <500 line limit
    /// </summary>
    public class PlantInstanceHarvestHelper
    {
        private readonly PlantHarvestData _harvestData;
        private readonly PlantGrowthData _growthData;
        private readonly PlantStateData _stateData;

        public PlantInstanceHarvestHelper(PlantHarvestData harvestData, PlantGrowthData growthData, PlantStateData stateData)
        {
            _harvestData = harvestData;
            _growthData = growthData;
            _stateData = stateData;
        }

        public YieldCalculation CalculateYieldPotential()
        {
            return new YieldCalculation
            {
                EstimatedYield = _harvestData?.ExpectedYield ?? 0f,
                YieldConfidence = _growthData?.GrowthStage == PlantGrowthStage.Flowering ? 0.85f : 0.5f,
                CalculationDate = DateTime.Now
            };
        }

        public HarvestReadiness CheckHarvestReadiness()
        {
            if (_harvestData == null) return HarvestReadiness.NotReady;

            return _harvestData.IsHarvestReady ? HarvestReadiness.Ready : HarvestReadiness.NotReady;
        }

        public HarvestRecommendation GetHarvestRecommendations()
        {
            return new HarvestRecommendation
            {
                IsReadyToHarvest = _harvestData?.IsHarvestReady ?? false,
                OptimalHarvestDate = _harvestData?.OptimalHarvestDate ?? DateTime.Now,
                TrichomeMaturity = _harvestData?.TrichomeMaturity ?? 0f,
                RecommendationReason = _harvestData?.IsHarvestReady ?? false 
                    ? "Trichome maturity optimal" 
                    : "Continue monitoring trichome development"
            };
        }

        public PostHarvestProcess GetPostHarvestProcess(HarvestResult harvestResult)
        {
            return new PostHarvestProcess
            {
                ProcessType = "Standard Drying",
                DryingDuration = 7f,
                DryingTemperature = 20f,
                DryingHumidity = 55f,
                CuringDuration = 14f,
                CuringTemperature = 18f,
                CuringHumidity = 62f
            };
        }

        public HarvestResult PerformHarvest()
        {
            if (_harvestData == null)
            {
                return new HarvestResult
                {
                    Success = false,
                    Message = "Harvest data not initialized"
                };
            }

            var harvestResult = new HarvestResult
            {
                ActualYield = _harvestData.ExpectedYield,
                THCContent = _harvestData.THCContent,
                CBDContent = _harvestData.CBDContent,
                TerpeneContent = _harvestData.TerpeneProfile,
                HarvestDate = DateTime.Now,
                Success = true,
                Message = "Harvest successful"
            };

            // Update harvest data
            _harvestData.LastHarvestDate = DateTime.Now;
            _harvestData.TotalHarvestCount++;

            return harvestResult;
        }

        public float CalculatePotencyPotential()
        {
            if (_harvestData == null) return 0f;

            return (_harvestData.THCContent + _harvestData.CBDContent) / 2f;
        }
    }
}

