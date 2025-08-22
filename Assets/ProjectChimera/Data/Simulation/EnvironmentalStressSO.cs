using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using System.Collections.Generic;

namespace ProjectChimera.Data.Simulation
{
    /// <summary>
    /// Defines environmental stress factors and their effects on plant growth and trait expression.
    /// Includes stress thresholds, damage calculations, and recovery mechanisms.
    /// </summary>
    [CreateAssetMenu(fileName = "New Environmental Stress", menuName = "Project Chimera/Environment/Environmental Stress")]
    public class EnvironmentalStressSO : ChimeraDataSO
    {
        [Header("Stress Identity")]
        [SerializeField] private string _stressName;
        [SerializeField] private StressType _stressType = StressType.Temperature;
        [SerializeField, TextArea(3, 5)] private string _stressDescription;
        [SerializeField] private StressSeverity _baseSeverity = StressSeverity.Moderate;
        
        [Header("Stress Thresholds")]
        [SerializeField] private StressThreshold _mildStressThreshold;
        [SerializeField] private StressThreshold _moderateStressThreshold;
        [SerializeField] private StressThreshold _severeStressThreshold;
        [SerializeField] private StressThreshold _lethalStressThreshold;
        
        [Header("Stress Effects")]
        [SerializeField] private List<StressEffect> _immediateEffects = new List<StressEffect>();
        [SerializeField] private List<StressEffect> _cumulativeEffects = new List<StressEffect>();
        [SerializeField] private List<StressEffect> _longTermEffects = new List<StressEffect>();
        
        [Header("Damage Mechanics")]
        [SerializeField] private DamageType _damageType = DamageType.Reversible;
        [SerializeField] private AnimationCurve _damageCurve;
        [SerializeField, Range(0f, 1f)] private float _maxDamagePerHour = 0.1f;
        [SerializeField, Range(0f, 100f)] private float _lethalThreshold = 90f;
        
        [Header("Recovery Mechanisms")]
        [SerializeField] private bool _allowsRecovery = true;
        [SerializeField] private AnimationCurve _recoveryCurve;
        [SerializeField, Range(0f, 1f)] private float _maxRecoveryPerHour = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _permanentDamageRatio = 0.1f;
        
        [Header("Adaptation and Tolerance")]
        [SerializeField] private bool _allowsAdaptation = false;
        [SerializeField, Range(0f, 1f)] private float _adaptationRate = 0.01f;
        [SerializeField, Range(0f, 2f)] private float _maxAdaptationBonus = 0.3f;
        [SerializeField] private List<ToleranceModifier> _geneticToleranceModifiers = new List<ToleranceModifier>();
        
        [Header("Interactive Effects")]
        [SerializeField] private List<StressInteraction> _interactiveStresses = new List<StressInteraction>();
        [SerializeField] private bool _hasCompoundingEffects = true;
        [SerializeField, Range(1f, 3f)] private float _compoundingMultiplier = 1.5f;
        
        // Public Properties
        public string StressName => _stressName;
        public StressType StressType => _stressType;
        public string StressDescription => _stressDescription;
        public StressSeverity BaseSeverity => _baseSeverity;
        public StressThreshold MildStressThreshold => _mildStressThreshold;
        public StressThreshold ModerateStressThreshold => _moderateStressThreshold;
        public StressThreshold SevereStressThreshold => _severeStressThreshold;
        public StressThreshold LethalStressThreshold => _lethalStressThreshold;
        public List<StressEffect> ImmediateEffects => _immediateEffects;
        public List<StressEffect> CumulativeEffects => _cumulativeEffects;
        public List<StressEffect> LongTermEffects => _longTermEffects;
        public DamageType DamageType => _damageType;
        public AnimationCurve DamageCurve => _damageCurve;
        public float MaxDamagePerHour => _maxDamagePerHour;
        public float DamagePerSecond => _maxDamagePerHour / 3600f; // Convert per hour to per second
        public float LethalThreshold => _lethalThreshold;
        public bool AllowsRecovery => _allowsRecovery;
        public AnimationCurve RecoveryCurve => _recoveryCurve;
        public float MaxRecoveryPerHour => _maxRecoveryPerHour;
        public float PermanentDamageRatio => _permanentDamageRatio;
        public bool AllowsAdaptation => _allowsAdaptation;
        public float AdaptationRate => _adaptationRate;
        public float MaxAdaptationBonus => _maxAdaptationBonus;
        public List<ToleranceModifier> GeneticToleranceModifiers => _geneticToleranceModifiers;
        public List<StressInteraction> InteractiveStresses => _interactiveStresses;
        public bool HasCompoundingEffects => _hasCompoundingEffects;
        public float CompoundingMultiplier => _compoundingMultiplier;
        
