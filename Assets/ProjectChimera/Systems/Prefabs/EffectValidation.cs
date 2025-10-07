using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Systems.Prefabs;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// Effect validation, optimization, and performance management system
    /// </summary>
    [CreateAssetMenu(fileName = "Effect Validation System", menuName = "Project Chimera/Effects/Effect Validation")]
    public class EffectValidationSO : ScriptableObject
    {
        [Header("Performance Thresholds")]
        [SerializeField] private float _maxPerformanceCost = 15f;
        [SerializeField] private int _maxConcurrentEffects = 50;
        [SerializeField] private int _maxParticleEffects = 10;
        [SerializeField] private int _maxAudioEffects = 20;

        [Header("Validation Settings")]
        [SerializeField] private bool _enablePerformanceValidation = true;
        [SerializeField] private bool _enableConflictDetection = true;
        [SerializeField] private bool _enableOptimizationSuggestions = true;

        public float MaxPerformanceCost => _maxPerformanceCost;
        public int MaxConcurrentEffects => _maxConcurrentEffects;
        public int MaxParticleEffects => _maxParticleEffects;
        public int MaxAudioEffects => _maxAudioEffects;

        public EffectRecommendation GetEffectRecommendation(string context, EffectCategory category, PerformanceCost maxCost, List<EffectPrefabEntry> availableEffects)
        {
            var filteredEffects = availableEffects
                .Where(e => e.EffectCategory == category && e.PerformanceCost <= maxCost)
                .ToList();

            var recommendation = new EffectRecommendation
            {
                Context = context,
                RecommendedEffects = new List<string>(),
                EstimatedPerformanceImpact = 0f,
                QualityScore = 0f
            };

            // Context-based recommendation logic
            switch (context.ToLower())
            {
                case "plant_interaction":
                    recommendation.RecommendedEffects.AddRange(
                        filteredEffects.Where(e => e.PrefabId.Contains("plant")).Select(e => e.PrefabId)
                    );
                    recommendation.ReasoningExplanation = "Selected plant-related effects for agricultural interaction context";
                    break;

                case "system_feedback":
                    recommendation.RecommendedEffects.AddRange(
                        filteredEffects.Where(e => e.PrefabId.Contains("system") || e.PrefabId.Contains("equipment")).Select(e => e.PrefabId)
                    );
                    recommendation.ReasoningExplanation = "Selected system and equipment effects for technical feedback";
                    break;

                case "environmental":
                    recommendation.RecommendedEffects.AddRange(
                        filteredEffects.Where(e => e.EffectCategory == EffectCategory.Environmental).Select(e => e.PrefabId)
                    );
                    recommendation.ReasoningExplanation = "Selected environmental effects for atmospheric enhancement";
                    break;

                case "ui_interaction":
                    recommendation.RecommendedEffects.AddRange(
                        filteredEffects.Where(e => e.EffectCategory == EffectCategory.UI || e.EffectCategory == EffectCategory.Interaction).Select(e => e.PrefabId)
                    );
                    recommendation.ReasoningExplanation = "Selected UI and interaction effects for user interface feedback";
                    break;

                default:
                    // General recommendation based on performance cost
                    recommendation.RecommendedEffects.AddRange(
                        filteredEffects.OrderBy(e => e.PerformanceCost).Take(3).Select(e => e.PrefabId)
                    );
                    recommendation.ReasoningExplanation = "Selected low-cost effects suitable for general use";
                    break;
            }

            // Calculate performance impact and quality score
            var effectLookup = availableEffects.ToDictionary(e => e.PrefabId, e => e);
            foreach (var effectId in recommendation.RecommendedEffects)
            {
                if (effectLookup.TryGetValue(effectId, out var effect))
                {
                    recommendation.EstimatedPerformanceImpact += (float)effect.PerformanceCost;
                }
            }

            recommendation.QualityScore = Mathf.Clamp01(1f - recommendation.EstimatedPerformanceImpact / 20f);

            return recommendation;
        }

        public EffectValidationResult ValidateEffectConfiguration(List<string> effectIds, List<EffectPrefabEntry> allEffects)
        {
            var result = new EffectValidationResult
            {
                IsValid = true,
                ValidationIssues = new List<string>(),
                PerformanceWarnings = new List<string>(),
                TotalPerformanceCost = 0f
            };

            if (!_enablePerformanceValidation)
                return result;

            var effectLookup = allEffects.ToDictionary(e => e.PrefabId, e => e);

            // Check total performance cost
            foreach (var effectId in effectIds)
            {
                if (effectLookup.TryGetValue(effectId, out var effect))
                {
                    result.TotalPerformanceCost += (float)effect.PerformanceCost;
                }
                else
                {
                    result.ValidationIssues.Add($"Effect '{effectId}' not found in available effects");
                    result.IsValid = false;
                }
            }

            // Performance validation
            if (result.TotalPerformanceCost > _maxPerformanceCost)
            {
                result.PerformanceWarnings.Add($"High total performance cost ({result.TotalPerformanceCost:F1}) may impact frame rate (max: {_maxPerformanceCost})");
            }

            if (effectIds.Count > _maxConcurrentEffects)
            {
                result.ValidationIssues.Add($"Too many concurrent effects ({effectIds.Count} > {_maxConcurrentEffects})");
                result.IsValid = false;
            }

            // Check for conflicting effects
            if (_enableConflictDetection)
            {
                ValidateEffectConflicts(effectIds, effectLookup, result);
            }

            return result;
        }

        private void ValidateEffectConflicts(List<string> effectIds, Dictionary<string, EffectPrefabEntry> effectLookup, EffectValidationResult result)
        {
            var particleEffects = effectIds.Where(id =>
                effectLookup.TryGetValue(id, out var effect) && effect.EffectType == EffectType.Particle
            ).ToList();

            if (particleEffects.Count > _maxParticleEffects)
            {
                result.PerformanceWarnings.Add($"Large number of particle effects ({particleEffects.Count} > {_maxParticleEffects}) may cause performance issues");
            }

            var audioEffects = effectIds.Where(id =>
                effectLookup.TryGetValue(id, out var effect) && effect.EffectType == EffectType.Audio
            ).ToList();

            if (audioEffects.Count > _maxAudioEffects)
            {
                result.PerformanceWarnings.Add($"Large number of audio effects ({audioEffects.Count} > {_maxAudioEffects}) may cause audio mixing issues");
            }

            // Check for duplicate effect types in same category
            var groupedEffects = effectIds
                .Where(id => effectLookup.ContainsKey(id))
                .Select(id => effectLookup[id])
                .GroupBy(e => new { e.EffectType, e.EffectCategory })
                .Where(g => g.Count() > 3)
                .ToList();

            foreach (var group in groupedEffects)
            {
                result.PerformanceWarnings.Add($"Multiple {group.Key.EffectType} effects in {group.Key.EffectCategory} category may cause visual/audio clutter");
            }
        }

        public EffectOptimizationSuggestion OptimizeEffectConfiguration(List<string> effectIds, PerformanceCost targetCost, List<EffectPrefabEntry> allEffects)
        {
            var suggestion = new EffectOptimizationSuggestion
            {
                OriginalEffects = effectIds,
                OptimizedEffects = new List<string>(),
                RemovableEffects = new List<string>(),
                AlternativeEffects = new Dictionary<string, string>(),
                PerformanceGain = 0f
            };

            if (!_enableOptimizationSuggestions)
                return suggestion;

            var effectLookup = allEffects.ToDictionary(e => e.PrefabId, e => e);
            float currentCost = 0f;
            float targetCostValue = (float)targetCost;

            // Calculate current cost and identify optimization opportunities
            foreach (var effectId in effectIds)
            {
                if (effectLookup.TryGetValue(effectId, out var effect))
                {
                    float effectCost = (float)effect.PerformanceCost;
                    currentCost += effectCost;

                    if (effectCost <= targetCostValue)
                    {
                        suggestion.OptimizedEffects.Add(effectId);
                    }
                    else
                    {
                        // Find alternative with lower cost
                        var alternative = FindAlternativeEffect(effect, targetCost, allEffects);
                        if (alternative != null)
                        {
                            suggestion.AlternativeEffects[effectId] = alternative.PrefabId;
                            suggestion.OptimizedEffects.Add(alternative.PrefabId);
                        }
                        else
                        {
                            suggestion.RemovableEffects.Add(effectId);
                        }
                    }
                }
            }

            // Calculate performance gain
            float optimizedCost = 0f;
            foreach (var effectId in suggestion.OptimizedEffects)
            {
                if (effectLookup.TryGetValue(effectId, out var effect))
                {
                    optimizedCost += (float)effect.PerformanceCost;
                }
            }

            suggestion.PerformanceGain = currentCost - optimizedCost;

            return suggestion;
        }

        private EffectPrefabEntry FindAlternativeEffect(EffectPrefabEntry originalEffect, PerformanceCost targetCost, List<EffectPrefabEntry> allEffects)
        {
            return allEffects
                .Where(e => e.EffectType == originalEffect.EffectType &&
                           e.EffectCategory == originalEffect.EffectCategory &&
                           e.PerformanceCost <= targetCost &&
                           e.PrefabId != originalEffect.PrefabId)
                .OrderByDescending(e => e.PerformanceCost) // Get the highest quality within budget
                .FirstOrDefault();
        }

        public EffectsLibraryStats GetLibraryStats(List<EffectPrefabEntry> allEffects)
        {
            var typeLookup = allEffects.GroupBy(e => e.EffectType)
                                     .ToDictionary(g => g.Key, g => g.ToList());

            return new EffectsLibraryStats
            {
                TotalEffects = allEffects.Count,
                TotalSequences = 0, // This would be populated by EffectSequences if needed
                TypeDistribution = typeLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count),
                CategoryDistribution = allEffects.GroupBy(e => e.EffectCategory)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AveragePerformanceCost = allEffects.Any() ? allEffects.Average(e => (float)e.PerformanceCost) : 0f,
                ParticlePoolingEnabled = true, // This would come from EffectSequences
                Audio3DEnabled = true // This would come from EffectSequences
            };
        }

        public bool IsEffectCombinationValid(List<string> effectIds, List<EffectPrefabEntry> allEffects)
        {
            var result = ValidateEffectConfiguration(effectIds, allEffects);
            return result.IsValid;
        }

        public List<string> GetPerformanceWarnings(List<string> effectIds, List<EffectPrefabEntry> allEffects)
        {
            var result = ValidateEffectConfiguration(effectIds, allEffects);
            return result.PerformanceWarnings;
        }
    }

    // NOTE: All validation data structures moved to EffectDataStructures.cs to avoid CS0101 duplicate definitions
    // Use: using ProjectChimera.Systems.Prefabs; to access these types
}
