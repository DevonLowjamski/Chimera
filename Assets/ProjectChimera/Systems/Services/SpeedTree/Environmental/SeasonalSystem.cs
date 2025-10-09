using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using System.Linq;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// REFACTORED: Seasonal System Coordinator for SpeedTree Plants
    /// Single Responsibility: Coordinate seasonal changes and effects through helper classes
    /// Reduced from 542 lines using composition with SeasonalEffectsManager
    /// </summary>
    public class SeasonalSystem : MonoBehaviour
    {
        [Header("Seasonal Configuration")]
        [SerializeField] private bool _enableSeasonalChanges = true;
        [SerializeField] private float _seasonalTransitionDuration = 30f; // Days
        [SerializeField] private float _seasonalUpdateFrequency = 1f; // Updates per day

        // Helper component (Composition pattern for SRP)
        private SeasonalEffectsManager _effectsManager;

        // Seasonal state
        private Season _currentSeason = Season.Spring;
        private float _seasonalTransitionProgress = 0f;
        private float _seasonTimer = 0f;

        // Shader property IDs
        private int _seasonalTintPropertyId;
        private int _seasonalBrightnessPropertyId;
        private int _seasonalContrastPropertyId;

        #region Public Events
        public event Action<Season> OnSeasonChanged;
        public event Action<Season, float> OnSeasonalTransition;
        public event Action<int, Season, float> OnPlantSeasonalEffect;
        #endregion

        #region Initialization
        public void Initialize()
        {
            ChimeraLogger.Log("SPEEDTREE/SEASON", "SeasonalSystem Initialize", this);

            // Initialize helper component
            _effectsManager = new SeasonalEffectsManager();

            // Cache shader properties
            CacheShaderProperties();

            // Start with current season
            _seasonTimer = 0f;
            _seasonalTransitionProgress = 1f; // Start fully transitioned

            ChimeraLogger.Log("SPEEDTREE/SEASON", "SeasonalSystem Initialized", this);
        }

        public void Shutdown()
        {
            ChimeraLogger.Log("SPEEDTREE/SEASON", "SeasonalSystem Shutdown", this);

            _effectsManager?.Clear();

            ChimeraLogger.Log("SPEEDTREE/SEASON", "SeasonalSystem Shutdown Complete", this);
        }

        private void CacheShaderProperties()
        {
            _seasonalTintPropertyId = Shader.PropertyToID("_SeasonalTint");
            _seasonalBrightnessPropertyId = Shader.PropertyToID("_SeasonalBrightness");
            _seasonalContrastPropertyId = Shader.PropertyToID("_SeasonalContrast");
        }

        #endregion

        #region Public Methods

        public void RegisterPlant(int plantId)
        {
            _effectsManager?.RegisterPlant(plantId, _currentSeason);
            ChimeraLogger.Log("SPEEDTREE/SEASON", $"Plant {plantId} registered for seasonal effects", this);
        }

        public void UnregisterPlant(int plantId)
        {
            _effectsManager?.UnregisterPlant(plantId);
            ChimeraLogger.Log("SPEEDTREE/SEASON", $"Plant {plantId} unregistered from seasonal effects", this);
        }

        public void SetSeason(Season season)
        {
            if (_currentSeason != season)
            {
                _currentSeason = season;
                _seasonalTransitionProgress = 0f;
                OnSeasonChanged?.Invoke(_currentSeason);

                ChimeraLogger.Log("SPEEDTREE/SEASON", $"Season changed to {season}", this);
            }
        }

        public Season GetCurrentSeason() => _currentSeason;

        public float GetSeasonalTransitionProgress() => _seasonalTransitionProgress;

        public SeasonalStatistics GetStatistics()
        {
            return new SeasonalStatistics
            {
                CurrentSeason = _currentSeason,
                TransitionProgress = _seasonalTransitionProgress,
                RegisteredPlants = _effectsManager?.PlantCount ?? 0,
                SeasonTimer = _seasonTimer,
                LastUpdate = DateTime.Now
            };
        }

        public bool EnableSeasonalChanges
        {
            get => _enableSeasonalChanges;
            set => _enableSeasonalChanges = value;
        }

        #endregion

        #region Update System

        public void Tick(float deltaTime)
        {
            if (!_enableSeasonalChanges) return;

            _seasonTimer += deltaTime;

            // Update seasonal transition
            if (_seasonalTransitionProgress < 1f)
            {
                _seasonalTransitionProgress += deltaTime / (_seasonalTransitionDuration * 86400f);
                _seasonalTransitionProgress = Mathf.Clamp01(_seasonalTransitionProgress);

                OnSeasonalTransition?.Invoke(_currentSeason, _seasonalTransitionProgress);
            }

            // Apply seasonal effects to plants
            if (_seasonTimer >= 1f / _seasonalUpdateFrequency)
            {
                ApplySeasonalEffectsToPlants();
                _seasonTimer = 0f;
            }
        }

        private void ApplySeasonalEffectsToPlants()
        {
            var seasonalEffect = _effectsManager?.GetSeasonalEffect(_currentSeason);
            if (seasonalEffect == null) return;

            // Get all registered renderers
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var allRenderers = registry?.GetAll<Renderer>();

            if (allRenderers == null || !allRenderers.Any())
            {
                ChimeraLogger.LogWarning("SPEEDTREE/SEASON", "No Renderers found - ensure they are registered with GameObjectRegistry", this);
                return;
            }

            // Filter for SpeedTree renderers
            var renderers = allRenderers
                .Where(r => r.sharedMaterial != null &&
                           r.sharedMaterial.shader != null &&
                           r.sharedMaterial.shader.name.Contains("SpeedTree"));

            foreach (var renderer in renderers)
            {
                ApplySeasonalEffectToRenderer(renderer, seasonalEffect.Value);
            }
        }

        private void ApplySeasonalEffectToRenderer(Renderer renderer, SeasonalEffectProfile effect)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;

            try
            {
                var material = renderer.material;

                // Apply seasonal tint
                if (material.HasProperty(_seasonalTintPropertyId))
                {
                    var currentTint = material.GetColor(_seasonalTintPropertyId);
                    var targetTint = effect.ColorTint;
                    material.SetColor(_seasonalTintPropertyId, 
                        Color.Lerp(currentTint, targetTint, _seasonalTransitionProgress));
                }

                // Apply seasonal brightness
                if (material.HasProperty(_seasonalBrightnessPropertyId))
                {
                    material.SetFloat(_seasonalBrightnessPropertyId, effect.Brightness);
                }

                // Apply seasonal contrast
                if (material.HasProperty(_seasonalContrastPropertyId))
                {
                    material.SetFloat(_seasonalContrastPropertyId, effect.Contrast);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/SEASON", "ApplySeasonalEffectToRenderer exception", this);
            }
        }

        #endregion

        #region Season Cycle Management

        public void AdvanceToNextSeason()
        {
            var nextSeason = _currentSeason switch
            {
                Season.Spring => Season.Summer,
                Season.Summer => Season.Autumn,
                Season.Autumn => Season.Winter,
                Season.Winter => Season.Spring,
                _ => Season.Spring
            };

            SetSeason(nextSeason);
        }

        public SeasonalConditions GetCurrentSeasonalConditions()
        {
            var effect = _effectsManager?.GetSeasonalEffect(_currentSeason);
            if (effect == null)
                return new SeasonalConditions { Season = _currentSeason };

            return new SeasonalConditions
            {
                Season = _currentSeason,
                GrowthMultiplier = effect.Value.GrowthMultiplier,
                HealthMultiplier = effect.Value.HealthMultiplier,
                TransitionProgress = _seasonalTransitionProgress,
                IsTransitioning = _seasonalTransitionProgress < 1f
            };
        }

        #endregion
    }
}
