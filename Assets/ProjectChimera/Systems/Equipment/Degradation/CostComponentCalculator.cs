using UnityEngine;
using System;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Cost Component Calculator
    /// Single Responsibility: Calculate individual cost components (labor, parts, overhead, etc.) and modifiers
    /// Extracted from CostCalculationEngine (687 lines → 4 files <500 lines each)
    /// </summary>
    public class CostComponentCalculator
    {
        private readonly CostCalculationProfileManager _profileManager;
        private readonly CostCalculationParameters _parameters;
        private readonly bool _enableLogging;

        public CostComponentCalculator(CostCalculationProfileManager profileManager, CostCalculationParameters parameters, bool enableLogging = false)
        {
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _parameters = parameters;
            _enableLogging = enableLogging;
        }

        #region Component Calculations

        /// <summary>
        /// Calculate labor cost component
        /// </summary>
        public float CalculateLaborCost(CostCalculationRequest request)
        {
            var profile = _profileManager.GetCalculationProfile(request.MalfunctionType);
            var baseHourlyRate = request.RequiresSpecialist ? _parameters.SpecialistHourlyRate : _parameters.StandardHourlyRate;

            // Apply complexity multiplier
            var complexityMultiplier = GetComplexityMultiplier(request.MalfunctionType, request.Severity);
            var adjustedHourlyRate = baseHourlyRate * complexityMultiplier;

            // Apply equipment type modifier
            var equipmentModifier = GetEquipmentLaborModifier(request.EquipmentType);
            adjustedHourlyRate *= equipmentModifier;

            // Calculate total labor hours
            var laborHours = profile.EstimatedLaborHours;
            if (request.RequiresSpecialist)
            {
                laborHours *= _parameters.SpecialistTimeMultiplier;
            }

            // Apply severity multiplier
            laborHours *= GetSeverityTimeMultiplier(request.Severity);

            var laborCost = adjustedHourlyRate * laborHours;

            // Apply minimum labor charge
            laborCost = Mathf.Max(laborCost, _parameters.MinimumLaborCharge);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Labor cost calculated: ${laborCost:F2} ({laborHours:F1}h @ ${adjustedHourlyRate:F2}/h)");
            }

            return laborCost;
        }

        /// <summary>
        /// Calculate parts cost component
        /// </summary>
        public float CalculatePartsCost(CostCalculationRequest request)
        {
            var profile = _profileManager.GetCalculationProfile(request.MalfunctionType);
            var baseCost = profile.BasePartsCost;

            // Apply severity multiplier for parts
            var severityMultiplier = GetSeverityPartsMultiplier(request.Severity);
            var adjustedCost = baseCost * severityMultiplier;

            // Apply equipment type modifier
            var equipmentModifier = GetEquipmentPartsModifier(request.EquipmentType);
            adjustedCost *= equipmentModifier;

            // Add variance for market fluctuations
            if (_parameters.ApplyPartsVariance)
            {
                var varianceMultiplier = 1f + UnityEngine.Random.Range(-_parameters.PartsVariancePercent, _parameters.PartsVariancePercent);
                adjustedCost *= varianceMultiplier;
            }

            // Calculate parts availability premium
            var availabilityPremium = CalculatePartsAvailabilityPremium(request);
            adjustedCost += availabilityPremium;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Parts cost calculated: ${adjustedCost:F2} (base: ${baseCost:F2}, premium: ${availabilityPremium:F2})");
            }

            return adjustedCost;
        }

        /// <summary>
        /// Calculate overhead cost component
        /// </summary>
        public float CalculateOverheadCost(CostCalculationRequest request)
        {
            var profile = _profileManager.GetCalculationProfile(request.MalfunctionType);
            var laborCost = CalculateLaborCost(request);
            var partsCost = CalculatePartsCost(request);

            // Calculate overhead as percentage of labor + parts
            var overheadRate = profile.OverheadPercentage / 100f;
            var overhead = (laborCost + partsCost) * overheadRate;

            // Add fixed overhead components
            overhead += _parameters.FixedOverheadCost;

            // Apply facility overhead multiplier
            overhead *= GetFacilityOverheadMultiplier(request.EquipmentType);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Overhead cost calculated: ${overhead:F2} ({profile.OverheadPercentage}% + fixed ${_parameters.FixedOverheadCost:F2})");
            }

            return overhead;
        }

        /// <summary>
        /// Calculate emergency surcharge
        /// </summary>
        public float CalculateEmergencySurcharge(CostCalculationRequest request)
        {
            if (!request.IsEmergency)
                return 0f;

            var laborCost = CalculateLaborCost(request);
            var partsCost = CalculatePartsCost(request);
            var baseRepairCost = laborCost + partsCost;

            // Apply emergency multiplier
            var emergencyRate = (_parameters.EmergencyMultiplier - 1f); // Subtract 1 since it's a surcharge
            var surcharge = baseRepairCost * emergencyRate;

            // Apply minimum emergency charge
            surcharge = Mathf.Max(surcharge, _parameters.MinimumEmergencyCharge);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Emergency surcharge calculated: ${surcharge:F2} (rate: {_parameters.EmergencyMultiplier}x)");
            }

            return surcharge;
        }

        /// <summary>
        /// Calculate taxes and fees
        /// </summary>
        public float CalculateTaxesAndFees(CostCalculationRequest request)
        {
            var laborCost = CalculateLaborCost(request);
            var partsCost = CalculatePartsCost(request);
            var overheadCost = CalculateOverheadCost(request);
            var subtotal = laborCost + partsCost + overheadCost;

            var taxes = subtotal * (_parameters.TaxRate / 100f);
            var fees = _parameters.ServiceFee;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Taxes and fees calculated: ${taxes + fees:F2} (tax: ${taxes:F2}, fee: ${fees:F2})");
            }

            return taxes + fees;
        }

        /// <summary>
        /// Calculate applicable discounts
        /// </summary>
        public float CalculateDiscounts(CostCalculationRequest request)
        {
            var subtotal = CalculateLaborCost(request) + CalculatePartsCost(request) + CalculateOverheadCost(request);
            var totalDiscount = 0f;

            // Volume discount
            if (request.IsVolumeRepair)
            {
                totalDiscount += subtotal * (_parameters.VolumeDiscountPercent / 100f);
            }

            // Contract customer discount
            if (request.IsContractCustomer)
            {
                totalDiscount += subtotal * (_parameters.ContractDiscountPercent / 100f);
            }

            // Warranty discount
            if (request.IsUnderWarranty)
            {
                totalDiscount += subtotal * (_parameters.WarrantyDiscountPercent / 100f);
            }

            // Preventive maintenance discount
            if (request.IsPreventiveMaintenance)
            {
                totalDiscount += subtotal * (_parameters.PreventiveMaintenanceDiscountPercent / 100f);
            }

            // Cap discount at maximum allowed
            totalDiscount = Mathf.Min(totalDiscount, subtotal * (_parameters.MaxDiscountPercent / 100f));

            if (_enableLogging && totalDiscount > 0)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Discounts calculated: ${totalDiscount:F2} ({(totalDiscount / subtotal * 100f):F1}% of subtotal)");
            }

            return totalDiscount;
        }

        #endregion

        #region Modifiers and Multipliers

        /// <summary>
        /// Get complexity multiplier for labor cost
        /// </summary>
        public float GetComplexityMultiplier(MalfunctionType type, MalfunctionSeverity severity)
        {
            var baseMultiplier = type switch
            {
                MalfunctionType.MechanicalFailure => 1.2f,
                MalfunctionType.ElectricalFailure => 1.1f,
                MalfunctionType.SensorDrift => 1.0f,
                MalfunctionType.OverheatingProblem => 1.15f,
                MalfunctionType.SoftwareError => 0.9f,
                MalfunctionType.WearAndTear => 1.0f,
                _ => 1.0f
            };

            var severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => 1.0f,
                MalfunctionSeverity.Moderate => 1.2f,
                MalfunctionSeverity.Major => 1.5f,
                MalfunctionSeverity.Critical => 2.0f,
                _ => 1.0f
            };

            return baseMultiplier * severityMultiplier;
        }

        /// <summary>
        /// Get severity time multiplier
        /// </summary>
        public float GetSeverityTimeMultiplier(MalfunctionSeverity severity)
        {
            return severity switch
            {
                MalfunctionSeverity.Minor => 1.0f,
                MalfunctionSeverity.Moderate => 1.3f,
                MalfunctionSeverity.Major => 1.8f,
                MalfunctionSeverity.Critical => 2.5f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get severity parts multiplier
        /// </summary>
        public float GetSeverityPartsMultiplier(MalfunctionSeverity severity)
        {
            return severity switch
            {
                MalfunctionSeverity.Minor => 1.0f,
                MalfunctionSeverity.Moderate => 1.25f,
                MalfunctionSeverity.Major => 1.6f,
                MalfunctionSeverity.Critical => 2.2f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get equipment labor modifier
        /// </summary>
        public float GetEquipmentLaborModifier(EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                EquipmentType.HVAC => 1.1f,
                EquipmentType.Lighting => 0.9f,
                EquipmentType.Irrigation => 1.0f,
                EquipmentType.Monitoring => 1.2f,
                EquipmentType.Generic => 1.0f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get equipment parts modifier
        /// </summary>
        public float GetEquipmentPartsModifier(EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                EquipmentType.HVAC => 1.3f,
                EquipmentType.Lighting => 0.8f,
                EquipmentType.Irrigation => 1.0f,
                EquipmentType.Monitoring => 1.4f,
                EquipmentType.Generic => 1.0f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get facility overhead multiplier
        /// </summary>
        public float GetFacilityOverheadMultiplier(EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                EquipmentType.HVAC => 1.15f,
                EquipmentType.Lighting => 1.05f,
                EquipmentType.Irrigation => 1.0f,
                EquipmentType.Monitoring => 1.1f,
                EquipmentType.Generic => 1.0f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Calculate parts availability premium
        /// </summary>
        private float CalculatePartsAvailabilityPremium(CostCalculationRequest request)
        {
            var premium = request.PartsAvailability switch
            {
                PartsAvailability.Immediate => 0f,
                PartsAvailability.NextDay => _parameters.NextDayPartsPremium,
                PartsAvailability.WeekLead => _parameters.WeekLeadPartsPremium,
                PartsAvailability.SpecialOrder => _parameters.SpecialOrderPartsPremium,
                _ => 0f
            };

            return premium;
        }

        #endregion
    }
}

