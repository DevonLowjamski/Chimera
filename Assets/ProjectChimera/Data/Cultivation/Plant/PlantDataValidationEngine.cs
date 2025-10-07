using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Data Validation Engine
    /// Single Responsibility: Data integrity checking, validation logic, and consistency enforcement
    /// Extracted from PlantDataSynchronizer for better separation of concerns
    /// </summary>
    [System.Serializable]
    public class PlantDataValidationEngine
    {
        [Header("Validation Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _strictValidation = true;
        [SerializeField] private bool _autoCorrectValues = false;
        [SerializeField] private float _healthTolerance = 0.1f;

        // Validation rules
        [SerializeField] private ValidationRules _rules = new ValidationRules();

        // Validation state
        private List<ValidationError> _validationErrors = new List<ValidationError>();
        private Dictionary<string, float> _lastValidValues = new Dictionary<string, float>();
        private bool _isDataValid = true;

        // Statistics
        private ValidationStats _stats = new ValidationStats();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event System.Action<List<ValidationError>> OnValidationFailed;
        public event System.Action OnValidationPassed;
        public event System.Action<ValidationError> OnValueCorrected;
        public event System.Action<ValidationReport> OnValidationComplete;

        public bool IsInitialized => _isInitialized;
        public ValidationStats Stats => _stats;
        public bool IsDataValid => _isDataValid;
        public List<ValidationError> LastValidationErrors => new List<ValidationError>(_validationErrors);
        public ValidationRules Rules => _rules;

        public void Initialize()
        {
            if (_isInitialized) return;

            _validationErrors.Clear();
            _lastValidValues.Clear();
            ResetStats();
            InitializeDefaultRules();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Plant Data Validation Engine initialized");
            }
        }

        /// <summary>
        /// Validate complete plant data
        /// </summary>
        public ValidationReport ValidateCompleteData(SerializedPlantData data)
        {
            if (!_isInitialized)
            {
                return new ValidationReport
                {
                    IsValid = false,
                    ErrorMessage = "Validation engine not initialized",
                    Errors = new List<ValidationError>()
                };
            }

            var startTime = DateTime.Now;
            _validationErrors.Clear();
            _isDataValid = true;

            // Validate all data categories
            ValidateIdentityData(data);
            ValidateStateData(data);
            ValidateResourceData(data);
            ValidateGrowthData(data);
            ValidateHarvestData(data);
            ValidateLogicalConsistency(data);

            var validationTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
            _stats.TotalValidations++;
            _stats.TotalValidationTime += validationTime;

            var report = new ValidationReport
            {
                IsValid = _isDataValid,
                Errors = new List<ValidationError>(_validationErrors),
                ValidationTime = validationTime,
                FieldsValidated = CountValidatedFields(),
                CriticalErrors = _validationErrors.FindAll(e => e.Severity == ValidationSeverity.Critical).Count,
                WarningCount = _validationErrors.FindAll(e => e.Severity == ValidationSeverity.Warning).Count
            };

            if (_isDataValid)
            {
                _stats.SuccessfulValidations++;
                OnValidationPassed?.Invoke();
            }
            else
            {
                _stats.FailedValidations++;
                OnValidationFailed?.Invoke(_validationErrors);
            }

            OnValidationComplete?.Invoke(report);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Validation completed: {(_isDataValid ? "PASS" : "FAIL")} ({_validationErrors.Count} errors, {validationTime:F2}ms)");
            }

            return report;
        }

        /// <summary>
        /// Validate single field value
        /// </summary>
        public ValidationResult ValidateField(string fieldName, object value, Type expectedType)
        {
            if (!_isInitialized)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Engine not initialized" };
            }

            var result = new ValidationResult { FieldName = fieldName, Value = value, IsValid = true };

            // Type validation
            if (!IsTypeValid(value, expectedType))
            {
                result.IsValid = false;
                result.ErrorMessage = $"Type mismatch: expected {expectedType.Name}, got {value?.GetType().Name ?? "null"}";
                return result;
            }

            // Range validation
            if (value is float floatValue)
            {
                var rangeResult = ValidateFloatRange(fieldName, floatValue);
                if (!rangeResult.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = rangeResult.ErrorMessage;

                    if (_autoCorrectValues && rangeResult.CorrectedValue != null)
                    {
                        result.CorrectedValue = rangeResult.CorrectedValue;
                        result.WasCorrected = true;

                        OnValueCorrected?.Invoke(new ValidationError
                        {
                            FieldName = fieldName,
                            ErrorMessage = $"Auto-corrected {fieldName}: {floatValue} -> {result.CorrectedValue}",
                            Severity = ValidationSeverity.Warning,
                            OriginalValue = floatValue,
                            CorrectedValue = result.CorrectedValue
                        });
                    }
                }
            }

            _stats.FieldValidations++;
            return result;
        }

        /// <summary>
        /// Validate identity data section
        /// </summary>
        private void ValidateIdentityData(SerializedPlantData data)
        {
            // Plant ID validation
            if (string.IsNullOrEmpty(data.PlantID))
            {
                AddValidationError("PlantID", "Plant ID is required", ValidationSeverity.Critical);
            }
            else if (data.PlantID.Length < 3)
            {
                AddValidationError("PlantID", "Plant ID must be at least 3 characters", ValidationSeverity.Warning);
            }

            // Plant name validation
            if (string.IsNullOrEmpty(data.PlantName))
            {
                AddValidationError("PlantName", "Plant name is recommended", ValidationSeverity.Warning);
            }

            // Generation validation
            if (data.GenerationNumber < 1)
            {
                AddValidationError("GenerationNumber", $"Invalid generation number: {data.GenerationNumber}", ValidationSeverity.Critical);
            }

            // Creation date validation
            if (data.CreationDate > DateTime.Now)
            {
                AddValidationError("CreationDate", "Creation date cannot be in the future", ValidationSeverity.Critical);
            }

            _stats.IdentityValidations++;
        }

        /// <summary>
        /// Validate state data section
        /// </summary>
        private void ValidateStateData(SerializedPlantData data)
        {
            // Health validation
            ValidateFloatField("OverallHealth", data.OverallHealth, 0f, 1f, ValidationSeverity.Critical);

            // Vigor validation
            ValidateFloatField("Vigor", data.Vigor, 0f, 2f, ValidationSeverity.Warning);

            // Stress level validation
            ValidateFloatField("StressLevel", data.StressLevel, 0f, 1f, ValidationSeverity.Warning);

            // Age validation
            if (data.AgeInDays < 0f)
            {
                AddValidationError("AgeInDays", $"Negative age: {data.AgeInDays}", ValidationSeverity.Critical);
            }

            // Physical characteristics validation
            if (data.CurrentHeight <= 0f)
            {
                AddValidationError("CurrentHeight", $"Invalid height: {data.CurrentHeight}", ValidationSeverity.Critical);
            }

            if (data.CurrentWidth <= 0f)
            {
                AddValidationError("CurrentWidth", $"Invalid width: {data.CurrentWidth}", ValidationSeverity.Critical);
            }

            if (data.LeafArea < 0f)
            {
                AddValidationError("LeafArea", $"Negative leaf area: {data.LeafArea}", ValidationSeverity.Warning);
            }

            _stats.StateValidations++;
        }

        /// <summary>
        /// Validate resource data section
        /// </summary>
        private void ValidateResourceData(SerializedPlantData data)
        {
            // Water level validation
            ValidateFloatField("WaterLevel", data.WaterLevel, 0f, 1f, ValidationSeverity.Critical);

            // Nutrient level validation
            ValidateFloatField("NutrientLevel", data.NutrientLevel, 0f, 1f, ValidationSeverity.Warning);

            // Energy reserves validation
            ValidateFloatField("EnergyReserves", data.EnergyReserves, 0f, 1f, ValidationSeverity.Warning);

            // Date validation
            if (data.LastWatering > DateTime.Now)
            {
                AddValidationError("LastWatering", "Last watering cannot be in the future", ValidationSeverity.Warning);
            }

            if (data.LastFeeding > DateTime.Now)
            {
                AddValidationError("LastFeeding", "Last feeding cannot be in the future", ValidationSeverity.Warning);
            }

            _stats.ResourceValidations++;
        }

        /// <summary>
        /// Validate growth data section
        /// </summary>
        private void ValidateGrowthData(SerializedPlantData data)
        {
            // Growth progress validation
            ValidateFloatField("GrowthProgress", data.GrowthProgress, 0f, 1f, ValidationSeverity.Warning);

            // Growth rates validation
            if (data.DailyGrowthRate < 0f)
            {
                AddValidationError("DailyGrowthRate", $"Negative growth rate: {data.DailyGrowthRate}", ValidationSeverity.Warning);
            }

            if (data.BiomassAccumulation < 0f)
            {
                AddValidationError("BiomassAccumulation", $"Negative biomass: {data.BiomassAccumulation}", ValidationSeverity.Warning);
            }

            // Genetic parameters validation
            ValidateFloatField("GeneticVigorModifier", data.GeneticVigorModifier, 0.1f, 2f, ValidationSeverity.Warning);

            _stats.GrowthValidations++;
        }

        /// <summary>
        /// Validate harvest data section
        /// </summary>
        private void ValidateHarvestData(SerializedPlantData data)
        {
            // Readiness validation
            ValidateFloatField("HarvestReadiness", data.HarvestReadiness, 0f, 1f, ValidationSeverity.Info);

            // Yield validation
            if (data.EstimatedYield < 0f)
            {
                AddValidationError("EstimatedYield", $"Negative yield: {data.EstimatedYield}", ValidationSeverity.Warning);
            }

            // Potency validation
            ValidateFloatField("EstimatedPotency", data.EstimatedPotency, 0f, 1f, ValidationSeverity.Info);

            // Harvest date validation
            if (data.OptimalHarvestDate != DateTime.MinValue && data.OptimalHarvestDate < data.CreationDate)
            {
                AddValidationError("OptimalHarvestDate", "Harvest date before creation date", ValidationSeverity.Warning);
            }

            _stats.HarvestValidations++;
        }

        /// <summary>
        /// Validate logical consistency across all data
        /// </summary>
        private void ValidateLogicalConsistency(SerializedPlantData data)
        {
            // Growth stage vs age consistency
            var minAgeForStage = GetMinimumAgeForStage(data.CurrentGrowthStage);
            if (data.AgeInDays < minAgeForStage)
            {
                AddValidationError("GrowthStage", $"Plant too young for {data.CurrentGrowthStage} stage (age: {data.AgeInDays:F1}d, min: {minAgeForStage:F1}d)", ValidationSeverity.Warning);
            }

            // Height vs growth stage consistency
            var expectedHeight = GetExpectedHeightForStage(data.CurrentGrowthStage);
            if (data.CurrentHeight < expectedHeight * 0.5f || data.CurrentHeight > expectedHeight * 2f)
            {
                AddValidationError("CurrentHeight", $"Height unusual for {data.CurrentGrowthStage} stage (actual: {data.CurrentHeight:F1}cm, expected: ~{expectedHeight:F1}cm)", ValidationSeverity.Info);
            }

            // Health vs stress consistency
            if (data.OverallHealth > 0.8f && data.StressLevel > 0.5f)
            {
                AddValidationError("HealthStress", "High health with high stress is inconsistent", ValidationSeverity.Warning);
            }

            _stats.ConsistencyValidations++;
        }

        /// <summary>
        /// Validate float field against range
        /// </summary>
        private void ValidateFloatField(string fieldName, float value, float min, float max, ValidationSeverity severity)
        {
            if (value < min || value > max)
            {
                var message = $"{fieldName} out of range: {value:F3} (expected: {min:F1}-{max:F1})";
                AddValidationError(fieldName, message, severity);

                // Auto-correct if enabled
                if (_autoCorrectValues)
                {
                    var corrected = Mathf.Clamp(value, min, max);
                    OnValueCorrected?.Invoke(new ValidationError
                    {
                        FieldName = fieldName,
                        ErrorMessage = $"Auto-corrected {fieldName}: {value:F3} -> {corrected:F3}",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = value,
                        CorrectedValue = corrected
                    });
                }
            }
        }

        /// <summary>
        /// Validate float range with correction
        /// </summary>
        private ValidationResult ValidateFloatRange(string fieldName, float value)
        {
            var result = new ValidationResult { FieldName = fieldName, Value = value, IsValid = true };

            if (_rules.FloatRanges.TryGetValue(fieldName, out var range))
            {
                if (value < range.Min || value > range.Max)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"{fieldName} out of range: {value:F3} (expected: {range.Min:F1}-{range.Max:F1})";

                    if (_autoCorrectValues)
                    {
                        result.CorrectedValue = Mathf.Clamp(value, range.Min, range.Max);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Add validation error
        /// </summary>
        private void AddValidationError(string fieldName, string message, ValidationSeverity severity)
        {
            var error = new ValidationError
            {
                FieldName = fieldName,
                ErrorMessage = message,
                Severity = severity,
                Timestamp = DateTime.Now
            };

            _validationErrors.Add(error);

            if (severity == ValidationSeverity.Critical)
            {
                _isDataValid = false;
            }

            _stats.TotalErrors++;
        }

        /// <summary>
        /// Check if type is valid
        /// </summary>
        private bool IsTypeValid(object value, Type expectedType)
        {
            if (value == null) return !expectedType.IsValueType || Nullable.GetUnderlyingType(expectedType) != null;
            return expectedType.IsAssignableFrom(value.GetType());
        }

        /// <summary>
        /// Get minimum age for growth stage
        /// </summary>
        private float GetMinimumAgeForStage(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Seedling => 0f,
                PlantGrowthStage.Vegetative => 14f,
                PlantGrowthStage.PreFlowering => 35f,
                PlantGrowthStage.Flowering => 56f,
                PlantGrowthStage.Ripening => 77f,
                PlantGrowthStage.Harvest => 98f,
                _ => 0f
            };
        }

        /// <summary>
        /// Get expected height for growth stage
        /// </summary>
        private float GetExpectedHeightForStage(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Seedling => 5f,
                PlantGrowthStage.Vegetative => 25f,
                PlantGrowthStage.PreFlowering => 60f,
                PlantGrowthStage.Flowering => 100f,
                PlantGrowthStage.Ripening => 120f,
                PlantGrowthStage.Harvest => 150f,
                _ => 50f
            };
        }

        /// <summary>
        /// Count validated fields
        /// </summary>
        private int CountValidatedFields()
        {
            return 25; // Total serialized fields
        }

        /// <summary>
        /// Initialize default validation rules
        /// </summary>
        private void InitializeDefaultRules()
        {
            _rules.FloatRanges["OverallHealth"] = new FloatRange { Min = 0f, Max = 1f };
            _rules.FloatRanges["Vigor"] = new FloatRange { Min = 0f, Max = 2f };
            _rules.FloatRanges["StressLevel"] = new FloatRange { Min = 0f, Max = 1f };
            _rules.FloatRanges["WaterLevel"] = new FloatRange { Min = 0f, Max = 1f };
            _rules.FloatRanges["NutrientLevel"] = new FloatRange { Min = 0f, Max = 1f };
            _rules.FloatRanges["EnergyReserves"] = new FloatRange { Min = 0f, Max = 1f };
            _rules.FloatRanges["GrowthProgress"] = new FloatRange { Min = 0f, Max = 1f };
            _rules.FloatRanges["HarvestReadiness"] = new FloatRange { Min = 0f, Max = 1f };
            _rules.FloatRanges["EstimatedPotency"] = new FloatRange { Min = 0f, Max = 1f };
            _rules.FloatRanges["GeneticVigorModifier"] = new FloatRange { Min = 0.1f, Max = 2f };
        }

        /// <summary>
        /// Reset validation statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new ValidationStats();
        }

        /// <summary>
        /// Set validation configuration
        /// </summary>
        public void SetValidationConfig(bool strictValidation, bool autoCorrect, float tolerance)
        {
            _strictValidation = strictValidation;
            _autoCorrectValues = autoCorrect;
            _healthTolerance = Mathf.Max(0f, tolerance);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Validation config updated: Strict={strictValidation}, AutoCorrect={autoCorrect}, Tolerance={tolerance:F2}");
            }
        }

        /// <summary>
        /// Add custom validation rule
        /// </summary>
        public void AddFloatValidationRule(string fieldName, float min, float max)
        {
            _rules.FloatRanges[fieldName] = new FloatRange { Min = min, Max = max };

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Added validation rule for {fieldName}: {min:F1}-{max:F1}");
            }
        }
    }

    /// <summary>
    /// Validation rules configuration
    /// </summary>
    [System.Serializable]
    }
