using UnityEngine;
using ProjectChimera.Data.Shared;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant State Coordinator
    /// Single Responsibility: Plant state management, growth stage transitions, and position tracking
    /// Extracted from PlantInstanceSO for better separation of concerns
    /// </summary>
    [System.Serializable]
    public class PlantStateCoordinator
    {
        [Header("State Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _validateTransitions = true;
        [SerializeField] private bool _trackStateHistory = true;
        [SerializeField] private int _maxHistoryEntries = 50;

        // Current state data
        [SerializeField] private PlantGrowthStage _currentGrowthStage = PlantGrowthStage.Seedling;
        [SerializeField] private float _ageInDays = 0f;
        [SerializeField] private float _daysInCurrentStage = 0f;
        [SerializeField] private Vector3 _worldPosition = Vector3.zero;

        // Physical characteristics
        [SerializeField, Range(0f, 500f)] private float _currentHeight = 5f;
        [SerializeField, Range(0f, 200f)] private float _currentWidth = 2f;
        [SerializeField, Range(0f, 100f)] private float _rootMassPercentage = 30f;
        [SerializeField, Range(0f, 1000f)] private float _leafArea = 10f;

        // Health and vitality
        [SerializeField, Range(0f, 1f)] private float _overallHealth = 1f;
        [SerializeField, Range(0f, 1f)] private float _vigor = 1f;
        [SerializeField, Range(0f, 1f)] private float _stressLevel = 0f;
        [SerializeField, Range(0f, 1f)] private float _immuneResponse = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _maturityLevel = 0f;

        // Growth metrics
        [SerializeField] private float _dailyGrowthRate = 1f;
        [SerializeField] private float _biomassAccumulation = 2f;
        [SerializeField] private float _rootDevelopmentRate = 1f;
        [SerializeField, Range(0f, 2f)] private float _growthProgress = 0f;
        [SerializeField] private int _daysSincePlanted = 0;

        // Environmental tracking
        [SerializeField] private string _currentEnvironment = "";
        [SerializeField] private float _cumulativeStressDays = 0f;
        [SerializeField] private float _optimalDays = 0f;

        // State history
        private System.Collections.Generic.List<PlantStateSnapshot> _stateHistory = new System.Collections.Generic.List<PlantStateSnapshot>();

        // Statistics
        private PlantStateStats _stats = new PlantStateStats();

        // State tracking
        private bool _isInitialized = false;
        private DateTime _lastStateUpdate = DateTime.Now;

        // Events
        public event System.Action<PlantGrowthStage, PlantGrowthStage> OnGrowthStageChanged; // old stage, new stage
        public event System.Action<float> OnHealthChanged; // new health value
        public event System.Action<Vector3, Vector3> OnPositionChanged; // old position, new position
        public event System.Action<float> OnStressLevelChanged; // new stress level
        public event System.Action<PlantStateSnapshot> OnStateSnapshotTaken;

        public bool IsInitialized => _isInitialized;
        public PlantStateStats Stats => _stats;
        public PlantGrowthStage CurrentGrowthStage => _currentGrowthStage;
        public float AgeInDays => _ageInDays;
        public float DaysInCurrentStage => _daysInCurrentStage;
        public Vector3 WorldPosition => _worldPosition;
        public float CurrentHeight => _currentHeight;
        public float CurrentWidth => _currentWidth;
        public float RootMassPercentage => _rootMassPercentage;
        public float LeafArea => _leafArea;
        public float OverallHealth => _overallHealth;
        public float Vigor => _vigor;
        public float StressLevel => _stressLevel;
        public float ImmuneResponse => _immuneResponse;
        public float MaturityLevel => _maturityLevel;
        public float DailyGrowthRate => _dailyGrowthRate;
        public float BiomassAccumulation => _biomassAccumulation;
        public float GrowthProgress => _growthProgress;
        public float RootDevelopmentRate => _rootDevelopmentRate;
        public float CumulativeStressDays => _cumulativeStressDays;
        public float OptimalDays => _optimalDays;
        public string CurrentEnvironment => _currentEnvironment;
        public bool IsActive => _overallHealth > 0f && _currentGrowthStage != PlantGrowthStage.Dormant;

        public void Initialize()
        {
            if (_isInitialized) return;

            _stateHistory.Clear();
            ResetStats();
            _lastStateUpdate = DateTime.Now;

            // Take initial state snapshot
            if (_trackStateHistory)
            {
                TakeStateSnapshot("Initialization");
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Plant State Coordinator initialized - Stage: {_currentGrowthStage}, Health: {_overallHealth:F2}");
            }
        }

        /// <summary>
        /// Update plant age and stage progression
        /// </summary>
        public void UpdateAge(float deltaTimeDays)
        {
            if (!_isInitialized) Initialize();

            _ageInDays += deltaTimeDays;
            _daysInCurrentStage += deltaTimeDays;
            _daysSincePlanted = (int)_ageInDays;

            _stats.AgeUpdates++;
            _lastStateUpdate = DateTime.Now;

            if (_enableLogging && _stats.AgeUpdates % 10 == 0) // Log every 10 updates
            {
                ChimeraLogger.Log("PLANT", $"Plant aged: {_ageInDays:F1} days, Current stage: {_daysInCurrentStage:F1} days");
            }
        }

        /// <summary>
        /// Set growth stage with validation
        /// </summary>
        public bool SetGrowthStage(PlantGrowthStage newStage)
        {
            if (!_isInitialized) Initialize();

            if (_validateTransitions && !IsValidStageTransition(_currentGrowthStage, newStage))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("PLANT", $"Invalid stage transition: {_currentGrowthStage} -> {newStage}");
                }
                return false;
            }

            var oldStage = _currentGrowthStage;
            _currentGrowthStage = newStage;
            _daysInCurrentStage = 0f; // Reset stage timer

            _stats.StageTransitions++;
            OnGrowthStageChanged?.Invoke(oldStage, newStage);

            if (_trackStateHistory)
            {
                TakeStateSnapshot($"Stage Change: {oldStage} -> {newStage}");
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Growth stage changed: {oldStage} -> {newStage}");
            }

            return true;
        }

        /// <summary>
        /// Update overall health
        /// </summary>
        public void UpdateHealth(float newHealth)
        {
            if (!_isInitialized) Initialize();

            var oldHealth = _overallHealth;
            _overallHealth = Mathf.Clamp01(newHealth);

            if (Mathf.Abs(_overallHealth - oldHealth) > 0.01f)
            {
                _stats.HealthChanges++;
                OnHealthChanged?.Invoke(_overallHealth);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Health updated: {oldHealth:F2} -> {_overallHealth:F2}");
                }
            }
        }

        /// <summary>
        /// Update physical characteristics
        /// </summary>
        public void UpdatePhysicalCharacteristics(float height, float width, float rootMass, float leafArea)
        {
            if (!_isInitialized) Initialize();

            _currentHeight = Mathf.Max(0f, height);
            _currentWidth = Mathf.Max(0f, width);
            _rootMassPercentage = Mathf.Clamp(rootMass, 0f, 100f);
            _leafArea = Mathf.Max(0f, leafArea);

            _stats.PhysicalUpdates++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Physical characteristics updated: H:{_currentHeight:F1}cm, W:{_currentWidth:F1}cm, Leaf:{_leafArea:F1}cm²");
            }
        }

        /// <summary>
        /// Update stress level
        /// </summary>
        public void UpdateStressLevel(float newStressLevel)
        {
            if (!_isInitialized) Initialize();

            var oldStress = _stressLevel;
            _stressLevel = Mathf.Clamp01(newStressLevel);

            // Track cumulative stress days
            if (_stressLevel > 0.5f)
            {
                _cumulativeStressDays += Time.deltaTime / 86400f; // Convert seconds to days
            }
            else if (_stressLevel < 0.2f)
            {
                _optimalDays += Time.deltaTime / 86400f;
            }

            if (Mathf.Abs(_stressLevel - oldStress) > 0.01f)
            {
                _stats.StressChanges++;
                OnStressLevelChanged?.Invoke(_stressLevel);

                if (_enableLogging && _stressLevel > 0.7f)
                {
                    ChimeraLogger.LogWarning("PLANT", $"High stress level detected: {_stressLevel:F2}");
                }
            }
        }

        /// <summary>
        /// Update world position
        /// </summary>
        public void UpdatePosition(Vector3 newPosition)
        {
            if (!_isInitialized) Initialize();

            var oldPosition = _worldPosition;
            _worldPosition = newPosition;

            _stats.PositionChanges++;
            OnPositionChanged?.Invoke(oldPosition, newPosition);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Position updated: {oldPosition} -> {newPosition}");
            }
        }

        /// <summary>
        /// Update vitality metrics
        /// </summary>
        public void UpdateVitality(float vigor, float immuneResponse, float maturityLevel)
        {
            if (!_isInitialized) Initialize();

            _vigor = Mathf.Clamp01(vigor);
            _immuneResponse = Mathf.Clamp01(immuneResponse);
            _maturityLevel = Mathf.Clamp01(maturityLevel);

            _stats.VitalityUpdates++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Vitality updated: Vigor:{_vigor:F2}, Immune:{_immuneResponse:F2}, Maturity:{_maturityLevel:F2}");
            }
        }

        /// <summary>
        /// Update growth metrics
        /// </summary>
        public void UpdateGrowthMetrics(float dailyGrowthRate, float biomassAccumulation, float rootDevelopmentRate, float growthProgress)
        {
            if (!_isInitialized) Initialize();

            _dailyGrowthRate = Mathf.Max(0f, dailyGrowthRate);
            _biomassAccumulation = Mathf.Max(0f, biomassAccumulation);
            _rootDevelopmentRate = Mathf.Max(0f, rootDevelopmentRate);
            _growthProgress = Mathf.Clamp(growthProgress, 0f, 2f);

            _stats.GrowthUpdates++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Growth metrics updated: Daily:{_dailyGrowthRate:F2}, Biomass:{_biomassAccumulation:F2}, Progress:{_growthProgress:F2}");
            }
        }

        /// <summary>
        /// Set environmental context
        /// </summary>
        public void SetEnvironmentalContext(string environmentDescription)
        {
            if (!_isInitialized) Initialize();

            _currentEnvironment = environmentDescription ?? "";
            _stats.EnvironmentChanges++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Environmental context updated: {_currentEnvironment}");
            }
        }

        /// <summary>
        /// Take a state snapshot for history tracking
        /// </summary>
        public void TakeStateSnapshot(string reason = "Manual")
        {
            if (!_trackStateHistory) return;

            var snapshot = new PlantStateSnapshot
            {
                Timestamp = DateTime.Now,
                Reason = reason,
                GrowthStage = _currentGrowthStage,
                AgeInDays = _ageInDays,
                DaysInCurrentStage = _daysInCurrentStage,
                Health = _overallHealth,
                Vigor = _vigor,
                StressLevel = _stressLevel,
                Height = _currentHeight,
                Width = _currentWidth,
                MaturityLevel = _maturityLevel,
                GrowthProgress = _growthProgress,
                Position = _worldPosition
            };

            _stateHistory.Add(snapshot);

            // Limit history size
            while (_stateHistory.Count > _maxHistoryEntries)
            {
                _stateHistory.RemoveAt(0);
            }

            _stats.SnapshotsTaken++;
            OnStateSnapshotTaken?.Invoke(snapshot);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"State snapshot taken: {reason}");
            }
        }

        /// <summary>
        /// Get state summary
        /// </summary>
        public PlantStateSummary GetStateSummary()
        {
            return new PlantStateSummary
            {
                PlantID = "", // Will be set by calling code
                CurrentStage = _currentGrowthStage,
                AgeInDays = _ageInDays,
                DaysInCurrentStage = _daysInCurrentStage,
                OverallHealth = _overallHealth,
                Vigor = _vigor,
                StressLevel = _stressLevel,
                MaturityLevel = _maturityLevel,
                CurrentHeight = _currentHeight,
                CurrentWidth = _currentWidth,
                LeafArea = _leafArea,
                IsActive = IsActive,
                LastUpdate = _lastStateUpdate,
                CumulativeStressDays = _cumulativeStressDays,
                OptimalDays = _optimalDays
            };
        }

        /// <summary>
        /// Get state history
        /// </summary>
        public System.Collections.Generic.List<PlantStateSnapshot> GetStateHistory()
        {
            return new System.Collections.Generic.List<PlantStateSnapshot>(_stateHistory);
        }

        /// <summary>
        /// Validate stage transition
        /// </summary>
        private bool IsValidStageTransition(PlantGrowthStage from, PlantGrowthStage to)
        {
            // Basic validation - can only progress forward or go to dormant
            if (to == PlantGrowthStage.Dormant) return true; // Can always go dormant

            // Define valid transitions
            switch (from)
            {
                case PlantGrowthStage.Seedling:
                    return to == PlantGrowthStage.Vegetative;
                case PlantGrowthStage.Vegetative:
                    return to == PlantGrowthStage.Flowering;
                case PlantGrowthStage.Flowering:
                    return to == PlantGrowthStage.Mature;
                case PlantGrowthStage.Mature:
                    return to == PlantGrowthStage.Dormant; // Already mature
                case PlantGrowthStage.Dormant:
                    return to == PlantGrowthStage.Seedling; // Can restart cycle
                default:
                    return false;
            }
        }

        /// <summary>
        /// Reset state statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new PlantStateStats();
        }

        /// <summary>
        /// Set state tracking parameters
        /// </summary>
        public void SetTrackingParameters(bool validateTransitions, bool trackHistory, int maxHistoryEntries)
        {
            _validateTransitions = validateTransitions;
            _trackStateHistory = trackHistory;
            _maxHistoryEntries = Mathf.Max(10, maxHistoryEntries);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"State tracking parameters updated: Validate={validateTransitions}, Track={trackHistory}, Max={maxHistoryEntries}");
            }
        }

        /// <summary>
        /// Force state refresh and snapshot
        /// </summary>
        [ContextMenu("Force State Refresh")]
        public void ForceStateRefresh()
        {
            if (_isInitialized)
            {
                TakeStateSnapshot("Manual Refresh");
                _lastStateUpdate = DateTime.Now;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Plant state manually refreshed");
                }
            }
        }
    }

    /// <summary>
    /// Plant state statistics
    /// </summary>
    [System.Serializable]
    public struct PlantStateStats
    {
        public int AgeUpdates;
        public int StageTransitions;
        public int HealthChanges;
        public int StressChanges;
        public int PositionChanges;
        public int VitalityUpdates;
        public int GrowthUpdates;
        public int PhysicalUpdates;
        public int EnvironmentChanges;
        public int SnapshotsTaken;
    }

    /// <summary>
    /// Plant state snapshot for history tracking
    /// </summary>
    [System.Serializable]
    public struct PlantStateSnapshot
    {
        public DateTime Timestamp;
        public string Reason;
        public PlantGrowthStage GrowthStage;
        public float AgeInDays;
        public float DaysInCurrentStage;
        public float Health;
        public float Vigor;
        public float StressLevel;
        public float Height;
        public float Width;
        public float MaturityLevel;
        public float GrowthProgress;
        public Vector3 Position;
    }

    /// <summary>
    /// Plant state summary
    /// </summary>
    [System.Serializable]
    public struct PlantStateSummary
    {
        public string PlantID;
        public PlantGrowthStage CurrentStage;
        public float AgeInDays;
        public float DaysInCurrentStage;
        public float OverallHealth;
        public float Vigor;
        public float StressLevel;
        public float MaturityLevel;
        public float CurrentHeight;
        public float CurrentWidth;
        public float LeafArea;
        public bool IsActive;
        public DateTime LastUpdate;
        public float CumulativeStressDays;
        public float OptimalDays;
    }
}