using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Simulation;
using LSchedule = ProjectChimera.Data.Simulation.LightingSchedule;
using ProjectChimera.Core.Events;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Advanced Lighting Management System for Project Chimera cannabis cultivation simulation.
    /// Manages spectrum control, DLI calculations, photoperiod automation, and energy-efficient
    /// lighting strategies optimized for cannabis growth phases and cannabinoid production.
    /// </summary>
    public class LightingManager : DIChimeraManager, ITickable
    {
        [Header("Lighting System Configuration")]
        [SerializeField] private LightingSystemSettings _lightingSettings;
        [SerializeField] private float _lightingUpdateFrequency = 1f; // Updates per second
        [SerializeField] private bool _enableAdvancedSpectrum = true;
        [SerializeField] private bool _enableDLIOptimization = true;
        [SerializeField] private bool _enableEnergyOptimization = true;

        [Header("Spectrum Control")]
        [SerializeField] private SpectrumControlSettings _spectrumSettings;
        [SerializeField] private List<LightSpectrumData> _availableSpectrums = new List<LightSpectrumData>();
        [SerializeField] private bool _enableUVSupplementation = true;
        [SerializeField] private bool _enableFarRedControl = true;

        [Header("DLI Management")]
        [SerializeField] private DLISettings _dliSettings;
        [SerializeField] private bool _enableAdaptiveDLI = true;
        [SerializeField] private bool _enableGrowthStageOptimization = true;

        [Header("Photoperiod Control")]
        [SerializeField] private PhotoperiodSettings _photoperiodSettings;
        [SerializeField] private bool _enableAutomaticPhotoperiod = true;
        [SerializeField] private bool _enableSunriseSimulation = true;
        [SerializeField] private bool _enableSunsetSimulation = true;

        [Header("Energy Management")]
        [SerializeField] private LightingEnergySettings _energySettings;
        [SerializeField] private bool _enableDimmingOptimization = true;
        [SerializeField] private bool _enableScheduleOptimization = true;

        [Header("Events")]
        [SerializeField] private SimpleGameEventSO _lightingScheduleEvent;
        [SerializeField] private SimpleGameEventSO _spectrumChangeEvent;
        [SerializeField] private SimpleGameEventSO _dliOptimizationEvent;
        [SerializeField] private SimpleGameEventSO _lightingAlarmEvent;

        // Runtime Data
        private Dictionary<string, LightingZone> _lightingZones;
        private Dictionary<string, ActiveLightingFixture> _activeLights;
        private Dictionary<string, LSchedule> _lightingSchedules;
        private Dictionary<string, DLITracker> _dliTrackers;
        private LightingEnergyTracker _energyTracker;
        private PhotoperiodController _photoperiodController;
        private SpectrumController _spectrumController;
        private EnvironmentManager _environmentalManager;
        private float _lastLightingUpdate;
        private float _currentDLI;

        // Properties
        public Dictionary<string, LightingZone> LightingZones => _lightingZones;
        public PhotoperiodController PhotoperiodController => _photoperiodController;
        public SpectrumController SpectrumController => _spectrumController;
        public float CurrentDLI => _currentDLI;
        public LightingEnergyTracker EnergyTracker => _energyTracker;

        // Events
        public System.Action<string, LSchedule> OnScheduleChanged;
        public System.Action<string, LightSpectrumData> OnSpectrumChanged;
        public System.Action<string, float> OnDLIOptimized;
        public System.Action<string, LightingAlarm> OnLightingAlarm;

        public override ManagerPriority Priority => ManagerPriority.High;

        protected override void OnManagerInitialize()
        {
            _lightingZones = new Dictionary<string, LightingZone>();
            _activeLights = new Dictionary<string, ActiveLightingFixture>();
            _lightingSchedules = new Dictionary<string, LSchedule>();
            _dliTrackers = new Dictionary<string, DLITracker>();
            _energyTracker = new LightingEnergyTracker();
            _photoperiodController = new PhotoperiodController(_photoperiodSettings);
            _spectrumController = new SpectrumController(_spectrumSettings);

            InitializeLightingSettings();
            InitializeDefaultSchedules();

            // Get reference to EnvironmentManager
            _environmentalManager = GameManager.Instance.GetManager<EnvironmentManager>();

            _lastLightingUpdate = Time.time;

            // Register with UpdateOrchestrator for centralized update management
            UpdateOrchestrator.Instance.RegisterTickable(this);

            LogInfo("LightingManager initialized with advanced spectrum and DLI control");
        }

        #region ITickable Implementation

        int ITickable.Priority => TickPriority.EnvironmentalManager;
        public bool Enabled => IsInitialized;

        public void Tick(float deltaTime)
        {
            if (!Enabled) return;

            float currentTime = Time.time;

            // Main lighting update
            if (currentTime - _lastLightingUpdate >= 1f / _lightingUpdateFrequency)
            {
                UpdateLightingSchedules();
                UpdateSpectrumControl();
                UpdateDLICalculations();
                UpdateEnergyOptimization();
                ProcessLightingAlarms();
                UpdatePhotoperiodControl();

                _lastLightingUpdate = currentTime;
            }

            // Continuous monitoring
            MonitorLightingPerformance();
            ProcessAdaptiveLighting();
        }

        #endregion

        /// <summary>
        /// Creates a new lighting zone with comprehensive lighting control.
        /// </summary>
        public string CreateLightingZone(string zoneName, LightingZoneSettings zoneSettings)
        {
            string zoneId = System.Guid.NewGuid().ToString();

            var lightingZone = new LightingZone
            {
                ZoneId = zoneId,
                ZoneName = zoneName,
                ZoneSettings = zoneSettings,
                CurrentSpectrum = new LightSpectrumData(),
                TargetSpectrum = new LightSpectrumData(),
                LightingFixtures = new List<ActiveLightingFixture>(),
                CurrentDLI = 0f,
                TargetDLI = zoneSettings.DefaultDLI,
                GrowthStage = zoneSettings.InitialGrowthStage,
                PhotoperiodStage = PhotoperiodStage.Day,
                ZoneStatus = LightingZoneStatus.Active,
                AutomaticPhotoperiod = zoneSettings.EnableAutoScheduling,
                CreatedAt = System.DateTime.Now,
                LastUpdated = System.DateTime.Now
            };

            // Initialize default spectrum and schedule
            InitializeZoneDefaults(lightingZone);

            // Create DLI tracker for this zone
            _dliTrackers[zoneId] = new DLITracker();

            _lightingZones[zoneId] = lightingZone;

            LogInfo($"Created lighting zone '{zoneName}' with ID {zoneId}");
            return zoneId;
        }

        /// <summary>
        /// Installs lighting fixture in a specific zone.
        /// </summary>
        public bool InstallLightingFixture(string zoneId, LightingFixtureSO fixture, Vector3 position, Quaternion rotation)
        {
            if (!_lightingZones.TryGetValue(zoneId, out var zone))
            {
                LogWarning($"Lighting zone {zoneId} not found");
                return false;
            }

            var activeFixture = new ActiveLightingFixture
            {
                FixtureId = System.Guid.NewGuid().ToString(),
                FixtureData = fixture,
                ZoneId = zoneId,
                Position = position,
                Rotation = rotation,
                Status = LightingFixtureStatus.Standby,
                Intensity = 0f,
                CurrentSpectrum = new LightSpectrumData(),
                OperatingHours = 0f,
                EfficiencyRating = 1f,
                MaintenanceStatus = LightingMaintenanceStatus.Good,
                InstallationDate = System.DateTime.Now,
                LastMaintenanceDate = System.DateTime.Now
            };

            zone.LightingFixtures.Add(activeFixture);
            _activeLights[activeFixture.FixtureId] = activeFixture;

            LogInfo($"Installed {fixture.EquipmentName} in zone {zone.ZoneName}");
            return true;
        }

        /// <summary>
        /// Sets target DLI for a specific zone with growth stage optimization.
        /// </summary>
        public bool SetZoneTargetDLI(string zoneId, float targetDLI, ProjectChimera.Data.Shared.PlantGrowthStage growthStage = ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative)
        {
            if (!_lightingZones.TryGetValue(zoneId, out var zone))
                return false;

            // Validate DLI range based on growth stage
            float minDLI = GetMinDLIForGrowthStage(growthStage);
            float maxDLI = GetMaxDLIForGrowthStage(growthStage);

            targetDLI = Mathf.Clamp(targetDLI, minDLI, maxDLI);

            zone.TargetDLI = targetDLI;
            zone.GrowthStage = growthStage;
            zone.LastUpdated = System.DateTime.Now;

            // Update lighting schedule to achieve target DLI
            UpdateZoneLightingSchedule(zone);

            OnDLIOptimized?.Invoke(zoneId, targetDLI);
            _dliOptimizationEvent?.Raise();

            LogInfo($"Set target DLI to {targetDLI:F1} mol/m²/day for zone {zone.ZoneName}");
            return true;
        }

        /// <summary>
        /// Sets target spectrum for a zone with cannabinoid optimization.
        /// </summary>
        public bool SetZoneTargetSpectrum(string zoneId, LightSpectrumData targetSpectrum, SpectrumOptimizationTarget target)
        {
            if (!_lightingZones.TryGetValue(zoneId, out var zone))
                return false;

            zone.TargetSpectrum = targetSpectrum;
            zone.SpectrumTarget = target;
            zone.LastUpdated = System.DateTime.Now;

            // Apply spectrum to all fixtures in zone
            ApplySpectrumToZone(zone, targetSpectrum);

            OnSpectrumChanged?.Invoke(zoneId, targetSpectrum);
            _spectrumChangeEvent?.Raise();

            LogInfo($"Applied target spectrum for {target} in zone {zone.ZoneName}");
            return true;
        }

        /// <summary>
        /// Creates custom lighting schedule with photoperiod control.
        /// </summary>
        public bool CreateLightingSchedule(string zoneId, LSchedule schedule)
        {
            if (!_lightingZones.TryGetValue(zoneId, out var zone))
                return false;

            schedule.ZoneId = zoneId;
            schedule.ScheduleId = System.Guid.NewGuid().ToString();
            schedule.CreatedAt = System.DateTime.Now;

            _lightingSchedules[schedule.ScheduleId] = schedule;
            zone.ActiveScheduleId = schedule.ScheduleId;

            OnScheduleChanged?.Invoke(zoneId, schedule);
            _lightingScheduleEvent?.Raise();

            LogInfo($"Created lighting schedule for zone {zone.ZoneName}");
            return true;
        }

        /// <summary>
        /// Optimizes lighting for energy efficiency while maintaining target DLI.
        /// </summary>
        public LightingOptimizationResult OptimizeZoneLighting(string zoneId)
        {
            if (!_lightingZones.TryGetValue(zoneId, out var zone))
                return null;

            var optimization = CalculateOptimalLightingConfiguration(zone);

            // Apply optimization
            ApplyLightingOptimization(zone, optimization);

            LogInfo($"Applied lighting optimization to zone {zone.ZoneName}");
            return optimization;
        }

        /// <summary>
        /// Enables automatic photoperiod progression for flowering induction.
        /// </summary>
        public bool EnableAutomaticPhotoperiod(string zoneId, PhotoperiodProgression progression)
        {
            if (!_lightingZones.TryGetValue(zoneId, out var zone))
                return false;

            zone.PhotoperiodProgression = progression;
            zone.AutomaticPhotoperiod = true;

            _photoperiodController.StartProgression(zoneId, progression);

            LogInfo($"Enabled automatic photoperiod progression for zone {zone.ZoneName}");
            return true;
        }

        /// <summary>
        /// Gets comprehensive lighting status for a zone.
        /// </summary>
        public LightingZoneSnapshot GetZoneLightingSnapshot(string zoneId)
        {
            if (!_lightingZones.TryGetValue(zoneId, out var zone))
                return null;

            var fixtureStatus = zone.LightingFixtures.Select(fixture => new LightingFixtureSnapshot
            {
                FixtureId = fixture.FixtureId,
                FixtureName = fixture.FixtureData?.name ?? "Unknown",
                Status = fixture.Status,
                Intensity = fixture.Intensity,
                CurrentSpectrum = fixture.CurrentSpectrum,
                EnergyConsumption = CalculateFixtureEnergyConsumption(fixture),
                EfficiencyRating = fixture.EfficiencyRating,
                PPFD = CalculateFixturePPFD(fixture),
                MaintenanceStatus = fixture.MaintenanceStatus
            }).ToList();

            return new LightingZoneSnapshot
            {
                ZoneId = zoneId,
                ZoneName = zone.ZoneName,
                Timestamp = System.DateTime.Now,
                CurrentDLI = zone.CurrentDLI,
                TargetDLI = zone.TargetDLI,
                CurrentSpectrum = zone.CurrentSpectrum,
                TargetSpectrum = zone.TargetSpectrum,
                PhotoperiodStage = zone.PhotoperiodStage,
                ZoneStatus = zone.ZoneStatus,
                FixtureStatus = fixtureStatus,
                EnergyEfficiency = CalculateZoneEnergyEfficiency(zone),
                LightUniformity = CalculateZoneLightUniformity(zone),
                SpectralQuality = CalculateSpectralQuality(zone)
            };
        }

        /// <summary>
        /// Calculates current DLI for a specific zone.
        /// </summary>
        public float CalculateZoneDLI(string zoneId)
        {
            if (!_lightingZones.TryGetValue(zoneId, out var zone))
                return 0f;

            if (!_dliTrackers.TryGetValue(zoneId, out var tracker))
                return 0f;

            float totalPPFD = 0f;

            foreach (var fixture in zone.LightingFixtures.Where(f => f.Status == LightingFixtureStatus.On))
            {
                totalPPFD += CalculateFixturePPFD(fixture);
            }

            // Convert PPFD to DLI (μmol/m²/s to mol/m²/day)
            float photoperiodHours = GetCurrentPhotoperiodHours(zone);
            float dli = (totalPPFD * photoperiodHours * 3600f) / 1000000f;

            tracker.RecordDLI(dli, System.DateTime.Now);
            zone.CurrentDLI = dli;

            return dli;
        }

        /// <summary>
        /// Gets energy consumption report for lighting systems.
        /// </summary>
        public LightingEnergyReport GetLightingEnergyReport(string zoneId = null)
        {
            var report = new LightingEnergyReport
            {
                ReportDate = System.DateTime.Now,
                ReportingPeriod = TimeSpan.FromDays(1),
                ZoneReports = new List<ZoneLightingEnergyReport>()
            };

            var zonesToReport = string.IsNullOrEmpty(zoneId) ?
                _lightingZones.Values :
                _lightingZones.Values.Where(z => z.ZoneId == zoneId);

            foreach (var zone in zonesToReport)
            {
                var zoneReport = new ZoneLightingEnergyReport
                {
                    ZoneId = zone.ZoneId,
                    ZoneName = zone.ZoneName,
                    TotalEnergyConsumption = CalculateZoneTotalEnergyConsumption(zone),
                    FixtureConsumption = zone.LightingFixtures.ToDictionary(
                        fixture => fixture.FixtureId,
                        fixture => CalculateFixtureEnergyConsumption(fixture)
                    ),
                    EnergyEfficiency = CalculateZoneEnergyEfficiency(zone),
                    DLIEfficiency = zone.CurrentDLI / CalculateZoneTotalEnergyConsumption(zone),
                    OptimizationOpportunities = IdentifyLightingOptimizationOpportunities(zone)
                };

                report.ZoneReports.Add(zoneReport);
            }

            report.TotalEnergyConsumption = report.ZoneReports.Sum(zr => zr.TotalEnergyConsumption);
            report.AverageEfficiency = report.ZoneReports.Average(zr => zr.EnergyEfficiency);

            return report;
        }

        private void InitializeLightingSettings()
        {
            if (_lightingSettings == null)
            {
                _lightingSettings = new LightingSystemSettings
                {
                    DefaultDLI = 35f,
                    MinDLI = 15f,
                    MaxDLI = 65f,
                    DefaultPhotoperiod = 18f,
                    EnableUVWarnings = true,
                    MaxFixturesPerZone = 20,
                    EnergyOptimizationInterval = 24f
                };
            }
        }

        private void InitializeDefaultSchedules()
        {
            // Create default vegetative schedule
            var vegetativeSchedule = CreateDefaultVegetativeSchedule();

            // Create default flowering schedule
            var floweringSchedule = CreateDefaultFloweringSchedule();
        }

        private void InitializeZoneDefaults(LightingZone zone)
        {
            // Set default spectrum based on zone settings
            zone.CurrentSpectrum = GetDefaultSpectrumForGrowthStage(zone.GrowthStage);
            zone.TargetSpectrum = zone.CurrentSpectrum;

            // Create default schedule
            var defaultSchedule = CreateDefaultScheduleForZone(zone);
            // Ensure ZoneId is set for schedule, and use its ScheduleId if provided
            defaultSchedule.ZoneId = zone.ZoneId;
            var scheduleId = string.IsNullOrEmpty(defaultSchedule.ScheduleId) ? System.Guid.NewGuid().ToString() : defaultSchedule.ScheduleId;
            defaultSchedule.ScheduleId = scheduleId;
            _lightingSchedules[scheduleId] = defaultSchedule;
            zone.ActiveScheduleId = scheduleId;
        }

        private void UpdateLightingSchedules()
        {
            foreach (var zone in _lightingZones.Values.Where(z => z.ZoneStatus == LightingZoneStatus.Active))
            {
                if (_lightingSchedules.TryGetValue(zone.ActiveScheduleId, out var schedule))
                {
                    ProcessZoneSchedule(zone, schedule);
                }
            }
        }

        private void UpdateSpectrumControl()
        {
            foreach (var zone in _lightingZones.Values.Where(z => z.ZoneStatus == LightingZoneStatus.Active))
            {
                UpdateZoneSpectrum(zone);
            }
        }

        private void UpdateDLICalculations()
        {
            foreach (var zone in _lightingZones.Values.Where(z => z.ZoneStatus == LightingZoneStatus.Active))
            {
                CalculateZoneDLI(zone.ZoneId);

                if (_enableAdaptiveDLI)
                {
                    ProcessAdaptiveDLIControl(zone);
                }
            }
        }

        private void UpdateEnergyOptimization()
        {
            if (!_enableEnergyOptimization) return;

            foreach (var zone in _lightingZones.Values.Where(z => z.ZoneStatus == LightingZoneStatus.Active))
            {
                ProcessEnergyOptimization(zone);
            }
        }

        private void ProcessLightingAlarms()
        {
            foreach (var zone in _lightingZones.Values)
            {
                CheckZoneLightingAlarms(zone);
            }
        }

        private void UpdatePhotoperiodControl()
        {
            if (!_enableAutomaticPhotoperiod) return;

            foreach (var zone in _lightingZones.Values.Where(z => z.AutomaticPhotoperiod))
            {
                _photoperiodController.UpdateProgression(zone.ZoneId, zone);
            }
        }

        private void MonitorLightingPerformance()
        {
            var keysToUpdate = _activeLights.Keys.ToList();
            foreach (var fixtureId in keysToUpdate)
            {
                var fixture = _activeLights[fixtureId];
                if (fixture.Status != LightingFixtureStatus.On) continue;

                // Update operating hours
                fixture.OperatingHours += Time.deltaTime / 3600f;

                // Update the struct in the dictionary
                _activeLights[fixtureId] = fixture;

                // Monitor efficiency degradation
                UpdateFixtureEfficiency(fixture);

                // Check for maintenance needs
                CheckFixtureMaintenanceNeeds(fixture);
            }
        }

        private void ProcessAdaptiveLighting()
        {
            foreach (var zone in _lightingZones.Values.Where(z => z.ZoneStatus == LightingZoneStatus.Active))
            {
                ProcessAdaptiveLightingControl(zone);
            }
        }

        // Helper methods for lighting calculations
        private float GetMinDLIForGrowthStage(ProjectChimera.Data.Shared.PlantGrowthStage growthStage)
        {
            return growthStage switch
            {
                ProjectChimera.Data.Shared.PlantGrowthStage.Seedling => 15f,
                ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative => 25f,
                ProjectChimera.Data.Shared.PlantGrowthStage.Flowering => 35f,
                _ => 20f
            };
        }

        private float GetMaxDLIForGrowthStage(ProjectChimera.Data.Shared.PlantGrowthStage growthStage)
        {
            return growthStage switch
            {
                ProjectChimera.Data.Shared.PlantGrowthStage.Seedling => 25f,
                ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative => 45f,
                ProjectChimera.Data.Shared.PlantGrowthStage.Flowering => 65f,
                _ => 40f
            };
        }

        private float CalculateFixturePPFD(ActiveLightingFixture fixture)
        {
            if (fixture.Status != LightingFixtureStatus.On)
                return 0f;

            // Default PPFD value since FixtureData is a string, not an object
            float basePPFD = 800f; // Default PPFD
            float actualPPFD = basePPFD * fixture.Intensity * fixture.EfficiencyRating;

            return actualPPFD;
        }

        private float CalculateFixtureEnergyConsumption(ActiveLightingFixture fixture)
        {
            if (fixture.Status != LightingFixtureStatus.On)
                return 0f;

            // Default power consumption since FixtureData is a string, not an object
            float basePower = 400f; // Default power consumption in watts
            float actualPower = basePower * fixture.Intensity;

            return actualPower * Time.deltaTime / 3600f; // Convert to kWh
        }

        // Additional helper methods (placeholders for complex calculations)
        private void UpdateZoneLightingSchedule(LightingZone zone) { }
        private void ApplySpectrumToZone(LightingZone zone, LightSpectrumData spectrum) { }
        private LightingOptimizationResult CalculateOptimalLightingConfiguration(LightingZone zone) { return new LightingOptimizationResult(); }
        private void ApplyLightingOptimization(LightingZone zone, LightingOptimizationResult optimization) { }
        private float GetCurrentPhotoperiodHours(LightingZone zone) { return 18f; }
        private float CalculateZoneEnergyEfficiency(LightingZone zone) { return 0.85f; }
        private float CalculateZoneLightUniformity(LightingZone zone) { return 0.9f; }
        private float CalculateSpectralQuality(LightingZone zone) { return 0.8f; }
        private float CalculateZoneTotalEnergyConsumption(LightingZone zone) { return zone.LightingFixtures.Sum(f => CalculateFixtureEnergyConsumption(f)); }
        private List<string> IdentifyLightingOptimizationOpportunities(LightingZone zone) { return new List<string>(); }
        private LightSpectrumData GetDefaultSpectrumForGrowthStage(ProjectChimera.Data.Shared.PlantGrowthStage growthStage) { return new LightSpectrumData(); }
        private LSchedule CreateDefaultScheduleForZone(LightingZone zone) { return new LSchedule { ScheduleId = System.Guid.NewGuid().ToString(), ZoneId = zone.ZoneId }; }
        private LSchedule CreateDefaultVegetativeSchedule() { return new LSchedule(); }
        private LSchedule CreateDefaultFloweringSchedule() { return new LSchedule(); }
        private void ProcessZoneSchedule(LightingZone zone, LSchedule schedule) { }
        private void UpdateZoneSpectrum(LightingZone zone) { }
        private void ProcessAdaptiveDLIControl(LightingZone zone) { }
        private void ProcessEnergyOptimization(LightingZone zone) { }
        private void CheckZoneLightingAlarms(LightingZone zone) { }
        private void UpdateFixtureEfficiency(ActiveLightingFixture fixture) { }
        private void CheckFixtureMaintenanceNeeds(ActiveLightingFixture fixture) { }
        private void ProcessAdaptiveLightingControl(LightingZone zone) { }

        protected override void OnManagerShutdown()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance.UnregisterTickable(this);

            _lightingZones.Clear();
            _activeLights.Clear();
            _lightingSchedules.Clear();
            _dliTrackers.Clear();

            LogInfo("LightingManager shutdown complete");
        }
    }



}
