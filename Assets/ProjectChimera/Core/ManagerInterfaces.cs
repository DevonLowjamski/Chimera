using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Environment;
using ProjectChimera.Shared;
// using ProjectChimera.Data.Progression; // Removed - namespace deleted during cleanup
// using ProjectChimera.Systems.SpeedTree; // Removed to prevent circular dependency

namespace ProjectChimera.Core
{
    /// <summary>
    /// Core manager interfaces for dependency injection
    /// These interfaces define contracts for Project Chimera's manager system
    /// </summary>

    /// <summary>
    /// Time speed level options for game time scaling
    /// </summary>
    public enum TimeSpeedLevel
    {
        Slow = 0,      // 0.5x speed
        Normal = 1,    // 1x speed
        Fast = 2,      // 2x speed
        VeryFast = 3,  // 4x speed
        Maximum = 4    // 8x speed
    }

    /// <summary>
    /// Time display format options for UI systems
    /// </summary>
    public enum TimeDisplayFormat
    {
        Adaptive,      // Automatically choose best format based on duration
        Compact,       // Shortest possible format (e.g., "2h 15m")
        Detailed,      // Include seconds when relevant (e.g., "2h 15m 30s")
        Precise        // Always show seconds (e.g., "2h 15m 30s")
    }

    /// <summary>
    /// Interface for listening to time scale changes
    /// </summary>
    public interface ITimeScaleListener
    {
        void OnTimeScaleChanged(float previousScale, float newScale);
    }

    /// <summary>
    /// Interface for listening to speed penalty changes
    /// </summary>
    public interface ISpeedPenaltyListener
    {
        void OnSpeedPenaltyChanged(TimeSpeedLevel speedLevel, float penaltyMultiplier);
    }

    /// <summary>
    /// Interface for listening to offline progression events
    /// </summary>
    public interface IOfflineProgressionListener
    {
        void OnOfflineProgressionCalculated(float offlineHours);
    }

    /// <summary>
    /// Comprehensive time display data for UI systems
    /// </summary>
    [System.Serializable]
    public struct TimeDisplayData
    {
        /// <summary>
        /// Current accelerated game time since session start
        /// </summary>
        public System.TimeSpan GameTime;

        /// <summary>
        /// Real-world time elapsed in current session
        /// </summary>
        public System.TimeSpan RealTime;

        /// <summary>
        /// Total game time since world creation
        /// </summary>
        public System.TimeSpan TotalGameTime;

        /// <summary>
        /// Current discrete speed level
        /// </summary>
        public TimeSpeedLevel CurrentSpeedLevel;

        /// <summary>
        /// Current time scale multiplier
        /// </summary>
        public float CurrentTimeScale;

        /// <summary>
        /// Ratio of accelerated game time to real time (e.g., 2.1 means 2.1 hours of game time per 1 hour real time)
        /// </summary>
        public float TimeEfficiencyRatio;

        /// <summary>
        /// Whether time progression is currently paused
        /// </summary>
        public bool IsPaused;

        /// <summary>
        /// Whether current speed level applies a quality penalty
        /// </summary>
        public bool HasSpeedPenalty;

        /// <summary>
        /// Current penalty/bonus multiplier for simulation quality
        /// </summary>
        public float PenaltyMultiplier;

        /// <summary>
        /// User-friendly speed level display string (e.g., "2x (Fast)")
        /// </summary>
        public string SpeedLevelDisplay;

        /// <summary>
        /// User-friendly penalty description (e.g., "-15% quality penalty")
        /// </summary>
        public string PenaltyDescription;
    }

    /// <summary>
    /// Interface for Time Manager - handles game time, pausing, and temporal calculations
    /// </summary>
    public interface ITimeManager : IChimeraManager
    {
        TimeSpeedLevel CurrentSpeedLevel { get; }
        float CurrentTimeScale { get; }
        bool IsTimePaused { get; }

