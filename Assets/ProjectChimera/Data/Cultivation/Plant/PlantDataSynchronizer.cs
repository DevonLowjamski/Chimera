using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Data Synchronizer Coordinator
    /// Single Responsibility: Orchestrate plant data synchronization
    /// BEFORE: 834 lines (massive SRP violation)
    /// AFTER: 4 files <500 lines each (PlantSyncDataStructures, PlantDataValidator, PlantComponentSynchronizer, this coordinator)
    /// </summary>
    [Serializable]
    public class PlantDataSynchronizer
    {
        [Header("Synchronization Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _autoSyncOnUpdate = true;
        [SerializeField] private bool _validateDataIntegrity = true;
        [SerializeField] private float _syncFrequency = 1f; // Seconds

        // PHASE 0: Component-based architecture (SRP)
        private PlantComponentSynchronizer _componentSynchronizer;
        private PlantDataValidator _validator;

        // Serialized data
        [SerializeField] private SerializedPlantData _serializedData;

        // Synchronization state
        [SerializeField] private DateTime _lastSyncTime = DateTime.Now;
        [SerializeField] private bool _isDirty = false;
        [SerializeField] private int _syncVersion = 1;
        private float _timeSinceLastSync = 0f;

        // Validation state
        private List<string> _validationErrors = new List<string>();
        private bool _isDataValid = true;

        // Statistics
        private PlantDataSyncStats _stats;
        private bool _isInitialized = false;

        // Events
        public event Action OnDataSynchronized;
        public event Action<List<string>> OnValidationErrors;
        public event Action<string, object, object> OnDataChanged;
        public event Action<float> OnSyncPerformanceUpdate;

        // Public properties
        public bool IsInitialized => _isInitialized;
        public PlantDataSyncStats Stats => _stats;
        public bool IsDirty => _isDirty;
        public bool IsDataValid => _isDataValid;
        public DateTime LastSyncTime => _lastSyncTime;
        public int SyncVersion => _syncVersion;
        public SerializedPlantData SerializedData => _serializedData;

        /// <summary>
        /// Initialize synchronizer with component references
        /// </summary>
        public void Initialize(
            PlantIdentityManager identity,
            PlantStateCoordinator state,
            PlantResourceHandler resources,
            PlantGrowthProcessor growth,
            PlantHarvestOperator harvest)
        {
            if (_isInitialized) return;

            // Initialize components
            _componentSynchronizer = new PlantComponentSynchronizer();
            _componentSynchronizer.SetComponents(identity, state, resources, growth, harvest);

            _validator = new PlantDataValidator(_enableLogging);

            // Initialize data
            _serializedData = SerializedPlantData.CreateEmpty();
            _stats = PlantDataSyncStats.CreateEmpty();

            // Subscribe to component events
            SubscribeToComponentEvents(identity, state, resources, growth, harvest);

            _isInitialized = true;
            _isDirty = true; // Force initial sync

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Plant Data Synchronizer initialized with component references", null);
            }
        }

        /// <summary>
        /// Synchronize all data from components to serialized fields
        /// </summary>
        public DataSyncResult SyncFromComponents()
        {
            if (!_isInitialized)
            {
                return DataSyncResult.CreateFailure("Synchronizer not initialized");
            }

            var startTime = DateTime.Now;

            try
            {
                // Sync from components
                _componentSynchronizer.SyncAllFromComponents(ref _serializedData, ref _stats);

                // Validate synchronized data
                if (_validateDataIntegrity)
                {
                    var (isValid, errors) = _validator.ValidateData(_serializedData);
                    _isDataValid = isValid;
                    _validationErrors = errors;

                    if (!isValid)
                    {
                        _stats.ValidationFailures++;
                        OnValidationErrors?.Invoke(_validationErrors);
                    }
                    else
                    {
                        _stats.ValidationSuccesses++;
                    }
                }

                _syncVersion++;
                _lastSyncTime = DateTime.Now;
                _isDirty = false;
                _stats.SuccessfulSyncs++;

                var syncTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
                _stats.TotalSyncTime += syncTime;

                OnDataSynchronized?.Invoke();
                OnSyncPerformanceUpdate?.Invoke(syncTime);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Data synchronized from components (v{_syncVersion}, {syncTime:F2}ms)", null);
                }

                return DataSyncResult.CreateSuccess(syncTime, _syncVersion);
            }
            catch (Exception ex)
            {
                _stats.FailedSyncs++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Sync from components failed: {ex.Message}", null);
                }

                return DataSyncResult.CreateFailure($"Sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronize all data from serialized fields to components
        /// </summary>
        public DataSyncResult SyncToComponents()
        {
            if (!_isInitialized)
            {
                return DataSyncResult.CreateFailure("Synchronizer not initialized");
            }

            var startTime = DateTime.Now;

            try
            {
                // Validate before syncing to components
                if (_validateDataIntegrity)
                {
                    var (isValid, errors) = _validator.ValidateData(_serializedData);
                    if (!isValid)
                    {
                        return DataSyncResult.CreateFailure("Data validation failed before sync to components", errors);
                    }
                }

                // Sync to components
                _componentSynchronizer.SyncAllToComponents(_serializedData);

                _syncVersion++;
                _lastSyncTime = DateTime.Now;
                _isDirty = false;
                _stats.SuccessfulSyncs++;

                var syncTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
                _stats.TotalSyncTime += syncTime;

                OnDataSynchronized?.Invoke();
                OnSyncPerformanceUpdate?.Invoke(syncTime);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Data synchronized to components (v{_syncVersion}, {syncTime:F2}ms)", null);
                }

                return DataSyncResult.CreateSuccess(syncTime, _syncVersion);
            }
            catch (Exception ex)
            {
                _stats.FailedSyncs++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Sync to components failed: {ex.Message}", null);
                }

                return DataSyncResult.CreateFailure($"Sync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update synchronizer (call from MonoBehaviour Update)
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isInitialized || !_autoSyncOnUpdate) return;

            _timeSinceLastSync += deltaTime;
            _stats.UpdateCycles++;

            if (_isDirty && _timeSinceLastSync >= _syncFrequency)
            {
                SyncFromComponents();
                _timeSinceLastSync = 0f;
            }
        }

        /// <summary>
        /// Mark data as dirty (needs sync)
        /// </summary>
        public void MarkDirty(string reason = "External change")
        {
            _isDirty = true;
            _stats.DirtyMarks++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Data marked dirty: {reason}", null);
            }
        }

        /// <summary>
        /// Force immediate synchronization
        /// </summary>
        public DataSyncResult ForceSyncNow(bool fromComponents = true)
        {
            return fromComponents ? SyncFromComponents() : SyncToComponents();
        }

        /// <summary>
        /// Get complete plant summary
        /// </summary>
        public CompletePlantSummary GetCompletePlantSummary()
        {
            if (!_isInitialized)
            {
                return new CompletePlantSummary { IsValid = false };
            }

            return _componentSynchronizer.GetCompleteSummary(
                _serializedData,
                _lastSyncTime,
                _syncVersion,
                _isDirty,
                _isDataValid
            );
        }

        /// <summary>
        /// Set synchronization parameters
        /// </summary>
        public void SetSyncParameters(bool autoSync, bool validateData, float frequency)
        {
            _autoSyncOnUpdate = autoSync;
            _validateDataIntegrity = validateData;
            _syncFrequency = Mathf.Max(0.1f, frequency);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Sync parameters updated: Auto={autoSync}, Validate={validateData}, Frequency={frequency}s", null);
            }
        }

        /// <summary>
        /// Export serialized data as JSON
        /// </summary>
        public string ExportSerializedData()
        {
            try
            {
                return JsonUtility.ToJson(_serializedData, true);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Failed to export data: {ex.Message}", null);
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Import serialized data from JSON
        /// </summary>
        public bool ImportSerializedData(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData)) return false;

            try
            {
                _serializedData = JsonUtility.FromJson<SerializedPlantData>(jsonData);
                _isDirty = true; // Mark for sync to components
                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Failed to import data: {ex.Message}", null);
                }
                return false;
            }
        }

        /// <summary>
        /// Force data refresh from components
        /// </summary>
        public void ForceDataRefresh()
        {
            _isDirty = true;
            SyncFromComponents();
        }

        #region Private Methods

        /// <summary>
        /// Subscribe to component change events
        /// </summary>
        private void SubscribeToComponentEvents(
            PlantIdentityManager identity,
            PlantStateCoordinator state,
            PlantResourceHandler resources,
            PlantGrowthProcessor growth,
            PlantHarvestOperator harvest)
        {
            if (identity != null)
            {
                identity.OnIdentityChanged += (oldId, newId) => MarkDirty($"Identity changed: {oldId} -> {newId}");
            }

            if (state != null)
            {
                state.OnGrowthStageChanged += (oldStage, newStage) => MarkDirty($"Growth stage: {oldStage} -> {newStage}");
                state.OnHealthChanged += (health) => MarkDirty($"Health changed: {health:F2}");
            }

            if (resources != null)
            {
                resources.OnWaterLevelChanged += (old, now) => MarkDirty($"Water: {old:F2} -> {now:F2}");
                resources.OnNutrientLevelChanged += (old, now) => MarkDirty($"Nutrients: {old:F2} -> {now:F2}");
            }

            if (growth != null)
            {
                growth.OnGrowthProgressChanged += (old, now) => MarkDirty($"Growth: {old:F2} -> {now:F2}");
            }

            if (harvest != null)
            {
                harvest.OnReadinessChanged += (readiness) => MarkDirty($"Harvest readiness: {readiness:F2}");
            }
        }

        #endregion
    }
}