        /// <summary>
        /// Gets the stress multiplier based on base severity for stress level calculations.
        /// </summary>
        public float StressMultiplier
        {
            get
            {
                switch (_baseSeverity)
                {
                    case StressSeverity.Mild: return 0.25f;
                    case StressSeverity.Moderate: return 0.5f;
                    case StressSeverity.Severe: return 0.75f;
                    case StressSeverity.Lethal: return 1f;
                    default: return 0.5f;
                }
            }
        }
        
        /// <summary>
        /// Evaluates the current stress level based on environmental conditions.
        /// </summary>
        /// <param name="environment">Current environmental conditions</param>
        /// <returns>Stress level from 0 (no stress) to 1 (lethal stress)</returns>
        public float EvaluateStressLevel(EnvironmentalConditions environment)
        {
            float environmentalValue = GetEnvironmentalValue(environment);
            
            if (environmentalValue <= _mildStressThreshold.ThresholdValue)
                return 0f;
            else if (environmentalValue <= _moderateStressThreshold.ThresholdValue)
                return Mathf.InverseLerp(_mildStressThreshold.ThresholdValue, _moderateStressThreshold.ThresholdValue, environmentalValue) * 0.25f;
            else if (environmentalValue <= _severeStressThreshold.ThresholdValue)
                return 0.25f + Mathf.InverseLerp(_moderateStressThreshold.ThresholdValue, _severeStressThreshold.ThresholdValue, environmentalValue) * 0.5f;
            else if (environmentalValue <= _lethalStressThreshold.ThresholdValue)
                return 0.75f + Mathf.InverseLerp(_severeStressThreshold.ThresholdValue, _lethalStressThreshold.ThresholdValue, environmentalValue) * 0.25f;
            else
                return 1f;
        }
        
        /// <summary>
        /// Calculates damage accumulation over time based on stress level.
        /// </summary>
        /// <param name="stressLevel">Current stress level (0-1)</param>
        /// <param name="deltaTime">Time elapsed in hours</param>
        /// <param name="currentDamage">Current damage level</param>
        /// <returns>New damage level</returns>
        public float CalculateDamageAccumulation(float stressLevel, float deltaTime, float currentDamage)
        {
            if (stressLevel <= 0f) return currentDamage;
            
            float damageRate = _damageCurve != null ? _damageCurve.Evaluate(stressLevel) : stressLevel;
            damageRate *= _maxDamagePerHour;
            
            float newDamage = currentDamage + (damageRate * deltaTime);
            return Mathf.Min(100f, newDamage);
        }
        
        /// <summary>
        /// Calculates recovery over time when stress is reduced.
        /// </summary>
        /// <param name="stressLevel">Current stress level (0-1)</param>
        /// <param name="deltaTime">Time elapsed in hours</param>
        /// <param name="currentDamage">Current damage level</param>
        /// <returns>New damage level after recovery</returns>
        public float CalculateRecovery(float stressLevel, float deltaTime, float currentDamage)
        {
            if (!_allowsRecovery || stressLevel > 0.1f || currentDamage <= 0f)
                return currentDamage;
            
            float recoveryRate = _recoveryCurve != null ? _recoveryCurve.Evaluate(1f - stressLevel) : (1f - stressLevel);
            recoveryRate *= _maxRecoveryPerHour;
            
            // Account for permanent damage
            float recoverableDamage = currentDamage * (1f - _permanentDamageRatio);
            float recovery = recoveryRate * deltaTime;
            
            return Mathf.Max(currentDamage * _permanentDamageRatio, currentDamage - recovery);
        }
        
        /// <summary>
        /// Applies stress effects to specific plant traits.
        /// </summary>
        /// <param name="trait">Target trait</param>
        /// <param name="stressLevel">Current stress level</param>
        /// <param name="severity">Stress severity category</param>
        /// <returns>Trait modifier value</returns>
        public float ApplyStressEffect(TraitType trait, float stressLevel, StressSeverity severity)
        {
            var effects = GetEffectsForSeverity(severity);
            var targetEffect = effects.Find(e => e.AffectedTrait == trait);
            
            if (targetEffect == null) return 1f;
            
            float effectStrength = Mathf.Lerp(1f, targetEffect.EffectMagnitude, stressLevel);
            return targetEffect.IsNegativeEffect ? effectStrength : (2f - effectStrength);
        }
        