        void Pause();
        void Resume();
        void SetSpeedLevel(TimeSpeedLevel newSpeedLevel);
        void IncreaseSpeedLevel();
        void DecreaseSpeedLevel();
        void ResetSpeedLevel();
    }

    /// <summary>
    /// Interface for Data Manager - handles save/load operations and data persistence
    /// </summary>
    public interface IDataManager : IChimeraManager
    {
        bool HasSaveData { get; }
        string CurrentSaveFile { get; }

        void SaveGame(string saveFileName = null);
        void LoadGame(string saveFileName = null);
        void AutoSave();
        void DeleteSave(string saveFileName);
        IEnumerable<string> GetSaveFiles();
    }

    /// <summary>
    /// Interface for Event Manager - handles game-wide event communication
    /// </summary>
    public interface IEventManager : IChimeraManager
    {
        void Subscribe<T>(Action<T> callback) where T : class;
        void Unsubscribe<T>(Action<T> callback) where T : class;
        void Publish<T>(T eventData) where T : class;
        void PublishImmediate<T>(T eventData) where T : class;
        int GetSubscriberCount<T>() where T : class;
    }

    /// <summary>
    /// Interface for Settings Manager - handles game configuration and preferences
    /// </summary>
    public interface ISettingsManager : IChimeraManager
    {
        T GetSetting<T>(string key, T defaultValue = default);
        void SetSetting<T>(string key, T value);
        bool HasSetting(string key);
        void SaveSettings();
        void LoadSettings();
        void ResetToDefaults();
        IEnumerable<string> GetAllSettingKeys();
    }

    /// <summary>
    /// Interface for Plant Manager - handles plant lifecycle and cultivation
    /// </summary>
    public interface IPlantManager : IChimeraManager
    {
        int TotalPlantCount { get; }
        int HealthyPlantCount { get; }
        int MaturePlantCount { get; }

        GameObject CreatePlant(Vector3 position, object strain = null);
        void RemovePlant(GameObject plant);
        void UpdatePlant(GameObject plant, float deltaTime);
        IEnumerable<GameObject> GetAllPlants();
        IEnumerable<GameObject> GetPlantsInRadius(Vector3 center, float radius);
        IEnumerable<GameObject> GetPlantsByStrain(object strain);
        IEnumerable<GameObject> GetPlantsByGrowthStage(object stage);
    }

    /// <summary>
    /// Interface for Genetics Manager - handles breeding, genetics, and strain management
    /// </summary>
    public interface IGeneticsManager : IChimeraManager
    {
        IEnumerable<object> AvailableStrains { get; }
        int DiscoveredTraitsCount { get; }

        object BreedPlants(object parent1, object parent2);
        object GenerateGenotype(object strain);
        ChimeraScriptableObject ExpressPhenotype(object genotype, object conditions);
        void DiscoverTrait(string traitId);
        bool IsTraitDiscovered(string traitId);
        float CalculateBreedingSuccess(object parent1, object parent2);
    }

    /// <summary>
    /// Interface for Environmental Manager - handles climate control and environmental conditions
    /// </summary>
    public interface IEnvironmentalManager : IChimeraManager
    {
        object CurrentConditions { get; }
        float Temperature { get; set; }
        float Humidity { get; set; }
        float CO2Level { get; set; }
        float LightIntensity { get; set; }

        void SetTargetConditions(object conditions);
        void UpdateEnvironment(float deltaTime);
        object GetConditionsAtPosition(Vector3 position);
        void RegisterEnvironmentalZone(object zone);
        void UnregisterEnvironmentalZone(object zone);

        // Additional methods for compatibility
        ZoneEnvironmentDTO GetZoneEnvironment(string zoneName);
        float GetAverageTemperature();
        float GetAverageHumidity();
    }

    /// <summary>
    /// Interface for Progression Manager - handles player progression, skills, and achievements
    /// </summary>
    public interface IProgressionManager : IChimeraManager
    {
        int PlayerLevel { get; }
        float CurrentExperience { get; }
        float ExperienceToNextLevel { get; }
        int SkillPoints { get; }
        int UnlockedAchievements { get; }

