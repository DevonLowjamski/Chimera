using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// BASIC: Simple save data validation for Project Chimera's save system.
    /// Focuses on essential save data checking without complex validation rules and migration systems.
    /// </summary>
    public static class SaveDataValidation
    {
        /// <summary>
        /// Validate basic save data
        /// </summary>
        public static ValidationResult ValidateBasicSaveData(BasicSaveData saveData)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check required fields
            if (string.IsNullOrEmpty(saveData.SaveName))
            {
                errors.Add("Save name is required");
            }

            if (string.IsNullOrEmpty(saveData.PlayerName))
            {
                errors.Add("Player name is required");
            }

            if (saveData.PlayerLevel < 1)
            {
                warnings.Add("Player level seems low");
            }

            if (saveData.Currency < 0)
            {
                errors.Add("Currency cannot be negative");
            }

            // Check for reasonable values
            if (saveData.PlayTimeHours < 0)
            {
                errors.Add("Play time cannot be negative");
            }

            // Check data integrity
            if (saveData.PlayerState == null)
            {
                errors.Add("Player state is missing");
            }

            if (saveData.CultivationState == null)
            {
                warnings.Add("Cultivation state is missing - will use defaults");
            }

            if (saveData.ConstructionState == null)
            {
                warnings.Add("Construction state is missing - will use defaults");
            }

            if (saveData.EconomyState == null)
            {
                warnings.Add("Economy state is missing - will use defaults");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                CanLoad = errors.Count == 0,
                NeedsRepair = warnings.Count > 0
            };
        }

        /// <summary>
        /// Validate save data for a specific game version
        /// </summary>
        public static ValidationResult ValidateVersionCompatibility(BasicSaveData saveData, string currentVersion)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrEmpty(saveData.GameVersion))
            {
                warnings.Add("Game version not specified in save data");
                return new ValidationResult
                {
                    IsValid = true,
                    Warnings = warnings,
                    CanLoad = true,
                    NeedsRepair = true
                };
            }

            if (saveData.GameVersion != currentVersion)
            {
                warnings.Add($"Save data version ({saveData.GameVersion}) differs from current version ({currentVersion})");

                // Check if it's a compatible version (simple check)
                if (IsVersionCompatible(saveData.GameVersion, currentVersion))
                {
                    return new ValidationResult
                    {
                        IsValid = true,
                        Warnings = warnings,
                        CanLoad = true,
                        NeedsRepair = true
                    };
                }
                else
                {
                    errors.Add($"Save data version {saveData.GameVersion} is not compatible with current version {currentVersion}");
                    return new ValidationResult
                    {
                        IsValid = false,
                        Errors = errors,
                        Warnings = warnings,
                        CanLoad = false,
                        NeedsRepair = false
                    };
                }
            }

            return new ValidationResult
            {
                IsValid = true,
                CanLoad = true
            };
        }

        /// <summary>
        /// Validate save file integrity
        /// </summary>
        public static ValidationResult ValidateFileIntegrity(string filePath)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(filePath))
            {
                errors.Add("File path is empty");
                return new ValidationResult { IsValid = false, Errors = errors, CanLoad = false };
            }

            if (!System.IO.File.Exists(filePath))
            {
                errors.Add("Save file does not exist");
                return new ValidationResult { IsValid = false, Errors = errors, CanLoad = false };
            }

            try
            {
                // Check file size
                var fileInfo = new System.IO.FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    errors.Add("Save file is empty");
                    return new ValidationResult { IsValid = false, Errors = errors, CanLoad = false };
                }

                if (fileInfo.Length > 50 * 1024 * 1024) // 50MB limit
                {
                    errors.Add("Save file is too large");
                    return new ValidationResult { IsValid = false, Errors = errors, CanLoad = false };
                }

                // Try to read and parse as JSON
                string jsonContent = System.IO.File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    errors.Add("Save file contains no data");
                    return new ValidationResult { IsValid = false, Errors = errors, CanLoad = false };
                }

                // Basic JSON validation
                if (!jsonContent.TrimStart().StartsWith("{"))
                {
                    errors.Add("Save file is not valid JSON format");
                    return new ValidationResult { IsValid = false, Errors = errors, CanLoad = false };
                }
            }
            catch (System.Exception ex)
            {
                errors.Add($"Failed to read save file: {ex.Message}");
                return new ValidationResult { IsValid = false, Errors = errors, CanLoad = false };
            }

            return new ValidationResult
            {
                IsValid = true,
                CanLoad = true
            };
        }

        /// <summary>
        /// Quick validation check
        /// </summary>
        public static bool IsValidSaveData(BasicSaveData saveData)
        {
            if (saveData == null) return false;
            if (string.IsNullOrEmpty(saveData.SaveName)) return false;
            if (string.IsNullOrEmpty(saveData.PlayerName)) return false;
            if (saveData.PlayerLevel < 1) return false;
            if (saveData.Currency < 0) return false;

            return true;
        }

        /// <summary>
        /// Get validation summary
        /// </summary>
        public static ValidationSummary GetValidationSummary(BasicSaveData saveData, string currentVersion)
        {
            var basicResult = ValidateBasicSaveData(saveData);
            var versionResult = ValidateVersionCompatibility(saveData, currentVersion);

            return new ValidationSummary
            {
                BasicValidation = basicResult,
                VersionValidation = versionResult,
                OverallValid = basicResult.IsValid && versionResult.IsValid,
                CanLoad = basicResult.CanLoad && versionResult.CanLoad,
                NeedsRepair = basicResult.NeedsRepair || versionResult.NeedsRepair,
                TotalErrors = basicResult.Errors.Count + versionResult.Errors.Count,
                TotalWarnings = basicResult.Warnings.Count + versionResult.Warnings.Count
            };
        }

        #region Private Methods

        private static bool IsVersionCompatible(string saveVersion, string currentVersion)
        {
            // Simple version compatibility check
            // In a real implementation, this would check version compatibility rules
            try
            {
                var saveParts = saveVersion.Split('.');
                var currentParts = currentVersion.Split('.');

                if (saveParts.Length >= 2 && currentParts.Length >= 2)
                {
                    // Major version must match, minor version can be different
                    return saveParts[0] == currentParts[0];
                }
            }
            catch
            {
                // If parsing fails, assume incompatible
                return false;
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// Basic save data structure
    /// </summary>
    [System.Serializable]
    public class BasicSaveData
    {
        public string SaveName;
        public string PlayerName;
        public int PlayerLevel;
        public float Currency;
        public float PlayTimeHours;
        public string GameVersion;
        public PlayerSaveState PlayerState;
        public CultivationSaveState CultivationState;
        public ConstructionSaveState ConstructionState;
        public EconomySaveState EconomyState;
    }

    /// <summary>
    /// Basic save state classes
    /// </summary>
    [System.Serializable]
    public class PlayerSaveState { public int Level; public float Experience; }
    [System.Serializable]
    public class CultivationSaveState { public int PlantCount; }
    [System.Serializable]
    public class ConstructionSaveState { public int BuildingCount; }
    [System.Serializable]
    public class EconomySaveState { public float Currency; }

    /// <summary>
    /// Validation result
    /// </summary>
    [System.Serializable]
    public class ValidationResult
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        public bool CanLoad = true;
        public bool NeedsRepair = false;
    }

    /// <summary>
    /// Validation summary
    /// </summary>
    [System.Serializable]
    public struct ValidationSummary
    {
        public ValidationResult BasicValidation;
        public ValidationResult VersionValidation;
        public bool OverallValid;
        public bool CanLoad;
        public bool NeedsRepair;
        public int TotalErrors;
        public int TotalWarnings;
    }
}
