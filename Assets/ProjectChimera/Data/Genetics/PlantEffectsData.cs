using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Contains effects and medical properties data for cannabis strains including
    /// psychoactive effects, medical applications, and therapeutic benefits.
    /// Separated from PlantStrainSO to follow Single Responsibility Principle.
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Effects Data", menuName = "Project Chimera/Genetics/Plant Effects Data", order = 13)]
    public class PlantEffectsData : ChimeraDataSO
    {
        [Header("Primary Effects")]
        [SerializeField] private EffectsProfile _effectsProfile;

        [Header("Effect Intensity and Duration")]
        [SerializeField, Range(0f, 1f)] private float _onsetSpeed = 0.5f; // How quickly effects manifest
        [SerializeField, Range(0f, 1f)] private float _effectDuration = 0.5f; // How long effects last
        [SerializeField, Range(0f, 1f)] private float _effectIntensity = 0.5f; // Overall potency of effects
        [SerializeField, Range(0f, 1f)] private float _toleranceBuildUp = 0.5f; // How quickly tolerance develops

        [Header("Medical Applications")]
        [SerializeField] private List<MedicalApplication> _medicalApplications = new List<MedicalApplication>();
        [SerializeField] private List<TherapeuticEffect> _therapeuticEffects = new List<TherapeuticEffect>();

        [Header("Medical Efficacy")]
        [SerializeField, Range(0f, 1f)] private float _painReliefEfficacy = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _antiInflammatoryEfficacy = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _anxiolyticEfficacy = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _antiemeticEfficacy = 0.5f; // Anti-nausea
        [SerializeField, Range(0f, 1f)] private float _appetiteStimulationEfficacy = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _sleepAidEfficacy = 0.5f;

        [Header("Side Effects")]
        [SerializeField] private List<SideEffect> _commonSideEffects = new List<SideEffect>();
        [SerializeField, Range(0f, 1f)] private float _dryMouthLikelihood = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _dryEyesLikelihood = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _anxietyLikelihood = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _paranoidLikelihood = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _dizzinessLikelihood = 0.1f;

        [Header("User Experience")]
        [SerializeField] private ConsumptionMethod _preferredConsumption = ConsumptionMethod.Smoking;
        [SerializeField] private ExperienceType _experienceType = ExperienceType.Balanced;
        [SerializeField, Range(0f, 1f)] private float _beginnerFriendliness = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _socialSuitability = 0.7f;
        [SerializeField] private TimeOfDay _optimalUseTime = TimeOfDay.Anytime;

        [Header("Terpene-Effect Correlation")]
        [SerializeField, Range(0f, 1f)] private float _terpeneEffectCorrelation = 0.8f; // How closely effects match terpene profile
        [SerializeField] private bool _entourageEffectActive = true; // Whether cannabinoids work synergistically

        // Public Properties
        public EffectsProfile EffectsProfile => _effectsProfile;

        // Effect Properties
        public float OnsetSpeed => _onsetSpeed;
        public float EffectDuration => _effectDuration;
        public float EffectIntensity => _effectIntensity;
        public float ToleranceBuildUp => _toleranceBuildUp;

        // Medical Applications
        public List<MedicalApplication> MedicalApplications => _medicalApplications;
        public List<TherapeuticEffect> TherapeuticEffects => _therapeuticEffects;

        // Medical Efficacy
        public float PainReliefEfficacy => _painReliefEfficacy;
        public float AntiInflammatoryEfficacy => _antiInflammatoryEfficacy;
        public float AnxiolyticEfficacy => _anxiolyticEfficacy;
        public float AntiemeticEfficacy => _antiemeticEfficacy;
        public float AppetiteStimulationEfficacy => _appetiteStimulationEfficacy;
        public float SleepAidEfficacy => _sleepAidEfficacy;

        // Side Effects
        public List<SideEffect> CommonSideEffects => _commonSideEffects;
        public float DryMouthLikelihood => _dryMouthLikelihood;
        public float DryEyesLikelihood => _dryEyesLikelihood;
        public float AnxietyLikelihood => _anxietyLikelihood;
        public float ParanoidLikelihood => _paranoidLikelihood;
        public float DizzinessLikelihood => _dizzinessLikelihood;

        // User Experience
        public ConsumptionMethod PreferredConsumption => _preferredConsumption;
        public ExperienceType ExperienceType => _experienceType;
        public float BeginnerFriendliness => _beginnerFriendliness;
        public float SocialSuitability => _socialSuitability;
        public TimeOfDay OptimalUseTime => _optimalUseTime;

        // Terpene-Effect Correlation
        public float TerpeneEffectCorrelation => _terpeneEffectCorrelation;
        public bool EntourageEffectActive => _entourageEffectActive;

        /// <summary>
        /// Gets the overall medical value score based on efficacy ratings.
        /// </summary>
        public float GetMedicalValueScore()
        {
            float totalEfficacy = _painReliefEfficacy + _antiInflammatoryEfficacy + _anxiolyticEfficacy +
                                  _antiemeticEfficacy + _appetiteStimulationEfficacy + _sleepAidEfficacy;
            return totalEfficacy / 6f; // Average of all medical efficacies
        }

        /// <summary>
        /// Determines if this strain is suitable for a specific medical condition.
        /// </summary>
        public bool IsSuitableForCondition(MedicalApplication condition)
        {
            if (_medicalApplications.Contains(condition))
            {
                return GetEfficacyForCondition(condition) > 0.6f;
            }
            return false;
        }

        /// <summary>
        /// Gets the efficacy rating for a specific medical condition.
        /// </summary>
        public float GetEfficacyForCondition(MedicalApplication condition)
        {
            return condition switch
            {
                MedicalApplication.PainRelief => _painReliefEfficacy,
                MedicalApplication.InflammationReduction => _antiInflammatoryEfficacy,
                MedicalApplication.AnxietyReduction => _anxiolyticEfficacy,
                MedicalApplication.NauseaReduction => _antiemeticEfficacy,
                MedicalApplication.AppetiteStimulation => _appetiteStimulationEfficacy,
                MedicalApplication.InsomniaeTreatment => _sleepAidEfficacy,
                _ => 0f
            };
        }

        /// <summary>
        /// Calculates the likelihood of experiencing side effects.
        /// </summary>
        public float GetOverallSideEffectRisk()
        {
            float averageRisk = (_dryMouthLikelihood + _dryEyesLikelihood + _anxietyLikelihood +
                               _paranoidLikelihood + _dizzinessLikelihood) / 5f;
            return averageRisk;
        }

        /// <summary>
        /// Determines if this strain is suitable for beginners based on effects and side effects.
        /// </summary>
        public bool IsBeginnerFriendly()
        {
            float sideEffectRisk = GetOverallSideEffectRisk();
            return _beginnerFriendliness > 0.6f && sideEffectRisk < 0.3f && _effectIntensity < 0.8f;
        }

        /// <summary>
        /// Gets a recommended dosage based on user experience level.
        /// </summary>
        public DosageRecommendation GetDosageRecommendation(UserExperienceLevel userLevel)
        {
            float baseIntensity = _effectIntensity;
            float adjustedIntensity = userLevel switch
            {
                UserExperienceLevel.Beginner => baseIntensity * 0.5f,
                UserExperienceLevel.Intermediate => baseIntensity * 0.75f,
                UserExperienceLevel.Experienced => baseIntensity,
                UserExperienceLevel.Expert => baseIntensity * 1.2f,
                _ => baseIntensity
            };

            return new DosageRecommendation
            {
                RecommendedAmount = adjustedIntensity,
                UserLevel = userLevel,
                OnsetTime = _onsetSpeed,
                Duration = _effectDuration,
                Warnings = GetWarningsForUser(userLevel)
            };
        }

        private List<string> GetWarningsForUser(UserExperienceLevel userLevel)
        {
            var warnings = new List<string>();

            if (userLevel == UserExperienceLevel.Beginner)
            {
                if (_effectIntensity > 0.7f)
                    warnings.Add("High potency strain - start with very small amounts");
                if (_anxietyLikelihood > 0.3f)
                    warnings.Add("May cause anxiety in sensitive users");
                if (_paranoidLikelihood > 0.2f)
                    warnings.Add("May cause paranoia - use in comfortable environment");
            }

            if (_toleranceBuildUp > 0.7f)
                warnings.Add("Tolerance builds quickly with regular use");

            return warnings;
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_effectsProfile == null)
            {
                SharedLogger.LogWarning($"[Chimera] PlantEffectsData '{DisplayName}' has no effects profile assigned.");
                isValid = false;
            }

            return isValid;
        }
    }

    /// <summary>
    /// Represents therapeutic effects beyond basic psychoactive effects.
    /// </summary>
    [System.Serializable]
    public struct TherapeuticEffect
    {
        public string EffectName;
        public float Efficacy;
        public string Description;
        public MedicalApplication TargetCondition;
    }

    /// <summary>
    /// Represents potential side effects and their likelihood.
    /// </summary>
    [System.Serializable]
    public struct SideEffect
    {
        public string EffectName;
        public float Likelihood;
        public SideEffectSeverity Severity;
        public string Description;
    }

    /// <summary>
    /// Represents a dosage recommendation for a user.
    /// </summary>
    [System.Serializable]
    public struct DosageRecommendation
    {
        public float RecommendedAmount;
        public UserExperienceLevel UserLevel;
        public float OnsetTime;
        public float Duration;
        public List<string> Warnings;
    }

    /// <summary>
    /// Defines consumption methods for cannabis.
    /// </summary>
    public enum ConsumptionMethod
    {
        Smoking,
        Vaporizing,
        Edibles,
        Tinctures,
        Topicals,
        Concentrates,
        Any
    }

    /// <summary>
    /// Defines the overall experience type.
    /// </summary>
    public enum ExperienceType
    {
        Energizing,
        Relaxing,
        Balanced,
        Cerebral,
        Physical,
        Creative,
        Social,
        Introspective
    }

    /// <summary>
    /// Defines optimal time of day for consumption.
    /// </summary>
    public enum TimeOfDay
    {
        Morning,
        Afternoon,
        Evening,
        Night,
        Anytime
    }

    /// <summary>
    /// Defines user experience levels.
    /// </summary>
    public enum UserExperienceLevel
    {
        Beginner,
        Intermediate,
        Experienced,
        Expert
    }

    /// <summary>
    /// Defines severity levels for side effects.
    /// </summary>
    public enum SideEffectSeverity
    {
        Mild,
        Moderate,
        Severe
    }
}
