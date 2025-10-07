using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Interface for domain-specific save section providers.
    /// Each provider handles serialization and deserialization for a specific game domain.
    /// Enables modular save system with selective loading and domain-specific validation.
    /// </summary>
    public interface ISaveSectionProvider
    {
        /// <summary>
        /// Unique identifier for this save section (e.g., "cultivation", "economy", "construction")
        /// </summary>
        string SectionKey { get; }

        /// <summary>
        /// Human-readable name for this save section
        /// </summary>
        string SectionName { get; }

        /// <summary>
        /// Version of this provider's save format
        /// </summary>
        string SectionVersion { get; }

        /// <summary>
        /// Priority for save/load operations (higher values processed first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether this section is required for the game to function properly
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Whether this section supports incremental/delta saves
        /// </summary>
        bool SupportsIncrementalSave { get; }

        /// <summary>
        /// Estimated memory usage of this section's data (for optimization)
        /// </summary>
        long EstimatedDataSize { get; }

        /// <summary>
        /// List of section keys this provider depends on (must be loaded first)
        /// </summary>
        IReadOnlyList<string> Dependencies { get; }

        /// <summary>
        /// Gather current state data for this section
        /// </summary>
        /// <returns>Serializable data object for this section</returns>
        Task<ISaveSectionData> GatherSectionDataAsync();

        /// <summary>
        /// Apply loaded data to restore this section's state
        /// </summary>
        /// <param name="sectionData">Previously saved section data</param>
        /// <returns>Result indicating success/failure and any errors</returns>
        Task<SaveSectionResult> ApplySectionDataAsync(ISaveSectionData sectionData);

        /// <summary>
        /// Validate section data integrity and compatibility
        /// </summary>
        /// <param name="sectionData">Section data to validate</param>
        /// <returns>Validation result with details about any issues</returns>
        Task<SaveSectionValidation> ValidateSectionDataAsync(ISaveSectionData sectionData);

        /// <summary>
        /// Migrate section data from an older version to current format
        /// </summary>
        /// <param name="oldData">Data from previous version</param>
        /// <param name="fromVersion">Version of the old data</param>
        /// <returns>Migrated data in current format</returns>
        Task<ISaveSectionData> MigrateSectionDataAsync(ISaveSectionData oldData, string fromVersion);

        /// <summary>
        /// Get a lightweight summary of this section's current state (for save slot display)
        /// </summary>
        /// <returns>Summary information for UI display</returns>
        SaveSectionSummary GetSectionSummary();

        /// <summary>
        /// Check if this section has changed since last save (for optimization)
        /// </summary>
        /// <returns>True if section needs to be saved</returns>
        bool HasChanges();

        /// <summary>
        /// Mark section as clean (no changes since last save)
        /// </summary>
        void MarkClean();

        /// <summary>
        /// Reset section to default state (for new games)
        /// </summary>
        Task ResetToDefaultStateAsync();

        /// <summary>
        /// Get supported version migration paths for this section
        /// </summary>
        /// <returns>Dictionary mapping from-version to supported target versions</returns>
        IReadOnlyDictionary<string, IReadOnlyList<string>> GetSupportedMigrations();

        /// <summary>
        /// Perform cleanup operations before save (e.g., remove temporary data)
        /// </summary>
        Task PreSaveCleanupAsync();

        /// <summary>
        /// Perform initialization after load (e.g., rebuild caches)
        /// </summary>
        Task PostLoadInitializationAsync();
    }

    /// <summary>
    /// Base interface for all save section data objects
    /// </summary>
    public interface ISaveSectionData
    {
        /// <summary>
        /// Section identifier this data belongs to
        /// </summary>
        string SectionKey { get; set; }

        /// <summary>
        /// Version of the data format
        /// </summary>
        string DataVersion { get; set; }

        /// <summary>
        /// Timestamp when this data was created
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// Estimated size of this data in bytes
        /// </summary>
        long EstimatedSize { get; }

        /// <summary>
        /// Hash or checksum for data integrity validation
        /// </summary>
        string DataHash { get; set; }

        /// <summary>
        /// Validate this data object's integrity
        /// </summary>
        bool IsValid();

        /// <summary>
        /// Get human-readable summary of this data
        /// </summary>
        string GetSummary();
    }

    /// <summary>
    /// Result of a save section operation
    /// </summary>
    public struct SaveSectionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public TimeSpan Duration { get; set; }
        public long DataSize { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public static SaveSectionResult CreateSuccess(TimeSpan duration = default, long dataSize = 0, Dictionary<string, object> metadata = null)
        {
            return new SaveSectionResult
            {
                Success = true,
                Duration = duration,
                DataSize = dataSize,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }

        public static SaveSectionResult CreateFailure(string errorMessage, Exception exception = null)
        {
            return new SaveSectionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                Metadata = new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Result of save section data validation
    /// </summary>
    public struct SaveSectionValidation
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public bool CanMigrate { get; set; }
        public string RequiredMigrationVersion { get; set; }
        public Dictionary<string, object> ValidationMetadata { get; set; }

        public static SaveSectionValidation CreateValid()
        {
            return new SaveSectionValidation
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                ValidationMetadata = new Dictionary<string, object>()
            };
        }

        public static SaveSectionValidation CreateInvalid(List<string> errors, List<string> warnings = null, bool canMigrate = false, string migrationVersion = null)
        {
            return new SaveSectionValidation
            {
                IsValid = false,
                Errors = errors ?? new List<string>(),
                Warnings = warnings ?? new List<string>(),
                CanMigrate = canMigrate,
                RequiredMigrationVersion = migrationVersion,
                ValidationMetadata = new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Lightweight summary of a save section for UI display
    /// </summary>
    public struct SaveSectionSummary
    {
        public string SectionKey { get; set; }
        public string SectionName { get; set; }
        public string StatusDescription { get; set; }
        public int ItemCount { get; set; }
        public long DataSize { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, string> KeyValuePairs { get; set; }
        public bool HasErrors { get; set; }
        public List<string> ErrorMessages { get; set; }
    }

    /// <summary>
    /// Priority levels for save section processing
    /// </summary>
    public enum SaveSectionPriority
    {
        Lowest = 0,
        Low = 10,
        Normal = 50,
        High = 80,
        Critical = 100
    }
}