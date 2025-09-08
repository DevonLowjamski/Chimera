using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Environment;
using ProjectChimera.Data.Shared;
using LightingSchedule = ProjectChimera.Data.Simulation.LightingSchedule;
using System.Collections.Generic;
// Explicit alias to resolve LightSpectrum ambiguity - use class from Environment namespace
using LightSpectrum = ProjectChimera.Data.Environment.LightSpectrum;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Controls grow lighting systems with spectrum management and automation.
    /// Supports LED, HPS, CMH, and other grow light types with customizable schedules.
    /// </summary>
    public class LightingController : MonoBehaviour, ITickable
    {
        [Header("Lighting Configuration")]
        [SerializeField] private string _systemId;
        [SerializeField] private LightingType _lightType = LightingType.LED;
        [SerializeField] private float _maxPowerConsumption = 600f; // Watts
        [SerializeField] private float _maxPPFD = 1500f; // μmol/m²/s
        [SerializeField] private int _lightCount = 4;

        [Header("Light Control")]
        [SerializeField] private bool _enableLighting = true;
        [SerializeField] private float _currentIntensity = 1f; // 0-1
        [SerializeField] private LightSpectrum _currentSpectrum;
        [SerializeField] private bool _enableAutomation = true;
        [SerializeField] private bool _enableSchedule = true;

        [Header("Automation Settings")]
        [SerializeField] private float _scheduleUpdateInterval = 60f; // seconds
        [SerializeField] private LightingSchedule _lightingSchedule;
        [SerializeField] private bool _adaptToPlantStage = true;

        [Header("Unity Lights")]
        [SerializeField] private Light[] _unityLights;
        [SerializeField] private Renderer[] _lightFixtureRenderers;
        [SerializeField] private ParticleSystem[] _lightEffects;
        [SerializeField] private AudioSource _ballastAudio;

        [Header("Visual Effects")]
        [SerializeField] private Material _activeLightMaterial;
        [SerializeField] private Material _inactiveLightMaterial;
        [SerializeField] private Color _ledSpectrumColor = Color.white;
        [SerializeField] private Color _hpsSpectrumColor = new Color(1f, 0.7f, 0.3f);
        [SerializeField] private Color _cmhSpectrumColor = new Color(0.9f, 0.9f, 1f);

        // System State
        private bool _isOperational = true;
        private bool _isAutomated = false;
        private float _currentPowerConsumption = 0f;
        private float _currentPPFD = 0f;
        private float _operatingHours = 0f;
        private float _lastScheduleUpdate = 0f;

        // Performance Tracking
        private float _totalEnergyUsed = 0f; // kWh
        private int _onOffCycles = 0;
        private float _avgDailyLightIntegral = 0f; // mol/m²/day

        // Schedule Management
        private bool _isLightPeriod = true;
        private float _currentPhotoperiod = 18f; // hours
        private System.DateTime _lastCycleTime = System.DateTime.Now;

        // Events
        public System.Action<LightingController> OnLightingStateChanged;
        public System.Action<float> OnIntensityChanged;
        public System.Action<LightSpectrum> OnSpectrumChanged;
        public System.Action<float> OnPowerConsumptionChanged;

        // Properties
        public string SystemId => string.IsNullOrEmpty(_systemId) ? name : _systemId;
        public LightingType Type => _lightType;
        public bool IsOperational => _isOperational;
        public bool IsAutomated => _isAutomated;
        public bool IsLightingEnabled => _enableLighting && _isOperational;
        public float CurrentIntensity => _currentIntensity;
        public float CurrentPPFD => _currentPPFD;
        public float PowerConsumption => _currentPowerConsumption;
        public LightSpectrum CurrentSpectrum => _currentSpectrum;
        public bool IsLightPeriod => _isLightPeriod;
        public float Photoperiod => _currentPhotoperiod;
        public float OperatingHours => _operatingHours;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_systemId))
                _systemId = System.Guid.NewGuid().ToString();

            InitializeDefaultSpectrum();
        }

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeSystem();

            if (_enableAutomation)
                SetAutomationMode(true);
        }

        public void Tick(float deltaTime)
        {
            if (!_isOperational) return;

            // Update operating hours
            if (_enableLighting)
                _operatingHours += deltaTime / 3600f;

            // Update schedule
            if (_enableSchedule && Time.time - _lastScheduleUpdate >= _scheduleUpdateInterval)
            {
                UpdateLightingSchedule();
                _lastScheduleUpdate = Time.time;
            }

            // Update power consumption and PPFD
            UpdateLightingMetrics();

            // Update visual effects
            UpdateVisualEffects();
        }

        private void InitializeSystem()
        {
            // Initialize Unity lights if not set
            if (_unityLights == null || _unityLights.Length == 0)
            {
                _unityLights = GetComponentsInChildren<Light>();
            }

            // Initialize light fixture renderers
            if (_lightFixtureRenderers == null || _lightFixtureRenderers.Length == 0)
            {
                _lightFixtureRenderers = GetComponentsInChildren<Renderer>();
            }

            // Initialize default lighting schedule
            if (_lightingSchedule == null)
            {
                CreateDefaultSchedule();
            }

            // Set up audio
            if (_ballastAudio != null)
            {
                _ballastAudio.loop = true;
                _ballastAudio.volume = 0.1f;
            }

            UpdateLightingState();
            ChimeraLogger.Log($"Lighting System {SystemId} initialized - Type: {_lightType}");
        }

        private void InitializeDefaultSpectrum()
        {
            _currentSpectrum = new LightSpectrum
            {
                Red = 0.3f,
                Blue = 0.25f,
                Green = 0.2f,
                FarRed = 0.1f,
                UVA = 0.05f,
                UVB = 0.02f,
                WhiteBalance = 0.08f
            };
        }

        #region Public Control Methods

        /// <summary>
        /// Enable or disable lighting
        /// </summary>
        public void SetLightingEnabled(bool enabled)
        {
            bool wasEnabled = _enableLighting;
            _enableLighting = enabled && _isOperational;

            if (wasEnabled != _enableLighting)
            {
                if (_enableLighting)
                    _onOffCycles++;

                UpdateLightingState();
                OnLightingStateChanged?.Invoke(this);

                ChimeraLogger.Log($"Lighting {SystemId} {(_enableLighting ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Set light intensity (0-1)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            float newIntensity = Mathf.Clamp01(intensity);

            if (Mathf.Abs(_currentIntensity - newIntensity) > 0.01f)
            {
                _currentIntensity = newIntensity;
                UpdateLightingState();
                OnIntensityChanged?.Invoke(_currentIntensity);

                ChimeraLogger.Log($"Lighting {SystemId} intensity set to {_currentIntensity:P0}");
            }
        }

        /// <summary>
        /// Set target light intensity (alias for SetIntensity for compatibility)
        /// </summary>
        public void SetTargetIntensity(float intensity)
        {
            SetIntensity(intensity);
        }

        /// <summary>
        /// Set light spectrum
        /// </summary>
        public void SetSpectrum(LightSpectrum spectrum)
        {
            _currentSpectrum = spectrum;
            UpdateLightingState();
            OnSpectrumChanged?.Invoke(_currentSpectrum);

            ChimeraLogger.Log($"Lighting {SystemId} spectrum updated");
        }

        /// <summary>
        /// Set photoperiod for light schedule
        /// </summary>
        public void SetPhotoperiod(float hours)
        {
            _currentPhotoperiod = Mathf.Clamp(hours, 8f, 24f);

            if (_lightingSchedule != null)
            {
                UpdateScheduleFromPhotoperiod();
            }

            ChimeraLogger.Log($"Lighting {SystemId} photoperiod set to {_currentPhotoperiod} hours");
        }

        /// <summary>
        /// Enable or disable automation
        /// </summary>
        public void SetAutomationMode(bool enabled)
        {
            _isAutomated = enabled;

            if (enabled && _enableSchedule)
            {
                UpdateLightingSchedule();
            }

            OnLightingStateChanged?.Invoke(this);
            ChimeraLogger.Log($"Lighting {SystemId} automation {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Set lighting schedule
        /// </summary>
        public void SetLightingSchedule(LightingSchedule schedule)
        {
            _lightingSchedule = schedule;

            if (_isAutomated && _enableSchedule)
            {
                UpdateLightingSchedule();
            }

            ChimeraLogger.Log($"Lighting {SystemId} schedule updated");
        }

        /// <summary>
        /// System startup
        /// </summary>
        public void Startup()
        {
            _isOperational = true;

            if (_enableAutomation)
                UpdateLightingSchedule();
            else
                UpdateLightingState();

            OnLightingStateChanged?.Invoke(this);
            ChimeraLogger.Log($"Lighting {SystemId} started up");
        }

        /// <summary>
        /// System shutdown
        /// </summary>
        public void Shutdown()
        {
            _isOperational = false;
            _enableLighting = false;

            UpdateLightingState();
            OnLightingStateChanged?.Invoke(this);

            ChimeraLogger.Log($"Lighting {SystemId} shut down");
        }

        /// <summary>
        /// Set light schedule (alias for SetLightingSchedule for compatibility)
        /// </summary>
        public void SetLightSchedule(LightingSchedule schedule)
        {
            SetLightingSchedule(schedule);
        }

        /// <summary>
        /// Set light schedule from FacilityData.LightSchedule (conversion overload)
        /// </summary>
        public void SetLightSchedule(ProjectChimera.Data.Facilities.LightSchedule facilitySchedule)
        {
            if (facilitySchedule == null)
            {
                ChimeraLogger.LogWarning($"[Chimera] LightingController '{SystemId}' received null facility schedule.");
                return;
            }

            // Convert FacilityData.LightSchedule to LightingSchedule
            var lightingSchedule = new LightingSchedule
            {
                Name = facilitySchedule.ScheduleName ?? "Converted Schedule",
                ScheduleEntries = new List<LightingScheduleEntry>()
            };

            // Convert simple facility schedule to detailed schedule entries
            // The facility schedule has simple on/off times, so create one entry
            lightingSchedule.ScheduleEntries.Add(new LightingScheduleEntry
            {
                StartTime = facilitySchedule.LightStartTime,
                EndTime = facilitySchedule.LightStartTime.Add(System.TimeSpan.FromHours(facilitySchedule.LightPeriodHours)),
                Intensity = 1f,
                Spectrum = _currentSpectrum
            });

            SetLightingSchedule(lightingSchedule);
        }

        /// <summary>
        /// Update system - called by GrowRoomController
        /// </summary>
        public void UpdateSystem()
        {
            if (!_isOperational) return;

            // Update schedule if automation is enabled
            if (_isAutomated && _enableSchedule)
            {
                UpdateLightingSchedule();
            }

            // Update lighting metrics
            UpdateLightingMetrics();

            // Update visual effects
            UpdateVisualEffects();

            // Update lighting state
            UpdateLightingState();
        }

        /// <summary>
        /// Check if lights should be on based on schedule
        /// </summary>
        public bool ShouldLightBeOn(ProjectChimera.Data.Facilities.LightSchedule schedule)
        {
            if (schedule == null || !schedule.IsActive) return false;

            var currentTime = System.DateTime.Now.TimeOfDay;
            return currentTime >= schedule.OnTime && currentTime <= schedule.OffTime;
        }

        /// <summary>
        /// Check if lights are currently on
        /// </summary>
        public bool IsLightOn => _enableLighting && _isOperational;

        /// <summary>
        /// Turn on lights
        /// </summary>
        public void TurnOnLights()
        {
            SetLightingEnabled(true);
        }

        /// <summary>
        /// Turn off lights
        /// </summary>
        public void TurnOffLights()
        {
            SetLightingEnabled(false);
        }

        /// <summary>
        /// Make preemptive intensity adjustment
        /// </summary>
        public void MakePreemptiveIntensityAdjustment(float targetIntensity)
        {
            SetIntensity(Mathf.Clamp01(targetIntensity));
        }

        /// <summary>
        /// Set power efficiency mode
        /// </summary>
        public void SetPowerEfficiencyMode(bool enabled)
        {
            if (enabled)
            {
                // Reduce intensity slightly for power savings
                _currentIntensity = Mathf.Max(0.7f, _currentIntensity * 0.9f);
            }
            else
            {
                _currentIntensity = 1.0f; // Full intensity
            }
        }

        #endregion

        #region Schedule Management

        private void UpdateLightingSchedule()
        {
            if (!_isAutomated || !_enableSchedule || _lightingSchedule == null) return;

            var currentTime = System.DateTime.Now;
            bool shouldBeLightPeriod = IsCurrentlyLightPeriod(currentTime);

            if (shouldBeLightPeriod != _isLightPeriod)
            {
                _isLightPeriod = shouldBeLightPeriod;
                _lastCycleTime = currentTime;

                SetLightingEnabled(_isLightPeriod);

                ChimeraLogger.Log($"Lighting {SystemId} schedule: {(_isLightPeriod ? "Light" : "Dark")} period started");
            }

            // Adjust intensity based on schedule
            if (_isLightPeriod && _enableLighting)
            {
                float scheduleIntensity = GetScheduledIntensity(currentTime);
                SetIntensity(scheduleIntensity);
            }
        }

        private bool IsCurrentlyLightPeriod(System.DateTime currentTime)
        {
            if (_lightingSchedule?.ScheduleEntries == null || _lightingSchedule.ScheduleEntries.Count == 0)
            {
                // Fallback to simple photoperiod calculation
                var timeOfDay = currentTime.TimeOfDay;
                var lightStartTime = System.TimeSpan.FromHours(6); // 6 AM default
                var lightEndTime = lightStartTime.Add(System.TimeSpan.FromHours(_currentPhotoperiod));

                return timeOfDay >= lightStartTime && timeOfDay <= lightEndTime;
            }

            // Use detailed schedule
            var currentTimeSpan = currentTime.TimeOfDay;
            foreach (var entry in _lightingSchedule.ScheduleEntries)
            {
                if (currentTimeSpan >= entry.StartTime && currentTimeSpan <= entry.EndTime)
                {
                    return entry.Intensity > 0f;
                }
            }

            return false; // Default to dark period if no schedule entry matches
        }

        private float GetScheduledIntensity(System.DateTime currentTime)
        {
            if (_lightingSchedule?.ScheduleEntries == null || _lightingSchedule.ScheduleEntries.Count == 0)
                return 1f;

            var currentTimeSpan = currentTime.TimeOfDay;
            foreach (var entry in _lightingSchedule.ScheduleEntries)
            {
                if (currentTimeSpan >= entry.StartTime && currentTimeSpan <= entry.EndTime)
                {
                    return entry.Intensity;
                }
            }

            return 0f;
        }

        private void CreateDefaultSchedule()
        {
            _lightingSchedule = new LightingSchedule
            {
                Name = "Default Vegetative Schedule",
                ScheduleEntries = new List<LightingScheduleEntry>
                {
                    new LightingScheduleEntry
                    {
                        StartTime = System.TimeSpan.FromHours(6), // 6 AM
                        EndTime = System.TimeSpan.FromHours(24), // Midnight (18 hour photoperiod)
                        Intensity = 1f,
                        Spectrum = _currentSpectrum
                    }
                }
            };
        }

        private void UpdateScheduleFromPhotoperiod()
        {
            if (_lightingSchedule?.ScheduleEntries != null && _lightingSchedule.ScheduleEntries.Count > 0)
            {
                var entry = _lightingSchedule.ScheduleEntries[0];
                entry.EndTime = entry.StartTime.Add(System.TimeSpan.FromHours(_currentPhotoperiod));
            }
        }

        #endregion

        #region System Updates

        private void UpdateLightingState()
        {
            bool lightsOn = _enableLighting && _isOperational;
            float effectiveIntensity = lightsOn ? _currentIntensity : 0f;

            // Update Unity lights
            if (_unityLights != null)
            {
                foreach (var light in _unityLights)
                {
                    if (light != null)
                    {
                        light.enabled = lightsOn;
                        light.intensity = GetBaseLightIntensity() * effectiveIntensity;
                        light.color = GetSpectrumColor();
                    }
                }
            }

            // Update fixture materials
            UpdateFixtureMaterials(lightsOn);

            // Update audio
            UpdateAudioEffects(lightsOn);
        }

        private void UpdateLightingMetrics()
        {
            // Calculate current PPFD
            if (_enableLighting && _isOperational)
            {
                _currentPPFD = _maxPPFD * _currentIntensity;
            }
            else
            {
                _currentPPFD = 0f;
            }

            // Calculate power consumption
            float previousPower = _currentPowerConsumption;

            if (_enableLighting && _isOperational)
            {
                _currentPowerConsumption = _maxPowerConsumption * _currentIntensity;

                // Add ballast overhead
                _currentPowerConsumption += GetBallastOverhead();
            }
            else
            {
                _currentPowerConsumption = 0f;
            }

            // Track energy usage
            float deltaTime = Time.deltaTime / 3600f; // Convert to hours
            _totalEnergyUsed += _currentPowerConsumption * deltaTime / 1000f; // Convert to kWh

            // Notify if power consumption changed significantly
            if (Mathf.Abs(_currentPowerConsumption - previousPower) > 10f)
            {
                OnPowerConsumptionChanged?.Invoke(_currentPowerConsumption);
            }
        }

        private void UpdateVisualEffects()
        {
            // Update light effects
            if (_lightEffects != null)
            {
                foreach (var effect in _lightEffects)
                {
                    if (effect != null)
                    {
                        if (_enableLighting && _isOperational && !effect.isPlaying)
                        {
                            effect.Play();
                        }
                        else if ((!_enableLighting || !_isOperational) && effect.isPlaying)
                        {
                            effect.Stop();
                        }

                        // Adjust effect intensity
                        if (effect.isPlaying)
                        {
                            var main = effect.main;
                            main.startColor = GetSpectrumColor() * _currentIntensity;
                        }
                    }
                }
            }
        }

        #endregion

        #region Utility Methods

        private float GetBaseLightIntensity()
        {
            return _lightType switch
            {
                LightingType.LED => 2f,
                LightingType.HPS => 3f,
                LightingType.CMH => 2.5f,
                LightingType.Fluorescent => 1f,
                LightingType.Halogen => 1.5f,
                _ => 2f
            };
        }

        private Color GetSpectrumColor()
        {
            if (_currentSpectrum == null)
                return GetDefaultSpectrumColor();

            // Blend spectrum components
            Color color = Color.black;
            color += Color.red * _currentSpectrum.Red;
            color += Color.blue * _currentSpectrum.Blue;
            color += Color.green * _currentSpectrum.Green;
            color += new Color(0.8f, 0.3f, 0.3f) * _currentSpectrum.FarRed; // Far red
            color += new Color(0.4f, 0.2f, 0.8f) * _currentSpectrum.UVA; // UV-A
            color += Color.white * _currentSpectrum.WhiteBalance;

            // Normalize color manually since Color doesn't have normalized property
            float maxComponent = Mathf.Max(color.r, color.g, color.b, color.a);
            if (maxComponent > 1f)
            {
                return new Color(color.r / maxComponent, color.g / maxComponent, color.b / maxComponent, color.a / maxComponent);
            }
            return color;
        }

        private Color GetDefaultSpectrumColor()
        {
            return _lightType switch
            {
                LightingType.LED => _ledSpectrumColor,
                LightingType.HPS => _hpsSpectrumColor,
                LightingType.CMH => _cmhSpectrumColor,
                LightingType.Fluorescent => Color.white,
                LightingType.Halogen => new Color(1f, 0.8f, 0.6f),
                _ => Color.white
            };
        }

        private float GetBallastOverhead()
        {
            return _lightType switch
            {
                LightingType.LED => _maxPowerConsumption * 0.05f, // 5% overhead
                LightingType.HPS => _maxPowerConsumption * 0.15f, // 15% overhead
                LightingType.CMH => _maxPowerConsumption * 0.10f, // 10% overhead
                LightingType.Fluorescent => _maxPowerConsumption * 0.20f, // 20% overhead
                LightingType.Halogen => _maxPowerConsumption * 0.25f, // 25% overhead
                _ => _maxPowerConsumption * 0.10f
            };
        }

        private void UpdateFixtureMaterials(bool lightsOn)
        {
            if (_lightFixtureRenderers == null) return;

            Material targetMaterial = lightsOn ? _activeLightMaterial : _inactiveLightMaterial;

            if (targetMaterial != null)
            {
                foreach (var renderer in _lightFixtureRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.material = targetMaterial;
                    }
                }
            }
        }

        private void UpdateAudioEffects(bool lightsOn)
        {
            if (_ballastAudio == null) return;

            if (lightsOn && !_ballastAudio.isPlaying)
            {
                _ballastAudio.Play();
            }
            else if (!lightsOn && _ballastAudio.isPlaying)
            {
                _ballastAudio.Stop();
            }
        }

        /// <summary>
        /// Get current lighting status
        /// </summary>
        public LightingStatus GetLightingStatus()
        {
            return new LightingStatus
            {
                SystemId = SystemId,
                Type = _lightType,
                IsOperational = _isOperational,
                IsAutomated = _isAutomated,
                IsLightingEnabled = _enableLighting,
                CurrentIntensity = _currentIntensity,
                CurrentPPFD = _currentPPFD,
                PowerConsumption = _currentPowerConsumption,
                CurrentSpectrum = _currentSpectrum,
                IsLightPeriod = _isLightPeriod,
                Photoperiod = _currentPhotoperiod,
                OperatingHours = _operatingHours,
                TotalEnergyUsed = _totalEnergyUsed,
                OnOffCycles = _onOffCycles,
                LightCount = _lightCount
            };
        }

        #endregion

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

        protected virtual void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    // Supporting data structures
    [System.Serializable]
    public enum LightingType
    {
        LED,
        HPS, // High Pressure Sodium
        CMH, // Ceramic Metal Halide
        Fluorescent,
        Halogen
    }

    [System.Serializable]
    public class LightingStatus
    {
        public string SystemId;
        public LightingType Type;
        public bool IsOperational;
        public bool IsAutomated;
        public bool IsLightingEnabled;
        public float CurrentIntensity;
        public float CurrentPPFD;
        public float PowerConsumption;
        public LightSpectrum CurrentSpectrum;
        public bool IsLightPeriod;
        public float Photoperiod;
        public float OperatingHours;
        public float TotalEnergyUsed;
        public int OnOffCycles;
        public int LightCount;
    }

    [System.Serializable]
    public class LightingScheduleEntry
    {
        public System.TimeSpan StartTime;
        public System.TimeSpan EndTime;
        public float Intensity; // 0-1
        public LightSpectrum Spectrum;
    }

    // Additional schedule entries property for GrowLightAutomationSystem compatibility
    public static class LightingScheduleExtensions
    {
        public static List<LightingScheduleEntry> GetScheduleEntries(this LightingSchedule schedule)
        {
            // Convert the single schedule to a list of entries for compatibility
            return new List<LightingScheduleEntry>();
        }
    }
}
