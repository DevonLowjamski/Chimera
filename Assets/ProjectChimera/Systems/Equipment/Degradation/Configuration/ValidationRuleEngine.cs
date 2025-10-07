using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// PHASE 0 REFACTORED: Validation Rule Engine
    /// Single Responsibility: Execute custom validation rules and cross-parameter validation
    /// Extracted from ConfigurationValidationManager (759 lines → 4 files <500 lines each)
    /// </summary>
    public class ValidationRuleEngine
    {
        private readonly List<ValidationRule> _customRules;
        private readonly bool _enableLogging;
        private readonly ValidationMode _validationMode;

        public event Action<ValidationRule, bool> OnCustomRuleValidated;
        public IReadOnlyList<ValidationRule> CustomRules => _customRules;

        public ValidationRuleEngine(bool enableLogging, ValidationMode validationMode)
        {
            _customRules = new List<ValidationRule>();
            _enableLogging = enableLogging;
            _validationMode = validationMode;
        }

        /// <summary>
        /// Add a custom validation rule
        /// </summary>
        public void AddCustomRule(ValidationRule rule)
        {
            if (rule == null || string.IsNullOrEmpty(rule.Name))
                return;

            // Remove existing rule with the same name
            _customRules.RemoveAll(r => r.Name == rule.Name);
            _customRules.Add(rule);

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Added custom rule '{rule.Name}'", null);
        }

        /// <summary>
        /// Remove a custom validation rule
        /// </summary>
        public bool RemoveCustomRule(string ruleName)
        {
            var removed = _customRules.RemoveAll(r => r.Name == ruleName);

            if (removed > 0 && _enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Removed custom rule '{ruleName}'", null);

            return removed > 0;
        }

        /// <summary>
        /// Validate all custom rules
        /// </summary>
        public void ValidateCustomRules(CostConfigurationProfile profile, List<string> errors, List<string> warnings)
        {
            foreach (var rule in _customRules)
            {
                try
                {
                    var ruleResult = rule.ValidationFunction(profile);
                    OnCustomRuleValidated?.Invoke(rule, ruleResult.IsValid);

                    if (!ruleResult.IsValid)
                    {
                        if (rule.Severity == ValidationSeverity.Error)
                            errors.Add($"Custom rule '{rule.Name}': {ruleResult.ErrorMessage}");
                        else
                            warnings.Add($"Custom rule '{rule.Name}': {ruleResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Custom rule '{rule.Name}' threw exception: {ex.Message}");

                    if (_enableLogging)
                        ChimeraLogger.LogError("CONFIG_VAL", $"Custom rule '{rule.Name}' exception: {ex.Message}", null);
                }
            }
        }

        /// <summary>
        /// Validate cross-parameter rules
        /// </summary>
        public void ValidateCrossParameterRules(CostConfigurationProfile profile, List<string> errors, List<string> warnings)
        {
            // Rule 1: Minimum cost should not exceed maximum cost
            ValidateMinMaxCostConsistency(profile, errors);

            // Rule 2: Material markup should be reasonable relative to base cost
            ValidateMaterialMarkupReasonableness(profile, warnings);

            // Rule 3: Urgency multiplier should not create unreasonable total costs
            ValidateUrgencyMultiplierImpact(profile, warnings);

            // Rule 4: Complexity factor should align with other cost parameters
            ValidateComplexityFactorAlignment(profile, warnings);
        }

        #region Cross-Parameter Validation Rules

        /// <summary>
        /// Validate minimum/maximum cost consistency
        /// </summary>
        private void ValidateMinMaxCostConsistency(CostConfigurationProfile profile, List<string> errors)
        {
            if (profile.Parameters.TryGetValue("MinimumCost", out var minCostObj) &&
                profile.Parameters.TryGetValue("MaximumCost", out var maxCostObj))
            {
                if (minCostObj is float minCost && maxCostObj is float maxCost)
                {
                    if (minCost > maxCost)
                    {
                        errors.Add("MinimumCost cannot be greater than MaximumCost");
                    }
                }
            }
        }

        /// <summary>
        /// Validate material markup reasonableness
        /// </summary>
        private void ValidateMaterialMarkupReasonableness(CostConfigurationProfile profile, List<string> warnings)
        {
            if (profile.Parameters.TryGetValue("BaseCost", out var baseCostObj) &&
                profile.Parameters.TryGetValue("MaterialMarkup", out var markupObj))
            {
                if (baseCostObj is float baseCost && markupObj is float markup)
                {
                    var adjustedCost = baseCost * markup;
                    if (adjustedCost > 100000.0f) // Arbitrary high threshold
                    {
                        warnings.Add("Material markup results in extremely high adjusted cost");
                    }

                    if (markup > 5.0f) // Very high markup
                    {
                        warnings.Add("Material markup is unusually high (>5x)");
                    }
                }
            }
        }

        /// <summary>
        /// Validate urgency multiplier impact
        /// </summary>
        private void ValidateUrgencyMultiplierImpact(CostConfigurationProfile profile, List<string> warnings)
        {
            if (profile.Parameters.TryGetValue("BaseCost", out var baseCostObj) &&
                profile.Parameters.TryGetValue("UrgencyMultiplier", out var urgencyObj))
            {
                if (baseCostObj is float baseCost && urgencyObj is float urgency)
                {
                    var adjustedCost = baseCost * urgency;
                    if (adjustedCost > 50000.0f)
                    {
                        warnings.Add("Urgency multiplier creates very high total cost");
                    }
                }
            }
        }

        /// <summary>
        /// Validate complexity factor alignment
        /// </summary>
        private void ValidateComplexityFactorAlignment(CostConfigurationProfile profile, List<string> warnings)
        {
            if (profile.Parameters.TryGetValue("ComplexityFactor", out var complexityObj) &&
                profile.Parameters.TryGetValue("LaborRatePerHour", out var laborRateObj))
            {
                if (complexityObj is float complexity && laborRateObj is float laborRate)
                {
                    // High complexity should correlate with higher labor rates
                    if (complexity > 5.0f && laborRate < 100.0f)
                    {
                        warnings.Add("High complexity factor but low labor rate - this may be inconsistent");
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Create common cross-parameter validation rules
        /// </summary>
        public void AddCommonCrossParameterRules()
        {
            // Rule: Labor cost shouldn't exceed 80% of base cost
            AddCustomRule(ValidationRule.Create(
                "LaborCostRatio",
                profile =>
                {
                    var baseCost = profile.GetParameter<float>("BaseCost", 0f);
                    var laborRate = profile.GetParameter<float>("LaborRatePerHour", 0f);
                    var estimatedHours = profile.GetParameter<float>("EstimatedHours", 1f);

                    if (baseCost > 0)
                    {
                        var laborCost = laborRate * estimatedHours;
                        var laborRatio = laborCost / baseCost;

                        if (laborRatio > 0.8f)
                        {
                            return ParameterValidationResult.Failure(
                                $"Labor cost ({laborCost:F2}) exceeds 80% of base cost ({baseCost:F2})");
                        }
                    }

                    return ParameterValidationResult.Success();
                },
                ValidationSeverity.Warning,
                "Validates labor cost doesn't exceed reasonable ratio of base cost"));

            // Rule: Total multipliers shouldn't create unrealistic costs
            AddCustomRule(ValidationRule.Create(
                "TotalMultiplierCheck",
                profile =>
                {
                    var materialMarkup = profile.GetParameter<float>("MaterialMarkup", 1f);
                    var urgencyMultiplier = profile.GetParameter<float>("UrgencyMultiplier", 1f);
                    var complexityFactor = profile.GetParameter<float>("ComplexityFactor", 1f);

                    var totalMultiplier = materialMarkup * urgencyMultiplier * complexityFactor;

                    if (totalMultiplier > 50.0f)
                    {
                        return ParameterValidationResult.Failure(
                            $"Combined multipliers ({totalMultiplier:F2}x) create unrealistic cost escalation");
                    }

                    return ParameterValidationResult.Success();
                },
                ValidationSeverity.Warning,
                "Validates combined multipliers don't create unrealistic costs"));

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Added {_customRules.Count} common cross-parameter rules", null);
        }

        #endregion
    }
}

