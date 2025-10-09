using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// REFACTORED: Configuration Profile Manager - Focused profile lifecycle management
    /// Single Responsibility: Managing configuration profiles, switching, and profile metadata
    /// Extracted from CostConfigurationManager for better SRP compliance
    /// </summary>
    public class ConfigurationProfileManager
    {
        private readonly bool _enableLogging;
        private readonly int _maxProfiles;
        private readonly string _defaultProfileName;
        private readonly bool _allowProfileSwitching;

        // Profile storage
        private readonly Dictionary<string, CostConfigurationProfile> _configProfiles = new Dictionary<string, CostConfigurationProfile>();
        private CostConfigurationProfile _activeProfile;
        private string _activeProfileName;

        // Profile statistics
        private ProfileStatistics _profileStats = new ProfileStatistics();

        // Events
        public event System.Action<string, CostConfigurationProfile> OnProfileCreated;
        public event System.Action<string, CostConfigurationProfile> OnProfileUpdated;
        public event System.Action<string> OnProfileDeleted;
        public event System.Action<string, string> OnProfileSwitched;

        public ConfigurationProfileManager(bool enableLogging = false, int maxProfiles = 10,
                                         string defaultProfileName = "Default", bool allowProfileSwitching = true)
        {
            _enableLogging = enableLogging;
            _maxProfiles = maxProfiles;
            _defaultProfileName = defaultProfileName;
            _allowProfileSwitching = allowProfileSwitching;
        }

        // Properties
        public string ActiveProfileName => _activeProfileName;
        public CostConfigurationProfile ActiveProfile => _activeProfile;
        public int ProfileCount => _configProfiles.Count;
        public ProfileStatistics Statistics => _profileStats;
        public IEnumerable<string> ProfileNames => _configProfiles.Keys;

        #region Profile Lifecycle Management

        /// <summary>
        /// Initialize with default profile
        /// </summary>
        public void Initialize()
        {
            CreateDefaultProfile();
            SetActiveProfile(_defaultProfileName);

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_PROFILE", "Configuration profile manager initialized", null);
        }

        /// <summary>
        /// Create a new configuration profile
        /// </summary>
        public bool CreateProfile(string profileName, CostConfigurationProfile profile = null)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PROFILE", "Cannot create profile with empty name", null);
                return false;
            }

            if (_configProfiles.ContainsKey(profileName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PROFILE", $"Profile '{profileName}' already exists", null);
                return false;
            }

            if (_configProfiles.Count >= _maxProfiles)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PROFILE", $"Maximum profiles ({_maxProfiles}) reached", null);
                return false;
            }

            var newProfile = profile ?? CreateDefaultProfileStructure();
            newProfile.Name = profileName;
            newProfile.CreatedAt = DateTime.Now;
            newProfile.LastModified = DateTime.Now;

            _configProfiles[profileName] = newProfile;
            _profileStats.TotalProfilesCreated++;

            OnProfileCreated?.Invoke(profileName, newProfile);

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_PROFILE", $"Created profile '{profileName}'", null);

            return true;
        }

        /// <summary>
        /// Set the active configuration profile
        /// </summary>
        public bool SetActiveProfile(string profileName)
        {
            if (!_allowProfileSwitching && _activeProfile != null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PROFILE", "Profile switching is disabled", null);
                return false;
            }

            if (!_configProfiles.TryGetValue(profileName, out var profile))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PROFILE", $"Profile '{profileName}' not found", null);
                return false;
            }

            var previousProfile = _activeProfileName;
            _activeProfile = profile;
            _activeProfileName = profileName;
            _profileStats.ProfileSwitches++;

            OnProfileSwitched?.Invoke(previousProfile, profileName);

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_PROFILE", $"Switched to profile '{profileName}'", null);

            return true;
        }

        /// <summary>
        /// Delete a configuration profile
        /// </summary>
        public bool DeleteProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return false;

            if (!_configProfiles.ContainsKey(profileName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PROFILE", $"Profile '{profileName}' not found for deletion", null);
                return false;
            }

            // Prevent deletion of active profile
            if (profileName == _activeProfileName)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PROFILE", "Cannot delete active profile", null);
                return false;
            }

            // Prevent deletion of default profile
            if (profileName == _defaultProfileName && _configProfiles.Count == 1)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PROFILE", "Cannot delete the last remaining profile", null);
                return false;
            }

            _configProfiles.Remove(profileName);
            _profileStats.TotalProfilesDeleted++;

            OnProfileDeleted?.Invoke(profileName);

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_PROFILE", $"Deleted profile '{profileName}'", null);

            return true;
        }

        /// <summary>
        /// Get a specific profile by name
        /// </summary>
        public CostConfigurationProfile GetProfile(string profileName)
        {
            return _configProfiles.TryGetValue(profileName, out var profile) ? profile : null;
        }

        /// <summary>
        /// Get all profiles
        /// </summary>
        public Dictionary<string, CostConfigurationProfile> GetAllProfiles()
        {
            return new Dictionary<string, CostConfigurationProfile>(_configProfiles);
        }

        /// <summary>
        /// Update active profile metadata
        /// </summary>
        public void UpdateActiveProfileMetadata()
        {
            if (_activeProfile != null)
            {
                _activeProfile.LastModified = DateTime.Now;
                OnProfileUpdated?.Invoke(_activeProfileName, _activeProfile);
            }
        }

        /// <summary>
        /// Clone a profile
        /// </summary>
        public bool CloneProfile(string sourceProfileName, string newProfileName)
        {
            if (!_configProfiles.TryGetValue(sourceProfileName, out var sourceProfile))
                return false;

            var clonedProfile = CloneProfileStructure(sourceProfile);
            return CreateProfile(newProfileName, clonedProfile);
        }

        #endregion

        #region Profile Creation Helpers

        /// <summary>
        /// Create the default profile
        /// </summary>
        private void CreateDefaultProfile()
        {
            if (!_configProfiles.ContainsKey(_defaultProfileName))
            {
                var defaultProfile = CreateDefaultProfileStructure();
                defaultProfile.Name = _defaultProfileName;
                defaultProfile.IsDefault = true;
                _configProfiles[_defaultProfileName] = defaultProfile;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PROFILE", $"Created default profile '{_defaultProfileName}'", null);
            }
        }

        /// <summary>
        /// Create default profile structure with base parameters
        /// </summary>
        private CostConfigurationProfile CreateDefaultProfileStructure()
        {
            var profile = new CostConfigurationProfile
            {
                Parameters = new Dictionary<string, object>(),
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                Version = "1.0"
            };

            // Add default parameters
            profile.Parameters["BaseCost"] = 100.0f;
            profile.Parameters["LaborRatePerHour"] = 50.0f;
            profile.Parameters["MaterialMarkup"] = 1.2f;
            profile.Parameters["UrgencyMultiplier"] = 1.5f;
            profile.Parameters["ComplexityFactor"] = 1.0f;
            profile.Parameters["SeasonalAdjustment"] = 1.0f;
            profile.Parameters["MinimumCost"] = 25.0f;
            profile.Parameters["MaximumCost"] = 10000.0f;

            return profile;
        }

        /// <summary>
        /// Clone a profile structure
        /// </summary>
        private CostConfigurationProfile CloneProfileStructure(CostConfigurationProfile source)
        {
            var cloned = new CostConfigurationProfile
            {
                Name = source.Name + "_Copy",
                Parameters = new Dictionary<string, object>(source.Parameters),
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                Version = source.Version,
                IsDefault = false
            };

            return cloned;
        }

        #endregion

        #region Parameter Management (Delegating to Active Profile)

        /// <summary>
        /// Get parameter from active profile
        /// </summary>
        public T GetParameter<T>(string parameterName, T defaultValue)
        {
            if (_activeProfile == null)
                return defaultValue;

            return _activeProfile.GetParameter(parameterName, defaultValue);
        }

        /// <summary>
        /// Set parameter in active profile
        /// </summary>
        public bool SetParameter<T>(string parameterName, T value)
        {
            if (_activeProfile == null) return false;

            _activeProfile.SetParameter(parameterName, value);
            UpdateActiveProfileMetadata();
            return true;
        }

        /// <summary>
        /// Check if active profile has parameter
        /// </summary>
        public bool HasParameter(string parameterName)
        {
            return _activeProfile?.HasParameter(parameterName) ?? false;
        }

        /// <summary>
        /// Get all parameter names from active profile
        /// </summary>
        public IEnumerable<string> GetParameterNames()
        {
            return _activeProfile?.Parameters.Keys ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Duplicate an existing profile
        /// </summary>
        public bool DuplicateProfile(string sourceProfileName, string newProfileName)
        {
            return CloneProfile(sourceProfileName, newProfileName);
        }

        /// <summary>
        /// Rename an existing profile
        /// </summary>
        public bool RenameProfile(string oldName, string newName)
        {
            if (!_configProfiles.TryGetValue(oldName, out var profile))
                return false;

            if (_configProfiles.ContainsKey(newName))
                return false;

            _configProfiles.Remove(oldName);
            profile.Name = newName;
            profile.LastModified = DateTime.Now;
            _configProfiles[newName] = profile;

            if (_activeProfileName == oldName)
                _activeProfileName = newName;

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_PROFILE", $"Renamed profile '{oldName}' to '{newName}'", null);

            return true;
        }

        #endregion

        #region Statistics and Utilities

        /// <summary>
        /// Reset profile statistics
        /// </summary>
        public void ResetStatistics()
        {
            _profileStats = new ProfileStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_PROFILE", "Profile statistics reset", null);
        }

        /// <summary>
        /// Get profile usage statistics
        /// </summary>
        public ProfileUsageInfo? GetProfileUsage(string profileName)
        {
            if (!_configProfiles.TryGetValue(profileName, out var profile))
                return null;

            return new ProfileUsageInfo
            {
                ProfileName = profileName,
                CreatedAt = profile.CreatedAt,
                LastModified = profile.LastModified,
                ParameterCount = profile.Parameters.Count,
                IsActive = profileName == _activeProfileName,
                IsDefault = profile.IsDefault
            };
        }

        /// <summary>
        /// Validate profile integrity
        /// </summary>
        public ProfileValidationResult ValidateProfile(string profileName)
        {
            if (!_configProfiles.TryGetValue(profileName, out var profile))
            {
                return new ProfileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Profile not found"
                };
            }

            var result = new ProfileValidationResult { IsValid = true };

            // Basic validation checks
            if (string.IsNullOrEmpty(profile.Name))
            {
                result.IsValid = false;
                result.ValidationErrors.Add("Profile name is empty");
            }

            if (profile.Parameters == null || profile.Parameters.Count == 0)
            {
                result.IsValid = false;
                result.ValidationErrors.Add("Profile has no parameters");
            }

            return result;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Configuration profile data structure
    /// </summary>
    [System.Serializable]
    public class CostConfigurationProfile
    {
        public string Name;
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();
        public DateTime CreatedAt;
        public DateTime LastModified;
        public string Version = "1.0";
        public bool IsDefault = false;

        /// <summary>
        /// Get parameter value with default fallback
        /// </summary>
        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Set parameter value
        /// </summary>
        public void SetParameter(string key, object value)
        {
            Parameters[key] = value;
            LastModified = DateTime.Now;
        }

        /// <summary>
        /// Check if parameter exists
        /// </summary>
        public bool HasParameter(string key)
        {
            return Parameters.ContainsKey(key);
        }
    }

    /// <summary>
    /// Profile statistics tracking
    /// </summary>
    [System.Serializable]
    public class ProfileStatistics
    {
        public int TotalProfilesCreated = 0;
        public int TotalProfilesDeleted = 0;
        public int ProfileSwitches = 0;
        public DateTime LastProfileOperation = DateTime.MinValue;
    }

    /// <summary>
    /// Profile usage information
    /// </summary>
    [System.Serializable]
    public struct ProfileUsageInfo
    {
        public string ProfileName;
        public DateTime CreatedAt;
        public DateTime LastModified;
        public int ParameterCount;
        public bool IsActive;
        public bool IsDefault;
    }

    /// <summary>
    /// Profile validation result
    /// </summary>
    [System.Serializable]
    public class ProfileValidationResult
    {
        public bool IsValid = true;
        public string ErrorMessage = string.Empty;
        public List<string> ValidationErrors = new List<string>();
    }

    #endregion
}
