using System;
using System.Collections.Generic;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Cost Estimation Data Structures
    /// Single Responsibility: Define all cost estimation data types
    /// Extracted from MalfunctionCostEstimator (782 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Base cost data for malfunction types
    /// </summary>
    [Serializable]
    public class BaseCostData
    {
        public float BaseCost;
        public int BaseTimeMinutes;
        public float PartsCostRatio;
        public float LaborCostRatio;
        public float AverageActualCost;
        public float AverageActualTime;
        public List<float> ActualCostHistory = new List<float>();
        public List<float> ActualTimeHistory = new List<float>();

        /// <summary>
        /// Create default cost data
        /// </summary>
        public static BaseCostData CreateDefault(float baseCost, int baseTime, float partsRatio, float laborRatio)
        {
            return new BaseCostData
            {
                BaseCost = baseCost,
                BaseTimeMinutes = baseTime,
                PartsCostRatio = partsRatio,
                LaborCostRatio = laborRatio,
                AverageActualCost = baseCost,
                AverageActualTime = baseTime,
                ActualCostHistory = new List<float>(),
                ActualTimeHistory = new List<float>()
            };
        }

        /// <summary>
        /// Update historical data
        /// </summary>
        public void UpdateHistory(float actualCost, float actualTime, int maxHistorySize)
        {
            ActualCostHistory.Add(actualCost);
            ActualTimeHistory.Add(actualTime);

            // Maintain history size limit
            if (ActualCostHistory.Count > maxHistorySize)
            {
                ActualCostHistory.RemoveAt(0);
            }
            if (ActualTimeHistory.Count > maxHistorySize)
            {
                ActualTimeHistory.RemoveAt(0);
            }

            // Recalculate averages
            float costSum = 0f;
            float timeSum = 0f;
            foreach (var cost in ActualCostHistory)
            {
                costSum += cost;
            }
            foreach (var time in ActualTimeHistory)
            {
                timeSum += time;
            }
            AverageActualCost = ActualCostHistory.Count > 0 ? costSum / ActualCostHistory.Count : BaseCost;
            AverageActualTime = ActualTimeHistory.Count > 0 ? timeSum / ActualTimeHistory.Count : BaseTimeMinutes;
        }
    }

    /// <summary>
    /// Comprehensive cost estimate
    /// </summary>
    [Serializable]
    public class CostEstimate
    {
        public string EstimateId;
        public string MalfunctionId;
        public string EquipmentId;
        public DateTime EstimationTime;

        // Cost breakdown
        public float LaborCost;
        public float PartsCost;
        public float OverheadCost;
        public float EmergencySurcharge;
        public float TotalCost;

        // Time estimates
        public TimeSpan EstimatedRepairTime;
        public TimeSpan DiagnosticTime;

        // Additional details
        public bool RequiresSpecialist;
        public bool IsEmergency;
        public float Confidence;

        // Parts and materials
        public List<string> RequiredParts = new List<string>();
        public List<string> AdditionalMaterials = new List<string>();

        /// <summary>
        /// Create basic cost estimate
        /// </summary>
        public static CostEstimate CreateBasic(
            string malfunctionId,
            string equipmentId,
            float totalCost,
            TimeSpan repairTime,
            bool requiresSpecialist,
            bool isEmergency)
        {
            return new CostEstimate
            {
                EstimateId = GenerateEstimateId(),
                MalfunctionId = malfunctionId,
                EquipmentId = equipmentId,
                EstimationTime = DateTime.Now,
                TotalCost = totalCost,
                EstimatedRepairTime = repairTime,
                RequiresSpecialist = requiresSpecialist,
                IsEmergency = isEmergency,
                Confidence = 0.8f,
                RequiredParts = new List<string>(),
                AdditionalMaterials = new List<string>()
            };
        }

        /// <summary>
        /// Generate unique estimate ID
        /// </summary>
        private static string GenerateEstimateId()
        {
            return $"EST_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// Get cache key for estimate
        /// </summary>
        public string GetCacheKey()
        {
            return $"{MalfunctionId}_{EquipmentId}_{RequiresSpecialist}_{IsEmergency}";
        }
    }

    /// <summary>
    /// Cost breakdown details
    /// </summary>
    [Serializable]
    public struct CostBreakdown
    {
        public string CalculationId;
        public CostCalculationRequest Request;
        public DateTime CalculationTime;
        public float CalculationDuration;

        public float LaborCost;
        public float PartsCost;
        public float OverheadCost;
        public float EmergencySurcharge;
        public float TaxesFees;
        public float DiscountAdjustment;

        public float Subtotal;
        public float TotalCost;

        public float ConfidenceScore;

        public static CostBreakdown Calculate(float labor, float parts, float overhead, float emergency)
        {
            return new CostBreakdown
            {
                LaborCost = labor,
                PartsCost = parts,
                OverheadCost = overhead,
                EmergencySurcharge = emergency,
                TotalCost = labor + parts + overhead + emergency
            };
        }
    }

    /// <summary>
    /// Cost estimator statistics
    /// </summary>
    [Serializable]
    public struct MalfunctionCostEstimatorStats
    {
        public int CostEstimatesGenerated;
        public int TimeEstimatesGenerated;
        public int EstimatesWithoutBaseData;
        public int EstimationErrors;

        public float TotalEstimationTime;
        public float AverageEstimationTime;
        public float MaxEstimationTime;

        public float TotalEstimatedCost;
        public float AverageEstimatedCost;

        /// <summary>
        /// Create empty statistics
        /// </summary>
        public static MalfunctionCostEstimatorStats CreateEmpty()
        {
            return new MalfunctionCostEstimatorStats
            {
                CostEstimatesGenerated = 0,
                TimeEstimatesGenerated = 0,
                EstimatesWithoutBaseData = 0,
                EstimationErrors = 0,
                TotalEstimationTime = 0f,
                AverageEstimationTime = 0f,
                MaxEstimationTime = 0f,
                TotalEstimatedCost = 0f,
                AverageEstimatedCost = 0f
            };
        }

        /// <summary>
        /// Update statistics
        /// </summary>
        public void UpdateStats(float estimationTime, float estimatedCost)
        {
            TotalEstimationTime += estimationTime;
            AverageEstimationTime = CostEstimatesGenerated > 0
                ? TotalEstimationTime / CostEstimatesGenerated
                : 0f;

            if (estimationTime > MaxEstimationTime)
                MaxEstimationTime = estimationTime;

            TotalEstimatedCost += estimatedCost;
            AverageEstimatedCost = CostEstimatesGenerated > 0
                ? TotalEstimatedCost / CostEstimatesGenerated
                : 0f;
        }
    }

    /// <summary>
    /// Cost estimation parameters
    /// </summary>
    [Serializable]
    public struct CostEstimationParameters
    {
        public float LaborCostPerHour;
        public float SpecialistCostPerHour;
        public float EmergencyMultiplier;
        public float PartsCostVariance;
        public float InflationRate;
        public bool UseMarketPricing;
        public bool IncludeInflation;
        public bool UseRealisticTimeEstimates;

        /// <summary>
        /// Create default parameters
        /// </summary>
        public static CostEstimationParameters CreateDefault()
        {
            return new CostEstimationParameters
            {
                LaborCostPerHour = 75f,
                SpecialistCostPerHour = 150f,
                EmergencyMultiplier = 2f,
                PartsCostVariance = 0.15f,
                InflationRate = 0.03f,
                UseMarketPricing = true,
                IncludeInflation = true,
                UseRealisticTimeEstimates = true
            };
        }
    }

    /// <summary>
    /// Cost calculation statistics
    /// </summary>
    [Serializable]
    public struct CostCalculationStats
    {
        public int CalculationsPerformed;
        public int SuccessfulCalculations;
        public int FailedCalculations;
        public float TotalCalculationTime;

        public readonly float AverageCalculationTime => SuccessfulCalculations > 0 ? TotalCalculationTime / SuccessfulCalculations : 0f;
        public readonly float SuccessRate => CalculationsPerformed > 0 ? (float)SuccessfulCalculations / CalculationsPerformed : 0f;
    }

    /// <summary>
    /// Detailed cost calculation parameters for component-based calculations
    /// </summary>
    [Serializable]
    public struct CostCalculationParameters
    {
        // Labor Rates
        public float StandardHourlyRate;
        public float SpecialistHourlyRate;
        public float SpecialistTimeMultiplier;
        public float MinimumLaborCharge;

        // Emergency Pricing
        public float EmergencyMultiplier;
        public float MinimumEmergencyCharge;

        // Taxes and Fees
        public float TaxRate;
        public float ServiceFee;
        public float FixedOverheadCost;

        // Parts Pricing
        public bool ApplyPartsVariance;
        public float PartsVariancePercent;
        public float NextDayPartsPremium;
        public float WeekLeadPartsPremium;
        public float SpecialOrderPartsPremium;

        // Discounts
        public float VolumeDiscountPercent;
        public float ContractDiscountPercent;
        public float WarrantyDiscountPercent;
        public float PreventiveMaintenanceDiscountPercent;
        public float MaxDiscountPercent;
    }

    /// <summary>
    /// Cost calculation profile per malfunction type
    /// </summary>
    [Serializable]
    public struct CostCalculationProfile
    {
        public float BasePartsCost;
        public float EstimatedLaborHours;
        public float OverheadPercentage;
        public int HistoricalDataPoints;
    }

    /// <summary>
    /// Cost calculation request structure
    /// </summary>
    [Serializable]
    public struct CostCalculationRequest
    {
        public MalfunctionType MalfunctionType;
        public MalfunctionSeverity Severity;
        public EquipmentType EquipmentType;
        public bool RequiresSpecialist;
        public bool IsEmergency;
        public bool IsVolumeRepair;
        public bool IsContractCustomer;
        public bool IsUnderWarranty;
        public bool IsPreventiveMaintenance;
        public PartsAvailability PartsAvailability;
    }

    /// <summary>
    /// Parts availability levels
    /// </summary>
    public enum PartsAvailability
    {
        Immediate = 0,
        NextDay = 1,
        WeekLead = 2,
        SpecialOrder = 3
    }

    /// <summary>
    /// Cost calculation exception for calculation-specific errors
    /// </summary>
    public class CostCalculationException : Exception
    {
        public CostCalculationException(string message) : base(message) { }
        public CostCalculationException(string message, Exception innerException) : base(message, innerException) { }
    }
}

