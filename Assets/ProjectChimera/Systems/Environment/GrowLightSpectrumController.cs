using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Shared;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Manages spectrum control, color transitions, and spectral optimization.
    /// Extracted from AdvancedGrowLightSystem for modular architecture.
    /// Handles spectrum presets, custom configurations, and smooth transitions.
    /// </summary>
    public class GrowLightSpectrumController : MonoBehaviour
    {
        [Header("Spectrum Control Configuration")]
        [SerializeField] private bool _enableSpectrumLogging = true;
        [SerializeField] private float _spectrumTransitionTime = 3f;
        [SerializeField] private AnimationCurve _spectrumTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Spectrum presets
        private Dictionary<SpectrumPreset, SpectrumConfiguration> _spectrumPresets = new Dictionary<SpectrumPreset, SpectrumConfiguration>();

        // Light components
        private Light _primaryLight;
        private Light[] _supplementalLights;

        // Spectrum state
        private SpectrumConfiguration _currentSpectrum;
        private SpectrumConfiguration _targetSpectrum;
        private Coroutine _spectrumTransition;
        private bool _isTransitioning = false;

        // Events
        public System.Action<SpectrumConfiguration> OnSpectrumChanged;
        public System.Action<SpectrumPreset> OnPresetActivated;
        public System.Action<bool> OnSpectrumTransitionStateChanged;

        // Properties
        public SpectrumConfiguration CurrentSpectrum => _currentSpectrum;
        public SpectrumConfiguration TargetSpectrum => _targetSpectrum;
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Initialize spectrum controller with configuration
        /// </summary>
        public void Initialize(Light primaryLight, Light[] supplementalLights)
        {
            _primaryLight = primaryLight;
            _supplementalLights = supplementalLights;

            InitializeSpectrumPresets();

            // Set default spectrum
            _currentSpectrum = _spectrumPresets[SpectrumPreset.Balanced];
            _targetSpectrum = _currentSpectrum;

            ApplySpectrum(_currentSpectrum);

            LogDebug("Grow light spectrum controller initialized");
        }

        #region Spectrum Presets

        /// <summary>
        /// Initialize built-in spectrum presets
        /// </summary>
        private void InitializeSpectrumPresets()
        {
            // Vegetative growth spectrum (blue-heavy)
            _spectrumPresets[SpectrumPreset.Vegetative] = new SpectrumConfiguration
            {
                Red = 0.3f,
                Green = 0.4f,
                Blue = 0.8f,
                FarRed = 0.2f,
                UV = 0.1f,
                Temperature = 6500f,
                Name = "Vegetative Growth"
            };

            // Flowering spectrum (red-heavy)
            _spectrumPresets[SpectrumPreset.Flowering] = new SpectrumConfiguration
            {
                Red = 0.9f,
                Green = 0.3f,
                Blue = 0.4f,
                FarRed = 0.7f,
                UV = 0.2f,
                Temperature = 3000f,
                Name = "Flowering"
            };

            // Balanced full spectrum
            _spectrumPresets[SpectrumPreset.Balanced] = new SpectrumConfiguration
            {
                Red = 0.6f,
                Green = 0.5f,
                Blue = 0.6f,
                FarRed = 0.4f,
                UV = 0.15f,
                Temperature = 4000f,
                Name = "Balanced"
            };

            // High intensity for maximum photosynthesis
            _spectrumPresets[SpectrumPreset.High_Intensity] = new SpectrumConfiguration
            {
                Red = 0.8f,
                Green = 0.6f,
                Blue = 0.7f,
                FarRed = 0.5f,
                UV = 0.3f,
                Temperature = 5000f,
                Name = "High Intensity"
            };

            // Dawn simulation
            _spectrumPresets[SpectrumPreset.Dawn] = new SpectrumConfiguration
            {
                Red = 0.9f,
                Green = 0.7f,
                Blue = 0.3f,
                FarRed = 0.8f,
                UV = 0.05f,
                Temperature = 2700f,
                Name = "Dawn"
            };

            // Midday sun simulation
            _spectrumPresets[SpectrumPreset.Midday] = new SpectrumConfiguration
            {
                Red = 0.7f,
                Green = 0.8f,
                Blue = 0.9f,
                FarRed = 0.4f,
                UV = 0.4f,
                Temperature = 5500f,
                Name = "Midday"
            };
        }

        /// <summary>
        /// Activate a spectrum preset
        /// </summary>
        public void ActivatePreset(SpectrumPreset preset)
        {
            if (_spectrumPresets.TryGetValue(preset, out SpectrumConfiguration spectrum))
            {
                SetSpectrum(spectrum);
                OnPresetActivated?.Invoke(preset);
                LogDebug($"Activated spectrum preset: {preset}");
            }
            else
            {
                LogError($"Unknown spectrum preset: {preset}");
            }
        }

        /// <summary>
        /// Get all available spectrum presets
        /// </summary>
        public Dictionary<SpectrumPreset, SpectrumConfiguration> GetAvailablePresets()
        {
            return new Dictionary<SpectrumPreset, SpectrumConfiguration>(_spectrumPresets);
        }

        #endregion

        #region Spectrum Control

        /// <summary>
        /// Set target spectrum configuration
        /// </summary>
        public void SetSpectrum(SpectrumConfiguration spectrum)
        {
            _targetSpectrum = spectrum;

            if (_spectrumTransition != null)
                StopCoroutine(_spectrumTransition);

            _spectrumTransition = StartCoroutine(TransitionToSpectrum());
        }

        /// <summary>
        /// Set spectrum immediately without transition
        /// </summary>
        public void SetSpectrumImmediate(SpectrumConfiguration spectrum)
        {
            _currentSpectrum = spectrum;
            _targetSpectrum = spectrum;

            if (_spectrumTransition != null)
            {
                StopCoroutine(_spectrumTransition);
                _spectrumTransition = null;
            }

            ApplySpectrum(_currentSpectrum);
            OnSpectrumChanged?.Invoke(_currentSpectrum);

            LogDebug($"Spectrum set immediately: {spectrum.Name}");
        }

        /// <summary>
        /// Smooth transition to target spectrum
        /// </summary>
        private IEnumerator TransitionToSpectrum()
        {
            _isTransitioning = true;
            OnSpectrumTransitionStateChanged?.Invoke(true);

            SpectrumConfiguration startSpectrum = _currentSpectrum;
            float elapsed = 0f;

            while (elapsed < _spectrumTransitionTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _spectrumTransitionTime;
                t = _spectrumTransitionCurve.Evaluate(t);

                _currentSpectrum = LerpSpectrum(startSpectrum, _targetSpectrum, t);
                ApplySpectrum(_currentSpectrum);

                yield return null;
            }

            _currentSpectrum = _targetSpectrum;
            ApplySpectrum(_currentSpectrum);

            _isTransitioning = false;
            OnSpectrumTransitionStateChanged?.Invoke(false);
            OnSpectrumChanged?.Invoke(_currentSpectrum);
            _spectrumTransition = null;

            LogDebug($"Spectrum transition completed: {_currentSpectrum.Name}");
        }

        /// <summary>
        /// Apply spectrum configuration to lights
        /// </summary>
        private void ApplySpectrum(SpectrumConfiguration spectrum)
        {
            // Apply to primary light
            if (_primaryLight != null)
            {
                Color lightColor = CalculateColorFromSpectrum(spectrum);
                _primaryLight.color = lightColor;
                _primaryLight.colorTemperature = spectrum.Temperature;
            }

            // Apply to supplemental lights with variations
            ApplySpectrumToSupplementalLights(spectrum);
        }

        /// <summary>
        /// Apply spectrum to supplemental lights with variations
        /// </summary>
        private void ApplySpectrumToSupplementalLights(SpectrumConfiguration spectrum)
        {
            if (_supplementalLights == null) return;

            for (int i = 0; i < _supplementalLights.Length; i++)
            {
                if (_supplementalLights[i] != null)
                {
                    // Create slight variations for more realistic lighting
                    var modifiedSpectrum = new SpectrumConfiguration
                    {
                        Red = spectrum.Red * Random.Range(0.9f, 1.1f),
                        Green = spectrum.Green * Random.Range(0.9f, 1.1f),
                        Blue = spectrum.Blue * Random.Range(0.9f, 1.1f),
                        FarRed = spectrum.FarRed,
                        UV = spectrum.UV,
                        Temperature = spectrum.Temperature + Random.Range(-200f, 200f),
                        Name = spectrum.Name + $" Variant {i}"
                    };

                    Color lightColor = CalculateColorFromSpectrum(modifiedSpectrum);
                    _supplementalLights[i].color = lightColor;
                    _supplementalLights[i].colorTemperature = modifiedSpectrum.Temperature;
                }
            }
        }

        /// <summary>
        /// Calculate Unity Color from spectrum configuration
        /// </summary>
        private Color CalculateColorFromSpectrum(SpectrumConfiguration spectrum)
        {
            // Convert spectrum values to Unity Color
            return new Color(
                Mathf.Clamp01(spectrum.Red),
                Mathf.Clamp01(spectrum.Green),
                Mathf.Clamp01(spectrum.Blue),
                1f
            );
        }

        /// <summary>
        /// Lerp between two spectrum configurations
        /// </summary>
        private SpectrumConfiguration LerpSpectrum(SpectrumConfiguration from, SpectrumConfiguration to, float t)
        {
            return new SpectrumConfiguration
            {
                Red = Mathf.Lerp(from.Red, to.Red, t),
                Green = Mathf.Lerp(from.Green, to.Green, t),
                Blue = Mathf.Lerp(from.Blue, to.Blue, t),
                FarRed = Mathf.Lerp(from.FarRed, to.FarRed, t),
                UV = Mathf.Lerp(from.UV, to.UV, t),
                Temperature = Mathf.Lerp(from.Temperature, to.Temperature, t),
                Name = $"Transitioning to {to.Name}"
            };
        }

        #endregion

        #region Custom Spectrum Creation

        /// <summary>
        /// Create custom spectrum configuration
        /// </summary>
        public SpectrumConfiguration CreateCustomSpectrum(string name, float red, float green, float blue, float farRed, float uv, float temperature)
        {
            return new SpectrumConfiguration
            {
                Red = Mathf.Clamp01(red),
                Green = Mathf.Clamp01(green),
                Blue = Mathf.Clamp01(blue),
                FarRed = Mathf.Clamp01(farRed),
                UV = Mathf.Clamp01(uv),
                Temperature = Mathf.Clamp(temperature, 1000f, 10000f),
                Name = name
            };
        }

        /// <summary>
        /// Add custom preset to available presets
        /// </summary>
        public void AddCustomPreset(SpectrumPreset presetKey, SpectrumConfiguration spectrum)
        {
            _spectrumPresets[presetKey] = spectrum;
            LogDebug($"Added custom spectrum preset: {presetKey}");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get spectrum effectiveness for plant growth stage
        /// </summary>
        public float GetSpectrumEffectiveness(ProjectChimera.Data.Shared.PlantGrowthStage growthStage)
        {
            switch (growthStage)
            {
                case ProjectChimera.Data.Shared.PlantGrowthStage.Seedling:
                case ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative:
                    // Blue light is more effective for vegetative growth
                    return _currentSpectrum.Blue * 0.4f + _currentSpectrum.Green * 0.3f + _currentSpectrum.Red * 0.2f + _currentSpectrum.UV * 0.1f;

                case ProjectChimera.Data.Shared.PlantGrowthStage.Flowering:
                case ProjectChimera.Data.Shared.PlantGrowthStage.Harvest:
                    // Red light is more effective for flowering
                    return _currentSpectrum.Red * 0.4f + _currentSpectrum.FarRed * 0.3f + _currentSpectrum.Green * 0.2f + _currentSpectrum.Blue * 0.1f;

                default:
                    return (_currentSpectrum.Red + _currentSpectrum.Green + _currentSpectrum.Blue) / 3f;
            }
        }

        /// <summary>
        /// Calculate photosynthetic photon flux density (PPFD) effectiveness
        /// </summary>
        public float CalculatePPFDEffectiveness()
        {
            // PAR region effectiveness (400-700nm)
            float parEffectiveness = (_currentSpectrum.Red * 0.85f) + (_currentSpectrum.Green * 0.75f) + (_currentSpectrum.Blue * 0.85f);

            // Far-red contributes to photosynthesis but less efficiently
            float farRedContribution = _currentSpectrum.FarRed * 0.15f;

            return Mathf.Clamp01(parEffectiveness + farRedContribution);
        }

        /// <summary>
        /// Reset spectrum to balanced preset
        /// </summary>
        public void ResetToDefault()
        {
            ActivatePreset(SpectrumPreset.Balanced);
            LogDebug("Spectrum reset to default (Balanced)");
        }

        #endregion

        private void OnDestroy()
        {
            if (_spectrumTransition != null)
            {
                StopCoroutine(_spectrumTransition);
            }
        }

        private void LogDebug(string message)
        {
            if (_enableSpectrumLogging)
                ChimeraLogger.Log($"[GrowLightSpectrumController] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[GrowLightSpectrumController] {message}");
        }
    }

    /// <summary>
    /// Spectrum configuration data
    /// </summary>
    [System.Serializable]
    public class SpectrumConfiguration
    {
        [Range(0f, 1f)] public float Red = 0.6f;
        [Range(0f, 1f)] public float Green = 0.5f;
        [Range(0f, 1f)] public float Blue = 0.6f;
        [Range(0f, 1f)] public float FarRed = 0.3f;
        [Range(0f, 1f)] public float UV = 0.1f;
        [Range(1000f, 10000f)] public float Temperature = 4000f;
        public string Name = "Custom";
    }

    /// <summary>
    /// Built-in spectrum presets
    /// </summary>
    public enum SpectrumPreset
    {
        Vegetative,
        Flowering,
        Balanced,
        High_Intensity,
        Dawn,
        Midday
    }
}