        void AddExperience(float amount, string source = null);
        void AddSkillPoints(int amount, string source = null);
        void SpendSkillPoints(int amount, string reason = null);
        void UnlockSkill(string skillId);
        void UnlockAchievement(string achievementId);
        bool IsSkillUnlocked(string skillId);
        bool IsAchievementUnlocked(string achievementId);
        IEnumerable<string> GetUnlockedSkills();
        IEnumerable<string> GetUnlockedAchievements();
    }

    /// <summary>
    /// Interface for Research Manager - simplified after research system cleanup
    /// </summary>
    public interface IResearchManager : IChimeraManager
    {
        float CurrentProjectProgress { get; }
        bool HasActiveResearch { get; }

        void AddResearchProgress(float amount);
        void CompleteCurrentProject();
    }

    /// <summary>
    /// Interface for Economy Manager - handles financial systems and market dynamics
    /// </summary>
    public interface IEconomyManager : IChimeraManager
    {
        float PlayerMoney { get; }
        float TotalRevenue { get; }
        float TotalExpenses { get; }
        float NetProfit { get; }

        bool CanAfford(float amount);
        void AddMoney(float amount, string source = null);
        void SpendMoney(float amount, string reason = null);
        void ProcessTransaction(float amount, string description);
        IEnumerable<object> GetTransactionHistory(int count = 10);
        float GetMarketPrice(string itemId);
        void UpdateMarketPrices();
    }

    /// <summary>
    /// Interface for UI Manager - handles user interface coordination
    /// </summary>
    public interface IUIManager : IChimeraManager
    {
        bool IsUIOpen { get; }
        string CurrentPanel { get; }

        void ShowPanel(string panelId);
        void HidePanel(string panelId);
        void TogglePanel(string panelId);
        bool IsPanelOpen(string panelId);
        void ShowNotification(string message, float duration = 3f);
        void ShowDialog(string title, string message, System.Action onConfirm = null);
        void UpdateUI();
    }

    /// <summary>
    /// Interface for Audio Manager - handles music, sound effects, and audio settings
    /// </summary>
    public interface IAudioManager : IChimeraManager
    {
        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        float SFXVolume { get; set; }
        bool IsMuted { get; set; }

        void PlayMusic(string musicId, bool loop = true);
        void StopMusic();
        void PlaySFX(string sfxId, Vector3 position = default);
        void PlaySFX(string sfxId, float volume = 1f);
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSFXVolume(float volume);
    }

    /// <summary>
    /// Interface for Schematic Library Panel - handles schematic library UI
    /// </summary>
    public interface ISchematicLibraryPanel
    {
        ChimeraScriptableObject SelectedSchematic { get; }
        LibraryViewMode CurrentViewMode { get; }

        System.Action<ChimeraScriptableObject> SchematicSelected { get; set; }
        System.Action<ChimeraScriptableObject> SchematicApplied { get; set; }
        System.Action<ChimeraScriptableObject> SchematicDeleted { get; set; }
        System.Action<string> SearchQueryChanged { get; set; }
        System.Action<LibraryViewMode> ViewModeChanged { get; set; }

        void Show();
        void Hide();
        void AddSchematic(ChimeraScriptableObject schematic);
        void RemoveSchematic(ChimeraScriptableObject schematic);
    }

    /// <summary>
    /// Interface for Grid Placement Controller - handles construction placement logic
    /// </summary>
    public interface IGridPlacementController
    {
        bool IsPlacementMode { get; }
        bool IsInPlacementMode { get; }  // Alternative property name for compatibility
        bool CanPlaceAt(Vector3Int position);

        System.Action<ChimeraScriptableObject> OnSchematicApplied { get; set; }

