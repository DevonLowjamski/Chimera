using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Sync Data Structures
    /// Single Responsibility: Define all plant synchronization data types
    /// Extracted from PlantSyncConfigurationManager (736 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Sync direction
    /// </summary>
    public enum SyncDirection
    {
        FromComponentsToData,
        FromDataToComponents,
        Bidirectional
    }

    /// <summary>
    /// Plant sync configuration
    /// </summary>
    [Serializable]
    public struct PlantSyncConfiguration
    {
        [Header("Sync Settings")]
        public bool AutoSyncEnabled;
        public float SyncFrequency;
        public bool ValidateDataIntegrity;
        public bool EnableLogging;
        public SyncDirection DefaultSyncDirection;

        [Header("Batching")]
        public int BatchSize;
        public bool EnableBatching;

        [Header("Performance")]
        public float OperationTimeoutSeconds;
        public int MaxRetryAttempts;
        public float RetryDelayMultiplier;
        public bool EnablePerformanceTracking;
        public float PerformanceAlertThreshold;

        /// <summary>
        /// Create default configuration
        /// </summary>
        public static PlantSyncConfiguration CreateDefault()
        {
            return new PlantSyncConfiguration
            {
                AutoSyncEnabled = true,
                SyncFrequency = 1.0f,
                ValidateDataIntegrity = true,
                EnableLogging = false,
                DefaultSyncDirection = SyncDirection.FromComponentsToData,
                BatchSize = 10,
                EnableBatching = true,
                OperationTimeoutSeconds = 5.0f,
                MaxRetryAttempts = 3,
                RetryDelayMultiplier = 2.0f,
                EnablePerformanceTracking = true,
                PerformanceAlertThreshold = 100.0f
            };
        }
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    [Serializable]
    public struct ConfigurationValidationResult
    {
        public bool IsValid;
        public List<string> Errors;
        public List<string> Warnings;
        public DateTime ValidationTime;

        /// <summary>
        /// Create successful validation
        /// </summary>
        public static ConfigurationValidationResult Success()
        {
            return new ConfigurationValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                ValidationTime = DateTime.Now
            };
        }

        /// <summary>
        /// Create failed validation
        /// </summary>
        public static ConfigurationValidationResult Failure(List<string> errors, List<string> warnings = null)
        {
            return new ConfigurationValidationResult
            {
                IsValid = false,
                Errors = errors ?? new List<string>(),
                Warnings = warnings ?? new List<string>(),
                ValidationTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Configuration update result
    /// </summary>
    [Serializable]
    public struct ConfigurationUpdateResult
    {
        public bool Success;
        public string ErrorMessage;
        public PlantSyncConfiguration? PreviousConfiguration;
        public ConfigurationValidationResult ValidationResult;

        /// <summary>
        /// Create successful update
        /// </summary>
        public static ConfigurationUpdateResult CreateSuccess(PlantSyncConfiguration previous)
        {
            return new ConfigurationUpdateResult
            {
                Success = true,
                PreviousConfiguration = previous,
                ValidationResult = ConfigurationValidationResult.Success()
            };
        }

        /// <summary>
        /// Create failed update
        /// </summary>
        public static ConfigurationUpdateResult Failure(string errorMessage)
        {
            return new ConfigurationUpdateResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Configuration statistics
    /// </summary>
    [Serializable]
    public struct ConfigurationStats
    {
        public int ConfigurationUpdates;
        public int ParameterUpdates;
        public int ProfilesCreated;
        public int ProfilesSaved;
        public int ProfilesDeleted;
        public int ProfileSwitches;
        public int ValidationAttempts;
        public int ConfigurationResets;
        public int ConfigurationSaves;
        public int ConfigurationLoads;
        public int SaveFailures;
        public int LoadFailures;

        /// <summary>
        /// Reset statistics
        /// </summary>
        public void Reset()
        {
            ConfigurationUpdates = 0;
            ParameterUpdates = 0;
            ProfilesCreated = 0;
            ProfilesSaved = 0;
            ProfilesDeleted = 0;
            ProfileSwitches = 0;
            ValidationAttempts = 0;
            ConfigurationResets = 0;
            ConfigurationSaves = 0;
            ConfigurationLoads = 0;
            SaveFailures = 0;
            LoadFailures = 0;
        }
    }

    /// <summary>
    /// Configuration summary
    /// </summary>
    [Serializable]
    public struct ConfigurationSummary
    {
        public PlantSyncConfiguration CurrentConfiguration;
        public string ActiveProfile;
        public List<string> AvailableProfiles;
        public bool IsDirty;
        public DateTime LastConfigChange;
        public ConfigurationValidationResult ValidationResult;
        public ConfigurationStats Stats;
        public bool IsInitialized;

        /// <summary>
        /// Create configuration summary
        /// </summary>
        public static ConfigurationSummary Create(
            PlantSyncConfiguration config,
            string activeProfile,
            List<string> profiles,
            bool isDirty,
            DateTime lastChange,
            ConfigurationValidationResult validation,
            ConfigurationStats stats,
            bool isInitialized)
        {
            return new ConfigurationSummary
            {
                CurrentConfiguration = config,
                ActiveProfile = activeProfile,
                AvailableProfiles = profiles,
                IsDirty = isDirty,
                LastConfigChange = lastChange,
                ValidationResult = validation,
                Stats = stats,
                IsInitialized = isInitialized
            };
        }
    }

    /// <summary>
    /// Serializable profiles container
    /// </summary>
    [Serializable]
    public struct SerializableProfiles
    {
        public Dictionary<string, PlantSyncConfiguration> Profiles;

        /// <summary>
        /// Create profiles container
        /// </summary>
        public static SerializableProfiles Create(Dictionary<string, PlantSyncConfiguration> profiles)
        {
            return new SerializableProfiles
            {
                Profiles = profiles ?? new Dictionary<string, PlantSyncConfiguration>()
            };
        }
    }

    /// <summary>
    /// Serialized plant data structure for persistence
    /// </summary>
    [Serializable]
    public struct SerializedPlantData
    {
        // Identity
        public string PlantID;
        public string PlantName;
        public string StrainName;
        public string GenotypeName;
        public string ParentPlantID;
        public int GenerationNumber;
        public DateTime CreationDate;

        // State
        public PlantGrowthStage CurrentGrowthStage;
        public float AgeInDays;
        public float DaysInCurrentStage;
        public float OverallHealth;
        public float Vigor;
        public float StressLevel;
        public float MaturityLevel;
        public float CurrentHeight;
        public float CurrentWidth;
        public float LeafArea;
        public Vector3 WorldPosition;

        // Resources
        public float WaterLevel;
        public float NutrientLevel;
        public float EnergyReserves;
        public DateTime LastWatering;
        public DateTime LastFeeding;
        public DateTime LastTraining;

        // Growth
        public float GrowthProgress;
        public float DailyGrowthRate;
        public float BiomassAccumulation;
        public float RootDevelopmentRate;
        public float CalculatedMaxHeight;
        public float GeneticVigorModifier;

        // Harvest
        public float HarvestReadiness;
        public float EstimatedYield;
        public float EstimatedPotency;
        public DateTime OptimalHarvestDate;
        public bool IsHarvested;

        public static SerializedPlantData CreateEmpty()
        {
            return new SerializedPlantData
            {
                PlantID = string.Empty,
                PlantName = "New Plant",
                CreationDate = DateTime.Now,
                LastWatering = DateTime.Now,
                LastFeeding = DateTime.Now,
                LastTraining = DateTime.Now,
                OptimalHarvestDate = DateTime.Now.AddDays(90),
                WaterLevel = 1f,
                NutrientLevel = 1f,
                EnergyReserves = 1f
            };
        }
    }

    /// <summary>
    /// Plant data synchronization statistics
    /// </summary>
    [Serializable]
    public struct PlantDataSyncStats
    {
        public int UpdateCycles;
        public int SuccessfulSyncs;
        public int FailedSyncs;
        public int DirtyMarks;
        public int ValidationSuccesses;
        public int ValidationFailures;
        public int IdentitySyncs;
        public int StateSyncs;
        public int ResourceSyncs;
        public int GrowthSyncs;
        public int HarvestSyncs;
        public float TotalSyncTime;

        public static PlantDataSyncStats CreateEmpty()
        {
            return new PlantDataSyncStats
            {
                UpdateCycles = 0,
                SuccessfulSyncs = 0,
                FailedSyncs = 0,
                DirtyMarks = 0,
                ValidationSuccesses = 0,
                ValidationFailures = 0,
                IdentitySyncs = 0,
                StateSyncs = 0,
                ResourceSyncs = 0,
                GrowthSyncs = 0,
                HarvestSyncs = 0,
                TotalSyncTime = 0f
            };
        }
    }

    /// <summary>
    /// Data synchronization result
    /// </summary>
    [Serializable]
    public struct DataSyncResult
    {
        public bool Success;
        public string ErrorMessage;
        public DateTime SyncTime;
        public float SyncDuration;

        public static DataSyncResult CreateSuccess(float duration)
        {
            return new DataSyncResult
            {
                Success = true,
                SyncTime = DateTime.Now,
                SyncDuration = duration
            };
        }

        public static DataSyncResult CreateSuccess(float duration, int version)
        {
            return new DataSyncResult
            {
                Success = true,
                SyncTime = DateTime.Now,
                SyncDuration = duration
            };
        }

        public static DataSyncResult CreateSuccess(SerializedPlantData data, float duration)
        {
            return new DataSyncResult
            {
                Success = true,
                SyncTime = DateTime.Now,
                SyncDuration = duration
            };
        }

        public static DataSyncResult CreateFailure(string error)
        {
            return new DataSyncResult
            {
                Success = false,
                ErrorMessage = error,
                SyncTime = DateTime.Now
            };
        }

        public static DataSyncResult CreateFailure(string error, List<string> validationErrors)
        {
            var errorMessage = error;
            if (validationErrors != null && validationErrors.Count > 0)
            {
                errorMessage += ": " + string.Join(", ", validationErrors);
            }

            return new DataSyncResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                SyncTime = DateTime.Now
            };
        }

        public static DataSyncResult CreateFailure(string error, Exception exception)
        {
            return new DataSyncResult
            {
                Success = false,
                ErrorMessage = $"{error}: {exception?.Message}",
                SyncTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Complete plant summary
    /// </summary>
    [Serializable]
    public struct CompletePlantSummary
    {
        public bool IsValid;
        public SyncInfo SyncInfo;
        public SerializedPlantData Data;
        public object Identity;
        public object State;
        public object Resources;
        public object Growth;
        public object Harvest;
    }

    /// <summary>
    /// Sync information
    /// </summary>
    [Serializable]
    public struct SyncInfo
    {
        public DateTime LastSync;
        public DateTime LastSyncTime; // Alias for compatibility
        public bool IsDirty;
        public bool IsDataValid;
        public int SyncVersion;
    }
}
