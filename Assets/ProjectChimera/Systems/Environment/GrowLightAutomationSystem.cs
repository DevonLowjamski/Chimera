using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Handles automation, scheduling, and timer-based lighting operations.
    /// Extracted from AdvancedGrowLightSystem for modular architecture.
    /// Manages photoperiod cycles, automated schedules, and smart lighting programs.
    /// </summary>
    public class GrowLightAutomationSystem : MonoBehaviour
    {
        [Header("Automation Configuration")]
        [SerializeField] private bool _enableAutomationLogging = true;
        [SerializeField] private bool _automationEnabled = true;
        [SerializeField] private float _scheduleCheckInterval = 60f; // Check every minute
        [SerializeField] private int _maxScheduleHistory = 100;
        
        // Dependencies
        private GrowLightController _lightController;
        private GrowLightSpectrumController _spectrumController;
        private GrowLightPlantOptimizer _plantOptimizer;
        
        // Automation data
        private List<LightingSchedule> _activeSchedules = new List<LightingSchedule>();
        private List<AutomationEvent> _eventHistory = new List<AutomationEvent>();
        private PhotoperiodProgram _currentPhotoperiod;
        private float _lastScheduleCheck = 0f;
        
        // Automation state
        private bool _isInPhotoperiod = false;
        private System.DateTime _currentCycleStartTime;
        private AutomationMode _currentMode = AutomationMode.Schedule;
        
        // Events
        public System.Action<bool> OnPhotoperiodStateChanged;
        public System.Action<LightingSchedule> OnScheduleExecuted;
        public System.Action<AutomationEvent> OnAutomationEvent;
        public System.Action<PhotoperiodProgram> OnPhotoperiodChanged;
        
        // Properties
        public bool AutomationEnabled => _automationEnabled;
        public bool IsInPhotoperiod => _isInPhotoperiod;
        public PhotoperiodProgram CurrentPhotoperiod => _currentPhotoperiod;
        public AutomationMode CurrentMode => _currentMode;
        public int ActiveSchedulesCount => _activeSchedules.Count;
        
        /// <summary>
        /// Initialize automation system with dependencies
        /// </summary>
        public void Initialize(GrowLightController lightController, GrowLightSpectrumController spectrumController, GrowLightPlantOptimizer plantOptimizer)
        {
            _lightController = lightController;
            _spectrumController = spectrumController;
            _plantOptimizer = plantOptimizer;
            
            InitializeDefaultPhotoperiod();
            
            LogDebug("Grow light automation system initialized");
        }
        
        private void Update()
        {
            if (!_automationEnabled) return;
            
            _lastScheduleCheck += Time.deltaTime;
            
            if (_lastScheduleCheck >= _scheduleCheckInterval)
            {
                CheckSchedules();
                UpdatePhotoperiod();
                _lastScheduleCheck = 0f;
            }
        }
        
        #region Photoperiod Management
        
        /// <summary>
        /// Initialize default photoperiod program
        /// </summary>
        private void InitializeDefaultPhotoperiod()
        {
            _currentPhotoperiod = new PhotoperiodProgram
            {
                Name = "Default 18/6",
                LightHours = 18f,
                DarkHours = 6f,
                StartTime = new System.TimeSpan(6, 0, 0), // 6:00 AM
                LightIntensity = 600f,
                DarkIntensity = 0f,
                SpectrumPreset = SpectrumPreset.Balanced,
                IsActive = true
            };
            
            _currentCycleStartTime = System.DateTime.Today.Add(_currentPhotoperiod.StartTime);
            if (_currentCycleStartTime > System.DateTime.Now)
            {
                _currentCycleStartTime = _currentCycleStartTime.AddDays(-1);
            }
        }
        
        /// <summary>
        /// Set photoperiod program
        /// </summary>
        public void SetPhotoperiod(PhotoperiodProgram photoperiod)
        {
            _currentPhotoperiod = photoperiod;
            _currentCycleStartTime = System.DateTime.Today.Add(photoperiod.StartTime);
            
            if (_currentCycleStartTime > System.DateTime.Now)
            {
                _currentCycleStartTime = _currentCycleStartTime.AddDays(-1);
            }
            
            OnPhotoperiodChanged?.Invoke(_currentPhotoperiod);
            LogDebug($"Photoperiod set: {photoperiod.Name} ({photoperiod.LightHours}h light / {photoperiod.DarkHours}h dark)");
        }
        
        /// <summary>
        /// Update photoperiod state
        /// </summary>
        private void UpdatePhotoperiod()
        {
            if (_currentPhotoperiod == null || !_currentPhotoperiod.IsActive) return;
            
            var currentTime = System.DateTime.Now;
            var cycleElapsed = (currentTime - _currentCycleStartTime).TotalHours;
            var totalCycleHours = _currentPhotoperiod.LightHours + _currentPhotoperiod.DarkHours;
            
            // Check if we need to start a new cycle
            if (cycleElapsed >= totalCycleHours)
            {
                _currentCycleStartTime = _currentCycleStartTime.AddHours(totalCycleHours);
                cycleElapsed = (currentTime - _currentCycleStartTime).TotalHours;
                
                RecordAutomationEvent(AutomationEventType.PhotoperiodCycleStart, "New photoperiod cycle started");
            }
            
            // Determine if we should be in light or dark period
            bool shouldBeInLightPeriod = cycleElapsed < _currentPhotoperiod.LightHours;
            
            if (shouldBeInLightPeriod != _isInPhotoperiod)
            {
                _isInPhotoperiod = shouldBeInLightPeriod;
                OnPhotoperiodStateChanged?.Invoke(_isInPhotoperiod);
                
                if (_isInPhotoperiod)
                {
                    // Switch to light period
                    _lightController.SetIntensity(_currentPhotoperiod.LightIntensity);
                    _spectrumController.ActivatePreset(_currentPhotoperiod.SpectrumPreset);
                    _lightController.TurnOn();
                    
                    RecordAutomationEvent(AutomationEventType.PhotoperiodLightStart, "Photoperiod light period started");
                }
                else
                {
                    // Switch to dark period
                    _lightController.SetIntensity(_currentPhotoperiod.DarkIntensity);
                    if (_currentPhotoperiod.DarkIntensity == 0f)
                    {
                        _lightController.TurnOff();
                    }
                    
                    RecordAutomationEvent(AutomationEventType.PhotoperiodDarkStart, "Photoperiod dark period started");
                }
            }
        }
        
        /// <summary>
        /// Get photoperiod status information
        /// </summary>
        public PhotoperiodStatus GetPhotoperiodStatus()
        {
            if (_currentPhotoperiod == null)
                return null;
            
            var currentTime = System.DateTime.Now;
            var cycleElapsed = (currentTime - _currentCycleStartTime).TotalHours;
            var totalCycleHours = _currentPhotoperiod.LightHours + _currentPhotoperiod.DarkHours;
            
            float hoursRemainingInCurrentPeriod;
            if (_isInPhotoperiod)
            {
                hoursRemainingInCurrentPeriod = (float)(_currentPhotoperiod.LightHours - cycleElapsed);
            }
            else
            {
                hoursRemainingInCurrentPeriod = (float)(totalCycleHours - cycleElapsed);
            }
            
            return new PhotoperiodStatus
            {
                IsInLightPeriod = _isInPhotoperiod,
                CycleElapsedHours = (float)cycleElapsed,
                HoursRemainingInCurrentPeriod = Mathf.Max(0f, hoursRemainingInCurrentPeriod),
                NextStateChangeTime = _currentCycleStartTime.AddHours(_isInPhotoperiod ? _currentPhotoperiod.LightHours : totalCycleHours),
                CurrentProgram = _currentPhotoperiod
            };
        }
        
        #endregion
        
        #region Schedule Management
        
        /// <summary>
        /// Add a lighting schedule
        /// </summary>
        public void AddSchedule(LightingSchedule schedule)
        {
            schedule.ScheduleId = System.Guid.NewGuid().ToString();
            schedule.IsActive = true;
            
            _activeSchedules.Add(schedule);
            
            LogDebug($"Added lighting schedule: {schedule.Name}");
        }
        
        /// <summary>
        /// Remove a lighting schedule
        /// </summary>
        public bool RemoveSchedule(string scheduleId)
        {
            var schedule = _activeSchedules.FirstOrDefault(s => s.ScheduleId == scheduleId);
            if (schedule != null)
            {
                _activeSchedules.Remove(schedule);
                LogDebug($"Removed lighting schedule: {schedule.Name}");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Check and execute due schedules
        /// </summary>
        private void CheckSchedules()
        {
            var currentTime = System.DateTime.Now;
            var dueSchedules = _activeSchedules
                .Where(s => s.IsActive && ShouldExecuteSchedule(s, currentTime))
                .ToList();
            
            foreach (var schedule in dueSchedules)
            {
                ExecuteSchedule(schedule);
            }
        }
        
        /// <summary>
        /// Check if a schedule should be executed
        /// </summary>
        private bool ShouldExecuteSchedule(LightingSchedule schedule, System.DateTime currentTime)
        {
            // Check if enough time has passed since last execution
            if ((currentTime - schedule.LastExecuted).TotalMinutes < 1) // Minimum 1 minute between executions
                return false;
            
            var currentTimeSpan = currentTime.TimeOfDay;
            
            switch (schedule.ScheduleType)
            {
                case ScheduleType.Daily:
                    return IsTimeClose(currentTimeSpan, schedule.ExecutionTime, System.TimeSpan.FromMinutes(1));
                    
                case ScheduleType.Weekly:
                    return currentTime.DayOfWeek == schedule.DayOfWeek && 
                           IsTimeClose(currentTimeSpan, schedule.ExecutionTime, System.TimeSpan.FromMinutes(1));
                    
                case ScheduleType.Interval:
                    return (currentTime - schedule.LastExecuted).TotalMinutes >= schedule.IntervalMinutes;
                    
                case ScheduleType.OneTime:
                    return schedule.ExecutionDateTime.HasValue && 
                           Mathf.Abs((float)(currentTime - schedule.ExecutionDateTime.Value).TotalMinutes) < 1f;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Execute a lighting schedule
        /// </summary>
        private void ExecuteSchedule(LightingSchedule schedule)
        {
            try
            {
                // Apply schedule settings
                switch (schedule.Action)
                {
                    case ScheduleAction.TurnOn:
                        _lightController.TurnOn();
                        break;
                        
                    case ScheduleAction.TurnOff:
                        _lightController.TurnOff();
                        break;
                        
                    case ScheduleAction.SetIntensity:
                        _lightController.SetIntensity(schedule.TargetIntensity);
                        break;
                        
                    case ScheduleAction.SetSpectrum:
                        _spectrumController.ActivatePreset(schedule.TargetSpectrum);
                        break;
                        
                    case ScheduleAction.Custom:
                        ExecuteCustomScheduleAction(schedule);
                        break;
                }
                
                schedule.LastExecuted = System.DateTime.Now;
                schedule.ExecutionCount++;
                
                // Handle one-time schedules
                if (schedule.ScheduleType == ScheduleType.OneTime)
                {
                    schedule.IsActive = false;
                }
                
                OnScheduleExecuted?.Invoke(schedule);
                RecordAutomationEvent(AutomationEventType.ScheduleExecuted, $"Executed schedule: {schedule.Name}");
                
                LogDebug($"Executed schedule: {schedule.Name} - {schedule.Action}");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to execute schedule {schedule.Name}: {ex.Message}");
                RecordAutomationEvent(AutomationEventType.Error, $"Schedule execution failed: {schedule.Name}");
            }
        }
        
        /// <summary>
        /// Execute custom schedule action
        /// </summary>
        private void ExecuteCustomScheduleAction(LightingSchedule schedule)
        {
            // Custom actions could include:
            // - Complex spectrum transitions
            // - Multi-step intensity changes
            // - Coordination with other systems
            
            LogDebug($"Executing custom action for schedule: {schedule.Name}");
        }
        
        #endregion
        
        #region Smart Programs
        
        /// <summary>
        /// Start a smart lighting program
        /// </summary>
        public void StartSmartProgram(SmartLightingProgram program)
        {
            switch (program.ProgramType)
            {
                case SmartProgramType.SunriseSunset:
                    StartCoroutine(ExecuteSunriseSunsetProgram(program));
                    break;
                    
                case SmartProgramType.GrowthStageAdaptive:
                    StartCoroutine(ExecuteGrowthStageProgram(program));
                    break;
                    
                case SmartProgramType.EnergyOptimized:
                    StartCoroutine(ExecuteEnergyOptimizedProgram(program));
                    break;
            }
            
            RecordAutomationEvent(AutomationEventType.SmartProgramStart, $"Started smart program: {program.Name}");
            LogDebug($"Started smart lighting program: {program.Name}");
        }
        
        /// <summary>
        /// Execute sunrise/sunset simulation program
        /// </summary>
        private IEnumerator ExecuteSunriseSunsetProgram(SmartLightingProgram program)
        {
            float transitionDuration = program.Parameters.GetValueOrDefault("transitionMinutes", 30f) * 60f;
            
            // Sunrise
            _spectrumController.ActivatePreset(SpectrumPreset.Dawn);
            yield return new WaitForSeconds(1f);
            
            float startIntensity = 0f;
            float targetIntensity = program.Parameters.GetValueOrDefault("maxIntensity", 800f);
            
            for (float t = 0; t < transitionDuration; t += Time.deltaTime)
            {
                float progress = t / transitionDuration;
                float currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, progress);
                _lightController.SetIntensityImmediate(currentIntensity);
                
                // Gradually shift to midday spectrum
                if (progress > 0.5f)
                {
                    _spectrumController.ActivatePreset(SpectrumPreset.Midday);
                }
                
                yield return null;
            }
            
            RecordAutomationEvent(AutomationEventType.SmartProgramComplete, "Sunrise simulation completed");
        }
        
        /// <summary>
        /// Execute growth stage adaptive program
        /// </summary>
        private IEnumerator ExecuteGrowthStageProgram(SmartLightingProgram program)
        {
            while (program.IsActive)
            {
                // Get dominant growth stage from plant optimizer
                var monitoringData = _plantOptimizer.GetAllMonitoringData();
                if (monitoringData.Count > 0)
                {
                    var dominantStage = monitoringData
                        .GroupBy(p => GetPlantGrowthStage(p.PlantComponent))
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key;
                    
                    if (dominantStage.HasValue)
                    {
                        AdaptToGrowthStage(dominantStage.Value);
                    }
                }
                
                yield return new WaitForSeconds(program.Parameters.GetValueOrDefault("checkIntervalMinutes", 30f) * 60f);
            }
        }
        
        /// <summary>
        /// Execute energy optimized program
        /// </summary>
        private IEnumerator ExecuteEnergyOptimizedProgram(SmartLightingProgram program)
        {
            // This would implement energy-saving strategies
            // - Dimming during off-peak hours
            // - Using more efficient spectrums
            // - Coordinating with other environmental systems
            
            yield return new WaitForSeconds(1f);
            RecordAutomationEvent(AutomationEventType.SmartProgramComplete, "Energy optimization cycle completed");
        }
        
        /// <summary>
        /// Adapt lighting to specific growth stage
        /// </summary>
        private void AdaptToGrowthStage(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seedling:
                    _lightController.SetIntensity(300f);
                    _spectrumController.ActivatePreset(SpectrumPreset.Dawn);
                    break;
                    
                case PlantGrowthStage.Vegetative:
                    _lightController.SetIntensity(600f);
                    _spectrumController.ActivatePreset(SpectrumPreset.Vegetative);
                    break;
                    
                case PlantGrowthStage.Flowering:
                    _lightController.SetIntensity(800f);
                    _spectrumController.ActivatePreset(SpectrumPreset.Flowering);
                    break;
            }
            
            RecordAutomationEvent(AutomationEventType.GrowthStageAdaptation, $"Adapted to growth stage: {stage}");
        }
        
        #endregion
        
        #region Control Methods
        
        /// <summary>
        /// Enable or disable automation
        /// </summary>
        public void SetAutomationEnabled(bool enabled)
        {
            _automationEnabled = enabled;
            RecordAutomationEvent(AutomationEventType.ModeChange, $"Automation {(enabled ? "enabled" : "disabled")}");
            LogDebug($"Automation {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Set automation mode
        /// </summary>
        public void SetAutomationMode(AutomationMode mode)
        {
            _currentMode = mode;
            RecordAutomationEvent(AutomationEventType.ModeChange, $"Automation mode changed to: {mode}");
            LogDebug($"Automation mode set to: {mode}");
        }
        
        /// <summary>
        /// Get all active schedules
        /// </summary>
        public List<LightingSchedule> GetActiveSchedules()
        {
            return _activeSchedules.Where(s => s.IsActive).ToList();
        }
        
        /// <summary>
        /// Clear all schedules
        /// </summary>
        public void ClearAllSchedules()
        {
            _activeSchedules.Clear();
            RecordAutomationEvent(AutomationEventType.ScheduleCleared, "All schedules cleared");
            LogDebug("All lighting schedules cleared");
        }
        
        #endregion
        
        #region Event Tracking
        
        /// <summary>
        /// Record automation event
        /// </summary>
        private void RecordAutomationEvent(AutomationEventType eventType, string description)
        {
            var automationEvent = new AutomationEvent
            {
                EventId = System.Guid.NewGuid().ToString(),
                Timestamp = System.DateTime.Now,
                EventType = eventType,
                Description = description
            };
            
            _eventHistory.Add(automationEvent);
            OnAutomationEvent?.Invoke(automationEvent);
            
            // Keep history manageable
            if (_eventHistory.Count > _maxScheduleHistory)
            {
                _eventHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Get recent automation events
        /// </summary>
        public List<AutomationEvent> GetRecentEvents(int count = 20)
        {
            return _eventHistory.TakeLast(count).ToList();
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Check if two times are close to each other
        /// </summary>
        private bool IsTimeClose(System.TimeSpan time1, System.TimeSpan time2, System.TimeSpan tolerance)
        {
            var difference = time1 > time2 ? time1 - time2 : time2 - time1;
            return difference <= tolerance;
        }
        
        /// <summary>
        /// Get plant growth stage from component using reflection if needed
        /// </summary>
        private ProjectChimera.Data.Shared.PlantGrowthStage GetPlantGrowthStage(MonoBehaviour plantComponent)
        {
            if (plantComponent == null) return ProjectChimera.Data.Shared.PlantGrowthStage.Seed;
            
            // Try to get CurrentGrowthStage property using reflection
            var property = plantComponent.GetType().GetProperty("CurrentGrowthStage");
            if (property != null && property.PropertyType == typeof(ProjectChimera.Data.Shared.PlantGrowthStage))
            {
                return (ProjectChimera.Data.Shared.PlantGrowthStage)property.GetValue(plantComponent);
            }
            
            return ProjectChimera.Data.Shared.PlantGrowthStage.Seed; // Default
        }
        
        #endregion
        
        private void LogDebug(string message)
        {
            if (_enableAutomationLogging)
                Debug.Log($"[GrowLightAutomationSystem] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[GrowLightAutomationSystem] {message}");
        }
    }
    
    /// <summary>
    /// Photoperiod program configuration
    /// </summary>
    [System.Serializable]
    public class PhotoperiodProgram
    {
        public string Name = "Default";
        public float LightHours = 18f;
        public float DarkHours = 6f;
        public System.TimeSpan StartTime = new System.TimeSpan(6, 0, 0);
        public float LightIntensity = 600f;
        public float DarkIntensity = 0f;
        public SpectrumPreset SpectrumPreset = SpectrumPreset.Balanced;
        public bool IsActive = true;
    }
    
    /// <summary>
    /// Photoperiod status information
    /// </summary>
    [System.Serializable]
    public class PhotoperiodStatus
    {
        public bool IsInLightPeriod;
        public float CycleElapsedHours;
        public float HoursRemainingInCurrentPeriod;
        public System.DateTime NextStateChangeTime;
        public PhotoperiodProgram CurrentProgram;
    }
    
    /// <summary>
    /// Lighting schedule configuration
    /// </summary>
    [System.Serializable]
    public class LightingSchedule
    {
        public string ScheduleId;
        public string Name;
        public ScheduleType ScheduleType;
        public ScheduleAction Action;
        public System.TimeSpan ExecutionTime;
        public System.DayOfWeek DayOfWeek;
        public System.DateTime? ExecutionDateTime;
        public int IntervalMinutes;
        public float TargetIntensity;
        public SpectrumPreset TargetSpectrum;
        public bool IsActive = true;
        public System.DateTime LastExecuted;
        public int ExecutionCount = 0;
        
        // Additional property for compatibility with LightingController
        public List<LightingScheduleEntry> ScheduleEntries { get; set; } = new List<LightingScheduleEntry>();
    }
    
    /// <summary>
    /// Smart lighting program
    /// </summary>
    [System.Serializable]
    public class SmartLightingProgram
    {
        public string Name;
        public SmartProgramType ProgramType;
        public Dictionary<string, float> Parameters = new Dictionary<string, float>();
        public bool IsActive = true;
    }
    
    /// <summary>
    /// Automation event record
    /// </summary>
    [System.Serializable]
    public class AutomationEvent
    {
        public string EventId;
        public System.DateTime Timestamp;
        public AutomationEventType EventType;
        public string Description;
    }
    
    public enum AutomationMode
    {
        Manual,
        Schedule,
        Photoperiod,
        Smart,
        PlantAdaptive
    }
    
    public enum ScheduleType
    {
        Daily,
        Weekly,
        Interval,
        OneTime
    }
    
    public enum ScheduleAction
    {
        TurnOn,
        TurnOff,
        SetIntensity,
        SetSpectrum,
        Custom
    }
    
    public enum SmartProgramType
    {
        SunriseSunset,
        GrowthStageAdaptive,
        EnergyOptimized,
        WeatherSync
    }
    
    public enum AutomationEventType
    {
        ScheduleExecuted,
        ScheduleCleared,
        PhotoperiodCycleStart,
        PhotoperiodLightStart,
        PhotoperiodDarkStart,
        SmartProgramStart,
        SmartProgramComplete,
        GrowthStageAdaptation,
        ModeChange,
        Error
    }
}