        void EnterPlacementMode(ChimeraScriptableObject schematic);
        void ExitPlacementMode();
        void PlaceSchematic(ChimeraScriptableObject schematic, Vector3Int position);
        bool ValidatePlacement(ChimeraScriptableObject schematic, Vector3Int position);
    }

    /// <summary>
    /// Interface for Construction Palette Manager - handles construction palette UI
    /// </summary>
    public interface IConstructionPaletteManager
    {
        bool IsPaletteVisible { get; }
        PaletteTab CurrentTab { get; }

        System.Action<bool> OnPaletteVisibilityChanged { get; set; }
        System.Action<PaletteTab> OnActiveTabChanged { get; set; }
        System.Action<ChimeraScriptableObject> OnSchematicApplied { get; set; }

        void ShowPalette();
        void HidePalette();
        void TogglePalette();
        void SwitchToTab(PaletteTab tab);
        void AddSchematic(ChimeraScriptableObject schematic);
        void RemoveSchematic(ChimeraScriptableObject schematic);
    }

    /// <summary>
    /// Interface for Schematic Asset Service - handles schematic asset loading
    /// Replaces Resources.LoadAll anti-pattern with proper dependency injection
    /// </summary>
    public interface ISchematicAssetService
    {
        ChimeraScriptableObject[] GetAllSchematics();
        ChimeraScriptableObject GetSchematic(string schematicId);
        IEnumerable<ChimeraScriptableObject> GetSchematicsByCategory(string category);
        bool TryGetSchematic(string schematicId, out ChimeraScriptableObject schematic);
    }

    /// <summary>
    /// Interface for Update Orchestrator - handles centralized update management
    /// Part of Phase 0.5 Central Update Bus implementation
    /// </summary>
    public interface IUpdateOrchestrator : IChimeraManager
    {
        void RegisterTickable(object tickable);
        void UnregisterTickable(object tickable);
        void RegisterFixedTickable(object fixedTickable);
        void UnregisterFixedTickable(object fixedTickable);
        void RegisterLateTickable(object lateTickable);
        void UnregisterLateTickable(object lateTickable);
        object GetStatistics();
        void ClearAll();
    }

    /// <summary>
    /// Interface for Grid System - handles grid-based positioning and validation
    /// </summary>
    public interface IGridSystem
    {
        Vector3Int WorldToGrid(Vector3 worldPosition);
        Vector3 GridToWorld(Vector3Int gridPosition);
        Vector3 GridToWorldPosition(Vector3Int gridPosition);  // Alternative method name for compatibility
        bool IsValidGridPosition(Vector3Int gridPosition);
        bool IsOccupied(Vector3Int gridPosition);
        void OccupyPosition(Vector3Int gridPosition, GameObject occupant);
        void FreePosition(Vector3Int gridPosition);
    }

    /// <summary>
    /// Interface for Interactive Facility Constructor - handles facility construction
    /// </summary>
    public interface IInteractiveFacilityConstructor
    {
        bool IsConstructing { get; }
        GameObject StartConstruction(Vector3Int gridPosition, ChimeraScriptableObject schematic);
        void CompleteConstruction(string constructionId);
        void CancelConstruction(string constructionId);
        bool CanConstruct(Vector3Int gridPosition, ChimeraScriptableObject schematic);

        // Extended methods for ConstructionManager compatibility
        void StartPlacement(object template);
        void CancelPlacement();

        // Events for construction operations
        System.Action<object> OnObjectPlaced { get; set; }
        System.Action<object> OnObjectRemoved { get; set; }
        System.Action<string> OnError { get; set; }
    }

    /// <summary>
    /// Interface for Construction Cost Manager - handles construction cost calculations
    /// </summary>
    public interface IConstructionCostManager
    {
        float CalculateCost(ChimeraScriptableObject schematic);
        bool CanAffordConstruction(ChimeraScriptableObject schematic);
        void ProcessPayment(float amount, string description);
        float GetTotalCosts();
        float GetAvailableFunds();

