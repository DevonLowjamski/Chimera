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
    /// REFACTORED: Cost Configuration Manager Coordinator
    /// Single Responsibility: Coordinate configuration management through composed services
    /// Uses ITickable for centralized update management. Reduced from 591 lines using delegation consolidation
    /// </summary>
    public class CostConfigurationManager : MonoBehaviour, ITickable
    {
        [Header("Configuration Management")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _persistConfiguration = true;
        [SerializeField] private string _configFileName = "cost_estimation_config.json";
        [SerializeField] private float _autoSaveInterval = 600f;
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
        [SerializeField] private float _backupInterval = 3600f;
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
                _persistenceManager.UpdatePersistence(_profileManager.GetAllProfiles());
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

                // Wire up events using lambdas for conciseness
                _profileManager.OnProfileCreated += (name, profile) => { OnProfileCreated?.Invoke(name, profile); _persistenceManager?.MarkDirty(); };
                _profileManager.OnProfileUpdated += (name, profile) => { OnProfileUpdated?.Invoke(name, profile); _persistenceManager?.MarkDirty(); };
                _profileManager.OnProfileDeleted += (name) => { OnProfileDeleted?.Invoke(name); _persistenceManager?.MarkDirty(); };
                _profileManager.OnProfileSwitched += OnProfileSwitched;

                _validationManager.OnValidationCompleted += OnValidationCompleted;

                _persistenceManager.OnConfigurationSaved += OnConfigurationSaved;
                _persistenceManager.OnConfigurationLoaded += OnConfigurationLoaded;
                _persistenceManager.OnConfigurationError += OnConfigurationError;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG", "CostConfigurationManager components initialized", this);
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
                        _profileManager.CreateProfile(profile.Key, profile.Value);
                }

                // Ensure we have an active profile
                if (_profileManager.ActiveProfile == null)
                    _profileManager.SetActiveProfile(_defaultProfileName);

                // Validate configuration if enabled
                if (_enableValidation && _validateOnLoad && _profileManager.ActiveProfile != null)
                    _validationManager.ValidateConfiguration(_profileManager.ActiveProfile);

                ResetStats();
                _isInitialized = true;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG", $"Cost Configuration Manager initialized with profile '{ActiveProfileName}'", this);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CONFIG", $"CostConfigurationManager initialization failed: {ex.Message}", this);
            }
        }

        #endregion

        #region Public API - Delegates to Components

        public T GetParameter<T>(string parameterName, T defaultValue = default)
        {
            return _isInitialized && _profileManager?.ActiveProfile != null ? 
                _profileManager.GetParameter(parameterName, defaultValue) : 
                defaultValue;
        }

        public bool SetParameter<T>(string parameterName, T value)
        {
            if (!_isInitialized || _profileManager == null) return false;

            var success = _profileManager.SetParameter(parameterName, value);
            if (success)
            {
                _persistenceManager?.MarkDirty();
                _stats.ParametersModified++;
            }
            return success;
        }

        public bool HasParameter(string parameterName) =>
            _profileManager?.HasParameter(parameterName) ?? false;

        public IEnumerable<string> GetParameterNames() =>
            _profileManager?.GetParameterNames() ?? Enumerable.Empty<string>();

        public bool ValidateParameter<T>(string parameterName, T value) =>
            _validationManager?.ValidateParameter(parameterName, value, _profileManager?.ActiveProfile) ?? true;

        public ValidationResult ValidateConfiguration() =>
            _validationManager?.ValidateConfiguration(_profileManager?.ActiveProfile) ?? new ValidationResult { IsValid = true };

        public bool CreateProfile(string profileName, CostConfigurationProfile profile = null) =>
            _profileManager?.CreateProfile(profileName, profile) ?? false;

        public bool DeleteProfile(string profileName) =>
            _profileManager?.DeleteProfile(profileName) ?? false;

        public bool SetActiveProfile(string profileName) =>
            _profileManager?.SetActiveProfile(profileName) ?? false;

        public CostConfigurationProfile GetProfile(string profileName) =>
            _profileManager?.GetProfile(profileName);

        public IEnumerable<string> GetProfileNames() =>
            _profileManager?.ProfileNames ?? Enumerable.Empty<string>();

        public bool DuplicateProfile(string sourceProfileName, string newProfileName) =>
            _profileManager?.DuplicateProfile(sourceProfileName, newProfileName) ?? false;

        public bool RenameProfile(string oldName, string newName) =>
            _profileManager?.RenameProfile(oldName, newName) ?? false;

        public bool ExportProfile(string profileName, string filePath) =>
            _persistenceManager?.ExportProfile(profileName, _profileManager.GetProfile(profileName), filePath) ?? false;

        public bool ImportProfile(string filePath)
        {
            if (_persistenceManager == null || _profileManager == null) return false;

            var importedProfile = _persistenceManager.ImportProfile(filePath);
            if (importedProfile.profile != null)
            {
                _profileManager.CreateProfile(importedProfile.name, importedProfile.profile);
                return true;
            }
            return false;
        }

        public bool SaveConfiguration()
        {
            if (_profileManager != null && _persistenceManager != null)
                return _persistenceManager.SaveConfiguration(_profileManager.GetAllProfiles());
            return false;
        }

        public bool LoadConfiguration()
        {
            if (_persistenceManager == null || _profileManager == null) return false;

            var loadedProfiles = _persistenceManager.LoadConfiguration();

            // Clear existing profiles and load new ones
            foreach (var profileName in _profileManager.ProfileNames.ToList())
            {
                if (profileName != _defaultProfileName)
                    _profileManager.DeleteProfile(profileName);
            }

            foreach (var profile in loadedProfiles)
                _profileManager.CreateProfile(profile.Key, profile.Value);

            return true;
        }

        public bool CreateBackup()
        {
            if (_profileManager != null && _persistenceManager != null)
                return _persistenceManager.CreateBackup(_profileManager.GetAllProfiles());
            return false;
        }

        public IEnumerable<BackupInfo> GetAvailableBackups() =>
            _persistenceManager?.GetAvailableBackups() ?? Enumerable.Empty<BackupInfo>();

        public bool RestoreFromBackup(string backupId)
        {
            if (_persistenceManager == null || _profileManager == null) return false;

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
                    _profileManager.CreateProfile(profile.Key, profile.Value);

                return true;
            }
            return false;
        }

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
                    SaveConfiguration();

                // Events are automatically unsubscribed when components are garbage collected
                // No need for explicit unsubscription with lambda expressions

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
}
