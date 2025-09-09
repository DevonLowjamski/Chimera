using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Manages plant health, stress responses, and disease resistance.
    /// Handles health updates, stress calculations, and recovery mechanisms.
    /// </summary>
    public class PlantHealthSystem
    {
        private object _strain;
        private float _currentHealth = 1f;
        private float _maxHealth = 1f;
        private float _stressLevel = 0f;
        private float _diseaseResistance = 1f;
        private float _recoveryRate = 0.1f;

        // Health tracking
        private float _lastHealthCheckTime;
        private List<HealthEvent> _healthHistory = new List<HealthEvent>();
        private Dictionary<string, float> _activeStressFactors = new Dictionary<string, float>();

        // Configuration
        private PlantUpdateConfiguration _configuration;
        private const int MAX_HEALTH_HISTORY = 100;

        /// <summary>
        /// Initialize the health system with strain and resistance data
        /// </summary>
        public void Initialize(object strain, float diseaseResistance, PlantUpdateConfiguration configuration = null)
        {
            _strain = strain;
            _configuration = configuration ?? PlantUpdateConfiguration.CreateDefault();

            // Extract health parameters from strain
            ExtractStrainHealthParameters(strain);

            _currentHealth = _maxHealth;
            _diseaseResistance = Mathf.Clamp01(diseaseResistance);
            _lastHealthCheckTime = Time.time;

            // Record initialization
            RecordHealthEvent("System Initialized", _currentHealth);

            ChimeraLogger.Log($"[PlantHealthSystem] Initialized - Max Health: {_maxHealth:F2}, Disease Resistance: {_diseaseResistance:F2}");
        }

        /// <summary>
        /// Updates health based on stressors and environmental conditions
        /// </summary>
        public void UpdateHealth(float deltaTime, List<ActiveStressor> stressors, float environmentalFitness)
        {
            float previousHealth = _currentHealth;

            // Calculate stress damage
            float stressDamage = CalculateStressDamage(stressors, deltaTime);

            // Calculate environmental health effects
            float environmentalEffect = CalculateEnvironmentalHealthEffect(environmentalFitness, deltaTime);

            // Apply natural recovery
            float naturalRecovery = CalculateNaturalRecovery(deltaTime);

            // Apply regenerative effects from good conditions
            float regenerativeEffect = CalculateRegenerativeEffects(environmentalFitness, deltaTime);

            // Update health with all factors
            float healthChange = environmentalEffect + naturalRecovery + regenerativeEffect - stressDamage;
            _currentHealth = Mathf.Clamp(_currentHealth + healthChange, 0f, _maxHealth);

            // Update stress level
            UpdateStressLevel(stressors);

            // Track significant health changes
            if (Mathf.Abs(_currentHealth - previousHealth) > 0.01f)
            {
                string changeDescription = healthChange > 0 ? "Health Improved" : "Health Declined";
                RecordHealthEvent(changeDescription, _currentHealth, healthChange);
            }

            // Check for critical health conditions
            CheckCriticalHealthConditions();

            _lastHealthCheckTime = Time.time;
        }

        /// <summary>
        /// Apply immediate health change (for external effects)
        /// </summary>
        public void ApplyHealthChange(float change, string reason = "External Effect")
        {
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + change, 0f, _maxHealth);

            if (change != 0f)
            {
                RecordHealthEvent(reason, _currentHealth, change);

                if (_configuration.EnablePerformanceOptimization)
                {
                    ChimeraLogger.LogVerbose($"[PlantHealthSystem] {reason}: {change:F3} (Health: {previousHealth:F2} → {_currentHealth:F2})");
                }
            }
        }

        /// <summary>
        /// Apply specific stress effects to the plant
        /// </summary>
        public void ApplyStressEffect(StressFactor stressFactor, float deltaTime)
        {
            if (stressFactor == null) return;

            string stressTypeName = stressFactor.GetStressTypeName();
            float damage = CalculateStressSpecificDamage(stressFactor, deltaTime);

            // Track active stress factors
            _activeStressFactors[stressTypeName] = stressFactor.Severity;

            // Apply the damage
            ApplyHealthChange(-damage, $"Stress: {stressTypeName}");
        }

        /// <summary>
        /// Process recovery from stress removal
        /// </summary>
        public void ProcessStressRecovery(string stressType, float recoveryRate, float deltaTime)
        {
            if (_activeStressFactors.ContainsKey(stressType))
            {
                _activeStressFactors.Remove(stressType);

                // Apply recovery bonus when stress is removed
                float recoveryBonus = recoveryRate * deltaTime * 2f; // 2x bonus for removing stress
                ApplyHealthChange(recoveryBonus, $"Recovery from {stressType}");
            }
        }

        #region Public Properties and Getters

        public float GetCurrentHealth() => _currentHealth;
        public float GetMaxHealth() => _maxHealth;
        public float GetStressLevel() => _stressLevel;
        public float GetHealthPercentage() => _currentHealth / _maxHealth;
        public float GetDiseaseResistance() => _diseaseResistance;
        public float GetRecoveryRate() => _recoveryRate;

        /// <summary>
        /// Get health status classification
        /// </summary>
        public HealthStatus GetHealthStatus()
        {
            float percentage = GetHealthPercentage();

            if (percentage >= 0.8f) return HealthStatus.Excellent;
            if (percentage >= 0.6f) return HealthStatus.Good;
            if (percentage >= 0.4f) return HealthStatus.Fair;
            if (percentage >= 0.2f) return HealthStatus.Poor;
            return HealthStatus.Critical;
        }

        /// <summary>
        /// Get current active stress factors
        /// </summary>
        public Dictionary<string, float> GetActiveStressFactors()
        {
            return new Dictionary<string, float>(_activeStressFactors);
        }

        /// <summary>
        /// Get health trend over time
        /// </summary>
        public HealthTrend GetHealthTrend(int sampleCount = 10)
        {
            if (_healthHistory.Count < 2) return HealthTrend.Stable;

            int samples = Mathf.Min(sampleCount, _healthHistory.Count);
            float startHealth = _healthHistory[_healthHistory.Count - samples].Health;
            float endHealth = _healthHistory[_healthHistory.Count - 1].Health;

            float difference = endHealth - startHealth;

            if (difference > 0.05f) return HealthTrend.Improving;
            if (difference < -0.05f) return HealthTrend.Declining;
            return HealthTrend.Stable;
        }

        #endregion

        #region Private Health Calculation Methods

        private float CalculateStressDamage(List<ActiveStressor> stressors, float deltaTime)
        {
            float totalDamage = 0f;

            foreach (var stressor in stressors)
            {
                if (!stressor.IsActive)
                    continue;

                totalDamage += CalculateStressSpecificDamage(stressor, deltaTime);
            }

            return totalDamage;
        }

        private float CalculateStressSpecificDamage(ActiveStressor stressor, float deltaTime)
        {
            float baseDamage = stressor.Intensity * stressor.StressSource.DamagePerSecond * deltaTime;

            // Apply disease resistance for biotic stress
            if (stressor.StressSource.IsBiotic())
            {
                baseDamage *= (1f - _diseaseResistance);
            }

            // Apply chronic stress multiplier
            if (stressor.IsChronic)
            {
                baseDamage *= 1.3f; // 30% more damage for chronic stress
            }

            return baseDamage;
        }

        private float CalculateStressSpecificDamage(StressFactor stressFactor, float deltaTime)
        {
            float baseDamage = stressFactor.Severity * 0.01f * deltaTime; // Base damage rate

            // Apply disease resistance for biotic stress
            string stressTypeName = stressFactor.GetStressTypeName();
            if (stressTypeName.Contains("Biotic") || stressTypeName.Contains("Disease"))
            {
                baseDamage *= (1f - _diseaseResistance);
            }

            // Apply severity multipliers
            if (stressFactor.IsCritical)
            {
                baseDamage *= 2f; // Double damage for critical stress
            }

            return baseDamage;
        }

        private float CalculateEnvironmentalHealthEffect(float environmentalFitness, float deltaTime)
        {
            // Good environmental conditions promote health recovery
            if (environmentalFitness > 0.8f)
            {
                float bonus = (environmentalFitness - 0.8f) * 0.5f * deltaTime;
                return bonus * (1f + _recoveryRate); // Recovery rate affects environmental bonus
            }
            // Poor conditions cause slow health decline
            else if (environmentalFitness < 0.4f)
            {
                float penalty = (environmentalFitness - 0.4f) * 0.2f * deltaTime;
                return penalty * (2f - _diseaseResistance); // Disease resistance helps with poor conditions
            }

            return 0f;
        }

        private float CalculateNaturalRecovery(float deltaTime)
        {
            // Natural recovery is reduced when health is very low (plant is struggling)
            float healthFactor = Mathf.Lerp(0.3f, 1f, GetHealthPercentage());

            // Recovery is faster when not under significant stress
            float stressFactor = Mathf.Lerp(1.2f, 0.5f, _stressLevel);

            return _recoveryRate * deltaTime * healthFactor * stressFactor;
        }

        private float CalculateRegenerativeEffects(float environmentalFitness, float deltaTime)
        {
            // Only apply regenerative effects under excellent conditions
            if (environmentalFitness < 0.9f) return 0f;

            // Regenerative bonus for excellent conditions
            float regenerativeRate = 0.05f; // Base regenerative rate
            float bonus = (environmentalFitness - 0.9f) * 10f; // Scale the bonus

            return regenerativeRate * bonus * deltaTime;
        }

        private void UpdateStressLevel(List<ActiveStressor> stressors)
        {
            _stressLevel = 0f;

            foreach (var stressor in stressors)
            {
                if (stressor.IsActive)
                {
                    float stressContribution = stressor.GetCurrentSeverity();
                    _stressLevel += stressContribution;
                }
            }

            _stressLevel = Mathf.Clamp01(_stressLevel);
        }

        #endregion

        #region Private Helper Methods

        private void ExtractStrainHealthParameters(object strain)
        {
            try
            {
                if (strain is PlantStrainSO strainSO)
                {
                    // _maxHealth = strainSO.BaseHealthModifier; // Property doesn't exist yet
                    // _recoveryRate = strainSO.HealthRecoveryRate; // Property doesn't exist yet
                    _maxHealth = 1f; // Default until property is implemented
                    _recoveryRate = 0.1f; // Default until property is implemented
                }
                else
                {
                    _maxHealth = 1f;
                    _recoveryRate = 0.1f;
                }
            }
            catch
            {
                _maxHealth = 1f;
                _recoveryRate = 0.1f;
                ChimeraLogger.LogWarning("[PlantHealthSystem] Could not extract strain health parameters, using defaults");
            }
        }

        private void RecordHealthEvent(string description, float currentHealth, float change = 0f)
        {
            var healthEvent = new HealthEvent
            {
                Timestamp = Time.time,
                Description = description,
                Health = currentHealth,
                Change = change,
                StressLevel = _stressLevel
            };

            _healthHistory.Add(healthEvent);

            // Limit history size for performance
            if (_healthHistory.Count > MAX_HEALTH_HISTORY)
            {
                _healthHistory.RemoveAt(0);
            }
        }

        private void CheckCriticalHealthConditions()
        {
            float healthPercentage = GetHealthPercentage();

            if (healthPercentage <= 0.1f && _currentHealth > 0f)
            {
                ChimeraLogger.LogWarning($"[PlantHealthSystem] Critical health condition detected: {healthPercentage:P1}");
            }

            if (_stressLevel >= 0.8f)
            {
                ChimeraLogger.LogWarning($"[PlantHealthSystem] High stress level detected: {_stressLevel:P1}");
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Get health recovery estimate for given conditions
        /// </summary>
        public float EstimateRecoveryTime(float targetHealthPercentage, float environmentalFitness)
        {
            if (targetHealthPercentage <= GetHealthPercentage()) return 0f;

            float healthNeeded = _maxHealth * targetHealthPercentage - _currentHealth;
            float recoveryRate = CalculateNaturalRecovery(1f) + CalculateEnvironmentalHealthEffect(environmentalFitness, 1f);

            if (recoveryRate <= 0f) return float.MaxValue; // Cannot recover under current conditions

            return healthNeeded / recoveryRate; // Time in seconds
        }

        /// <summary>
        /// Get recommended actions for current health status
        /// </summary>
        public List<string> GetHealthRecommendations()
        {
            var recommendations = new List<string>();

            float healthPercentage = GetHealthPercentage();

            if (healthPercentage < 0.3f)
            {
                recommendations.Add("Critical: Immediate intervention required");
                recommendations.Add("Remove all stress factors if possible");
                recommendations.Add("Optimize environmental conditions");
            }
            else if (healthPercentage < 0.6f)
            {
                recommendations.Add("Monitor plant closely");
                recommendations.Add("Address active stress factors");
            }

            if (_stressLevel > 0.6f)
            {
                recommendations.Add("High stress detected - investigate causes");
            }

            if (_activeStressFactors.Count > 3)
            {
                recommendations.Add("Multiple stress factors active - prioritize mitigation");
            }

            return recommendations;
        }

        /// <summary>
        /// Reset health system (for testing or plant revival)
        /// </summary>
        public void ResetHealth(float healthPercentage = 1f)
        {
            _currentHealth = _maxHealth * Mathf.Clamp01(healthPercentage);
            _stressLevel = 0f;
            _activeStressFactors.Clear();
            _healthHistory.Clear();

            RecordHealthEvent("Health System Reset", _currentHealth);

            ChimeraLogger.Log($"[PlantHealthSystem] Health reset to {healthPercentage:P1}");
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Health status classification
    /// </summary>
    public enum HealthStatus
    {
        Critical,
        Poor,
        Fair,
        Good,
        Excellent
    }

    /// <summary>
    /// Health trend analysis
    /// </summary>
    public enum HealthTrend
    {
        Declining,
        Stable,
        Improving
    }


    #endregion
}
