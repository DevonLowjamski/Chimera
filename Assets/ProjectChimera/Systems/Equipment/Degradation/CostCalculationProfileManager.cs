using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Cost Calculation Profile Manager
    /// Single Responsibility: Manage calculation profiles, parameters, and default configurations
    /// Extracted from CostCalculationEngine (687 lines → 4 files <500 lines each)
    /// </summary>
    public class CostCalculationProfileManager
    {
        private Dictionary<MalfunctionType, CostCalculationProfile> _calculationProfiles = new Dictionary<MalfunctionType, CostCalculationProfile>();
        private CostCalculationParameters _parameters;
        private bool _isInitialized = false;
        private readonly bool _enableLogging;

        public bool IsInitialized => _isInitialized;
        public CostCalculationParameters Parameters => _parameters;

        public CostCalculationProfileManager(bool enableLogging = false)
        {
            _enableLogging = enableLogging;
        }

        /// <summary>
        /// Initialize profiles and parameters with defaults
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            LoadDefaultParameters();
            InitializeCalculationProfiles();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Cost Calculation Profile Manager initialized with {_calculationProfiles.Count} profiles");
            }
        }

        /// <summary>
        /// Get calculation profile for malfunction type
        /// </summary>
        public CostCalculationProfile GetCalculationProfile(MalfunctionType type)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Profile manager not initialized");
            }

            // Return profile if exists, otherwise return default WearAndTear profile
            return _calculationProfiles.TryGetValue(type, out var profile)
                ? profile
                : _calculationProfiles[MalfunctionType.WearAndTear];
        }

        /// <summary>
        /// Set custom calculation parameters
        /// </summary>
        public void SetCalculationParameters(CostCalculationParameters parameters)
        {
            _parameters = parameters;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Cost calculation parameters updated");
            }
        }

        /// <summary>
        /// Update specific calculation profile
        /// </summary>
        public void UpdateCalculationProfile(MalfunctionType type, CostCalculationProfile profile)
        {
            _calculationProfiles[type] = profile;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Calculation profile updated for {type}: ${profile.BasePartsCost:F2}, {profile.EstimatedLaborHours:F1}h");
            }
        }

        /// <summary>
        /// Get all registered profiles
        /// </summary>
        public IReadOnlyDictionary<MalfunctionType, CostCalculationProfile> GetAllProfiles()
        {
            return _calculationProfiles;
        }

        /// <summary>
        /// Reset all profiles to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            LoadDefaultParameters();
            InitializeCalculationProfiles();

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Cost calculation profiles and parameters reset to defaults");
            }
        }

        #region Default Configuration

        /// <summary>
        /// Load default calculation parameters
        /// </summary>
        private void LoadDefaultParameters()
        {
            _parameters = new CostCalculationParameters
            {
                StandardHourlyRate = 75f,
                SpecialistHourlyRate = 150f,
                SpecialistTimeMultiplier = 1.2f,
                MinimumLaborCharge = 50f,
                EmergencyMultiplier = 2.0f,
                MinimumEmergencyCharge = 100f,
                TaxRate = 8.5f,
                ServiceFee = 25f,
                FixedOverheadCost = 15f,
                ApplyPartsVariance = true,
                PartsVariancePercent = 0.15f,
                VolumeDiscountPercent = 5f,
                ContractDiscountPercent = 10f,
                WarrantyDiscountPercent = 25f,
                PreventiveMaintenanceDiscountPercent = 15f,
                MaxDiscountPercent = 30f,
                NextDayPartsPremium = 50f,
                WeekLeadPartsPremium = 25f,
                SpecialOrderPartsPremium = 100f
            };

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Default parameters loaded: ${_parameters.StandardHourlyRate}/h labor, {_parameters.TaxRate}% tax");
            }
        }

        /// <summary>
        /// Initialize calculation profiles for all malfunction types
        /// </summary>
        private void InitializeCalculationProfiles()
        {
            _calculationProfiles.Clear();

            // Mechanical Failure - High parts cost, moderate labor
            _calculationProfiles[MalfunctionType.MechanicalFailure] = new CostCalculationProfile
            {
                BasePartsCost = 200f,
                EstimatedLaborHours = 2.5f,
                OverheadPercentage = 25f,
                HistoricalDataPoints = 75
            };

            // Electrical Failure - Moderate parts, requires specialist
            _calculationProfiles[MalfunctionType.ElectricalFailure] = new CostCalculationProfile
            {
                BasePartsCost = 150f,
                EstimatedLaborHours = 1.8f,
                OverheadPercentage = 20f,
                HistoricalDataPoints = 60
            };

            // Sensor Drift - Low parts cost, quick fix
            _calculationProfiles[MalfunctionType.SensorDrift] = new CostCalculationProfile
            {
                BasePartsCost = 75f,
                EstimatedLaborHours = 0.8f,
                OverheadPercentage = 15f,
                HistoricalDataPoints = 90
            };

            // Overheating Problem - Moderate cost, system diagnostic needed
            _calculationProfiles[MalfunctionType.OverheatingProblem] = new CostCalculationProfile
            {
                BasePartsCost = 180f,
                EstimatedLaborHours = 1.5f,
                OverheadPercentage = 22f,
                HistoricalDataPoints = 45
            };

            // Software Error - Minimal parts, labor-intensive
            _calculationProfiles[MalfunctionType.SoftwareError] = new CostCalculationProfile
            {
                BasePartsCost = 25f,
                EstimatedLaborHours = 1.2f,
                OverheadPercentage = 18f,
                HistoricalDataPoints = 30
            };

            // Wear and Tear - Moderate parts, standard labor
            _calculationProfiles[MalfunctionType.WearAndTear] = new CostCalculationProfile
            {
                BasePartsCost = 125f,
                EstimatedLaborHours = 1.8f,
                OverheadPercentage = 20f,
                HistoricalDataPoints = 120
            };

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Initialized {_calculationProfiles.Count} calculation profiles");
            }
        }

        #endregion

        #region Profile Validation

        /// <summary>
        /// Validate calculation profile
        /// </summary>
        public bool ValidateProfile(CostCalculationProfile profile, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (profile.BasePartsCost < 0)
            {
                errorMessage = "Base parts cost cannot be negative";
                return false;
            }

            if (profile.EstimatedLaborHours <= 0)
            {
                errorMessage = "Estimated labor hours must be positive";
                return false;
            }

            if (profile.OverheadPercentage < 0 || profile.OverheadPercentage > 100)
            {
                errorMessage = "Overhead percentage must be between 0 and 100";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate calculation parameters
        /// </summary>
        public bool ValidateParameters(CostCalculationParameters parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.StandardHourlyRate <= 0 || parameters.SpecialistHourlyRate <= 0)
            {
                errorMessage = "Hourly rates must be positive";
                return false;
            }

            if (parameters.EmergencyMultiplier < 1.0f)
            {
                errorMessage = "Emergency multiplier must be at least 1.0";
                return false;
            }

            if (parameters.TaxRate < 0 || parameters.TaxRate > 100)
            {
                errorMessage = "Tax rate must be between 0 and 100";
                return false;
            }

            if (parameters.MaxDiscountPercent < 0 || parameters.MaxDiscountPercent > 100)
            {
                errorMessage = "Maximum discount must be between 0 and 100";
                return false;
            }

            return true;
        }

        #endregion
    }
}

