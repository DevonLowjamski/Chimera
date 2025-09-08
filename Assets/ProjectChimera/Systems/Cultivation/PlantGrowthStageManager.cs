using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Events;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
// using object = ProjectChimera.Systems.Genetics.object; // Genetics not available
// using ProjectChimera.Systems.Genetics; // Genetics assembly not available

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Phase 2.2.1: Plant Growth Stage Manager
    /// Advanced plant growth tracking with environmental monitoring and stress detection
    /// Manages stage transitions, monitors plant health, and provides data to other systems
    /// </summary>
    public class PlantGrowthStageManager : MonoBehaviour, IChimeraManager, ITickable
    {
        [Header("Configuration")]
        [SerializeField] private bool _enableDebugLogs = false;
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private bool _enableStressDetection = true;
        [SerializeField] private bool _automaticStageTransitions = true;
        [SerializeField] private float _updateFrequency = 1.0f;

        [Header("Stage Transition Settings")]
        [SerializeField] private bool _requireMinimumStageTime = true;
        [SerializeField] private float _minimumStageTimeHours = 24.0f;
        [SerializeField] private bool _allowSkippingStages = false;
        [SerializeField] private float _transitionDelaySeconds = 0.5f;

        [Header("Performance Monitoring")]
        [SerializeField] private int _maxTrackedPlants = 1000;
        [SerializeField] private float _cleanupInterval = 300.0f;
        [SerializeField] private bool _logPerformanceMetrics = false;

        // Core system state
        private Dictionary<string, PlantStageData> _plantStageTracking = new Dictionary<string, PlantStageData>();
        private List<string> _plantUpdateQueue = new List<string>();
        private Dictionary<string, float> _plantLastUpdateTime = new Dictionary<string, float>();

        // Performance optimization
        private float _lastCleanupTime = 0f;
        private int _plantsProcessedThisFrame = 0;
        private int _maxPlantsPerFrame = 50;

        // System integration
        private ITimeManager _timeManager;

        // Manager interface implementation
        public bool IsInitialized { get; private set; }
        public string ManagerName => "Plant Growth Stage Manager";
        public float InitializationProgress { get; private set; }

        /// <summary>
        /// Initialize the manager and prepare it for use.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            InitializeAsync();
        }

        /// <summary>
        /// Shutdown the manager and clean up resources.
        /// </summary>
        public void Shutdown()
        {
            if (!IsInitialized) return;

            // Clean up tracking data
            _plantStageTracking.Clear();
            _plantUpdateQueue.Clear();
            _plantLastUpdateTime.Clear();

            IsInitialized = false;
            InitializationProgress = 0f;

            if (_enableDebugLogs)
            {
                ChimeraLogger.Log("[PlantGrowthStageManager] Shutdown complete.");
            }
        }

        private void Awake()
        {
            InitializationProgress = 0f;

            if (_enableDebugLogs)
            {
                ChimeraLogger.Log("[PlantGrowthStageManager] Awakening...");
            }
        }

        private void Start()
        {
            InitializeAsync();
        }

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
            Shutdown();
        }

        private async void InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.1f;

                // Initialize core systems
                if (_enableDebugLogs)
                {
                    ChimeraLogger.Log("[PlantGrowthStageManager] Core systems initialized.");
                }

                InitializationProgress = 0.3f;

                // Try to find ITimeManager implementation
                _timeManager = FindTimeManager();
                if (_timeManager == null)
                {
                    ChimeraLogger.LogWarning("[PlantGrowthStageManager] ITimeManager not found. Using Unity Time.");
                }

                InitializationProgress = 0.5f;

                // Initialize tracking systems
                _plantStageTracking.Clear();
                _plantUpdateQueue.Clear();
                _plantLastUpdateTime.Clear();
                _lastCleanupTime = GetCurrentTime();

                InitializationProgress = 0.8f;

                // Core initialization complete
                if (_enableDebugLogs)
                {
                    ChimeraLogger.Log("[PlantGrowthStageManager] Core initialization complete.");
                }

                InitializationProgress = 1.0f;
                IsInitialized = true;

                // Register with UpdateOrchestrator
                UpdateOrchestrator.Instance.RegisterTickable(this);

                if (_enableDebugLogs)
                {
                    ChimeraLogger.Log("[PlantGrowthStageManager] Initialization complete.");
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PlantGrowthStageManager] Initialization failed: {ex.Message}");
                IsInitialized = false;
            }
        }

        #region ITickable Implementation

        public int Priority => TickPriority.PlantLifecycle;
        public bool Enabled => IsInitialized;

        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            _plantsProcessedThisFrame = 0;
            float currentTime = GetCurrentTime();

            // Process plant updates based on frequency
            ProcessPlantUpdates(currentTime);

            // Periodic cleanup
            if (currentTime - _lastCleanupTime >= _cleanupInterval)
            {
                PerformCleanup();
                _lastCleanupTime = currentTime;
            }

            // Performance monitoring
            if (_enablePerformanceMonitoring && _logPerformanceMetrics)
            {
                LogPerformanceMetrics();
            }
        }

        #endregion

        /// <summary>
        /// Register a plant for stage tracking
        /// </summary>
        public bool RegisterPlant(PlantInstanceSO plantInstance)
        {
            if (plantInstance == null)
            {
                ChimeraLogger.LogWarning("[PlantGrowthStageManager] Cannot register null plant instance.");
                return false;
            }

            string plantId = plantInstance.PlantID;
            if (string.IsNullOrEmpty(plantId))
            {
                ChimeraLogger.LogWarning("[PlantGrowthStageManager] Cannot register plant with empty ID.");
                return false;
            }

            if (_plantStageTracking.ContainsKey(plantId))
            {
                if (_enableDebugLogs)
                {
                    ChimeraLogger.Log($"[PlantGrowthStageManager] Plant {plantId} already registered. Updating data.");
                }
            }

            var stageData = new PlantStageData
            {
                PlantId = plantId,
                PlantInstance = plantInstance,
                Genotype = plantInstance.Genotype,
                CurrentStage = (PlantGrowthStage)plantInstance.CurrentGrowthStage,
                TimeInCurrentStage = 0f,
                StageStartTime = GetCurrentTime(),
                StageDuration = GetStageDuration((PlantGrowthStage)plantInstance.CurrentGrowthStage, plantInstance.Genotype),
                StageProgress = 0f,
                IsStageComplete = false,
                EnvironmentalHistory = new List<object>(),
                StageTransitionHistory = new List<StageTransitionRecord>()
            };

            _plantStageTracking[plantId] = stageData;
            _plantLastUpdateTime[plantId] = GetCurrentTime();

            if (!_plantUpdateQueue.Contains(plantId))
            {
                _plantUpdateQueue.Add(plantId);
            }

            if (_enableDebugLogs)
            {
                ChimeraLogger.Log($"[PlantGrowthStageManager] Registered plant {plantId} at stage {stageData.CurrentStage}");
            }

            return true;
        }

        /// <summary>
        /// Unregister a plant from stage tracking
        /// </summary>
        public bool UnregisterPlant(string plantId)
        {
            if (string.IsNullOrEmpty(plantId))
            {
                return false;
            }

            bool removed = _plantStageTracking.Remove(plantId);
            _plantLastUpdateTime.Remove(plantId);
            _plantUpdateQueue.Remove(plantId);

            if (removed && _enableDebugLogs)
            {
                ChimeraLogger.Log($"[PlantGrowthStageManager] Unregistered plant {plantId}");
            }

            return removed;
        }

        /// <summary>
        /// Get current stage information for a plant
        /// </summary>
        public PlantStageInfo GetPlantStageInfo(string plantId)
        {
            if (!_plantStageTracking.TryGetValue(plantId, out var stageData))
            {
                return new PlantStageInfo { IsValid = false };
            }

            return new PlantStageInfo
            {
                PlantId = plantId,
                CurrentStage = stageData.CurrentStage,
                StageProgress = stageData.StageProgress,
                ElapsedTimeInStage = stageData.TimeInCurrentStage,
                TotalStageDuration = stageData.StageDuration,
                TimeRemainingInStage = Mathf.Max(0f, stageData.StageDuration - stageData.TimeInCurrentStage),
                NextStage = GetNextStage(stageData.CurrentStage),
                IsStageComplete = stageData.IsStageComplete,
                TransitionHistory = stageData.StageTransitionHistory,
                IsValid = true
            };
        }

        /// <summary>
        /// Force a stage transition for a plant
        /// </summary>
        public bool ForceStageTransition(string plantId, PlantGrowthStage targetStage)
        {
            if (!_plantStageTracking.TryGetValue(plantId, out var stageData))
            {
                ChimeraLogger.LogWarning($"[PlantGrowthStageManager] Cannot force transition for unregistered plant {plantId}");
                return false;
            }

            if (stageData.CurrentStage == targetStage)
            {
                ChimeraLogger.LogWarning($"[PlantGrowthStageManager] Plant {plantId} is already at stage {targetStage}");
                return false;
            }

            return ExecuteStageTransition(stageData, targetStage, forced: true);
        }

        /// <summary>
        /// Check if a plant is experiencing stress
        /// </summary>
        public bool IsPlantStressed(string plantId)
        {
            if (!_plantStageTracking.TryGetValue(plantId, out var stageData))
            {
                return false;
            }

            return IsPlantStressed(stageData);
        }

        /// <summary>
        /// Get all tracked plants
        /// </summary>
        public List<string> GetTrackedPlants()
        {
            return new List<string>(_plantStageTracking.Keys);
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public (int TrackedPlants, int PlantsInQueue, float LastUpdateTime) GetPerformanceStats()
        {
            return (_plantStageTracking.Count, _plantUpdateQueue.Count, _lastCleanupTime);
        }

        // Private helper methods
        private void ProcessPlantUpdates(float currentTime)
        {
            int plantsToProcess = Mathf.Min(_plantUpdateQueue.Count, _maxPlantsPerFrame);

            for (int i = 0; i < plantsToProcess; i++)
            {
                if (_plantUpdateQueue.Count == 0) break;

                string plantId = _plantUpdateQueue[0];
                _plantUpdateQueue.RemoveAt(0);

                if (_plantStageTracking.TryGetValue(plantId, out var stageData))
                {
                    UpdatePlantStage(stageData, currentTime);
                    _plantLastUpdateTime[plantId] = currentTime;
                    _plantsProcessedThisFrame++;
                }

                // Re-queue for next update cycle
                if (_plantStageTracking.ContainsKey(plantId))
                {
                    _plantUpdateQueue.Add(plantId);
                }
            }
        }

        private void UpdatePlantStage(PlantStageData stageData, float currentTime)
        {
            if (stageData == null || stageData.PlantInstance == null) return;

            // Update time tracking
            float deltaTime = currentTime - stageData.StageStartTime;
            stageData.TimeInCurrentStage = deltaTime;
            stageData.StageProgress = Mathf.Clamp01(deltaTime / stageData.StageDuration);

            // Check for stage completion
            if (!stageData.IsStageComplete && stageData.StageProgress >= 1.0f)
            {
                stageData.IsStageComplete = true;

                if (_automaticStageTransitions)
                {
                    var nextStage = GetNextStage(stageData.CurrentStage);
                    if (nextStage != stageData.CurrentStage)
                    {
                        ExecuteStageTransition(stageData, nextStage, forced: false);
                    }
                }
            }

            // Note: PlantInstance.CurrentGrowthStage is read-only, managed by the plant instance itself

            // Environmental monitoring and stress detection
            if (_enableStressDetection)
            {
                bool isStressed = IsPlantStressed(stageData);
                if (isStressed && _enableDebugLogs)
                {
                    ChimeraLogger.LogWarning($"[PlantGrowthStageManager] Plant {stageData.PlantId} is experiencing stress");
                }
            }
        }

        private bool ExecuteStageTransition(PlantStageData stageData, PlantGrowthStage newStage, bool forced)
        {
            if (stageData == null) return false;

            var oldStage = stageData.CurrentStage;

            // Validate transition
            if (!forced && !CanTransitionToStage(oldStage, newStage))
            {
                if (_enableDebugLogs)
                {
                    ChimeraLogger.LogWarning($"[PlantGrowthStageManager] Invalid transition from {oldStage} to {newStage} for plant {stageData.PlantId}");
                }
                return false;
            }

            // Record transition
            var transitionRecord = new StageTransitionRecord
            {
                FromStage = oldStage,
                ToStage = newStage,
                TransitionTime = GetCurrentTime(),
                WasForced = forced,
                Duration = stageData.TimeInCurrentStage
            };

            stageData.StageTransitionHistory.Add(transitionRecord);

            // Update stage data
            stageData.CurrentStage = newStage;
            stageData.StageStartTime = GetCurrentTime();
            stageData.TimeInCurrentStage = 0f;
            stageData.StageDuration = GetStageDuration(newStage, stageData.Genotype);
            stageData.StageProgress = 0f;
            stageData.IsStageComplete = false;

            // Note: PlantInstance.CurrentGrowthStage is read-only, managed by the plant instance itself

            // Log transition event
            if (_enableDebugLogs)
            {
                ChimeraLogger.Log($"[PlantGrowthStageManager] Plant {stageData.PlantId} transitioned from {oldStage} to {newStage} (Forced: {forced})");
            }

            return true;
        }

        private bool CanTransitionToStage(PlantGrowthStage fromStage, PlantGrowthStage toStage)
        {
            // Basic validation - prevent invalid transitions
            if (fromStage == toStage) return false;

            // Check if skipping stages is allowed
            if (!_allowSkippingStages)
            {
                var nextStage = GetNextStage(fromStage);
                return nextStage == toStage;
            }

            return true;
        }

        private PlantGrowthStage GetNextStage(PlantGrowthStage currentStage)
        {
            // Simple linear progression - can be enhanced based on plant genetics
            switch (currentStage)
            {
                case PlantGrowthStage.Seed: return PlantGrowthStage.Germination;
                case PlantGrowthStage.Germination: return PlantGrowthStage.Seedling;
                case PlantGrowthStage.Seedling: return PlantGrowthStage.Vegetative;
                case PlantGrowthStage.Vegetative: return PlantGrowthStage.PreFlowering;
                case PlantGrowthStage.PreFlowering: return PlantGrowthStage.Flowering;
                case PlantGrowthStage.Flowering: return PlantGrowthStage.Harvest;
                case PlantGrowthStage.Harvest: return PlantGrowthStage.Harvest; // Terminal stage
                default: return currentStage;
            }
        }

        private float GetStageDuration(PlantGrowthStage stage, GenotypeDataSO genotype)
        {
            // Base durations in hours - can be modified by genetics
            float baseDuration = stage switch
            {
                PlantGrowthStage.Seed => 24f,
                PlantGrowthStage.Germination => 72f,
                PlantGrowthStage.Seedling => 168f, // 1 week
                PlantGrowthStage.Vegetative => 672f, // 4 weeks
                PlantGrowthStage.PreFlowering => 168f, // 1 week
                PlantGrowthStage.Flowering => 1344f, // 8 weeks
                PlantGrowthStage.Harvest => float.MaxValue, // Terminal
                _ => 168f
            };

            // Apply genetic modifiers if available
            if (genotype != null)
            {
                // This can be enhanced to use genetic traits for duration modification
                baseDuration *= 1.0f; // Placeholder for genetic influence
            }

            return baseDuration;
        }

        private bool IsPlantStressed(PlantStageData stageData)
        {
            if (stageData?.PlantInstance == null) return false;

            // Implement stress detection logic
            // This is a placeholder - real implementation would check:
            // - Environmental conditions
            // - Nutrient levels
            // - Water status
            // - Disease/pest presence
            // - Growth rate anomalies

            return false; // Placeholder
        }

        private void PerformCleanup()
        {
            float currentTime = GetCurrentTime();
            var plantsToRemove = new List<string>();

            foreach (var kvp in _plantStageTracking)
            {
                var plantId = kvp.Key;
                var stageData = kvp.Value;

                // Remove plants that no longer exist or are invalid
                if (stageData?.PlantInstance == null || stageData.PlantInstance == null)
                {
                    plantsToRemove.Add(plantId);
                }
            }

            foreach (var plantId in plantsToRemove)
            {
                UnregisterPlant(plantId);
            }

            if (_enableDebugLogs && plantsToRemove.Count > 0)
            {
                ChimeraLogger.Log($"[PlantGrowthStageManager] Cleaned up {plantsToRemove.Count} invalid plant entries");
            }
        }

        private void LogPerformanceMetrics()
        {
            var stats = GetPerformanceStats();
            ChimeraLogger.Log($"[PlantGrowthStageManager] Performance - Tracked: {stats.TrackedPlants}, Queue: {stats.PlantsInQueue}, Processed this frame: {_plantsProcessedThisFrame}");
        }

        private float GetCurrentTime()
        {
            return Time.time;
        }

        private ITimeManager FindTimeManager()
        {
            // Look for ITimeManager implementation in the scene
            MonoBehaviour[] allComponents = new MonoBehaviour[0]; // TODO: Replace with ServiceContainer.GetAll<MonoBehaviour>()
            foreach (var component in allComponents)
            {
                if (component is ITimeManager timeManager)
                {
                    return timeManager;
                }
            }

            ChimeraLogger.LogWarning("[PlantGrowthStageManager] Could not find ITimeManager implementation");
            return null;
        }

        // IChimeraManager implementation
        public ManagerMetrics GetMetrics()
        {
            return new ManagerMetrics
            {
                ManagerName = ManagerName,
                IsHealthy = ValidateHealth(),
                Performance = IsInitialized ? 1f : 0f,
                ManagedItems = _plantStageTracking.Count,
                Uptime = IsInitialized ? Time.time / 3600f : 0f,
                LastActivity = IsInitialized ? "Tracking Plant Growth" : "Not Initialized"
            };
        }

        public string GetStatus()
        {
            if (!IsInitialized) return "Not Initialized";
            return $"Active - Tracking {_plantStageTracking.Count} plants";
        }

        public bool ValidateHealth()
        {
            return IsInitialized && _plantStageTracking.Count <= _maxTrackedPlants;
        }
    }

    /// <summary>
    /// Record of a stage transition
    /// </summary>
    [System.Serializable]
    public class StageTransitionRecord
    {
        public PlantGrowthStage FromStage;
        public PlantGrowthStage ToStage;
        public float TransitionTime;
        public bool WasForced;
        public float Duration;
    }

    /// <summary>
    /// Complete stage information for external access
    /// </summary>
    public struct PlantStageInfo
    {
        public string PlantId;
        public PlantGrowthStage CurrentStage;
        public float StageProgress;
        public float ElapsedTimeInStage;
        public float TotalStageDuration;
        public float TimeRemainingInStage;
        public PlantGrowthStage NextStage;
        public bool IsStageComplete;
        public List<StageTransitionRecord> TransitionHistory;
        public bool IsValid;
    }

    /// <summary>
    /// Internal data structure for tracking plant stage progression
    /// </summary>
    [System.Serializable]
    public class PlantStageData
    {
        public string PlantId;
        public PlantInstanceSO PlantInstance;
        public GenotypeDataSO Genotype;
        public PlantGrowthStage CurrentStage;
        public float TimeInCurrentStage;
        public float StageStartTime;
        public float StageDuration;
        public float StageProgress;
        public bool IsStageComplete;
        public List<object> EnvironmentalHistory;
        public List<StageTransitionRecord> StageTransitionHistory;
    }
}
