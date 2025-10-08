using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Equipment.Degradation.Configuration;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// REFACTORED: Cost Configuration Manager with ITickable - Coordinator using SRP-compliant components
    /// Single Responsibility: Coordinating configuration management through composed services
    /// Uses composition with ConfigurationProfileManager, ConfigurationValidationManager, and ConfigurationPersistenceManager
    /// Uses ITickable for centralized update management
    /// Reduced from 1156 lines to maintain SRP compliance
    /// </summary>
    public class CostConfigurationManager : MonoBehaviour, ITickable
    {
        [Header("Configuration Management")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _persistConfiguration = true;
        [SerializeField] private string _configFileName = "cost_estimation_config.json";
        [SerializeField] private float _autoSaveInterval = 600f; // 10 minutes
        [SerializeField] private bool _validateOnLoad = true;

        [Header("Configuration Profiles")]
        [SerializeField] private bool _enableProfiles = true;
        [SerializeField] private string _defaultProfileName = "Default";
        [SerializeField] private bool _allowProfileSwitching = true;
        [SerializeField] private int _maxProfiles = 10;

        [Header("Parameter Validation")]
        [SerializeField] private bool _enableValidation = true;
        [SerializeField] private bool _enforceConstraints = true;
        [SerializeField] private bool _logValidationWarnings = true;
        [SerializeField] private ValidationMode _validationMode = ValidationMode.Strict;

        [Header("Configuration Backup")]
        [SerializeField] private bool _enableBackups = true;
        [SerializeField] private int _maxBackups = 5;
        [SerializeField] private float _backupInterval = 3600f; // 1 hour
        [SerializeField] private bool _compressBackups = true;

        // Composition: Delegate responsibilities to focused components
        private ConfigurationProfileManager _profileManager;
        private ConfigurationValidationManager _validationManager;
        private ConfigurationPersistenceManager _persistenceManager;

        // Coordinator state
        private bool _isInitialized = false;
        private ConfigurationStats _stats = new ConfigurationStats();

        // Events
        public System.Action<string, CostConfigurationProfile> OnProfileCreated;
        public System.Action<string, CostConfigurationProfile> OnProfileUpdated;
        public System.Action<string> OnProfileDeleted;
        public System.Action<string, string> OnProfileSwitched;
        public System.Action<ValidationResult> OnValidationCompleted;
        public System.Action<string> OnConfigurationSaved;
        public System.Action<string> OnConfigurationLoaded;
        public System.Action<string> OnConfigurationError;

        // Properties
        public bool IsInitialized => _isInitialized;
        public ConfigurationStats Stats => _stats;
        public string ActiveProfileName => _profileManager?.ActiveProfileName ?? string.Empty;
        public CostConfigurationProfile ActiveProfile => _profileManager?.ActiveProfile;
        public bool IsConfigurationDirty => _persistenceManager?.IsDirty ?? false;
        public int ProfileCount => _profileManager?.ProfileCount ?? 0;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CacheManager;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && _isInitialized;

        public void Tick(float deltaTime)
        {
            if (_persistenceManager != null)
            {
                _persistenceManager.UpdatePersistence(_profileManager.GetAllProfiles());
            }
        }

        private void OnEnable()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            try
            {
                // Initialize components using composition
                _profileManager = new ConfigurationProfileManager(_enableLogging, _maxProfiles, _defaultProfileName, _allowProfileSwitching);
                _validationManager = new ConfigurationValidationManager(_enableLogging, _enableValidation, _enforceConstraints, _logValidationWarnings, _validationMode);
                _persistenceManager = new ConfigurationPersistenceManager(_enableLogging, _persistConfiguration, _configFileName, _autoSaveInterval, _enableBackups, _maxBackups, _backupInterval, _compressBackups);

                // Wire up events between components
                _profileManager.OnProfileCreated += OnProfileCreatedInternal;
                _profileManager.OnProfileUpdated += OnProfileUpdatedInternal;
                _profileManager.OnProfileDeleted += OnProfileDeletedInternal;
                _profileManager.OnProfileSwitched += OnProfileSwitchedInternal;

                _validationManager.OnValidationCompleted += OnValidationCompletedInternal;

                _persistenceManager.OnConfigurationSaved += OnConfigurationSavedInternal;
                _persistenceManager.OnConfigurationLoaded += OnConfigurationLoadedInternal;
                _persistenceManager.OnConfigurationError += OnConfigurationErrorInternal;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("CONFIG", "CostConfigurationManager components initialized", this);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CONFIG", $"Failed to initialize CostConfigurationManager components: {ex.Message}", this);
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Initialize all components
                _profileManager.Initialize();
                _persistenceManager.Initialize();

                // Load configuration if persistence is enabled
                if (_persistConfiguration)
                {
                    var loadedProfiles = _persistenceManager.LoadConfiguration();
                    foreach (var profile in loadedProfiles)
                    {
                        _profileManager.CreateProfile(profile.Key, profile.Value);
                    }
                }

                // Ensure we have an active profile
                if (_profileManager.ActiveProfile == null)
                {
                    _profileManager.SetActiveProfile(_defaultProfileName);
                }

                // Validate configuration if enabled
                if (_enableValidation && _validateOnLoad && _profileManager.ActiveProfile != null)
                {
                    _validationManager.ValidateConfiguration(_profileManager.ActiveProfile);
                }

                ResetStats();
                _isInitialized = true;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("CONFIG", $"Cost Configuration Manager initialized with profile '{ActiveProfileName}'", this);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CONFIG", $"CostConfigurationManager initialization failed: {ex.Message}", this);
            }
        }

        #endregion

        #region Event Handlers

        private void OnProfileCreatedInternal(string profileName, CostConfigurationProfile profile)
        {
            OnProfileCreated?.Invoke(profileName, profile);
            _persistenceManager?.MarkDirty();
        }

        private void OnProfileUpdatedInternal(string profileName, CostConfigurationProfile profile)
        {
            OnProfileUpdated?.Invoke(profileName, profile);
            _persistenceManager?.MarkDirty();
        }

        private void OnProfileDeletedInternal(string profileName)
        {
            OnProfileDeleted?.Invoke(profileName);
            _persistenceManager?.MarkDirty();
        }

        private void OnProfileSwitchedInternal(string previousProfile, string newProfile)
        {
            OnProfileSwitched?.Invoke(previousProfile, newProfile);
        }

        private void OnValidationCompletedInternal(ValidationResult result)
        {
            OnValidationCompleted?.Invoke(result);
        }

        private void OnConfigurationSavedInternal(string filePath)
        {
            OnConfigurationSaved?.Invoke(filePath);
        }

        private void OnConfigurationLoadedInternal(string filePath)
        {
            OnConfigurationLoaded?.Invoke(filePath);
        }

        private void OnConfigurationErrorInternal(string error)
        {
            OnConfigurationError?.Invoke(error);
        }

        #endregion

        #region Public API - Delegates to Components

        /// <summary>
        /// Get configuration parameter value
        /// </summary>
        public T GetParameter<T>(string parameterName, T defaultValue = default)
        {
            if (!_isInitialized || _profileManager?.ActiveProfile == null)
                return defaultValue;

            try
            {
                var activeProfile = _profileManager.ActiveProfile;
                if (activeProfile.Parameters.TryGetValue(parameterName, out var value))
                {
                    _stats.ParameterRetrievals++;

                    if (value is T typedValue)
                        return typedValue;

                    // Try to convert if types don't match
                    var converted = Convert.ChangeType(value, typeof(T));
                    return (T)converted;
                }

                _stats.ParameterMisses++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("CONFIG", $"Parameter '{parameterName}' not found, using default value", this);
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                _stats.ParameterErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("CONFIG", $"Error retrieving parameter '{parameterName}': {ex.Message}", this);
                }

                return defaultValue;
            }
        }

        /// <summary>
        /// Set configuration parameter value
        /// </summary>
        public bool SetParameter<T>(string parameterName, T value, bool validate = true)
        {
            if (!_isInitialized || _profileManager?.ActiveProfile == null || string.IsNullOrEmpty(parameterName))
                return false;

            try
            {
                // Validate parameter if enabled
                if (_enableValidation && validate)
                {
                    if (!_validationManager.ValidateParameter(parameterName, value))
                    {
                        _stats.ParameterValidationFailures++;

                        if (_enforceConstraints)
                        {
                            if (_enableLogging)
                            {
                                ChimeraLogger.LogWarning("CONFIG", $"Parameter '{parameterName}' validation failed, value not set", this);
                            }
                            return false;
                        }
                    }
                }

                var activeProfile = _profileManager.ActiveProfile;
                activeProfile.Parameters[parameterName] = value;
                _profileManager.UpdateActiveProfileMetadata();

                _persistenceManager?.MarkDirty();
                _stats.ParameterUpdates++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("CONFIG", $"Parameter '{parameterName}' updated to '{value}'", this);
                }

                return true;
            }
            catch (Exception ex)
            {
                _stats.ParameterErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("CONFIG", $"Error setting parameter '{parameterName}': {ex.Message}", this);
                }

                return false;
            }
        }

        /// <summary>
        /// Create a new configuration profile
        /// </summary>
        public bool CreateProfile(string profileName, CostConfigurationProfile profile = null)
        {
            return _profileManager?.CreateProfile(profileName, profile) ?? false;
        }

        /// <summary>
        /// Set the active configuration profile
        /// </summary>
        public bool SetActiveProfile(string profileName)
        {
            return _profileManager?.SetActiveProfile(profileName) ?? false;
        }

        /// <summary>
        /// Delete a configuration profile
        /// </summary>
        public bool DeleteProfile(string profileName)
        {
            return _profileManager?.DeleteProfile(profileName) ?? false;
        }

        /// <summary>
        /// Get a specific profile by name
        /// </summary>
        public CostConfigurationProfile GetProfile(string profileName)
        {
            return _profileManager?.GetProfile(profileName);
        }

        /// <summary>
        /// Get all available profile names
        /// </summary>
        public IEnumerable<string> GetProfileNames()
        {
            return _profileManager?.ProfileNames ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Validate the current configuration
        /// </summary>
        public ValidationResult ValidateConfiguration()
        {
            if (_profileManager?.ActiveProfile != null)
            {
                return _validationManager?.ValidateConfiguration(_profileManager.ActiveProfile) ?? new ValidationResult { Success = false, Message = "Validation manager not available" };
            }

            return new ValidationResult { Success = false, Message = "No active profile to validate" };
        }

        /// <summary>
        /// Save configuration to disk
        /// </summary>
        public bool SaveConfiguration()
        {
            if (_profileManager != null && _persistenceManager != null)
            {
                return _persistenceManager.SaveConfiguration(_profileManager.GetAllProfiles());
            }

            return false;
        }

        /// <summary>
        /// Load configuration from disk
        /// </summary>
        public bool LoadConfiguration()
        {
            if (_persistenceManager != null && _profileManager != null)
            {
                var loadedProfiles = _persistenceManager.LoadConfiguration();

                // Clear existing profiles and load new ones
                foreach (var profileName in _profileManager.ProfileNames.ToList())
                {
                    if (profileName != _defaultProfileName)
                        _profileManager.DeleteProfile(profileName);
                }

                foreach (var profile in loadedProfiles)
                {
                    _profileManager.CreateProfile(profile.Key, profile.Value);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Create a backup of current configuration
        /// </summary>
        public bool CreateBackup()
        {
            if (_profileManager != null && _persistenceManager != null)
            {
                return _persistenceManager.CreateBackup(_profileManager.GetAllProfiles());
            }

            return false;
        }

        /// <summary>
        /// Get available backups
        /// </summary>
        public IEnumerable<BackupInfo> GetAvailableBackups()
        {
            return _persistenceManager?.GetAvailableBackups() ?? Enumerable.Empty<BackupInfo>();
        }

        /// <summary>
        /// Restore from a specific backup
        /// </summary>
        public bool RestoreFromBackup(string backupId)
        {
            if (_persistenceManager != null && _profileManager != null)
            {
                var restoredProfiles = _persistenceManager.RestoreFromBackup(backupId);
                if (restoredProfiles != null)
                {
                    // Clear existing profiles and load restored ones
                    foreach (var profileName in _profileManager.ProfileNames.ToList())
                    {
                        if (profileName != _defaultProfileName)
                            _profileManager.DeleteProfile(profileName);
                    }

                    foreach (var profile in restoredProfiles)
                    {
                        _profileManager.CreateProfile(profile.Key, profile.Value);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reset configuration to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            if (_profileManager != null)
            {
                // Clear all profiles except default
                foreach (var profileName in _profileManager.ProfileNames.ToList())
                {
                    if (profileName != _defaultProfileName)
                        _profileManager.DeleteProfile(profileName);
                }

                // Switch to default profile
                _profileManager.SetActiveProfile(_defaultProfileName);
                _persistenceManager?.MarkDirty();
            }

            ResetStats();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG", "Configuration reset to defaults", this);
        }

        /// <summary>
        /// Get comprehensive configuration statistics
        /// </summary>
        public ConfigurationStatistics GetConfigurationStatistics()
        {
            return new ConfigurationStatistics
            {
                ProfileStats = _profileManager?.Statistics ?? new ProfileStatistics(),
                ValidationStats = _validationManager?.Statistics ?? new ValidationStatistics(),
                PersistenceStats = _persistenceManager?.Statistics ?? new PersistenceStatistics(),
                GeneralStats = _stats
            };
        }

        #endregion

        #region Statistics

        private void ResetStats()
        {
            _stats = new ConfigurationStats();
            _profileManager?.ResetStatistics();
            _validationManager?.ResetStatistics();
            _persistenceManager?.ResetStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG", "Configuration statistics reset", this);
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            try
            {
                // Save configuration before cleanup if dirty
                if (_persistenceManager?.IsDirty == true)
                {
                    SaveConfiguration();
                }

                // Cleanup event handlers
                if (_profileManager != null)
                {
                    _profileManager.OnProfileCreated -= OnProfileCreatedInternal;
                    _profileManager.OnProfileUpdated -= OnProfileUpdatedInternal;
                    _profileManager.OnProfileDeleted -= OnProfileDeletedInternal;
                    _profileManager.OnProfileSwitched -= OnProfileSwitchedInternal;
                }

                if (_validationManager != null)
                {
                    _validationManager.OnValidationCompleted -= OnValidationCompletedInternal;
                }

                if (_persistenceManager != null)
                {
                    _persistenceManager.OnConfigurationSaved -= OnConfigurationSavedInternal;
                    _persistenceManager.OnConfigurationLoaded -= OnConfigurationLoadedInternal;
                    _persistenceManager.OnConfigurationError -= OnConfigurationErrorInternal;
                }

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG", "CostConfigurationManager cleanup completed", this);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CONFIG", $"Error during CostConfigurationManager cleanup: {ex.Message}", this);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// General configuration statistics
    /// </summary>
    [System.Serializable]
    public class ConfigurationStats
    {
        public int ParameterRetrievals = 0;
        public int ParameterUpdates = 0;
        public int ParameterMisses = 0;
        public int ParameterErrors = 0;
        public int ParameterValidationFailures = 0;
        public DateTime LastParameterAccess = DateTime.MinValue;
    }

    /// <summary>
    /// Comprehensive configuration statistics
    /// </summary>
    [System.Serializable]
    public struct ConfigurationStatistics
    {
        public ProfileStatistics ProfileStats;
        public ValidationStatistics ValidationStats;
        public PersistenceStatistics PersistenceStats;
        public ConfigurationStats GeneralStats;
    }

    #endregion
}
