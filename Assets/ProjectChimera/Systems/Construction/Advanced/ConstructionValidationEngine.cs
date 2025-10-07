using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction.Advanced
{
    /// <summary>
    /// REFACTORED: Construction Validation Engine
    /// Single Responsibility: Construction validation processing and rule enforcement
    /// Extracted from AdvancedConstructionSystem for better separation of concerns
    /// </summary>
    public class ConstructionValidationEngine : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableRealTimeValidation = true;
        [SerializeField] private int _maxValidationsPerFrame = 20;
        [SerializeField] private float _validationRadius = 50f;

        // Validation processing
        private readonly List<ConstructionValidation> _pendingValidations = new List<ConstructionValidation>();
        private readonly List<ConstructionValidation> _completedValidations = new List<ConstructionValidation>();
        private readonly Dictionary<string, ValidationRule> _validationRules = new Dictionary<string, ValidationRule>();

        // External validators
        private StructuralValidator _structuralValidator;
        
        // Performance tracking
        private int _validationsThisFrame;
        private bool _isInitialized = false;

        // Statistics
        private ConstructionValidationStats _stats = new ConstructionValidationStats();

        // Events
        public event System.Action<ConstructionValidation> OnValidationStarted;
        public event System.Action<ConstructionValidation> OnValidationCompleted;
        public event System.Action<ConstructionValidation> OnValidationFailed;

        public bool IsInitialized => _isInitialized;
        public ConstructionValidationStats Stats => _stats;
        public int PendingValidationCount => _pendingValidations.Count;
        public int ValidationsThisFrame => _validationsThisFrame;

        public void Initialize(StructuralValidator structuralValidator = null)
        {
            if (_isInitialized) return;

            _structuralValidator = structuralValidator;
            _pendingValidations.Clear();
            _completedValidations.Clear();
            InitializeValidationRules();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CONSTRUCTION", "Construction Validation Engine initialized", this);
            }
        }

        /// <summary>
        /// Initialize validation rules
        /// </summary>
        private void InitializeValidationRules()
        {
            _validationRules.Clear();

            // Register standard validation rules
            RegisterValidationRule("StructuralIntegrity", new StructuralIntegrityRule());
            RegisterValidationRule("ResourceAvailability", new ResourceAvailabilityRule());
            RegisterValidationRule("SpatialPlacement", new SpatialPlacementRule());
            RegisterValidationRule("EnvironmentalSuitability", new EnvironmentalSuitabilityRule());
            RegisterValidationRule("BuildingCodes", new BuildingCodesRule());
        }

        /// <summary>
        /// Process validation queue (call from Update or ITickable)
        /// </summary>
        public void ProcessValidations(int maxValidations = -1)
        {
            if (!_isInitialized) return;

            _validationsThisFrame = 0;
            int validationsToProcess = maxValidations > 0 ? maxValidations : _maxValidationsPerFrame;

            var validationsToCheck = _pendingValidations.Take(validationsToProcess).ToList();

            foreach (var validation in validationsToCheck)
            {
                if (_validationsThisFrame >= validationsToProcess)
                    break;

                ProcessValidation(validation);
                _validationsThisFrame++;
            }

            // Remove processed validations
            foreach (var processedValidation in validationsToCheck.Where(v => v.IsProcessed))
            {
                _pendingValidations.Remove(processedValidation);
                _completedValidations.Add(processedValidation);
            }

            _stats.LastProcessingTime = Time.time;
        }

        /// <summary>
        /// Queue validation request
        /// </summary>
        public bool QueueValidation(ConstructionValidation validation)
        {
            if (validation == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("CONSTRUCTION", "Cannot queue null validation", this);
                }
                return false;
            }

            validation.RequestTime = Time.realtimeSinceStartup;
            validation.ValidationId = System.Guid.NewGuid().ToString();
            
            _pendingValidations.Add(validation);
            _stats.ValidationsQueued++;

            OnValidationStarted?.Invoke(validation);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CONSTRUCTION", $"Queued validation {validation.ValidationId} ({_pendingValidations.Count} pending)", this);
            }

            return true;
        }

        /// <summary>
        /// Process individual validation
        /// </summary>
        private void ProcessValidation(ConstructionValidation validation)
        {
            if (validation.IsProcessed) return;

            try
            {
                var validationStartTime = Time.realtimeSinceStartup;

                // Run all validation rules
                validation.IsValid = RunValidationRules(validation);
                validation.ProcessingTime = Time.realtimeSinceStartup - validationStartTime;
                validation.IsProcessed = true;

                // Update statistics
                _stats.ValidationsProcessed++;
                if (!validation.IsValid)
                {
                    _stats.ValidationsFailed++;
                    OnValidationFailed?.Invoke(validation);
                }

                OnValidationCompleted?.Invoke(validation);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CONSTRUCTION", $"Validation {validation.ValidationId} completed - Result: {validation.IsValid} (Time: {validation.ProcessingTime:F3}s)", this);
                }
            }
            catch (System.Exception ex)
            {
                validation.IsValid = false;
                validation.ErrorMessage = ex.Message;
                validation.IsProcessed = true;
                validation.ProcessingTime = Time.realtimeSinceStartup - validation.RequestTime;

                _stats.ValidationErrors++;
                OnValidationFailed?.Invoke(validation);

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("CONSTRUCTION", $"Validation {validation.ValidationId} error: {ex.Message}", this);
                }
            }
        }

        /// <summary>
        /// Run all validation rules
        /// </summary>
        private bool RunValidationRules(ConstructionValidation validation)
        {
            bool overallResult = true;
            var results = new Dictionary<string, bool>();

            foreach (var rule in _validationRules)
            {
                try
                {
                    bool ruleResult = rule.Value.Validate(validation);
                    results[rule.Key] = ruleResult;
                    overallResult &= ruleResult;

                    if (!ruleResult && _enableLogging)
                    {
                        ChimeraLogger.Log("CONSTRUCTION", $"Validation rule '{rule.Key}' failed for {validation.ValidationId}", this);
                    }
                }
                catch (System.Exception ex)
                {
                    results[rule.Key] = false;
                    overallResult = false;
                    
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogError("CONSTRUCTION", $"Error in validation rule '{rule.Key}': {ex.Message}", this);
                    }
                }
            }

            validation.RuleResults = results;
            return overallResult;
        }

        /// <summary>
        /// Register custom validation rule
        /// </summary>
        public void RegisterValidationRule(string ruleName, ValidationRule rule)
        {
            if (string.IsNullOrEmpty(ruleName) || rule == null)
                return;

            _validationRules[ruleName] = rule;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CONSTRUCTION", $"Registered validation rule: {ruleName}", this);
            }
        }

        /// <summary>
        /// Unregister validation rule
        /// </summary>
        public bool UnregisterValidationRule(string ruleName)
        {
            if (_validationRules.Remove(ruleName))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log("CONSTRUCTION", $"Unregistered validation rule: {ruleName}", this);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get validation by ID
        /// </summary>
        public ConstructionValidation GetValidation(string validationId)
        {
            return _completedValidations.FirstOrDefault(v => v.ValidationId == validationId) ??
                   _pendingValidations.FirstOrDefault(v => v.ValidationId == validationId);
        }

        /// <summary>
        /// Clear completed validations
        /// </summary>
        public int ClearCompletedValidations()
        {
            var count = _completedValidations.Count;
            _completedValidations.Clear();

            if (_enableLogging && count > 0)
            {
                ChimeraLogger.Log("CONSTRUCTION", $"Cleared {count} completed validations", this);
            }

            return count;
        }

        /// <summary>
        /// Set maximum validations per frame
        /// </summary>
        public void SetMaxValidationsPerFrame(int maxValidations)
        {
            _maxValidationsPerFrame = Mathf.Max(1, maxValidations);
            
            if (_enableLogging)
            {
                ChimeraLogger.Log("CONSTRUCTION", $"Max validations per frame set to {_maxValidationsPerFrame}", this);
            }
        }

        /// <summary>
        /// Enable or disable real-time validation
        /// </summary>
        public void SetRealTimeValidationEnabled(bool enabled)
        {
            _enableRealTimeValidation = enabled;
            
            if (_enableLogging)
            {
                ChimeraLogger.Log("CONSTRUCTION", $"Real-time validation {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new ConstructionValidationStats
            {
                ValidationsQueued = 0,
                ValidationsProcessed = 0,
                ValidationsFailed = 0,
                ValidationErrors = 0,
                LastProcessingTime = Time.time
            };
        }

        private void OnDestroy()
        {
            _pendingValidations.Clear();
            _completedValidations.Clear();
            _validationRules.Clear();
        }
    }

    /// <summary>
    /// Construction validation data structure
    /// </summary>
    [System.Serializable]
    public class ConstructionValidation
    {
        public string ValidationId;
        public string ProjectId;
        public Vector3 Position;
        public Vector3 Size;
        public string ConstructionType;
        public bool IsValid;
        public bool IsProcessed;
        public float RequestTime;
        public float ProcessingTime;
        public string ErrorMessage;
        public Dictionary<string, bool> RuleResults;
        public object CustomData;
    }

    /// <summary>
    /// Base validation rule interface
    /// </summary>
    public abstract class ValidationRule
    {
        public abstract bool Validate(ConstructionValidation validation);
    }

    /// <summary>
    /// Structural integrity validation rule
    /// </summary>
    public class StructuralIntegrityRule : ValidationRule
    {
        public override bool Validate(ConstructionValidation validation)
        {
            // Implement structural integrity validation logic
            return true; // Placeholder
        }
    }

    /// <summary>
    /// Resource availability validation rule
    /// </summary>
    public class ResourceAvailabilityRule : ValidationRule
    {
        public override bool Validate(ConstructionValidation validation)
        {
            // Implement resource availability validation logic
            return true; // Placeholder
        }
    }

    /// <summary>
    /// Spatial placement validation rule
    /// </summary>
    public class SpatialPlacementRule : ValidationRule
    {
        public override bool Validate(ConstructionValidation validation)
        {
            // Implement spatial placement validation logic
            return true; // Placeholder
        }
    }

    /// <summary>
    /// Environmental suitability validation rule
    /// </summary>
    public class EnvironmentalSuitabilityRule : ValidationRule
    {
        public override bool Validate(ConstructionValidation validation)
        {
            // Implement environmental suitability validation logic
            return true; // Placeholder
        }
    }

    /// <summary>
    /// Building codes validation rule
    /// </summary>
    public class BuildingCodesRule : ValidationRule
    {
        public override bool Validate(ConstructionValidation validation)
        {
            // Implement building codes validation logic
            return true; // Placeholder
        }
    }

    /// <summary>
    /// Placeholder for StructuralValidator
    /// </summary>
    public class StructuralValidator : MonoBehaviour
    {
        public void Initialize(object parent) { }
        public bool ValidateStructuralIntegrity(ConstructionValidation validation) { return true; }
    }

    /// <summary>
    /// Construction validation statistics
    /// </summary>
    [System.Serializable]
    public struct ConstructionValidationStats
    {
        public int ValidationsQueued;
        public int ValidationsProcessed;
        public int ValidationsFailed;
        public int ValidationErrors;
        public float LastProcessingTime;
    }
}