        // Additional methods for construction management
        bool CheckResourceAvailability(object template);
        ICostEstimate CreateCostEstimate(string projectId, object template);
        object CreateProjectBudget(string projectId, ICostEstimate costEstimate, float totalCost);
        void AllocateResources(string projectId, object template);
        float TotalCostEstimate { get; }

        // Extended methods for ConstructionManager compatibility
        void ConsumeResources(object template);
        bool CanAfford(object template, float availableFunds);
        float GetQuickCostEstimate(object template);
        float TotalBudgetAllocated { get; }
        float TotalBudgetSpent { get; }
    }

    /// <summary>
    /// Base data structures used by manager interfaces
    /// </summary>

    /// <summary>
    /// Interface for cost estimates returned by construction cost manager
    /// </summary>
    public interface ICostEstimate
    {
        float TotalCost { get; }
        string ProjectId { get; }
    }

    // Renamed to avoid conflicts with ProjectChimera.Data.Economy.Transaction
    public class CoreTransaction
    {
        public DateTime Timestamp { get; set; }
        public float Amount { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Type { get; set; } // Simplified from TransactionType enum for core interfaces
    }

    // NOTE: Core TransactionType removed to avoid ambiguity with data-layer types.
    // Use explicit types from `ProjectChimera.Data.Economy` or `ProjectChimera.Data.Economy.Market` in systems.

    public enum ResearchCategory
    {
        Genetics,
        Environment,
        Equipment,
        Processing,
        Business,
        Automation
    }

    // PlantGrowthStage now lives in ProjectChimera.Data.Shared to keep data types centralized.

    public class EnvironmentalZone
    {
        public string Id { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Size { get; set; }
        public object Conditions { get; set; }
        public float Priority { get; set; }
    }

    /// <summary>
    /// UI-related enums for dependency injection interfaces
    /// These are referenced from their original UI namespace locations
    /// </summary>

    /// <summary>
    /// Configuration for time management behavior
    /// </summary>
    [UnityEngine.CreateAssetMenu(fileName = "Time Config", menuName = "Project Chimera/Core/Time Config")]
    public class TimeConfigSO : ChimeraConfigSO
    {
        [UnityEngine.Header("Time Speed Settings")]
        public TimeSpeedLevel DefaultSpeedLevel = TimeSpeedLevel.Normal;

        [UnityEngine.Header("Risk/Reward System")]
        [UnityEngine.Tooltip("Enable speed-based quality penalties/bonuses")]
        public bool EnableSpeedPenalties = true;

        [UnityEngine.Tooltip("Multiplier for penalty severity (1.0 = default, >1.0 = harsher penalties)")]
        [UnityEngine.Range(0.1f, 2.0f)]
        public float PenaltySeverityMultiplier = 1.0f;

        [UnityEngine.Header("Offline Progression")]
        public bool EnableOfflineProgression = true;

        [UnityEngine.Range(1.0f, 168.0f)] // 1 hour to 1 week
        public float MaxOfflineHours = 72.0f;

        [UnityEngine.Range(0.1f, 10.0f)]
        public float OfflineProgressionMultiplier = 0.5f;

        [UnityEngine.Header("UI Display Settings")]
        [UnityEngine.Tooltip("Show both game time and real time in UI")]
        public bool ShowCombinedTimeDisplay = true;

        [UnityEngine.Tooltip("Include time efficiency ratio in displays")]
        public bool ShowTimeEfficiencyRatio = true;

        [UnityEngine.Tooltip("Show penalty information in time displays")]
        public bool ShowPenaltyInformation = true;

        [UnityEngine.Tooltip("Time format for UI displays")]
        public TimeDisplayFormat TimeFormat = TimeDisplayFormat.Adaptive;

        [UnityEngine.Header("Performance")]
        public bool EnableFrameTimeTracking = true;
        public bool EnableTimeDebugLogging = false;

        [UnityEngine.Range(30, 120)]
        public int FrameHistorySize = 60;
    }
}

