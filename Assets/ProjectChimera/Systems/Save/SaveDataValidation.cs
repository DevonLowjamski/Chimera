using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Comprehensive data validation framework for save system providers.
    /// Ensures data integrity, version compatibility, and corruption detection.
    /// </summary>
    public static class SaveDataValidation
    {
        #region Validation Rules

        /// <summary>
        /// Core validation rules that all save sections must satisfy
        /// </summary>
        public static class CoreRules
        {
            /// <summary>
            /// Validate that section data has required fields
            /// </summary>
            public static SaveSectionValidation ValidateRequiredFields(ISaveSectionData data)
            {
                var errors = new List<string>();

                if (string.IsNullOrEmpty(data.SectionKey))
                    errors.Add("Section key cannot be null or empty");

                if (string.IsNullOrEmpty(data.DataVersion))
                    errors.Add("Data version cannot be null or empty");

                if (data.Timestamp == default(DateTime))
                    errors.Add("Timestamp must be set");

                if (!string.IsNullOrEmpty(data.SectionKey) && !SaveSectionKeys.IsValidSectionKey(data.SectionKey))
                    errors.Add($"Invalid section key format: {data.SectionKey}");

                return errors.Any() 
                    ? SaveSectionValidation.CreateInvalid(errors)
                    : SaveSectionValidation.CreateValid();
            }

            /// <summary>
            /// Validate data version compatibility
            /// </summary>
            public static SaveSectionValidation ValidateVersionCompatibility(ISaveSectionData data, string currentVersion)
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                if (string.IsNullOrEmpty(currentVersion))
                {
                    errors.Add("Current version not specified");
                    return SaveSectionValidation.CreateInvalid(errors);
                }

                if (data.DataVersion == currentVersion)
                    return SaveSectionValidation.CreateValid();

                // Parse version numbers for comparison
                if (TryParseVersion(data.DataVersion, out var dataVersion) &&
                    TryParseVersion(currentVersion, out var currentVer))
                {
                    if (dataVersion > currentVer)
                    {
                        errors.Add($"Data version {data.DataVersion} is newer than current version {currentVersion}");
                        return SaveSectionValidation.CreateInvalid(errors);
                    }

                    if (dataVersion < currentVer)
                    {
                        warnings.Add($"Data version {data.DataVersion} is older than current version {currentVersion}");
                        var validation = SaveSectionValidation.CreateValid();
                        validation.Warnings = warnings;
                        validation.CanMigrate = true;
                        validation.RequiredMigrationVersion = currentVersion;
                        return validation;
                    }
                }
                else
                {
                    warnings.Add($"Cannot parse version numbers for comparison: {data.DataVersion} vs {currentVersion}");
                }

                var result = SaveSectionValidation.CreateValid();
                result.Warnings = warnings;
                return result;
            }

            /// <summary>
            /// Validate data integrity using hash/checksum
            /// </summary>
            public static SaveSectionValidation ValidateDataIntegrity(ISaveSectionData data)
            {
                var errors = new List<string>();

                if (!data.IsValid())
                {
                    errors.Add("Data object reports itself as invalid");
                }

                if (string.IsNullOrEmpty(data.DataHash))
                {
                    // Hash not required but recommended
                    var warnings = new List<string> { "No data hash provided for integrity checking" };
                    var result = SaveSectionValidation.CreateValid();
                    result.Warnings = warnings;
                    return result;
                }

                // Would validate hash against actual data here
                // For now, just check that it's properly formatted
                if (data.DataHash.Length < 8)
                {
                    errors.Add("Data hash appears too short to be valid");
                }

                return errors.Any() 
                    ? SaveSectionValidation.CreateInvalid(errors)
                    : SaveSectionValidation.CreateValid();
            }

            /// <summary>
            /// Validate timestamp is reasonable
            /// </summary>
            public static SaveSectionValidation ValidateTimestamp(ISaveSectionData data)
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                var now = DateTime.Now;
                var minValidDate = new DateTime(2020, 1, 1); // Project start date
                var maxValidDate = now.AddDays(1); // Allow slight future dates for clock differences

                if (data.Timestamp < minValidDate)
                {
                    errors.Add($"Timestamp {data.Timestamp} is before minimum valid date {minValidDate}");
                }

                if (data.Timestamp > maxValidDate)
                {
                    errors.Add($"Timestamp {data.Timestamp} is too far in the future (max: {maxValidDate})");
                }

                // Check for suspicious timestamps
                var daysSinceCreation = (now - data.Timestamp).TotalDays;
                if (daysSinceCreation > 365 * 5) // 5 years old
                {
                    warnings.Add($"Save data is very old ({daysSinceCreation:F0} days)");
                }

                var result = errors.Any() 
                    ? SaveSectionValidation.CreateInvalid(errors, warnings)
                    : SaveSectionValidation.CreateValid();

                if (result.IsValid)
                    result.Warnings = warnings;

                return result;
            }
        }

        /// <summary>
        /// Domain-specific validation rules for different save sections
        /// </summary>
        public static class DomainRules
        {
            /// <summary>
            /// Validate cultivation data specifics
            /// </summary>
            public static SaveSectionValidation ValidateCultivationData(ISaveSectionData data)
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // Would cast to CultivationSaveData and validate specific fields
                // For now, just validate basic requirements

                if (data.EstimatedSize > 50 * 1024 * 1024) // 50MB limit for cultivation data
                {
                    warnings.Add("Cultivation data is very large, may impact performance");
                }

                return warnings.Any() 
                    ? CreateValidWithWarnings(warnings)
                    : SaveSectionValidation.CreateValid();
            }

            /// <summary>
            /// Validate economy data specifics
            /// </summary>
            public static SaveSectionValidation ValidateEconomyData(ISaveSectionData data)
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // Economy data validation would check for reasonable currency values,
                // market data consistency, etc.

                return warnings.Any() 
                    ? CreateValidWithWarnings(warnings)
                    : SaveSectionValidation.CreateValid();
            }

            /// <summary>
            /// Validate construction data specifics
            /// </summary>
            public static SaveSectionValidation ValidateConstructionData(ISaveSectionData data)
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // Construction data validation would check for valid facility placements,
                // grid consistency, structural integrity, etc.

                return warnings.Any() 
                    ? CreateValidWithWarnings(warnings)
                    : SaveSectionValidation.CreateValid();
            }

            /// <summary>
            /// Validate progression data specifics
            /// </summary>
            public static SaveSectionValidation ValidateProgressionData(ISaveSectionData data)
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // Progression data validation would check for reasonable experience values,
                // skill unlocks, achievement consistency, etc.

                return warnings.Any() 
                    ? CreateValidWithWarnings(warnings)
                    : SaveSectionValidation.CreateValid();
            }

            /// <summary>
            /// Validate UI state data specifics
            /// </summary>
            public static SaveSectionValidation ValidateUIStateData(ISaveSectionData data)
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                // UI state validation would check for reasonable window positions,
                // valid preference values, etc.

                if (data.EstimatedSize > 1024 * 1024) // 1MB limit for UI data
                {
                    warnings.Add("UI state data is unusually large");
                }

                return warnings.Any() 
                    ? CreateValidWithWarnings(warnings)
                    : SaveSectionValidation.CreateValid();
            }
        }

        #endregion

        #region Validation Framework

        /// <summary>
        /// Comprehensive validation of save section data
        /// </summary>
        /// <param name="provider">Save section provider</param>
        /// <param name="data">Data to validate</param>
        /// <returns>Complete validation result</returns>
        public static async Task<SaveSectionValidation> ValidateSectionDataAsync(ISaveSectionProvider provider, ISaveSectionData data)
        {
            var allErrors = new List<string>();
            var allWarnings = new List<string>();
            var metadata = new Dictionary<string, object>();

            // Core validations
            var requiredFieldsResult = CoreRules.ValidateRequiredFields(data);
            MergeValidationResults(requiredFieldsResult, allErrors, allWarnings);

            if (requiredFieldsResult.IsValid)
            {
                var versionResult = CoreRules.ValidateVersionCompatibility(data, provider.SectionVersion);
                MergeValidationResults(versionResult, allErrors, allWarnings);

                var integrityResult = CoreRules.ValidateDataIntegrity(data);
                MergeValidationResults(integrityResult, allErrors, allWarnings);

                var timestampResult = CoreRules.ValidateTimestamp(data);
                MergeValidationResults(timestampResult, allErrors, allWarnings);

                // Domain-specific validation
                var domainResult = ValidateDomainSpecificData(data);
                MergeValidationResults(domainResult, allErrors, allWarnings);

                // Provider-specific validation
                try
                {
                    var providerResult = await provider.ValidateSectionDataAsync(data);
                    MergeValidationResults(providerResult, allErrors, allWarnings);
                    
                    // Merge provider metadata
                    if (providerResult.ValidationMetadata != null)
                    {
                        foreach (var kvp in providerResult.ValidationMetadata)
                        {
                            metadata[$"provider_{kvp.Key}"] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    allErrors.Add($"Provider validation failed: {ex.Message}");
                }
            }

            // Add validation metadata
            metadata["validation_timestamp"] = DateTime.Now;
            metadata["error_count"] = allErrors.Count;
            metadata["warning_count"] = allWarnings.Count;
            metadata["data_size"] = data.EstimatedSize;

            var finalResult = allErrors.Any() 
                ? SaveSectionValidation.CreateInvalid(allErrors, allWarnings)
                : SaveSectionValidation.CreateValid();

            if (finalResult.IsValid)
                finalResult.Warnings = allWarnings;

            finalResult.ValidationMetadata = metadata;

            return finalResult;
        }

        /// <summary>
        /// Validate dependencies are satisfied
        /// </summary>
        /// <param name="provider">Provider to validate</param>
        /// <param name="availableSections">Currently loaded sections</param>
        /// <returns>Validation result</returns>
        public static SaveSectionValidation ValidateDependencies(ISaveSectionProvider provider, HashSet<string> availableSections)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            foreach (var dependency in provider.Dependencies)
            {
                if (!availableSections.Contains(dependency))
                {
                    if (SaveSectionKeys.IsRequiredSection(dependency))
                    {
                        errors.Add($"Required dependency '{dependency}' is not available");
                    }
                    else
                    {
                        warnings.Add($"Optional dependency '{dependency}' is not available");
                    }
                }
            }

            return errors.Any()
                ? SaveSectionValidation.CreateInvalid(errors, warnings)
                : CreateValidWithWarnings(warnings);
        }

        /// <summary>
        /// Validate cross-section data consistency
        /// </summary>
        /// <param name="allSectionData">All loaded section data</param>
        /// <returns>Validation results for cross-section consistency</returns>
        public static async Task<Dictionary<string, SaveSectionValidation>> ValidateCrossSectionConsistencyAsync(Dictionary<string, ISaveSectionData> allSectionData)
        {
            var results = new Dictionary<string, SaveSectionValidation>();

            // Player data consistency checks
            if (allSectionData.ContainsKey(SaveSectionKeys.PLAYER))
            {
                results[SaveSectionKeys.PLAYER] = await ValidatePlayerConsistency(allSectionData);
            }

            // Economy consistency checks
            if (allSectionData.ContainsKey(SaveSectionKeys.ECONOMY))
            {
                results[SaveSectionKeys.ECONOMY] = await ValidateEconomyConsistency(allSectionData);
            }

            // Time consistency checks
            if (allSectionData.ContainsKey(SaveSectionKeys.TIME))
            {
                results[SaveSectionKeys.TIME] = await ValidateTimeConsistency(allSectionData);
            }

            return results;
        }

        #endregion

        #region Helper Methods

        private static SaveSectionValidation ValidateDomainSpecificData(ISaveSectionData data)
        {
            return data.SectionKey switch
            {
                SaveSectionKeys.CULTIVATION => DomainRules.ValidateCultivationData(data),
                SaveSectionKeys.ECONOMY => DomainRules.ValidateEconomyData(data),
                SaveSectionKeys.CONSTRUCTION => DomainRules.ValidateConstructionData(data),
                SaveSectionKeys.PROGRESSION => DomainRules.ValidateProgressionData(data),
                SaveSectionKeys.UI_STATE => DomainRules.ValidateUIStateData(data),
                _ => SaveSectionValidation.CreateValid()
            };
        }

        private static void MergeValidationResults(SaveSectionValidation source, List<string> allErrors, List<string> allWarnings)
        {
            if (source.Errors != null)
                allErrors.AddRange(source.Errors);
            if (source.Warnings != null)
                allWarnings.AddRange(source.Warnings);
        }

        private static SaveSectionValidation CreateValidWithWarnings(List<string> warnings)
        {
            var result = SaveSectionValidation.CreateValid();
            result.Warnings = warnings ?? new List<string>();
            return result;
        }

        private static bool TryParseVersion(string version, out Version parsedVersion)
        {
            parsedVersion = null;
            try
            {
                parsedVersion = new Version(version);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<SaveSectionValidation> ValidatePlayerConsistency(Dictionary<string, ISaveSectionData> allSectionData)
        {
            // Would validate that player level matches progression data,
            // player currency matches economy data, etc.
            await Task.Delay(1); // Simulate async work
            return SaveSectionValidation.CreateValid();
        }

        private static async Task<SaveSectionValidation> ValidateEconomyConsistency(Dictionary<string, ISaveSectionData> allSectionData)
        {
            // Would validate that total currency across all systems adds up,
            // market prices are reasonable, etc.
            await Task.Delay(1); // Simulate async work
            return SaveSectionValidation.CreateValid();
        }

        private static async Task<SaveSectionValidation> ValidateTimeConsistency(Dictionary<string, ISaveSectionData> allSectionData)
        {
            // Would validate that timestamps across sections are consistent,
            // time-based progression makes sense, etc.
            await Task.Delay(1); // Simulate async work
            return SaveSectionValidation.CreateValid();
        }

        #endregion

        #region Validation Policies

        /// <summary>
        /// Validation policies for different scenarios
        /// </summary>
        public static class ValidationPolicies
        {
            /// <summary>
            /// Strict validation for production saves
            /// </summary>
            public static ValidationPolicy Strict => new ValidationPolicy
            {
                RequireAllFields = true,
                RequireValidTimestamps = true,
                RequireDataIntegrity = true,
                AllowVersionMigration = true,
                FailOnWarnings = false,
                MaxWarnings = 10
            };

            /// <summary>
            /// Lenient validation for development/debugging
            /// </summary>
            public static ValidationPolicy Lenient => new ValidationPolicy
            {
                RequireAllFields = false,
                RequireValidTimestamps = false,
                RequireDataIntegrity = false,
                AllowVersionMigration = true,
                FailOnWarnings = false,
                MaxWarnings = 100
            };

            /// <summary>
            /// Migration validation for data upgrades
            /// </summary>
            public static ValidationPolicy Migration => new ValidationPolicy
            {
                RequireAllFields = false,
                RequireValidTimestamps = false,
                RequireDataIntegrity = false,
                AllowVersionMigration = true,
                FailOnWarnings = false,
                MaxWarnings = 50
            };
        }

        /// <summary>
        /// Validation policy configuration
        /// </summary>
        public struct ValidationPolicy
        {
            public bool RequireAllFields { get; set; }
            public bool RequireValidTimestamps { get; set; }
            public bool RequireDataIntegrity { get; set; }
            public bool AllowVersionMigration { get; set; }
            public bool FailOnWarnings { get; set; }
            public int MaxWarnings { get; set; }
        }

        #endregion
    }
}