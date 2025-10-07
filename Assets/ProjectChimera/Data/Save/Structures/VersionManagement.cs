using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Data.Save.Structures
{
    /// <summary>
    /// Version management system for save data compatibility
    /// Handles version migration, compatibility checking, and data transformation
    /// </summary>
    public static class VersionManagement
    {
        #region Version Constants

        public const string CurrentVersion = "1.0.0";

        /// <summary>
        /// Parses version string to integer for SaveSystemVersion
        /// </summary>
        public static int ParseVersionNumber(string version)
        {
            // Simple parsing - extract major version number
            var parts = version.Split('.');
            return parts.Length > 0 ? int.Parse(parts[0]) : 1;
        }
        public const string MinimumSupportedVersion = "0.9.0";
        public static readonly DateTime VersionIntroduced = new DateTime(2024, 1, 1);

        #endregion

        #region Version History

        private static readonly Dictionary<string, VersionInfo> VersionHistory = new Dictionary<string, VersionInfo>
        {
            ["0.9.0"] = new VersionInfo
            {
                Version = "0.9.0",
                IntroducedDate = new DateTime(2023, 6, 1),
                Description = "Initial save system implementation",
                MajorChanges = new List<string>
                {
                    "Basic save/load functionality",
                    "Core game state serialization",
                    "Simple data validation"
                },
                IsBreakingChange = false
            },
            ["1.0.0"] = new VersionInfo
            {
                Version = "1.0.0",
                IntroducedDate = new DateTime(2024, 1, 1),
                Description = "Modular DTO-based save system",
                MajorChanges = new List<string>
                {
                    "DTO-based data structures",
                    "Enhanced validation and integrity checking",
                    "Improved compression and performance",
                    "Modular save system architecture"
                },
                IsBreakingChange = true
            }
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if a save version is compatible with the current system
        /// </summary>
        public static bool IsVersionCompatible(string version)
        {
            if (string.IsNullOrEmpty(version))
                return false;

            try
            {
                Version saveVersion = new Version(version);
                Version currentVersion = new Version(CurrentVersion);
                Version minimumVersion = new Version(MinimumSupportedVersion);

                return saveVersion >= minimumVersion && saveVersion <= currentVersion;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets version information for a specific version
        /// </summary>
        public static VersionInfo GetVersionInfo(string version)
        {
            return VersionHistory.TryGetValue(version, out var info) ? info : null;
        }

        /// <summary>
        /// Gets all available versions
        /// </summary>
        public static IEnumerable<string> GetAvailableVersions()
        {
            return VersionHistory.Keys.OrderBy(v => new Version(v));
        }

        /// <summary>
        /// Migrates save data from one version to another
        /// </summary>
        public static MigrationResult MigrateData(SaveGameData data, string targetVersion = null)
        {
            targetVersion ??= CurrentVersion;

            var result = new MigrationResult
            {
                OriginalVersion = data.SaveSystemVersion.ToString(),
                TargetVersion = targetVersion,
                Success = true,
                MigratedData = data
            };

            try
            {
                // If versions match, no migration needed
                if (data.SaveSystemVersion.ToString() == targetVersion)
                {
                    result.RequiredMigration = false;
                    return result;
                }

                result.RequiredMigration = true;

                // Apply migration steps
                var migrationPath = GetMigrationPath(data.SaveSystemVersion.ToString(), targetVersion);

                foreach (var step in migrationPath)
                {
                    step.ApplyMigration(data);
                }

                // Update version information
                if (int.TryParse(targetVersion, out int versionNumber))
                {
                    data.SaveSystemVersion = versionNumber;
                }
                data.SaveTimestamp = DateTime.Now;

                // Validate migrated data
                if (!data.ValidateData())
                {
                    throw new InvalidOperationException("Migration resulted in invalid data");
                }

                result.MigratedData = data;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Migration failed: {ex.Message}";
                result.MigratedData = null;
            }

            return result;
        }

        /// <summary>
        /// Creates a backup before migration
        /// </summary>
        public static SaveGameData CreateBackup(SaveGameData data)
        {
            var backup = data.Clone();
            backup.SlotName = $"{data.SlotName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}";
            backup.Description = $"Backup of {data.SlotName} before migration";
            return backup;
        }

        /// <summary>
        /// Validates data integrity after migration
        /// </summary>
        public static ValidationResult ValidateMigration(SaveGameData originalData, SaveGameData migratedData)
        {
            var result = new ValidationResult();

            try
            {
                // Basic validation
                if (migratedData == null)
                    throw new ArgumentNullException(nameof(migratedData));

                if (!migratedData.ValidateData())
                    throw new InvalidOperationException("Migrated data failed validation");

                // Version check
                if (migratedData.SaveSystemVersion.ToString() != CurrentVersion)
                    throw new InvalidOperationException("Version mismatch after migration");

                // Data integrity checks
                if (migratedData.FacilityData == null)
                    throw new InvalidOperationException("Facility data is missing");

                result.IsValid = true;
                result.Message = "Migration validation successful";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Migration validation failed: {ex.Message}";
                result.ErrorCode = ex.GetType().Name;
            }

            return result;
        }

        #endregion

        #region Private Methods

        private static List<IMigrationStep> GetMigrationPath(string fromVersion, string toVersion)
        {
            var steps = new List<IMigrationStep>();

            // Define migration steps based on version differences
            if (fromVersion == "0.9.0" && toVersion == "1.0.0")
            {
                steps.Add(new MigrateToDTO_v1_0_0());
            }

            return steps;
        }

        #endregion

        #region Migration Steps

        private interface IMigrationStep
        {
            void ApplyMigration(SaveGameData data);
        }

        private class MigrateToDTO_v1_0_0 : IMigrationStep
        {
            public void ApplyMigration(SaveGameData data)
            {
                // Migrate legacy data structures to DTO-based system
                // This would contain the actual migration logic
                ProjectChimera.Shared.SharedLogger.Log("OTHER", "$1", null);

                // Example migration logic (would be more comprehensive in real implementation)
                if (data.FacilityData == null)
                {
                    data.FacilityData = new FacilityStateDTO
                    {
                        FacilityId = "migrated_facility",
                        FacilityName = "Migrated Facility",
                        FacilityType = "Cultivation",
                        FacilityLevel = 1,
                        Position = Vector3.zero,
                        Size = new Vector3(15f, 10f, 10f),
                        IsOperational = true,
                        RoomCount = 1,
                        EquipmentCount = 0,
                        PowerConsumption = 0f,
                        LastUpdate = DateTime.Now
                    };
                }

                // Migrate other core systems
                if (data.ConstructionState == null)
                {
                    data.ConstructionState = new ConstructionSaveState();
                }
                if (data.PlantsData == null)
                {
                    data.PlantsData = new CultivationStateDTO
                    {
                        Plants = new System.Collections.Generic.List<ProjectChimera.Data.Save.Structures.PlantStateDTO>(),
                        TotalPlants = 0,
                        HealthyPlants = 0,
                        FloweringPlants = 0,
                        AverageHealth = 1f,
                        Temperature = 25f,
                        Humidity = 60f,
                        LastUpdate = DateTime.Now
                    };
                }
                if (data.EconomyStateData == null)
                {
                    data.EconomyStateData = new EconomyStateDTO
                    {
                        Currency = 0f,
                        IncomeRate = 0f,
                        ExpenseRate = 0f,
                        ItemCount = 0,
                        LastUpdate = DateTime.Now
                    };
                }
                if (data.ProgressionStateData == null)
                {
                    data.ProgressionStateData = new ProgressionStateDTO
                    {
                        PlayerLevel = 1,
                        Experience = 0f,
                        SkillPoints = 0,
                        AchievementCount = 0,
                        LastUpdate = DateTime.Now
                    };
                }
                if (data.UIData == null)
                {
                    data.UIData = new UIStateDTO
                    {
                        CameraPosition = Vector3.zero,
                        CameraRotation = Vector3.zero,
                        ZoomLevel = 1f,
                        IsPaused = false,
                        LastUpdate = DateTime.Now
                    };
                }
            }
        }

        #endregion

        #region Data Structures

        /// <summary>
        /// Version information structure
        /// </summary>
        [Serializable]
        public class VersionInfo
        {
            public string Version;
            public DateTime IntroducedDate;
            public string Description;
            public List<string> MajorChanges;
            public bool IsBreakingChange;
        }

        /// <summary>
        /// Migration result structure
        /// </summary>
        [Serializable]
        public class MigrationResult
        {
            public bool Success;
            public string ErrorMessage;
            public SaveGameData MigratedData;
            public bool RequiredMigration;
            public string OriginalVersion;
            public string TargetVersion;
            public TimeSpan MigrationDuration;
            public List<string> MigrationSteps;
            public Dictionary<string, object> Metadata;

            public MigrationResult()
            {
                MigrationSteps = new List<string>();
                Metadata = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Validation result structure
        /// </summary>
        [Serializable]
        public class ValidationResult
        {
            public bool IsValid;
            public string Message;
            public string ErrorCode;
            public DateTime ValidationTime;
            public Dictionary<string, object> ValidationDetails;

            public ValidationResult()
            {
                ValidationTime = DateTime.Now;
                ValidationDetails = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Version compatibility information
        /// </summary>
        [Serializable]
        public class CompatibilityInfo
        {
            public string Version;
            public bool IsCompatible;
            public string CompatibilityMessage;
            public List<string> Incompatibilities;
            public List<string> RequiredMigrations;

            public CompatibilityInfo()
            {
                Incompatibilities = new List<string>();
                RequiredMigrations = new List<string>();
            }
        }

    #endregion

    #region DTO Classes for Version Management

    [System.Serializable]
    public class PlantStateDTO
    {
        public string PlantId;
        public string StrainName;
        public UnityEngine.Vector3 Position;
        public float Age;
        public float Health;
        public float GrowthStage;
        public float NutrientLevel = 1f;
        public float WaterLevel = 1f;
        public bool IsHealthy = true;
        public System.DateTime LastUpdate;
    }

    #endregion
}
}
