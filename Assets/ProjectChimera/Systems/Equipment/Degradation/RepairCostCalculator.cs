using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Repair Cost Calculator
    /// Single Responsibility: Calculate repair costs and component costs
    /// Extracted from MalfunctionCostEstimator (782 lines → 4 files <500 lines each)
    /// </summary>
    public class RepairCostCalculator
    {
        private readonly CostEstimationParameters _parameters;
        private readonly Dictionary<EquipmentType, float> _equipmentCostModifiers;

        public RepairCostCalculator(CostEstimationParameters parameters)
        {
            _parameters = parameters;
            _equipmentCostModifiers = InitializeEquipmentModifiers();
        }

        /// <summary>
        /// Calculate total repair cost
        /// </summary>
        public float CalculateRepairCost(
            BaseCostData costData,
            MalfunctionSeverity severity,
            EquipmentType equipmentType,
            bool requiresSpecialist,
            bool isEmergency)
        {
            // Calculate base cost
            float baseCost = CalculateBaseCost(costData, severity);

            // Apply equipment modifier
            float equipmentModifier = GetEquipmentCostModifier(equipmentType);
            float adjustedCost = baseCost * equipmentModifier;

            // Apply specialist cost
            if (requiresSpecialist)
            {
                adjustedCost *= 1.3f; // 30% increase for specialist
            }

            // Apply emergency multiplier
            if (isEmergency)
            {
                adjustedCost *= _parameters.EmergencyMultiplier;
            }

            // Apply market pricing and inflation
            if (_parameters.UseMarketPricing)
            {
                adjustedCost = ApplyMarketPricing(adjustedCost, MalfunctionType.WearAndTear);
            }

            if (_parameters.IncludeInflation)
            {
                adjustedCost = ApplyInflation(adjustedCost);
            }

            // Add variance
            float variance = UnityEngine.Random.Range(-_parameters.PartsCostVariance, _parameters.PartsCostVariance);
            float finalCost = adjustedCost * (1f + variance);

            return Mathf.Max(0f, finalCost);
        }

        /// <summary>
        /// Calculate cost breakdown
        /// </summary>
        public CostBreakdown CalculateCostBreakdown(
            MalfunctionType type,
            MalfunctionSeverity severity,
            BaseCostData costData,
            List<string> requiredParts,
            bool requiresSpecialist,
            bool isEmergency)
        {
            float laborCost = CalculateLaborCost(type, severity, requiresSpecialist, costData);
            float partsCost = CalculatePartsCost(requiredParts, type);
            float overheadCost = CalculateOverheadCost(type, severity, costData);
            float emergencySurcharge = isEmergency ? CalculateEmergencySurcharge(type, severity, costData) : 0f;

            return CostBreakdown.Calculate(laborCost, partsCost, overheadCost, emergencySurcharge);
        }

        /// <summary>
        /// Calculate base cost from severity
        /// </summary>
        public float CalculateBaseCost(BaseCostData costData, MalfunctionSeverity severity)
        {
            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => UnityEngine.Random.Range(0.8f, 1.2f),
                MalfunctionSeverity.Moderate => UnityEngine.Random.Range(1.5f, 2.5f),
                MalfunctionSeverity.Major => UnityEngine.Random.Range(3f, 5f),
                MalfunctionSeverity.Critical => UnityEngine.Random.Range(6f, 10f),
                MalfunctionSeverity.Catastrophic => UnityEngine.Random.Range(12f, 18f),
                _ => 1f
            };

            return costData.BaseCost * severityMultiplier;
        }

        /// <summary>
        /// Calculate estimate confidence
        /// </summary>
        public float CalculateEstimateConfidence(
            MalfunctionType type,
            EquipmentType equipmentType,
            BaseCostData costData)
        {
            float baseConfidence = 0.8f;

            // Reduce confidence for complex types
            if (type == MalfunctionType.SoftwareError) baseConfidence -= 0.1f;
            if (equipmentType == EquipmentType.Security) baseConfidence -= 0.05f;

            // Increase confidence if we have historical data
            if (costData.ActualCostHistory.Count > 10)
            {
                baseConfidence += 0.1f;
            }

            return Mathf.Clamp01(baseConfidence);
        }

        /// <summary>
        /// Estimate required parts
        /// </summary>
        public List<string> EstimateRequiredParts(MalfunctionType type, MalfunctionSeverity severity)
        {
            var parts = new List<string>();

            switch (type)
            {
                case MalfunctionType.ElectricalFailure:
                    parts.Add("Fuse");
                    parts.Add("Relay");
                    if (severity >= MalfunctionSeverity.Major)
                    {
                        parts.Add("Wiring");
                    }
                    break;

                case MalfunctionType.MechanicalFailure:
                    parts.Add("Bearing");
                    parts.Add("Seal");
                    if (severity >= MalfunctionSeverity.Major)
                    {
                        parts.Add("Motor");
                    }
                    break;

                case MalfunctionType.SensorDrift:
                    parts.Add("Sensor");
                    if (severity >= MalfunctionSeverity.Critical)
                    {
                        parts.Add("Calibration Unit");
                    }
                    break;

                case MalfunctionType.OverheatingProblem:
                    parts.Add("Fan");
                    parts.Add("Thermal Paste");
                    break;

                case MalfunctionType.SoftwareError:
                    // Software issues typically don't require parts
                    break;

                default:
                    parts.Add("Generic Component");
                    break;
            }

            return parts;
        }

        /// <summary>
        /// Estimate additional materials
        /// </summary>
        public List<string> EstimateAdditionalMaterials(MalfunctionType type, MalfunctionSeverity severity)
        {
            var materials = new List<string>();

            if (severity >= MalfunctionSeverity.Major)
            {
                materials.Add("Industrial Cleaning Supplies");
                materials.Add("Safety Equipment");
            }

            switch (type)
            {
                case MalfunctionType.ElectricalFailure:
                    materials.Add("Electrical Tape");
                    materials.Add("Wire Connectors");
                    break;

                case MalfunctionType.MechanicalFailure:
                    materials.Add("Lubricant");
                    materials.Add("Fasteners");
                    break;

                case MalfunctionType.OverheatingProblem:
                    materials.Add("Thermal Compound");
                    break;
            }

            return materials;
        }

        #region Private Calculation Methods

        /// <summary>
        /// Get equipment cost modifier
        /// </summary>
        private float GetEquipmentCostModifier(EquipmentType equipmentType)
        {
            return _equipmentCostModifiers.TryGetValue(equipmentType, out var modifier) ? modifier : 1.0f;
        }

        /// <summary>
        /// Apply market pricing adjustments
        /// </summary>
        private float ApplyMarketPricing(float baseCost, MalfunctionType type)
        {
            // Simulate market demand fluctuations
            float marketMultiplier = type switch
            {
                MalfunctionType.ElectricalFailure => UnityEngine.Random.Range(0.95f, 1.15f), // High demand variability
                MalfunctionType.SoftwareError => UnityEngine.Random.Range(1.05f, 1.25f), // Premium pricing
                _ => UnityEngine.Random.Range(0.98f, 1.08f) // Standard variability
            };

            return baseCost * marketMultiplier;
        }

        /// <summary>
        /// Apply inflation to cost
        /// </summary>
        private float ApplyInflation(float baseCost)
        {
            float timeSinceStart = Time.time; // Simplified - in real system would use actual dates
            float inflationMultiplier = Mathf.Pow(1f + _parameters.InflationRate, timeSinceStart / 31536000f); // Annual rate
            return baseCost * inflationMultiplier;
        }

        /// <summary>
        /// Calculate labor cost component
        /// </summary>
        private float CalculateLaborCost(
            MalfunctionType type,
            MalfunctionSeverity severity,
            bool requiresSpecialist,
            BaseCostData costData)
        {
            // Estimate repair time in hours
            float hours = costData.BaseTimeMinutes / 60f;

            // Apply severity multiplier
            hours *= GetSeverityMultiplier(severity);

            // Apply hourly rate
            float hourlyRate = requiresSpecialist
                ? _parameters.SpecialistCostPerHour
                : _parameters.LaborCostPerHour;

            return hours * hourlyRate;
        }

        /// <summary>
        /// Calculate parts cost component
        /// </summary>
        private float CalculatePartsCost(List<string> requiredParts, MalfunctionType type)
        {
            if (requiredParts == null || requiredParts.Count == 0)
                return 0f;

            float totalPartsCost = 0f;
            foreach (var part in requiredParts)
            {
                totalPartsCost += EstimatePartCost(part, type);
            }

            return totalPartsCost;
        }

        /// <summary>
        /// Estimate individual part cost
        /// </summary>
        private float EstimatePartCost(string partName, MalfunctionType type)
        {
            // Simplified part cost estimation
            float baseCost = partName.ToLowerInvariant() switch
            {
                var p when p.Contains("sensor") => UnityEngine.Random.Range(50f, 200f),
                var p when p.Contains("bearing") => UnityEngine.Random.Range(20f, 100f),
                var p when p.Contains("seal") => UnityEngine.Random.Range(10f, 50f),
                var p when p.Contains("fan") => UnityEngine.Random.Range(30f, 150f),
                var p when p.Contains("fuse") => UnityEngine.Random.Range(5f, 25f),
                var p when p.Contains("relay") => UnityEngine.Random.Range(15f, 75f),
                var p when p.Contains("wiring") => UnityEngine.Random.Range(25f, 100f),
                var p when p.Contains("motor") => UnityEngine.Random.Range(100f, 500f),
                _ => UnityEngine.Random.Range(20f, 80f) // Default range
            };

            return baseCost;
        }

        /// <summary>
        /// Calculate overhead cost
        /// </summary>
        private float CalculateOverheadCost(MalfunctionType type, MalfunctionSeverity severity, BaseCostData costData)
        {
            // Overhead typically 15-25% of labor
            float laborCost = CalculateLaborCost(type, severity, false, costData);
            return 0.2f * laborCost;
        }

        /// <summary>
        /// Calculate emergency surcharge
        /// </summary>
        private float CalculateEmergencySurcharge(MalfunctionType type, MalfunctionSeverity severity, BaseCostData costData)
        {
            float baseCost = CalculateBaseCost(costData, severity);
            return baseCost * (_parameters.EmergencyMultiplier - 1f);
        }

        /// <summary>
        /// Get severity multiplier
        /// </summary>
        private float GetSeverityMultiplier(MalfunctionSeverity severity)
        {
            return severity switch
            {
                MalfunctionSeverity.Minor => 1f,
                MalfunctionSeverity.Moderate => 2f,
                MalfunctionSeverity.Major => 4f,
                MalfunctionSeverity.Critical => 8f,
                MalfunctionSeverity.Catastrophic => 12f,
                _ => 1f
            };
        }

        /// <summary>
        /// Initialize equipment cost modifiers
        /// </summary>
        private Dictionary<EquipmentType, float> InitializeEquipmentModifiers()
        {
            return new Dictionary<EquipmentType, float>
            {
                [EquipmentType.Generic] = 1.0f,
                [EquipmentType.HVAC] = 1.2f,
                [EquipmentType.Lighting] = 0.8f,
                [EquipmentType.Irrigation] = 1.1f,
                [EquipmentType.Climate] = 1.3f,
                [EquipmentType.Security] = 1.5f,
                [EquipmentType.Power] = 1.4f
            };
        }

        #endregion
    }
}

