using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProjectChimera.Core;
using ProjectChimera.Data.UI;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Manager responsible for saving and loading UI state across sessions.
    /// Handles persistence of UI preferences, layouts, and configurations.
    /// </summary>
    public class UIStateManager : DIChimeraManager
    {
        [Header("State Configuration")]
        [SerializeField] private UIStatePersistenceSO _persistenceConfig;
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _autoInitialize = true;
        
        [Header("Runtime State")]
        [SerializeField] private bool _isInitialized = false;
        [SerializeField] private float _lastSaveTime = 0f;
        [SerializeField] private int _pendingStateChanges = 0;
        
        // State storage
        private Dictionary<string, UIStateData> _currentStates;
        private Dictionary<string, System.DateTime> _stateTimestamps;
        private Dictionary<string, int> _stateVersions;
        private Queue<UIStateOperation> _pendingOperations;
        
        // Autosave
        private float _autosaveTimer = 0f;
        private bool _isDirty = false;
        
        // Events
        public System.Action<string, UIStateData> OnStateLoaded;
        public System.Action<string, UIStateData> OnStateSaved;
        public System.Action<string> OnStateRemoved;
        public System.Action OnAllStatesLoaded;
        public System.Action OnAllStatesSaved;
        
        // Properties
        public UIStatePersistenceSO PersistenceConfig => _persistenceConfig;
        public bool IsInitialized => _isInitialized;
        public int StateCount => _currentStates?.Count ?? 0;
        public int PendingStateChanges => _pendingStateChanges;
        public bool IsDirty => _isDirty;
        public float LastSaveTime => _lastSaveTime;
        
        protected override void OnManagerInitialize()
        {
            
            InitializeStateStorage();
            
            if (_persistenceConfig != null && _autoInitialize)
            {
                LoadAllStates();
            }
            
            SetupAutosave();
            _isInitialized = true;
            
            // LogInfo("UI State Manager initialized successfully");
        }
        
        protected override void OnManagerShutdown()
        {
            SaveAllStates();
            ClearAllStates();
            
            // LogInfo("UI State Manager shutdown completed");
        }
        
        /// <summary>
        /// Initialize state storage containers
        /// </summary>
        private void InitializeStateStorage()
        {
            _currentStates = new Dictionary<string, UIStateData>();
            _stateTimestamps = new Dictionary<string, System.DateTime>();
            _stateVersions = new Dictionary<string, int>();
            _pendingOperations = new Queue<UIStateOperation>();
        }
        
        /// <summary>
        /// Setup autosave system
        /// </summary>
        private void SetupAutosave()
        {
            if (_persistenceConfig != null && _persistenceConfig.EnableAutosave)
            {
                _autosaveTimer = 0f;
                
                // Register for application events
                Application.focusChanged += OnApplicationFocusChanged;
                
                if (_persistenceConfig.SaveOnApplicationPause)
                {
                    // Application.pauseStateChanged += OnApplicationPauseChanged; // pauseStateChanged doesn't exist in Unity
                }
            }
        }
        
        /// <summary>
        /// Save UI state for specific element
        /// </summary>
        public void SaveState(string elementId, UIStateCategory category, object stateData)
        {
            if (!_isInitialized || _persistenceConfig == null)
                return;
                
            if (!_persistenceConfig.ShouldPersist(elementId, category))
                return;
                
            var stateKey = _persistenceConfig.GetStorageKey(elementId, category);
            var uiState = new UIStateData
            {
                ElementId = elementId,
                Category = category,
                Data = stateData,
                Timestamp = System.DateTime.Now,
                Version = GetNextVersion(stateKey)
            };
            
            _currentStates[stateKey] = uiState;
            _stateTimestamps[stateKey] = uiState.Timestamp;
            _stateVersions[stateKey] = uiState.Version;
            
            _isDirty = true;
            _pendingStateChanges++;
            
            // Immediate save for critical states
            var scope = _persistenceConfig.GetPersistenceScope(elementId);
            if (scope != null && scope.Priority >= 9)
            {
                SaveStateImmediate(stateKey, uiState);
            }
            // else
            // {
                QueueSaveOperation(stateKey, uiState);
            // }
            
            OnStateSaved?.Invoke(elementId, uiState);
            
            if (_enableDebugLogging)
            {
                LogInfo($"UI State saved: {elementId} ({category})");
            }
        }
        
        /// <summary>
        /// Load UI state for specific element
        /// </summary>
        public T LoadState<T>(string elementId, UIStateCategory category, T defaultValue = default(T))
        {
            if (!_isInitialized || _persistenceConfig == null)
                return defaultValue;
                
            var stateKey = _persistenceConfig.GetStorageKey(elementId, category);
            
            // Check in-memory cache first
            if (_currentStates.TryGetValue(stateKey, out var cachedState))
            {
                try
                {
                    return (T)cachedState.Data;
                }
                catch
                {
                    LogWarning($"Failed to cast cached state data for {elementId}");
                    return defaultValue;
                }
            }
            
            // Load from storage
            var loadedState = LoadStateFromStorage(stateKey);
            if (loadedState != null)
            {
                _currentStates[stateKey] = loadedState;
                _stateTimestamps[stateKey] = loadedState.Timestamp;
                _stateVersions[stateKey] = loadedState.Version;
                
                OnStateLoaded?.Invoke(elementId, loadedState);
                
                try
                {
                    return (T)loadedState.Data;
                }
                catch
                {
                    LogWarning($"Failed to cast loaded state data for {elementId}");
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Remove UI state for specific element
        /// </summary>
        public void RemoveState(string elementId, UIStateCategory category)
        {
            if (!_isInitialized || _persistenceConfig == null)
                return;
                
            var stateKey = _persistenceConfig.GetStorageKey(elementId, category);
            
            _currentStates.Remove(stateKey);
            _stateTimestamps.Remove(stateKey);
            _stateVersions.Remove(stateKey);
            
            RemoveStateFromStorage(stateKey);
            
            OnStateRemoved?.Invoke(elementId);
            
            if (_enableDebugLogging)
            {
                LogInfo($"UI State removed: {elementId} ({category})");
            }
        }
        
        /// <summary>
        /// Check if state exists for element
        /// </summary>
        public bool HasState(string elementId, UIStateCategory category)
        {
            if (!_isInitialized || _persistenceConfig == null)
                return false;
                
            var stateKey = _persistenceConfig.GetStorageKey(elementId, category);
            return _currentStates.ContainsKey(stateKey) || StateExistsInStorage(stateKey);
        }
        
        /// <summary>
        /// Get state timestamp
        /// </summary>
        public System.DateTime GetStateTimestamp(string elementId, UIStateCategory category)
        {
            if (!_isInitialized || _persistenceConfig == null)
                return System.DateTime.MinValue;
                
            var stateKey = _persistenceConfig.GetStorageKey(elementId, category);
            return _stateTimestamps.TryGetValue(stateKey, out var timestamp) ? timestamp : System.DateTime.MinValue;
        }
        
        /// <summary>
        /// Save all current states
        /// </summary>
        public void SaveAllStates()
        {
            if (!_isInitialized || _persistenceConfig == null)
                return;
                
            foreach (var kvp in _currentStates)
            {
                SaveStateImmediate(kvp.Key, kvp.Value);
            }
            
            ProcessPendingOperations();
            
            _isDirty = false;
            _pendingStateChanges = 0;
            _lastSaveTime = Time.time;
            
            OnAllStatesSaved?.Invoke();
            
            LogInfo($"All UI states saved ({_currentStates.Count} states)");
        }
        
        /// <summary>
        /// Load all states from storage
        /// </summary>
        public void LoadAllStates()
        {
            if (!_isInitialized || _persistenceConfig == null)
                return;
                
            var loadedCount = 0;
            
            try
            {
                switch (_persistenceConfig.StorageType)
                {
                    case UIStateStorageType.PlayerPrefs:
                        loadedCount = LoadAllStatesFromPlayerPrefs();
                        break;
                        
                    case UIStateStorageType.FileSystem:
                        loadedCount = LoadAllStatesFromFileSystem();
                        break;
                        
                    default:
                        LogWarning($"Storage type {_persistenceConfig.StorageType} not implemented");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to load UI states: {ex.Message}");
            }
            
            OnAllStatesLoaded?.Invoke();
            
            LogInfo($"Loaded {loadedCount} UI states from storage");
        }
        
        /// <summary>
        /// Clear all states
        /// </summary>
        public void ClearAllStates()
        {
            _currentStates.Clear();
            _stateTimestamps.Clear();
            _stateVersions.Clear();
            _pendingOperations.Clear();
            
            ClearAllStatesFromStorage();
            
            _isDirty = false;
            _pendingStateChanges = 0;
            
            LogInfo("All UI states cleared");
        }
        
        /// <summary>
        /// Save state with UIStateData object (compatibility method)
        /// </summary>
        public void SaveState(string stateKey, UIStateData stateData)
        {
            if (!_isInitialized)
            {
                LogWarning("UI State Manager not initialized");
                return;
            }
            
            if (string.IsNullOrEmpty(stateKey) || stateData == null)
            {
                LogWarning("Invalid state key or data");
                return;
            }
            
            SaveState(stateData.ElementId ?? stateKey, stateData.Category, stateData.Data);
        }
        
        /// <summary>
        /// Load state with UIStateData return (compatibility method)
        /// </summary>
        public UIStateData LoadState(string stateKey)
        {
            if (!_isInitialized)
            {
                LogWarning("UI State Manager not initialized");
                return null;
            }
            
            if (string.IsNullOrEmpty(stateKey))
            {
                LogWarning("Invalid state key");
                return null;
            }
            
            // Try to find the state in current states
            foreach (var kvp in _currentStates)
            {
                if (kvp.Key.Contains(stateKey) || kvp.Value.ElementId == stateKey)
                {
                    return kvp.Value;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Save state immediately to storage
        /// </summary>
        private void SaveStateImmediate(string stateKey, UIStateData stateData)
        {
            try
            {
                switch (_persistenceConfig.StorageType)
                {
                    case UIStateStorageType.PlayerPrefs:
                        SaveStateToPlayerPrefs(stateKey, stateData);
                        break;
                        
                    case UIStateStorageType.FileSystem:
                        SaveStateToFileSystem(stateKey, stateData);
                        break;
                        
                    default:
                        LogWarning($"Storage type {_persistenceConfig.StorageType} not implemented");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to save state {stateKey}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load state from storage
        /// </summary>
        private UIStateData LoadStateFromStorage(string stateKey)
        {
            try
            {
                return _persistenceConfig.StorageType switch
                {
                    UIStateStorageType.PlayerPrefs => LoadStateFromPlayerPrefs(stateKey),
                    UIStateStorageType.FileSystem => LoadStateFromFileSystem(stateKey),
                    _ => null
                };
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to load state {stateKey}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Save state to PlayerPrefs
        /// </summary>
        private void SaveStateToPlayerPrefs(string stateKey, UIStateData stateData)
        {
            var json = JsonUtility.ToJson(stateData);
            
            // Validate and sanitize Unicode before saving
            json = SanitizeUnicodeString(json);
            
            if (_persistenceConfig.CompressData)
            {
                json = CompressString(json);
            }
            
            if (!_persistenceConfig.IsDataSizeValid(json.Length))
            {
                LogWarning($"State data too large for {stateKey}: {json.Length} bytes");
                return;
            }
            
            PlayerPrefs.SetString(stateKey, json);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Load state from PlayerPrefs
        /// </summary>
        private UIStateData LoadStateFromPlayerPrefs(string stateKey)
        {
            if (!PlayerPrefs.HasKey(stateKey))
                return null;
                
            var json = PlayerPrefs.GetString(stateKey);
            
            if (_persistenceConfig.CompressData)
            {
                json = DecompressString(json);
            }
            
            return JsonUtility.FromJson<UIStateData>(json);
        }
        
        /// <summary>
        /// Save state to file system
        /// </summary>
        private void SaveStateToFileSystem(string stateKey, UIStateData stateData)
        {
            var filePath = GetStateFilePath(stateKey);
            var directory = Path.GetDirectoryName(filePath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonUtility.ToJson(stateData, true);
            
            // Validate and sanitize Unicode before saving
            json = SanitizeUnicodeString(json);
            
            if (_persistenceConfig.CompressData)
            {
                json = CompressString(json);
            }
            
            if (!_persistenceConfig.IsDataSizeValid(json.Length))
            {
                LogWarning($"State data too large for {stateKey}: {json.Length} bytes");
                return;
            }
            
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
        
        /// <summary>
        /// Load state from file system
        /// </summary>
        private UIStateData LoadStateFromFileSystem(string stateKey)
        {
            var filePath = GetStateFilePath(stateKey);
            
            if (!File.Exists(filePath))
                return null;
                
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            
            if (_persistenceConfig.CompressData)
            {
                json = DecompressString(json);
            }
            
            return JsonUtility.FromJson<UIStateData>(json);
        }
        
        /// <summary>
        /// Get file path for state
        /// </summary>
        private string GetStateFilePath(string stateKey)
        {
            var fileName = stateKey.Replace(".", "_").Replace("/", "_") + ".json";
            return Path.Combine(Application.persistentDataPath, _persistenceConfig.FileStoragePath, fileName);
        }
        
        /// <summary>
        /// Check if state exists in storage
        /// </summary>
        private bool StateExistsInStorage(string stateKey)
        {
            return _persistenceConfig.StorageType switch
            {
                UIStateStorageType.PlayerPrefs => PlayerPrefs.HasKey(stateKey),
                UIStateStorageType.FileSystem => File.Exists(GetStateFilePath(stateKey)),
                _ => false
            };
        }
        
        /// <summary>
        /// Remove state from storage
        /// </summary>
        private void RemoveStateFromStorage(string stateKey)
        {
            switch (_persistenceConfig.StorageType)
            {
                case UIStateStorageType.PlayerPrefs:
                    PlayerPrefs.DeleteKey(stateKey);
                    PlayerPrefs.Save();
                    break;
                    
                case UIStateStorageType.FileSystem:
                    var filePath = GetStateFilePath(stateKey);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Clear all states from storage
        /// </summary>
        private void ClearAllStatesFromStorage()
        {
            switch (_persistenceConfig.StorageType)
            {
                case UIStateStorageType.PlayerPrefs:
                    // Note: PlayerPrefs doesn't have a direct way to delete by prefix
                    // This would require iterating through all keys
                    break;
                    
                case UIStateStorageType.FileSystem:
                    var directory = Path.Combine(Application.persistentDataPath, _persistenceConfig.FileStoragePath);
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Load all states from PlayerPrefs
        /// </summary>
        private int LoadAllStatesFromPlayerPrefs()
        {
            // PlayerPrefs doesn't provide enumeration, so this is limited
            // Would need to maintain a separate index of keys
            return 0;
        }
        
        /// <summary>
        /// Load all states from file system
        /// </summary>
        private int LoadAllStatesFromFileSystem()
        {
            var directory = Path.Combine(Application.persistentDataPath, _persistenceConfig.FileStoragePath);
            if (!Directory.Exists(directory))
                return 0;
                
            var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
            var loadedCount = 0;
            
            foreach (var file in files)
            {
                try
                {
                    var stateKey = Path.GetFileNameWithoutExtension(file).Replace("_", ".");
                    var stateData = LoadStateFromFileSystem(stateKey);
                    
                    if (stateData != null)
                    {
                        _currentStates[stateKey] = stateData;
                        _stateTimestamps[stateKey] = stateData.Timestamp;
                        _stateVersions[stateKey] = stateData.Version;
                        loadedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    LogWarning($"Failed to load state file {file}: {ex.Message}");
                }
            }
            
            return loadedCount;
        }
        
        /// <summary>
        /// Get next version number for state
        /// </summary>
        private int GetNextVersion(string stateKey)
        {
            return _stateVersions.TryGetValue(stateKey, out var version) ? version + 1 : 1;
        }
        
        /// <summary>
        /// Queue save operation for batch processing
        /// </summary>
        private void QueueSaveOperation(string stateKey, UIStateData stateData)
        {
            _pendingOperations.Enqueue(new UIStateOperation
            {
                Type = UIStateOperationType.Save,
                StateKey = stateKey,
                StateData = stateData,
                Timestamp = System.DateTime.Now
            });
        }
        
        /// <summary>
        /// Process pending operations
        /// </summary>
        private void ProcessPendingOperations()
        {
            while (_pendingOperations.Count > 0)
            {
                var operation = _pendingOperations.Dequeue();
                
                if (operation.Type == UIStateOperationType.Save)
                {
                    SaveStateImmediate(operation.StateKey, operation.StateData);
                    _pendingStateChanges--;
                }
            }
        }
        
        /// <summary>
        /// Compress string data
        /// </summary>
        private string CompressString(string input)
        {
            // Simple compression implementation
            // In a real implementation, you might use GZip or other compression
            return System.Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }
        
        /// <summary>
        /// Decompress string data
        /// </summary>
        private string DecompressString(string input)
        {
            // Simple decompression implementation
            try
            {
                var bytes = System.Convert.FromBase64String(input);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return input; // Return as-is if decompression fails
            }
        }
        
        // Event handlers
        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus && _isDirty)
            {
                SaveAllStates();
            }
        }
        
        private void OnApplicationPauseChanged(bool pauseStatus)
        {
            if (pauseStatus && _isDirty)
            {
                SaveAllStates();
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (!_isInitialized || _persistenceConfig == null || !_persistenceConfig.EnableAutosave)
                return;
                
            _autosaveTimer += Time.deltaTime;
            
            if (_autosaveTimer >= _persistenceConfig.AutosaveInterval && _isDirty)
            {
                SaveAllStates();
                _autosaveTimer = 0f;
            }
            
            // Process pending operations in batches
            if (_pendingOperations.Count > 0)
            {
                var operationsToProcess = Mathf.Min(5, _pendingOperations.Count);
                for (int i = 0; i < operationsToProcess; i++)
                {
                    if (_pendingOperations.Count > 0)
                    {
                        var operation = _pendingOperations.Dequeue();
                        if (operation.Type == UIStateOperationType.Save)
                        {
                            SaveStateImmediate(operation.StateKey, operation.StateData);
                            _pendingStateChanges--;
                        }
                    }
                }
            }
        }
        
        protected override void OnDestroy()
        {
            if (_isDirty)
            {
                SaveAllStates();
            }
            
            Application.focusChanged -= OnApplicationFocusChanged;
            // Application.pauseStateChanged -= OnApplicationPauseChanged;
            
            base.OnDestroy();
        }
        
        protected void OnValidate()
        {
            
            if (_persistenceConfig == null)
            {
                LogWarning("No persistence configuration assigned to UI State Manager");
            }
        }

        /// <summary>
        /// Sanitize Unicode string to prevent invalid surrogate pairs
        /// </summary>
        private string SanitizeUnicodeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var stringBuilder = new System.Text.StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                
                // Check for high surrogate
                if (char.IsHighSurrogate(c))
                {
                    // Ensure there's a valid low surrogate following
                    if (i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                    {
                        // Valid surrogate pair, add both characters
                        stringBuilder.Append(c);
                        stringBuilder.Append(input[i + 1]);
                        i++; // Skip the low surrogate in next iteration
                    }
                    else
                    {
                        // Invalid high surrogate, replace with Unicode replacement character
                        stringBuilder.Append('\uFFFD');
                        if (_enableDebugLogging)
                            UnityEngine.Debug.LogWarning($"Invalid Unicode high surrogate at position {i}, replaced with Unicode replacement character");
                    }
                }
                else if (char.IsLowSurrogate(c))
                {
                    // Orphaned low surrogate, replace with Unicode replacement character
                    stringBuilder.Append('\uFFFD');
                    if (_enableDebugLogging)
                        UnityEngine.Debug.LogWarning($"Orphaned Unicode low surrogate at position {i}, replaced with Unicode replacement character");
                }
                else
                {
                    // Regular character, add as-is
                    stringBuilder.Append(c);
                }
            }
            
            return stringBuilder.ToString();
        }
    }
    
    /// <summary>
    /// UI state data container
    /// </summary>
    [System.Serializable]
    public class UIStateData
    {
        public string ElementId;
        public UIStateCategory Category;
        public object Data;
        public System.DateTime Timestamp;
        public int Version;
        
        // Compatibility properties for testing
        public string StateId { get => ElementId; set => ElementId = value; }
        public int StateVersion { get => Version; set => Version = value; }
        public object StateData { get => Data; set => Data = value; }
    }
    
    /// <summary>
    /// UI state operation for batch processing
    /// </summary>
    public struct UIStateOperation
    {
        public UIStateOperationType Type;
        public string StateKey;
        public UIStateData StateData;
        public System.DateTime Timestamp;
    }
    
    /// <summary>
    /// UI state operation types
    /// </summary>
    public enum UIStateOperationType
    {
        Save,
        Load,
        Remove
    }
}