        /// <summary>
        /// Calculates genetic tolerance modifier for a specific strain.
        /// </summary>
        public float CalculateGeneticTolerance(PlantStrainSO strain)
        {
            // Base tolerance from strain characteristics
            float tolerance = 1f;
            
            // Apply specific genetic modifiers
            foreach (var modifier in _geneticToleranceModifiers)
            {
                if (IsModifierApplicable(modifier, strain))
                {
                    tolerance *= modifier.ToleranceMultiplier;
                }
            }
            
            return tolerance;
        }
        
        private float GetEnvironmentalValue(EnvironmentalConditions environment)
        {
            switch (_stressType)
            {
                case StressType.Temperature:
                    return environment.Temperature;
                case StressType.Humidity:
                    return environment.Humidity;
                case StressType.Light:
                    return environment.LightIntensity;
                case StressType.CO2:
                    return environment.CO2Level;
                default:
                    return 0f;
            }
        }
        
        private List<StressEffect> GetEffectsForSeverity(StressSeverity severity)
        {
            switch (severity)
            {
                case StressSeverity.Mild:
                    return _immediateEffects;
                case StressSeverity.Moderate:
                    return _cumulativeEffects;
                case StressSeverity.Severe:
                case StressSeverity.Lethal:
                    return _longTermEffects;
                default:
                    return _immediateEffects;
            }
        }
        
        private bool IsModifierApplicable(ToleranceModifier modifier, PlantStrainSO strain)
        {
            // Check if strain has the required characteristics for this tolerance modifier
            // This is a simplified check - could be made more sophisticated
            return modifier.ApplicableStrainTypes.Contains(strain.StrainType);
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_stressName))
                Debug.LogError($"Environmental Stress {name}: Stress name cannot be empty");
                
            if (_maxDamagePerHour <= 0f)
                Debug.LogError($"Environmental Stress {name}: Max damage per hour must be positive");
                
            if (_allowsRecovery && _maxRecoveryPerHour <= 0f)
                Debug.LogWarning($"Environmental Stress {name}: Recovery enabled but max recovery rate is zero");
            
            return isValid;
        }
    }
    
    [System.Serializable]
    public class StressThreshold
    {
        public float ThresholdValue;
        public string ThresholdDescription;
    }
    
    [System.Serializable]
    public class StressEffect
    {
        public TraitType AffectedTrait;
        [Range(0f, 2f)] public float EffectMagnitude = 0.8f;
        public bool IsNegativeEffect = true;
        public EffectDuration Duration = EffectDuration.WhileStressed;
        [TextArea(2, 3)] public string EffectDescription;
    }
    
    [System.Serializable]
    public class ToleranceModifier
    {
        public string ModifierName;
        public List<StrainType> ApplicableStrainTypes = new List<StrainType>();
        [Range(0.5f, 2f)] public float ToleranceMultiplier = 1f;
        [TextArea(2, 3)] public string ModifierDescription;
    }
    
    [System.Serializable]
    public class StressInteraction
    {
        public EnvironmentalStressSO InteractingStress;
        public InteractionType InteractionType = InteractionType.Additive;
        [Range(0.5f, 3f)] public float InteractionStrength = 1.5f;
        [TextArea(2, 3)] public string InteractionDescription;
    }
    
    public enum StressType
    {
        Temperature,
        Humidity,
        Light,
        CO2,
        Nutrient,
        Water,
        pH,
        Salinity,
        Toxicity,
        Physical,
        Biotic
    }
    
    public enum StressSeverity
    {
        Mild,
        Moderate,
        Severe,
        Lethal
    }
    
    public enum DamageType
    {
        Reversible,
        PartiallyReversible,
        Permanent,
        Lethal
    }
    
    public enum EffectDuration
    {
        Immediate,
        WhileStressed,
        Cumulative,
        Permanent
    }
    
    public enum InteractionType
    {
        Additive,
        Multiplicative,
        Synergistic,
        Antagonistic,
        Masking
    }
}